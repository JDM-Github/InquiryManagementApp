
// using Microsoft.AspNetCore.Mvc;

// namespace InquiryManagementApp.Controllers
// {
//     public class EmailController : Controller
//     {
//         private readonly EmailService _emailService;

//         public EmailController()
//         {
//             _emailService = new EmailService();
//         }

//         public async Task<IActionResult> SendTestEmail()
//         {
//             string toEmail = "recipient-email@example.com";
//             string subject = "Test Email";
//             string body = "This is a test email sent from ASP.NET using Gmail SMTP.";

//             await _emailService.SendEmailAsync(toEmail, subject, body);
//             return View();
//         }
//     }
// }
