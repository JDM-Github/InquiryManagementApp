using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using InquiryManagementApp.Models;
using Microsoft.Extensions.Hosting;
using InquiryManagementApp.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace InquiryManagementApp.Controllers
{
    public class EnrollmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FileUploadService _fileUploadService;
        private readonly EmailService _emailService;
        private readonly EnrollmentScheduleService _enrollmentScheduleService;


        public EnrollmentController(ApplicationDbContext context, FileUploadService fileUploadService, EmailService emailService, EnrollmentScheduleService enrollmentScheduleService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _emailService = emailService;
            _enrollmentScheduleService = enrollmentScheduleService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> CreateClick(int id)
        {
            var inquiry = await _context.Inquiries.FirstOrDefaultAsync(i => i.InquiryId == id);
            if (inquiry != null)
            {
                inquiry.IsClickedOnEmail = true;
                _context.Inquiries.Update(inquiry);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Create", "Enrollment", new { id });
        }

        public async Task<IActionResult> Create(int? id = null)
        {
            if (!_enrollmentScheduleService.IsEnrollmentOpen())
            {
                TempData["ErrorMessage"] = "Enrollment is not open.";
                return RedirectToAction("Index", "Home");
            }

            var fee = await _context.Fees.FirstOrDefaultAsync();
            if (fee == null)
            {
                TempData["ErrorMessage"] = "Fee not found.";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.TuitionFee = fee.TuitionFee;
            ViewBag.Miscellaneous = fee.Miscellaneous;

            if (id != null)
            {
                var inquiry = await _context.Inquiries.FirstOrDefaultAsync(i => i.InquiryId == id);
                if (inquiry != null)
                {
                    var enrollment = new Enrollment();
                    enrollment.Firstname = inquiry.Firstname;
                    enrollment.Middlename = inquiry.Middlename;
                    enrollment.Surname = inquiry.Surname;
                    enrollment.GradeLevel = inquiry.GradeLevel;
                    enrollment.Gender = inquiry.Gender;
                    enrollment.DateOfBirth = inquiry.DateOfBirth;
                    enrollment.Email = inquiry.EmailAddress;
                    return View(enrollment);
                }
            }
            return View(new Enrollment());
        }

        public async Task<ActionResult> StudentInformation(int studentId)
        {
            var student = await _context.Students
                .Where(s => s.EnrollmentId == studentId)
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
                    Address = s.Address
                })
                .FirstOrDefaultAsync();
            
            if (student == null)
            {
                TempData["ErrorMessage"] = "Student not found.";
                return RedirectToAction("Walkin", "Enrollment");
            }
            return View(student);
        }

        public async Task<IActionResult> Walkin(int? id = null)
        {
            var fee = await _context.Fees.FirstOrDefaultAsync();
            if (fee == null)
            {
                TempData["ErrorMessage"] = "Fee not found.";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.TuitionFee = fee.TuitionFee;
            ViewBag.Miscellaneous = fee.Miscellaneous;
            return View(new Enrollment());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Walkin(Enrollment enrollment)
        {
            var fee = await _context.Fees.FirstOrDefaultAsync();
            if (fee == null)
            {
                TempData["ErrorMessage"] = "Fee not found.";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.TuitionFee = fee.TuitionFee;
            ViewBag.Miscellaneous = fee.Miscellaneous;
            if (enrollment.Surname == null
             || enrollment.Firstname == null
             || enrollment.GradeLevel == null
             || enrollment.Gender == null
             || enrollment.Email == null
             || enrollment.Address == null
             || enrollment.FatherFirstName == null
             || enrollment.FatherLastName == null
             || enrollment.FatherOccupation == null
             || enrollment.MotherFirstName == null
             || enrollment.MotherLastName == null
             || enrollment.MotherOccupation == null
             || enrollment.MotherMaidenName == null)
            {
                TempData["ErrorMessage"] = "Please fill all required field.";
                return View(enrollment);
            }

            if (await _context.Accounts.FirstOrDefaultAsync(s => s.Email == enrollment.Email) != null
            || await _context.Students.FirstOrDefaultAsync(s => s.Email == enrollment.Email) != null)
            {
                TempData["ErrorMessage"] = "Email already exists.";
                return View(enrollment);
            }

            if (await _context.Students.FirstOrDefaultAsync(s => s.LRN == enrollment.LRN) != null)
            {
                TempData["ErrorMessage"] = "This LRN is already being used.";
                return View(enrollment);
            }
            if (enrollment.DateOfBirth.AddYears(4) > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Student must be at least 4 years old.";
                return View(enrollment);
            }
            if (enrollment.DateOfBirth.AddYears(80) <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Invalid year.";
                return View(enrollment);
            }


            var requiredFiles = await _context.Requirements
                .Where(r => r.GradeLevel == enrollment.GradeLevel)
                .ToListAsync();

            string studentNumber = enrollment.EnrollmentId.ToString("D4");
            string schoolYear = DateTime.Now.Year.ToString();
            if (enrollment.LRN == null)
            {
                enrollment.LRN = "";
            }
            var enrollmentNo = await _context.Students.CountAsync();
            double discount = 0;

            if (_enrollmentScheduleService.IsEnrollmentOpen())
            {
                discount += 0.1;
            }
            if (enrollment.NumberOfSibling > 0)
            {
                discount += Math.Max(5, enrollment.NumberOfSibling) / 10;
            }

            if (enrollment.PaymentType == "Cash")
            {
                discount += 0.1;
                enrollment.PayPerDate = 0;
                enrollment.TotalToPay = fee.TuitionFee * (1 - discount) + fee.Miscellaneous;
            }
            else if (enrollment.PaymentType == "Quarterly")
            {
                double newTuition = fee.TuitionFee * (1 - discount);
                enrollment.PayPerDate = newTuition / 4;
                enrollment.TotalToPay = newTuition + fee.Miscellaneous;
            }
            else if (enrollment.PaymentType == "Monthly")
            {
                double newTuition = fee.TuitionFee * (1 - discount);
                enrollment.PayPerDate = newTuition / 10;
                enrollment.TotalToPay = newTuition + fee.Miscellaneous;
            }
            else
            {
                enrollment.PayPerDate = (fee.TuitionFee + fee.Miscellaneous - 5000) / 10;
                enrollment.TotalToPay = fee.TuitionFee + fee.Miscellaneous;
            }
            enrollment.TemporaryUsername = $"temp{enrollment.Firstname}{enrollmentNo}{schoolYear}{enrollment.Surname}";
            enrollment.TemporaryPassword = GenerateSecurePassword();
            _context.Add(enrollment);
            await _context.SaveChangesAsync();

            // --------------------------------------------------------------
            // AUTO APPROVE
            var approvedId = Guid.NewGuid().ToString();
            enrollment.ApproveId = approvedId;
            enrollment.ApprovedEnrolled = DateTime.Now;
            enrollment.IsApproved = true;
            var enrolledNo = await _context.Students.CountAsync(e => e.IsEnrolled);
            enrollment.StudentID = $"{schoolYear}-{enrolledNo:D4}";
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

            var paymentLink = $"{Request.Scheme}://{Request.Host}/Home/ApprovedEnrolled?id={approvedId}";
            string subject = "Your Enrollment Has Been Approved!";
            string body = $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f9ff; padding: 20px;'>
                    <table style='width: 100%; max-width: 600px; margin: auto; background-color: #fff; border: 1px solid #d9e6f2; border-radius: 8px;'>
                        <thead style='background-color: #0056b3; color: #fff;'>
                            <tr>
                                <th style='padding: 15px; text-align: center;'>
                                    <img src='https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTO9a84kDZORy-tOxHr1uSsYZM4hubrh6AThQ&s' alt='School Logo' style='height: 50px; margin-bottom: 10px;'>
                                    <h2 style='margin: 0; font-size: 24px;'>DE ROMAN MONTESSORI SCHOOL</h2>
                                </th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td style='padding: 20px;'>
                                    <p style='font-size: 16px; color: #0056b3;'>Dear <strong>{enrollment.Firstname} {enrollment.Surname}</strong>,</p>
                                    <p style='font-size: 14px;'>Congratulations! Your enrollment has been approved.</p>
                                    <p style='font-size: 14px;'>Here are your account details:</p>
                                        <p><strong>Temporary Username:</strong> {enrollment.TemporaryUsername}</p>
                                        <p><strong>Temporay Password:</strong> {enrollment.TemporaryPassword}</p>
                                        <br>
                                        <p style='font-size: 14px;'>To get the permanent account details. Complete your payment first.</p>
                                    <p style='font-size: 14px;'>To complete your enrollment, please make your payment by clicking on the link below:</p>
                                    <p style='font-size: 14px;'>
                                        <a href='{paymentLink}' style='color: #ffffff; background-color: #007bff; padding: 10px 15px; text-decoration: none; border-radius: 5px;'>Proceed To Payment</a>
                                    </p>
                                    <p style='font-size: 14px;'>If you prefer, you may also visit us in person to make the payment.</p>
                                    <p style='font-size: 14px;'>Thank you for choosing De Roman Montessori School. We look forward to having you with us!</p>
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

            await _emailService.SendEmailAsync(enrollment.Email, subject, body);

            var notification = new Notification
            {
                Message = $"Your enrollment has been approved.",
                UserId = enrollment.EnrollmentId,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var recent = new RecentActivity
            {
                Activity = $"Enrollment for {enrollment.Firstname} {enrollment.Surname} has been approved.",
                CreatedAt = DateTime.Now
            };
            _context.RecentActivities.Add(recent);
            await _context.SaveChangesAsync();

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

            HttpContext.Session.SetString("isAdmin", "0");
            TempData["SuccessMessage"] = "Enrollment created successfully.";
            return RedirectToAction("StudentInformation", "Enrollment", new { studentId = enrollment.EnrollmentId});
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Enrollment enrollment)
        {
            var fee = await _context.Fees.FirstOrDefaultAsync();
            if (fee == null)
            {
                TempData["ErrorMessage"] = "Fee not found.";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.TuitionFee = fee.TuitionFee;
            ViewBag.Miscellaneous = fee.Miscellaneous;
            if (enrollment.Surname == null
             || enrollment.Firstname == null
             || enrollment.GradeLevel == null
             || enrollment.Gender == null
             || enrollment.Email == null
             || enrollment.Address == null
             || enrollment.FatherFirstName == null
             || enrollment.FatherLastName == null
             || enrollment.FatherOccupation == null
             || enrollment.MotherFirstName == null
             || enrollment.MotherLastName == null
             || enrollment.MotherOccupation == null
             || enrollment.MotherMaidenName == null)
            {
                TempData["ErrorMessage"] = "Please fill all required field.";
                return View(enrollment);
            }

            var inquired = await _context.Inquiries.FirstOrDefaultAsync(s => s.EmailAddress == enrollment.Email);
            if (inquired == null)
            {
                TempData["ErrorMessage"] = "Email is not inquired.";
                return View(enrollment);
            }

            if (inquired.IsCancelled == true)
            {
                TempData["ErrorMessage"] = "Inquired email is cancelled.";
                return View(enrollment);
            }

            if (!inquired.IsClickedOnEmail)
            {
                TempData["ErrorMessage"] = "Please enroll using the email sent to you.";
                return View(enrollment);
            }

            if (await _context.Accounts.FirstOrDefaultAsync(s => s.Email == enrollment.Email) != null
            || await _context.Students.FirstOrDefaultAsync(s => s.Email == enrollment.Email) != null)
            {
                TempData["ErrorMessage"] = "Email already exists.";
                return View(enrollment);
            }

            if (await _context.Students.FirstOrDefaultAsync(s => s.LRN == enrollment.LRN) != null)
            {
                TempData["ErrorMessage"] = "This LRN is already being used.";
                return View(enrollment);
            }
            if (enrollment.DateOfBirth.AddYears(4) > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Student must be at least 4 years old.";
                return View(enrollment);
            }
            if (enrollment.DateOfBirth.AddYears(80) <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Invalid year.";
                return View(enrollment);
            }


            var requiredFiles = await _context.Requirements
                .Where(r => r.GradeLevel == enrollment.GradeLevel)
                .ToListAsync();

            string studentNumber = enrollment.EnrollmentId.ToString("D4");
            string schoolYear = DateTime.Now.Year.ToString();
            if (enrollment.LRN == null)
            {
                enrollment.LRN = "";
            }
            var enrollmentNo = await _context.Students.CountAsync();
            double discount = 0;

            if (_enrollmentScheduleService.IsEnrollmentOpen())
            {
                discount += 0.1;
            }
            if (enrollment.NumberOfSibling > 0)
            {
                discount += Math.Max(5, enrollment.NumberOfSibling) / 10;
            }

            if (enrollment.PaymentType == "Cash")
            {
                discount += 0.1;
                enrollment.PayPerDate = 0;
                enrollment.TotalToPay = fee.TuitionFee * (1 - discount) + fee.Miscellaneous;
            } else if (enrollment.PaymentType == "Quarterly") {
                double newTuition = fee.TuitionFee * (1 - discount);
                enrollment.PayPerDate = newTuition / 4;
                enrollment.TotalToPay = newTuition + fee.Miscellaneous;
            }
            else if (enrollment.PaymentType == "Monthly")
            {
                double newTuition = fee.TuitionFee * (1 - discount);
                enrollment.PayPerDate = newTuition / 10;
                enrollment.TotalToPay = newTuition + fee.Miscellaneous;
            }
            else
            {
                enrollment.PayPerDate = (fee.TuitionFee + fee.Miscellaneous - 5000) / 10;
                enrollment.TotalToPay = fee.TuitionFee + fee.Miscellaneous;
            }
            var enrolledNo = await _context.Students.CountAsync();
            enrollment.StudentID = $"{schoolYear}-{enrolledNo}";
            var approvedId = Guid.NewGuid().ToString();
            enrollment.ApproveId = approvedId;

            enrollment.TemporaryUsername = $"temp{enrollment.Firstname}{enrollmentNo}{schoolYear}{enrollment.Surname}";
            enrollment.TemporaryPassword = GenerateSecurePassword();


            var paymentLink = $"{Request.Scheme}://{Request.Host}/Home/ApprovedEnrolled?id={approvedId}";
            var subject = "Your Enrollment Has Been Created!";
            var body = $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f9ff; padding: 20px;'>
                    <table style='width: 100%; max-width: 600px; margin: auto; background-color: #fff; border: 1px solid #d9e6f2; border-radius: 8px;'>
                        <thead style='background-color: #0056b3; color: #fff;'>
                            <tr>
                                <th style='padding: 15px; text-align: center;'>
                                    <img src='https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTO9a84kDZORy-tOxHr1uSsYZM4hubrh6AThQ&s' alt='School Logo' style='height: 50px; margin-bottom: 10px;'>
                                    <h2 style='margin: 0; font-size: 24px;'>DE ROMAN MONTESSORI SCHOOL</h2>
                                </th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td style='padding: 20px;'>
                                    <p style='font-size: 16px; color: #0056b3;'>Dear <strong>{enrollment.Firstname} {enrollment.Surname}</strong>,</p>
                                    <p style='font-size: 14px;'>Congratulations! Your enrollment has been successfully created.</p>
                                    <p style='font-size: 14px;'>Here are your account details:</p>
                                        <p><strong>Temporary Username:</strong> {enrollment.TemporaryUsername}</p>
                                        <p><strong>Temporay Password:</strong> {enrollment.TemporaryPassword}</p>
                                        <br>
                                        <p style='font-size: 14px;'>To get the permanent account details. We will need to approve your enrollment first.</p>
                                    <p style='font-size: 14px;'>To complete your enrollment, please make your payment by clicking on the link below:</p>
                                    <p style='font-size: 14px;'>
                                        <a href='{paymentLink}' style='color: #ffffff; background-color: #007bff; padding: 10px 15px; text-decoration: none; border-radius: 5px;'>Complete Payment</a>
                                    </p>
                                    <p style='font-size: 14px;'>If you prefer, you may also visit us in person to make the payment.</p>
                                    <p style='font-size: 14px;'>Thank you for choosing De Roman Montessori School. We look forward to having you with us!</p>
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

            await _emailService.SendEmailAsync(enrollment.Email, subject, body);


            _context.Add(enrollment);
            await _context.SaveChangesAsync();

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
            HttpContext.Session.SetString("isAdmin", "0");
            TempData["SuccessMessage"] = "Enrollment created successfully.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Approve(int id)
        {
            return RedirectToAction(nameof(Index));
        }
    }
}
