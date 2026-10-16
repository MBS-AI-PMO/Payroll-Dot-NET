using System.Threading.Channels;
using PayslipMgtSystem.ViewModel;

namespace PayslipMgtSystem.Services.Background
{
    /// <summary>Thread-safe in-memory queue that hands work from the UI to a background worker.</summary>
    public class BackgroundQueue<T>
    {
        private readonly Channel<T> _channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions { SingleReader = true });

        public void Enqueue(T item) => _channel.Writer.TryWrite(item);

        public IAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAllAsync(cancellationToken);
    }

    /// <summary>Payroll record ids (Files.Id) whose payslips should be emailed.</summary>
    public sealed record PayslipEmailBatch(IReadOnlyList<int> RecordIds);

    /// <summary>Employees whose Income Tax Certificate should be emailed for the financial year starting 1 July <see cref="StartYear"/>.</summary>
    public sealed record ItcEmailBatch(IReadOnlyList<EmailRequest> Employees, int StartYear);

    public class PayslipEmailQueue : BackgroundQueue<PayslipEmailBatch> { }

    public class ItcEmailQueue : BackgroundQueue<ItcEmailBatch> { }
}
