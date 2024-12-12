using System;
using System.ComponentModel.DataAnnotations;

namespace InquiryManagementApp.Models
{
    public class Enrollment
    {
        [Key]
        public int EnrollmentId { get; set; }

        // Student Information
        [Required]
        [StringLength(50)]
        public string Surname { get; set; }

        [Required]
        [StringLength(50)]
        public string Firstname { get; set; }

        [StringLength(50)]
        public string Middlename { get; set; }

        [Required]
        public string Gender { get; set; }


        public string GradeLevel { get; set; }

        // [Required]
        // public string Subject { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        public int Age { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        [StringLength(20)]
        public string LRN { get; set; }

        // Parent Information (Father)
        public string FatherLastName { get; set; }
        public string FatherFirstName { get; set; }
        public string FatherOccupation { get; set; }

        // Parent Information (Mother)
        public string MotherLastName { get; set; }
        public string MotherFirstName { get; set; }
        public string MotherOccupation { get; set; }
        public string MotherMaidenName { get; set; }

        public string Form10 { get; set; } = "";

        public string Form9 { get; set; } = "";

        public string PSA { get; set; } = "";

        public string GoodMoralCertificate { get; set; } = "";

        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        public bool IsApproved { get; set; } = false;
        public int FeePaid { get; set; } = 0;

        public string TemporaryUsername => $"temp_{LRN}_{DateTime.Now.Year}_{Surname}";
        public string TemporaryPassword => $"{Firstname}{LRN}{DateTime.Now.Year}{Surname}";

        public string Username => $"{LRN}";
        public string Password => $"{Surname}{LRN}";
    }
}
