using Microsoft.AspNetCore.Mvc;
using InquiryManagementApp.Models;

namespace InquiryManagementApp.Controllers;
public class EnrollmentScheduleController : Controller
{
    private readonly ApplicationDbContext _context;

    public EnrollmentScheduleController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Create()
    {
        if (EnrollmentSchedule.InstanceExists)
        {
            return View("Error", new ErrorViewModel { RequestId = "An instance of Enrollment Schedule already exists." });
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("StartDate,EndDate")] EnrollmentSchedule enrollmentSchedule)
    {
        if (ModelState.IsValid)
        {
            if (EnrollmentSchedule.InstanceExists)
            {
                var schedule = _context.EnrollmentSchedules.FirstOrDefault();
                _context.EnrollmentSchedules.Remove(schedule!);
                _context.SaveChanges();
            }
            EnrollmentSchedule.CreateInstance();
            _context.EnrollmentSchedules.Add(enrollmentSchedule);
            await _context.SaveChangesAsync();

            var recent = new RecentActivity
            {
                Activity = $"Enrollment schedule created. Start date: {enrollmentSchedule.StartDate}, End date: {enrollmentSchedule.EndDate}",
                CreatedAt = DateTime.Now
            };
            _context.RecentActivities.Add(recent);

            TempData["SuccessMessage"] = "Enrollment schedule created successfully."; 
            return RedirectToAction(nameof(Index));
        }
        TempData["ErrorMessage"] = "Error creating enrollment schedule.";
        return View(enrollmentSchedule);
    }

    public bool IsEnrollmentActive()
    {
        var schedule = _context.EnrollmentSchedules.FirstOrDefault();
        if (schedule == null)
        {
            return false;
        }
        return DateTime.Now >= schedule.StartDate && DateTime.Now <= schedule.EndDate;
    }
}
