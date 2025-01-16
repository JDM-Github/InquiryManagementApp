using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InquiryManagementApp.Models
{
    public class CashFeeView {
        // SINCE TUITION FEE IS FIXED PER SOMETHING
        public static double TuitionFee { get; set; } = 19000;
        // THIS WILL BE STATIC
        public static double Miscellaneous { get; set; } = 14000;

        public static double QuarterlyFee { get; set; } = 4750;
        public static double MonthlyFee { get; set; } = 1900;
        public static double UponEnrollment { get; set; } = 5000;
        public static double PromoFee { get; set; } = 2800;

    }

    public class Fee {
        public int Id { get; set; }
        public double TuitionFee { get; set; } = 19000;
        public double Miscellaneous { get; set; } = 14000;
    }

    public class StudentPaymentView {
        public string ApprovedId { get; set; } = "";
        public string PaymentType { get; set; }
    }

    public class StudentPayment {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public Enrollment Student { get; set; }

        public string ReferenceNumber { get; set; }
        public string Status { get; set; } = "Pending";

        public double PaymentAmount { get; set; }
        public string MonthPaid { get; set; }
        public string YearPaid { get; set; }
        public string PaymentFor { get; set; } = "Tuition";
        public DateTime? Date { get; set; }
    }

    public class StudentPaymentRecord {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public Enrollment Student { get; set; }

        // This will determine all Discount but this thing suck
        // public double TotalDiscount { get; set; } = 10;

        // QUATER, MONTHLY, CASH
        public string? PaymentType { get; set; } = null;
        public double Balance { get; set; } = 0;
        public bool CashDiscount { get; set; } = false;
        public bool EarlyBird { get; set; } = false;
        public int SiblingDiscount { get; set; } = 0;

        public double? PerPayment { get; set; } = null; // This will be calculated in ApproveStudent
        public List<int> PaymentId { get; set; } = new List<int>();
    }

    // public class Quarterly
    // {
    //     // October Start
    //     public double TuitionFee { get; set; } = 19000;
    //     public double Miscellaneous { get; set; } = 14000;
    //     public double Discount { get; set; } = 10;
    //     public List<int> Payment { get; set; } = new List<int>();
    // }

    // public class Monthly
    // {
    //     public double TuitionFee { get; set; } = 19000;
    //     public double Miscellaneous { get; set; } = 14000;
    //     public double Discount { get; set; } = 10; // PERCENTAGE
    //     public List<int> Payment { get; set; } = new List<int>();
    // }

    public class FeeModel
    {
        [Key]
        public int Id { get; set; }
        public string Level { get; set; }
        public double Fee { get; set; }
        public string PaymentType { get; set; }

        public FeeModel() { }
        public FeeModel(string level, double fee, string paymentType)
        {
            Level = level;
            Fee = fee;
            PaymentType = paymentType;
        }
    }

    public class FeeListModel
    {
        public List<FeeModel> Fees { get; set; }

        public FeeListModel()
        {
            Fees = new List<FeeModel>
        {
            new FeeModel("NURSERY", 2000, "Cash"),
            new FeeModel("KINDER", 5000, "Cash"),
            new FeeModel("ELEMENTARY", 4000, "Cash"),
            new FeeModel("JUNIOR HIGH SCHOOL", 6000, "Installment"),
            new FeeModel("SENIOR HIGH SCHOOL - ABM", 7000, "Installment"),
            new FeeModel("SENIOR HIGH SCHOOL - HUMSS", 8000, "Installment")
        };
        }
    }

}
