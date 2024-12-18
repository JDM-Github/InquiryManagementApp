using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using InquiryManagementApp.Models;
using System.Text.Json;
using InquiryManagementApp.Service;

namespace InquiryManagementApp.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly FileUploadService _fileUploadService;
    public HomeController(ApplicationDbContext context, FileUploadService fileUploadService)
    {
        _context = context;
        _fileUploadService = fileUploadService;
    }

    public IActionResult Index()
    {
        var userId = HttpContext.Session.GetString("LRN");
        var notifications = _context.Notifications
                                    .Where(n => n.UserId == userId)
                                    .OrderByDescending(n => n.CreatedAt)
                                    .ToList();

        var notificationsJson = JsonSerializer.Serialize(notifications);
        HttpContext.Session.SetString("Notifications", notificationsJson);
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

    public IActionResult Document()
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
            return RedirectToAction("Error", "Home");
        }

        var uploadedDocuments = enrollment.UploadedFiles;
        ViewBag.UploadedDocuments = uploadedDocuments;
        return View(enrollment);
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

    public IActionResult Account()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }


    [HttpPost]
    public async Task<IActionResult> UploadDocument(List<IFormFile> files)
    {
        var userId = HttpContext.Session.GetString("LRN");
        var enrollment = _context.Students.FirstOrDefault(e => e.LRN == userId);

        if (enrollment == null)
        {
            return NotFound("Enrollment record not found.");
        }
        foreach (var file in files)
        {
            var fileUrl = await _fileUploadService.UploadFileToCloudinaryAsync(file);
            enrollment.UploadedFiles.Add(fileUrl);
        }
        _context.SaveChanges();
        return RedirectToAction("Document");
    }

    [HttpPost]
    public IActionResult DeleteDocument(string filename)
    {
        var userId = HttpContext.Session.GetString("LRN");
        var enrollment = _context.Students.FirstOrDefault(e => e.LRN == userId);

        if (enrollment == null || !enrollment.UploadedFiles.Contains(filename))
        {
            return BadRequest("File not found.");
        }

        var filePath = Path.Combine("wwwroot/uploads", filename);
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        enrollment.UploadedFiles.Remove(filename);
        _context.SaveChanges();

        return Ok();
    }




}
