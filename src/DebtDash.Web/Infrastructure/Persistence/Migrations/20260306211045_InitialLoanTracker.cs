using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DebtDash.Web.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialLoanTracker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoanProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InitialPrincipal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualRate = table.Column<decimal>(type: "decimal(10,6)", nullable: false),
                    TermMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    FixedMonthlyCosts = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LoanProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TotalPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrincipalPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InterestPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FeesPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DaysSincePreviousPayment = table.Column<int>(type: "INTEGER", nullable: false),
                    RemainingBalanceAfterPayment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CalculatedRealRate = table.Column<decimal>(type: "decimal(10,6)", nullable: false),
                    ManualRateOverrideEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ManualRateOverride = table.Column<decimal>(type: "decimal(10,6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentLogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentLogEntries_LoanProfiles_LoanProfileId",
                        column: x => x.LoanProfileId,
                        principalTable: "LoanProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectionSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LoanProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AsOfPaymentLogEntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PredictedEndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    RemainingMonthsEstimate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PrincipalVelocity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    BaselineRemainingMonths = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DeltaMonthsVsBaseline = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectionSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectionSnapshots_LoanProfiles_LoanProfileId",
                        column: x => x.LoanProfileId,
                        principalTable: "LoanProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RateVarianceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentLogEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CalculatedRate = table.Column<decimal>(type: "decimal(10,6)", nullable: false),
                    StatedOrOverrideRate = table.Column<decimal>(type: "decimal(10,6)", nullable: true),
                    VarianceAbsolute = table.Column<decimal>(type: "decimal(10,6)", nullable: false),
                    VarianceBasisPoints = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    IsFlagged = table.Column<bool>(type: "INTEGER", nullable: false),
                    ThresholdBasisPoints = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateVarianceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RateVarianceRecords_PaymentLogEntries_PaymentLogEntryId",
                        column: x => x.PaymentLogEntryId,
                        principalTable: "PaymentLogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLogEntries_LoanProfileId_PaymentDate",
                table: "PaymentLogEntries",
                columns: new[] { "LoanProfileId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectionSnapshots_LoanProfileId",
                table: "ProjectionSnapshots",
                column: "LoanProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_RateVarianceRecords_PaymentLogEntryId",
                table: "RateVarianceRecords",
                column: "PaymentLogEntryId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectionSnapshots");

            migrationBuilder.DropTable(
                name: "RateVarianceRecords");

            migrationBuilder.DropTable(
                name: "PaymentLogEntries");

            migrationBuilder.DropTable(
                name: "LoanProfiles");
        }
    }
}
