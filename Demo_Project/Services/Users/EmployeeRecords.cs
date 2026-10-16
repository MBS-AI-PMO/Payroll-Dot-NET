using Microsoft.EntityFrameworkCore;

namespace PayslipMgtSystem.Services.Users
{
    /// <summary>
    /// The only way employee self-service reads payroll data: every query is scoped to the
    /// employee's own CNIC so one employee can never see another employee's records.
    /// </summary>
    internal static class EmployeeRecords
    {
        public static IQueryable<UploadFile> Own(AppDbContext context, string cnic) =>
            context.Files.AsNoTracking()
                .Where(f => f.Cnic != null && f.Cnic.Replace("-", "").Replace(" ", "") == cnic);

        public static Task<string?> GetCnicAsync(AppDbContext context, string userId) =>
            context.UserClaims.AsNoTracking()
                .Where(c => c.UserId == userId && c.ClaimType == AppClaimTypes.Cnic)
                .Select(c => c.ClaimValue)
                .FirstOrDefaultAsync();
    }
}
