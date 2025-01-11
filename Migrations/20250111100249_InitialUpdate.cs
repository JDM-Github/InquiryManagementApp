using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InquiryManagementApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConfirmPassword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "EnrollmentSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fee = table.Column<double>(type: "float", nullable: false),
                    PaymentType = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inquiries",
                columns: table => new
                {
                    InquiryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GuardianName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceOfInformation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    IsRejected = table.Column<bool>(type: "bit", nullable: false),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CancellationNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inquiries", x => x.InquiryId);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaidAmount = table.Column<double>(type: "float", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentLink = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EnrollreesId = table.Column<int>(type: "int", nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrentPaymentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecentActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Activity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecentActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Requirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequirementName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedFile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    GradeLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRejected = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    EnrollmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Surname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Firstname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Middlename = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GradeLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LRN = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FatherLastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FatherFirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FatherOccupation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MotherLastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MotherFirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MotherOccupation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MotherMaidenName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GoodMoralCertificate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedEnrolled = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApproveId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnrolled = table.Column<bool>(type: "bit", nullable: false),
                    EnrolledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRejected = table.Column<bool>(type: "bit", nullable: false),
                    FeePaid = table.Column<double>(type: "float", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TemporaryUsername = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemporaryPassword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsWalkin = table.Column<bool>(type: "bit", nullable: false),
                    HaveSiblingInSchool = table.Column<bool>(type: "bit", nullable: false),
                    NumberOfSibling = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.EnrollmentId);
                });

            migrationBuilder.CreateTable(
                name: "RequirementModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequirementName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedFile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    GradeLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
                    IsRejected = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequirementModels_Students_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Students",
                        principalColumn: "EnrollmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentPaymentRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PaymentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Balance = table.Column<double>(type: "float", nullable: false),
                    CashDiscount = table.Column<bool>(type: "bit", nullable: false),
                    EarlyBird = table.Column<bool>(type: "bit", nullable: false),
                    SiblingDiscount = table.Column<int>(type: "int", nullable: false),
                    PerPayment = table.Column<double>(type: "float", nullable: true),
                    PaymentId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentPaymentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentPaymentRecords_Students_UserId",
                        column: x => x.UserId,
                        principalTable: "Students",
                        principalColumn: "EnrollmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentAmount = table.Column<double>(type: "float", nullable: false),
                    MonthPaid = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YearPaid = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentFor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentPayments_Students_UserId",
                        column: x => x.UserId,
                        principalTable: "Students",
                        principalColumn: "EnrollmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Fees",
                columns: new[] { "Id", "Fee", "Level", "PaymentType" },
                values: new object[,]
                {
                    { 1, 19000.0, "NURSERY", "Cash" },
                    { 2, 19000.0, "KINDER", "Cash" },
                    { 3, 19000.0, "ELEMENTARY", "Cash" },
                    { 4, 19000.0, "JUNIOR HIGH SCHOOL", "Installment" },
                    { 5, 19000.0, "SENIOR HIGH SCHOOL 11 ABM (1ST SEM)", "Installment" },
                    { 6, 19000.0, "SENIOR HIGH SCHOOL 11 ABM (2ND SEM)", "Installment" },
                    { 7, 19000.0, "SENIOR HIGH SCHOOL 12 ABM (1ST SEM)", "Installment" },
                    { 8, 19000.0, "SENIOR HIGH SCHOOL 12 ABM (2ND SEM)", "Installment" },
                    { 9, 19000.0, "SENIOR HIGH SCHOOL 11 HUMSS (1ST SEM)", "Installment" },
                    { 10, 19000.0, "SENIOR HIGH SCHOOL 11 HUMSS (2ND SEM)", "Installment" },
                    { 11, 19000.0, "SENIOR HIGH SCHOOL 12 HUMSS (1ST SEM)", "Installment" },
                    { 12, 19000.0, "SENIOR HIGH SCHOOL 12 HUMSS (2ND SEM)", "Installment" }
                });

            migrationBuilder.InsertData(
                table: "Requirements",
                columns: new[] { "Id", "Description", "GradeLevel", "IsApproved", "IsRejected", "IsRequired", "RequirementName", "UploadedFile" },
                values: new object[,]
                {
                    { 1, "A valid copy of the Birth Certificate.", "NURSERY", false, false, true, "Birth Certificate", "" },
                    { 2, "Last year’s report card.", "KINDER", false, false, true, "Report Card", "" },
                    { 3, "A valid copy of the Birth Certificate.", "KINDER", false, false, true, "Birth Certificate", "" },
                    { 4, "A current medical certificate.", "ELEMENTARY", false, false, true, "Medical Certificate", "" },
                    { 5, "Last year’s report card.", "ELEMENTARY", false, false, true, "Report Card", "" },
                    { 6, "A valid PSA-certified Birth Certificate.", "ELEMENTARY", false, false, true, "PSA Birth Certificate", "" },
                    { 7, "Form 138 (Report Card) for the last grade level.", "JUNIOR HIGH SCHOOL", false, false, true, "Form 138 (Report Card)", "" },
                    { 8, "A valid PSA-certified Birth Certificate.", "JUNIOR HIGH SCHOOL", false, false, true, "PSA Birth Certificate", "" },
                    { 9, "Certificate of Good Moral Character from previous school.", "JUNIOR HIGH SCHOOL", false, false, true, "Certificate of Good Moral", "" },
                    { 10, "Form 137 or the Grade 10 Report Card.", "SENIOR HIGH SCHOOL 11 ABM (1ST SEM)", false, false, true, "Form 137 (Grade 10 Report Card)", "" },
                    { 11, "A valid PSA-certified Birth Certificate.", "SENIOR HIGH SCHOOL 11 ABM (1ST SEM)", false, false, true, "PSA Birth Certificate", "" },
                    { 12, "Certificate of Good Moral Character from previous school.", "SENIOR HIGH SCHOOL 11 ABM (1ST SEM)", false, false, true, "Certificate of Good Moral", "" },
                    { 13, "Form 137 or the Grade 10 Report Card.", "SENIOR HIGH SCHOOL 11 ABM (2ND SEM)", false, false, true, "Form 137 (Grade 10 Report Card)", "" },
                    { 14, "A valid PSA-certified Birth Certificate.", "SENIOR HIGH SCHOOL 11 ABM (2ND SEM)", false, false, true, "PSA Birth Certificate", "" },
                    { 15, "Certificate of Good Moral Character from previous school.", "SENIOR HIGH SCHOOL 11 ABM (2ND SEM)", false, false, true, "Certificate of Good Moral", "" },
                    { 16, "Form 137 or the Grade 11 Report Card.", "SENIOR HIGH SCHOOL 12 ABM (1ST SEM)", false, false, true, "Form 137 (Grade 11 Report Card)", "" },
                    { 17, "A valid PSA-certified Birth Certificate.", "SENIOR HIGH SCHOOL 12 ABM (1ST SEM)", false, false, true, "PSA Birth Certificate", "" },
                    { 18, "Certificate of Good Moral Character from previous school.", "SENIOR HIGH SCHOOL 12 ABM (1ST SEM)", false, false, true, "Certificate of Good Moral", "" },
                    { 19, "Form 137 or the Grade 11 Report Card.", "SENIOR HIGH SCHOOL 12 ABM (2ND SEM)", false, false, true, "Form 137 (Grade 11 Report Card)", "" },
                    { 20, "A valid PSA-certified Birth Certificate.", "SENIOR HIGH SCHOOL 12 ABM (2ND SEM)", false, false, true, "PSA Birth Certificate", "" },
                    { 21, "Certificate of Good Moral Character from previous school.", "SENIOR HIGH SCHOOL 12 ABM (2ND SEM)", false, false, true, "Certificate of Good Moral", "" },
                    { 22, "Form 137 or the Grade 10 Report Card.", "SENIOR HIGH SCHOOL 11 HUMSS (1ST SEM)", false, false, true, "Form 137 (Grade 10 Report Card)", "" },
                    { 23, "A valid PSA-certified Birth Certificate.", "SENIOR HIGH SCHOOL 11 HUMSS (1ST SEM)", false, false, true, "PSA Birth Certificate", "" },
                    { 24, "Certificate of Good Moral Character from previous school.", "SENIOR HIGH SCHOOL 11 HUMSS (1ST SEM)", false, false, true, "Certificate of Good Moral", "" },
                    { 25, "Form 137 or the Grade 10 Report Card.", "SENIOR HIGH SCHOOL 11 HUMSS (2ND SEM)", false, false, true, "Form 137 (Grade 10 Report Card)", "" },
                    { 26, "A valid PSA-certified Birth Certificate.", "SENIOR HIGH SCHOOL 11 HUMSS (2ND SEM)", false, false, true, "PSA Birth Certificate", "" },
                    { 27, "Certificate of Good Moral Character from previous school.", "SENIOR HIGH SCHOOL 11 HUMSS (2ND SEM)", false, false, true, "Certificate of Good Moral", "" },
                    { 28, "Form 137 or the Grade 11 Report Card.", "SENIOR HIGH SCHOOL 12 HUMSS (1ST SEM)", false, false, true, "Form 137 (Grade 11 Report Card)", "" },
                    { 29, "A valid PSA-certified Birth Certificate.", "SENIOR HIGH SCHOOL 12 HUMSS (1ST SEM)", false, false, true, "PSA Birth Certificate", "" },
                    { 30, "Certificate of Good Moral Character from previous school.", "SENIOR HIGH SCHOOL 12 HUMSS (1ST SEM)", false, false, true, "Certificate of Good Moral", "" },
                    { 31, "Form 137 or the Grade 11 Report Card.", "SENIOR HIGH SCHOOL 12 HUMSS (2ND SEM)", false, false, true, "Form 137 (Grade 11 Report Card)", "" },
                    { 32, "A valid PSA-certified Birth Certificate.", "SENIOR HIGH SCHOOL 12 HUMSS (2ND SEM)", false, false, true, "PSA Birth Certificate", "" },
                    { 33, "Certificate of Good Moral Character from previous school.", "SENIOR HIGH SCHOOL 12 HUMSS (2ND SEM)", false, false, true, "Certificate of Good Moral", "" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequirementModels_EnrollmentId",
                table: "RequirementModels",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPaymentRecords_UserId",
                table: "StudentPaymentRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPayments_UserId",
                table: "StudentPayments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "EnrollmentSchedules");

            migrationBuilder.DropTable(
                name: "Fees");

            migrationBuilder.DropTable(
                name: "Inquiries");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PaymentSchedules");

            migrationBuilder.DropTable(
                name: "RecentActivities");

            migrationBuilder.DropTable(
                name: "RequirementModels");

            migrationBuilder.DropTable(
                name: "Requirements");

            migrationBuilder.DropTable(
                name: "StudentPaymentRecords");

            migrationBuilder.DropTable(
                name: "StudentPayments");

            migrationBuilder.DropTable(
                name: "Students");
        }
    }
}
