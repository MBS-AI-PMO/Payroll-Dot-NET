using PayslipMgtSystem.Models;

namespace PayslipMgtSystem.ViewModel
{
    public static class UploadFileMappings
    {
        /// <summary>
        /// Fields used to render a payslip from a stored payroll row. Travelling, equipment and
        /// communication allowances are intentionally left out, matching the payslips emailed so far.
        /// </summary>
        public static UploadFileVM ToPayslipViewModel(this UploadFile f) => new()
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
        };

        /// <summary>Converts a selected ITC row into the view model the ITC template renders.</summary>
        public static UploadFileVM ToItcViewModel(this EmailRequest f) => new()
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
        };
    }
}
