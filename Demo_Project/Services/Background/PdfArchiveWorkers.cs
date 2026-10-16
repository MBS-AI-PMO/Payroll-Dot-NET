using Microsoft.EntityFrameworkCore;
using PayslipMgtSystem.Models;
using PayslipMgtSystem.Services.Pdf;
using PayslipMgtSystem.Services.Pdf.Templates;
using PayslipMgtSystem.ViewModel;

namespace PayslipMgtSystem.Services.Background
{
    /// <summary>Archive folder (wwwroot/Downloaded Files) for generated PDFs. It is blocked from public access.</summary>
    public static class PdfArchive
    {
        public const string FolderName = "Downloaded Files";

        public static string Folder(IWebHostEnvironment env, params string[] subFolders)
        {
            var path = Path.Combine(new[] { env.WebRootPath, FolderName }.Concat(subFolders).ToArray());
            Directory.CreateDirectory(path);
            return path;
        }

        public static string SafeFileName(string name) =>
            string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
    }

    /// <summary>Generates and archives a payslip PDF for every newly uploaded payroll row.</summary>
    public class PayslipPdfWorker : BackgroundService
    {
        private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(8);
        private static readonly TimeSpan ErrorDelay = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<PayslipPdfWorker> _logger;

        public PayslipPdfWorker(IServiceScopeFactory scopeFactory, IWebHostEnvironment environment, ILogger<PayslipPdfWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _environment = environment;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var renderer = scope.ServiceProvider.GetRequiredService<PdfRenderer>();

                    var item = await context.Files.FirstOrDefaultAsync(x => !x.isPdfGenerated, stoppingToken);
                    if (item == null)
                    {
                        await Task.Delay(IdleDelay, stoppingToken);
                        continue;
                    }

                    var fileName = PdfArchive.SafeFileName($"{item.EmployeeName}_{item.Year}_{item.Month:D2}_{item.Cnic}.pdf");
                    var pdf = renderer.Render(PayslipTemplate.Render(item.ToPayslipViewModel()), PdfLayout.Plain);
                    await File.WriteAllBytesAsync(Path.Combine(PdfArchive.Folder(_environment), fileName), pdf, stoppingToken);

                    item.isPdfGenerated = true;
                    await context.SaveChangesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating payslip PDFs; retrying in {Delay}", ErrorDelay);
                    await Task.Delay(ErrorDelay, stoppingToken);
                }
            }
        }
    }

    /// <summary>Generates the yearly tax certificates requested from the ITC request page.</summary>
    public class TaxCertificatePdfWorker : BackgroundService
    {
        private static readonly TimeSpan IdleDelay = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TaxCertificatePdfWorker> _logger;

        public TaxCertificatePdfWorker(IServiceScopeFactory scopeFactory, IWebHostEnvironment environment, ILogger<TaxCertificatePdfWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _environment = environment;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var renderer = scope.ServiceProvider.GetRequiredService<PdfRenderer>();

                    var taxYear = await context.ITCRequests.FirstOrDefaultAsync(x => x.isRequestGenerated, stoppingToken);
                    if (taxYear != null)
                    {
                        await GenerateAsync(context, renderer, taxYear, stoppingToken);
                        continue;
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating tax certificates");
                }

                await Task.Delay(IdleDelay, stoppingToken);
            }
        }

        private async Task GenerateAsync(AppDbContext context, PdfRenderer renderer, TaxCertificateRequest taxYear, CancellationToken ct)
        {
            // Financial year: July–December of taxYear.Year plus January–June of the following year
            var rows = await context.Files.AsNoTracking()
                .Where(x => (x.Month >= 7 && x.Month <= 12 && x.Year == taxYear.Year)
                         || (x.Month >= 1 && x.Month <= 6 && x.Year == taxYear.Year + 1))
                .ToListAsync(ct);

            var folder = PdfArchive.Folder(_environment, "Tax Certificate");
            foreach (var employee in rows.GroupBy(x => x.Cnic).OrderBy(g => g.Key))
            {
                var first = employee.First();
                var html = TaxCertificateTemplate.Render(
                    employee.Key, first.EmployeeName, first.ServiceCategory,
                    employee.Sum(x => x.GrossSalary) ?? 0,
                    employee.Sum(x => x.AdvancesLoan) ?? 0,
                    employee.Sum(x => x.LeaveDeduction) ?? 0,
                    employee.Sum(x => x.WHITDeduction) ?? 0,
                    employee.Sum(x => x.EOBI) ?? 0,
                    employee.Sum(x => x.TotalDeductions) ?? 0,
                    employee.Sum(x => x.NetAmount) ?? 0,
                    taxYear.Year);

                var fileName = PdfArchive.SafeFileName($"{first.EmployeeName}_{taxYear.Year}-{taxYear.Year + 1}_{employee.Key}.pdf");
                await File.WriteAllBytesAsync(Path.Combine(folder, fileName), renderer.Render(html, PdfLayout.Plain), ct);
            }

            taxYear.isRequestGenerated = false;
            taxYear.isPdfGenerated = true;
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("Generated tax certificates for {StartYear}-{EndYear}", taxYear.Year, taxYear.Year + 1);
        }
    }
}
