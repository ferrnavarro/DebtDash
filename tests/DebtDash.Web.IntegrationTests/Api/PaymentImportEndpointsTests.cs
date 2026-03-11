using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DebtDash.Web.Api.Contracts;
using DebtDash.Web.IntegrationTests.TestInfrastructure;
using FluentAssertions;

namespace DebtDash.Web.IntegrationTests.Api;

public class PaymentImportEndpointsTests : IDisposable
{
    private readonly DebtDashWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public PaymentImportEndpointsTests()
    {
        _factory = new DebtDashWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<Guid> SeedLoanAndGetId()
    {
        var loan = new LoanProfileUpsertRequest(
            InitialPrincipal: 200000m,
            AnnualRate: 5.5m,
            TermMonths: 360,
            StartDate: new DateOnly(2024, 1, 15),
            FixedMonthlyCosts: 50m,
            CurrencyCode: "USD");

        var response = await _client.PutAsJsonAsync("/api/loan", loan);
        response.EnsureSuccessStatusCode();

        var getResponse = await _client.GetAsync("/api/loan");
        var loanProfile = await getResponse.Content.ReadFromJsonAsync<LoanProfileResponse>(JsonOptions);
        return loanProfile!.Id;
    }

    private static MultipartFormDataContent MakeCsvFormData(string csv, string filename = "payments.csv")
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", filename);
        return content;
    }

    private static string MakeHeaderRow() =>
        "LoanId,PaymentDate,TotalPaid,PrincipalPaid,InterestPaid,FeesPaid";

    private static string MakeDataRow(Guid loanId, string date, decimal total = 1500m,
        decimal principal = 1200m, decimal interest = 280m, decimal fees = 20m) =>
        $"{loanId},{date},{total},{principal},{interest},{fees}";

    // ── GET /api/payments/import/template ─────────────────────────────────────

    [Fact]
    public async Task Get_template_returns_csv_file()
    {
        var response = await _client.GetAsync("/api/payments/import/template");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var body = await response.Content.ReadAsStringAsync();
        var lines = body.Split('\n');
        lines.Length.Should().BeGreaterThanOrEqualTo(2);
        lines[0].Should().Contain("LoanId");
        lines[0].Should().Contain("PaymentDate");
    }

    [Fact]
    public async Task Get_template_sets_content_disposition_attachment()
    {
        var response = await _client.GetAsync("/api/payments/import/template");

        response.Content.Headers.ContentDisposition.Should().NotBeNull();
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        response.Content.Headers.ContentDisposition.FileName.Should().Contain("template");
    }

    // ── POST /api/payments/import/validate ────────────────────────────────────

