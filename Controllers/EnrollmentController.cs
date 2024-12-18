using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using InquiryManagementApp.Models;
using Microsoft.Extensions.Hosting;
using InquiryManagementApp.Service;

namespace InquiryManagementApp.Controllers
{
    public class EnrollmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FileUploadService _fileUploadService;
        private readonly EmailService _emailService;

        public EnrollmentController(ApplicationDbContext context, FileUploadService fileUploadService, EmailService emailService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _emailService = emailService;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Surname, Firstname, Middlename, Gender, GradeLevel, Email, DateOfBirth, Age, Address, LRN, FatherLastName, FatherFirstName, FatherOccupation, FatherMaidenName, MotherLastName, MotherFirstName, MotherOccupation, MotherMaidenName")]
            Enrollment enrollment,
            List<IFormFile> uploadedFiles)
        {
            if (ModelState.IsValid)
            {
                enrollment.SetTemporaryCredentials();

                foreach (var file in uploadedFiles)
                {
                    if (file != null && file.Length > 0)
                    {
                        Console.WriteLine($"Uploading file: {file.FileName}, Length: {file.Length}");
                        try
                        {
                            var fileUrl = await _fileUploadService.UploadFileToCloudinaryAsync(file);
                            Console.WriteLine($"File uploaded to: {fileUrl}");
                            enrollment.UploadedFiles.Add(fileUrl);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error uploading file {file.FileName}: {ex.Message}");
                        }
                    }
                }
                _context.Add(enrollment);
                await _context.SaveChangesAsync();

                Console.WriteLine(enrollment.TemporaryUsername);
                Console.WriteLine(enrollment.TemporaryPassword);

                string subject = "Enrollment Confirmation";
                string body = $@"
                    <p>Dear {enrollment.Firstname} {enrollment.Surname},</p>
                    <p>Your enrollment has been successfully created. Below are your temporary credentials:</p>
                    <p><strong>Username:</strong> {enrollment.TemporaryUsername}</p>
                    <p><strong>Password:</strong> {enrollment.TemporaryPassword}</p>
                    <p>Thank you for enrolling in our system.</p>
                    <p>Best regards,<br>Enrollment Team</p>
                ";

                try
                {
                    await _emailService.SendEmailAsync(enrollment.Email, subject, body);
                    Console.WriteLine("Email sent successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error sending email: " + ex.Message);
                }

                HttpContext.Session.SetString("ToastMessage", "Enrollment created successfully!");
                HttpContext.Session.SetString("isAdmin", "0");

                return RedirectToAction("Index", "Home");
            }

            return View(enrollment);
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
