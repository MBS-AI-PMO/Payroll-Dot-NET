using Microsoft.JSInterop;

namespace PayslipMgtSystem.Services.UI
{
    public enum ToastLevel
    {
        Success,
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Shows toast notifications through wwwroot/js/app.js so Blazor pages and the Razor
    /// sign-in pages share the same notification style.
    /// </summary>
    public class ToastService
    {
        private readonly IJSRuntime _js;

        public ToastService(IJSRuntime js)
        {
            _js = js;
        }

        public Task Success(string message, string? title = null) => Show(ToastLevel.Success, message, title);
        public Task Info(string message, string? title = null) => Show(ToastLevel.Info, message, title);
        public Task Warning(string message, string? title = null) => Show(ToastLevel.Warning, message, title);
        public Task Error(string message, string? title = null) => Show(ToastLevel.Error, message, title);

        public async Task Show(ToastLevel level, string message, string? title = null)
        {
            try
            {
                await _js.InvokeVoidAsync("appToast.show", level.ToString().ToLowerInvariant(), message, title);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or InvalidOperationException or TaskCanceledException)
            {
                // Circuit gone or prerendering – nothing to show the toast on
            }
        }
    }
}
