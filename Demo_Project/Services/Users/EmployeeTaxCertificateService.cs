using Microsoft.EntityFrameworkCore;

namespace PayslipMgtSystem.Services.Users
{
    public class TaxCertificateSummary
    {
        /// <summary>The financial year runs from 1 July <see cref="StartYear"/> to 30 June <see cref="EndYear"/>.</summary>
        public int StartYear { get; set; }
        public int EndYear => StartYear + 1;
        public string FinancialYear => $"{StartYear}-{EndYear}";
        public string PeriodName => $"July {StartYear} – June {EndYear}";

        public int MonthsPaid { get; set; }
        public int GrossSalary { get; set; }
        public int IncomeTax { get; set; }
        public int Eobi { get; set; }
        public int AdvanceDeduction { get; set; }
        public int TotalDeductions => Eobi + AdvanceDeduction + IncomeTax;
        public int NetAmount { get; set; }

        /// <summary>True while the financial year has not ended yet, so the totals can still change.</summary>
        public bool IsInProgress => DateTime.Today < new DateTime(EndYear, 7, 1);
    }

    /// <summary>
    /// Yearly Income Tax Certificates for the signed-in employee, built from the same payroll
    /// totals and template as the certificates emailed from the ITC Mgt page.
    /// </summary>
    public class EmployeeTaxCertificateService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly PdfRenderer _pdfRenderer;

        public EmployeeTaxCertificateService(IServiceScopeFactory scopeFactory, PdfRenderer pdfRenderer)
        {
            _scopeFactory = scopeFactory;
            _pdfRenderer = pdfRenderer;
        }

        /// <summary>Financial year (its July start year) that a payroll month belongs to.</summary>
        public static int FinancialYearOf(int year, int month) => month >= 7 ? year : year - 1;

        public async Task<string?> GetCnicForUserAsync(string userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await EmployeeRecords.GetCnicAsync(context, userId);
        }

        public async Task<List<TaxCertificateSummary>> GetCertificatesAsync(string cnic)
        {
            var rows = await LoadRowsAsync(cnic, startYear: null);
            return rows
                .GroupBy(r => FinancialYearOf(r.Year, r.Month))
                .Select(g => new TaxCertificateSummary
                {
                    StartYear = g.Key,
                    MonthsPaid = g.Count(),
                    GrossSalary = g.Sum(r => r.GrossSalary ?? 0),
                    IncomeTax = g.Sum(r => r.WHITDeduction ?? 0),
                    Eobi = g.Sum(r => r.EOBI ?? 0),
                    AdvanceDeduction = g.Sum(r => r.AdvancesLoanDeduction ?? 0),
                    NetAmount = g.Sum(r => r.NetAmount ?? 0)
                })
                .OrderByDescending(c => c.StartYear)
                .ToList();
        }

        /// <summary>Totals for one financial year, shaped for <see cref="ItcTemplate"/>; null when there are no rows.</summary>
        public async Task<UploadFileVM?> GetCertificateAsync(string cnic, int startYear)
        {
            var rows = await LoadRowsAsync(cnic, startYear);
            if (rows.Count == 0)
                return null;

            // Name/designation from the latest month; amounts summed like the ITC Mgt page
            var latest = rows.OrderByDescending(r => r.Year).ThenByDescending(r => r.Month).First();
            return new UploadFileVM
            {
                Cnic = latest.Cnic,
                EmployeeName = latest.EmployeeName,
                Email = latest.Email,
                ServiceCategory = latest.ServiceCategory,
                Year = latest.Year,
                Month = latest.Month,
                EOBI = rows.Sum(x => x.EOBI),
                AdvancesLoan = rows.Sum(x => x.AdvancesLoan),
                AdvancesLoanDeduction = rows.Sum(x => x.AdvancesLoanDeduction),
                Allowances = rows.Sum(x => x.Allowances),
                Arrears = rows.Sum(x => x.Arrears),
                NetAmount = rows.Sum(x => x.NetAmount),
                BasicPayafterDeduction = rows.Sum(x => x.BasicPayafterDeduction),
                GrossSalary = rows.Sum(x => x.GrossSalary),
                LeaveDeduction = rows.Sum(x => x.LeaveDeduction),
                OfferedSalary = rows.Sum(x => x.OfferedSalary),
                PresentDays = rows.Sum(x => x.PresentDays),
                TotalDeductions = rows.Sum(x => x.TotalDeductions),
                OtherDeduction = rows.Sum(x => x.OtherDeduction),
                WHITDeduction = rows.Sum(x => x.WHITDeduction),
                CommunicationAllowance = rows.Sum(x => x.CommunicationAllowance),
                EquipmentAllowance = rows.Sum(x => x.EquipmentAllowance),
                MedicalAllowance = rows.Sum(x => x.MedicalAllowance),
                TravellingAllowance = rows.Sum(x => x.TravellingAllowance)
            };
        }

        /// <summary>The official Income Tax Certificate PDF – identical to the one emailed to the employee.</summary>
        public byte[] RenderPdf(UploadFileVM certificate, int startYear) =>
            _pdfRenderer.Render(ItcTemplate.Render(certificate, startYear), PdfLayout.ItcEmail);

        public static string PdfFileName(int startYear) => $"Income_Tax_Certificate_{startYear}-{startYear + 1}.pdf";

        private async Task<List<UploadFile>> LoadRowsAsync(string cnic, int? startYear)
        {
            if (string.IsNullOrWhiteSpace(cnic))
                return new List<UploadFile>();

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var query = EmployeeRecords.Own(context, cnic);
            if (startYear.HasValue)
            {
                var start = startYear.Value;
                query = query.Where(x => (x.Year == start && x.Month >= 7) || (x.Year == start + 1 && x.Month <= 6));
            }
            return await query.ToListAsync();
        }
    }
}
