using Microsoft.EntityFrameworkCore;
using PayslipMgtSystem.Models;
using PayslipMgtSystem.Services.Email;
using PayslipMgtSystem.Services.Pdf;
using PayslipMgtSystem.ViewModel;

namespace PayslipMgtSystem.Services.Background
{
    /// <summary>Sends the payslip emails requested from the Payslip Mgt page.</summary>
    public class PayslipEmailWorker : BackgroundService
    {
        private static readonly TimeSpan SendDelay = TimeSpan.FromSeconds(2);

        private readonly PayslipEmailQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PayslipEmailWorker> _logger;

        public PayslipEmailWorker(PayslipEmailQueue queue, IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<PayslipEmailWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var batch in _queue.ReadAllAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<SmtpEmailSender>();
                var emailService = scope.ServiceProvider.GetRequiredService<DocumentEmailService>();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await NotifyAdminAsync(sender, batch.RecordIds);

                var sent = 0;
                foreach (var id in batch.RecordIds)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    var record = await context.Files.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, stoppingToken);
                    if (record == null) continue;

                    if (await emailService.SendPayslipAsync(record.ToPayslipViewModel(), PdfLayout.Standard, SendDelay))
                        sent++;
                }
                _logger.LogInformation("Payslip email batch finished: {Sent} of {Total} sent", sent, batch.RecordIds.Count);
            }
        }

        private async Task NotifyAdminAsync(SmtpEmailSender sender, IReadOnlyList<int> ids)
        {
            var adminEmail = _configuration["Notifications:AdminEmail"];
            if (string.IsNullOrWhiteSpace(adminEmail))
                return;

            try
            {
                var body = $"Total processed: {ids.Count}<br/>The following IDs are to be processed: {string.Join(",", ids)}";
                await sender.SendAsync(adminEmail, "IDs to be Sent", body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not send the payslip batch notification to {AdminEmail}", adminEmail);
            }
        }
    }

    /// <summary>Sends the Income Tax Certificate emails requested from the ITC Mgt page.</summary>
    public class ItcEmailWorker : BackgroundService
    {
        private static readonly TimeSpan SendDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan PauseBetweenEmails = TimeSpan.FromSeconds(5);

        private readonly ItcEmailQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ItcEmailWorker> _logger;

        public ItcEmailWorker(ItcEmailQueue queue, IServiceScopeFactory scopeFactory, ILogger<ItcEmailWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var batch in _queue.ReadAllAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<DocumentEmailService>();

                var sent = 0;
                foreach (var employee in batch.Employees)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    if (await emailService.SendItcAsync(employee.ToItcViewModel(), batch.StartYear, PdfLayout.ItcEmail, SendDelay))
                        sent++;
                    await Task.Delay(PauseBetweenEmails, stoppingToken);
                }
                _logger.LogInformation("ITC email batch for {StartYear}-{EndYear} finished: {Sent} of {Total} sent",
                    batch.StartYear, batch.StartYear + 1, sent, batch.Employees.Count);
            }
        }
    }
}
