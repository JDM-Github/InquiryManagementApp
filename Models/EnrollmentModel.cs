using System;
using System.ComponentModel.DataAnnotations;

namespace InquiryManagementApp.Models
{

    public class ManageEnrolledView
    {
        public IEnumerable<Enrollment> Enrolled { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchFilter { get; set; }
        public string GradeFilter { get; set; }
        public string StatusFilter { get; set; } = "";
    }

    public class EnrollmentRequirementsViewModel
    {
        public Enrollment Enrollment { get; set; }
        public List<RequirementModel> Requirements { get; set; }
    }

    public class StudentViewModel
    {
        public int EnrollmentId { get; set; }
        public string StudentID { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Surname { get; set; }
        public string GradeLevel { get; set; }
        public DateTime Birthday { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }

        public string PaymentType { get; set; }
        public double TotalToPay { get; set; }
    }

    public class Enrollment
    {
        [Key]
        public int EnrollmentId { get; set; }
        [Required]
        [StringLength(50)]
        public string Surname { get; set; }
        [Required]
        [StringLength(50)]
        public string Firstname { get; set; }
        [StringLength(50)]
        public string? Middlename { get; set; } = "";
        [Required]
        public string Gender { get; set; }
        public string GradeLevel { get; set; }
        [Required]
        [StringLength(50)]
        public string Email { get; set; } = "";
        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; } = DateTime.Now;
        public int Age { get; set; } = 0;
        
        [Required]
        public string Address { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string? LRN { get; set; } = null;
        public string? StudentID { get; set; } = null;

        public string FatherLastName { get; set; } = "";
        public string FatherFirstName { get; set; } = "";
        public string FatherOccupation { get; set; } = "";

        public string MotherLastName { get; set; } = "";
        public string MotherFirstName { get; set; } = "";
        public string MotherOccupation { get; set; } = "";
        public string MotherMaidenName { get; set; } = "";


        public string GoodMoralCertificate { get; set; } = "";
        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        public bool IsApproved { get; set; } = false;
        public DateTime? ApprovedEnrolled { get; set; } = null;
        public String? ApproveId { get; set; } = null;

        public bool IsEnrolled { get; set; } = false;
        public DateTime? EnrolledDate { get; set; } = null;

        public bool IsRejected { get; set; } = false;
        public double FeePaid { get; set; } = 0;

        public bool IsDeleted { get; set; } = false;

        public string TemporaryUsername { get; set; } = "";
        public string TemporaryPassword { get; set; } = "";

        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        public bool IsWalkin { get; set; } = false;
        public bool IsEarlyBird { get; set; } = false;
        public bool IsCashPayment { get; set; } = false;
        public int NumberOfSibling { get; set; } = 0;
        public string PaymentType { get; set; } = "Cash";
        public double PayPerDate { get; set; } = 0;
        public double TotalToPay { get; set; } = 0;
        public double BalanceToPay { get; set; } = 0;
    }
}


