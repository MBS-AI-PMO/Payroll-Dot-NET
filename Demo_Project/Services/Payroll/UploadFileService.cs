using Microsoft.EntityFrameworkCore;
using PayslipMgtSystem.Helper;

namespace PayslipMgtSystem.Services.Payroll
{
    public class UploadFileService
    {
        protected AppDbContext _context;
        protected IServiceScopeFactory? ScopeFactory { get; set; }
        public UploadFileService(AppDbContext context, IServiceScopeFactory? scopeFactory)
        {
            this._context = context;
            ScopeFactory = scopeFactory;    
        }

        public async Task SaveFileContentToDatabase(List<UploadFileVM> lines, int year, int month, bool isPdfGenerated, bool isTaxGenerated)
        {
            var uploadFiles = new List<UploadFile>();

            foreach (var f in lines)
            {
                f.Year = year;
                f.Month = month;
                f.isPdfGenerated = isPdfGenerated;
                f.isTaxGenerated = isTaxGenerated;

                var file = new UploadFile
                {
                    Id = f.Id,
                    EmployeeName = f.EmployeeName,
                    Email = f.Email,
                    SrNo = f.SrNo,
                    EOBI = f.EOBI,
                    AdvancesLoan = f.AdvancesLoan,
                    AdvancesLoanDeduction = f.AdvancesLoanDeduction,
                    Allowances = f.Allowances,
                    MedicalAllowance=f.MedicalAllowance,
                    Arrears = f.Arrears,
                    NetAmount = f.NetAmount,
                    BasicPayafterDeduction = f.BasicPayafterDeduction,
                    Cnic = f.Cnic,
                    GrossSalary = f.GrossSalary,
                    JoiningDate = f.JoiningDate,
                    LeaveDeduction = f.LeaveDeduction,
                    Month = f.Month,
                    MonthDays = f.MonthDays,
                    isPdfGenerated = isPdfGenerated,
                    isTaxGenerated = isTaxGenerated,
                    OfferedSalary = f.OfferedSalary,
                    PresentDays = f.PresentDays,
                    ServiceCategory = f.ServiceCategory,
                    OtherDeduction = f.OtherDeduction,
                    TotalDeductions = f.TotalDeductions,
                    WHITDeduction = f.WHITDeduction,
                    Year = f.Year,
                };

                uploadFiles.Add(file);
            }

            _context.Files.AddRange(uploadFiles);
            await _context.SaveChangesAsync();
        }
        public async Task<bool> UpdateRecord(UploadFile file)
        {
            try
            {

                var dep = _context.Files.Where(x => x.Id == file.Id).FirstOrDefault();
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<UploadFile>> CheckForExistingRecords(List<UploadFileVM> fileContent)
        {
            try
            {
                var existingRecords = new List<UploadFile>();

                foreach (var item in fileContent)
                {
                    var existingRecord = await _context.Files.FirstOrDefaultAsync(x => x.Cnic == item.Cnic);
                    if (existingRecord != null)
                    {
                        existingRecords.Add(existingRecord);
                    }
                }

                return existingRecords;
            }
            catch (Exception ex)
            {

                throw;
            }

        }
        public async Task DeleteExistingRecords(List<UploadFile> existingRecords)
        {
            _context.Files.RemoveRange(existingRecords);
            await _context.SaveChangesAsync();
        }
        public async Task<UploadFile> GetRecord(int Id)
        {
            UploadFile uploadFile = await _context.Files.FirstOrDefaultAsync(c => c.Id.Equals(Id));
            return uploadFile;
        }
        public async Task<PaginationVM> GetDataAsync(PaginationDTO pdto, string name, int selectedYear, int selectedMonth)
        {
            try
            {

            PaginationVM vm = new();
            using var scope = ScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var queryable = context.Files.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).ThenBy(x => x.EmployeeName).Select(f => new UploadFileVM
            {
                Id = f.Id,
                EmployeeName = f.EmployeeName,
                Email = f.Email,
                SrNo = f.SrNo,
                EOBI = f.EOBI,
                AdvancesLoan = f.AdvancesLoan,
                AdvancesLoanDeduction = f.AdvancesLoanDeduction,
                Allowances = f.Allowances,
                Arrears = f.Arrears,
                NetAmount = f.NetAmount,
                BasicPayafterDeduction = f.BasicPayafterDeduction,
                Cnic = f.Cnic,
                GrossSalary = f.GrossSalary,
                JoiningDate = f.JoiningDate,
                JoinedAt = f.JoinedAt,
                LeaveDeduction = f.LeaveDeduction,
                Month = f.Month,
                MonthDays = f.MonthDays,
                OfferedSalary = f.OfferedSalary,
                PresentDays = f.PresentDays,
                ServiceCategory = f.ServiceCategory,
                TotalDeductions = f.TotalDeductions,
                OtherDeduction = f.OtherDeduction,
                WHITDeduction = f.WHITDeduction,
                Year = f.Year,
                MedicalAllowance = f.MedicalAllowance
            });

            if (!string.IsNullOrEmpty(name))
            {
                queryable = queryable.Where(x =>
                    x.EmployeeName.ToLower().Contains(name.ToLower()) || x.Cnic.ToLower().Contains(name.ToLower())
                );
            }
            if (selectedYear != 0 && selectedMonth == 0)
            {
                queryable = queryable.Where(x => x.Year == selectedYear);
            }

            if (selectedMonth != 0 && selectedYear != 0)
            {
                queryable = queryable.Where(x => x.Month == selectedMonth && x.Year == selectedYear);
            }

            if (selectedYear == 0 && selectedMonth != 0)
            {
                queryable = queryable.Where(x => x.Year == selectedYear && x.Month == selectedMonth);
            }

            vm.TotalPages = await queryable.PageCountAsync(pdto.QuantityPerPage);
            vm.uploadFileVMs = await queryable.Paginate(pdto).ToListAsync();
            vm.TotalCount = queryable.Count();

            return vm;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public IQueryable<int> GetDataIds(string name, int selectedYear, int selectedMonth)
        {
            PaginationVM vm = new();
            var queryable = _context.Files.OrderByDescending(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.EmployeeName).Select(f => new UploadFileVM
            {
                Id = f.Id,
                EmployeeName = f.EmployeeName,
                Email = f.Email,
                SrNo = f.SrNo,
                EOBI = f.EOBI,
                AdvancesLoan = f.AdvancesLoan,
                AdvancesLoanDeduction = f.AdvancesLoanDeduction,
                Allowances = f.Allowances,
                Arrears = f.Arrears,
                NetAmount = f.NetAmount,
                BasicPayafterDeduction = f.BasicPayafterDeduction,
                Cnic = f.Cnic,
                GrossSalary = f.GrossSalary,
                JoiningDate = f.JoiningDate,
                JoinedAt = f.JoinedAt,
                LeaveDeduction = f.LeaveDeduction,
                Month = f.Month,
                MonthDays = f.MonthDays,
                OfferedSalary = f.OfferedSalary,
                PresentDays = f.PresentDays,
                ServiceCategory = f.ServiceCategory,
                TotalDeductions = f.TotalDeductions,
                OtherDeduction = f.OtherDeduction,
                WHITDeduction = f.WHITDeduction,
                Year = f.Year,
                MedicalAllowance = f.MedicalAllowance
            });

            if (!string.IsNullOrEmpty(name))
            {
                queryable = queryable.Where(x =>
                    x.EmployeeName.ToLower().Contains(name.ToLower()) || x.Cnic.ToLower().Contains(name.ToLower())
                );
            }
            if (selectedYear != 0 && selectedMonth == 0)
            {
                queryable = queryable.Where(x => x.Year == selectedYear);
            }

            if (selectedMonth != 0 && selectedYear != 0)
            {
                queryable = queryable.Where(x => x.Month == selectedMonth && x.Year == selectedYear);
            }

            if (selectedYear == 0 && selectedMonth != 0)
            {
                queryable = queryable.Where(x => x.Year == selectedYear && x.Month == selectedMonth);
            }

            return queryable.Select(x => x.Id);
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
        public List<EmailRequest> GetDataITCIdsAsync(string name, int? startYear, int? startMonth, int? endYear, int? endMonth, int selectedYear, int selectedMonth)
        {
            PaginationVM vm = new();
            using var scope = ScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var queryable = context.Files.OrderByDescending(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.EmployeeName).Select(f => new UploadFileVM
            {
                Id = f.Id,
                EmployeeName = f.EmployeeName,
                Email = f.Email,
                SrNo = f.SrNo,
                EOBI = f.EOBI,
                AdvancesLoan = f.AdvancesLoan,
                AdvancesLoanDeduction = f.AdvancesLoanDeduction,
                Allowances = f.Allowances,
                Arrears = f.Arrears,
                NetAmount = f.NetAmount,
                BasicPayafterDeduction = f.BasicPayafterDeduction,
                Cnic = f.Cnic,
                GrossSalary = f.GrossSalary,
                JoiningDate = f.JoiningDate,
                JoinedAt = f.JoinedAt,
                LeaveDeduction = f.LeaveDeduction,
                Month = f.Month,
                MonthDays = f.MonthDays,
                OfferedSalary = f.OfferedSalary,
                PresentDays = f.PresentDays,
                ServiceCategory = f.ServiceCategory,
                TotalDeductions = f.TotalDeductions,
                OtherDeduction = f.OtherDeduction,
                WHITDeduction = f.WHITDeduction,
                Year = f.Year,
                CommunicationAllowance = f.CommunicationAllowance,
                EquipmentAllowance = f.EquipmentAllowance,
                MedicalAllowance = f.MedicalAllowance,
                TravellingAllowance = f.TravellingAllowance
            });

            if (startYear != null && startMonth != null && endYear != null && endMonth != null)
            {
                queryable = queryable.Where(x =>
                    (x.Year > startYear || (x.Year == startYear && x.Month >= startMonth)) &&
                    (x.Year < endYear || (x.Year == endYear && x.Month <= endMonth))
                );
            }

            if (!string.IsNullOrEmpty(name))
            {
                queryable = queryable.Where(x =>
                    x.EmployeeName.ToLower().Contains(name.ToLower()) || x.Cnic.ToLower().Contains(name.ToLower())
                );
            }

            if (selectedYear != 0 && selectedMonth != 0)
            {
                queryable = queryable.Where(x => x.Month == selectedMonth && x.Year == selectedYear);
            }
            else if (selectedMonth != 0)
            {
                queryable = queryable.Where(x => x.Month == selectedMonth);
            }

          List<EmailRequest> groupedData = queryable
                .GroupBy(x => x.Cnic)
                .Select(g => new EmailRequest
                {
                    EmployeeName = g.First().EmployeeName,
                    Email = g.First().Email,
                    SrNo = g.First().SrNo,
                    EOBI = g.Sum(x => x.EOBI),
                    AdvancesLoan = g.Sum(x => x.AdvancesLoan),
                    AdvancesLoanDeduction = g.Sum(x => x.AdvancesLoanDeduction),
                    Allowances = g.Sum(x => x.Allowances),
                    Arrears = g.Sum(x => x.Arrears),
                    NetAmount = g.Sum(x => x.NetAmount),
                    BasicPayafterDeduction = g.Sum(x => x.BasicPayafterDeduction),
                    Cnic = g.Key,
                    GrossSalary = g.Sum(x => x.GrossSalary),
                    JoiningDate = g.First().JoiningDate,
                    JoinedAt = g.First().JoinedAt,
                    LeaveDeduction = g.Sum(x => x.LeaveDeduction),
                    Month = g.First().Month,
                    MonthDays = g.First().MonthDays,
                    OfferedSalary = g.Sum(x => x.OfferedSalary),
                    PresentDays = g.Sum(x => x.PresentDays),
                    ServiceCategory = g.First().ServiceCategory,
                    TotalDeductions = g.Sum(x => x.TotalDeductions),
                    OtherDeduction = g.Sum(x => x.OtherDeduction),
                    WHITDeduction = g.Sum(x => x.WHITDeduction),
                    CommunicationAllowance = g.Sum(x => x.CommunicationAllowance),
                    EquipmentAllowance = g.Sum(x => x.EquipmentAllowance),
                    MedicalAllowance = g.Sum(x => x.MedicalAllowance),
                    TravellingAllowance = g.Sum(x => x.TravellingAllowance),
                    Year = g.First().Year,
                }).ToList();

            return groupedData;
        }
    }
}
