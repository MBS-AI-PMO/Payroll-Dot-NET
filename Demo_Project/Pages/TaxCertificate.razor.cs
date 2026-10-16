using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using PayslipMgtSystem.Helper;
using System.Data;

namespace PayslipMgtSystem.Pages
{
    public partial class TaxCertificate : IDisposable
    {
        private string searchQuery;
        public string name { get; set; }
        private IEnumerable<UploadFileVM> filteredFileContent;
        public int selectedYear { get; set; } 
        public int selectedMonth { get; set; }
        UploadFile filess = new();
        private List<UploadFileVM> fileContent = new();
        private string emailStatusMessage;
        private string emailStatusMessage2;
        private string filterMessage;
        private bool isSendingEmails = false;
        private string message;

        [Inject] private ItcEmailQueue ItcEmails { get; set; } = default!;
        [Inject] private DocumentEmailService DocumentEmails { get; set; } = default!;
        [Inject] private PdfRenderer PdfRenderer { get; set; } = default!;
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
        private UploadFileVM selectedRecord; // Add this property to store the selected record
        private void ShowDetails(UploadFileVM record)
        {
            selectedRecord = record; // Set the selected record
        }
        //    else
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

        //    else
        private async Task YearMonthFilter()
        {
            await LoadRecord();
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
        private string fileDownloadUrl;
        private string fileName;

        private bool selectAll;
        private bool selectPage;

        private List<EmailRequest> SelectedIds = new List<EmailRequest>();
        private List<EmailRequest> RemovedIds = new List<EmailRequest>();
        private List<EmailRequest> RemoveEmailRequests = new List<EmailRequest>();
        public void ToggleSelectPage()
        {
            selectPage = !selectPage;

            // Get the current page's CNIC and Year
            var currentPageEmailRequests = filteredFileContent
                .Select(f => new EmailRequest { Cnic = f.Cnic, Year = f.Year })
                .Distinct()
                .ToList();

            if (selectPage)
            {
                // Select all records on the current page
                foreach (var emailRequest in currentPageEmailRequests)
                {
                    if (!SelectedIds.Contains(emailRequest))
                    {
                        SelectedIds.Add(emailRequest);
                    }
                    ToggleSelect(emailRequest.Cnic, emailRequest.Year, selectPage);

                }
            }
            else
            {
                // Deselect all records on the current page
                foreach (var emailRequest in currentPageEmailRequests)
                {
                    SelectedIds.Remove(emailRequest);
                }
            }
        }

        private void ToggleSelectAll()
        {
            // Toggle the selection state
            selectAll = !selectAll;

            if (selectAll)
            {
                // Select all items from filteredFileContent
                // Assuming filteredFileContent is a list of objects that include Cnic and Year
                SelectedIds = filteredFileContent
                    .Select(f => new EmailRequest { Cnic = f.Cnic, Year = f.Year })
                    .Distinct()
                    .ToList();
                foreach (var item in SelectedIds)
                {
                    ToggleSelect(item.Cnic, item.Year,selectAll);
                }
            }
            else
            {
                // Clear the selection and removed lists
                SelectedIds = new List<EmailRequest>();
                RemovedIds = new List<EmailRequest>();
            }
        }

        private void ToggleSelect(string cnic, int year, bool isToSelect)
        {
            // Create a new EmailRequest object with the given cnic and year
            var request = new EmailRequest { Cnic = cnic, Year = year };

            if (isToSelect)
            {
                // Add the request to SelectedIds if it's not already present
                if (!SelectedIds.Any(x => x.Cnic == cnic && x.Year == year))
                {
                    SelectedIds.Add(request);
                }

                // Remove the request from RemovedIds if it's present
                var removedRequest = RemovedIds.FirstOrDefault(x => x.Cnic == cnic && x.Year == year);
                if (removedRequest != null)
                {
                    RemovedIds.Remove(removedRequest);
                }
            }
            else
            {
                // Add the request to RemovedIds if it's not already present
                if (!RemovedIds.Any(x => x.Cnic == cnic && x.Year == year))
                {
                    RemovedIds.Add(request);
                }

                // Remove the request from SelectedIds if it's present
                var selectedRequest = SelectedIds.FirstOrDefault(x => x.Cnic == cnic && x.Year == year);
                if (selectedRequest != null)
                {
                    SelectedIds.Remove(selectedRequest);
                }
            }
        }

        private bool showConfirmationPopup = false;

        // This method shows the confirmation popup
        private void ShowConfirmationPopup()
        {
            showConfirmationPopup = true;
        }

        // This method closes the confirmation popup without generating the request
        private void ClosePopup()
        {
            showConfirmationPopup = false;
        }

        public async Task GenerateEmailRequest()
        {
            showConfirmationPopup = false;
            int? startYear = null;
            int? startMonth = null;
            int? endYear = null;
            int? endMonth = null;
            if (selectedYear == 0)
            {
                startYear = DateTime.Now.Year - 1;
                startMonth = 7; // July of selectedYear
                endYear = DateTime.Now.Year;
                endMonth = 6; // June of the next year

            }
            if (selectedYear != 0)
            {
                startYear = selectedYear;
                startMonth = 7; // July of selectedYear
                endYear = selectedYear + 1;
                endMonth = 6; // June of the next year
            }
            // Fetch the data from the service
            var ids =  uploadFileService.GetDataITCIdsAsync(searchQuery, startYear, startMonth, endYear, endMonth, selectedYear, selectedMonth);

            if (selectAll)
            {
                // Filter out removed IDs from the list
                var tempIds = ids
                    .Where(x => !RemovedIds.Any(r => r.Cnic == x.Cnic && r.Year == x.Year))
                    .ToList();

                ItcEmails.Enqueue(new ItcEmailBatch(tempIds, ItcTemplate.ResolveStartYear(selectedYear)));
            }
            else
            {
                // Filter by selected IDs
                var tempIds = ids
                    .Where(x => SelectedIds.Any(s => s.Cnic == x.Cnic && s.Year == x.Year))
                    .ToList();

                ItcEmails.Enqueue(new ItcEmailBatch(tempIds, ItcTemplate.ResolveStartYear(selectedYear)));
               
            }
            message = "Request to Send Emails has been generated successfully.";
            StateHasChanged();

            await Task.Delay(3000);
            message = string.Empty;
            StateHasChanged();

            // Reset selection
            foreach (var item in filteredFileContent) item.Selected = false;
            selectAll = false;
            SelectedIds.Clear();
        }

        private async Task SendEmailWithPdfIndividually(UploadFileVM file)
        {
            isSendingEmails = true;
            var sent = await DocumentEmails.SendItcAsync(file, ItcTemplate.ResolveStartYear(selectedYear), PdfLayout.IndividualEmail);
            isSendingEmails = false;

            emailStatusMessage = sent ? "Emails sent successfully." : "The email could not be sent. Please check the employee's email address and try again.";
            StateHasChanged();
        }
        public async Task CloseMessage()
        {
            emailStatusMessage = string.Empty;
            StateHasChanged();
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

            int? startYear = null;
            int? startMonth = null;
            int? endYear = null;
            int? endMonth = null;
            if(selectedYear == 0)
            {
                startYear = DateTime.Now.Year - 1;
                startMonth = 7; // July of selectedYear
                endYear = DateTime.Now.Year;
                endMonth = 6; // June of the next year

            }
            if (selectedYear != 0)
            {
                startYear = selectedYear;
                startMonth = 7; // July of selectedYear
                endYear = selectedYear + 1;
                endMonth = 6; // June of the next year
            }

            // Load data based on the current pagination and filters
            singleRecord = await uploadFileService.GetDataITCAsync(dTO, searchQuery, startYear, startMonth, endYear, endMonth, selectedYear, selectedMonth);
            var totalCount = singleRecord.TotalCount;   
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
            if (selectAll)
            {
                SelectedIds = filteredFileContent.ToList()
             .Where(x => !RemovedIds.Any(r => r.Cnic == x.Cnic && r.Year == x.Year))
             .Select(x => new EmailRequest { Cnic = x.Cnic , Year =x.Year} )
             .ToList(); 
                SelectedIds = SelectedIds.Distinct().ToList();
            }
            if (selectPage)
            {
                selectPage = false;
                // Ensure that the selection state reflects the `SelectedIds` list
                //        // Optionally, set some state in `filteredFileContent` to indicate selection
                //        // This might be useful if you need to show selected state in the UI
            }
            StateHasChanged();
        }

        #region Export
        public void ExportClick(string exportType)
        {
            string exportName = "Employees Data";
            var dataTable = GetDatatable(exportName);
            ExportHelper.Export(exportType, exportName, JSRuntime, dataTable);
        }
        private DataTable GetDatatable(string exportName)
        {
            DataTable dataTable = new DataTable(exportName);

            dataTable.Columns.AddRange(new DataColumn[] {
                                            new DataColumn("Service Category"),
                                            new DataColumn("Name"),
                                            new DataColumn("Cnic"),
                                            new DataColumn("Email"),
                                            new DataColumn("Year"),
                                            new DataColumn("Month"),
                                            new DataColumn("Net Amount"),
            });
            AddRowsToDataTable(dataTable);

            return dataTable;
        }
        private void AddRowsToDataTable(DataTable dataTable)
        {
            var context = ScopeFactory.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            var query = (from a in context.Files
                         where (string.IsNullOrEmpty(searchQuery) || a.EmployeeName.ToLower().Contains(searchQuery) || a.Cnic.ToLower().Contains(searchQuery))
                         && selectedYear == 0 || selectedMonth == 0
                         && selectedYear == a.Year || selectedMonth != 0 && selectedYear == a.Year && selectedMonth == a.Month
                         select new UploadFileVM()
                         {
                             Id = a.Id,
                             ServiceCategory = a.ServiceCategory,
                             EmployeeName = a.EmployeeName,
                             Cnic = a.Cnic,
                             Email = a.Email,
                             Year = a.Year,
                             Month = a.Month,
                             NetAmount = a.NetAmount
                         }).AsEnumerable();
            var Rows = (from a in query
                        select dataTable.Rows.Add(a.ServiceCategory, a.EmployeeName, a.Cnic, a.Email, a.Year, a.Month, a.NetAmount)).Count();
        }
        #endregion
        private async Task DownloadPdf2(UploadFileVM file)
        {
            var startYear = ItcTemplate.ResolveStartYear(selectedYear);
            var pdfBytes = PdfRenderer.Render(ItcTemplate.Render(file, startYear), PdfLayout.Standard);
            using var memoryStream = new MemoryStream(pdfBytes);
            using var streamRef = new DotNetStreamReference(stream: memoryStream);
            await JSRuntime.InvokeVoidAsync("downloadFileStream", ItcTemplate.FileName(file, startYear), streamRef);
        }
    }
}