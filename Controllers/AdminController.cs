using InquiryManagementApp.Models;
using Microsoft.AspNetCore.Mvc;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var viewModel = new AdminDashboardViewModel
        {
            TotalInquiries = _context.Inquiries.Count(),
            TotalEnrolled = _context.Students.Count(),
            TotalApproved = _context.Students.Where(e => e.IsApproved).Count(),
            TotalRevenue = _context.Students.Sum(e => e.FeePaid)
        };
        return View(viewModel);
    }

    public IActionResult Logout()
    {
        HttpContext.Session.SetString("isAdmin", "0");
        return RedirectToAction("Index", "Home");
    }

    public IActionResult ManageEnrolled()
    {
        var enrollments = _context.Students.ToList();
        return View(enrollments);
    }

    public IActionResult ManageEnrollees()
    {
        var enrollments = _context.Students.ToList();
        return View(enrollments);
    }

    public IActionResult ManageInquiries()
    {
        var inquiries = _context.Inquiries.ToList();
        return View(inquiries);
    }

    public IActionResult ManageAccounts()
    {
        var accounts = _context.Accounts;
        if (accounts == null)
        {
            return View(new List<Account>());
        }
        else
        {
            return View(_context.Accounts.ToList());
        }
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Account account, string confirmPassword)
    {
        if (account.Password != confirmPassword)
        {
            ViewBag.ErrorMessage = "Passwords do not match.";
            return View();
        }

        if (ModelState.IsValid)
        {
            _context.Accounts.Add(account);
            _context.SaveChanges();

            return RedirectToAction("ManageAccounts");
        }
        return View(account);
    }



    [HttpPost]
    public async Task<IActionResult> SetEnrollmentSchedule(DateTime startDate, DateTime endDate)
    {
        try
        {
            var existingSchedule = _context.EnrollmentSchedules.FirstOrDefault();

            if (existingSchedule != null)
            {
                existingSchedule.StartDate = startDate;
                existingSchedule.EndDate = endDate;
                EnrollmentSchedule.CreateInstance(startDate, endDate);
                _context.EnrollmentSchedules.Update(existingSchedule);
            }
            else
            {
                var enrollmentSchedule = new EnrollmentSchedule
                {
                    StartDate = startDate,
                    EndDate = endDate
                };

                EnrollmentSchedule.CreateInstance(startDate, endDate);
                _context.EnrollmentSchedules.Add(enrollmentSchedule);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Admin");
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = $"Error: {ex.Message}";
            return RedirectToAction("Index", "Admin");
        }
    }

    [HttpPost]
    public async Task<IActionResult> ClearEnrollment()
    {
        try
        {
            var existingSchedule = _context.EnrollmentSchedules.FirstOrDefault();

            if (existingSchedule != null)
            {
                _context.EnrollmentSchedules.Remove(existingSchedule);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Admin");
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = $"Error: {ex.Message}";
            return RedirectToAction("Index", "Admin");
        }
    }

    [HttpPost]
    public IActionResult Approve(int id)
    {
        var enrollment = _context.Students.FirstOrDefault(e => e.EnrollmentId == id);

        if (enrollment == null)
        {
            return NotFound();
        }

        if (!enrollment.IsApproved)
        {
            enrollment.IsApproved = true;
            // var account = new Account
            // {
            //     Username = enrollment.Username,
            //     Password = enrollment.Password,
            //     IsStudent = true,
            //     EnrollmentId = enrollment.EnrollmentId
            // };
            // _context.Accounts.Add(account);
            _context.SaveChanges();
        }
        return RedirectToAction("ManageAccounts", "Admin");
    }
}
