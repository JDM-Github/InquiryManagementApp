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

        var student = await _context.Students
                .Where(s => s.EnrollmentId == enrollees.EnrollmentId)
                .Select(s => new StudentViewModel
                {
                    EnrollmentId = s.EnrollmentId,
                    StudentID = s.StudentID ?? "",
                    FirstName = s.Firstname,
                    MiddleName = s.Middlename ?? "",
                    Surname = s.Surname,
                    GradeLevel = s.GradeLevel,
                    Birthday = s.DateOfBirth,
                    Email = s.Email,
                    Address = s.Address,
                    TotalToPay = s.TotalToPay,
                    PaymentType = s.PaymentType
                })
                .FirstOrDefaultAsync();

        if (student == null)
        {
            TempData["ErrorMessage"] = "Student not found.";
            return RedirectToAction("Walkin", "Enrollment");
        }
        return View(student);

    }

    public async Task<IActionResult> PayEnrollment(int studentId, double? amount)
    {
        var enrollees = await _context.Students.FirstOrDefaultAsync(e => e.EnrollmentId == studentId);
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

        studentPay.PaymentType = enrollees.PaymentType;
        var firstPayment = amount ?? enrollees.TotalToPay;
        if (firstPayment > enrollees.TotalToPay)
        {
            firstPayment = enrollees.TotalToPay;
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
                enrollees.BalanceToPay = enrollees.TotalToPay - amount;
                enrollees.IsEnrolled = true;
                enrollees.EnrolledDate = DateTime.Now;
                _context.Update(enrollees);
                await _context.SaveChangesAsync();

                if (enrollees.IsApproved) {
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
                                        <p style='font-size: 16px; color: #0056b3;'>Dear {enrollees.Firstname} {enrollees.Surname},</p>
                                        <p style='font-size: 14px;'>We are pleased to inform you that your payment of {amount} has been successfully processed.</p>
                                        <p style='font-size: 14px;'>Here are your account details:</p>
                                        <p><strong>Username:</strong> {enrollees.Username}</p>
                                        <p><strong>Password:</strong> {enrollees.Password}</p>
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

                    await _emailService.SendEmailAsync(enrollees.Email, subject, body);
                } else {
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
                                        <p style='font-size: 16px; color: #0056b3;'>Dear {enrollees.Firstname} {enrollees.Surname},</p>
                                        <p style='font-size: 14px;'>We are pleased to inform you that your payment of {amount} has been successfully processed.</p>
                                        <p style='font-size: 14px;'>Here are your account details:</p>
                                        <p><strong>Temporary Username:</strong> {enrollees.TemporaryUsername}</p>
                                        <p><strong>Temporay Password:</strong> {enrollees.TemporaryPassword}</p>
                                        <br>
                                        <p style='font-size: 14px;'>To get the permanent account details. We will need to approve your enrollment first.</p>
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

                    await _emailService.SendEmailAsync(enrollees.Email, subject, body);
                }

                
                var notification = new Notification
                {
                    Message = $"You've successfully been enrolled.",
                    UserId = enrollees.EnrollmentId,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                var recent = new RecentActivity
                {
                    Activity = $"Enrollment payment success for {enrollees.Firstname} {enrollees.Surname}. Amount: {amount}",
                    CreatedAt = DateTime.Now
                };
                _context.RecentActivities.Add(recent);
                await _context.SaveChangesAsync();


                DateTime date = DateTime.Now;
                string monthName = date.ToString("MMMM");
                var Payment = new StudentPayment
                {
                    UserId = enrollees.EnrollmentId,
                    ReferenceNumber = Guid.NewGuid().ToString(),
                    PaymentAmount = amount,
                    MonthPaid = monthName,
                    YearPaid = DateTime.Now.Year.ToString(),
                    Status = "Paid",
                    PaymentFor = "First Payment",
                    Date = DateTime.Now
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
        var userId = HttpContext.Session.GetInt32("EnrollmentId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var enrollment = _context.Students
            .Where(e => e.EnrollmentId == userId)
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

    public async Task<IActionResult> ViewAccount(int userId)
    {
        var account = await _context.Students.FirstOrDefaultAsync(c => c.EnrollmentId == userId);
        if (account == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction("Index", "Admin");
        }
        var enrollView = new EnrollmentRequirementsViewModel
        {
            Enrollment = account,
            Requirements = await _context.RequirementModels.Where(c => c.EnrollmentId == account.EnrollmentId).ToListAsync()
        };
        ViewBag.UserId = userId;
        return View(enrollView);
    }

    public async Task<IActionResult> Notification()
    {
        var userId = HttpContext.Session.GetInt32("EnrollmentId");
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

            var notification = new Notification
            {
                Message = $"Requirement {requirement.RequirementName} has been approved.",
                UserId = requirement.EnrollmentId,
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notification);
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

            var notification = new Notification
            {
                Message = $"Requirement has been rejected. {requirement.RequirementName}",
                UserId = requirement.EnrollmentId,
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notification);
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
