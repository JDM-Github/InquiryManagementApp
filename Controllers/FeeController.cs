using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using InquiryManagementApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InquiryManagementApp.Controllers;

public class FeeController : Controller
{
    private readonly ApplicationDbContext _context;

    public FeeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return RedirectToAction("ManageFees", "Admin");
    }


    // public IActionResult Add()
    // {
    //     return View();
    // }

    // [HttpPost]
    // public IActionResult Add(FeeModel newFee)
    // {
    //     // if (ModelState.IsValid)
    //     // {
    //     //     fees.Add(newFee);
    //     //     return RedirectToAction("Index");
    //     // }

    //     return View(newFee);
    // }

    // public IActionResult Edit(string level)
    // {
    //     var fee = fees.FirstOrDefault(f => f.Level == level);
    //     if (fee == null)
    //     {
    //         return NotFound();
    //     }

    //     return View(fee);
    // }

    // [HttpPost]
    // public IActionResult Edit(FeeModel updatedFee)
    // {
    //     var fee = fees.FirstOrDefault(f => f.Level == updatedFee.Level);
    //     if (fee != null)
    //     {
    //         fee.Fee = updatedFee.Fee;
    //         fee.PaymentType = updatedFee.PaymentType;
    //         return RedirectToAction("Index");
    //     }

    //     return NotFound();
    // }

    // public IActionResult Delete(string level)
    // {
    //     var fee = fees.FirstOrDefault(f => f.Level == level);
    //     if (fee != null)
    //     {
    //         fees.Remove(fee);
    //         return RedirectToAction("Index");
    //     }

    //     return NotFound();
    // }


    // [HttpGet]
    // public async Task<IActionResult> Edit(int id)
    // {
    //     var fee = await _context.Fees.FirstOrDefaultAsync(f => f.Id == id);
    //     if (fee == null)
    //     {
    //         return NotFound();
    //     }

    //     return PartialView("_EditFeeModal", fee);
    // }

    [HttpGet]
    public async Task<IActionResult> GetTuitionFees()
    {
        var fees = await _context.Fees.ToListAsync();
        return Ok(fees);
    }

    // [HttpPost]
    // public async Task<IActionResult> Edit(int id, string Level, double Fee)
    // {
    //     var fee = await _context.Fees.FirstOrDefaultAsync(f => f.Id == id);
    //     if (fee == null)
    //     {
    //         TempData["ErrorMessage"] = "Fee not found.";
    //         return RedirectToAction("ManageFees", "Admin");
    //     }

    //     fee.Level = Level;
    //     fee.Fee = Fee;
    //     await _context.SaveChangesAsync();

    //     var recent = new RecentActivity
    //     {
    //         Activity = $"Fee details updated for {fee.Level}: {fee.Fee}",
    //         CreatedAt = DateTime.Now
    //     };
    //     _context.RecentActivities.Add(recent);
    //     await _context.SaveChangesAsync();

    //     TempData["SuccessMessage"] = "Fee details updated successfully!";
    //     return RedirectToAction("ManageFees", "Admin");
    // }
}