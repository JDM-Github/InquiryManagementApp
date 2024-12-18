using InquiryManagementApp.Models;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public required DbSet<Inquiry> Inquiries { get; set; }
    public required DbSet<Enrollment> Students { get; set; }
    public required DbSet<EnrollmentSchedule> EnrollmentSchedules { get; set; }
    public required DbSet<Notification> Notifications { get; set; }
    // public required DbSet<FeeModel> Fees { get; set; }

    public required DbSet<Account> Accounts { get; set; }
    public required DbSet<Payment> Payments { get; set; }
    // public required DbSet<Enrollments>

}