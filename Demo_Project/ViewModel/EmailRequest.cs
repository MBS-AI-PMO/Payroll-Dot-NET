using Syncfusion.Blazor.Buttons;

namespace PayslipMgtSystem.ViewModel
{
    public class EmailRequest
    {
        public int Id { get; set; }
        public int SrNo { get; set; }
        public string? EmoployeeId { get; set; }
        public string? ServiceCategory { get; set; }
        public string? EmployeeName { get; set; }
        public string? Email { get; set; }
        public string? JoiningDate { get; set; }
        public DateTime JoinedAt { get; set; }
        public int? MonthDays { get; set; }
        public int? PresentDays { get; set; }
        public int? OfferedSalary { get; set; }
        public int? LeaveDeduction { get; set; }
        public int? BasicPayafterDeduction { get; set; }
        public int? Arrears { get; set; }
        public int? Allowances { get; set; }
        public int? AdvancesLoan { get; set; }
        public int? GrossSalary { get; set; }
        public int? AdvancesLoanDeduction { get; set; }
        public int? EOBI { get; set; }
        public int? WHITDeduction { get; set; }
        public int? OtherDeduction { get; set; }
        public int? TotalDeductions { get; set; }
        public int? NetAmount { get; set; }
        public int Month { get; set; }
        public bool Selected { get; set; }
        public bool isPdfGenerated { get; set; }
        public bool isTaxGenerated { get; set; }
        public DateTime? AddedAt { get; set; }
        public float? CommunicationAllowance { get; set; }
        public float? EquipmentAllowance { get; set; }
        public float? MedicalAllowance { get; set; }
        public float? TravellingAllowance { get; set; }
        public int? Overtime {  get; set; }
        public string Cnic { get; set; }
        public int Year { get; set; }
        public override bool Equals(object obj)
        {
            if (obj is EmailRequest other)
            {
                return this.Cnic == other.Cnic && this.Year == other.Year;
            }
            return false;
        }
    }
}
