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
                    if (enrollment.IsEnrolled)
                    {
                        TempData["ErrorMessage"] = "Your account is already enrolled. Please use your new account login information.";
                        return RedirectToAction("Login", "Account");
                    }
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
            HttpContext.Session.SetString("GradeLevel", enrollment.GradeLevel);
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
                UserId = account.EnrollmentId,
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
            // string body = $@"
            //         <p>Dear {account.Firstname} {account.Surname},</p>
            //         <p>Your new account login information</p>
            //         <p>Username: {account.TemporaryUsername}</p>
            //         <p>Password: {account.TemporaryPassword}</p>
            //         <p>We appreciate your interest in our services. Please feel free to reply to this email if you have any questions or concerns.</p>
            //         <p>Best regards,<br>Your Team</p>
            //     ";
            string body = $@"
                    <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f9ff; padding: 20px;'>
                        <table style='width: 100%; max-width: 600px; margin: auto; background-color: #fff; border: 1px solid #d9e6f2; border-radius: 8px;'>
                            <thead style='background-color: #0056b3; color: #fff;'>
                                <tr>
                                    <th style='padding: 15px; text-align: left; display: flex; align-items: center;'>
                                        <img src='https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTO9a84kDZORy-tOxHr1uSsYZM4hubrh6AThQ&s' alt='School Logo' style='height: 50px; margin-right: 15px;'>
                                        <div>
                                            <h2 style='margin: 0; font-size: 24px;'>DE ROMAN MONTESSORI SCHOOL</h2>
                                            <p style='margin: 0; font-size: 14px;'>Your gateway to excellence in education</p>
                                        </div>
                                    </th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td style='padding: 20px;'>
                                        <p style='font-size: 16px; color: #0056b3;'>Dear <strong>{account.Firstname} {account.Surname}</strong>,</p>
                                        <p style='font-size: 14px;'>
                                            <p>Your new account login information</p>
                                            <p>Username: {account.TemporaryUsername}</p>
                                            <p>Password: {account.TemporaryPassword}</p>
                                        </p>
                                        <p style='font-size: 14px;'>If you have any questions or need assistance, feel free to reply to this email or contact us directly:</p>
                                        <ul style='font-size: 14px; color: #333;'>
                                            <li><strong>Phone:</strong> +123-456-7890</li>
                                            <li><strong>Email:</strong> <a href='mailto:contact@dromanmontessori.edu' style='color: #0056b3;'>contact@dromanmontessori.edu</a></li>
                                            <li><strong>Website:</strong> <a href='https://www.dromanmontessori.edu' style='color: #0056b3;'>www.dromanmontessori.edu</a></li>
                                        </ul>
                                        <p style='font-size: 14px;'>Thank you for choosing De Roman Montessori School. We look forward to assisting you!</p>
                                    </td>
                                </tr>
                            </tbody>
                            <tfoot style='background-color: #fbe052; color: #0056b3;'>
                                <tr>
                                    <td style='padding: 10px; text-align: center; font-size: 12px;'>
                                        <p style='margin: 0;'>De Roman Montessori School, 123 Academic Street, Education City</p>
                                        <p style='margin: 0;'>Contact us: +123-456-7890 | <a href='mailto:contact@dromanmontessori.edu' style='color: #0056b3;'>contact@dromanmontessori.edu</a></p>
                                        <p style='margin: 0;'>&copy; {DateTime.Now.Year} De Roman Montessori School. All rights reserved.</p>
                                    </td>
                                </tr>
                            </tfoot>
                        </table>
                    </div>";
            await _emailService.SendEmailAsync(account.Email, subject, body);

            var notification = new Notification
            {
                Message = $"Your temporary signin information has been updated.",
                UserId = account.EnrollmentId,
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
                UserId = account.EnrollmentId,
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
        public async Task<IActionResult> EditProfileInfo(string? FatherFirstName, string? FatherLastName, string? FatherOccupation, string? MotherFirstName, string? MotherLastName, string? MotherOccupation, string? MotherMaidenName)
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

            account.FatherFirstName = FatherFirstName ?? "";
            account.FatherLastName = FatherLastName ?? "";
            account.FatherOccupation = FatherOccupation ?? "";
            account.MotherFirstName = MotherFirstName ?? "";
            account.MotherLastName = MotherLastName ?? "";
            account.MotherOccupation = MotherOccupation ?? "";
            account.MotherMaidenName = MotherMaidenName ?? "";

            var notification = new Notification
            {
                Message = $"Your profile has been updated.",
                UserId = account.EnrollmentId,
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
