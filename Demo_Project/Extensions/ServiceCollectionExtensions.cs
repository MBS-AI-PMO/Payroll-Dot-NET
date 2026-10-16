using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PayslipMgtSystem.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>Database, ASP.NET Identity and the authentication cookie.</summary>
        public static IServiceCollection AddPortalIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/Login";
            });

            // Invitation / password reset links stay valid for 3 days
            services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromDays(3));

            // Re-validate the security stamp on every request so a changed/disabled user is signed out immediately
            services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);

            services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
            services.AddDistributedMemoryCache();
            services.AddTransient<UserCacheService>();
            return services;
        }

        /// <summary>Application services: payroll data, PDFs, email and the background workers.</summary>
        public static IServiceCollection AddPayslipPortal(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

            // PDF + email
            services.AddSingleton<PdfRenderer>();
            services.AddSingleton<SmtpEmailSender>();
            services.AddSingleton<DocumentEmailService>();
            services.AddScoped<AccountEmailSender>();

            // Payroll data
            services.AddSingleton<PayrollExcel>();
            services.AddScoped<UploadFileService>();
            services.AddScoped<ChangeEmailService>();
            services.AddScoped<GenerateITCService>();

            // Users & employee self-service
            services.AddScoped<UserManagementService>();
            services.AddScoped<EmployeePayslipService>();
            services.AddScoped<EmployeeTaxCertificateService>();

            // UI
            services.AddScoped<ToastService>();

            // Background work
            services.AddSingleton<PayslipEmailQueue>();
            services.AddSingleton<ItcEmailQueue>();
            services.AddHostedService<PayslipEmailWorker>();
            services.AddHostedService<ItcEmailWorker>();
            services.AddHostedService<PayslipPdfWorker>();
            services.AddHostedService<TaxCertificatePdfWorker>();

            return services;
        }
    }
}
