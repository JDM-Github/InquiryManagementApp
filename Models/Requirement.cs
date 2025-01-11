using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InquiryManagementApp.Models
{
    public class RequirementModel
    {
        [Key]
        public int Id { get; set; }
        public string RequirementName { get; set; }
        public string Description { get; set; }
        public string UploadedFile { get; set; } = "";

        public bool IsRequired { get; set; } = true;
        public string GradeLevel { get; set; }

        public int EnrollmentId { get; set; }

        [ForeignKey("EnrollmentId")]
        public Enrollment Enrollment { get; set; }
        public bool IsRejected { get; set; } = false;
        public bool IsApproved { get; set; } = false;
    }

    public class Requirement
    {
        [Key]
        public int Id { get; set; }

        public string RequirementName { get; set; }
        public string Description { get; set; }
        public string UploadedFile { get; set; } = "";

        public bool IsRequired { get; set; } = true;
        public string GradeLevel { get; set; }
        public bool IsRejected { get; set; } = false;
        public bool IsApproved { get; set; } = false;
    }
}
