using Microsoft.AspNetCore.Mvc;
using InquiryManagementApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InquiryManagementApp.Controllers
{
    public class InquiryController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;


        public InquiryController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: Inquiry
        public async Task<IActionResult> IndexAsync()
        {
            return View(await _context.Inquiries.ToListAsync());
        }

        // GET: Inquiry/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult UpdateReason(int inquiryId, string reason)
        {
            var inquiry = _context.Inquiries.FirstOrDefault(i => i.InquiryId == inquiryId);

            if (inquiry != null)
            {
                inquiry.Reason = reason;
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Reason saved successfully!";
                return RedirectToAction("Details", new { id = inquiryId });
            }
            TempData["ErrorMessage"] = "Unable to find the inquiry.";
            return RedirectToAction("Index", "Admin");
        }

        public IActionResult Details(int id)
        {
            var inquiry = _context.Inquiries.FirstOrDefault(i => i.InquiryId == id);
            if (inquiry == null)
            {
                return NotFound();
            }

            return View(inquiry);
        }

        [HttpGet]
        public async Task<IActionResult> Cancel(int id)
        {
            var student = await _context.Inquiries.FirstOrDefaultAsync(c => c.InquiryId == id);
            if (student != null)
            {
                if (student.IsCancelled)
                {
                    TempData["ErrorMessage"] = "Inquiry already cancelled.";
                    return RedirectToAction("Index", "Home");
                }
            }

            var viewModel = new InquiryCancellationViewModel
            {
                InquiryId = id
            };
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Confirm(int id)
        {
            var student = await _context.Inquiries.FirstOrDefaultAsync(c => c.InquiryId == id);
            if (student != null)
            {
                if (student.IsConfirmed)
                {
                    TempData["ErrorMessage"] = "Inquiry already confirmed.";
                    return RedirectToAction("Index", "Home");
                }
                student.IsConfirmed = true;
                _context.Update(student);

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Inquiry confirmed successfully.";
                return RedirectToAction("Index", "Home");
            }
            
            TempData["ErrorMessage"] = "Inquiry not found.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(InquiryCancellationViewModel model)
        {
            if (ModelState.IsValid)
            {
                var inquiry = await _context.Inquiries.FindAsync(model.InquiryId);
                if (inquiry != null)
                {
                    inquiry.IsCancelled = true;
                    inquiry.CancellationReason = model.CancellationReason;
                    inquiry.CancellationNotes = model.CancellationNotes;
                    await _context.SaveChangesAsync();

                    var recent = new RecentActivity
                    {
                        Activity = $"Inquiry {inquiry.InquiryId} cancelled",
                        CreatedAt = DateTime.Now
                    };
                    _context.RecentActivities.Add(recent);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Inquiry canceled successfully.";
                    return RedirectToAction("Index", "Home");
                }

                TempData["ErrorMessage"] = "Inquiry not found.";
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudentName,GuardianName,ContactNumber,EmailAddress,SourceOfInformation,Notes")] Inquiry inquiry)
        {
            if (ModelState.IsValid)
            {
                var admins = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == inquiry.EmailAddress);
                if (admins != null)
                {
                    TempData["ErrorMessage"] = "Email already exists.";
                    return View(inquiry);
                }
                var anotherInquiry = await _context.Inquiries.FirstOrDefaultAsync(c => c.EmailAddress == inquiry.EmailAddress);
                if (anotherInquiry != null)
                {
                    if (!anotherInquiry.IsCancelled)
                    {
                        TempData["ErrorMessage"] = "Email already inquired.";
                        return View(inquiry);
                    }
                    _context.Inquiries.Remove(anotherInquiry);
                    await _context.SaveChangesAsync();
                }

                inquiry.DateCreated = DateTime.Now;
                _context.Add(inquiry);
                await _context.SaveChangesAsync();

                string subject = "Inquiry Confirmation";
                string cancellationLink = Url.Action("Cancel", "Inquiry", new { id = inquiry.InquiryId }, Request.Scheme) ?? "";
                string confirmationLink = Url.Action("Confirm", "Inquiry", new { id = inquiry.InquiryId }, Request.Scheme) ?? "";
                // string cancellationLink = Url.Action("Cancel", "Inquiry", new { id = inquiry.InquiryId }, Request.Scheme) ?? "";
                string body = $@"
                    <p>Dear {inquiry.StudentName},</p>
                    <p>Thank you for reaching out to us. Your inquiry has been successfully recorded. We will get back to you soon.</p>
                    <p>If you need to cancel your inquiry, you can do so by clicking the link below:</p>
                    <p><a href='{confirmationLink}'>Confirm My Inquiry</a></p>
                    <p><a href='{cancellationLink}'>Cancel My Inquiry</a></p>
                    <p>We appreciate your interest in our services. Please feel free to reply to this email if you have any questions or concerns.</p>
                    <p>Best regards,<br>Your Team</p>
                ";

                try
                {
                    await _emailService.SendEmailAsync(inquiry.EmailAddress, subject, body);
                    Console.WriteLine("Email sent successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error sending email: " + ex.Message);
                }

                var recent = new RecentActivity
                {
                    Activity = $"New Inquiry from {inquiry.StudentName}",
                    CreatedAt = DateTime.Now
                };
                _context.RecentActivities.Add(recent);
                await _context.SaveChangesAsync();


                TempData["SuccessMessage"] = "Successfully Inquired!";
                return RedirectToAction("Index", "Home");
            }
            TempData["ErrorMessage"] = "Error when Inquiring.";
            return View(inquiry);
        }

    }
}
