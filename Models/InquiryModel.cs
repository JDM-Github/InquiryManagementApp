using System.ComponentModel.DataAnnotations;

namespace InquiryManagementApp.Models
{
    public class Inquiry
    {
        [Key]
        public int InquiryId { get; set; }

        [Required]
        [StringLength(100)]
        public string StudentName { get; set; }

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

        // Optional field for future use
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }
    }
}
