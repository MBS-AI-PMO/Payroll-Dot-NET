using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PayslipMgtSystem.Models;

namespace PayslipMgtSystem.Services.Users
{
    /// <summary>
    /// Creates and manages employee login accounts based on the payroll records in the Files table.
    /// Employees are identified by CNIC; the most recent payroll record supplies their name and email.
    /// </summary>
    public class UserManagementService
    {
        private static readonly SemaphoreSlim BulkLock = new(1, 1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(IServiceScopeFactory scopeFactory, ILogger<UserManagementService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public static bool IsBulkRunning => BulkLock.CurrentCount == 0;

        private class EmployeeSource
        {
            public string Key { get; set; } = "";
            public string? Cnic { get; set; }
            public string? Name { get; set; }
            public string? Email { get; set; }
        }

        private static async Task<List<EmployeeSource>> LoadEmployeesAsync(AppDbContext context, CancellationToken ct)
        {
            var rows = await context.Files.AsNoTracking()
                .Select(f => new { f.Id, f.Cnic, f.EmployeeName, f.Email, f.Year, f.Month })
                .ToListAsync(ct);

            return rows
                .GroupBy(r =>
                {
                    var cnic = AccountLinks.NormalizeCnic(r.Cnic);
                    // Rows without a CNIC cannot be linked; group them by name/email so each is reported once
                    return cnic.Length > 0 ? cnic : $"~{r.EmployeeName?.Trim().ToUpperInvariant()}|{r.Email?.Trim().ToUpperInvariant()}";
                })
                .Select(g =>
                {
                    var latest = g.OrderByDescending(r => r.Year).ThenByDescending(r => r.Month).ThenByDescending(r => r.Id).First();
                    var latestEmail = g.OrderByDescending(r => r.Year).ThenByDescending(r => r.Month).ThenByDescending(r => r.Id)
                        .Select(r => r.Email?.Trim())
                        .FirstOrDefault(e => !string.IsNullOrWhiteSpace(e));
                    return new EmployeeSource
                    {
                        Key = g.Key,
                        Cnic = g.Key.StartsWith("~") ? null : g.Key,
                        Name = latest.EmployeeName?.Trim(),
                        Email = latestEmail
                    };
                })
                .OrderBy(e => e.Name)
                .ToList();
        }

        public async Task<UserOverview> GetOverviewAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cnics = await context.Files.AsNoTracking().Select(f => f.Cnic).Distinct().ToListAsync();
            var totalEmployees = cnics.Select(AccountLinks.NormalizeCnic).Where(c => c.Length > 0).Distinct().Count();

            var employeeUsers = EmployeeUsersQuery(context);
            return new UserOverview
            {
                TotalEmployees = totalEmployees,
                EmployeeAccounts = await employeeUsers.CountAsync(),
                PendingSetup = await employeeUsers.CountAsync(u => u.PasswordHash == null)
            };
        }

        private static IQueryable<ApplicationUser> EmployeeUsersQuery(AppDbContext context) =>
            from u in context.Users
            join ur in context.UserRoles on u.Id equals ur.UserId
            join r in context.Roles on ur.RoleId equals r.Id
            where r.NormalizedName == "EMPLOYEE"
            select u;

        private static IQueryable<IdentityUserClaim<string>> CnicClaims(AppDbContext context) =>
            context.UserClaims.Where(c => c.ClaimType == AppClaimTypes.Cnic);

        public async Task<EmployeeAccountPage> GetEmployeeAccountsAsync(string? search, int page, int pageSize)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var query = EmployeeUsersQuery(context).AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                var cnicTerm = AccountLinks.NormalizeCnic(term);
                query = query.Where(u =>
                    (u.Name != null && u.Name.Contains(term)) ||
                    (u.Email != null && u.Email.Contains(term)) ||
                    (cnicTerm.Length > 0 && CnicClaims(context).Any(c => c.UserId == u.Id && c.ClaimValue!.Contains(cnicTerm))));
            }

            var total = await query.CountAsync();
            page = Math.Max(page, 1);
            var now = DateTimeOffset.UtcNow;
            var items = await query
                .OrderBy(u => u.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new EmployeeAccount
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Cnic = CnicClaims(context).Where(c => c.UserId == u.Id).Select(c => c.ClaimValue).FirstOrDefault(),
                    CreatedAt = u.CreatedAt,
                    PasswordSet = u.PasswordHash != null,
                    IsLockedOut = u.LockoutEnd != null && u.LockoutEnd > now
                })
                .ToListAsync();

            return new EmployeeAccountPage { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
        }

