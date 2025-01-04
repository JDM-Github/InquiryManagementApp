
using System.ComponentModel.DataAnnotations;

namespace InquiryManagementApp.Models
{
    public class RecentActivity
    {
        [Key]
        public int Id { get; set; }
        public string Activity { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
