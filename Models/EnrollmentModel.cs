using System;
using System.ComponentModel.DataAnnotations;

namespace InquiryManagementApp.Models
{
    public class Enrollment
    {
        public Enrollment()
        {
            UploadedFiles = new List<string>();
            PaymentHistories = new List<int>();
            TemporaryUsername = $"temp_{LRN}_{DateTime.Now.Year}_{Surname}";
            TemporaryPassword = $"{Firstname}{LRN}{DateTime.Now.Year}{Surname}";
            Username = $"{LRN}";
            Password = $"{Surname}{LRN}";
        }

        public void SetTemporaryCredentials()
        {
            TemporaryUsername = $"temp_{LRN}_{DateTime.Now.Year}_{Surname}";
            TemporaryPassword = $"{Firstname}{LRN}{DateTime.Now.Year}{Surname}";
            Username = $"{LRN}";
            Password = $"{Surname}{LRN}";
        }

        [Key]
        public int EnrollmentId { get; set; }
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

        [Required]
        [StringLength(50)]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        public int Age { get; set; }

        [Required]
        public string Address { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string LRN { get; set; }

        public string FatherLastName { get; set; } = "";
        public string FatherFirstName { get; set; } = "";
        public string FatherOccupation { get; set; } = "";

        public string MotherLastName { get; set; } = "";
        public string MotherFirstName { get; set; } = "";
        public string MotherOccupation { get; set; } = "";
        public string MotherMaidenName { get; set; } = "";

        public string Form10 { get; set; } = "";

        public string Form9 { get; set; } = "";

        public string PSA { get; set; } = "";

        public string GoodMoralCertificate { get; set; } = "";

        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        public bool IsApproved { get; set; } = false;
        public bool IsRejected { get; set; } = false;
        public int FeePaid { get; set; } = 0;



        public string TemporaryUsername { get; set; }
        public string TemporaryPassword { get; set; }

        public string Username { get; set; }
        public string Password { get; set; }



        public required List<string> UploadedFiles { get; set; }
        public required List<int> PaymentHistories { get; set; }
    }
}
