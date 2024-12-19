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

        // public IActionResult Detail(int id)
        // {
        //     var account = _context.Accounts.Find(id);
        //     if (account == null)
        //     {
        //         return NotFound();
        //     }

        //     return View(account);
        // }

        // public IActionResult Edit(int id)
        // {
        //     var account = _context.Accounts.Find(id);
        //     if (account == null)
        //     {
        //         return NotFound();
        //     }

        //     return View(account);
        // }


        public IActionResult EnrolleesDetail(int id)
        {
            var account = _context.Students.Find(id);
            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }
        public IActionResult EnrollDetail(int id)
        {
            var account = _context.Students.Find(id);
            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        public IActionResult Delete(int id)
        {
            var account = _context.Accounts.Find(id);
            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var account = _context.Accounts.Find(id);
            if (account == null)
            {
                return NotFound();
            }

            _context.Accounts.Remove(account);
            _context.SaveChanges();
            return RedirectToAction("ManageAccounts", "Admin");
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
        public async Task<IActionResult> Admin(string username, string password)
        {
            if (username == "admin" && password == "admin")
            {
                HttpContext.Session.SetString("isAdmin", "1");
                return RedirectToAction("Index", "Admin");
            }

            var enrollment = await _context.Students
                .Where(s => s.TemporaryUsername == username)
                .FirstOrDefaultAsync();

            if (enrollment == null)
            {
                enrollment = await _context.Students
                    .Where(s => s.Username == username)
                    .FirstOrDefaultAsync();
            }

            if (enrollment != null)
            {
                if (enrollment.TemporaryPassword == password)
                {
                    HttpContext.Session.SetString("isAdmin", "2");
                    SetSessionVariables(enrollment);
                    return RedirectToAction("Account", "Home");
                }
                else
                {
                    if (enrollment.Password == password && enrollment.IsApproved)
                    {
                        HttpContext.Session.SetString("isAdmin", "2");
                        SetSessionVariables(enrollment);
                        return RedirectToAction("Account", "Home");
                    }
                    else
                    {
                        if (enrollment.IsRejected)
                            ViewBag.ErrorMessage = "Your account has been rejected";
                        else
                            ViewBag.ErrorMessage = "Your account is still not approved.";
                    }
                }
            }
            ViewBag.ErrorMessage = "Invalid username or password.";
            return View();
        }

        private void SetSessionVariables(Enrollment enrollment)
        {
            HttpContext.Session.SetString("TempUsername", enrollment.TemporaryUsername);
            HttpContext.Session.SetString("TempPassword", enrollment.TemporaryPassword);
            HttpContext.Session.SetInt32("EnrollmentId", enrollment.EnrollmentId);
            HttpContext.Session.SetString("Surname", enrollment.Surname);
            HttpContext.Session.SetString("Email", enrollment.Email);
            HttpContext.Session.SetString("Firstname", enrollment.Firstname);
            HttpContext.Session.SetString("Middlename", enrollment.Middlename);
            HttpContext.Session.SetString("Gender", enrollment.Gender);
            HttpContext.Session.SetString("GradeLevel", enrollment.GradeLevel);
            HttpContext.Session.SetString("Address", enrollment.Address);
            HttpContext.Session.SetString("LRN", enrollment.LRN);
            HttpContext.Session.SetString("DateOfBirth", enrollment.DateOfBirth.ToString());
            HttpContext.Session.SetString("Mother", enrollment.MotherFirstName + " " + enrollment.MotherLastName);
            HttpContext.Session.SetString("Father", enrollment.FatherFirstName + " " + enrollment.FatherLastName);
            HttpContext.Session.SetString("IsApproved", enrollment.IsApproved ? "Approved" : "Not Approved");

        }


        public IActionResult Detail(int id)
        {
            var account = _context.Accounts.FirstOrDefault(a => a.AccountId == id);
            if (account == null)
            {
                return NotFound();
            }
            return View(account);
        }

        // GET: Account/Edit/5
        public IActionResult Edit(int id)
        {
            var account = _context.Accounts.FirstOrDefault(a => a.AccountId == id);
            if (account == null)
            {
                return NotFound();
            }
            return View(account);
        }

        // POST: Account/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Account account)
        {
            if (id != account.AccountId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(account);
                    _context.SaveChanges();
                }
                catch
                {
                    if (!_context.Accounts.Any(a => a.AccountId == account.AccountId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Detail), new { id = account.AccountId });
            }
            return View(account);
        }

    }
}
