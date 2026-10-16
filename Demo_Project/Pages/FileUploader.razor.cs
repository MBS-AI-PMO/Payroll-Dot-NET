using PayslipMgtSystem.Models;
using ExcelDataReader;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OfficeOpenXml;

namespace PayslipMgtSystem.Pages
{
    public partial class FileUploader
    {
        List<UploadFile> uploadResult;

        public async Task HandleFileUpload(InputFileChangeEventArgs e)
        {
            var file = e.File;

            // Ensure the file is an Excel file (.xlsx, .xls)
            if (file.ContentType == "application/vnd.ms-excel" ||
                file.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                uploadResult = await ProcessExcelFile(file);
            }
            else
            {
                // Handle invalid file type
                // Show an error message or alert
            }
        }

       public async Task<List<UploadFile>> ProcessExcelFile(IBrowserFile file)
        {
            List<UploadFile> uploadResult = new List<UploadFile>();

            using (var stream = file.OpenReadStream())
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    while (reader.Read())
                    {
                        var uploadFile = new UploadFile
                        {
                            SrNo = reader.GetInt32(0),
                            //EmoployeeId = reader.GetString(1),
                            ServiceCategory = reader.GetString(2),
                            EmployeeName = reader.GetString(3),
                            Cnic = reader.GetString(4),
                            Email = reader.GetString(5),
                            JoiningDate = reader.GetString(6),
                            MonthDays = reader.GetInt32(7),
                            PresentDays = reader.GetInt32(8),
                            OfferedSalary = reader.GetInt32(9),
                            LeaveDeduction = reader.GetInt32(10),
                            BasicPayafterDeduction = reader.GetInt32(11),
                            Arrears = reader.GetInt32(12),
                            Allowances = reader.GetInt32(13),
                            AdvancesLoan = reader.GetInt32(14),
                            GrossSalary = reader.GetInt32(15),
                            AdvancesLoanDeduction = reader.GetInt32(16),
                            EOBI = reader.GetInt32(17),
                            OtherDeduction = reader.GetInt32(18),
                            WHITDeduction = reader.GetInt32(19),
                            TotalDeductions = reader.GetInt32(20),
                            NetAmount = reader.GetInt32(21)
                        };

                        uploadResult.Add(uploadFile);
                    }
                }
            }

            return uploadResult;
        }

    }
}