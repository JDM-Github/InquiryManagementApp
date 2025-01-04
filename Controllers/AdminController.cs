using InquiryManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class AdminController : Controller
{
    private readonly EmailService _emailService;
    private readonly ApplicationDbContext _context;

    public AdminController(EmailService emailService, ApplicationDbContext context)
    {
        _emailService = emailService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var existingSchedule = _context.PaymentSchedules.FirstOrDefault();
        if (existingSchedule != null && PaymentSchedule.CurrentPaymentSchedule == null)
        {
            PaymentSchedule.CreateInstance(existingSchedule);
        }

        var schedule = await _context.PaymentSchedules.FirstOrDefaultAsync();
        if (schedule == null)
        {
            schedule = new PaymentSchedule();
        }

        var cancellationAnalytics = _context.Inquiries
            .Where(i => i.IsCancelled)
            .GroupBy(i => i.CancellationReason)
            .Select(g => new { Reason = g.Key, Count = g.Count() })
            .ToDictionary(g => g.Reason ?? "Unknown", g => g.Count);

        var enrollmentTrends = _context.Students
            .Where(s => s.ApprovedEnrolled.HasValue)
            .GroupBy(s => s.ApprovedEnrolled!.Value.Year)
            .Select(g => new
            {
                Year = g.Key,
                Count = g.Count()
            })
            .ToList();

        var currentYear = DateTime.Now.Year;
        var yearRange = Enumerable.Range(currentYear - 5, 11).ToList();
        var enrollmentTrendData = yearRange.ToDictionary(year => year, year => enrollmentTrends.FirstOrDefault(e => e.Year == year)?.Count ?? 0);

        var recentActivities = await _context.RecentActivities
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync();

        var viewModel = new AdminDashboardViewModel
        {
            TotalInquiries = _context.Inquiries.Count(),
            TotalEnrolled = _context.Students.Where(e => !e.IsRejected && !e.IsApproved).Count(),
            TotalApproved = _context.Students.Where(e => e.IsApproved).Count(),
            TotalRevenue = _context.Payments.Sum(e => e.PaidAmount),
            CurrentPayment = schedule,
            CancellationAnalytics = cancellationAnalytics,
            EnrollmentTrends = enrollmentTrendData,
            RecentActivities = recentActivities
        };
        ViewBag.ActiveTab = "Index";
        return View(viewModel);
    }

    public IActionResult Logout()
    {
        HttpContext.Session.SetString("isAdmin", "0");
        return RedirectToAction("Index", "Home");
    }

    public IActionResult ManageEnrolled()
    {
        var enrollments = _context.Students.Where(e => e.IsApproved && !e.IsDeleted).ToList();
        ViewBag.ActiveTab = "ManageEnrolled";
        return View(enrollments);
    }

    public IActionResult ManageEnrollees()
    {
        var enrollments = _context.Students.Where(e => !e.IsRejected && !e.IsWalkin && !e.IsDeleted).ToList();
        ViewBag.ActiveTab = "ManageEnrollees";
        return View(enrollments);
    }

    public IActionResult ManageInquiries()
    {
        var inquiries = _context.Inquiries.ToList();
        ViewBag.ActiveTab = "ManageInquiries";
        return View(inquiries);
    }

    // ----------------------------------------------------------
    public async Task<IActionResult> ManageAccounts(int page = 1, string search = "", string role = "All")
    {
        int pageSize = 10;
        var query = _context.Accounts.AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(f => f.Email.Contains(search) || f.Username.Contains(search));
        }

        if (role != "All")
        {
            query = query.Where(f => f.Role == role);
        }

        var accounts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPayments = accounts.Count();
        var totalPages = (int)Math.Ceiling(totalPayments / (double)pageSize);

        var viewModel = new AccountView
        {
            Accounts = accounts,
            CurrentPage = page,
            TotalPages = totalPages,
            SearchFilter = search,
            RoleFilter = role
        };

        ViewBag.ActiveTab = "ManageAccounts";
        return View(viewModel);
    }
    // ----------------------------------------------------------


    public async Task<IActionResult> ManageFees()
    {
        var allFees = await _context.Fees.ToListAsync();
        var feeListModel = new FeeListModel
        {
            Fees = allFees
        };
        ViewBag.ActiveTab = "ManageFees";
        return View(feeListModel);
    }

    public async Task<IActionResult> ManageTransactions(int page = 1, string search = "")
    {
        int pageSize = 10;
        var query = _context.Payments.AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(f => f.ReferenceNumber.Contains(search));
        }

        var payments = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPayments = payments.Count();
        var totalPages = (int)Math.Ceiling(totalPayments / (double)pageSize);

        var enrollments = await _context.Students.ToListAsync();
        var paymentViewModels = payments.Select(payment => new PaymentViewModel
        {
            Payment = payment,
            Enrollees = enrollments.FirstOrDefault(e => e.EnrollmentId == payment.EnrollreesId)
        }).ToList();

        var viewModel = new PaymentsManagementViewModel
        {
            Payments = paymentViewModels,
            CurrentPage = page,
            TotalPages = totalPages,
            SearchFilter = search
        };

        ViewBag.ActiveTab = "ManageTransactions";
        return View(viewModel);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Account account, string confirmPassword)
    {
        if (account.Password != confirmPassword)
        {
            TempData["ErrorMessage"] = "Passwords do not match.";
            return View();
        }

        if (ModelState.IsValid)
        {
            if (await _context.Accounts.FirstOrDefaultAsync(a => a.Email == account.Email) != null)
            {
                TempData["ErrorMessage"] = "Email already exists.";
                return View(account);
            }
            if (await _context.Accounts.FirstOrDefaultAsync(a => a.Username == account.Username) != null)
            {
                TempData["ErrorMessage"] = "Username already exists.";
                return View(account);
            }
            var recent = new RecentActivity
            {
                Activity = $"New {account.Role} Account Created",
                CreatedAt = DateTime.Now
            };
            _context.RecentActivities.Add(recent);
            await _context.SaveChangesAsync();

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Account created successfully.";
            return RedirectToAction("ManageAccounts");
        }
        TempData["ErrorMessage"] = "Error creating account.";
        return View(account);
    }

    [HttpPost]
    public async Task<IActionResult> SetEnrollmentSchedule(DateTime startDate, DateTime endDate)
    {

        if (EnrollmentSchedule.InstanceExists)
        {
            var schedule = _context.EnrollmentSchedules.FirstOrDefault();
            schedule!.StartDate = startDate;
            schedule.EndDate = endDate;
            _context.EnrollmentSchedules.Update(schedule!);
            _context.SaveChanges();

            var recent2 = new RecentActivity
            {
                Activity = "Enrollment schedule updated",
                CreatedAt = DateTime.Now
            };
            _context.RecentActivities.Add(recent2);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Enrollment schedule updated successfully.";
            return RedirectToAction("Index", "Admin");
        }
        EnrollmentSchedule.CreateInstance();
        _context.EnrollmentSchedules.Add(new EnrollmentSchedule
        {
            StartDate = startDate,
            EndDate = endDate
        });

        var recent = new RecentActivity
        {
            Activity = "New Enrollment schedule created",
            CreatedAt = DateTime.Now
        };
        _context.RecentActivities.Add(recent);
        await _context.SaveChangesAsync();

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Enrollment schedule created successfully.";
        return RedirectToAction("Index", "Admin");
    }

    [HttpPost]
    public async Task<IActionResult> ClearEnrollment()
    {
        if (EnrollmentSchedule.InstanceExists)
        {
            var existingSchedule = _context.EnrollmentSchedules.FirstOrDefault();
            _context.EnrollmentSchedules.Remove(existingSchedule!);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index", "Admin");
    }

    [HttpPost]
    public async Task<IActionResult> Approve(int id)
    {
        var enrollment = _context.Students.FirstOrDefault(e => e.EnrollmentId == id);

        if (enrollment == null)
        {
            return NotFound();
        }

        if (!enrollment.IsApproved)
        {
            enrollment.ApprovedEnrolled = DateTime.Now;
            enrollment.IsApproved = true;
            _context.SaveChanges();

            var subject = "Enrollment Approved";
            var body = $"Dear {enrollment.Firstname} {enrollment.Surname},<br>Your enrollment has been approved. Thank you for completing the necessary requirements. Your new email and password is username: {enrollment.Username}, password: {enrollment.Password}";
            await _emailService.SendEmailAsync(enrollment.Email, subject, body);

            var notification = new Notification
            {
                Message = $"Your enrollment has been approved.",
                UserId = enrollment.LRN,
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var recent = new RecentActivity
            {
                Activity = $"Enrollment {enrollment.Firstname} {enrollment.Surname} approved",
                CreatedAt = DateTime.Now
            };
            _context.RecentActivities.Add(recent);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageEnrolled", "Admin");
        }
        return RedirectToAction("ManageAccounts", "Admin");
    }

    [HttpPost]
    public async Task<IActionResult> Reject(int id)
    {
        var enrollment = _context.Students.FirstOrDefault(e => e.EnrollmentId == id);

        if (enrollment == null)
        {
            TempData["ErrorMessage"] = "Enrollment not found.";
            return RedirectToAction("ManageEnrollees", "Admin");
        }

        if (!enrollment.IsApproved)
        {
            enrollment.IsRejected = true;
            _context.SaveChanges();

            var subject = "Enrollment Rejected";
            var body = $"Dear {enrollment.Firstname} {enrollment.Surname},<br>Your enrollment has been rejected. Please contact the administration for further details.";
            await _emailService.SendEmailAsync(enrollment.Email, subject, body);

            var notification = new Notification
            {
                Message = $"Your enrollment has been rejected.",
                UserId = enrollment.LRN,
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var recent = new RecentActivity
            {
                Activity = $"Enrollment {enrollment.Firstname} {enrollment.Surname} rejected",
                CreatedAt = DateTime.Now
            };
            _context.RecentActivities.Add(recent);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Enrollment rejected successfully.";
            return RedirectToAction("ManageEnrolled", "Admin");
        }
        TempData["ErrorMessage"] = "Enrollment already approved.";
        return RedirectToAction("ManageEnrollees", "Admin");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEnrollment(int id)
    {
        var enrollment = _context.Students.FirstOrDefault(e => e.EnrollmentId == id);
        if (enrollment == null)
        {
            TempData["ErrorMessage"] = "Enrollment not found.";
            return RedirectToAction("ManageEnrollees", "Admin");
        }

        var subject = "Enrollment Deleted";
        var body = $"Dear {enrollment.Firstname} {enrollment.Surname},<br>Your enrollment has been deleted. Please contact the administration for further details.";
        await _emailService.SendEmailAsync(enrollment.Email, subject, body);

        enrollment.IsDeleted = true;
        _context.Update(enrollment);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Enrollment deleted successfully.";
        return RedirectToAction("ManageEnrollees", "Admin");
    }

    [HttpPost]
    public async Task<IActionResult> Enroll(Enrollment enrollment)
    {
        if (ModelState.IsValid)
        {
            if (await _context.Students.FirstOrDefaultAsync(s => s.LRN == enrollment.LRN) != null
                || await _context.Students.FirstOrDefaultAsync(s => s.Email == enrollment.Email) != null)
            {
                TempData["ErrorMessage"] = "Enrollment already exists.";
                return RedirectToAction("ManageEnrolled", "Admin");
            }

            enrollment.IsApproved = true;
            enrollment.SubmissionDate = DateTime.Now;
            enrollment.IsWalkin = true;
            enrollment.SetTemporaryCredentials();
            enrollment.ApprovedEnrolled = DateTime.Now;

            var notification = new Notification
            {
                Message = $"Student {enrollment.Firstname} {enrollment.Surname} has been successfully enrolled and approved.",
                UserId = enrollment.LRN,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            var subject = "Enrollment Approved";
            var body = $"Dear {enrollment.Firstname} {enrollment.Surname},<br>Your enrollment has been approved. Thank you for completing the necessary requirements. Your new email and password is username: {enrollment.Username}, password: {enrollment.Password}";
            await _emailService.SendEmailAsync(enrollment.Email, subject, body);

            _context.Notifications.Add(notification);
            _context.Students.Add(enrollment);
            await _context.SaveChangesAsync();

            var recent = new RecentActivity
            {
                Activity = $"New Enrollment {enrollment.Firstname} {enrollment.Surname}",
                CreatedAt = DateTime.Now
            };
            _context.RecentActivities.Add(recent);
            await _context.SaveChangesAsync();

            var requiredFiles = await _context.Requirements
                .Where(r => r.GradeLevel == enrollment.GradeLevel)
                .ToListAsync();

            var requirementModels = requiredFiles.Select(r => new RequirementModel
            {
                RequirementName = r.RequirementName,
                Description = r.Description,
                UploadedFile = r.UploadedFile,
                IsRequired = r.IsRequired,
                GradeLevel = r.GradeLevel,
                EnrollmentId = enrollment.EnrollmentId
            }).ToList();

            _context.RequirementModels.AddRange(requirementModels);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Enrollment created successfully.";
            return RedirectToAction("ManageEnrolled", "Admin");
        }
        TempData["ErrorMessage"] = "Error creating enrollment.";
        return RedirectToAction("ManageEnrolled", "Admin");
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendNote(int id, string noteContent, string action)
    {
        var inquiry = await _context.Inquiries.FindAsync(id);
        if (inquiry == null)
        {
            TempData["ErrorMessage"] = "Inquiry not found.";
            return RedirectToAction(nameof(Index));
        }

        string emailSubject = "Inquiry Update";
        string emailBody = $@"
                <p>Dear {inquiry.StudentName},</p>
                <p>{noteContent}</p>
                <p>For further communication. here is our contact information:</p>
                <ul>
                </ul>
                <p>Best regards,<br>Your Team</p>";

        switch (action)
        {
            case "Verify":
                inquiry.IsApproved = true;
                _context.SaveChanges();

                emailSubject = "Inquiry Verification";
                emailBody = $@"
                <p>Dear {inquiry.StudentName},</p>
                <p>{noteContent}</p>
                <p>We are pleased to inform you that your inquiry has been verified successfully.</p>
                <p>Thank you for reaching out to us!</p>
                <p>For further communication. here is our contact information:</p>
                <ul>
                </ul>
                <p>Best regards,<br>Your Team</p>";
                break;

            case "Reject":
                inquiry.IsRejected = true;
                _context.SaveChanges();

                emailSubject = "Inquiry Rejected";
                emailBody = $@"
                <p>Dear {inquiry.StudentName},</p>
                <p>{noteContent}</p>
                <p>We regret to inform you that your inquiry has been rejected.</p>
                <p>If you have any questions, please feel free to contact us.</p>
                <p>For further communication. here is our contact information:</p>
                <ul>
                </ul>
                <p>Best regards,<br>Your Team</p>";
                break;

            default:
                break;
        }

        try
        {
            await _emailService.SendEmailAsync(inquiry.EmailAddress, emailSubject, emailBody);
            TempData["SuccessMessage"] = "Note sent successfully.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Failed to send the note. Please try again.";
            Console.WriteLine(ex.Message);
        }

        return RedirectToAction("ManageInquiries", "Admin");
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveInquiry(int id)
    {
        var inquiry = await _context.Inquiries.FindAsync(id);

        if (inquiry == null)
        {
            TempData["ErrorMessage"] = "Inquiry not found.";
            return RedirectToAction("Index");
        }

        inquiry.IsApproved = true;
        _context.Update(inquiry);
        await _context.SaveChangesAsync();

        string subject = "Inquiry Approved";
        string enrollmentUrl = Url.Action("Index", "Home", null, Request.Scheme) ?? "";
        string body = $@"
            <p>Dear {inquiry.StudentName},</p>
            <p>Your inquiry has been approved.</p>
            <p>You can proceed to the enrollment process by visiting the following link:</p>
            <p><a href='{enrollmentUrl}'>{enrollmentUrl}</a></p>
            <p>Thank you for your interest!</p>
            <p>Best regards,<br>Your Team</p>";

        var recent = new RecentActivity
        {
            Activity = $"Inquiry {inquiry.StudentName} approved",
            CreatedAt = DateTime.Now
        };
        _context.RecentActivities.Add(recent);
        await _context.SaveChangesAsync();

        await _emailService.SendEmailAsync(inquiry.EmailAddress, subject, body);
        TempData["SuccessMessage"] = "Inquiry approved and email sent.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> EditAccount(int AccountId, string Username, string Email, string Role)
    {
        var existingAccount = await _context.Accounts.FindAsync(AccountId);
        if (existingAccount == null)
        {
            TempData["ErrorMessage"] = "Account not found.";
            return RedirectToAction("ManageAccounts");
        }

        existingAccount.Username = Username;
        existingAccount.Email = Email;
        existingAccount.Role = Role;
        try
        {
            _context.Accounts.Update(existingAccount);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Account updated successfully.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error updating account: {ex.Message}";
        }
        return RedirectToAction("ManageAccounts");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAccount(int accountId)
    {
        var account = await _context.Accounts.FindAsync(accountId);
        if (account == null)
        {
            TempData["ErrorMessage"] = "Account not found.";
            return RedirectToAction("ManageAccounts");
        }
        try
        {
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Account deleted successfully.";

            var recent = new RecentActivity
            {
                Activity = $"Account {account.Username} deleted",
                CreatedAt = DateTime.Now
            };
            _context.RecentActivities.Add(recent);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting account: {ex.Message}";
        }

        return RedirectToAction("ManageAccounts");
    }

}
