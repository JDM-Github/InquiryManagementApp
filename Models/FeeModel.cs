using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InquiryManagementApp.Models
{
    public class CashFee {
        public double TuitionFee { get; set; } = 19000;
        public double Miscellaneous { get; set; } = 14000;

        public double Discount { get; set; } = 10; // PERCENTAGE
    }

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
