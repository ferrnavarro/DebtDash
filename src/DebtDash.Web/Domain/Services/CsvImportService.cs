using DebtDash.Web.Api.Contracts;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace DebtDash.Web.Domain.Services;

public interface ICsvImportService
{
    Task<(ImportPreviewResponse Preview, string? FileError)> ParseAndValidateAsync(
        IFormFile file, IReadOnlySet<Guid> validLoanIds);
    string GenerateTemplate();
}

public class CsvImportService : ICsvImportService
{
    private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB
    private const int MaxDataRows = 500;

    public static readonly string[] RequiredHeaders =
        ["LoanId", "PaymentDate", "TotalPaid", "PrincipalPaid", "InterestPaid", "FeesPaid"];

    public async Task<(ImportPreviewResponse Preview, string? FileError)> ParseAndValidateAsync(
        IFormFile file, IReadOnlySet<Guid> validLoanIds)
    {
        if (file.Length > MaxFileSizeBytes)
            return (Empty(), "File exceeds the 2 MB maximum size limit.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".csv")
            return (Empty(), $"Only .csv files are accepted. Received file with extension '{ext}'.");

        List<string> lines = [];
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) is not null)
                lines.Add(line);
        }

        if (lines.Count == 0)
            return (Empty(), "The uploaded file is empty.");

        var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
        foreach (var required in RequiredHeaders)
        {
            if (!headers.Any(h => h.Equals(required, StringComparison.OrdinalIgnoreCase)))
                return (Empty(), $"Missing required column '{required}'. Required columns: {string.Join(", ", RequiredHeaders)}.");
        }

        var colIdx = headers
            .Select((h, i) => (h, i))
            .ToDictionary(t => t.h, t => t.i, StringComparer.OrdinalIgnoreCase);

        var dataLines = lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

        if (dataLines.Count > MaxDataRows)
            return (Empty(), $"File exceeds the {MaxDataRows} data row limit. Found {dataLines.Count} rows.");

        if (dataLines.Count == 0)
            return (new ImportPreviewResponse(0, 0, 0, [], []), null);

        var validRows = new List<CsvPaymentRow>();
        var invalidRows = new List<CsvRowError>();

        for (int i = 0; i < dataLines.Count; i++)
        {
            var rowIndex = i + 1;
            var fields = dataLines[i].Split(',');
            var errors = new List<string>();

            string Field(string name) =>
                colIdx.TryGetValue(name, out var idx) && idx < fields.Length
                    ? fields[idx].Trim()
                    : string.Empty;

            // LoanId
            var loanIdStr = Field("LoanId");
            Guid loanId = Guid.Empty;
            if (string.IsNullOrEmpty(loanIdStr))
                errors.Add("LoanId is required.");
            else if (!Guid.TryParse(loanIdStr, out loanId))
                errors.Add($"LoanId '{loanIdStr}' is not a valid UUID.");
            else if (!validLoanIds.Contains(loanId))
                errors.Add($"LoanId '{loanId}' does not match any existing loan.");

            // PaymentDate
            var dateStr = Field("PaymentDate");
            DateOnly paymentDate = default;
            if (string.IsNullOrEmpty(dateStr))
                errors.Add("PaymentDate is required.");
            else if (!DateOnly.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out paymentDate))
                errors.Add($"PaymentDate '{dateStr}' must be in YYYY-MM-DD format.");

            // TotalPaid
            var totalStr = Field("TotalPaid");
            decimal totalPaid = 0;
            if (string.IsNullOrEmpty(totalStr))
                errors.Add("TotalPaid is required.");
            else if (!decimal.TryParse(totalStr, NumberStyles.Number, CultureInfo.InvariantCulture, out totalPaid))
                errors.Add($"TotalPaid '{totalStr}' is not a valid number.");
            else if (totalPaid <= 0)
                errors.Add("TotalPaid must be greater than 0.");

            // PrincipalPaid
            var principalStr = Field("PrincipalPaid");
            decimal principalPaid = 0;
            if (string.IsNullOrEmpty(principalStr))
                errors.Add("PrincipalPaid is required.");
            else if (!decimal.TryParse(principalStr, NumberStyles.Number, CultureInfo.InvariantCulture, out principalPaid))
                errors.Add($"PrincipalPaid '{principalStr}' is not a valid number.");
            else if (principalPaid < 0)
                errors.Add("PrincipalPaid cannot be negative.");

            // InterestPaid
            var interestStr = Field("InterestPaid");
            decimal interestPaid = 0;
            if (string.IsNullOrEmpty(interestStr))
                errors.Add("InterestPaid is required.");
            else if (!decimal.TryParse(interestStr, NumberStyles.Number, CultureInfo.InvariantCulture, out interestPaid))
                errors.Add($"InterestPaid '{interestStr}' is not a valid number.");
            else if (interestPaid < 0)
                errors.Add("InterestPaid cannot be negative.");

            // FeesPaid
            var feesStr = Field("FeesPaid");
            decimal feesPaid = 0;
            if (string.IsNullOrEmpty(feesStr))
                errors.Add("FeesPaid is required.");
            else if (!decimal.TryParse(feesStr, NumberStyles.Number, CultureInfo.InvariantCulture, out feesPaid))
                errors.Add($"FeesPaid '{feesStr}' is not a valid number.");
            else if (feesPaid < 0)
                errors.Add("FeesPaid cannot be negative.");

            if (errors.Count > 0)
                invalidRows.Add(new CsvRowError(rowIndex, errors));
            else
                validRows.Add(new CsvPaymentRow(rowIndex, loanId, paymentDate,
                    totalPaid, principalPaid, interestPaid, feesPaid));
        }

        return (new ImportPreviewResponse(
            dataLines.Count, validRows.Count, invalidRows.Count, validRows, invalidRows), null);
    }

    public string GenerateTemplate()
    {
        var exampleDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
        var example = $"00000000-0000-0000-0000-000000000000,{exampleDate:yyyy-MM-dd},1500.00,1200.00,280.00,20.00";
        return $"{string.Join(",", RequiredHeaders)}\n{example}";
    }

    private static ImportPreviewResponse Empty() => new(0, 0, 0, [], []);
}
