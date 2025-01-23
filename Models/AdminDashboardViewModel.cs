
namespace InquiryManagementApp.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalInquiries { get; set; }
        public int TotalEnrolled { get; set; }
        public int TotalApproved { get; set; }
        public double TotalRevenue { get; set; }
        public PaymentSchedule CurrentPayment { get; set; }

        public Dictionary<string, int> CancellationAnalytics { get; set; } = new Dictionary<string, int>();
        public Dictionary<int, int> EnrollmentTrends { get; set; } = new Dictionary<int, int>();
        public List<RecentActivity> RecentActivities { get; set; } = new List<RecentActivity>();
        public List<Inquiry> Inquiries { get; set; } = new List<Inquiry>();
    }
}

