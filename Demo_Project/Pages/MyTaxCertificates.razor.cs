using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;

namespace PayslipMgtSystem.Pages
{
    public partial class MyTaxCertificates
    {
        [Inject] private EmployeeTaxCertificateService CertificateService { get; set; } = default!;
        [Inject] private ToastService Toast { get; set; } = default!;
        [Inject] private ILogger<MyTaxCertificates> Logger { get; set; } = default!;
        [CascadingParameter] private Task<AuthenticationState> AuthState { get; set; } = default!;

        private List<TaxCertificateSummary> certificates = new();
        private bool loading = true;
        private bool loadFailed;
        private bool notLinked;

        private TaxCertificateSummary? selected;
        private TaxCertificateSummary? focusedFor;
        private ElementReference closeButton;

        protected override Task OnInitializedAsync() => LoadAsync();

        private async Task LoadAsync()
        {
            loading = true;
            loadFailed = false;
            try
            {
                var user = (await AuthState).User;
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                var cnic = userId == null ? null : await CertificateService.GetCnicForUserAsync(userId);
                notLinked = string.IsNullOrWhiteSpace(cnic);
                certificates = notLinked ? new List<TaxCertificateSummary>() : await CertificateService.GetCertificatesAsync(cnic!);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load tax certificates");
                loadFailed = true;
            }
            finally
            {
                loading = false;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Move focus into the dialog when it opens
            if (selected != null && !ReferenceEquals(selected, focusedFor))
            {
                focusedFor = selected;
                try { await closeButton.FocusAsync(); } catch { /* dialog not attached yet */ }
            }
            else if (selected == null)
            {
                focusedFor = null;
            }
        }

        private void CloseDetail() => selected = null;

        private void OnDetailKey(KeyboardEventArgs e)
        {
            if (e.Key == "Escape") CloseDetail();
        }

        private Task OnDownload(TaxCertificateSummary c) =>
            Toast.Info($"Your tax certificate for {c.FinancialYear} is downloading.", "Download started");

        private static string PdfUrl(int startYear, bool download) =>
            $"tax-certificates/{startYear}/pdf{(download ? "?download=true" : "")}";

        private static string Money(int value) => value == 0 ? "—" : value.ToString("N0");
    }
}