    [Fact]
    public async Task Validate_returns_400_when_no_file_uploaded()
    {
        var content = new MultipartFormDataContent();
        var response = await _client.PostAsync("/api/payments/import/validate", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Validate_returns_400_for_non_csv_extension()
    {
        var formData = MakeCsvFormData("some content", "data.xlsx");
        var response = await _client.PostAsync("/api/payments/import/validate", formData);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(".csv");
    }

    [Fact]
    public async Task Validate_returns_preview_with_valid_rows()
    {
        var loanId = await SeedLoanAndGetId();

        var csv = string.Join("\n",
            MakeHeaderRow(),
            MakeDataRow(loanId, "2024-02-15"),
            MakeDataRow(loanId, "2024-03-15"));

        var formData = MakeCsvFormData(csv);
        var response = await _client.PostAsync("/api/payments/import/validate", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await response.Content.ReadFromJsonAsync<ImportPreviewResponse>(JsonOptions);
        preview.Should().NotBeNull();
        preview!.TotalRows.Should().Be(2);
        preview.ValidCount.Should().Be(2);
        preview.InvalidCount.Should().Be(0);
    }

    [Fact]
    public async Task Validate_returns_invalid_row_for_unknown_loan_id()
    {
        await SeedLoanAndGetId();

        var unknownId = Guid.NewGuid();
        var csv = string.Join("\n",
            MakeHeaderRow(),
            MakeDataRow(unknownId, "2024-02-15"));

        var formData = MakeCsvFormData(csv);
        var response = await _client.PostAsync("/api/payments/import/validate", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await response.Content.ReadFromJsonAsync<ImportPreviewResponse>(JsonOptions);
        preview!.ValidCount.Should().Be(0);
        preview.InvalidCount.Should().Be(1);
    }

    // ── POST /api/payments/import/confirm ─────────────────────────────────────

    [Fact]
    public async Task Confirm_imports_valid_rows_and_returns_summary()
    {
        var loanId = await SeedLoanAndGetId();

        var rows = new List<CsvPaymentRow>
        {
            new(1, loanId, new DateOnly(2024, 2, 15), 1500m, 1200m, 280m, 20m),
            new(2, loanId, new DateOnly(2024, 3, 15), 1500m, 1210m, 270m, 20m)
        };

        var response = await _client.PostAsJsonAsync("/api/payments/import/confirm",
            new ImportConfirmRequest(rows), JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ImportConfirmResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.ImportedCount.Should().Be(2);
        result.SkippedCount.Should().Be(0);
        result.SkippedRows.Should().BeEmpty();
    }

    [Fact]
    public async Task Confirm_skips_duplicate_rows()
    {
        var loanId = await SeedLoanAndGetId();

        // Insert first row directly via API
        var firstRow = new List<CsvPaymentRow>
        {
            new(1, loanId, new DateOnly(2024, 2, 15), 1500m, 1200m, 280m, 20m)
        };
        var firstResponse = await _client.PostAsJsonAsync("/api/payments/import/confirm",
            new ImportConfirmRequest(firstRow), JsonOptions);
        firstResponse.EnsureSuccessStatusCode();

        // Now try to import the same row again plus a new row
        var secondRows = new List<CsvPaymentRow>
        {
            new(1, loanId, new DateOnly(2024, 2, 15), 1500m, 1200m, 280m, 20m), // duplicate
            new(2, loanId, new DateOnly(2024, 3, 15), 1500m, 1210m, 270m, 20m)  // new
        };
        var secondResponse = await _client.PostAsJsonAsync("/api/payments/import/confirm",
            new ImportConfirmRequest(secondRows), JsonOptions);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await secondResponse.Content.ReadFromJsonAsync<ImportConfirmResponse>(JsonOptions);
        result!.ImportedCount.Should().Be(1);
        result.SkippedCount.Should().Be(1);
        result.SkippedRows.Should().HaveCount(1);
    }

    [Fact]
    public async Task Confirm_returns_400_when_rows_empty()
    {
        var response = await _client.PostAsJsonAsync("/api/payments/import/confirm",
            new ImportConfirmRequest([]), JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Full_flow_validate_then_confirm_imports_all_valid_rows()
    {
        var loanId = await SeedLoanAndGetId();

        var csv = string.Join("\n",
            MakeHeaderRow(),
            MakeDataRow(loanId, "2024-02-15"),
            MakeDataRow(loanId, "2024-03-20"),
            MakeDataRow(loanId, "2024-04-18"));

        // Step 1: Validate
        var validateResponse = await _client.PostAsync(
            "/api/payments/import/validate", MakeCsvFormData(csv));
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await validateResponse.Content.ReadFromJsonAsync<ImportPreviewResponse>(JsonOptions);
        preview!.ValidCount.Should().Be(3);

        // Step 2: Confirm the valid rows
        var confirmResponse = await _client.PostAsJsonAsync("/api/payments/import/confirm",
            new ImportConfirmRequest(preview.ValidRows), JsonOptions);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await confirmResponse.Content.ReadFromJsonAsync<ImportConfirmResponse>(JsonOptions);
        result!.ImportedCount.Should().Be(3);

        // Step 3: Verify payments appear in the ledger
        var listResponse = await _client.GetAsync("/api/payments");
        var list = await listResponse.Content.ReadFromJsonAsync<PaymentListDto>(JsonOptions);
        list!.TotalItems.Should().Be(3);
    }

    // ── Performance: 500 rows ─────────────────────────────────────────────────

    [Fact]
    public async Task Validate_handles_500_rows_within_reasonable_time()
    {
        var loanId = await SeedLoanAndGetId();

        var csvRows = Enumerable.Range(0, 500)
            .Select(i => MakeDataRow(loanId, new DateOnly(2023, 1, 1).AddDays(i).ToString("yyyy-MM-dd")));
        var csv = string.Join("\n", new[] { MakeHeaderRow() }.Concat(csvRows));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _client.PostAsync("/api/payments/import/validate", MakeCsvFormData(csv));
        stopwatch.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000,
            "validating 500 rows should complete in under 5 seconds");
    }

    // ── DTOs for deserialization ───────────────────────────────────────────────
    private record PaymentListDto(List<object> Items, int Page, int PageSize, int TotalItems);
}
