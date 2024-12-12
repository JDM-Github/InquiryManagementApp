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

    // GET: EnrollmentSchedule/Create
    public IActionResult Create()
    {
        if (EnrollmentSchedule.InstanceExists)
        {
            return View("Error", new ErrorViewModel { RequestId = "An instance of Enrollment Schedule already exists." });
        }
        return View();
    }

    // POST: EnrollmentSchedule/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("StartDate,EndDate")] EnrollmentSchedule enrollmentSchedule)
    {
        if (ModelState.IsValid)
        {
            try
            {
                EnrollmentSchedule.CreateInstance(enrollmentSchedule.StartDate, enrollmentSchedule.EndDate);
                _context.Add(enrollmentSchedule);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException)
            {
                return View("Error", new ErrorViewModel { RequestId = "Only one instance of Enrollment Schedule is allowed." });
            }
        }
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
