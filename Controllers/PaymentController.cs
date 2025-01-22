using Microsoft.AspNetCore.Mvc;
using InquiryManagementApp.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;
using System.Net.Mime;
using System.Net.Http.Headers;
using PayPalCheckoutSdk.Orders;
using PayPalCheckoutSdk.Core;
using Helper;

public class PaymentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly EmailService _emailService;

    public PaymentController(ApplicationDbContext context, HttpClient httpClient, IConfiguration configuration, EmailService emailService)
    {
        _context = context;
        _httpClient = httpClient;
        _configuration = configuration;
        _emailService = emailService;
    }

    public async Task<bool> IsAlreadyCreated(int userId, string month)
    {
        string currentYear = DateTime.Now.Year.ToString();
        return await _context.StudentPayments
                .AnyAsync(payment =>
                    payment.UserId == userId &&
                    payment.MonthPaid == month &&
                    payment.YearPaid == currentYear);
    }


    public async Task<IActionResult> PaymentHistory()
    {
        var userIdString = HttpContext.Session.GetInt32("EnrollmentId");
        if (!userIdString.HasValue)
        {
            TempData["ErrorMessage"] = "You must be logged in to view payments.";
            return RedirectToAction("Login", "Account");
        }
        var user = await _context.Students.FirstOrDefaultAsync(e => e.EnrollmentId == userIdString);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction("Login", "Account");
        }

        var payments = await _context.StudentPayments
            .Where(p => p.UserId == user.EnrollmentId)
            .OrderDescending()
            .ToListAsync();

        var payment = await _context.StudentPaymentRecords.FirstOrDefaultAsync(p => p.UserId == user.EnrollmentId);

        if (payment == null)
        {
            ViewBag.IsAlreadyPaid = false;
            ViewBag.IsPaymentScheduleActive = false;;
            TempData["ErrorMessage"] = "Payment record not found.";
            return View(payments);
        }
        DateTime date = DateTime.Now;
        string monthName = date.ToString("MMMM");
        bool IsCreated = await IsAlreadyCreated(user.EnrollmentId, monthName);

        if (!IsCreated && payment.PaymentType == "Quarterly")
        {
            if (monthName == "October" || monthName == "January" || monthName == "April")
            {
                var Payment = new StudentPayment {
                    UserId = user.EnrollmentId,
                    ReferenceNumber = "-----",
                    PaymentAmount = payment.PerPayment ?? 4750,
                    MonthPaid = monthName,
                    YearPaid = DateTime.Now.Year.ToString(),
                    Date = null
                };
                _context.StudentPayments.Add(Payment);
                await _context.SaveChangesAsync();
            }
        }
        else if (!IsCreated && payment.PaymentType == "Monthly")
        {
            if (monthName == "September" || monthName == "October" || monthName == "November" || monthName == "December" || monthName == "January" || monthName == "February" || monthName == "March" || monthName == "April" || monthName == "May")
            {
                var Payment = new StudentPayment
                {
                    UserId = user.EnrollmentId,
                    ReferenceNumber = "-----",
                    PaymentAmount = payment.PerPayment ?? 1900,
                    MonthPaid = monthName,
                    YearPaid = DateTime.Now.Year.ToString(),
                    Date = null
                };
                _context.StudentPayments.Add(Payment);
                await _context.SaveChangesAsync();
            }
        }
        else if (!IsCreated && payment.PaymentType == "Initial5")
        {
            if (monthName == "August" || monthName == "September" || monthName == "October" || monthName == "November" || monthName == "December" || monthName == "January" || monthName == "February" || monthName == "March" || monthName == "April" || monthName == "May")
            {
                var Payment = new StudentPayment
                {
                    UserId = user.EnrollmentId,
                    ReferenceNumber = "-----",
                    PaymentAmount = 2800,
                    MonthPaid = monthName,
                    YearPaid = DateTime.Now.Year.ToString(),
                    Date = null
                };
                _context.StudentPayments.Add(Payment);
                await _context.SaveChangesAsync();
            }
        }
        ViewBag.UserId = user.EnrollmentId;
        ViewBag.PaymentId = payment.Id;
        ViewBag.PaymentType = user.PaymentType;
        ViewBag.RemainingBalance = user.BalanceToPay;

        return View(payments);
    }

    public async Task<IActionResult> SubmitBalancePayment(int userId, int paymentId, double AmountToPay)
    {
        var payment = await _context.StudentPaymentRecords.FirstOrDefaultAsync(p => p.Id == paymentId);
        if (payment == null)
        {
            TempData["ErrorMessage"] = "Invalid payment record.";
            return RedirectToAction("PaymentHistory");
        }

        var student = await _context.Students.FirstOrDefaultAsync(p => p.EnrollmentId == userId);
        if (student == null)
        {
            TempData["ErrorMessage"] = "Invalid student record.";
            return RedirectToAction("PaymentHistory");
        }
        if (student.BalanceToPay <= 0)
        {
            TempData["ErrorMessage"] = "Balance is already paid.";
            return RedirectToAction("PaymentHistory");
        }

        var firstPayment = AmountToPay;
        if (firstPayment > student.BalanceToPay)
        {
            firstPayment = student.BalanceToPay;
        }
        var orderRequest = new OrdersCreateRequest();
        orderRequest.Prefer("return=representation");
        orderRequest.RequestBody(new OrderRequest
        {
            CheckoutPaymentIntent = "CAPTURE",
            ApplicationContext = new ApplicationContext
            {
                ReturnUrl = Url.Action("PaymentBalanceSuccess", "Payment", new { userId, paymentId = payment.Id, amount = firstPayment }, Request.Scheme),
                CancelUrl = Url.Action("PaymentBalanceFailed", "Payment", new { userId, paymentId = payment.Id, amount = firstPayment }, Request.Scheme)
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
                    Description = "Balance Fee"
                }
            }
        });

        try
        {
            var client = new PayPalHttpClient(PayPalConfig.GetEnvironment());
            var response = await client.Execute(orderRequest);
            var result = response.Result<Order>();

            HttpContext.Session.SetString("PayPalOrderId", result.Id ?? "");

            var approvalUrl = result.Links.FirstOrDefault(link => link.Rel == "approve")?.Href;
            if (approvalUrl != null)
            {
                return Redirect(approvalUrl);
            }
            else
            {
                TempData["ErrorMessage"] = "Payment approval URL not found.";
                return RedirectToAction("PaymentHistory");
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error during payment: {ex.Message}";
            return RedirectToAction("PaymentHistory");
        }
    }

    public async Task<IActionResult> PaymentBalanceSuccess(int userId, string paymentId, double amount)
    {
        var student = await _context.Students.FirstOrDefaultAsync(e => e.EnrollmentId == userId);
        if (student == null)
        {
            TempData["ErrorMessage"] = "Student not found.";
            return RedirectToAction("PaymentHistory");
        }
        var payment = await _context.StudentPaymentRecords.FirstOrDefaultAsync(p => p.Id == int.Parse(paymentId));

        try
        {
            var client = new PayPalHttpClient(PayPalConfig.GetEnvironment());

            var captureRequest = new OrdersCaptureRequest(HttpContext.Session.GetString("PayPalOrderId") ?? "");
            captureRequest.RequestBody(new OrderActionRequest());

            var response = await client.Execute(captureRequest);
            var result = response.Result<Order>();

            if (result.Status == "COMPLETED")
            {
                student.BalanceToPay = student.BalanceToPay - amount;
                _context.Update(student);
                await _context.SaveChangesAsync();

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
                                        <p style='font-size: 14px;'>We are happy to inform you that you have successfully paid {amount} for your balance.</p>
                                        <p style='font-size: 14px;'>Your remaining balance is: <strong>{student.BalanceToPay}</strong></p>
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
                await _emailService.SendEmailAsync(student.Email, subject, body);


                var notification = new Notification
                {
                    Message = $"You've successfully paid for your balance. Your remaining balance is {student.BalanceToPay}",
                    UserId = student.EnrollmentId,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                DateTime date = DateTime.Now;
                string monthName = date.ToString("MMMM");

                var payment2 = new StudentPayment
                {
                    UserId = student.EnrollmentId,
                    ReferenceNumber = Guid.NewGuid().ToString(),
                    PaymentAmount = amount,
                    MonthPaid = monthName,
                    YearPaid = DateTime.Now.Year.ToString(),
                    Status = "Paid",
                    PaymentFor = "Balance",
                    Date = DateTime.Now
                };
                _context.StudentPayments.Add(payment2);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Payment Successful!";
                return RedirectToAction("PaymentHistory");
            }
            else
            {
                TempData["ErrorMessage"] = "Payment was not completed.";
                return RedirectToAction("PaymentHistory");
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error capturing payment: {ex.Message}";
            return RedirectToAction("PaymentHistory");
        }
    }

    public IActionResult PaymentBalanceFailed(int paymentId, double amount)
    {
        TempData["ErrorMessage"] = "Payment failed. Please try again later.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Pay(int paymentId)
    {
        var payment = await _context.StudentPayments.FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null || payment.Status != "Pending")
        {
            TempData["ErrorMessage"] = "Invalid payment or already processed.";
            return RedirectToAction("Index");
        }

        var firstPayment = payment.PaymentAmount;
        var orderRequest = new OrdersCreateRequest();
        orderRequest.Prefer("return=representation");
        orderRequest.RequestBody(new OrderRequest
        {
            CheckoutPaymentIntent = "CAPTURE",
            ApplicationContext = new ApplicationContext
            {
                ReturnUrl = Url.Action("PaymentSuccess", "Payment", new { paymentId = payment.Id }, Request.Scheme),
                CancelUrl = Url.Action("PaymentFailed", "Payment", new { paymentId = payment.Id }, Request.Scheme)
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

        try
        {
            var client = new PayPalHttpClient(PayPalConfig.GetEnvironment());
            var response = await client.Execute(orderRequest);
            var result = response.Result<Order>();
            HttpContext.Session.SetString("PayPalOrderId", result.Id ?? "");

            var approvalUrl = result.Links.FirstOrDefault(link => link.Rel == "approve")?.Href;
            if (approvalUrl != null)
            {
                return Redirect(approvalUrl);
            }
            else
            {
                TempData["ErrorMessage"] = "Payment approval URL not found.";
                return RedirectToAction("PaymentHistory");
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error during payment: {ex.Message}";
            return RedirectToAction("PaymentHistory");
        }
    }

    // Action for when payment is successful
    public async Task<IActionResult> PaymentSuccess(string paymentId)
    {
        var payment = await _context.StudentPayments.FirstOrDefaultAsync(p => p.Id == int.Parse(paymentId));

        if (payment == null || payment.Status != "Pending")
        {
            TempData["ErrorMessage"] = "Invalid payment.";
            return RedirectToAction("PaymentHistory");
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
                payment.ReferenceNumber = Guid.NewGuid().ToString();
                payment.Date = DateTime.Now;
                payment.Status = "Paid";
                _context.Update(payment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Payment successful. Your payment for this month has been paid successfully";

                var student = await _context.Students.FirstOrDefaultAsync(s => s.EnrollmentId == payment.UserId);
                var subject = "Payment Successful";
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
                                        <p style='font-size: 16px; color: #0056b3;'>Dear <strong>{student!.Firstname} {student.Surname}</strong>,</p>
                                        <p style='font-size: 14px;'>We are happy to inform you that your payment has been successfully processed. Your payment for this month has been paid successfully.</p>
                                        <p style='font-size: 14px;'>If you need further assistance or have any questions, feel free to reach out:</p>
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


                return RedirectToAction("PaymentHistory");
            }
            else
            {
                TempData["ErrorMessage"] = "Payment was not completed.";
                return RedirectToAction("PaymentHistory");
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error capturing payment: {ex.Message}";
            return RedirectToAction("PaymentHistory");
        }

        
    }

    public IActionResult PaymentFailed(int paymentId)
    {
        TempData["ErrorMessage"] = "Payment failed. Please try again later.";
        return RedirectToAction("Index");
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitWalkInPayment(string EnrollreesId, bool IsEarlyBird)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(e => e.LRN == EnrollreesId && e.IsApproved == true);

        if (student == null)
        {
            TempData["ErrorMessage"] = "Student not found.";
            return RedirectToAction("ManageTransactions", "Admin");
        }
        double baseAmount = 31100;
        if (IsEarlyBird)
        {
            baseAmount -= 1900;
        }

        var payment = new Payment
        {
            Date = DateTime.Now,
            PaymentId = Guid.NewGuid().ToString(),
            TransactionId = Guid.NewGuid().ToString(),
            PaidAmount = baseAmount,
            ReferenceNumber = Guid.NewGuid().ToString(),
            PaymentMethod = "Walk-in",
            Status = "Paid",
            EnrollreesId = student.EnrollmentId,
            ExpirationTime = DateTime.Now.AddDays(7)
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Walk-in payment added successfully!";
        return RedirectToAction("ManageTransactions", "Admin");

    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPaymentDate(string ActionType, [Bind("StartDate,EndDate")] PaymentSchedule paymentSchedule)
    {
        if (ModelState.IsValid)
        {
            var generateId = Guid.NewGuid().ToString();
            var existingSchedule = _context.PaymentSchedules.FirstOrDefault();

            switch (ActionType)
            {
                case "New":
                    if (existingSchedule == null)
                    {
                        paymentSchedule.CurrentPaymentId = generateId;
                        paymentSchedule.PaymentIds.Add(generateId);
                        paymentSchedule.IsActive = true;
                        _context.PaymentSchedules.Add(paymentSchedule);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        existingSchedule!.StartDate = paymentSchedule.StartDate;
                        existingSchedule.EndDate = paymentSchedule.EndDate;
                        existingSchedule.CurrentPaymentId = generateId;
                        existingSchedule.IsActive = true;
                        existingSchedule.PaymentIds.Add(generateId);
                        _context.PaymentSchedules.Update(existingSchedule!);
                        await _context.SaveChangesAsync();
                    }
                    var students = await _context.Students.ToListAsync();
                    foreach (var student in students)
                    {
                        string subject = "Payment Day";
                        string body = $@"Payment Day";
                        await _emailService.SendEmailAsync(student.Email, subject, body);
                    }
                    TempData["SuccessMessage"] = "Payment schedule created successfully.";
                    break;
                case "Edit":
                    if (existingSchedule == null)
                    {
                        paymentSchedule.CurrentPaymentId = generateId;
                        paymentSchedule.PaymentIds.Add(generateId);
                        paymentSchedule.IsActive = true;
                        _context.PaymentSchedules.Add(paymentSchedule);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        existingSchedule!.StartDate = paymentSchedule.StartDate;
                        existingSchedule.EndDate = paymentSchedule.EndDate;
                        existingSchedule.IsActive = true;
                        _context.PaymentSchedules.Update(existingSchedule!);
                        await _context.SaveChangesAsync();
                    }
                    TempData["SuccessMessage"] = "Payment schedule updated successfully.";
                    break;
                default:
                    throw new InvalidOperationException("Invalid action type.");
            }
            return RedirectToAction("Index", "Admin");
        }
        return View();
    }

    public IActionResult Activate()
    {
        var existingSchedule = _context.PaymentSchedules.FirstOrDefault();
        if (existingSchedule == null)
        {
            TempData["ErrorMessage"] = "Payment schedule not found.";
            return RedirectToAction("ManageTransactions", "Admin");
        }
        existingSchedule!.IsActive = true;
        _context.PaymentSchedules.Update(existingSchedule!);
        _context.SaveChanges();
        TempData["SuccessMessage"] = "Payment schedule activated successfully.";
        return RedirectToAction("ManageTransactions", "Admin");
    }

    public IActionResult Close()
    {
        var existingSchedule = _context.PaymentSchedules.FirstOrDefault();
        if (existingSchedule == null)
        {
            TempData["ErrorMessage"] = "Payment schedule not found.";
            return RedirectToAction("ManageTransactions", "Admin");
        }
        existingSchedule!.IsActive = false;
        _context.PaymentSchedules.Update(existingSchedule!);
        _context.SaveChanges();
        TempData["SuccessMessage"] = "Payment schedule closed successfully.";
        return RedirectToAction("ManageTransactions", "Admin");
    }
}
