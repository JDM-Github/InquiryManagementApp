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
            return View(new Inquiry());
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
        public async Task<IActionResult> Create(Inquiry inquiry)
        {
            if (ModelState.IsValid)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(inquiry.ContactNumber, @"^[0-9]\d{1,14}$"))
                {
                    TempData["ErrorMessage"] = "Invalid contact number format.";
                    return View(inquiry);
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(inquiry.EmailAddress, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                {
                    TempData["ErrorMessage"] = "Invalid email address format.";
                    return View(inquiry);
                }

                var admins = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == inquiry.EmailAddress);
                if (admins != null)
                {
                    TempData["ErrorMessage"] = "Email already exists.";
                    return View(inquiry);
                }
                if (await _context.Inquiries.FirstOrDefaultAsync(a => a.ContactNumber == inquiry.ContactNumber) != null)
                {
                    TempData["ErrorMessage"] = "Contact number is already being used.";
                    return View(inquiry);
                }

                if (await _context.Inquiries.FirstOrDefaultAsync(a => a.EmailAddress == inquiry.EmailAddress) != null
                 || await _context.Students.FirstOrDefaultAsync(a => a.Email == inquiry.EmailAddress) != null
                 || await _context.Accounts.FirstOrDefaultAsync(a => a.Email == inquiry.EmailAddress) != null)
                {
                    TempData["ErrorMessage"] = "Email is already being used.";
                    return View(inquiry);
                }

                if (inquiry.DateOfBirth.AddYears(4) > DateTime.Now)
                {
                    TempData["ErrorMessage"] = "Student must be at least 4 years old.";
                    return View(inquiry);
                }
                if (inquiry.DateOfBirth.AddYears(80) <= DateTime.Now)
                {
                    TempData["ErrorMessage"] = "Invalid year.";
                    return View(inquiry);
                }

                inquiry.StudentName = inquiry.Firstname + " " + (inquiry.Middlename?.First() + "." ?? "") + " " + inquiry.Surname;
                inquiry.DateCreated = DateTime.Now;
                _context.Add(inquiry);
                await _context.SaveChangesAsync();

                string subject = "Inquiry Confirmation";
                string confirmationLink = Url.Action("Create", "Enrollment", new { id = inquiry.InquiryId }, Request.Scheme) ?? "";
                // string body = $@"
                //     <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                //         <div style='background-color: #f7f7f7; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                //             <h2 style='color: #0056b3; text-align: center;'>Inquiry Confirmation</h2>
                //             <p>Dear <strong>{inquiry.StudentName}</strong>,</p>
                //             <p>Thank you for reaching out to us! Your inquiry has been successfully recorded. We are excited to assist you and will get back to you shortly.</p>
                //             <p>If you’re ready to take the next step, you can enroll by clicking the button below:</p>
                //             <div style='text-align: center; margin: 20px 0;'>
                //                 <a href='{confirmationLink}' 
                //                 style='display: inline-block; padding: 12px 24px; background-color: #0056b3; color: #fff; text-decoration: none; font-size: 16px; border-radius: 5px;'>
                //                     Enroll Now
                //                 </a>
                //             </div>
                //             <p>If you have any questions or need assistance, feel free to reply to this email or contact us directly.</p>
                //             <p>Best regards,<br>
                //             <strong>De Roman Montessori School</strong><br>
                //             <em>Your gateway to excellence in education</em></p>
                //         </div>
                //         <footer style='text-align: center; margin-top: 20px; font-size: 12px; color: #666;'>
                //             <p>De Roman Montessori School | [Your Address] | [Contact Info]</p>
                //         </footer>
                //     </div>";

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
                                        <p style='font-size: 14px;'>
                                            Thank you for reaching out to us! Your inquiry has been successfully recorded. We are excited to assist you and will get back to you shortly.
                                        </p>
                                        <p style='font-size: 14px;'>
                                            If you’re ready to take the next step, you can enroll by clicking the button below:
                                        </p>
                                        <div style='text-align: center; margin: 20px 0;'>
                                            <a href='{confirmationLink}' 
                                                style='display: inline-block; padding: 12px 24px; background-color: #0056b3; color: #fff; text-decoration: none; font-size: 16px; border-radius: 5px;'>
                                                Enroll Now
                                            </a>
                                        </div>
                                        <p style='font-size: 14px;'>If you have any questions or need assistance, feel free to reply to this email or contact us directly:</p>
                                        <ul style='font-size: 14px; color: #333;'>
                                            <li><strong>Phone:</strong> +123-456-7890</li>
                                            <li><strong>Email:</strong> <a href='mailto:contact@dromanmontessori.edu' style='color: #0056b3;'>contact@dromanmontessori.edu</a></li>
                                            <li><strong>Website:</strong> <a href='https://www.dromanmontessori.edu' style='color: #0056b3;'>www.dromanmontessori.edu</a></li>
                                        </ul>
                                        <p style='font-size: 14px;'>Thank you for choosing De Roman Montessori School. We look forward to assisting you!</p>
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

                TempData["SuccessMessage"] = "Inquire successful, please check your email for more details";
                return RedirectToAction("Index", "Home");
            }
            TempData["ErrorMessage"] = "Error when Inquiring.";
            return View(inquiry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInquiry(int id)
        {
            var inquiry = await _context.Inquiries.FindAsync(id);

            if (inquiry == null)
            {
                TempData["ErrorMessage"] = "Inquiry not found.";
                return RedirectToAction("Index", "Admin");
            }

            _context.Inquiries.Remove(inquiry);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Inquiry deleted.";
            return RedirectToAction("ManageInquiries", "Admin");
        }

        [HttpPost]
        public IActionResult EditInquiry(Inquiry inquiry)
        {

            var existingInquiry = _context.Inquiries.Find(inquiry.InquiryId);
            if (existingInquiry != null)
            {
                existingInquiry.StudentName = inquiry.StudentName;
                existingInquiry.GuardianName = inquiry.GuardianName;
                existingInquiry.ContactNumber = inquiry.ContactNumber;
                existingInquiry.SourceOfInformation = inquiry.SourceOfInformation;
                existingInquiry.Notes = inquiry.Notes;

                _context.SaveChanges();
            }
            TempData["SuccessMessage"] = "Inquiry updated successfully.";
            return RedirectToAction("ManageInquiries", "Admin");
        }

    }


}
