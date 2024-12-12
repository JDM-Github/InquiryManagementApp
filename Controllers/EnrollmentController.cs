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

        public EnrollmentController(ApplicationDbContext context, FileUploadService fileUploadService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
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
            [Bind("Surname, Firstname, Middlename, Gender, GradeLevel, DateOfBirth, Age, Address, LRN, FatherLastName, FatherFirstName, FatherOccupation, FatherMaidenName, MotherLastName, MotherFirstName, MotherOccupation, MotherMaidenName")]
            Enrollment enrollment,
            IFormFile Form10,
            IFormFile Form9,
            IFormFile Psa,
            IFormFile GoodMoralCertificate)
        {
            if (ModelState.IsValid)
            {
                if (Form10 != null && Form10.Length > 0)
                {
                    var form10Url = await _fileUploadService.UploadFileToCloudinaryAsync(Form10);
                    enrollment.Form10 = form10Url;
                }
                if (Form9 != null && Form9.Length > 0)
                {
                    var form9Url = await _fileUploadService.UploadFileToCloudinaryAsync(Form9);
                    enrollment.Form9 = form9Url;
                }
                if (Psa != null && Psa.Length > 0)
                {
                    var psaUrl = await _fileUploadService.UploadFileToCloudinaryAsync(Psa);
                    enrollment.PSA = psaUrl;
                }
                if (GoodMoralCertificate != null && GoodMoralCertificate.Length > 0)
                {
                    var goodMoralCertificateUrl = await _fileUploadService.UploadFileToCloudinaryAsync(GoodMoralCertificate);
                    enrollment.GoodMoralCertificate = goodMoralCertificateUrl;
                }

                _context.Add(enrollment);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("ToastMessage", "Enrollment created successfully!");
                HttpContext.Session.SetString("isAdmin", "0");

                return RedirectToAction("Index", "Home");
            }

            return View(enrollment);
        }


        [HttpPost]
        public IActionResult Approve(int id)
        {
            return RedirectToAction(nameof(Index));
            var enrollment = _context.Students.FirstOrDefault(e => e.EnrollmentId == id);

            if (enrollment == null)
            {
                return NotFound();
            }

            if (!enrollment.IsApproved)
            {
                enrollment.IsApproved = true;
                var account = new Account
                {
                    Username = enrollment.Username,
                    Password = enrollment.Password,
                    IsStudent = true,
                    EnrollmentId = enrollment.EnrollmentId
                };

                _context.Accounts.Add(account);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
