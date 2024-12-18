using Microsoft.AspNetCore.Mvc;
using InquiryManagementApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InquiryManagementApp.Controllers
{
    public class InquiryController : Controller
    {

        private readonly ApplicationDbContext _context;

        public InquiryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Inquiry
        public async Task<IActionResult> IndexAsync()
        {
            return View(await _context.Inquiries.ToListAsync());
        }

        // GET: Inquiry/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult UpdateReason(int inquiryId, string reason)
        {
            var inquiry = _context.Inquiries.FirstOrDefault(i => i.InquiryId == inquiryId);

            if (inquiry != null)
            {
                inquiry.Reason = reason;
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Reason saved successfully!";
                return RedirectToAction("Details", new { id = inquiryId });
            }
            TempData["ErrorMessage"] = "Unable to find the inquiry.";
            return RedirectToAction("Index", "Admin");
        }

        public IActionResult Details(int id)
        {
            var inquiry = _context.Inquiries.FirstOrDefault(i => i.InquiryId == id);
            if (inquiry == null)
            {
                return NotFound();
            }

            return View(inquiry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudentName,GuardianName,ContactNumber,EmailAddress,SourceOfInformation,Notes")] Inquiry inquiry)
        {
            if (ModelState.IsValid)
            {
                inquiry.DateCreated = DateTime.Now;
                _context.Add(inquiry);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Successfully Inquired!";
                return View(inquiry);
            }

            TempData["ErrorMessage"] = "Error when Inquiring.";
            return View(inquiry);
        }



        // GET: Inquiry/Edit/5
        // public IActionResult Edit(int id)
        // {
        //     var inquiry = _inquiries.FirstOrDefault(i => i.InquiryId == id);
        //     if (inquiry == null)
        //         return NotFound();

        //     return View(inquiry);
        // }

        // POST: Inquiry/Edit/5
        // [HttpPost]
        // [ValidateAntiForgeryToken]
        // public IActionResult Edit(int id, Inquiry inquiry)
        // {
        //     var existingInquiry = _inquiries.FirstOrDefault(i => i.InquiryId == id);
        //     if (existingInquiry == null)
        //         return NotFound();

        //     if (ModelState.IsValid)
        //     {
        //         existingInquiry.StudentName = inquiry.StudentName;
        //         existingInquiry.GuardianName = inquiry.GuardianName;
        //         existingInquiry.ContactNumber = inquiry.ContactNumber;
        //         existingInquiry.EmailAddress = inquiry.EmailAddress;
        //         existingInquiry.SourceOfInformation = inquiry.SourceOfInformation;
        //         existingInquiry.Notes = inquiry.Notes;

        //         return RedirectToAction(nameof(IndexAsync));
        //     }

        //     return View(inquiry);
        // }

        // // GET: Inquiry/Delete/5
        // public IActionResult Delete(int id)
        // {
        //     var inquiry = _inquiries.FirstOrDefault(i => i.InquiryId == id);
        //     if (inquiry == null)
        //         return NotFound();

        //     return View(inquiry);
        // }

        // // POST: Inquiry/Delete/5
        // [HttpPost, ActionName("Delete")]
        // [ValidateAntiForgeryToken]
        // public IActionResult DeleteConfirmed(int id)
        // {
        //     var inquiry = _inquiries.FirstOrDefault(i => i.InquiryId == id);
        //     if (inquiry != null)
        //     {
        //         _inquiries.Remove(inquiry);
        //     }

        //     return RedirectToAction(nameof(IndexAsync));
        // }
    }
}
