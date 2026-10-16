namespace PayslipMgtSystem.Models
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Employee = "Employee";
    }

    public static class AppClaimTypes
    {
        // Digits-only CNIC stored in AspNetUserClaims; links an employee account to its payroll rows (Files.Cnic)
        public const string Cnic = "cnic";
    }
}
