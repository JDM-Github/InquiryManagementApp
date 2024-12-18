using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using InquiryManagementApp.Models;

namespace InquiryManagementApp.Controllers;

public class FeeController : Controller
{
    private static List<FeeModel> fees = new FeeListModel().Fees;

    public IActionResult Index()
    {
        return RedirectToAction("ManageFees", "Admin");
    }


    public IActionResult Add()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Add(FeeModel newFee)
    {
        if (ModelState.IsValid)
        {
            fees.Add(newFee);
            return RedirectToAction("Index");
        }

        return View(newFee);
    }

    public IActionResult Edit(string level)
    {
        var fee = fees.FirstOrDefault(f => f.Level == level);
        if (fee == null)
        {
            return NotFound();
        }

        return View(fee);
    }

    [HttpPost]
    public IActionResult Edit(FeeModel updatedFee)
    {
        var fee = fees.FirstOrDefault(f => f.Level == updatedFee.Level);
        if (fee != null)
        {
            fee.Fee = updatedFee.Fee;
            fee.PaymentType = updatedFee.PaymentType;
            return RedirectToAction("Index");
        }

        return NotFound();
    }

    public IActionResult Delete(string level)
    {
        var fee = fees.FirstOrDefault(f => f.Level == level);
        if (fee != null)
        {
            fees.Remove(fee);
            return RedirectToAction("Index");
        }

        return NotFound();
    }
}