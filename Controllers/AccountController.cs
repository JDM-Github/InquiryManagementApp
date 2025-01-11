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
        private readonly EmailService _emailService;

        public AccountController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
                TempData["SuccessMessage"] = "Successfully login as admin.";
                return RedirectToAction("Index", "Admin");
            }

            var admins = await _context.Accounts.Where(s => s.Email == username || s.Username == username).FirstOrDefaultAsync();
            if (admins != null)
            {
                if (admins.Password == password)
                {
                    if (admins.Role == "Admin")
                    {
                        HttpContext.Session.SetString("isAdmin", "1");
                        TempData["SuccessMessage"] = "Successfully login as admin.";
                        return RedirectToAction("Index", "Admin");
                    }
                    else
                    {
                        HttpContext.Session.SetString("isAdmin", "3");
                        TempData["SuccessMessage"] = "Successfully login as marketing.";
                        return RedirectToAction("Index", "Admin");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid username or password.";
                    return RedirectToAction("Login", "Account");
                }
            }

            var enrollment = await _context.Students
                .Where(s => s.TemporaryUsername == username)
                .FirstOrDefaultAsync();

            var enrollment2 = await _context.Students
                    .Where(s => s.Username == username)
                    .FirstOrDefaultAsync();

            if (enrollment != null)
            {
                if (enrollment.IsDeleted)
                {
                    TempData["ErrorMessage"] = "Your account has been deleted.";
                    return RedirectToAction("Login", "Account");
                }
                if (enrollment.TemporaryPassword == password)
                {
                    HttpContext.Session.SetString("isAdmin", "2");
                    SetSessionVariables(enrollment);
                    TempData["SuccessMessage"] = "Login successful.";
                    return RedirectToAction("Account", "Home");
                }
            } 

            if (enrollment2 != null)
            {
                if (enrollment2.IsDeleted)
                {
                    TempData["ErrorMessage"] = "Your account has been deleted.";
                    return RedirectToAction("Login", "Account");
                }
                if (enrollment2!.Password == password)
                {
                    if (enrollment2.IsEnrolled)
                    {
                        HttpContext.Session.SetString("isAdmin", "2");
                        SetSessionVariables(enrollment2);
                        return RedirectToAction("Account", "Home");
                    }
                    else 
                    {
                        if (enrollment2!.IsRejected)
                            TempData["ErrorMessage"] = "Your account has been rejected";
                        else
                            TempData["ErrorMessage"] = "Your account is still not approved.";

                        return RedirectToAction("Login", "Account");
                    }
                }
            }
            TempData["ErrorMessage"] = "Invalid username or password.";
            return RedirectToAction("Login", "Account");
        }

        private void SetSessionVariables(Enrollment enrollment)
        {
            HttpContext.Session.SetInt32("EnrollmentId", enrollment.EnrollmentId);
            HttpContext.Session.SetString("LRN", enrollment?.LRN ?? "");

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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(string Address)
        {
            var userId = HttpContext.Session.GetString("LRN");

            if (userId == null)
            {
                TempData["ErrorMessage"] = "You must be logged in to edit your profile.";
                return RedirectToAction("Login", "Account");
            }
            var account = await _context.Students.FirstOrDefaultAsync(s => s.LRN == userId);

            if (account == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login", "Account");
            }

            var notification = new Notification
            {
                Message = $"Your profile has been updated.",
                UserId = account!.LRN,
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            account.Address = Address;
            _context.Update(account);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction("Account", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeTemporary(string Username, string? Password = null, string? ConfirmPassword = null)
        {
            var userId = HttpContext.Session.GetString("LRN");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "You must be logged in to edit your profile.";
                return RedirectToAction("Login", "Account");
            }
            var account = await _context.Students.FirstOrDefaultAsync(s => s.LRN == userId);
            if (await _context.Students.FirstOrDefaultAsync(s => s.Username == Username && s.LRN != userId) != null)
            {
                TempData["ErrorMessage"] = "Username already exists.";
                return RedirectToAction("Account", "Home");
            }

            if (account == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login", "Account");
            }

            account.TemporaryUsername = Username;
            if (Password != null)
            {
                if (Password != ConfirmPassword)
                {
                    TempData["ErrorMessage"] = "Passwords do not match.";
                    return RedirectToAction("Account", "Home");
                }
                else
                {
                    account.TemporaryPassword = Password;
                }
            }
            string subject = "New Temporary Signin Information";
            string body = $@"
                    <p>Dear {account.Firstname} {account.Surname},</p>
                    <p>Your new account login information</p>
                    <p>Username: {account.TemporaryUsername}</p>
                    <p>Password: {account.TemporaryPassword}</p>
                    <p>We appreciate your interest in our services. Please feel free to reply to this email if you have any questions or concerns.</p>
                    <p>Best regards,<br>Your Team</p>
                ";
            await _emailService.SendEmailAsync(account.Email, subject, body);

            var notification = new Notification
            {
                Message = $"Your temporary signin information has been updated.",
                UserId = account!.LRN,
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _context.Update(account);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Temporary signin information updated successfully.";
            return RedirectToAction("Account", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeSignin(string Username, string? Password = null, string? ConfirmPassword = null)
        {
            var userId = HttpContext.Session.GetString("LRN");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "You must be logged in to edit your profile.";
                return RedirectToAction("Login", "Account");
            }
            var account = await _context.Students.FirstOrDefaultAsync(s => s.LRN == userId);
            if (await _context.Students.FirstOrDefaultAsync(s => s.Username == Username && s.LRN != userId) != null)
            {
                TempData["ErrorMessage"] = "Username already exists.";
                return RedirectToAction("Account", "Home");
            }

            if (account == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login", "Account");
            }

            account.Username = Username;
            if (Password != null)
            {
                if (Password != ConfirmPassword)
                {
                    TempData["ErrorMessage"] = "Passwords do not match.";
                    return RedirectToAction("Account", "Home");
                }
                else
                {
                    account.Password = Password;
                }
            }
            string subject = "New Signin Information";
            string body = $@"
                    <p>Dear {account.Firstname} {account.Surname},</p>
                    <p>Your new account login information</p>
                    <p>Username: {account.Username}</p>
                    <p>Password: {account.Password}</p>
                    <p>We appreciate your interest in our services. Please feel free to reply to this email if you have any questions or concerns.</p>
                    <p>Best regards,<br>Your Team</p>
                ";
            await _emailService.SendEmailAsync(account.Email, subject, body);

            var notification = new Notification
            {
                Message = $"Your signin information has been updated.",
                UserId = account!.LRN,
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();


            _context.Update(account);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Signin information updated successfully.";
            return RedirectToAction("Account", "Home");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfileInfo(string FatherFirstName, string FatherLastName, string FatherOccupation, string MotherFirstName, string MotherLastName, string MotherOccupation, string MotherMaidenName)
        {
            var userId = HttpContext.Session.GetString("LRN");

            if (userId == null)
            {
                TempData["ErrorMessage"] = "You must be logged in to edit your profile.";
                return RedirectToAction("Login", "Account");
            }
            var account = await _context.Students.FirstOrDefaultAsync(s => s.LRN == userId);

            if (account == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login", "Account");
            }

            account.FatherFirstName = FatherFirstName;
            account.FatherLastName = FatherLastName;
            account.FatherOccupation = FatherOccupation;
            account.MotherFirstName = MotherFirstName;
            account.MotherLastName = MotherLastName;
            account.MotherOccupation = MotherOccupation;
            account.MotherMaidenName = MotherMaidenName;

            var notification = new Notification
            {
                Message = $"Your profile has been updated.",
                UserId = account!.LRN,
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();


            _context.Update(account);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction("Account", "Home");
        }
    }
}
