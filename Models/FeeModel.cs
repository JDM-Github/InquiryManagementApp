using System;
using System.Collections.Generic;

namespace InquiryManagementApp.Models
{
    public class FeeModel
    {
        public string Level { get; set; }
        public decimal Fee { get; set; }
        public string PaymentType { get; set; }

        public FeeModel(string level, decimal fee, string paymentType)
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
