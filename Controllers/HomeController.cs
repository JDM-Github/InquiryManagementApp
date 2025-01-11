using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using InquiryManagementApp.Models;
using System.Text.Json;
using InquiryManagementApp.Service;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using Helper;

namespace InquiryManagementApp.Controllers;


public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly FileUploadService _fileUploadService;
    private readonly FileDownloadService _fileDownloadService;
    private readonly EmailService _emailService;

    public HomeController(ApplicationDbContext context, FileUploadService fileUploadService, FileDownloadService fileDownloadService, EmailService emailService)
    {
        _context = context;
        _fileUploadService = fileUploadService;
        _fileDownloadService = fileDownloadService;
        _emailService = emailService;
    }

    public async Task<IActionResult> Assessment()
    {
        var userId = HttpContext.Session.GetString("LRN");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var enrollment = await _context.Students.FirstOrDefaultAsync(s => s.LRN == userId);
        var payment = await _context.StudentPaymentRecords.FirstOrDefaultAsync(s => s.UserId == enrollment!.EnrollmentId);
        return View(payment);
    }

    [HttpGet]
    public async Task<IActionResult> ApprovedEnrolled(string id)
    {
        var enrollees = await _context.Students.FirstOrDefaultAsync(e => e.ApproveId == id);
        if (enrollees!.IsEnrolled)
        {
            TempData["ErrorMessage"] = "Student already enrolled.";
            return RedirectToAction("Index");
        }

        var paymentView = new StudentPaymentView
        {
            ApprovedId = id,
            PaymentType = "",
        };
        return View(paymentView);
    }

    // public async Task<IActionResult> PayEnrollment(StudentPaymentView studentPayment)
    // {
    //     var enrollees = await _context.Students.FirstOrDefaultAsync(e => e.ApproveId == studentPayment.ApprovedId);
    //     if (enrollees == null)
    //     {
    //         TempData["ErrorMessage"] = "Student not found.";
    //         return RedirectToAction("Index");
    //     }


    //     var studentPay = await _context.StudentPaymentRecords.FirstOrDefaultAsync(s => s.UserId == enrollees.EnrollmentId);
    //     if (studentPay == null)
    //     {
    //         TempData["ErrorMessage"] = "Payment record not found.";
    //         return RedirectToAction("Index");
    //     }

    //     studentPay.PaymentType = studentPayment.PaymentType;
    //     var IsEnrollmentActive = false;
    //     var schedule = _context.EnrollmentSchedules.FirstOrDefault();
    //     if (schedule != null)
    //     {
    //         IsEnrollmentActive = DateTime.Now >= schedule.StartDate && DateTime.Now <= schedule.EndDate;
    //     }

    //     studentPay.EarlyBird = IsEnrollmentActive;
    //     studentPay.SiblingDiscount = Math.Min(5, enrollees.NumberOfSibling);
    //     if (studentPay.PaymentType == "Monthly")
    //     {
    //         studentPay.PerPayment = 1900;
    //     }
    //     else if (studentPay.PaymentType == "Quarterly")
    //     {
    //         studentPay.PerPayment = 4750;
    //     }

    //     var allDiscount = studentPay.SiblingDiscount + (studentPay.EarlyBird ? 1 : 0) + (studentPay.CashDiscount ? 1 : 0);
    //     var firstPayment = 14000 + (studentPay.PerPayment ?? 0) - (allDiscount * 1900);

    //     // First payment link, like paypal etc
    //     var payment = new StudentPayment {
    //         UserId = enrollees.EnrollmentId,
    //         ReferenceNumber = Guid.NewGuid().ToString(),
    //         PaymentAmount = firstPayment,
    //         MonthPaid = "First",
    //         Date = DateTime.Now
    //     };

    //     // PAYMENT SUCCESS
    //     // ----------------------------------------------
    //     enrollees.IsEnrolled = true;
    //     enrollees.EnrolledDate = DateTime.Now;
    //     _context.Update(enrollees);
    //     await _context.SaveChangesAsync();

    //     var subject = "Enrollment Approved";
    //     var body = $"Dear {enrollees.Firstname} {enrollees.Surname},<br>You've successfully enrolled in this school.";
    //     await _emailService.SendEmailAsync(enrollees.Email, subject, body);

    //     var notification = new Notification
    //     {
    //         Message = $"You've successfully been enrolled.",
    //         UserId = enrollees.LRN,
    //         CreatedAt = DateTime.Now,
    //         IsRead = false
    //     };
    //     _context.Notifications.Add(notification);
    //     await _context.SaveChangesAsync();

    //     // --------------------------------------------------
    //     TempData["SuccessMessage"] = "Payment successful. Congratualation you successfully enrolled in this school.";



    //     return RedirectToAction("Index");
    // }

    public async Task<IActionResult> PayEnrollment(StudentPaymentView studentPayment)
    {
        var enrollees = await _context.Students.FirstOrDefaultAsync(e => e.ApproveId == studentPayment.ApprovedId);
        if (enrollees == null)
        {
            TempData["ErrorMessage"] = "Student not found.";
            return RedirectToAction("Index");
        }

        var studentPay = await _context.StudentPaymentRecords.FirstOrDefaultAsync(s => s.UserId == enrollees.EnrollmentId);
        if (studentPay == null)
        {
            TempData["ErrorMessage"] = "Payment record not found.";
            return RedirectToAction("Index");
        }

        studentPay.PaymentType = studentPayment.PaymentType;
        var IsEnrollmentActive = false;
        var schedule = _context.EnrollmentSchedules.FirstOrDefault();
        if (schedule != null)
        {
            IsEnrollmentActive = DateTime.Now >= schedule.StartDate && DateTime.Now <= schedule.EndDate;
        }

        studentPay.EarlyBird = IsEnrollmentActive;
        studentPay.SiblingDiscount = Math.Min(5, enrollees.NumberOfSibling);
        if (studentPay.PaymentType == "Monthly")
        {
            studentPay.PerPayment = 1900;
        }
        else if (studentPay.PaymentType == "Quarterly")
        {
            studentPay.PerPayment = 4750;
        }
        else if (studentPay.PaymentType == "Initial5")
        {
            studentPay.PerPayment = 2800;
        }
        _context.Update(studentPay);
        await _context.SaveChangesAsync();

        var allDiscount = studentPay.SiblingDiscount + (studentPay.EarlyBird ? 1 : 0) + (studentPay.CashDiscount ? 1 : 0);
        var firstPayment = 14000 + (studentPay.PerPayment ?? 0) - (allDiscount * 1900);

        if (studentPay.PaymentType == "Initial5")
        {
            firstPayment = 5000;
        }

        var orderRequest = new OrdersCreateRequest();
        orderRequest.Prefer("return=representation");
        orderRequest.RequestBody(new OrderRequest
        {
            CheckoutPaymentIntent = "CAPTURE",
            ApplicationContext = new ApplicationContext
            {
                ReturnUrl = Url.Action("PaymentSuccess", "Home", new { enrollmentId = enrollees.EnrollmentId }, Request.Scheme),
                CancelUrl = Url.Action("PaymentFailed", "Home", new { enrollmentId = enrollees.EnrollmentId }, Request.Scheme)
            },
            PurchaseUnits = new List<PurchaseUnitRequest>
            {
                new PurchaseUnitRequest
                {
                    AmountWithBreakdown = new AmountWithBreakdown
                    {
                        CurrencyCode = "PHP",
                        Value = firstPayment.ToString("F2")
                    },
                    Description = "Enrollment Fee"
                }
            }
        });
        var client = new PayPalHttpClient(PayPalConfig.GetEnvironment());
        var response = await client.Execute(orderRequest);
        if (response.StatusCode == System.Net.HttpStatusCode.Created)
        {
            var result = response.Result<Order>();
            HttpContext.Session.SetString("PayPalOrderId", result.Id ?? "");
            var approveUrl = result.Links.FirstOrDefault(link => link.Rel == "approve")?.Href;

            if (!string.IsNullOrEmpty(approveUrl))
            {
                return Redirect(approveUrl);
            }
        }

        TempData["ErrorMessage"] = "Failed to create payment. Please try again.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> PaymentSuccess(int enrollmentId, double amount)
    {
        var enrollees = await _context.Students.FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId);
        if (enrollees == null)
        {
            TempData["ErrorMessage"] = "Enrollment not found.";
            return RedirectToAction("Index");
        }

        try
        {
            var client = new PayPalHttpClient(PayPalConfig.GetEnvironment());

            var captureRequest = new OrdersCaptureRequest(HttpContext.Session.GetString("PayPalOrderId") ?? "");
            captureRequest.RequestBody(new OrderActionRequest());

            var response = await client.Execute(captureRequest);
            var result = response.Result<Order>();

            if (result.Status == "COMPLETED")
            {
                enrollees.IsEnrolled = true;
                enrollees.EnrolledDate = DateTime.Now;
                _context.Update(enrollees);
                await _context.SaveChangesAsync();

                var subject = "Enrollment Approved";
                var body = $"Dear {enrollees.Firstname} {enrollees.Surname},<br>You've successfully enrolled in this school.<p>Your Username: {enrollees.Username}</p><p> Your Password: {enrollees.Username}</p>";
                await _emailService.SendEmailAsync(enrollees.Email, subject, body);

                var notification = new Notification
                {
                    Message = $"You've successfully been enrolled.",
                    UserId = enrollees!?.LRN,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                DateTime date = DateTime.Now;
                string monthName = date.ToString("MMMM");

                var Payment = new StudentPayment
                {
                    UserId = enrollees.EnrollmentId,
                    ReferenceNumber = "-----",
                    PaymentAmount = amount,
                    MonthPaid = monthName,
                    YearPaid = DateTime.Now.Year.ToString(),
                    Date = null
                };
                _context.StudentPayments.Add(Payment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Payment successful. Congratulations, you are now enrolled!";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["ErrorMessage"] = "Payment was not completed.";
                return RedirectToAction("CartFinished");
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error capturing payment: {ex.Message}";
            return RedirectToAction("CartFinished");
        }

        
    }

    // Payment failed handler
    public IActionResult PaymentFailed(int enrollmentId)
    {
        TempData["ErrorMessage"] = "Payment was canceled or failed. Please try again.";
        return RedirectToAction("Index");
    }


    public IActionResult Index()
    {
        var existingSchedule = _context.PaymentSchedules.FirstOrDefault();
        if (existingSchedule != null)
        {
            PaymentSchedule.CreateInstance(existingSchedule);
        }

        var userId = HttpContext.Session.GetString("LRN");
        if (HttpContext.Session.GetString("isAdmin") == "1")
        {
            return RedirectToAction("Index", "Admin");
        }
        else if (HttpContext.Session.GetString("isAdmin") == "2")
        {
            return RedirectToAction("Account", "Home");
        }
        return View();
    }

    public async Task<IActionResult> Document()
    {
        var userId = HttpContext.Session.GetString("LRN");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var enrollment = _context.Students
            .Where(e => e.LRN == userId)
            .FirstOrDefault();

        if (enrollment == null)
        {
            return NotFound();
        }

        var requiredFiles = await _context.RequirementModels
            .Where(c => c.EnrollmentId == enrollment.EnrollmentId)
            .ToListAsync();

        var model = new EnrollmentRequirementsViewModel
        {
            Enrollment = enrollment,
            Requirements = requiredFiles
        };
        return View(model);
    }

    public IActionResult Payment()
    {
        var userId = HttpContext.Session.GetString("LRN");
        var enrollment = _context.Students.FirstOrDefault(e => e.LRN == userId);

        if (enrollment == null)
        {
            return NotFound("Enrollment record not found.");
        }
        return View(enrollment);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public async Task<IActionResult> Account()
    {
        var userId = HttpContext.Session.GetString("LRN");
        var account = await _context.Students.FirstOrDefaultAsync(c => c.LRN == userId);
        if (account == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction("Login", "Account");
        }

        return View(account);
    }

    public async Task<IActionResult> ViewAccount(string userId)
    {
        var account = await _context.Students.FirstOrDefaultAsync(c => c.LRN == userId);
        if (account == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction("Login", "Account");
        }
        var enrollView = new EnrollmentRequirementsViewModel
        {
            Enrollment = account,
            Requirements = await _context.RequirementModels.Where(c => c.EnrollmentId == account.EnrollmentId).ToListAsync()
        };
        return View(enrollView);
    }

    public async Task<IActionResult> Notification()
    {
        var userId = HttpContext.Session.GetString("LRN");
        var notifications = await _context.Notifications
            .Where(c => c.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return View(notifications);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }


    [HttpPost]
    public async Task<IActionResult> UploadDocument(int id, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("File", "Please select a file to upload.");
            return RedirectToAction("Index");
        }

        var requirement = await _context.RequirementModels.FindAsync(id);
        if (requirement == null)
        {
            TempData["ErrorMessage"] = $"Requirement with ID {id} not found.";
            return RedirectToAction("Index");
        }

        var fileUrl = await _fileUploadService.UploadFileToCloudinaryAsync(file);
        requirement.UploadedFile = fileUrl;

        requirement.IsRejected = false;

        _context.Update(requirement);
        await _context.SaveChangesAsync();


        Console.WriteLine(fileUrl);

        TempData["SuccessMessage"] = "File uploaded successfully.";
        return RedirectToAction("Document");
    }

    [HttpPost]
    public async Task<IActionResult> ApproveRequirement(int id)
    {
        var requirement = await _context.RequirementModels.FindAsync(id);
        if (requirement != null)
        {
            requirement.IsApproved = true;
            requirement.IsRejected = false;
            _context.RequirementModels.Update(requirement);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        return Json(new { success = false });
    }

    [HttpPost]
    public async Task<IActionResult> RejectRequirement(int id)
    {
        var requirement = await _context.RequirementModels.FindAsync(id);
        if (requirement != null)
        {
            requirement.IsRejected = true;
            requirement.IsApproved = false;
            _context.RequirementModels.Update(requirement);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        return Json(new { success = false });
    }


    public async Task<IActionResult> ViewDocument(int id)
    {
        var requirement = await _context.RequirementModels.FindAsync(id);
        if (requirement == null || string.IsNullOrEmpty(requirement.UploadedFile))
        {
            TempData["ErrorMessage"] = "The file does not exist.";
            return RedirectToAction("Document");
        }
        return Redirect(requirement.UploadedFile);
    }

    public async Task<IActionResult> DownloadDocument(int id)
    {
        var requirement = await _context.RequirementModels.FindAsync(id);
        if (requirement == null || string.IsNullOrEmpty(requirement.UploadedFile))
        {
            TempData["ErrorMessage"] = "The file does not exist.";
            return RedirectToAction("Document");
        }

        var fileBytes = await _fileDownloadService.DownloadFileFromUrlAsync(requirement.UploadedFile);
        return File(fileBytes, "application/octet-stream", Path.GetFileName(requirement.UploadedFile));
    }

    public async Task<IActionResult> DeleteDocument(int id)
    {
        var requirement = await _context.RequirementModels.FindAsync(id);
        if (requirement == null || string.IsNullOrEmpty(requirement.UploadedFile))
        {
            TempData["ErrorMessage"] = "The file does not exist.";
            return RedirectToAction("Document");
        }
        requirement.UploadedFile = "";
        _context.RequirementModels.Update(requirement);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "File deleted successfully.";
        return RedirectToAction("Document");
    }




}
