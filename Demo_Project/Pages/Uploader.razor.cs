using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using PayslipMgtSystem.Helper;
using System.Data;

namespace PayslipMgtSystem.Pages
{
    public partial class Uploader : IDisposable
    {
        public static List<UploadFileVM> requestedList = new();

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
        private string SuccessMessage;

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

        //// Unsubscribe to avoid memory leaks when the component is destroyed
        private UploadFileVM selectedRecord; // Add this property to store the selected record
        private void ShowDetails(UploadFileVM record)
        {
            selectedRecord = record; // Set the selected record
        }

        private List<int> SelectedIds = new List<int>();
        private List<int> RemovedIds = new List<int>();
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

        //    else

        public async void ToggleSelectPage()
        {
            selectPage = !selectPage;
            // Get the IDs for the current page
            var currentPageIds = filteredFileContent.Select(f => f.Id).ToList();

            if (selectPage)
            {
                // Select all records on the current page
                foreach (var id in currentPageIds)
                {
                    if (!SelectedIds.Contains(id))
                    {
                        SelectedIds.Add(id);
                    }
                    ToggleSelect(id, selectPage);
                }
            }
            else
            {
                // Deselect all records on the current page
                foreach (var id in currentPageIds)
                {
                    SelectedIds.Remove(id);
                }
            }
        }

        private void ToggleSelectAll()
        {
            selectAll = !selectAll;
            SelectedIds = new List<int>();

            if (selectAll)
            {
                SelectedIds.AddRange(filteredFileContent.Select(f => f.Id));
                SelectedIds = SelectedIds.Distinct().ToList();
                foreach (var id in SelectedIds)
                {
                    ToggleSelect(id, selectAll);
                }

            }
            else
            {
                SelectedIds = new List<int>();
                RemovedIds = new List<int>();
            }

        }
        private void ToggleSelect(int Id, bool IsToSelect)
        {
            if (IsToSelect)
            {
                if (!SelectedIds.Contains(Id))
                    SelectedIds.Add(Id);
                if (RemovedIds.Contains(Id))
                    RemovedIds.Remove(Id);

            }
            else
            {
                if (!RemovedIds.Contains(Id))
                    RemovedIds.Add(Id);
                if (SelectedIds.Contains(Id))
                    SelectedIds.Remove(Id);
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
        [Inject] private PayslipEmailQueue PayslipEmails { get; set; } = default!;
        [Inject] private DocumentEmailService DocumentEmails { get; set; } = default!;
        [Inject] private PdfRenderer PdfRenderer { get; set; } = default!;
        public async Task GenerateEmailRequest()
        {
            showConfirmationPopup = false;
            var Ids = uploadFileService.GetDataIds(searchQuery, selectedYear, selectedMonth);
            if (selectAll)
            {
                var TempIds = await Ids.Where(x => !RemovedIds.Contains(x)).ToListAsync();
                PayslipEmails.Enqueue(new PayslipEmailBatch(TempIds));
            }
            else
            {
                var TempIds = await Ids.Where(x => SelectedIds.Contains(x)).ToListAsync();
                PayslipEmails.Enqueue(new PayslipEmailBatch(TempIds));
            }
            message = $"Request to Send Emails has been generated successfully.";
            StateHasChanged();

            await Task.Delay(3000);
            message = string.Empty;
            StateHasChanged();
            foreach (var item in filteredFileContent) item.Selected = false;
            selectAll = false;
            SelectedIds.Clear();

        }

        private async Task SendEmailWithPdfIndividually(UploadFileVM file)
        {
            isSendingEmails = true;
            var sent = await DocumentEmails.SendPayslipAsync(file, PdfLayout.IndividualEmail);
            isSendingEmails = false;

            emailStatusMessage = sent ? "Emails sent successfully." : "The email could not be sent. Please check the employee's email address and try again.";
            StateHasChanged();
        }
        public async Task CloseMessage()
        {
            emailStatusMessage = string.Empty;
            StateHasChanged();
        }
        //    return $@"
        //<!DOCTYPE html>
        //<html>
        //<head>
        //    <style>
        //    </style>
        //</head>
        //        <div>
        //        </div>
        //            <p>Office # 9, 2nd Floor,<br/> VIP Plaza, I-8 Markaz,<br/> Islamabad, Pakistan.</p>
        //            <p>Tel: +92 51 8446629</p>
        //            <p>Email: info@solochoicez.com</p>
        //            <p>Web: www.solochoicez.com</p>
        //        </div>
        //    </div>

        //    <hr />

        //    </table>

        //</body>

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
                SelectedIds.AddRange(filteredFileContent.Where(x => !RemovedIds.Contains(x.Id)).Select(f => f.Id));
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
        private async Task DownloadPdf(UploadFileVM file)
        {
            var pdfBytes = PdfRenderer.Render(PayslipTemplate.Render(file), PdfLayout.Standard);
            using var memoryStream = new MemoryStream(pdfBytes);
            using var streamRef = new DotNetStreamReference(stream: memoryStream);
            await JSRuntime.InvokeVoidAsync("downloadFileStream", PayslipTemplate.FileName(file), streamRef);
        }

    }

}