using System.ComponentModel.DataAnnotations;

namespace InquiryManagementApp.Models
{
    public class Account
    {
        [Key]
        public int AccountId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(50)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Passwords must match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [StringLength(50)]
        public string Email { get; set; }

        [Required]
        public bool IsAdmin { get; set; } = false;

        [Required]
        public bool IsMarketing { get; set; } = false;

        [Required]
        public bool IsStudent { get; set; } = false;

        public int? EnrollmentId { get; set; }

        public virtual Enrollment Enrollment { get; set; }
    }
}