        /// <summary>
        /// Creates an Employee account for every employee in the payroll data that does not yet have one,
        /// without sending any email. Employees set their password through the Forgot Password page.
        /// </summary>
        public async Task<BulkUserResult> CreateUsersAsync(IProgress<BulkUserProgress>? progress, CancellationToken ct)
        {
            if (!await BulkLock.WaitAsync(0, ct))
                throw new InvalidOperationException("A user creation job is already running. Please wait for it to finish.");

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                if (!await roleManager.RoleExistsAsync(AppRoles.Employee))
                    await roleManager.CreateAsync(new IdentityRole(AppRoles.Employee));

                var employees = await LoadEmployeesAsync(context, ct);
                var existingCnics = (await CnicClaims(context).AsNoTracking()
                        .Where(c => c.ClaimValue != null)
                        .Select(c => c.ClaimValue!)
                        .ToListAsync(ct))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var result = new BulkUserResult { TotalEmployees = employees.Count };
                var emailValidator = new EmailAddressAttribute();
                var processed = 0;
                progress?.Report(new BulkUserProgress(0, employees.Count, null));

                foreach (var employee in employees)
                {
                    if (ct.IsCancellationRequested)
                    {
                        result.WasCancelled = true;
                        break;
                    }

                    var record = new BulkUserRecord { EmployeeName = employee.Name, Cnic = employee.Cnic, Email = employee.Email };
                    try
                    {
                        await ProcessEmployeeAsync(employee, record, existingCnics, emailValidator, userManager);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create user for CNIC {Cnic}", employee.Cnic);
                        record.Status = BulkUserStatus.Failed;
                        record.Reason = "Unexpected error while creating the account.";
                    }

                    result.Records.Add(record);
                    processed++;
                    progress?.Report(new BulkUserProgress(processed, employees.Count, employee.Name));
                }

                result.Created = result.Records.Count(r => r.Status == BulkUserStatus.Created);
                result.AlreadyExisting = result.Records.Count(r => r.Status == BulkUserStatus.AlreadyExists);
                result.MissingEmail = result.Records.Count(r => r.Status == BulkUserStatus.MissingEmail);
                result.Failed = result.Records.Count(r => r.Status == BulkUserStatus.Failed);
                result.InvitationsFailed = result.Records.Count(r => r.InvitationFailed);
                result.CompletedAt = DateTime.Now;

                _logger.LogInformation("Bulk user creation finished: {Created} created, {Existing} existing, {Missing} missing email, {Failed} failed",
                    result.Created, result.AlreadyExisting, result.MissingEmail, result.Failed);
                return result;
            }
            finally
            {
                BulkLock.Release();
            }
        }

        private async Task ProcessEmployeeAsync(EmployeeSource employee, BulkUserRecord record, HashSet<string> existingCnics,
            EmailAddressAttribute emailValidator, UserManager<ApplicationUser> userManager)
        {
            if (employee.Cnic == null)
            {
                record.Status = BulkUserStatus.Failed;
                record.Reason = "CNIC is missing in the payroll data, so payslips cannot be linked to an account.";
                return;
            }

            if (existingCnics.Contains(employee.Cnic))
            {
                record.Status = BulkUserStatus.AlreadyExists;
                record.Reason = "An account already exists for this CNIC.";
                return;
            }

            if (string.IsNullOrWhiteSpace(employee.Email))
            {
                record.Status = BulkUserStatus.MissingEmail;
                record.Reason = "No email address found in the payroll data.";
                return;
            }

            var email = employee.Email.Trim();
            if (!emailValidator.IsValid(email) || !email.Contains('.'))
            {
                record.Status = BulkUserStatus.Failed;
                record.Reason = "Invalid email address.";
                return;
            }

            if (await userManager.FindByNameAsync(email) != null || await userManager.FindByEmailAsync(email) != null)
            {
                record.Status = BulkUserStatus.Failed;
                record.Reason = "This email address is already used by another account.";
                return;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Name = employee.Name,
                CreatedAt = DateTime.Now,
                LockoutEnabled = true
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                record.Status = BulkUserStatus.Failed;
                record.Reason = DescribeErrors(createResult);
                return;
            }

            var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Employee);
            var claimResult = roleResult.Succeeded
                ? await userManager.AddClaimAsync(user, new Claim(AppClaimTypes.Cnic, employee.Cnic))
                : IdentityResult.Failed();
            if (!roleResult.Succeeded || !claimResult.Succeeded)
            {
                await userManager.DeleteAsync(user);
                record.Status = BulkUserStatus.Failed;
                record.Reason = "Could not assign the Employee role or link the CNIC.";
                _logger.LogError("Role/CNIC assignment failed for {Email}: {Errors}", email,
                    DescribeErrors(roleResult.Succeeded ? claimResult : roleResult));
                return;
            }

            existingCnics.Add(employee.Cnic);
            record.Status = BulkUserStatus.Created;
            // No email here: the employee sets their password via "Forgot password?" on the login page
            record.Reason = "Account created. The employee can set a password via \"Forgot password?\".";
        }

        private static async Task SendInvitationAsync(UserManager<ApplicationUser> userManager, AccountEmailSender emailSender,
            ApplicationUser user, string baseUri)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var link = AccountLinks.BuildResetPasswordLink(baseUri, user.Id, token, isSetup: true);
            await emailSender.SendInvitationAsync(user.Email!, user.Name, link);
        }

        /// <summary>Re-sends the set-password (or reset) link to an existing employee account.</summary>
        public async Task ResendInvitationAsync(string userId, string baseUri)
        {
            using var scope = _scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var emailSender = scope.ServiceProvider.GetRequiredService<AccountEmailSender>();

            var user = await userManager.FindByIdAsync(userId)
                       ?? throw new InvalidOperationException("User not found.");
            if (string.IsNullOrWhiteSpace(user.Email))
                throw new InvalidOperationException("This user has no email address.");

            if (user.PasswordHash == null)
            {
                await SendInvitationAsync(userManager, emailSender, user, baseUri);
            }
            else
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var link = AccountLinks.BuildResetPasswordLink(baseUri, user.Id, token, isSetup: false);
                await emailSender.SendPasswordResetAsync(user.Email, user.Name, link);
            }
        }

        private static string DescribeErrors(IdentityResult result) =>
            string.Join(" ", result.Errors.Select(e => e.Description));
    }
}
