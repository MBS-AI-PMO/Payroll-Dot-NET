using Microsoft.EntityFrameworkCore;

namespace PayslipMgtSystem.Services.Users
{
    public class PayslipSummary
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string? EmployeeName { get; set; }
        public string? Designation { get; set; }
        public int? GrossSalary { get; set; }
        public int? TotalDeductions { get; set; }
        public int? NetAmount { get; set; }
        public DateTime PeriodEnd => new DateTime(Year, Math.Clamp(Month, 1, 12), 1).AddMonths(1).AddDays(-1);
        public string PeriodName => new DateTime(Year, Math.Clamp(Month, 1, 12), 1).ToString("MMMM yyyy");
    }

    /// <summary>Monthly payslips for the signed-in employee (see <see cref="EmployeeRecords"/>).</summary>
    public class EmployeePayslipService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly PdfRenderer _pdfRenderer;

        public EmployeePayslipService(IServiceScopeFactory scopeFactory, PdfRenderer pdfRenderer)
        {
            _scopeFactory = scopeFactory;
            _pdfRenderer = pdfRenderer;
        }

        public async Task<string?> GetCnicForUserAsync(string userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await EmployeeRecords.GetCnicAsync(context, userId);
        }

        public async Task<List<PayslipSummary>> GetPayslipsAsync(string cnic)
        {
            if (string.IsNullOrWhiteSpace(cnic))
                return new List<PayslipSummary>();

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return await EmployeeRecords.Own(context, cnic)
                .OrderByDescending(f => f.Year).ThenByDescending(f => f.Month)
                .Select(f => new PayslipSummary
                {
                    Id = f.Id,
                    Year = f.Year,
                    Month = f.Month,
                    EmployeeName = f.EmployeeName,
                    Designation = f.ServiceCategory,
                    GrossSalary = f.GrossSalary,
                    TotalDeductions = f.TotalDeductions,
                    NetAmount = f.NetAmount
                })
                .ToListAsync();
        }

        public async Task<UploadFile?> GetPayslipAsync(string cnic, int id)
        {
            if (string.IsNullOrWhiteSpace(cnic))
                return null;

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await EmployeeRecords.Own(context, cnic).FirstOrDefaultAsync(f => f.Id == id);
        }

        /// <summary>The official payslip PDF – identical to the one emailed to the employee.</summary>
        public byte[] RenderPdf(UploadFile record) =>
            _pdfRenderer.Render(PayslipTemplate.Render(record.ToPayslipViewModel()), PdfLayout.Standard);

        public static string PdfFileName(UploadFile record) => $"Payslip_{record.Year}_{record.Month:D2}.pdf";

        public async Task<UploadFile?> GetAnyPayslipAsync(int id)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await context.Files.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
        }
    }
}
