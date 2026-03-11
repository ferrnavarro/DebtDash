using DebtDash.Web.Domain.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace DebtDash.Web.UnitTests.Domain;

public class CsvImportServiceTests
{
    private readonly CsvImportService _sut = new();

    private static readonly Guid KnownLoanId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly IReadOnlySet<Guid> ValidLoanIds = new HashSet<Guid> { KnownLoanId };

    private static readonly string HeaderRow =
        "LoanId,PaymentDate,TotalPaid,PrincipalPaid,InterestPaid,FeesPaid";

    private static string MakeDataRow(
        string loanId = "11111111-1111-1111-1111-111111111111",
        string date = "2024-02-15",
        string total = "1500.00",
        string principal = "1200.00",
        string interest = "280.00",
        string fees = "20.00") =>
        $"{loanId},{date},{total},{principal},{interest},{fees}";

    private static IFormFile MakeCsvFile(string content, string fileName = "test.csv")
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };
        return file;
    }

    // ── GenerateTemplate ──────────────────────────────────────────────────────

    [Fact]
    public void GenerateTemplate_returns_header_and_example_row()
    {
        var result = _sut.GenerateTemplate();

        var lines = result.Split('\n');
        lines.Should().HaveCount(2);
        lines[0].Should().Be("LoanId,PaymentDate,TotalPaid,PrincipalPaid,InterestPaid,FeesPaid");
        lines[1].Should().Contain(",");
    }

    [Fact]
    public void GenerateTemplate_example_row_has_correct_column_count()
    {
        var result = _sut.GenerateTemplate();
        var dataRow = result.Split('\n')[1];
        dataRow.Split(',').Should().HaveCount(6);
    }

    // ── File-level guards ─────────────────────────────────────────────────────

    [Fact]
    public async Task ParseAndValidate_rejects_non_csv_extension()
    {
        var file = MakeCsvFile($"{HeaderRow}\n{MakeDataRow()}", "payments.xlsx");

        var (_, error) = await _sut.ParseAndValidateAsync(file, ValidLoanIds);

        error.Should().NotBeNull();
        error.Should().Contain(".csv");
    }

    [Fact]
    public async Task ParseAndValidate_rejects_empty_file()
    {
        var file = MakeCsvFile("", "test.csv");

        var (_, error) = await _sut.ParseAndValidateAsync(file, ValidLoanIds);

        error.Should().NotBeNull();
        error.Should().Contain("empty");
    }

    [Fact]
    public async Task ParseAndValidate_rejects_file_exceeding_size_limit()
    {
        var bigBytes = new byte[2 * 1024 * 1024 + 1];
        var stream = new MemoryStream(bigBytes);
        var file = new FormFile(stream, 0, bigBytes.Length, "file", "big.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };

        var (_, error) = await _sut.ParseAndValidateAsync(file, ValidLoanIds);

        error.Should().NotBeNull();
        error.Should().Contain("2 MB");
    }

    [Fact]
    public async Task ParseAndValidate_rejects_file_missing_required_column()
    {
        var csv = "LoanId,PaymentDate,TotalPaid,PrincipalPaid\n" +
                  $"{KnownLoanId},2024-02-15,1500,1200";
        var file = MakeCsvFile(csv);

        var (_, error) = await _sut.ParseAndValidateAsync(file, ValidLoanIds);

        error.Should().NotBeNull();
        error.Should().Contain("InterestPaid");
    }

    [Fact]
    public async Task ParseAndValidate_rejects_file_exceeding_row_limit()
    {
        var rows = Enumerable.Range(1, 501).Select(_ => MakeDataRow());
        var csv = string.Join("\n", new[] { HeaderRow }.Concat(rows));
        var file = MakeCsvFile(csv);

        var (_, error) = await _sut.ParseAndValidateAsync(file, ValidLoanIds);

        error.Should().NotBeNull();
        error.Should().Contain("500");
    }

    // ── Header-only file (zero data rows) ────────────────────────────────────

    [Fact]
    public async Task ParseAndValidate_returns_zero_rows_for_header_only_file()
    {
        var file = MakeCsvFile(HeaderRow);

        var (preview, error) = await _sut.ParseAndValidateAsync(file, ValidLoanIds);

        error.Should().BeNull();
        preview.TotalRows.Should().Be(0);
        preview.ValidCount.Should().Be(0);
        preview.InvalidCount.Should().Be(0);
    }

    // ── Row-level validation ──────────────────────────────────────────────────

    [Fact]
    public async Task ParseAndValidate_valid_row_appears_in_valid_list()
    {
        var csv = $"{HeaderRow}\n{MakeDataRow()}";
        var file = MakeCsvFile(csv);

        var (preview, error) = await _sut.ParseAndValidateAsync(file, ValidLoanIds);

        error.Should().BeNull();
        preview.TotalRows.Should().Be(1);
        preview.ValidCount.Should().Be(1);
        preview.InvalidCount.Should().Be(0);
        preview.ValidRows[0].LoanId.Should().Be(KnownLoanId);
        preview.ValidRows[0].TotalPaid.Should().Be(1500.00m);
    }

    [Fact]
    public async Task ParseAndValidate_unknown_loan_id_row_is_invalid()
    {
        var unknownId = Guid.NewGuid();
        var csv = $"{HeaderRow}\n{MakeDataRow(loanId: unknownId.ToString())}";
        var file = MakeCsvFile(csv);

        var (preview, error) = await _sut.ParseAndValidateAsync(file, ValidLoanIds);

        error.Should().BeNull();
        preview.InvalidCount.Should().Be(1);
        preview.InvalidRows[0].Errors.Should().ContainMatch("*does not match*");
    }

    [Fact]
    public async Task ParseAndValidate_invalid_date_format_row_is_invalid()
    {
        var csv = $"{HeaderRow}\n{MakeDataRow(date: "15/02/2024")}";
        var file = MakeCsvFile(csv);

        var (preview, error) = await _sut.ParseAndValidateAsync(file, ValidLoanIds);

        error.Should().BeNull();
        preview.InvalidCount.Should().Be(1);
        preview.InvalidRows[0].Errors.Should().ContainMatch("*YYYY-MM-DD*");
    }

    [Fact]
    public async Task ParseAndValidate_zero_total_paid_row_is_invalid()
    {
        var csv = $"{HeaderRow}\n{MakeDataRow(total: "0")}";
        var file = MakeCsvFile(csv);

        var (preview, error) = await _sut.ParseAndValidateAsync(file, ValidLoanIds);

        error.Should().BeNull();
        preview.InvalidCount.Should().Be(1);
        preview.InvalidRows[0].Errors.Should().ContainMatch("*greater than 0*");
    }

    [Fact]
    public async Task ParseAndValidate_negative_principal_row_is_invalid()
    {
        var csv = $"{HeaderRow}\n{MakeDataRow(principal: "-100")}";
        var file = MakeCsvFile(csv);

        var (preview, error) = await _sut.ParseAndValidateAsync(file, ValidLoanIds);

        error.Should().BeNull();
        preview.InvalidCount.Should().Be(1);
        preview.InvalidRows[0].Errors.Should().ContainMatch("*negative*");
    }

    [Fact]
    public async Task ParseAndValidate_mixed_valid_and_invalid_rows()
    {
        var csv = string.Join("\n",
            HeaderRow,
            MakeDataRow(),                             // row 1 – valid
            MakeDataRow(loanId: "not-a-guid"),         // row 2 – invalid
            MakeDataRow(date: "2024-03-20"),            // row 3 – valid
            MakeDataRow(total: "-50"));                 // row 4 – invalid

        var file = MakeCsvFile(csv);

        var (preview, error) = await _sut.ParseAndValidateAsync(file, ValidLoanIds);

        error.Should().BeNull();
        preview.TotalRows.Should().Be(4);
        preview.ValidCount.Should().Be(2);
        preview.InvalidCount.Should().Be(2);
    }

    [Fact]
    public async Task ParseAndValidate_header_matching_is_case_insensitive()
    {
        var csv = $"loanid,paymentdate,totalpaid,principalpaid,interestpaid,feespaid\n{MakeDataRow()}";
        var file = MakeCsvFile(csv);

        var (preview, error) = await _sut.ParseAndValidateAsync(file, ValidLoanIds);

        error.Should().BeNull();
        preview.ValidCount.Should().Be(1);
    }
}
