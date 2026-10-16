using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using PayslipMgtSystem.Models;
using PayslipMgtSystem.Services;

namespace PayslipMgtSystem.Pages
{
    public partial class MyPayslips
    {
        [Inject] private EmployeePayslipService PayslipService { get; set; } = default!;
        [Inject] private ToastService Toast { get; set; } = default!;
        [Inject] private ILogger<MyPayslips> Logger { get; set; } = default!;
        [CascadingParameter] private Task<AuthenticationState> AuthState { get; set; } = default!;

        private List<PayslipSummary> payslips = new();
        private bool loading = true;
        private bool loadFailed;
        private bool notLinked;
        private string? cnic;

        private int selectedYear;
        private int selectedMonth;

        private UploadFile? selected;
        private int? viewingId;
        private ElementReference closeButton;
        private bool focusDialog;

        private IEnumerable<PayslipSummary> Filtered => payslips
            .Where(p => selectedYear == 0 || p.Year == selectedYear)
            .Where(p => selectedMonth == 0 || p.Month == selectedMonth);

        protected override Task OnInitializedAsync() => LoadAsync();

        private async Task LoadAsync()
        {
            loading = true;
            loadFailed = false;
            try
            {
                var user = (await AuthState).User;
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                cnic = userId == null ? null : await PayslipService.GetCnicForUserAsync(userId);
                notLinked = string.IsNullOrWhiteSpace(cnic);
                payslips = notLinked ? new List<PayslipSummary>() : await PayslipService.GetPayslipsAsync(cnic!);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load payslips");
                loadFailed = true;
            }
            finally
            {
                loading = false;
            }
        }

        private void ClearFilters()
        {
            selectedYear = 0;
            selectedMonth = 0;
        }

        private async Task ViewPayslip(PayslipSummary summary)
        {
            viewingId = summary.Id;
            try
            {
                selected = await PayslipService.GetPayslipAsync(cnic ?? "", summary.Id);
                if (selected == null)
                    await Toast.Error("This payslip could not be found.");
                else
                    focusDialog = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load payslip {Id}", summary.Id);
                await Toast.Error("We couldn't open this payslip. Please try again.");
            }
            finally
            {
                viewingId = null;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (focusDialog && selected != null)
            {
                focusDialog = false;
                try { await closeButton.FocusAsync(); } catch { /* dialog not attached yet */ }
            }
        }

        private void CloseDetail() => selected = null;

        private void OnDetailKey(KeyboardEventArgs e)
        {
            if (e.Key == "Escape") CloseDetail();
        }

        private Task OnDownload(PayslipSummary p) =>
            Toast.Info($"Your payslip for {p.PeriodName} is downloading.", "Download started");

        private static string PdfUrl(int id, bool download) => $"payslips/{id}/pdf{(download ? "?download=true" : "")}";

        private static string Money(int? value) => value is null or 0 ? "—" : value.Value.ToString("N0");

        private static string MonthName(int month) =>
            CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Math.Clamp(month, 1, 12));
    }
}
