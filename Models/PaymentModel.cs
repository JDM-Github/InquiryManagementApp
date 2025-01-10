using System.ComponentModel.DataAnnotations;

namespace InquiryManagementApp.Models
{
    public class PaymentsManagementViewModel
    {
        public IEnumerable<PaymentViewModel> Payments { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchFilter { get; set; }
        public bool IsPaymentDay { get; set; }

    }

    public class PaymentViewModel
    {
        public Payment Payment { get; set; }
        public Enrollment? Enrollees { get; set; }

    }

    public class PaymentSchedule {
        [Key]
        public int Id { get; set; }
        public string ?CurrentPaymentId { get; set; } = null;
        public static PaymentSchedule ?CurrentPaymentSchedule { get; set; } = new PaymentSchedule();

        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now;
        public List<string> PaymentIds { get; set; } = new List<string>(); // All history of CurrentPaymentId
        public static bool InstanceExists { get; private set; }
        public bool IsActive { get; set; } = false;

        public static void CreateInstance(PaymentSchedule instance)
        {
            CurrentPaymentSchedule = instance;
            InstanceExists = true;
        }
        
    }

    public class Payment
    {
        [Key]
        public int Id { get; set; }
        public DateTime Date { get; set; }

        public string TransactionId { get; set; }
        public string PaymentId { get; set; } 
        public double PaidAmount { get; set; }
        public string ReferenceNumber { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public string PaymentLink { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public int EnrollreesId { get; set; }
        public DateTime ExpirationTime { get; set; }
    };
}
