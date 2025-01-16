using System.ComponentModel.DataAnnotations;

namespace InquiryManagementApp.Models
{
    public class AccountView {
        public IEnumerable<Account> Accounts { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchFilter { get; set; }
        public string RoleFilter { get; set; }
    }

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

        public string Role { get; set; } = "Admin";
    }
}
