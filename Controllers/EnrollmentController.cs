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

        public async Task<IActionResult> Create(int? id = null)
        {
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

            // if (id != null)
            // {
            //     var inquiry = await _context.Inquiries.FirstOrDefaultAsync(i => i.InquiryId == id);
            //     if (inquiry != null)
            //     {
            //         var enrollment = new Enrollment();
            //         enrollment.Firstname = inquiry.Firstname;
            //         enrollment.Middlename = inquiry.Middlename;
            //         enrollment.Surname = inquiry.Surname;
            //         enrollment.GradeLevel = inquiry.GradeLevel;
            //         enrollment.Gender = inquiry.Gender;
            //         enrollment.DateOfBirth = inquiry.DateOfBirth;
            //         enrollment.Email = inquiry.EmailAddress;
            //         return View(enrollment);
            //     }
            // }
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

            string subject = "Enrollment Confirmation";
            string body = $@"
                    <p>Dear {enrollment.Firstname} {enrollment.Surname},</p>
                    <p>Your enrollment has been successfully created. Below are your temporary credentials:</p>
                    <p><strong>Temporary Username:</strong> {enrollment.TemporaryUsername}</p>
                    <p><strong>Temporary Password:</strong> {enrollment.TemporaryPassword}</p>
                    <p>Thank you for enrolling in our system. Please complete all necessary information for admin to accept your enrollment</p>
                    <p>Best regards,<br>Enrollment Team</p>
                ";

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
            subject = "Your Enrollment Has Been Approved!";
            body = $@"
                <p>Dear {enrollment.Firstname} {enrollment.Surname},</p>
                <p>Congratulations! Your enrollment has been approved.</p>
                <p>To complete your enrollment, please make your payment by clicking on the link below:</p>
                <p>
                    <a href='{paymentLink}' style='color: #ffffff; background-color: #007bff; padding: 10px 15px; text-decoration: none; border-radius: 5px;'>Proceed To Payment</a>
                </p>
                <p>If you prefer, you may also visit us in person to make the payment.</p>
                <p>Thank you for choosing our institution. We look forward to having you with us!</p>
                <p>Best regards,</p>
                <p><strong>Your Enrollment Team</strong></p>";
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
                Activity = $"Enrollment {enrollment.Firstname} {enrollment.Surname} approved",
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
            try
            {
                await _emailService.SendEmailAsync(enrollment.Email, subject, body);
                Console.WriteLine("Email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending email: " + ex.Message);
            }

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
            var enrolledNo = await _context.Students.CountAsync(e => e.IsEnrolled);
            enrollment.StudentID = $"{schoolYear}-{enrolledNo}";
            var approvedId = Guid.NewGuid().ToString();
            enrollment.ApproveId = approvedId;
            // enrollment.Username = $"temp{enrollment.Firstname}{enrollment.StudentID}{enrollment.Surname}";
            // enrollment.Password = GenerateSecurePassword();

            enrollment.TemporaryUsername = $"temp{enrollment.Firstname}{enrollmentNo}{schoolYear}{enrollment.Surname}";
            enrollment.TemporaryPassword = GenerateSecurePassword();

            string subject = "Enrollment Confirmation";
            string body = $@"
                    <p>Dear {enrollment.Firstname} {enrollment.Surname},</p>
                    <p>Your enrollment has been successfully created. Below are your temporary credentials:</p>
                    <p><strong>Temporary Username:</strong> {enrollment.TemporaryUsername}</p>
                    <p><strong>Temporary Password:</strong> {enrollment.TemporaryPassword}</p>
                    <p>Thank you for enrolling in our system. Please complete all necessary information for admin to accept your enrollment</p>
                    <p>Best regards,<br>Enrollment Team</p>
                ";

            var paymentLink = $"{Request.Scheme}://{Request.Host}/Home/ApprovedEnrolled?id={approvedId}";
            var subject2 = "Your Enrollment Has Been Created!";
            var body2 = $@"
                <p>Dear {enrollment.Firstname} {enrollment.Surname},</p>
                <p>Congratulations! Your enrollment has been created.</p>
                <p>To complete your enrollment, please make your payment by clicking on the link below:</p>
                <p>
                    <a href='{paymentLink}' style='color: #ffffff; background-color: #007bff; padding: 10px 15px; text-decoration: none; border-radius: 5px;'>Complete Payment</a>
                </p>
                <p>If you prefer, you may also visit us in person to make the payment.</p>
                <p>Thank you for choosing our institution. We look forward to having you with us!</p>
                <p>Best regards,</p>
                <p><strong>Your Enrollment Team</strong></p>";
            await _emailService.SendEmailAsync(enrollment.Email, subject2, body2);

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
            try
            {
                await _emailService.SendEmailAsync(enrollment.Email, subject, body);
                Console.WriteLine("Email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending email: " + ex.Message);
            }

            HttpContext.Session.SetString("isAdmin", "0");
            TempData["SuccessMessage"] = "Enrollment created successfully.";
            return RedirectToAction("Index", "Home");
        }


        //     [HttpPost]
        //     [ValidateAntiForgeryToken]
        //     public async Task<IActionResult> Create(
        // [Bind("Surname, Firstname, Middlename, Gender, GradeLevel, DateOfBirth, Age, Address, LRN, FatherLastName, FatherFirstName, FatherOccupation, FatherMaidenName, MotherLastName, MotherFirstName, MotherOccupation, MotherMaidenName")]
        // Enrollment enrollment)
        //     {
        //         try
        //         {
        //             // Debug log to check if ModelState is valid
        //             Console.WriteLine("ModelState is valid: " + ModelState.IsValid);

        //             if (ModelState.IsValid)
        //             {
        //                 // Debug log before processing files
        //                 Console.WriteLine("Processing files...");

        //                 // foreach (var file in uploadedFiles)
        //                 // {
        //                 //     if (file != null && file.Length > 0)
        //                 //     {
        //                 //         // Debug log for each file
        //                 //         Console.WriteLine($"Uploading file: {file.FileName}, Length: {file.Length}");

        //                 //         try
        //                 //         {
        //                 //             var fileUrl = await _fileUploadService.UploadFileToCloudinaryAsync(file);

        //                 //             // Debug log for uploaded file URL
        //                 //             Console.WriteLine($"File uploaded to: {fileUrl}");

        //                 //             enrollment.UploadedFiles.Add(fileUrl);
        //                 //         }
        //                 //         catch (Exception ex)
        //                 //         {
        //                 //             // Debug log for any error during file upload
        //                 //             Console.WriteLine($"Error uploading file {file.FileName}: {ex.Message}");
        //                 //         }
        //                 //     }
        //                 // }

        //                 // Debug log before saving to the database
        //                 Console.WriteLine("Adding enrollment to database...");

        //                 _context.Add(enrollment);
        //                 await _context.SaveChangesAsync();

        //                 // Debug log after saving
        //                 Console.WriteLine("Enrollment saved to database.");

        //                 HttpContext.Session.SetString("ToastMessage", "Enrollment created successfully!" + enrollment.TemporaryUsername + enrollment.TemporaryPassword);
        //                 HttpContext.Session.SetString("isAdmin", "0");

        //                 return RedirectToAction("Index", "Home");
        //             }

        //             // Debug log if ModelState is not valid
        //             Console.WriteLine("ModelState is not valid.");

        //             return View(enrollment);
        //         }
        //         catch (Exception ex)
        //         {
        //             // Debug log for any unhandled errors
        //             Console.WriteLine($"Error in Create method: {ex.Message}");
        //             return View(enrollment);
        //         }
        //     }




        [HttpPost]
        public IActionResult Approve(int id)
        {
            return RedirectToAction(nameof(Index));
            // var enrollment = _context.Students.FirstOrDefault(e => e.EnrollmentId == id);

            // if (enrollment == null)
            // {
            //     return NotFound();
            // }

            // if (!enrollment.IsApproved)
            // {
            //     enrollment.IsApproved = true;
            //     var account = new Account
            //     {
            //         Username = enrollment.Username,
            //         Password = enrollment.Password,
            //         IsStudent = true,
            //         EnrollmentId = enrollment.EnrollmentId
            //     };

            //     _context.Accounts.Add(account);
            //     _context.SaveChanges();
            // }

            // return RedirectToAction(nameof(Index));
        }
    }
}
