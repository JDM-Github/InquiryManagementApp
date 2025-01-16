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
                var body = $"Dear {student.Firstname} {student.Surname},<br>You've successfully paid {amount} for your balance. Your remaining balance is {student.BalanceToPay}.";
                await _emailService.SendEmailAsync(student.Email, subject, body);

                var notification = new Notification
                {
                    Message = $"You've successfully been paid for your balance. Your remaining balance is {student.BalanceToPay}",
                    UserId = student.LRN ?? "",
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

    // public async Task<string> GetAccessToken()
    // {
    //     try
    //     {
    //         var url = "https://api.sandbox.paypal.com/v1/oauth2/token";
    //         var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(
    //             $"{_configuration["PaypalAccount:ClientId"]}:{_configuration["PaypalAccount:Secret"]}"));

    //         using (var client = new HttpClient())
    //         {
    //             client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
    //             client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); 

    //             var content = new FormUrlEncodedContent(new[]
    //             {
    //             new KeyValuePair<string, string>("grant_type", "client_credentials")
    //         });

    //             var response = await client.PostAsync(url, content);

    //             if (!response.IsSuccessStatusCode)
    //             {
    //                 var errorContent = await response.Content.ReadAsStringAsync();
    //                 Console.WriteLine($"Error fetching access token: {errorContent}");
    //                 throw new Exception($"Failed to fetch access token. Status Code: {response.StatusCode}");
    //             }

    //             var responseContent = await response.Content.ReadAsStringAsync();
    //             var data = JsonConvert.DeserializeObject<dynamic>(responseContent);

    //             Console.WriteLine($"Access Token: {data.access_token}");
    //             return data.access_token;
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine($"Error in GetAccessToken: {ex.Message}");
    //         throw; 
    //     }
    // }


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

                TempData["SuccessMessage"] = "Payment successful. Your payment for this month has been paid SUCCESSFULLY";

                var student = await _context.Students.FirstOrDefaultAsync(s => s.EnrollmentId == payment.UserId);
                var subject = "Payment Successful";
                var body = $"Dear {student!.Firstname} {student.Surname},<br>Your payment has been successfully processed.";
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



    // public async Task<IActionResult> Pay()
    // {
    //     var userId = HttpContext.Session.GetInt32("EnrollmentId");
    //     var user = await _context.Students.FirstOrDefaultAsync(e => e.EnrollmentId == userId);
    //     if (user == null)
    //     {
    //         TempData["ErrorMessage"] = "User not found.";
    //         return RedirectToAction("PaymentHistory");
    //     }

    //     var paymentSchedule = PaymentSchedule.CurrentPaymentSchedule;
    //     var currentPaymentId = paymentSchedule!.CurrentPaymentId;

    //     var existingPayment = _context.Payments
    //         .FirstOrDefault(p => p.EnrollreesId == userId && p.PaymentId.ToString() == currentPaymentId
    //             && p.Status == "Paid" || p.Status == "Pending");

    //     if (existingPayment != null)
    //     {
    //         TempData["ErrorMessage"] = "You have already made the payment for this schedule.";
    //         return RedirectToAction("PaymentHistory");
    //     }

    //     var referenceNumber = Guid.NewGuid().ToString();
    //     var successUrl = $"{Request.Scheme}://{Request.Host}/Payment/PaymentSuccess?reference={referenceNumber}";
    //     var cancelUrl = $"{Request.Scheme}://{Request.Host}/Payment/PaymentCancelled?reference={referenceNumber}";

    //     var fee = await _context.Fees.FirstOrDefaultAsync(f => f.Level == user!.GradeLevel);
    //     var adjustedLineItems = new List<object>
    //     {
    //         new
    //         {
    //             currency = "PHP",
    //             images = new string[] { "https://cdn-icons-png.flaticon.com/512/5166/5166991.png" },
    //             amount = (int)(fee!.Fee * 100),
    //             name = "Payment Fee",
    //             quantity = 1,
    //             description = "Payment fee for tuition"
    //         }
    //     };
    //     var lineItems = adjustedLineItems.ToArray();
    //     var payload = new
    //     {
    //         data = new
    //         {
    //             attributes = new
    //             {
    //                 billing = new
    //                 {
    //                     address = new
    //                     {
    //                         line1 = user!.Address,
    //                         country = "PH"
    //                     },
    //                     name = user.Firstname + " " + user.Middlename + " " + user.Surname,
    //                     email = user.Email,
    //                     phone = ""
    //                 },
    //                 send_email_receipt = true,
    //                 show_description = true,
    //                 show_line_items = true,
    //                 payment_method_types = new string[] { "qrph", "billease", "card", "dob", "dob_ubp", "brankas_bdo", "gcash", "brankas_landbank", "brankas_metrobank", "grab_pay", "paymaya" },
    //                 line_items = lineItems,
    //                 description = "Payment for school tuition",
    //                 reference_number = referenceNumber,
    //                 statement_descriptor = "Inquiry Management",
    //                 success_url = successUrl,
    //                 cancel_url = cancelUrl,
    //             }
    //         }
    //     };

    //     var jsonPayload = JsonConvert.SerializeObject(payload);
    //     var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

    //     try
    //     {
    //         _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(_configuration["PayMongo:SecretKey"])));
    //         var response = await _httpClient.PostAsync("https://api-m.sandbox.paypal.com/v2/checkout/orders", content);
    //         if (response.IsSuccessStatusCode)
    //         {
    //             var responseContent = await response.Content.ReadAsStringAsync();

    //             var jsonDeserialized = JsonConvert.DeserializeObject<dynamic>(responseContent);
    //             var paymentLink = jsonDeserialized?.data?.attributes?.checkout_url;

    //             var payment = new Payment
    //             {
    //                 Date = DateTime.Now,
    //                 PaymentId = currentPaymentId!,
    //                 PaidAmount = fee.Fee,
    //                 ReferenceNumber = referenceNumber,
    //                 PaymentMethod = "Paymongo",
    //                 PaymentLink = paymentLink!.ToString(),
    //                 Status = "Pending",
    //                 EnrollreesId = user.EnrollmentId,
    //                 TransactionId = jsonDeserialized!.data!.id,
    //                 ExpirationTime = DateTime.UtcNow.AddMinutes(15)
    //             };

    //             _context.Payments.Add(payment);
    //             _context.SaveChanges();
    //             return Redirect(paymentLink.ToString());
    //         }
    //         else
    //         {
    //             TempData["ErrorMessage"] = "Failed to create payment link.";
    //             return RedirectToAction("PaymentHistory");
    //         }
    //     }
    //     catch (Exception)
    //     {
    //         TempData["ErrorMessage"] = "An error occurred while processing your payment.";
    //         return RedirectToAction("PaymentHistory");
    //     }
    // }

    // public async Task<IActionResult> PaymentSuccess(string reference)
    // {
    //     var transaction = await _context.Payments
    //         .FirstOrDefaultAsync(t => t.ReferenceNumber == reference);

    //     if (transaction == null)
    //     {
    //         TempData["ErrorMessage"] = "Payment not found.";
    //         return RedirectToAction("PaymentHistory");
    //     }

    //     transaction.Status = "Paid";
    //     transaction.PaymentMethod = "Paymongo";

    //     var user = await _context.Students.FirstOrDefaultAsync(c => c.EnrollmentId == transaction.EnrollreesId);

    //     _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(_configuration["PayMongo:SecretKey"])));
    //     var response = await _httpClient.GetAsync(
    //         "https://api.paymongo.com/v1/checkout_sessions/" + transaction.TransactionId.ToString());

    //     if (response.IsSuccessStatusCode)
    //     {
    //         var responseContent = await response.Content.ReadAsStringAsync();
    //         var jsonDeserialized = JsonConvert.DeserializeObject<dynamic>(responseContent);
    //         transaction.PaymentMethod = ((string)jsonDeserialized!.data!.attributes!.payment_method_used).ToUpper();
    //     }

    //     await _context.SaveChangesAsync();
    //     var notification = new Notification
    //     {
    //         Message = $"You successfully paid the tuition in this semester. You paid {transaction.PaidAmount}.",
    //         UserId = user!.LRN,
    //         CreatedAt = DateTime.Now,
    //         IsRead = false
    //     };
    //     var recent = new RecentActivity
    //     {
    //         Activity = $"User {user.Firstname} {user.Surname} paid {transaction.PaidAmount}",
    //         CreatedAt = DateTime.Now
    //     };
    //     _context.RecentActivities.Add(recent);
    //     _context.Notifications.Add(notification);
    //     await _context.SaveChangesAsync();

    //     TempData["SuccessMessage"] = "Payment successful!";
    //     return RedirectToAction("PaymentHistory");
    // }

    // public async Task<IActionResult> PaymentCancelled(string reference)
    // {
    //     var transaction = await _context.Payments
    //         .FirstOrDefaultAsync(t => t.ReferenceNumber == reference);

    //     if (transaction == null || transaction.Status == "Expired")
    //     {
    //         TempData["ErrorMessage"] = "This transaction has expired.";
    //         return RedirectToAction("PaymentHistory");
    //     }
    //     transaction.Status = "Cancelled";
    //     await _context.SaveChangesAsync();
    //     TempData["ErrorMessage"] = "Payment was cancelled. Please try again.";
    //     return RedirectToAction("PaymentHistory");
    // }

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

        // var existingPayment = await _context.Payments
        //     .FirstOrDefaultAsync(p => p.EnrollreesId == student!.EnrollmentId
        //         && p.Status == "Paid" || p.Status == "Pending");

        // if (existingPayment != null)
        // {
        //     TempData["ErrorMessage"] = "User have already paid.";
        //     return RedirectToAction("ManageTransactions", "Admin");
        // }

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

        // TempData["ErrorMessage"] = "Failed to process the payment.";
        // return RedirectToAction("ManageTransactions", "Admin");
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
