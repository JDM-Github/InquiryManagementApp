using System.ComponentModel.DataAnnotations;

namespace InquiryManagementApp.Models
{
    public class InquiryView
    {
        public IEnumerable<Inquiry> Inquiries { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchFilter { get; set; }
        public string StatusFilter { get; set; } = "";
        public string RStatusFilter { get; set; } = "";
    }

    public class InquiryCancellationViewModel
    {
        public int InquiryId { get; set; }
        public string CancellationReason { get; set; }
        public string? CancellationNotes { get; set; }
    }

    public class Inquiry
    {
        [Key]
        public int InquiryId { get; set; }
        public string StudentName { get; set; } = "";
        public string Surname { get; set; }
        public string Firstname { get; set; }
        public string? Middlename { get; set; } = "";

        public DateTime DateOfBirth { get; set; } = DateTime.Now;
        public string Gender { get; set; } = "Male";


        [Required]
        [StringLength(100)]
        public string GuardianName { get; set; }

        [Required]
        [StringLength(15, ErrorMessage = "Contact number cannot exceed 15 characters.")]
        [Phone]
        public string ContactNumber { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string EmailAddress { get; set; }

        [StringLength(100, ErrorMessage = "Source of Information cannot exceed 100 characters.")]
        public string? SourceOfInformation { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public string GradeLevel { get; set; }


        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }
        public string? Reason { get; set; } = null;

        public bool IsApproved { get; set; } = false;
        public bool IsInquired { get; set; } = false;
        public bool IsEnrolled { get; set; } = false;
        public string InquiredString { get; set; } = "";
        public bool IsClickedOnEmail { get; set; } = false;

        public bool IsConfirmed { get; set; } = false;
        public bool IsRejected { get; set; } = false;
        public bool IsCancelled { get; set; } = false;
        public string CancellationReason { get; set; } = "";
        public string? CancellationNotes { get; set; } = "";

        // public bool 
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

