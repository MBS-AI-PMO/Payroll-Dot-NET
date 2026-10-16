using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace PayslipMgtSystem.Pages
{
    public partial class AddEmployees : IDisposable
    {
        private string searchQuery;
        public string name { get; set; }
        private IEnumerable<UploadFileVM> filteredFileContent;
        public int selectedYear { get; set; }
        public int selectedMonth { get; set; }
        UploadFile filess = new();
        private List<UploadFileVM> fileContent = new();
        private UploadFileVM selectedRecord;
        private IBrowserFile selectedFile;
        private string emailStatusMessage;
        private string emailStatusMessage2;
        private string filterMessage;
        private bool isSendingEmails = false;
        private bool selectAll;
        [Inject] private PayrollExcel PayrollExcel { get; set; } = default!;
        [Inject]
        protected IWebHostEnvironment _webHostEnvironment { get; set; }
        [Inject]
        protected IServiceScopeFactory ScopeFactory { get; set; }
        private int SelectedYear { get; set; }
        private List<int> YearRange { get; set; }
        protected override async Task OnInitializedAsync()
        {
            await LoadRecord();
        }
        [Inject] private ToastService Toast { get; set; } = default!;
        [Inject] private ILogger<AddEmployees> Logger { get; set; } = default!;

        private List<PayrollImportError> uploadErrors = new();
        private List<string> ignoredColumns = new();
        private bool isUploading;
        private int fileInputKey;

        private void HandleFileSelected(InputFileChangeEventArgs e)
        {
            selectedFile = e.File;
            uploadErrors.Clear();
        }
        private bool showReplaceConfirmationDialog = false;
        private int existingRecordCount = 0;
        private bool showYearMonthExistDialog = false;

        private static string MonthName(int month) =>
            System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Math.Clamp(month, 1, 12));

        private async Task Add()
        {
            uploadErrors.Clear();
            ignoredColumns.Clear();

            if (filess.Id != 0)
            {
                await EnsureItcRequestYear();
                await uploadFileService.UpdateRecord(filess);
                NavigationManager.NavigateTo("/", true);
                return;
            }

            if (filess.Year == 0 || filess.Month == 0)
            {
                uploadErrors.Add(new(null, null, "Please select the payroll year and month before uploading."));
                return;
            }
            if (selectedFile == null)
            {
                uploadErrors.Add(new(null, null, "Please choose the payroll sheet (.xlsx) to upload."));
                return;
            }

            var file = selectedFile;
            var fileNameError = PayrollExcel.CheckFileName(file.Name);
            if (fileNameError != null)
            {
                uploadErrors.Add(fileNameError);
                return;
            }
            if (file.Size == 0)
            {
                uploadErrors.Add(new(null, null, $"\"{file.Name}\" is empty (0 bytes). Please choose the correct payroll sheet."));
                return;
            }
            if (file.Size > PayrollExcel.MaxFileSize)
            {
                uploadErrors.Add(new(null, null, $"\"{file.Name}\" is {file.Size / 1024.0 / 1024.0:0.#} MB. The maximum upload size is {PayrollExcel.MaxFileSize / 1024 / 1024} MB – remove extra sheets, images or formatting and try again."));
                return;
            }

            isUploading = true;
            try
            {
                var filePath = Path.Combine(Environment.WebRootPath, "UploadFiles", Path.GetFileName(file.Name));
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.OpenReadStream(PayrollExcel.MaxFileSize).CopyToAsync(stream);
                }

                var import = PayrollExcel.Read(filePath);
                if (!import.IsValid)
                {
                    uploadErrors.AddRange(import.Errors);
                    return;
                }

                fileContent = import.Rows;
                ignoredColumns = import.IgnoredColumns;

                if (await CheckYearMonthExist(filess.Year, filess.Month))
                {
                    showYearMonthExistDialog = true;
                    return; // Wait for user response
                }

                var existingRecords = await CheckForExistingRecords(fileContent);
                if (existingRecords.Any())
                {
                    existingRecordCount = existingRecords.Count;
                    showReplaceConfirmationDialog = true;
                    return; // Wait for user response
                }

                await SaveUploadAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Payroll upload failed for {File}", file.Name);
                uploadErrors.Add(new(null, null, "The sheet could not be uploaded because of an unexpected error. Please try again; if it keeps failing, contact the system administrator."));
            }
            finally
            {
                isUploading = false;
            }
        }

        private async Task EnsureItcRequestYear()
        {
            var year = filess.Month < 7 ? filess.Year - 1 : filess.Year;
            if (!await _context.ITCRequests.AnyAsync(x => x.Year == year))
            {
                await _context.ITCRequests.AddAsync(new TaxCertificateRequest() { Year = year });
                await _context.SaveChangesAsync();
            }
        }

        private async Task SaveUploadAsync(List<UploadFile>? recordsToReplace = null)
        {
            isUploading = true;
            try
            {
                await EnsureItcRequestYear();
                if (recordsToReplace != null)
                    await uploadFileService.DeleteExistingRecords(recordsToReplace);
                await uploadFileService.SaveFileContentToDatabase(fileContent, filess.Year, filess.Month, filess.isPdfGenerated, filess.isTaxGenerated);

                var period = $"{MonthName(filess.Month)} {filess.Year}";
                var count = fileContent.Count;
                var ignored = ignoredColumns.ToList();

                await JSRuntime.InvokeVoidAsync("HideModal", "#myModal");
                filess = new();
                selectedFile = null;
                fileContent = new();
                fileInputKey++;
                await LoadRecord();

                await Toast.Success($"{count} employee record(s) uploaded for {period}.", "Upload complete");
                if (ignored.Any())
                    await Toast.Warning($"These columns are not part of the template and were not imported: {string.Join(", ", ignored)}.", "Some columns were ignored");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Saving uploaded payroll rows failed");
                uploadErrors.Add(new(null, null, "The records could not be saved. Nothing was uploaded – please try again."));
            }
            finally
            {
                isUploading = false;
            }
        }

        private async Task<bool> CheckYearMonthExist(int year, int month)
        {
            return await _context.Files.AnyAsync(x => x.Year == year && x.Month == month);
        }

        private async Task<List<UploadFile>> CheckForExistingRecords(List<UploadFileVM> fileContent)
        {
            var cnics = fileContent.Select(x => x.Cnic).Where(c => c != null).Distinct().ToList();
            return await _context.Files
                .Where(x => x.Year == filess.Year && x.Month == filess.Month && cnics.Contains(x.Cnic))
                .ToListAsync();
        }

        private async Task OnYearMonthDialogClose(bool confirmed)
        {
            showYearMonthExistDialog = false;
            if (!confirmed)
                return;

            var existingRecords = await CheckForExistingRecords(fileContent);
            if (existingRecords.Any())
            {
                existingRecordCount = existingRecords.Count;
                showReplaceConfirmationDialog = true;
                return; // Wait for user response
            }

            await SaveUploadAsync();
        }

        private async Task OnReplaceDialogClose(bool confirmed)
        {
            showReplaceConfirmationDialog = false;
            if (!confirmed)
                return;

            await SaveUploadAsync(await CheckForExistingRecords(fileContent));
        }
        private async Task DownloadTemplate()
        {
            var templateBytes = PayrollExcel.CreateTemplate();
            await using var stream = new MemoryStream(templateBytes);
            var fileName = "Template.xlsx";
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, contentType, stream.ToArray());
        }
        public async void UpdateRecord(int Id)
        {
            try
            {
                filess = await uploadFileService.GetRecord(Id);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private void ShowDetails(UploadFileVM record)
        {
            selectedRecord = record; // Set the selected record
        }
        public async void EditData()
        {
            filess = new();
        }
        private async Task SelectedPage(int page)
        {
            /// Epagination.currentPage = page;
            await LoadRecord(page, dTO.QuantityPerPage);

        }
        PaginationVM Epagination = new();
        PaginationDTO dTO = new();
        public PaginationVM singleRecord = new();

        int totalcount = 0;
        private async Task LoadRecord(int page = 1, int quantityPerPage = 50)
        {
            Epagination = new();
            Epagination.currentPage = page;
            dTO = new PaginationDTO() { Page = page, QuantityPerPage = quantityPerPage };
            // Load data based on the current pagination and filters
            singleRecord = await uploadFileService.GetDataAsync(dTO, name = searchQuery, selectedYear, selectedMonth);
            if (selectedYear == 0 && selectedMonth != 0)
            {
                filterMessage = "Please Select Year.";
                StateHasChanged();
                await Task.Delay(3000); // Wait for 3 seconds
                filterMessage = string.Empty;
                StateHasChanged();
            }
            if (singleRecord != null)
            {
                fileContent = singleRecord.uploadFileVMs;
                Epagination.TotalPages = singleRecord.TotalPages;
                totalcount = Epagination.TotalCount = singleRecord.TotalCount;
                filteredFileContent = fileContent;
            }

            YearRange = GetYearRange();
            if (selectAll) foreach (var item in filteredFileContent) item.Selected = true;
            StateHasChanged();
        }
        public List<int> GetYearRange()
        {
            var currentYear = DateTime.Now.Year;
            var years = new List<int>();

            for (int year = currentYear; year <= currentYear + 10; year++)
            {
                years.Add(year);
            }
            return years;
        }
        private readonly SearchDebouncer searchDebouncer = new();
        private Task OnInput(ChangeEventArgs e)
        {
            searchQuery = e.Value?.ToString() ?? "";
            return searchDebouncer.RunAsync(Filter);
        }

        public void Dispose() => searchDebouncer.Dispose();
        private async Task Filter()
        {
            Epagination.currentPage = 1;
            await LoadRecord();
        }
        private async Task YearMonthFilter()
        {
            await LoadRecord();
        }
    }
}