namespace PayslipMgtSystem.Services.UI
{
    /// <summary>
    /// Waits until the user stops typing before running a search, so a query is not
    /// sent (and the list re-rendered) for every keystroke.
    /// </summary>
    public sealed class SearchDebouncer : IDisposable
    {
        public static readonly TimeSpan DefaultDelay = TimeSpan.FromMilliseconds(800);

        private readonly TimeSpan _delay;
        private CancellationTokenSource? _pending;

        public SearchDebouncer(TimeSpan? delay = null)
        {
            _delay = delay ?? DefaultDelay;
        }

        /// <summary>Runs <paramref name="search"/> once no newer call has arrived within the delay.</summary>
        public async Task RunAsync(Func<Task> search)
        {
            _pending?.Cancel();
            var pending = _pending = new CancellationTokenSource();
            try
            {
                await Task.Delay(_delay, pending.Token);
            }
            catch (TaskCanceledException)
            {
                return; // superseded by a newer keystroke
            }
            await search();
        }

        public void Dispose() => _pending?.Cancel();
    }
}
