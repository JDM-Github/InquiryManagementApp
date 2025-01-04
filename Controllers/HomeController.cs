using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using InquiryManagementApp.Models;
using System.Text.Json;
using InquiryManagementApp.Service;
using Microsoft.EntityFrameworkCore;

namespace InquiryManagementApp.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly FileUploadService _fileUploadService;
    private readonly FileDownloadService _fileDownloadService;

    public HomeController(ApplicationDbContext context, FileUploadService fileUploadService, FileDownloadService fileDownloadService)
    {
        _context = context;
        _fileUploadService = fileUploadService;
        _fileDownloadService = fileDownloadService;
    }

    public IActionResult Index()
    {
        var existingSchedule = _context.PaymentSchedules.FirstOrDefault();
        if (existingSchedule != null)
        {
            PaymentSchedule.CreateInstance(existingSchedule);
        }

        var userId = HttpContext.Session.GetString("LRN");
        // var notifications = _context.Notifications
        //                             .Where(n => n.UserId == userId)
        //                             .OrderByDescending(n => n.CreatedAt)
        //                             .ToList();

        // var notificationsJson = JsonSerializer.Serialize(notifications);
        // HttpContext.Session.SetString("Notifications", notificationsJson);
        if (HttpContext.Session.GetString("isAdmin") == "1")
        {
            return RedirectToAction("Index", "Admin");
        }
        else if (HttpContext.Session.GetString("isAdmin") == "2")
        {
            return RedirectToAction("Account", "Home");
        }
        return View();
    }

    public async Task<IActionResult> Document()
    {
        var userId = HttpContext.Session.GetString("LRN");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var enrollment = _context.Students
            .Where(e => e.LRN == userId)
            .FirstOrDefault();

        if (enrollment == null)
        {
            return NotFound();
        }

        var requiredFiles = await _context.RequirementModels
            .Where(c => c.EnrollmentId == enrollment.EnrollmentId)
            .ToListAsync();

        var model = new EnrollmentRequirementsViewModel
        {
            Enrollment = enrollment,
            Requirements = requiredFiles
        };
        return View(model);
    }

    public IActionResult Payment()
    {
        var userId = HttpContext.Session.GetString("LRN");
        var enrollment = _context.Students.FirstOrDefault(e => e.LRN == userId);

        if (enrollment == null)
        {
            return NotFound("Enrollment record not found.");
        }
        return View(enrollment);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public async Task<IActionResult> Account()
    {
        var userId = HttpContext.Session.GetString("LRN");
        var account = await _context.Students.FirstOrDefaultAsync(c => c.LRN == userId);
        if (account == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction("Login", "Account");
        }

        return View(account);
    }

    public async Task<IActionResult> ViewAccount(string userId)
    {
        var account = await _context.Students.FirstOrDefaultAsync(c => c.LRN == userId);
        if (account == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction("Login", "Account");
        }
        return View(account);
    }

    public async Task<IActionResult> Notification()
    {
        var userId = HttpContext.Session.GetString("LRN");
        var notifications = await _context.Notifications
            .Where(c => c.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return View(notifications);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }


    [HttpPost]
    public async Task<IActionResult> UploadDocument(int id, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("File", "Please select a file to upload.");
            return RedirectToAction("Index"); 
        }

        var requirement = await _context.RequirementModels.FindAsync(id);
        if (requirement == null)
        {
            TempData["ErrorMessage"] = $"Requirement with ID {id} not found.";
            return RedirectToAction("Index");
        }

        var fileUrl = await _fileUploadService.UploadFileToCloudinaryAsync(file);
        requirement.UploadedFile = fileUrl;
        _context.Update(requirement);
        await _context.SaveChangesAsync();

        Console.WriteLine(fileUrl);

        TempData["SuccessMessage"] = "File uploaded successfully.";
        return RedirectToAction("Document");
    }

    public async Task<IActionResult> ViewDocument(int id)
    {
        var requirement = await _context.RequirementModels.FindAsync(id);
        if (requirement == null || string.IsNullOrEmpty(requirement.UploadedFile))
        {
            TempData["ErrorMessage"] = "The file does not exist.";
            return RedirectToAction("Document");
        }
        return Redirect(requirement.UploadedFile);
    }

    public async Task<IActionResult> DownloadDocument(int id)
    {
        var requirement = await _context.RequirementModels.FindAsync(id);
        if (requirement == null || string.IsNullOrEmpty(requirement.UploadedFile))
        {
            TempData["ErrorMessage"] = "The file does not exist.";
            return RedirectToAction("Document");
        }

        var fileBytes = await _fileDownloadService.DownloadFileFromUrlAsync(requirement.UploadedFile);
        return File(fileBytes, "application/octet-stream", Path.GetFileName(requirement.UploadedFile));
    }

    public async Task<IActionResult> DeleteDocument(int id)
    {
        var requirement = await _context.RequirementModels.FindAsync(id);
        if (requirement == null || string.IsNullOrEmpty(requirement.UploadedFile))
        {
            TempData["ErrorMessage"] = "The file does not exist.";
            return RedirectToAction("Document");
        }
        requirement.UploadedFile = "";
        _context.RequirementModels.Update(requirement);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "File deleted successfully.";
        return RedirectToAction("Document");
    }




}
