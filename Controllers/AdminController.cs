using Helper;
using InquiryManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.EntityFrameworkCore;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;

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

        var enrollmentStatusAnalytics = _context.Inquiries
            .GroupBy(i => i.IsEnrolled)
            .Select(g => new
            {
                Status = g.Key ? "Enrolled" : "Inquiry",
                Count = g.Count()
            })
            .ToDictionary(g => g.Status, g => g.Count);

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

        var inquiries = await _context.Inquiries
            .OrderByDescending(i => i.CreatedAt)
            .Take(10) 
            .ToListAsync();

        var recentActivities = await _context.RecentActivities
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync();

        var viewModel = new AdminDashboardViewModel
        {
            TotalInquiries = _context.Inquiries.Count(e => !e.IsInquired),
            TotalEnrolled = _context.Students.Where(e => !e.IsRejected && !e.IsApproved).Count(),
            TotalApproved = _context.Students.Where(e => e.IsEnrolled && e.IsApproved).Count(),
            TotalRevenue = _context.StudentPayments.Sum(e => e.PaymentAmount),
            CurrentPayment = schedule,
            CancellationAnalytics = enrollmentStatusAnalytics,
            EnrollmentTrends = enrollmentTrendData,
            RecentActivities = recentActivities,
            Inquiries = inquiries
        };
        ViewBag.ActiveTab = "Index";
        return View(viewModel);
    }

    public IActionResult Logout()
    {
        HttpContext.Session.SetString("isAdmin", "0");
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> ManageEnrolled(int page = 1, string search = "", string grade = "")
    {
        int pageSize = 10;
        var query = _context.Students.Where(e => e.IsEnrolled && !e.IsDeleted).AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(f => f.Email.Contains(search) || f.Username.Contains(search));
        }

        if (grade != "")
        {
            query = query.Where(f => f.GradeLevel == grade);
        }

        var enrollments = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPayments = enrollments.Count();
        var totalPages = (int)Math.Ceiling(totalPayments / (double)pageSize);

        var viewModel = new ManageEnrolledView
        {
            Enrolled = enrollments,
            CurrentPage = page,
            TotalPages = totalPages,
            SearchFilter = search,
            GradeFilter = grade
        };

        ViewBag.ActiveTab = "ManageEnrolled";
        return View(viewModel);
    }

    public async Task<IActionResult> ManageEnrollees(int page = 1, string search = "", string grade = "", string status = "")
    {
        int pageSize = 10;
        var query = _context.Students.Where(e => !e.IsRejected && !e.IsWalkin && !e.IsDeleted && !e.IsEnrolled).AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(f => f.Email.Contains(search) || f.Username.Contains(search));
        }

        if (grade != "")
        {
            query = query.Where(f => f.GradeLevel == grade);
        }
        if (status != "")
        {
            if (status == "Approved")
                query = query.Where(f => f.IsApproved);

            if (status == "Rejected")
                query = query.Where(f => f.IsRejected);

            if (status == "Pending")
                query = query.Where(f => !f.IsApproved && !f.IsRejected);
        }

        var enrollments = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPayments = enrollments.Count();
        var totalPages = (int)Math.Ceiling(totalPayments / (double)pageSize);

        var viewModel = new ManageEnrolledView
        {
            Enrolled = enrollments,
            CurrentPage = page,
            TotalPages = totalPages,
            SearchFilter = search,
            GradeFilter = grade
        };
        ViewBag.ActiveTab = "ManageEnrollees";
        return View(viewModel);
    }

    public async Task<IActionResult> ManageInquiries(int page = 1, string search = "", string status = "", string rstatus = "")
    {
        int pageSize = 10;
        var query = _context.Inquiries.AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(f => f.EmailAddress.Contains(search));
        }

        if (status != "")
        {
            if (status == "Answered")
                query = query.Where(f => f.IsInquired);

            if (status == "NAnswered")
                query = query.Where(f => !f.IsInquired);
        }

        if (rstatus != "")
        {
            if (rstatus == "Inquiry")
                query = query.Where(f => !f.IsEnrolled);

            if (rstatus == "Enrolled")
                query = query.Where(f => f.IsEnrolled);
        }

        var inquiries = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPayments = inquiries.Count();
        var totalPages = (int)Math.Ceiling(totalPayments / (double)pageSize);

        var viewModel = new InquiryView
        {
            Inquiries = inquiries,
            CurrentPage = page,
            TotalPages = totalPages,
            SearchFilter = search,
            StatusFilter = status,
            RStatusFilter = rstatus
        };

        ViewBag.ActiveTab = "ManageInquiries";
        return View(viewModel);
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
        Console.WriteLine(totalPayments);
        var totalPages = (int)Math.Ceiling(totalPayments / (double)pageSize);
        Console.WriteLine(totalPages);
        Console.WriteLine(page);

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

    public async Task<IActionResult> AllFees()
    {
        var fee = await _context.Fees.FirstOrDefaultAsync();
        return View(fee);
    }

    public async Task<IActionResult> UpdateTuition(Fee newFee)
    {
        var fee = await _context.Fees.FirstOrDefaultAsync();
        if (fee == null)
        {
            TempData["ErrorMessage"] = "Fees not found.";
            return RedirectToAction("AllFees", "Admin");
        }
        fee.TuitionFee = newFee.TuitionFee;
        fee.Miscellaneous = newFee.Miscellaneous;
        _context.Fees.Update(fee);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Fees updated successfully.";
        return RedirectToAction("AllFees", "Admin");
    }

    public async Task<IActionResult> ManageTransactions(int page = 1, string search = "", string month = "", string year = "")
    {
        int pageSize = 10;
        var query = _context.StudentPayments.AsQueryable();
        query = query.Where(f => f.Status == "Paid");
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(f => f.ReferenceNumber.Contains(search));
        }

        if (!string.IsNullOrEmpty(month))
        {
            query = query.Where(f => f.MonthPaid == month);
        }
        if (!string.IsNullOrEmpty(year))
        {
            query = query.Where(f => f.YearPaid == year);
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
            Enrollees = enrollments.FirstOrDefault(e => e.EnrollmentId == payment.UserId)
        }).ToList();

        var viewModel = new PaymentsManagementViewModel
        {
            Payments = paymentViewModels,
            CurrentPage = page,
            TotalPages = totalPages,
            SearchFilter = search,
            MonthFilter = month,
            YearFilter = year
        };

        ViewBag.ActiveTab = "ManageTransactions";
        return View(viewModel);
    }

    [HttpGet]
    public async Task<JsonResult> GetStudentName(string lrn)
    {
        var fee = await _context.Fees.FirstOrDefaultAsync();
        if (fee == null)
        {
            return Json(null);
        }
        var TuitionFee = fee.TuitionFee;
        var Miscellaneous = fee.Miscellaneous;
        var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentID == lrn || s.LRN == lrn);

        if (student != null)
        {
            if (!student.IsEnrolled)
            {
                var payment = await _context.StudentPaymentRecords.FirstOrDefaultAsync(p => p.UserId == student.EnrollmentId);
                Console.WriteLine("----------------------------------------");
                Console.WriteLine(payment);
                Console.WriteLine("----------------------------------------");

                var IsEnrollmentActive = false;
                var schedule = _context.EnrollmentSchedules.FirstOrDefault();
                if (schedule != null)
                {
                    IsEnrollmentActive = DateTime.Now >= schedule.StartDate && DateTime.Now <= schedule.EndDate;
                }

                var EarlyBird = IsEnrollmentActive;
                var SiblingDiscount = Math.Min(5, student.NumberOfSibling);
                var allDiscount = SiblingDiscount + (EarlyBird ? 1 : 0);

                return Json(new
                {
                    name = $"{student.Firstname} {student.Surname}",
                    isEnrolled = false,
                    balance = payment!.Balance,
                    allDiscount,
                    alreadyPaid = false,
                    TuitionFee,
                    Miscellaneous,
                    student.PaymentType,
                    student.TotalToPay,
                    student.PayPerDate
                });
            }
            else
            {
                var payment = await _context.StudentPaymentRecords.FirstOrDefaultAsync(p => p.UserId == student.EnrollmentId);
                var spayment = await _context.StudentPayments.FirstOrDefaultAsync(p => p.UserId == student.EnrollmentId && p.Status == "Pending");

                if (spayment == null)
                {
                    if (student.BalanceToPay > 0)
                    {
                        return Json(new
                        {
                            name = $"{student.Firstname} {student.Surname}",
                            balance = student.BalanceToPay,
                            isEnrolled = true,
                            allDiscount = -1,
                            alreadyPaid = false,
                            both = false,

                            TuitionFee,
                            Miscellaneous,
                            student.PaymentType,
                            student.TotalToPay,
                            student.PayPerDate
                        });
                    }
                    return Json(new
                    {
                        name = $"{student.Firstname} {student.Surname}",
                        alreadyPaid = true,

                        TuitionFee,
                        Miscellaneous,
                        student.PaymentType,
                        student.TotalToPay,
                        student.PayPerDate
                    });
                }

                return Json(new
                {
                    name = $"{student.Firstname} {student.Surname}",
                    paymentType = student.PaymentType,
                    perPayment = student.PayPerDate,
                    paymentId = spayment?.Id,
                    paymentTargetMonth = spayment?.MonthPaid,
                    paymentTargetYear = spayment?.YearPaid,
                    isEnrolled = true,
                    balance = student.BalanceToPay,
                    allDiscount = -1,
                    alreadyPaid = false,
                    both = true,

                    TuitionFee,
                    Miscellaneous,
                    student.PaymentType,
                    student.TotalToPay,
                    student.PayPerDate
                });
            }
        }

        return Json(null);
    }

    public async Task<IActionResult> SubmitWalkInPayment(string EnrollreesId, string? PaymentOption = null, int? PaymentID = null, double? UserWillPay = null, string? PaymentType = null, string? Target = null)
    {
        if (!string.IsNullOrEmpty(Target))
        {
            if (Target == "First")
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentID == EnrollreesId);
                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return RedirectToAction("Index");
                }

                var studentPay = await _context.StudentPaymentRecords.FirstOrDefaultAsync(s => s.UserId == student.EnrollmentId);
                if (studentPay == null)
                {
                    TempData["ErrorMessage"] = "Payment record not found.";
                    return RedirectToAction("Index");
                }
                studentPay.PaymentType = PaymentType;
                student.BalanceToPay = student.TotalToPay - (UserWillPay ?? 0);
                var firstPayment = UserWillPay ?? 0;

                student.IsEnrolled = true;
                student.ApprovedEnrolled = DateTime.Now;
                student.IsApproved = true;

                student.Username = $"temp{student.Firstname}{student.StudentID}{student.Surname}";
                student.Password = GenerateSecurePassword();

                student.EnrolledDate = DateTime.Now;
                _context.Update(student);
                await _context.SaveChangesAsync();

                var subject = "Enrollment Payment Success";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f9ff; padding: 20px;'>
                    <table style='width: 100%; max-width: 600px; margin: auto; background-color: #fff; border: 1px solid #d9e6f2; border-radius: 8px;'>
                        <thead style='background-color: #0056b3; color: #fff;'>
                            <tr>
                                <th style='padding: 15px; text-align: left; display: flex; align-items: center;'>
                                    <img src='https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTO9a84kDZORy-tOxHr1uSsYZM4hubrh6AThQ&s' alt='School Logo' style='height: 50px; margin-right: 15px;'>
                                    <div>
                                        <h2 style='margin: 0; font-size: 24px;'>DE ROMAN MONTESSORI SCHOOL</h2>
                                        <p style='margin: 0; font-size: 14px;'>Your gateway to excellence in education</p>
                                    </div>
                                </th>
                            </tr>
                        </thead>
                            <tbody>
                                <tr>
                                    <td style='padding: 20px;'>
                                        <p style='font-size: 16px; color: #0056b3;'>Dear {student.Firstname} {student.Surname},</p>
                                        <p style='font-size: 14px;'>We are pleased to inform you that your payment of {firstPayment} has been successfully processed.</p>
                                        <p style='font-size: 14px;'>Here are your account details:</p>
                                        <p><strong>Username:</strong> {student.Username}</p>
                                        <p><strong>Password:</strong> {student.Password}</p>
                                        <p style='font-size: 14px;'>Should you have any questions or require further assistance, do not hesitate to contact us:</p>
                                    <ul style='font-size: 14px; color: #333;'>
                                        <li><strong>Email:</strong> <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></li>
                                        <li><strong>Phone:</strong> 09274044188</li>
                                    </ul>
                                    <p style='font-size: 14px;'>Thank you for choosing our services. We are here to support you every step of the way.</p>
                                </td>
                            </tr>
                        </tbody>
                        <tfoot style='background-color: #fbe052; color: #0056b3;'>
                            <tr>
                                <td style='padding: 10px; text-align: center; font-size: 12px;'>
                                    <p style='margin: 0;'>De Roman Montessori School, Tanza, Philippines</p>
                                    <p style='margin: 0;'>Contact us: 09274044188 | <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></p>
                                    <p style='margin: 0;'>&copy; {DateTime.Now.Year} De Roman Montessori School. All rights reserved.</p>
                                </td>
                            </tr>
                        </tfoot>
                        </table>
                    </div>";

                await _emailService.SendEmailAsync(student.Email, subject, body);
                var notification = new Notification
                {
                    Message = $"You've successfully been enrolled.",
                    UserId = student.EnrollmentId,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                var recent = new RecentActivity
                {
                    Activity = $"Enrollment payment success for {student.Firstname} {student.Surname}. Amount: {firstPayment}",
                    CreatedAt = DateTime.Now
                };
                _context.RecentActivities.Add(recent);
                await _context.SaveChangesAsync();

                DateTime date = DateTime.Now;
                string monthName = date.ToString("MMMM");
                var payment = new StudentPayment
                {
                    UserId = student.EnrollmentId,
                    ReferenceNumber = Guid.NewGuid().ToString(),
                    PaymentAmount = firstPayment,
                    MonthPaid = monthName,
                    YearPaid = DateTime.Now.Year.ToString(),
                    Status = "Paid",
                    PaymentFor = "First Payment",
                    Date = DateTime.Now
                };
                _context.StudentPayments.Add(payment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Payment successful. Student {student.Firstname} {student.Surname} has been enrolled!";
                return RedirectToAction("ManageTransactions");
            }

            if (Target == "Balance")
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentID == EnrollreesId);
                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return RedirectToAction("Index");
                }

                student.BalanceToPay = student.BalanceToPay - UserWillPay ?? 0;
                var firstPayment = UserWillPay ?? 0;

                var subject = "Balance Paid";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f9ff; padding: 20px;'>
                    <table style='width: 100%; max-width: 600px; margin: auto; background-color: #fff; border: 1px solid #d9e6f2; border-radius: 8px;'>
                        <thead style='background-color: #0056b3; color: #fff;'>
                            <tr>
                                <th style='padding: 15px; text-align: left; display: flex; align-items: center;'>
                                    <img src='https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTO9a84kDZORy-tOxHr1uSsYZM4hubrh6AThQ&s' alt='School Logo' style='height: 50px; margin-right: 15px;'>
                                    <div>
                                        <h2 style='margin: 0; font-size: 24px;'>DE ROMAN MONTESSORI SCHOOL</h2>
                                        <p style='margin: 0; font-size: 14px;'>Your gateway to excellence in education</p>
                                    </div>
                                </th>
                            </tr>
                        </thead>
                            <tbody>
                                <tr>
                                    <td style='padding: 20px;'>
                                        <p style='font-size: 16px; color: #0056b3;'>Dear <strong>{student.Firstname} {student.Surname}</strong>,</p>
                                        <p style='font-size: 14px;'>You've successfully paid <strong>{firstPayment}</strong> for your balance. Your remaining balance is <strong>{student.BalanceToPay}</strong>.</p>
                                        <p style='font-size: 14px;'>Should you have any questions or require further assistance, do not hesitate to contact us:</p>
                                    <ul style='font-size: 14px; color: #333;'>
                                        <li><strong>Email:</strong> <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></li>
                                        <li><strong>Phone:</strong> 09274044188</li>
                                    </ul>
                                    <p style='font-size: 14px;'>Thank you for choosing our services. We are here to support you every step of the way.</p>
                                </td>
                            </tr>
                        </tbody>
                        <tfoot style='background-color: #fbe052; color: #0056b3;'>
                            <tr>
                                <td style='padding: 10px; text-align: center; font-size: 12px;'>
                                    <p style='margin: 0;'>De Roman Montessori School, Tanza, Philippines</p>
                                    <p style='margin: 0;'>Contact us: 09274044188 | <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></p>
                                    <p style='margin: 0;'>&copy; {DateTime.Now.Year} De Roman Montessori School. All rights reserved.</p>
                                </td>
                            </tr>
                        </tfoot>
                        </table>
                    </div>";

                await _emailService.SendEmailAsync(student.Email, subject, body);

                var notification = new Notification
                {
                    Message = $"You've successfully paid {firstPayment} for your balance. Your remaining balance is {student.BalanceToPay}.",
                    UserId = student.EnrollmentId,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();


                DateTime date = DateTime.Now;
                string monthName = date.ToString("MMMM");

                var payment = new StudentPayment
                {
                    UserId = student.EnrollmentId,
                    ReferenceNumber = Guid.NewGuid().ToString(),
                    PaymentAmount = firstPayment,
                    MonthPaid = monthName,
                    YearPaid = DateTime.Now.Year.ToString(),
                    Status = "Paid",
                    PaymentFor = "Balance",
                    Date = DateTime.Now
                };
                _context.StudentPayments.Add(payment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Payment successful!";
                return RedirectToAction("ManageTransactions");
            }

            if (Target == "Tuition")
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentID == EnrollreesId);
                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return RedirectToAction("Index");
                }

                var payment = await _context.StudentPayments.FirstOrDefaultAsync(s => s.Id == PaymentID);
                if (payment == null)
                {
                    TempData["ErrorMessage"] = "Invalid payment ID.";
                    return RedirectToAction("Index");
                }

                await _context.SaveChangesAsync();
                var firstPayment = UserWillPay ?? 0;

                var subject = "Payment For Tuition";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f9ff; padding: 20px;'>
                    <table style='width: 100%; max-width: 600px; margin: auto; background-color: #fff; border: 1px solid #d9e6f2; border-radius: 8px;'>
                        <thead style='background-color: #0056b3; color: #fff;'>
                            <tr>
                                <th style='padding: 15px; text-align: left; display: flex; align-items: center;'>
                                    <img src='https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTO9a84kDZORy-tOxHr1uSsYZM4hubrh6AThQ&s' alt='School Logo' style='height: 50px; margin-right: 15px;'>
                                    <div>
                                        <h2 style='margin: 0; font-size: 24px;'>DE ROMAN MONTESSORI SCHOOL</h2>
                                        <p style='margin: 0; font-size: 14px;'>Your gateway to excellence in education</p>
                                    </div>
                                </th>
                            </tr>
                        </thead>
                            <tbody>
                                <tr>
                                    <td style='padding: 20px;'>
                                        <p style='font-size: 16px; color: #0056b3;'>Dear <strong>{student.Firstname} {student.Surname}</strong>,</p>
                                        <p style='font-size: 14px;'>You've successfully paid <strong>{firstPayment}</strong> for your tuition.</p>
                                        <p style='font-size: 14px;'>Should you have any questions or require further assistance, do not hesitate to contact us:</p>
                                    <ul style='font-size: 14px; color: #333;'>
                                        <li><strong>Email:</strong> <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></li>
                                        <li><strong>Phone:</strong> 09274044188</li>
                                    </ul>
                                    <p style='font-size: 14px;'>Thank you for choosing our services. We are here to support you every step of the way.</p>
                                </td>
                            </tr>
                        </tbody>
                        <tfoot style='background-color: #fbe052; color: #0056b3;'>
                            <tr>
                                <td style='padding: 10px; text-align: center; font-size: 12px;'>
                                    <p style='margin: 0;'>De Roman Montessori School, Tanza, Philippines</p>
                                    <p style='margin: 0;'>Contact us: 09274044188 | <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></p>
                                    <p style='margin: 0;'>&copy; {DateTime.Now.Year} De Roman Montessori School. All rights reserved.</p>
                                </td>
                            </tr>
                        </tfoot>
                        </table>
                    </div>";

                await _emailService.SendEmailAsync(student.Email, subject, body);

                var notification = new Notification
                {
                    Message = $"You've successfully paid {firstPayment} for your tuition.",
                    UserId = student.EnrollmentId,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();


                payment.Status = "Paid";
                payment.Date = DateTime.Now;
                payment.PaymentAmount = firstPayment;
                payment.ReferenceNumber = Guid.NewGuid().ToString();

                _context.StudentPayments.Update(payment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Payment successful!";
                return RedirectToAction("ManageTransactions");
            }
        }
        return RedirectToAction("ManageTransactions");
    }

    public IActionResult Create()
    {
        return View(new Account());
    }

    private bool IsValidEmail(string email)
    {
        return !string.IsNullOrEmpty(email) &&
            System.Text.RegularExpressions.Regex.IsMatch(email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private bool IsStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(password,
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#_])[A-Za-z\d@$!%*?&_#]{8,}$");
    }

    [HttpPost]
    public async Task<IActionResult> Create(Account account, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(account.Role))
        {
            TempData["ErrorMessage"] = "Please select a role.";
            return View(account);
        }

        if (account.Password != confirmPassword)
        {
            TempData["ErrorMessage"] = "Passwords do not match.";
            return View();
        }

        if (!IsValidEmail(account.Email))
        {
            TempData["ErrorMessage"] = "Invalid email format.";
            return View(account);
        }

        if (!IsStrongPassword(account.Password))
        {
            TempData["ErrorMessage"] = "Password must be at least 8 characters long, contain uppercase and lowercase letters, a number, and a special character.";
            return View(account);
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


            var inquiries = await _context.Inquiries.ToListAsync();
            foreach (var inquiry in inquiries)
            {
                if (inquiry.IsEnrolled)
                    continue;

                string subject = "Exciting News: Enrollment is Now Open!";
                string confirmationLink = Url.Action("CreateClick", "Enrollment", new { id = inquiry.InquiryId }, Request.Scheme) ?? "";
                string body = $@"
                    <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f9ff; padding: 20px;'>
                    <table style='width: 100%; max-width: 600px; margin: auto; background-color: #fff; border: 1px solid #d9e6f2; border-radius: 8px;'>
                        <thead style='background-color: #0056b3; color: #fff;'>
                            <tr>
                                <th style='padding: 15px; text-align: left; display: flex; align-items: center;'>
                                    <img src='https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTO9a84kDZORy-tOxHr1uSsYZM4hubrh6AThQ&s' alt='School Logo' style='height: 50px; margin-right: 15px;'>
                                    <div>
                                        <h2 style='margin: 0; font-size: 24px;'>DE ROMAN MONTESSORI SCHOOL</h2>
                                        <p style='margin: 0; font-size: 14px;'>Your gateway to excellence in education</p>
                                    </div>
                                </th>
                            </tr>
                        </thead>
                            <tbody>
                                <tr>
                                    <td style='padding: 20px;'>
                                        <p style='font-size: 16px; color: #0056b3;'>Dear <strong>{inquiry.StudentName}</strong>,</p>
                                        <p style='font-size: 14px;'>We are thrilled to announce that enrollment for the upcoming academic year is officially open! Click the link below to secure your spot:</p>
                                        <p style='text-align: center; margin: 20px 0;'>
                                            <a href='{confirmationLink}' style='display: inline-block; background-color: #0056b3; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px; font-size: 16px;'>Complete Your Enrollment</a>
                                        </p>
                                        <p style='font-size: 14px;'>Should you have any questions or require further assistance, do not hesitate to contact us:</p>
                                    <ul style='font-size: 14px; color: #333;'>
                                        <li><strong>Email:</strong> <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></li>
                                        <li><strong>Phone:</strong> 09274044188</li>
                                    </ul>
                                    <p style='font-size: 14px;'>Thank you for choosing our services. We are here to support you every step of the way.</p>
                                </td>
                            </tr>
                        </tbody>
                        <tfoot style='background-color: #fbe052; color: #0056b3;'>
                            <tr>
                                <td style='padding: 10px; text-align: center; font-size: 12px;'>
                                    <p style='margin: 0;'>De Roman Montessori School, Tanza, Philippines</p>
                                    <p style='margin: 0;'>Contact us: 09274044188 | <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></p>
                                    <p style='margin: 0;'>&copy; {DateTime.Now.Year} De Roman Montessori School. All rights reserved.</p>
                                </td>
                            </tr>
                        </tfoot>
                        </table>
                    </div>";

                await _emailService.SendEmailAsync(inquiry.EmailAddress, subject, body);

            }

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
        var existingSchedule = _context.EnrollmentSchedules.FirstOrDefault();
        if (existingSchedule != null)
        {
            _context.EnrollmentSchedules.Remove(existingSchedule);
            await _context.SaveChangesAsync();

            var inquiries = await _context.Inquiries.ToListAsync();
            foreach (var inquiry in inquiries)
            {
                if (inquiry.IsEnrolled)
                    continue;

                string subject = "Enrollment Now Officially Closed";
                string confirmationLink = Url.Action("CreateClick", "Enrollment", new { id = inquiry.InquiryId }, Request.Scheme) ?? "";
                string body = $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f9ff; padding: 20px;'>
                    <table style='width: 100%; max-width: 600px; margin: auto; background-color: #fff; border: 1px solid #d9e6f2; border-radius: 8px;'>
                        <thead style='background-color: #e74c3c; color: #fff;'>
                            <tr>
                                <th style='padding: 15px; text-align: left; display: flex; align-items: center;'>
                                    <img src='https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTO9a84kDZORy-tOxHr1uSsYZM4hubrh6AThQ&s' alt='School Logo' style='height: 50px; margin-right: 15px;'>
                                    <div>
                                        <h2 style='margin: 0; font-size: 24px;'>DE ROMAN MONTESSORI SCHOOL</h2>
                                        <p style='margin: 0; font-size: 14px;'>Your gateway to excellence in education</p>
                                    </div>
                                </th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td style='padding: 20px;'>
                                    <p style='font-size: 16px; color: #e74c3c;'>Dear <strong>{inquiry.StudentName}</strong>,</p>
                                    <p style='font-size: 14px;'>We regret to inform you that enrollment for the upcoming academic year is now officially closed. Unfortunately, we are unable to accept new enrollments at this time. We thank you for your interest in De Roman Montessori School.</p>
                                    <p style='font-size: 14px;'>If you have any questions or need further assistance, please feel free to reach out to us:</p>
                                    <ul style='font-size: 14px; color: #333;'>
                                        <li><strong>Email:</strong> <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #e74c3c;'>depedcavite.deromanmontessori@gmail.com</a></li>
                                        <li><strong>Phone:</strong> 09274044188</li>
                                    </ul>
                                    <p style='font-size: 14px;'>Thank you again for considering our school. We wish you all the best in your educational journey.</p>
                                </td>
                            </tr>
                        </tbody>
                        <tfoot style='background-color: #fbe052; color: #e74c3c;'>
                            <tr>
                                <td style='padding: 10px; text-align: center; font-size: 12px;'>
                                    <p style='margin: 0;'>De Roman Montessori School, Tanza, Philippines</p>
                                    <p style='margin: 0;'>Contact us: 09274044188 | <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #e74c3c;'>depedcavite.deromanmontessori@gmail.com</a></p>
                                    <p style='margin: 0;'>&copy; {DateTime.Now.Year} De Roman Montessori School. All rights reserved.</p>
                                </td>
                            </tr>
                        </tfoot>
                    </table>
                </div>";

                await _emailService.SendEmailAsync(inquiry.EmailAddress, subject, body);
            }
        }
        return RedirectToAction("Index", "Admin");
    }


    string GenerateSecurePassword()
    {
        const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
        const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string numberChars = "0123456789";
        const string specialChars = "!@#$%^&*()_+[]{}|;:,.<>?";
        const string allChars = lowerChars + upperChars + numberChars + specialChars;

        var random = new Random();

        char lower = lowerChars[random.Next(lowerChars.Length)];
        char upper = upperChars[random.Next(upperChars.Length)];
        char number = numberChars[random.Next(numberChars.Length)];
        char special = specialChars[random.Next(specialChars.Length)];

        int length = random.Next(8, 13);
        var remainingChars = Enumerable.Range(0, length - 4)
            .Select(_ => allChars[random.Next(allChars.Length)]);

        var passwordChars = new[] { lower, upper, number, special }.Concat(remainingChars)
            .OrderBy(_ => random.Next())
            .ToArray();

        return new string(passwordChars);
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

            enrollment.Username = $"temp{enrollment.Firstname}{enrollment.StudentID}{enrollment.Surname}";
            enrollment.Password = GenerateSecurePassword();

            var payment = new StudentPaymentRecord
            {
                UserId = enrollment.EnrollmentId,
                PaymentType = "",
                SiblingDiscount = enrollment.NumberOfSibling
            };
            _context.StudentPaymentRecords.Add(payment);
            await _context.SaveChangesAsync();

            if (enrollment.IsEnrolled) {
                var subject2 = "Enrollment Payment Success";
                var body2 = $@"
                    <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f9ff; padding: 20px;'>
                    <table style='width: 100%; max-width: 600px; margin: auto; background-color: #fff; border: 1px solid #d9e6f2; border-radius: 8px;'>
                        <thead style='background-color: #0056b3; color: #fff;'>
                            <tr>
                                <th style='padding: 15px; text-align: left; display: flex; align-items: center;'>
                                    <img src='https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTO9a84kDZORy-tOxHr1uSsYZM4hubrh6AThQ&s' alt='School Logo' style='height: 50px; margin-right: 15px;'>
                                    <div>
                                        <h2 style='margin: 0; font-size: 24px;'>DE ROMAN MONTESSORI SCHOOL</h2>
                                        <p style='margin: 0; font-size: 14px;'>Your gateway to excellence in education</p>
                                    </div>
                                </th>
                            </tr>
                        </thead>
                            <tbody>
                                <tr>
                                    <td style='padding: 20px;'>
                                        <p style='font-size: 16px; color: #0056b3;'>Dear {enrollment.Firstname} {enrollment.Surname},</p>
                                        <p style='font-size: 14px;'>Congratulations! Your enrollment has been successfully approved.</p>
                                    <p style='font-size: 14px;'>To complete your enrollment, kindly proceed with the payment by clicking the button below:</p>
                                        <p style='font-size: 14px;'>Here are your permanent account details:</p>
                                        <p><strong>Username:</strong> {enrollment.Username}</p>
                                        <p><strong>Password:</strong> {enrollment.Password}</p>
                                        <p style='font-size: 14px;'>Should you have any questions or require further assistance, do not hesitate to contact us:</p>
                                    <ul style='font-size: 14px; color: #333;'>
                                        <li><strong>Email:</strong> <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></li>
                                        <li><strong>Phone:</strong> 09274044188</li>
                                    </ul>
                                    <p style='font-size: 14px;'>Thank you for choosing our services. We are here to support you every step of the way.</p>
                                </td>
                            </tr>
                        </tbody>
                        <tfoot style='background-color: #fbe052; color: #0056b3;'>
                            <tr>
                                <td style='padding: 10px; text-align: center; font-size: 12px;'>
                                    <p style='margin: 0;'>De Roman Montessori School, Tanza, Philippines</p>
                                    <p style='margin: 0;'>Contact us: 09274044188 | <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></p>
                                    <p style='margin: 0;'>&copy; {DateTime.Now.Year} De Roman Montessori School. All rights reserved.</p>
                                </td>
                            </tr>
                        </tfoot>
                        </table>
                    </div>";
                await _emailService.SendEmailAsync(enrollment.Email, subject2, body2);

            }
            var paymentLink = $"{Request.Scheme}://{Request.Host}/Home/ApprovedEnrolled?id={enrollment.ApproveId}";
            var subject = "Congratulations! Your Enrollment Has Been Approved";
            var body = $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f9ff; padding: 20px;'>
                    <table style='width: 100%; max-width: 600px; margin: auto; background-color: #fff; border: 1px solid #d9e6f2; border-radius: 8px;'>
                        <thead style='background-color: #0056b3; color: #fff;'>
                            <tr>
                                <th style='padding: 15px; text-align: left; display: flex; align-items: center;'>
                                    <img src='https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTO9a84kDZORy-tOxHr1uSsYZM4hubrh6AThQ&s' alt='School Logo' style='height: 50px; margin-right: 15px;'>
                                    <div>
                                        <h2 style='margin: 0; font-size: 24px;'>DE ROMAN MONTESSORI SCHOOL</h2>
                                        <p style='margin: 0; font-size: 14px;'>Your gateway to excellence in education</p>
                                    </div>
                                </th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td style='padding: 20px;'>
                                    <p style='font-size: 16px; color: #0056b3;'>Dear <strong>{enrollment.Firstname} {enrollment.Surname}</strong>,</p>
                                    <p style='font-size: 14px;'>Congratulations! Your enrollment has been successfully approved.</p>

                                    <p style='font-size: 14px;'>Here are your temporary account details:</p>
                                        <p><strong>Temporary Username:</strong> {enrollment.TemporaryUsername}</p>
                                        <p><strong>Temporary Password:</strong> {enrollment.TemporaryPassword}</p>
                                    <p style='font-size: 14px;'>To get the permanent account details. You need to complete your enrollment first.</p>
                                    <p style='font-size: 14px;'>To complete your enrollment, kindly proceed with the payment by clicking the button below:</p>
                                    <p>
                                        <a href='{paymentLink}' style='color: #ffffff; background-color: #007bff; padding: 10px 15px; text-decoration: none; border-radius: 5px;'>Proceed To Payment</a>
                                    </p>
                                    <p style='font-size: 14px;'>Alternatively, you may visit us in person to make the payment.</p>
                                    <p style='font-size: 14px;'>Thank you for choosing our institution. We are excited to have you join us!</p>
                                    <p style='font-size: 14px;'>If you need further assistance, please don't hesitate to reach out to us:</p>
                                    <ul style='font-size: 14px; color: #333;'>
                                        <li><strong>Email:</strong> <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></li>
                                        <li><strong>Phone:</strong> 09274044188</li>
                                    </ul>
                                    <p style='font-size: 14px;'>Thank you for choosing our services. We are here to support you every step of the way.</p>
                                </td>
                            </tr>
                        </tbody>
                        <tfoot style='background-color: #fbe052; color: #0056b3;'>
                            <tr>
                                <td style='padding: 10px; text-align: center; font-size: 12px;'>
                                    <p style='margin: 0;'>De Roman Montessori School, Tanza, Philippines</p>
                                    <p style='margin: 0;'>Contact us: 09274044188 | <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></p>
                                    <p style='margin: 0;'>&copy; {DateTime.Now.Year} De Roman Montessori School. All rights reserved.</p>
                                </td>
                            </tr>
                        </tfoot>
                    </table>
                </div>";
            await _emailService.SendEmailAsync(enrollment.Email, subject, body);

            var notification = new Notification
            {
                Message = "Your enrollment has been successfully approved.",
                UserId = enrollment.EnrollmentId,
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var recent = new RecentActivity
            {
                Activity = $"Enrollment approved for {enrollment.Firstname} {enrollment.Surname}",
                CreatedAt = DateTime.Now
            };
            _context.RecentActivities.Add(recent);
            await _context.SaveChangesAsync();


            return RedirectToAction("ManageEnrollees", "Admin");
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

            var subject = "Enrollment Status: Rejected";
            var body = $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f9ff; padding: 20px;'>
                    <table style='width: 100%; max-width: 600px; margin: auto; background-color: #fff; border: 1px solid #d9e6f2; border-radius: 8px;'>
                        <thead style='background-color: #0056b3; color: #fff;'>
                            <tr>
                                <th style='padding: 15px; text-align: left; display: flex; align-items: center;'>
                                    <img src='https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTO9a84kDZORy-tOxHr1uSsYZM4hubrh6AThQ&s' alt='School Logo' style='height: 50px; margin-right: 15px;'>
                                    <div>
                                        <h2 style='margin: 0; font-size: 24px;'>DE ROMAN MONTESSORI SCHOOL</h2>
                                        <p style='margin: 0; font-size: 14px;'>Your gateway to excellence in education</p>
                                    </div>
                                </th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td style='padding: 20px;'>
                                    <p style='font-size: 16px; color: #b30000;'>Dear <strong>{enrollment.Firstname} {enrollment.Surname}</strong>,</p>
                                    <p style='font-size: 14px;'>We regret to inform you that your enrollment has been rejected.</p>
                                    <p style='font-size: 14px;'>If you need further assistance, please don't hesitate to reach out to us:</p>
                                    <ul style='font-size: 14px; color: #333;'>
                                        <li><strong>Email:</strong> <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></li>
                                        <li><strong>Phone:</strong> 09274044188</li>
                                    </ul>
                                    <p style='font-size: 14px;'>Thank you for choosing our services. We are here to support you every step of the way.</p>
                                </td>
                            </tr>
                        </tbody>
                        <tfoot style='background-color: #fbe052; color: #0056b3;'>
                            <tr>
                                <td style='padding: 10px; text-align: center; font-size: 12px;'>
                                    <p style='margin: 0;'>De Roman Montessori School, Tanza, Philippines</p>
                                    <p style='margin: 0;'>Contact us: 09274044188 | <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></p>
                                    <p style='margin: 0;'>&copy; {DateTime.Now.Year} De Roman Montessori School. All rights reserved.</p>
                                </td>
                            </tr>
                        </tfoot>
                    </table>
                </div>";
            await _emailService.SendEmailAsync(enrollment.Email, subject, body);

            var notification = new Notification
            {
                Message = "Your enrollment has been rejected. Please contact the administration for further details.",
                UserId = enrollment.EnrollmentId,
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var recent = new RecentActivity
            {
                Activity = $"Enrollment rejected for {enrollment.Firstname} {enrollment.Surname}",
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

        var subject = "Enrollment Status: Deleted";
        var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f9ff; padding: 20px;'>
                    <table style='width: 100%; max-width: 600px; margin: auto; background-color: #fff; border: 1px solid #d9e6f2; border-radius: 8px;'>
                        <thead style='background-color: #0056b3; color: #fff;'>
                            <tr>
                                <th style='padding: 15px; text-align: left; display: flex; align-items: center;'>
                                    <img src='https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTO9a84kDZORy-tOxHr1uSsYZM4hubrh6AThQ&s' alt='School Logo' style='height: 50px; margin-right: 15px;'>
                                    <div>
                                        <h2 style='margin: 0; font-size: 24px;'>DE ROMAN MONTESSORI SCHOOL</h2>
                                        <p style='margin: 0; font-size: 14px;'>Your gateway to excellence in education</p>
                                    </div>
                                </th>
                            </tr>
                        </thead>
                    <tbody>
                        <tr>
                            <td style='padding: 20px;'>
                                <p style='font-size: 16px; color: #b30000;'>Dear <strong>{enrollment.Firstname} {enrollment.Surname}</strong>,</p>
                                <p style='font-size: 14px;'>We regret to inform you that your enrollment has been deleted.</p>
                                <p style='font-size: 14px;'>For further details or clarification, please contact our administration team:</p>
                                <ul style='font-size: 14px; color: #333;'>
                                    <li><strong>Email:</strong> <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #b30000;'>depedcavite.deromanmontessori@gmail.com</a></li>
                                    <li><strong>Phone:</strong> 09274044188</li>
                                </ul>
                                <p style='font-size: 14px;'>We apologize for any inconvenience caused and are here to assist you with any concerns.</p>
                                <p style='font-size: 14px;'>Best regards,</p>
                                <p style='font-size: 14px;'><strong>Your Enrollment Team</strong></p>
                            </td>
                        </tr>
                    </tbody>
                    <tfoot style='background-color: #fbe052; color: #b30000;'>
                        <tr>
                            <td style='padding: 10px; text-align: center; font-size: 12px;'>
                                <p style='margin: 0;'>De Roman Montessori School, Tanza, Philippines</p>
                                <p style='margin: 0;'>Contact us: 09274044188 | <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #b30000;'>depedcavite.deromanmontessori@gmail.com</a></p>
                                <p style='margin: 0;'>&copy; {DateTime.Now.Year} De Roman Montessori School. All rights reserved.</p>
                            </td>
                        </tr>
                    </tfoot>
                </table>
            </div>";
        await _emailService.SendEmailAsync(enrollment.Email, subject, body);

        enrollment.IsDeleted = true;
        enrollment.Email = string.Empty;
        _context.Update(enrollment);
        await _context.SaveChangesAsync();


        TempData["SuccessMessage"] = "Enrollment deleted successfully.";
        return RedirectToAction("ManageEnrollees", "Admin");
    }

    [HttpPost]
    public async Task<IActionResult> Enroll(Enrollment enrollment)
    {
        if (await _context.Students.FirstOrDefaultAsync(s => s.LRN == enrollment.LRN) != null
            || await _context.Students.FirstOrDefaultAsync(s => s.Email == enrollment.Email) != null)
        {
            TempData["ErrorMessage"] = "Enrollment already exists.";
            return RedirectToAction("ManageEnrolled", "Admin");
        }
        if (await _context.Accounts.FirstOrDefaultAsync(s => s.Email == enrollment.Email) != null)
        {
            TempData["ErrorMessage"] = "Email already exists.";
            return View(enrollment);
        }

        string schoolYear = DateTime.Now.Year.ToString();
        string studentNumber = enrollment.EnrollmentId.ToString("D4");
        if (enrollment.LRN == null)
        {
            if (enrollment.GradeLevel != "NURSERY" && enrollment.GradeLevel == "KINDER")
            {
                TempData["ErrorMessage"] = "LRN is required.";
                return View(enrollment);
            }
            string schoolId = "001994";
            enrollment.LRN = $"{schoolId}{schoolYear}{studentNumber}";
        }

        var enrolledNo = await _context.Students.CountAsync();
        enrollment.StudentID = $"{schoolYear}-{enrolledNo}";

        var approvedId = Guid.NewGuid().ToString();
        enrollment.ApproveId = approvedId;
        enrollment.IsApproved = true;
        enrollment.ApprovedEnrolled = DateTime.Now;

        enrollment.Username = $"temp{enrollment.Firstname}{enrollment.StudentID}{enrollment.Surname}";
        enrollment.Password = GenerateSecurePassword();


        enrollment.SubmissionDate = DateTime.Now;
        enrollment.IsWalkin = true;

        // enrollment.SetTemporaryCredentials(studentNumber);
        enrollment.ApprovedEnrolled = DateTime.Now;
        var paymentLink = $"{Request.Scheme}://{Request.Host}/Home/ApprovedEnrolled?id={approvedId}";
        var subject = "Your Enrollment Has Been Approved!";
        var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f9ff; padding: 20px;'>
                    <table style='width: 100%; max-width: 600px; margin: auto; background-color: #fff; border: 1px solid #d9e6f2; border-radius: 8px;'>
                        <thead style='background-color: #0056b3; color: #fff;'>
                            <tr>
                                <th style='padding: 15px; text-align: left; display: flex; align-items: center;'>
                                    <img src='https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTO9a84kDZORy-tOxHr1uSsYZM4hubrh6AThQ&s' alt='School Logo' style='height: 50px; margin-right: 15px;'>
                                    <div>
                                        <h2 style='margin: 0; font-size: 24px;'>DE ROMAN MONTESSORI SCHOOL</h2>
                                        <p style='margin: 0; font-size: 14px;'>Your gateway to excellence in education</p>
                                    </div>
                                </th>
                            </tr>
                        </thead>
                    <tbody>
                        <tr>
                            <td style='padding: 20px;'>
                                <p style='font-size: 16px;'>Dear <strong>{enrollment.Firstname} {enrollment.Surname}</strong>,</p>
                                <p style='font-size: 14px;'>Congratulations! Your enrollment has been approved.</p>
                                <p style='font-size: 14px;'>To complete your enrollment, please make your payment by clicking the link below:</p>
                                <p style='font-size: 14px;'>Your Temporary Username: <strong>{enrollment.TemporaryUsername}</strong></p>
                                <p style='font-size: 14px;'>Your Temporary Password: <strong>{enrollment.TemporaryPassword}</strong></p>
                                <p style='text-align: center; margin: 20px 0;'>
                                    <a href='{paymentLink}' style='color: #ffffff; background-color: #007bff; padding: 10px 15px; text-decoration: none; border-radius: 5px;'>Complete Payment</a>
                                </p>
                                <p style='font-size: 14px;'>If you prefer, you may also visit us in person to complete your payment.</p>
                                <p style='font-size: 14px;'>Thank you for choosing De Roman Montessori School. We are excited to have you join our community!</p>
                                <p style='font-size: 14px;'>Best regards,</p>
                                <p style='font-size: 14px;'><strong>Your Enrollment Team</strong></p>
                            </td>
                        </tr>
                    </tbody>
                    <tfoot style='background-color: #fbe052; color: #0056b3;'>
                        <tr>
                            <td style='padding: 10px; text-align: center; font-size: 12px;'>
                                <p style='margin: 0;'>De Roman Montessori School, Tanza, Philippines</p>
                                <p style='margin: 0;'>Contact us: 09274044188 | <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></p>
                                <p style='margin: 0;'>&copy; {DateTime.Now.Year} De Roman Montessori School. All rights reserved.</p>
                            </td>
                        </tr>
                    </tfoot>
                </table>
            </div>";
        await _emailService.SendEmailAsync(enrollment.Email, subject, body);

        var notification = new Notification
        {
            Message = $"Student {enrollment.Firstname} {enrollment.Surname} has been successfully approved.",
            UserId = enrollment.EnrollmentId,
            CreatedAt = DateTime.Now,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        _context.Students.Add(enrollment);
        await _context.SaveChangesAsync();

        var payment = new StudentPaymentRecord
        {
            UserId = enrollment.EnrollmentId,
            PaymentType = "",
            SiblingDiscount = 0,
        };
        _context.StudentPaymentRecords.Add(payment);
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


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendInquire(int id, string InquireContent)
    {
        var inquiry = await _context.Inquiries.FindAsync(id);
        if (inquiry == null)
        {
            TempData["ErrorMessage"] = "Inquiry not found.";
            return RedirectToAction(nameof(Index));
        }
        inquiry.IsInquired = true;
        _context.Update(inquiry);
        await _context.SaveChangesAsync();


        string emailSubject = "Inquiry Confirmation - De Roman Montessori School";
        string emailBody = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f9ff; padding: 20px;'>
                    <table style='width: 100%; max-width: 600px; margin: auto; background-color: #fff; border: 1px solid #d9e6f2; border-radius: 8px;'>
                        <thead style='background-color: #0056b3; color: #fff;'>
                            <tr>
                                <th style='padding: 15px; text-align: left; display: flex; align-items: center;'>
                                    <img src='https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTO9a84kDZORy-tOxHr1uSsYZM4hubrh6AThQ&s' alt='School Logo' style='height: 50px; margin-right: 15px;'>
                                    <div>
                                        <h2 style='margin: 0; font-size: 24px;'>DE ROMAN MONTESSORI SCHOOL</h2>
                                        <p style='margin: 0; font-size: 14px;'>Your gateway to excellence in education</p>
                                    </div>
                                </th>
                            </tr>
                        </thead>
                    <tbody>
                        <tr>
                            <td style='padding: 20px;'>
                                <p style='font-size: 16px; color: #0056b3;'>Dear <strong>{inquiry.StudentName}</strong>,</p>
                                <p style='font-size: 14px;'>{InquireContent}</p>
                                <p style='font-size: 14px;'>For further assistance, feel free to reach out to us using the contact details below:</p>
                                <ul style='font-size: 14px; color: #333;'>
                                    <li><strong>Phone:</strong> +123-456-7890</li>
                                    <li><strong>Email:</strong> <a href='mailto:contact@dromanmontessori.edu' style='color: #0056b3;'>contact@dromanmontessori.edu</a></li>
                                    <li><strong>Website:</strong> <a href='https://www.dromanmontessori.edu' style='color: #0056b3;'>www.dromanmontessori.edu</a></li>
                                </ul>
                                <p style='font-size: 14px;'>Thank you for your interest in De Roman Montessori School. We look forward to assisting you with any questions or concerns!</p>
                            </td>
                        </tr>
                    </tbody>
                    <tfoot style='background-color: #fbe052; color: #0056b3;'>
                        <tr>
                            <td style='padding: 10px; text-align: center; font-size: 12px;'>
                                <p style='margin: 0;'>De Roman Montessori School, 123 Academic Street, Education City</p>
                                <p style='margin: 0;'>Contact us: +123-456-7890 | <a href='mailto:contact@dromanmontessori.edu' style='color: #0056b3;'>contact@dromanmontessori.edu</a></p>
                                <p style='margin: 0;'>&copy; {DateTime.Now.Year} De Roman Montessori School. All rights reserved.</p>
                            </td>
                        </tr>
                    </tfoot>
                </table>
            </div>";

        await _emailService.SendEmailAsync(inquiry.EmailAddress, emailSubject, emailBody);
        TempData["SuccessMessage"] = "Inquiry sent successfully.";

        return RedirectToAction("ManageInquiries", "Admin");
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectInquiry(int id)
    {
        var inquiry = await _context.Inquiries.FindAsync(id);

        if (inquiry == null)
        {
            TempData["ErrorMessage"] = "Inquiry not found.";
            return RedirectToAction("Index");
        }

        inquiry.IsRejected = true;
        _context.Update(inquiry);
        await _context.SaveChangesAsync();

        string subject = "Inquiry Rejected";
        string body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f9ff; padding: 20px;'>
                    <table style='width: 100%; max-width: 600px; margin: auto; background-color: #fff; border: 1px solid #d9e6f2; border-radius: 8px;'>
                        <thead style='background-color: #0056b3; color: #fff;'>
                            <tr>
                                <th style='padding: 15px; text-align: left; display: flex; align-items: center;'>
                                    <img src='https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTO9a84kDZORy-tOxHr1uSsYZM4hubrh6AThQ&s' alt='School Logo' style='height: 50px; margin-right: 15px;'>
                                    <div>
                                        <h2 style='margin: 0; font-size: 24px;'>DE ROMAN MONTESSORI SCHOOL</h2>
                                        <p style='margin: 0; font-size: 14px;'>Your gateway to excellence in education</p>
                                    </div>
                                </th>
                            </tr>
                        </thead>
                    <tbody>
                        <tr>
                            <td style='padding: 20px;'>
                                <p style='font-size: 16px; color: #0056b3;'>Dear <strong>{inquiry.StudentName}</strong>,</p>
                                <p style='font-size: 14px;'>We regret to inform you that your inquiry has been rejected.</p>
                                <p style='font-size: 14px;'>If you have any questions or would like to discuss this further, please do not hesitate to contact us.</p>
                                <p style='font-size: 14px;'>For any further communication, here is our contact information:</p>
                                <ul style='font-size: 14px; color: #333;'>
                                    <li><strong>Email:</strong> <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></li>
                                    <li><strong>Phone:</strong> 09274044188</li>
                                </ul>
                                <p style='font-size: 14px;'>Thank you for choosing our services. We are here to support you every step of the way.</p>
                            </td>
                        </tr>
                    </tbody>
                    <tfoot style='background-color: #fbe052; color: #0056b3;'>
                        <tr>
                            <td style='padding: 10px; text-align: center; font-size: 12px;'>
                                <p style='margin: 0;'>De Roman Montessori School, Tanza, Philippines</p>
                                <p style='margin: 0;'>Contact us: 09274044188 | <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></p>
                                <p style='margin: 0;'>&copy; {DateTime.Now.Year} De Roman Montessori School. All rights reserved.</p>
                            </td>
                        </tr>
                    </tfoot>
                </table>
            </div>";

        await _emailService.SendEmailAsync(inquiry.EmailAddress, subject, body);
        var recent = new RecentActivity
        {
            Activity = $"Inquiry from {inquiry.StudentName} has been rejected.",
            CreatedAt = DateTime.Now
        };
        _context.RecentActivities.Add(recent);
        await _context.SaveChangesAsync();


        await _emailService.SendEmailAsync(inquiry.EmailAddress, subject, body);
        TempData["SuccessMessage"] = "Inquiry rejected.";
        return RedirectToAction("ManageInquiries");
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
        return RedirectToAction("ManageInquiries");
    }

    [HttpPost]
    public async Task<IActionResult> EditAccount(int AccountId, string Username, string Email, string Role)
    {
        if (string.IsNullOrWhiteSpace(Username) || Username.Length < 5)
        {
            TempData["ErrorMessage"] = "Username must be at least 5 characters long.";
            return RedirectToAction("ManageAccounts");
        }

        if (!IsValidEmail(Email))
        {
            TempData["ErrorMessage"] = "Invalid email format.";
            return RedirectToAction("ManageAccounts");
        }

        if (string.IsNullOrWhiteSpace(Role))
        {
            TempData["ErrorMessage"] = "Role is required.";
            return RedirectToAction("ManageAccounts");
        }

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
