using Microsoft.AspNetCore.Mvc;
using InquiryManagementApp.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

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

    public async Task<IActionResult> PaymentHistory()
    {
        var userIdString = HttpContext.Session.GetString("LRN");
        var existingSchedule = _context.PaymentSchedules.FirstOrDefault();

        bool isPaymentScheduleActive = existingSchedule != null && existingSchedule!.IsActive &&
            DateTime.Now >= existingSchedule!.StartDate && DateTime.Now <= existingSchedule.EndDate;

        if (userIdString == null)
        {
            TempData["ErrorMessage"] = "You must be logged in to view payments.";
            return RedirectToAction("Login", "Account");
        }
        var user = await _context.Students.FirstOrDefaultAsync(e => e.LRN == userIdString);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction("Login", "Account");
        }

        var payments = await _context.Payments
            .Where(p => p.EnrollreesId == user.EnrollmentId)
            .OrderDescending()
            .ToListAsync();

        var fee = await _context.Fees.FirstOrDefaultAsync(f => f.Level == user!.GradeLevel);
        bool isAlreadyPaid = payments.Any(p => p.PaymentId.ToString() == existingSchedule!.CurrentPaymentId && p.Status == "Paid");
        ViewBag.IsPaymentScheduleActive = isPaymentScheduleActive;
        ViewBag.IsAlreadyPaid = isAlreadyPaid;
        ViewBag.Fee = fee!.Fee;
        return View(payments);
    }


    public async Task<IActionResult> Pay()
    {
        var userId = HttpContext.Session.GetInt32("EnrollmentId");
        var user = await _context.Students.FirstOrDefaultAsync(e => e.EnrollmentId == userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction("PaymentHistory");
        }


        var paymentSchedule = PaymentSchedule.CurrentPaymentSchedule;
        var currentPaymentId = paymentSchedule!.CurrentPaymentId;

        var existingPayment = _context.Payments
            .FirstOrDefault(p => p.EnrollreesId == userId && p.PaymentId.ToString() == currentPaymentId
                && p.Status == "Paid" || p.Status == "Pending");

        if (existingPayment != null)
        {
            TempData["ErrorMessage"] = "You have already made the payment for this schedule.";
            return RedirectToAction("PaymentHistory");
        }

        var referenceNumber = Guid.NewGuid().ToString();
        var successUrl = $"{Request.Scheme}://{Request.Host}/Payment/PaymentSuccess?reference={referenceNumber}";
        var cancelUrl = $"{Request.Scheme}://{Request.Host}/Payment/PaymentCancelled?reference={referenceNumber}";

        var fee = await _context.Fees.FirstOrDefaultAsync(f => f.Level == user!.GradeLevel);
        var adjustedLineItems = new List<object>
        {
            new
            {
                currency = "PHP",
                images = new string[] { "https://cdn-icons-png.flaticon.com/512/5166/5166991.png" },
                amount = (int)(fee!.Fee * 100),
                name = "Payment Fee",
                quantity = 1,
                description = "Payment fee for tuition"
            }
        };
        var lineItems = adjustedLineItems.ToArray();
        var payload = new
        {
            data = new
            {
                attributes = new
                {
                    billing = new
                    {
                        address = new
                        {
                            line1 = user!.Address,
                            country = "PH"
                        },
                        name = user.Firstname + " " + user.Middlename + " " + user.Surname,
                        email = user.Email,
                        phone = ""
                    },
                    send_email_receipt = true,
                    show_description = true,
                    show_line_items = true,
                    payment_method_types = new string[] { "qrph", "billease", "card", "dob", "dob_ubp", "brankas_bdo", "gcash", "brankas_landbank", "brankas_metrobank", "grab_pay", "paymaya" },
                    line_items = lineItems,
                    description = "Payment for school tuition",
                    reference_number = referenceNumber,
                    statement_descriptor = "Inquiry Management",
                    success_url = successUrl,
                    cancel_url = cancelUrl,
                }
            }
        };

        var jsonPayload = JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(_configuration["PayMongo:SecretKey"])));
            var response = await _httpClient.PostAsync("https://api.paymongo.com/v1/checkout_sessions", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                var jsonDeserialized = JsonConvert.DeserializeObject<dynamic>(responseContent);
                var paymentLink = jsonDeserialized?.data?.attributes?.checkout_url;

                var payment = new Payment
                {
                    Date = DateTime.Now,
                    PaymentId = currentPaymentId!,
                    PaidAmount = fee.Fee,
                    ReferenceNumber = referenceNumber,
                    PaymentMethod = "Paymongo",
                    PaymentLink = paymentLink!.ToString(),
                    Status = "Pending",
                    EnrollreesId = user.EnrollmentId,
                    TransactionId = jsonDeserialized!.data!.id,
                    ExpirationTime = DateTime.UtcNow.AddMinutes(15)
                };

                _context.Payments.Add(payment);
                _context.SaveChanges();
                return Redirect(paymentLink.ToString());
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to create payment link.";
                return RedirectToAction("PaymentHistory");
            }
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "An error occurred while processing your payment.";
            return RedirectToAction("PaymentHistory");
        }
    }

    public async Task<IActionResult> PaymentSuccess(string reference)
    {
        var transaction = await _context.Payments
            .FirstOrDefaultAsync(t => t.ReferenceNumber == reference);

        if (transaction == null)
        {
            TempData["ErrorMessage"] = "Payment not found.";
            return RedirectToAction("PaymentHistory");
        }

        transaction.Status = "Paid";
        transaction.PaymentMethod = "Paymongo";

        var user = await _context.Students.FirstOrDefaultAsync(c => c.EnrollmentId == transaction.EnrollreesId);

        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(_configuration["PayMongo:SecretKey"])));
        var response = await _httpClient.GetAsync(
            "https://api.paymongo.com/v1/checkout_sessions/" + transaction.TransactionId.ToString());

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonDeserialized = JsonConvert.DeserializeObject<dynamic>(responseContent);
            transaction.PaymentMethod = ((string)jsonDeserialized!.data!.attributes!.payment_method_used).ToUpper();
        }

        await _context.SaveChangesAsync();
        var notification = new Notification
        {
            Message = $"You successfully paid the tuition in this semester. You paid {transaction.PaidAmount}.",
            UserId = user!.LRN,
            CreatedAt = DateTime.Now,
            IsRead = false
        };
        var recent = new RecentActivity
        {
            Activity = $"User {user.Firstname} {user.Surname} paid {transaction.PaidAmount}",
            CreatedAt = DateTime.Now
        };
        _context.RecentActivities.Add(recent);
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Payment successful!";
        return RedirectToAction("PaymentHistory");
    }

    public async Task<IActionResult> PaymentCancelled(string reference)
    {
        var transaction = await _context.Payments
            .FirstOrDefaultAsync(t => t.ReferenceNumber == reference);

        if (transaction == null || transaction.Status == "Expired")
        {
            TempData["ErrorMessage"] = "This transaction has expired.";
            return RedirectToAction("PaymentHistory");
        }
        transaction.Status = "Cancelled";
        await _context.SaveChangesAsync();
        TempData["ErrorMessage"] = "Payment was cancelled. Please try again.";
        return RedirectToAction("PaymentHistory");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitWalkInPayment(string EnrollreesId, bool IsEarlyBird)
    {
        if (ModelState.IsValid)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(e => e.LRN == EnrollreesId && e.IsApproved == true);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Student not found.";
                return RedirectToAction("ManageTransactions", "Admin");
            }

            var paymentSchedule = PaymentSchedule.CurrentPaymentSchedule;
            var currentPaymentId = paymentSchedule!.CurrentPaymentId;
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.EnrollreesId == student!.EnrollmentId
                    && p.Status == "Paid" || p.Status == "Pending");

            if (existingPayment != null)
            {
                TempData["ErrorMessage"] = "User have already paid.";
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
                PaymentId = PaymentSchedule.CurrentPaymentSchedule!.CurrentPaymentId!,
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

        TempData["ErrorMessage"] = "Failed to process the payment.";
        return RedirectToAction("ManageTransactions", "Admin");
    }



    // public IActionResult SetPaymentDate()
    // {
    //     return View();
    // }


    // [HttpPost]
    // [ValidateAntiForgeryToken]
    // public async Task<IActionResult> SetPaymentDate([Bind("StartDate,EndDate")] PaymentSchedule paymentSchedule)
    // {
    //     if (ModelState.IsValid)
    //     {
    //         var generateId = Guid.NewGuid().ToString();
    //         if (PaymentSchedule.InstanceExists)
    //         {
    //             var schedule = _context.PaymentSchedules.FirstOrDefault();
    //             schedule!.StartDate = paymentSchedule.StartDate;
    //             schedule.EndDate = paymentSchedule.EndDate;
    //             schedule.PaymentIds.Add(generateId);
    //             schedule.CurrentPaymentId = generateId;
    //             return RedirectToAction(nameof(Index));
    //         } else {
    //             PaymentSchedule.CreateInstance();
    //             paymentSchedule.PaymentIds.Add(generateId);
    //             paymentSchedule.CurrentPaymentId = generateId;
    //             _context.PaymentSchedules.Add(paymentSchedule);
    //             await _context.SaveChangesAsync();
    //             return RedirectToAction(nameof(Index));
    //         }
    //     }
    //     return View();
    // }

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


    // [HttpPost]
    // public IActionResult ProcessPayment(int amount)
    // {
    //     var userId = HttpContext.Session.GetString("LRN");
    //     var enrollment = _context.Students
    //         .Include(e => e.PaymentHistories)
    //         .FirstOrDefault(e => e.LRN == userId);

    //     if (enrollment == null)
    //     {
    //         return NotFound("Enrollment record not found.");
    //     }

    //     if (amount <= 0 || enrollment.FeePaid + amount > 5000)
    //     {
    //         TempData["ErrorMessage"] = "Invalid payment amount.";
    //         return RedirectToAction("Payment", "Home");
    //     }

    //     enrollment.FeePaid += amount;
    //     var payment = new Payment
    //     {
    //         Date = DateTime.Now,
    //         Amount = amount,
    //         EnrollmentId = enrollment.EnrollmentId
    //     };
    //     _context.Payments.Add(payment);
    //     _context.SaveChanges();

    //     TempData["SuccessMessage"] = $"Payment of ₱{amount} successfully processed.";
    //     return RedirectToAction("Payment", "Home");
    // }

}
