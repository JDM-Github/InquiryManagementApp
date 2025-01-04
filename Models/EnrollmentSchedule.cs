using System.ComponentModel.DataAnnotations;

namespace InquiryManagementApp.Models
{
    public class EnrollmentSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public static bool InstanceExists { get; private set; }

        static EnrollmentSchedule()
        {
            InstanceExists = false;
        }

        public static void CreateInstance()
        {
            if (InstanceExists)
            {
                throw new InvalidOperationException("Only one EnrollmentSchedule instance can exist.");
            }
            InstanceExists = true;
        }
    }
}
