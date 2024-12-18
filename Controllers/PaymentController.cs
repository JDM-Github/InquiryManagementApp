using Microsoft.AspNetCore.Mvc;
using InquiryManagementApp.Models;
using Microsoft.EntityFrameworkCore;

public class PaymentController : Controller
{
    private readonly ApplicationDbContext _context;

    public PaymentController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public IActionResult ProcessPayment(int amount)
    {
        var userId = HttpContext.Session.GetString("LRN");
        var enrollment = _context.Students
            .Include(e => e.PaymentHistories)
            .FirstOrDefault(e => e.LRN == userId);

        if (enrollment == null)
        {
            return NotFound("Enrollment record not found.");
        }

        if (amount <= 0 || enrollment.FeePaid + amount > 5000)
        {
            TempData["ErrorMessage"] = "Invalid payment amount.";
            return RedirectToAction("Payment", "Home");
        }

        enrollment.FeePaid += amount;
        var payment = new Payment
        {
            Date = DateTime.Now,
            Amount = amount,
            EnrollmentId = enrollment.EnrollmentId
        };
        _context.Payments.Add(payment);
        _context.SaveChanges();

        TempData["SuccessMessage"] = $"Payment of ₱{amount} successfully processed.";
        return RedirectToAction("Payment", "Home");
    }

}
