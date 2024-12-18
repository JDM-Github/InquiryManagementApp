using InquiryManagementApp.Models;
using Microsoft.AspNetCore.Mvc;

public class AdminController : Controller
{
    private readonly EmailService _emailService;
    private readonly ApplicationDbContext _context;

    public AdminController(EmailService emailService, ApplicationDbContext context)
    {
        _emailService = emailService;
        _context = context;
    }

    public IActionResult Index()
    {
        var viewModel = new AdminDashboardViewModel
        {
            TotalInquiries = _context.Inquiries.Count(),
            TotalEnrolled = _context.Students.Where(e => !e.IsRejected && !e.IsApproved).Count(),
            TotalApproved = _context.Students.Where(e => e.IsApproved).Count(),
            TotalRevenue = _context.Students.Sum(e => e.FeePaid)
        };
        return View(viewModel);
    }

    public IActionResult Logout()
    {
        HttpContext.Session.SetString("isAdmin", "0");
        return RedirectToAction("Index", "Home");
    }

    public IActionResult ManageEnrolled()
    {
        var enrollments = _context.Students.Where(e => e.IsApproved).ToList();
        return View(enrollments);
    }

    public IActionResult ManageEnrollees()
    {
        var enrollments = _context.Students.Where(e => !e.IsRejected).ToList();
        return View(enrollments);
    }

    public IActionResult ManageInquiries()
    {
        var inquiries = _context.Inquiries.ToList();
        return View(inquiries);
    }

    public IActionResult ManageAccounts()
    {
        var accounts = _context.Accounts;
        if (accounts == null)
        {
            return View(new List<Account>());
        }
        else
        {
            return View(_context.Accounts.ToList());
        }
    }

    public IActionResult ManageFees()
    {
        var feeListModel = new FeeListModel();
        return View(feeListModel);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Account account, string confirmPassword)
    {
        if (account.Password != confirmPassword)
        {
            ViewBag.ErrorMessage = "Passwords do not match.";
            return View();
        }

        if (ModelState.IsValid)
        {
            _context.Accounts.Add(account);
            _context.SaveChanges();
            return RedirectToAction("ManageAccounts");
        }
        return View(account);
    }



    [HttpPost]
    public async Task<IActionResult> SetEnrollmentSchedule(DateTime startDate, DateTime endDate)
    {
        try
        {
            var existingSchedule = _context.EnrollmentSchedules.FirstOrDefault();

            if (existingSchedule != null)
            {
                existingSchedule.StartDate = startDate;
                existingSchedule.EndDate = endDate;
                EnrollmentSchedule.CreateInstance(startDate, endDate);
                _context.EnrollmentSchedules.Update(existingSchedule);
            }
            else
            {
                var enrollmentSchedule = new EnrollmentSchedule
                {
                    StartDate = startDate,
                    EndDate = endDate
                };

                EnrollmentSchedule.CreateInstance(startDate, endDate);
                _context.EnrollmentSchedules.Add(enrollmentSchedule);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Admin");
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = $"Error: {ex.Message}";
            return RedirectToAction("Index", "Admin");
        }
    }

    [HttpPost]
    public async Task<IActionResult> ClearEnrollment()
    {
        try
        {
            var existingSchedule = _context.EnrollmentSchedules.FirstOrDefault();

            if (existingSchedule != null)
            {
                _context.EnrollmentSchedules.Remove(existingSchedule);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Admin");
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = $"Error: {ex.Message}";
            return RedirectToAction("Index", "Admin");
        }
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
            return NotFound();
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
            return RedirectToAction("Index", "Student");
        }
        return RedirectToAction("ManageAccounts", "Admin");
    }


    [HttpPost]
    public async Task<IActionResult> Enroll(Enrollment enrollment)
    {
        if (ModelState.IsValid)
        {
            enrollment.IsApproved = true;
            enrollment.SubmissionDate = DateTime.Now;
            enrollment.SetTemporaryCredentials();

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
            return RedirectToAction("ManageEnrollees", "Admin");
        }
        return RedirectToAction("ManageEnrollees", "Admin");
    }

}
