using InquiryManagementApp.Models;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var allTuition = new Dictionary<string, double>
        {
            { "NURSERY", 2000 },
            { "KINDER", 5000 },
            { "ELEMENTARY", 4000 },
            { "JUNIOR HIGH SCHOOL", 6000 },
            { "SENIOR HIGH SCHOOL 11 ABM (1ST SEM)", 7000 },
            { "SENIOR HIGH SCHOOL 11 ABM (2ND SEM)", 7000 },
            { "SENIOR HIGH SCHOOL 12 ABM (1ST SEM)", 8000 },
            { "SENIOR HIGH SCHOOL 12 ABM (2ND SEM)", 8000 },
            { "SENIOR HIGH SCHOOL 11 HUMSS (1ST SEM)", 7000 },
            { "SENIOR HIGH SCHOOL 11 HUMSS (2ND SEM)", 7000 },
            { "SENIOR HIGH SCHOOL 12 HUMSS (1ST SEM)", 8000 },
            { "SENIOR HIGH SCHOOL 12 HUMSS (2ND SEM)", 8000 }
        };

        modelBuilder.Entity<FeeModel>().HasData(
            new FeeModel { Id = 1, Level = "NURSERY", Fee = allTuition["NURSERY"], PaymentType = "Cash" },
            new FeeModel { Id = 2, Level = "KINDER", Fee = allTuition["KINDER"], PaymentType = "Cash" },
            new FeeModel { Id = 3, Level = "ELEMENTARY", Fee = allTuition["ELEMENTARY"], PaymentType = "Cash" },
            new FeeModel { Id = 4, Level = "JUNIOR HIGH SCHOOL", Fee = allTuition["JUNIOR HIGH SCHOOL"], PaymentType = "Installment" },
            new FeeModel { Id = 5, Level = "SENIOR HIGH SCHOOL 11 ABM (1ST SEM)", Fee = allTuition["SENIOR HIGH SCHOOL 11 ABM (1ST SEM)"], PaymentType = "Installment" },
            new FeeModel { Id = 6, Level = "SENIOR HIGH SCHOOL 11 ABM (2ND SEM)", Fee = allTuition["SENIOR HIGH SCHOOL 11 ABM (2ND SEM)"], PaymentType = "Installment" },
            new FeeModel { Id = 7, Level = "SENIOR HIGH SCHOOL 12 ABM (1ST SEM)", Fee = allTuition["SENIOR HIGH SCHOOL 12 ABM (1ST SEM)"], PaymentType = "Installment" },
            new FeeModel { Id = 8, Level = "SENIOR HIGH SCHOOL 12 ABM (2ND SEM)", Fee = allTuition["SENIOR HIGH SCHOOL 12 ABM (2ND SEM)"], PaymentType = "Installment" },
            new FeeModel { Id = 9, Level = "SENIOR HIGH SCHOOL 11 HUMSS (1ST SEM)", Fee = allTuition["SENIOR HIGH SCHOOL 11 HUMSS (1ST SEM)"], PaymentType = "Installment" },
            new FeeModel { Id = 10, Level = "SENIOR HIGH SCHOOL 11 HUMSS (2ND SEM)", Fee = allTuition["SENIOR HIGH SCHOOL 11 HUMSS (2ND SEM)"], PaymentType = "Installment" },
            new FeeModel { Id = 11, Level = "SENIOR HIGH SCHOOL 12 HUMSS (1ST SEM)", Fee = allTuition["SENIOR HIGH SCHOOL 12 HUMSS (1ST SEM)"], PaymentType = "Installment" },
            new FeeModel { Id = 12, Level = "SENIOR HIGH SCHOOL 12 HUMSS (2ND SEM)", Fee = allTuition["SENIOR HIGH SCHOOL 12 HUMSS (2ND SEM)"], PaymentType = "Installment" }
        );

        modelBuilder.Entity<Requirement>().HasData(
            new Requirement
            {
                Id = 1,
                RequirementName = "Birth Certificate",
                Description = "A valid copy of the Birth Certificate.",
                GradeLevel = "NURSERY",
                IsRequired = true
            },

            // KINDER
            new Requirement
            {
                Id = 2,
                RequirementName = "Report Card",
                Description = "Last year’s report card.",
                GradeLevel = "KINDER",
                IsRequired = true
            },
            new Requirement
            {
                Id = 3,
                RequirementName = "Birth Certificate",
                Description = "A valid copy of the Birth Certificate.",
                GradeLevel = "KINDER",
                IsRequired = true
            },

            // ELEMENTARY
            new Requirement
            {
                Id = 4,
                RequirementName = "Medical Certificate",
                Description = "A current medical certificate.",
                GradeLevel = "ELEMENTARY",
                IsRequired = true
            },
            new Requirement
            {
                Id = 5,
                RequirementName = "Report Card",
                Description = "Last year’s report card.",
                GradeLevel = "ELEMENTARY",
                IsRequired = true
            },
            new Requirement
            {
                Id = 6,
                RequirementName = "PSA Birth Certificate",
                Description = "A valid PSA-certified Birth Certificate.",
                GradeLevel = "ELEMENTARY",
                IsRequired = true
            },

            // JUNIOR HIGH SCHOOL
            new Requirement
            {
                Id = 7,
                RequirementName = "Form 138 (Report Card)",
                Description = "Form 138 (Report Card) for the last grade level.",
                GradeLevel = "JUNIOR HIGH SCHOOL",
                IsRequired = true
            },
            new Requirement
            {
                Id = 8,
                RequirementName = "PSA Birth Certificate",
                Description = "A valid PSA-certified Birth Certificate.",
                GradeLevel = "JUNIOR HIGH SCHOOL",
                IsRequired = true
            },
            new Requirement
            {
                Id = 9,
                RequirementName = "Certificate of Good Moral",
                Description = "Certificate of Good Moral Character from previous school.",
                GradeLevel = "JUNIOR HIGH SCHOOL",
                IsRequired = true
            },

            // SENIOR HIGH SCHOOL 11 ABM (1ST SEM)
            new Requirement
            {
                Id = 10,
                RequirementName = "Form 137 (Grade 10 Report Card)",
                Description = "Form 137 or the Grade 10 Report Card.",
                GradeLevel = "SENIOR HIGH SCHOOL 11 ABM (1ST SEM)",
                IsRequired = true
            },
            new Requirement
            {
                Id = 11,
                RequirementName = "PSA Birth Certificate",
                Description = "A valid PSA-certified Birth Certificate.",
                GradeLevel = "SENIOR HIGH SCHOOL 11 ABM (1ST SEM)",
                IsRequired = true
            },
            new Requirement
            {
                Id = 12,
                RequirementName = "Certificate of Good Moral",
                Description = "Certificate of Good Moral Character from previous school.",
                GradeLevel = "SENIOR HIGH SCHOOL 11 ABM (1ST SEM)",
                IsRequired = true
            },

            // SENIOR HIGH SCHOOL 11 ABM (2ND SEM)
            new Requirement
            {
                Id = 13,
                RequirementName = "Form 137 (Grade 10 Report Card)",
                Description = "Form 137 or the Grade 10 Report Card.",
                GradeLevel = "SENIOR HIGH SCHOOL 11 ABM (2ND SEM)",
                IsRequired = true
            },
            new Requirement
            {
                Id = 14,
                RequirementName = "PSA Birth Certificate",
                Description = "A valid PSA-certified Birth Certificate.",
                GradeLevel = "SENIOR HIGH SCHOOL 11 ABM (2ND SEM)",
                IsRequired = true
            },
            new Requirement
            {
                Id = 15,
                RequirementName = "Certificate of Good Moral",
                Description = "Certificate of Good Moral Character from previous school.",
                GradeLevel = "SENIOR HIGH SCHOOL 11 ABM (2ND SEM)",
                IsRequired = true
            },

            // SENIOR HIGH SCHOOL 12 ABM (1ST SEM)
            new Requirement
            {
                Id = 16,
                RequirementName = "Form 137 (Grade 11 Report Card)",
                Description = "Form 137 or the Grade 11 Report Card.",
                GradeLevel = "SENIOR HIGH SCHOOL 12 ABM (1ST SEM)",
                IsRequired = true
            },
            new Requirement
            {
                Id = 17,
                RequirementName = "PSA Birth Certificate",
                Description = "A valid PSA-certified Birth Certificate.",
                GradeLevel = "SENIOR HIGH SCHOOL 12 ABM (1ST SEM)",
                IsRequired = true
            },
            new Requirement
            {
                Id = 18,
                RequirementName = "Certificate of Good Moral",
                Description = "Certificate of Good Moral Character from previous school.",
                GradeLevel = "SENIOR HIGH SCHOOL 12 ABM (1ST SEM)",
                IsRequired = true
            },

            // SENIOR HIGH SCHOOL 12 ABM (2ND SEM)
            new Requirement
            {
                Id = 19,
                RequirementName = "Form 137 (Grade 11 Report Card)",
                Description = "Form 137 or the Grade 11 Report Card.",
                GradeLevel = "SENIOR HIGH SCHOOL 12 ABM (2ND SEM)",
                IsRequired = true
            },
            new Requirement
            {
                Id = 20,
                RequirementName = "PSA Birth Certificate",
                Description = "A valid PSA-certified Birth Certificate.",
                GradeLevel = "SENIOR HIGH SCHOOL 12 ABM (2ND SEM)",
                IsRequired = true
            },
            new Requirement
            {
                Id = 21,
                RequirementName = "Certificate of Good Moral",
                Description = "Certificate of Good Moral Character from previous school.",
                GradeLevel = "SENIOR HIGH SCHOOL 12 ABM (2ND SEM)",
                IsRequired = true
            },

            // SENIOR HIGH SCHOOL 11 HUMSS (1ST SEM)
            new Requirement
            {
                Id = 22,
                RequirementName = "Form 137 (Grade 10 Report Card)",
                Description = "Form 137 or the Grade 10 Report Card.",
                GradeLevel = "SENIOR HIGH SCHOOL 11 HUMSS (1ST SEM)",
                IsRequired = true
            },
            new Requirement
            {
                Id = 23,
                RequirementName = "PSA Birth Certificate",
                Description = "A valid PSA-certified Birth Certificate.",
                GradeLevel = "SENIOR HIGH SCHOOL 11 HUMSS (1ST SEM)",
                IsRequired = true
            },
            new Requirement
            {
                Id = 24,
                RequirementName = "Certificate of Good Moral",
                Description = "Certificate of Good Moral Character from previous school.",
                GradeLevel = "SENIOR HIGH SCHOOL 11 HUMSS (1ST SEM)",
                IsRequired = true
            },

            // SENIOR HIGH SCHOOL 11 HUMSS (2ND SEM)
            new Requirement
            {
                Id = 25,
                RequirementName = "Form 137 (Grade 10 Report Card)",
                Description = "Form 137 or the Grade 10 Report Card.",
                GradeLevel = "SENIOR HIGH SCHOOL 11 HUMSS (2ND SEM)",
                IsRequired = true
            },
            new Requirement
            {
                Id = 26,
                RequirementName = "PSA Birth Certificate",
                Description = "A valid PSA-certified Birth Certificate.",
                GradeLevel = "SENIOR HIGH SCHOOL 11 HUMSS (2ND SEM)",
                IsRequired = true
            },
            new Requirement
            {
                Id = 27,
                RequirementName = "Certificate of Good Moral",
                Description = "Certificate of Good Moral Character from previous school.",
                GradeLevel = "SENIOR HIGH SCHOOL 11 HUMSS (2ND SEM)",
                IsRequired = true
            },

            // SENIOR HIGH SCHOOL 12 HUMSS (1ST SEM)
            new Requirement
            {
                Id = 28,
                RequirementName = "Form 137 (Grade 11 Report Card)",
                Description = "Form 137 or the Grade 11 Report Card.",
                GradeLevel = "SENIOR HIGH SCHOOL 12 HUMSS (1ST SEM)",
                IsRequired = true
            },
            new Requirement
            {
                Id = 29,
                RequirementName = "PSA Birth Certificate",
                Description = "A valid PSA-certified Birth Certificate.",
                GradeLevel = "SENIOR HIGH SCHOOL 12 HUMSS (1ST SEM)",
                IsRequired = true
            },
            new Requirement
            {
                Id = 30,
                RequirementName = "Certificate of Good Moral",
                Description = "Certificate of Good Moral Character from previous school.",
                GradeLevel = "SENIOR HIGH SCHOOL 12 HUMSS (1ST SEM)",
                IsRequired = true
            },

            new Requirement
            {
                Id = 31,
                RequirementName = "Form 137 (Grade 11 Report Card)",
                Description = "Form 137 or the Grade 11 Report Card.",
                GradeLevel = "SENIOR HIGH SCHOOL 12 HUMSS (2ND SEM)",
                IsRequired = true
            },
            new Requirement
            {
                Id = 32,
                RequirementName = "PSA Birth Certificate",
                Description = "A valid PSA-certified Birth Certificate.",
                GradeLevel = "SENIOR HIGH SCHOOL 12 HUMSS (2ND SEM)",
                IsRequired = true
            },
            new Requirement
            {
                Id = 33,
                RequirementName = "Certificate of Good Moral",
                Description = "Certificate of Good Moral Character from previous school.",
                GradeLevel = "SENIOR HIGH SCHOOL 12 HUMSS (2ND SEM)",
                IsRequired = true
            }
        );

        // modelBuilder.Entity<Enrollment>().HasData(
        //     new Enrollment
        //     {
        //         EnrollmentId = 1,
        //         Surname = "Test",
        //         Firstname = "Test",
        //         Middlename = "Test",
        //         Gender = "Male",
        //         GradeLevel = "NURSERY",
        //         Email = "jdmaster888@gmail.com",
        //         DateOfBirth = DateTime.Now,
        //         Age = 20,
        //         Address = "Parian Laguna",
        //         LRN = "232234324324",
        //         // FatherLastName = "Test",
        //         // FatherFirstName = "Test",
        //         // FatherOccupation = "Test",
        //         // MotherLastName = "Test",
        //         // MotherFirstName = "Test",
        //         // MotherOccupation = "Test",
        //         // MotherMaidenName = "Test",
        //         TemporaryUsername = "Test",
        //         TemporaryPassword = "Test",
        //         Username = "Test",
        //         Password = "Test",
        //     }
        // );
        // modelBuilder.Entity<RequirementModel>().HasData(
        //     new RequirementModel { Id = 1, RequirementName = "Birth Certificate", Description = "A valid copy of the Birth Certificate", GradeLevel = "NURSERY", IsRequired = true, EnrollmentId = 1 },
        //     new RequirementModel { Id = 2, RequirementName = "Report Card", Description = "Last year’s report card", GradeLevel = "KINDER", IsRequired = true, EnrollmentId = 1 },
        //     new RequirementModel { Id = 3, RequirementName = "Medical Certificate", Description = "A current medical certificate", GradeLevel = "ELEMENTARY", IsRequired = true, EnrollmentId = 1 },
        //     new RequirementModel { Id = 4, RequirementName = "Form 9", Description = "Form 9", GradeLevel = "JUNIOR HIGH SCHOOL", IsRequired = true, EnrollmentId = 1 },
        //     new RequirementModel { Id = 5, RequirementName = "PSA", Description = "PSA Birth Certificate", GradeLevel = "SENIOR HIGH SCHOOL", IsRequired = true, EnrollmentId = 1 }
        // );
    }

    //DbSets
    public required DbSet<Inquiry> Inquiries { get; set; }
    public required DbSet<Enrollment> Students { get; set; }
    public required DbSet<EnrollmentSchedule> EnrollmentSchedules { get; set; }
    public required DbSet<PaymentSchedule> PaymentSchedules { get; set; }
    public required DbSet<Notification> Notifications { get; set; }
    public required DbSet<FeeModel> Fees { get; set; }
    public required DbSet<Payment> Payments { get; set; }

    public required DbSet<Account> Accounts { get; set; }
    public required DbSet<Requirement> Requirements { get; set; }
    public required DbSet<RequirementModel> RequirementModels { get; set; }
    public required DbSet<RecentActivity> RecentActivities { get; set; }
    
}
