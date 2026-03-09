using DebtDash.Web.Api;
using DebtDash.Web.Domain.Calculations;
using DebtDash.Web.Domain.Services;
using DebtDash.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<DebtDashDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DebtDash")));

// Validation
builder.Services.AddValidationPipeline();

// Domain services
builder.Services.AddSingleton<IFinancialCalculationService, FinancialCalculationService>();
builder.Services.AddScoped<IRateVarianceService, RateVarianceService>();
builder.Services.AddScoped<ILoanProfileService, LoanProfileService>();
builder.Services.AddScoped<IPaymentLedgerService, PaymentLedgerService>();
builder.Services.AddScoped<IProjectionService, ProjectionService>();
builder.Services.AddScoped<IDashboardAggregationService, DashboardAggregationService>();

var spaPath = Path.Combine(builder.Environment.ContentRootPath, "ClientApp", "dist");

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DebtDashDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseGlobalErrorHandling();

// Map API routes
var api = app.MapGroup("/api");
api.MapGroup("/loan").MapLoanEndpoints();
api.MapGroup("/payments").MapPaymentEndpoints();
api.MapGroup("/dashboard").MapDashboardEndpoints();
api.MapGroup("/projections").MapProjectionEndpoints();

// Serve SPA static files from ClientApp/dist
if (Directory.Exists(spaPath))
{
    var fileProvider = new PhysicalFileProvider(spaPath);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
    app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = fileProvider });
}

app.Run();

// Make Program accessible for WebApplicationFactory in integration tests
public partial class Program;

