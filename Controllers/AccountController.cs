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
                    if (enrollment2.IsEnrolled && enrollment2.IsApproved)
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
        public async Task<IActionResult> EditAdminProfile(int UserId, string Surname, string Firstname, string Middlename, string Gender, string Address)
        {
            ViewBag.UserId = UserId;
            var account = await _context.Students.FirstOrDefaultAsync(s => s.EnrollmentId == UserId);

            if (account == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("ViewAccount", "Home", new { userId = UserId });
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

            account.Surname = Surname;
            account.Firstname = Firstname;
            account.Middlename = Middlename;
            account.Gender = Gender;
            account.Address = Address;
            _context.Update(account);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction("ViewAccount", "Home", new { userId = UserId });
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
                Message = "Your profile information has been successfully updated.",
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
            string subject = "Your Temporary Sign-In Information";
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
                                    <p style='font-size: 14px;'>Welcome to De Roman Montessori School! Below are your temporary sign-in credentials:</p>
                                    <p style='font-size: 14px;'><strong>Username:</strong> {account.TemporaryUsername}</p>
                                    <p style='font-size: 14px;'><strong>Password:</strong> {account.TemporaryPassword}</p>
                                    <p style='font-size: 14px;'>Please use these credentials to access your account. We recommend updating your password immediately after logging in for the first time.</p>
                                    <p style='font-size: 14px;'>Should you have any questions or require further assistance, do not hesitate to contact us:</p>
                                    <ul style='font-size: 14px; color: #333;'>
                                        <li><strong>Email:</strong> <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></li>
                                        <li><strong>Phone:</strong> 09274044188</li>
                                    </ul>
                                    <p style='font-size: 14px;'>Thank you for choosing our services. We are here to support you every step of the way.</p>
                                </td>
                            </tr>
                        </tbody>
                        <tfoot style='background-color: #fbe052; color: #0056b3;'>
                            <tr>
                                <td style='padding: 10px; text-align: center; font-size: 12px;'>
                                    <p style='margin: 0;'>De Roman Montessori School, Tanza, Philippines</p>
                                    <p style='margin: 0;'>Contact us: 09274044188 | <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></p>
                                    <p style='margin: 0;'>&copy; {DateTime.Now.Year} De Roman Montessori School. All rights reserved.</p>
                                </td>
                            </tr>
                        </tfoot>
                    </table>
                </div>";
            await _emailService.SendEmailAsync(account.Email, subject, body);

            var notification = new Notification
            {
                Message = "Your temporary sign-in information has been successfully updated.",
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
            string subject = "Your Updated Sign-In Information";
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
                                    <p style='font-size: 14px;'>We are pleased to provide your updated sign-in credentials:</p>
                                    <p style='font-size: 14px;'><strong>Username:</strong> {account.Username}</p>
                                    <p style='font-size: 14px;'><strong>Password:</strong> {account.Password}</p>
                                    <p style='font-size: 14px;'>For your security, we recommend updating your password after your first login.</p>
                                    <p style='font-size: 14px;'>If you have any questions or need assistance, please feel free to contact us directly:</p>
                                    <ul style='font-size: 14px; color: #333;'>
                                        <li><strong>Email:</strong> <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></li>
                                        <li><strong>Phone:</strong> 09274044188</li>
                                    </ul>
                                    <p style='font-size: 14px;'>Thank you for choosing our services. We are here to support you every step of the way.</p>
                                </td>
                            </tr>
                        </tbody>
                        <tfoot style='background-color: #fbe052; color: #0056b3;'>
                            <tr>
                                <td style='padding: 10px; text-align: center; font-size: 12px;'>
                                    <p style='margin: 0;'>De Roman Montessori School, Tanza, Philippines</p>
                                    <p style='margin: 0;'>Contact us: 09274044188 | <a href='mailto:depedcavite.deromanmontessori@gmail.com' style='color: #0056b3;'>depedcavite.deromanmontessori@gmail.com</a></p>
                                    <p style='margin: 0;'>&copy; {DateTime.Now.Year} De Roman Montessori School. All rights reserved.</p>
                                </td>
                            </tr>
                        </tfoot>
                    </table>
                </div>";
            await _emailService.SendEmailAsync(account.Email, subject, body);

            var notification = new Notification
            {
                Message = "Your sign-in information has been successfully updated.",
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
                Message = "Your profile information has been successfully updated.",
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
