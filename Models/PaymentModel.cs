using System.ComponentModel.DataAnnotations;

namespace InquiryManagementApp.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }
        public DateTime Date { get; set; }
        public int Amount { get; set; }
        public int EnrollmentId { get; set; }
    };
}