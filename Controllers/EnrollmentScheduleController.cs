using Microsoft.AspNetCore.Mvc;
using InquiryManagementApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InquiryManagementApp.Controllers;
public class EnrollmentScheduleController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly EmailService _emailService;

    public EnrollmentScheduleController(ApplicationDbContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
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

            var inquiries = await _context.Inquiries.ToListAsync();
            foreach (var inquiry in inquiries)
            {
                if (inquiry.IsEnrolled)
                    continue;

                string subject = "Exciting News: Enrollment is Now Open!";
                string confirmationLink = Url.Action("CreateClick", "Enrollment", new { id = inquiry.InquiryId }, Request.Scheme) ?? "";
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
                                        <p style='font-size: 16px; color: #0056b3;'>Dear <strong>{inquiry.StudentName}</strong>,</p>
                                        <p style='font-size: 14px;'>We are thrilled to announce that enrollment for the upcoming academic year is officially open! Click the link below to secure your spot:</p>
                                        <p style='text-align: center; margin: 20px 0;'>
                                            <a href='{confirmationLink}' style='display: inline-block; background-color: #0056b3; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px; font-size: 16px;'>Complete Your Enrollment</a>
                                        </p>
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

                await _emailService.SendEmailAsync(inquiry.EmailAddress, subject, body);

            }

            var recent = new RecentActivity
            {
                Activity = $"Enrollment schedule created. Start date: {enrollmentSchedule.StartDate:MM/dd/yyyy}, End date: {enrollmentSchedule.EndDate:MM/dd/yyyy}",
                CreatedAt = DateTime.Now
            };

            _context.RecentActivities.Add(recent);
            await _context.SaveChangesAsync();


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
