using Microsoft.EntityFrameworkCore;
using PayslipMgtSystem.Helper;

namespace PayslipMgtSystem.Services.Payroll
{
    public class ChangeEmailService
    {
        protected IServiceScopeFactory? ScopeFactory { get; set; }

        protected AppDbContext _context;
        public ChangeEmailService(AppDbContext context, IServiceScopeFactory? scopeFactory)
        {
            _context = context;
            ScopeFactory = scopeFactory;
        }

        public async Task<PaginationVM> GetDataITCAsync(
    PaginationDTO pdto,
    string name,
    int? startYear, int? startMonth,
    int? endYear, int? endMonth,
    int selectedYear, int selectedMonth)
        {
            PaginationVM vm = new();

            using var scope = ScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var originalTimeout = context.Database.GetCommandTimeout();
            context.Database.SetCommandTimeout(1800); // 30 minutes

            try
            {
                IQueryable<UploadFile> query = context.Files.AsNoTracking();

                if (startYear.HasValue && startMonth.HasValue && endYear.HasValue && endMonth.HasValue)
                {
                    query = query.Where(x =>
                        (x.Year > startYear.Value || (x.Year == startYear.Value && x.Month >= startMonth.Value)) &&
                        (x.Year < endYear.Value || (x.Year == endYear.Value && x.Month <= endMonth.Value)));
                }

                if (selectedYear != 0 && selectedMonth != 0)
                {
                    query = query.Where(x => x.Year == selectedYear && x.Month == selectedMonth);
                }
                else if (selectedMonth != 0)
                {
                    query = query.Where(x => x.Month == selectedMonth);
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    var search = name.Trim().ToLower();
                    query = query.Where(x =>
                        x.EmployeeName.ToLower().Contains(search) ||
                        x.Cnic.ToLower().Contains(search));
                }

                vm.TotalCount = await query
                    .Select(x => x.Cnic)
                    .Distinct()
                    .CountAsync();

                vm.TotalPages = (int)Math.Ceiling(vm.TotalCount / (double)pdto.QuantityPerPage);

                if (vm.TotalCount == 0)
                {
                    vm.uploadFileVMs = new List<UploadFileVM>();
                    return vm;
                }

                var pageCnics = await query
                    .GroupBy(x => x.Cnic)
                    .Select(g => new
                    {
                        Cnic = g.Key,
                        EmployeeName = g.Min(x => x.EmployeeName)   // or FirstOrDefault() if you prefer
                    })
                    .OrderBy(x => x.EmployeeName)
                    .Skip((pdto.Page - 1) * pdto.QuantityPerPage)
                    .Take(pdto.QuantityPerPage)
                    .Select(x => x.Cnic)
                    .ToListAsync();

                var groupedData = await query
                    .Where(x => pageCnics.Contains(x.Cnic))
                    .GroupBy(x => x.Cnic)
                    .Select(g => new UploadFileVM
                    {
                        Cnic = g.Key,
                        EmployeeName = g.Select(x => x.EmployeeName).FirstOrDefault(),
                        Email = g.Select(x => x.Email).FirstOrDefault(),
                        SrNo = g.Select(x => x.SrNo).FirstOrDefault(),
                        JoiningDate = g.Select(x => x.JoiningDate).FirstOrDefault(),
                        JoinedAt = g.Select(x => x.JoinedAt).FirstOrDefault(),
                        Month = g.Select(x => x.Month).FirstOrDefault(),
                        MonthDays = g.Select(x => x.MonthDays).FirstOrDefault(),
                        ServiceCategory = g.Select(x => x.ServiceCategory).FirstOrDefault(),
                        Year = g.Select(x => x.Year).FirstOrDefault(),
                        Status = g.Select(x => x.Status).FirstOrDefault(),

                        EOBI = g.Sum(x => x.EOBI),
                        AdvancesLoan = g.Sum(x => x.AdvancesLoan),
                        AdvancesLoanDeduction = g.Sum(x => x.AdvancesLoanDeduction),
                        Allowances = g.Sum(x => x.Allowances),
                        Arrears = g.Sum(x => x.Arrears),
                        NetAmount = g.Sum(x => x.NetAmount),
                        BasicPayafterDeduction = g.Sum(x => x.BasicPayafterDeduction),
                        GrossSalary = g.Sum(x => x.GrossSalary),
                        LeaveDeduction = g.Sum(x => x.LeaveDeduction),
                        OfferedSalary = g.Sum(x => x.OfferedSalary),
                        PresentDays = g.Sum(x => x.PresentDays),
                        TotalDeductions = g.Sum(x => x.TotalDeductions),
                        OtherDeduction = g.Sum(x => x.OtherDeduction),
                        WHITDeduction = g.Sum(x => x.WHITDeduction),
                        CommunicationAllowance = g.Sum(x => x.CommunicationAllowance),
                        EquipmentAllowance = g.Sum(x => x.EquipmentAllowance),
                        MedicalAllowance = g.Sum(x => x.MedicalAllowance),
                        TravellingAllowance = g.Sum(x => x.TravellingAllowance)
                    })
                    .OrderBy(x => x.EmployeeName)   // final ordering (same as the page order)
                    .ToListAsync();

                vm.uploadFileVMs = groupedData;
                return vm;
            }
            finally
            {
                context.Database.SetCommandTimeout(originalTimeout);
            }
        }
        public async Task<int> UpdateEmails(string oldEmail, string newEmail)
        {
            var recordsToUpdate = await _context.Files
                .Where(f => f.Email == oldEmail)
                .ToListAsync();

            if (recordsToUpdate.Any())
            {
                foreach (var record in recordsToUpdate)
                {
                    record.Email = newEmail;
                }

                await _context.SaveChangesAsync();
            }

            return recordsToUpdate.Count;
        }

    }
}
