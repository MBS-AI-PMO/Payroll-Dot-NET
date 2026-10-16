using Microsoft.AspNetCore.Components;
using PayslipMgtSystem.Services;

namespace PayslipMgtSystem.Pages
{
    public partial class UserManagement : IDisposable
    {
        [Inject] private UserManagementService Users { get; set; } = default!;
        [Inject] private ToastService Toast { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private ILogger<UserManagement> Logger { get; set; } = default!;

        private const int PageSize = 15;

        private UserOverview? overview;
        private bool loadingOverview = true;

        private EmployeeAccountPage? accounts;
        private bool loadingAccounts = true;
        private string search = "";
        private readonly SearchDebouncer searchDebounce = new();
        private string? resendingId;

        private bool showConfirm;
        private bool isRunning;
        private BulkUserProgress? progress;
        private BulkUserResult? lastResult;
        private BulkUserStatus? resultFilter;
        private readonly CancellationTokenSource disposeCts = new();

        private int ProgressPercent => progress == null || progress.Total == 0 ? 0 : (int)(progress.Processed * 100.0 / progress.Total);

        private IEnumerable<BulkUserRecord> FilteredResults =>
            lastResult == null ? Enumerable.Empty<BulkUserRecord>()
            : resultFilter == null ? lastResult.Records
            : lastResult.Records.Where(r => r.Status == resultFilter);

        protected override async Task OnInitializedAsync()
        {
            isRunning = UserManagementService.IsBulkRunning;
            await Task.WhenAll(LoadOverview(), LoadAccounts(1));
        }

        private async Task LoadOverview()
        {
            try
            {
                overview = await Users.GetOverviewAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load user overview");
                await Toast.Error("We couldn't load the account statistics. Please refresh the page.");
            }
            finally
            {
                loadingOverview = false;
            }
        }

        private async Task LoadAccounts(int page)
        {
            loadingAccounts = true;
            try
            {
                accounts = await Users.GetEmployeeAccountsAsync(search, page, PageSize);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load employee accounts");
                await Toast.Error("We couldn't load the employee accounts. Please refresh the page.");
            }
            finally
            {
                loadingAccounts = false;
            }
        }

        private Task OnSearchInput(ChangeEventArgs e)
        {
            search = e.Value?.ToString() ?? "";
            return searchDebounce.RunAsync(() => LoadAccounts(1));
        }

        private void AskCreateUsers()
        {
            if (isRunning) return;
            showConfirm = true;
        }

        private async Task OnConfirmClosed(bool confirmed)
        {
            showConfirm = false;
            if (!confirmed || isRunning) return;

            isRunning = true;
            lastResult = null;
            resultFilter = null;
            progress = null;
            StateHasChanged();

            var reporter = new Progress<BulkUserProgress>(p =>
            {
                progress = p;
                InvokeAsync(StateHasChanged);
            });

            try
            {
                // Run off the render thread so progress updates can be pushed while the job runs
                var result = await Task.Run(() => Users.CreateUsersAsync(reporter, disposeCts.Token));
                lastResult = result;

                if (result.Failed + result.MissingEmail > 0)
                {
                    resultFilter = result.Failed > 0 ? BulkUserStatus.Failed : BulkUserStatus.MissingEmail;
                    await Toast.Warning($"{result.Created} user(s) created. {result.Failed + result.MissingEmail} record(s) need attention.", "User creation finished");
                }
                else if (result.Created == 0)
                {
                    await Toast.Info("All employees already have an account. No new users were created.", "Users already exist");
                }
                else
                {
                    await Toast.Success($"{result.Created} user(s) created. Employees can set their password using \"Forgot password?\" on the login page.", "Users created");
                }
            }
            catch (InvalidOperationException ex)
            {
                await Toast.Warning(ex.Message, "Please wait");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Bulk user creation failed");
                await Toast.Error("User creation could not be completed. Please try again.");
            }
            finally
            {
                isRunning = false;
            }

            await Task.WhenAll(LoadOverview(), LoadAccounts(1));
        }

        private async Task Resend(EmployeeAccount account)
        {
            resendingId = account.Id;
            try
            {
                await Users.ResendInvitationAsync(account.Id, Navigation.BaseUri);
                await Toast.Success($"A {(account.PasswordSet ? "password reset link" : "set-password invitation")} was sent to {account.Email}.", "Email sent");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to resend invitation to user {UserId}", account.Id);
                await Toast.Error("The email could not be sent. Please check the email settings and try again.");
            }
            finally
            {
                resendingId = null;
            }
        }

        public void Dispose()
        {
            searchDebounce.Dispose();
            disposeCts.Cancel();
        }
    }
}
