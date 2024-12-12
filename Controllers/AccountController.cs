using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InquiryManagementApp.Models;
using System;
using Microsoft.EntityFrameworkCore;

namespace InquiryManagementApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Admin()
        {
            return View();
        }

        // [HttpPost]
        // public IActionResult Admin(string username, string password)
        // {
        //     if (username == "admin" && password == "admin")
        //     {
        //         return RedirectToAction("Index", "Admin");
        //     }

        //     ViewBag.ErrorMessage = "Invalid username or password.";
        //     return RedirectToAction("Index", "Admin");
        // }

        [HttpPost]
        public async Task<IActionResult> Admin(string username, string password, string lrn, string surname)
        {
            if (username == "admin" && password == "admin")
            {
                HttpContext.Session.SetString("isAdmin", "1");
                return RedirectToAction("Index", "Admin");
            }
            var enrollment = await _context.Students
                .FirstOrDefaultAsync(s => s.LRN == lrn);

            if (enrollment != null && enrollment.Username == username && enrollment.Password == password)
            {
                HttpContext.Session.SetString("isAdmin", "0");
                HttpContext.Session.SetString("TempUsername", enrollment.TemporaryUsername);
                HttpContext.Session.SetString("TempPassword", enrollment.TemporaryPassword);

                HttpContext.Session.SetInt32("EnrollmentId", enrollment.EnrollmentId);
                HttpContext.Session.SetString("Surname", enrollment.Surname);
                HttpContext.Session.SetString("Firstname", enrollment.Firstname);
                HttpContext.Session.SetString("Middlename", enrollment.Middlename);
                HttpContext.Session.SetString("Gender", enrollment.Gender);
                HttpContext.Session.SetString("GradeLevel", enrollment.GradeLevel);
                HttpContext.Session.SetString("Address", enrollment.Address);
                HttpContext.Session.SetString("LRN", enrollment.LRN);

                return RedirectToAction("EnrollSuccess", "Account");
            }
            ViewBag.ErrorMessage = "Invalid username or password.";
            return RedirectToAction("Index", "Admin");
        }

        // [HttpPost]
        // public IActionResult Login(string lrn, string surname, string firstname, string password)
        // {
        //     var enrollment = new Enrollment
        //     {
        //         LRN = lrn,
        //         Surname = surname,
        //         Firstname = firstname
        //     };

        //     if (ValidateUser(enrollment, password))
        //     {
        //         HttpContext.Session.SetString("Username", enrollment.Username);
        //         HttpContext.Session.SetString("Firstname", enrollment.Firstname);
        //         HttpContext.Session.SetString("Surname", enrollment.Surname);
        //         HttpContext.Session.SetString("LRN", enrollment.LRN);
        //         return RedirectToAction("Home", "Dashboard");
        //     }

        //     TempData["Error"] = "Invalid username or password.";
        //     return View();
        // }

        // private bool ValidateUser(Enrollment enrollment, string enteredPassword)
        // {
        //     string expectedPassword = enrollment.Password;
        //     return enteredPassword == expectedPassword;
        // }

        // public IActionResult Logout()
        // {
        //     HttpContext.Session.Clear();
        //     return RedirectToAction("Login");
        // }
    }
}
