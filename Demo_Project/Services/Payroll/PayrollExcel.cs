using System.ComponentModel.DataAnnotations;
using System.Globalization;
using OfficeOpenXml;

namespace PayslipMgtSystem.Services.Payroll
{
    /// <summary>One problem found in an uploaded payroll sheet. Row/Column are null for file-level problems.</summary>
    public sealed record PayrollImportError(int? Row, string? Column, string Message, string? Value = null)
    {
        public string Location => Row == null ? (Column ?? "File") : Column == null ? $"Row {Row}" : $"Row {Row}, {Column}";
    }

    public class PayrollImportResult
    {
        public List<UploadFileVM> Rows { get; } = new();
        public List<PayrollImportError> Errors { get; } = new();

        /// <summary>Columns present in the sheet that are not part of the template (their values are not imported).</summary>
        public List<string> IgnoredColumns { get; } = new();

        public bool IsValid => Errors.Count == 0;
    }

    /// <summary>Reads and validates the monthly payroll workbook, and produces the blank upload template.</summary>
    public class PayrollExcel
    {
        public const long MaxFileSize = 10 * 1024 * 1024;

        private sealed record ColumnSpec(string Header, params string[] Aliases);

        // Template columns, in template order. Headers are matched ignoring case, spaces and punctuation,
        // so "Sr No.", "Employee Name" or "WH IT Deduction" are accepted too.
        private static readonly ColumnSpec[] Columns =
        {
            new("SrNo", "sno", "serialno", "srnumber"),
            new("ServiceCategory", "category"),
            new("EmployeeName", "name"),
            new("Cnic", "cnicno", "cnicnumber"),
            new("Email", "emailaddress", "emailid"),
            new("JoiningDate", "dateofjoining", "doj"),
            new("MonthDays", "daysinmonth"),
            new("PresentDays"),
            new("OfferedSalary"),
            new("LeaveDeduction", "leaveabsentdeduction", "absentdeduction"),
            new("BasicPayAfterDeduction"),
            new("Arrears"),
            new("Allowances"),
            new("AdvancesLoan", "advances", "advance", "loan"),
            new("GrossSalary", "grosspay"),
            new("AdvancesLoanDeduction", "advancededuction", "loandeduction"),
            new("EOBI"),
            new("OtherDeduction", "otherdeductions"),
            new("WHITDeduction", "whit", "incometax", "incometaxdeduction"),
            new("TotalDeductions", "totaldeduction"),
            new("NetAmount", "netpay", "netsalary"),
        };

        // Recognised extra column that the template does not include but the payslip can show
        private static readonly ColumnSpec MedicalAllowanceColumn = new("MedicalAllowance", "medical");

        private const int MaxErrors = 200;

        private readonly ILogger<PayrollExcel> _logger;

        public PayrollExcel(ILogger<PayrollExcel> logger)
        {
            _logger = logger;
        }

        /// <summary>Checks the file name before the upload is read.</summary>
        public static PayrollImportError? CheckFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".xlsx" => null,
                ".xls" => new(null, null, $"\"{fileName}\" is in the old Excel 97-2003 format (.xls). Open it in Excel and use File › Save As › Excel Workbook (*.xlsx), then upload it again."),
                ".csv" => new(null, null, $"\"{fileName}\" is a CSV file. Open it in Excel and save it as an Excel Workbook (*.xlsx), then upload it again."),
                "" => new(null, null, $"\"{fileName}\" has no file extension. Please upload the payroll sheet as an Excel Workbook (*.xlsx)."),
                _ => new(null, null, $"\"{fileName}\" is a {extension} file. Only Excel Workbooks (*.xlsx) based on the downloadable template can be uploaded.")
            };
        }

        /// <summary>Reads and validates the first worksheet. Rows are only returned when there are no errors.</summary>
        public PayrollImportResult Read(string filePath)
        {
            var result = new PayrollImportResult();

            ExcelPackage package;
            try
            {
                package = new ExcelPackage(new FileInfo(filePath));
                _ = package.Workbook.Worksheets.Count; // forces the workbook to load
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not open uploaded payroll workbook {File}", Path.GetFileName(filePath));
                result.Errors.Add(new(null, null, "The file could not be opened as an Excel workbook. It may be damaged, password-protected, or not really an .xlsx file. Open it in Excel, save it again as an Excel Workbook (*.xlsx) and retry."));
                return result;
            }

            using (package)
            {
                if (package.Workbook.Worksheets.Count == 0)
                {
                    result.Errors.Add(new(null, null, "The workbook does not contain any worksheets."));
                    return result;
                }

                var sheet = package.Workbook.Worksheets[0];
                if (sheet.Dimension == null)
                {
                    result.Errors.Add(new(null, $"Sheet \"{sheet.Name}\"", "The first worksheet is empty. Put the payroll data on the first sheet, starting with the header row in row 1."));
                    return result;
                }

                var map = ReadHeader(sheet, result);
                if (!result.IsValid)
                    return result;

                ReadRows(sheet, map, result);
            }

            if (!result.IsValid)
                result.Rows.Clear();
            return result;
        }

        private static string Normalize(string? text) =>
            new string((text ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        private static string ColumnLetter(int column) => ExcelCellAddress.GetColumnLetter(column);

        private static bool Matches(ColumnSpec spec, string normalizedHeader) =>
            normalizedHeader == Normalize(spec.Header) || spec.Aliases.Contains(normalizedHeader);

        /// <summary>Maps each known column to its position in the sheet and reports header problems.</summary>
        private static Dictionary<string, int> ReadHeader(ExcelWorksheet sheet, PayrollImportResult result)
        {
            var map = new Dictionary<string, int>();
            var lastColumn = sheet.Dimension.End.Column;

            for (int col = 1; col <= lastColumn; col++)
            {
                var header = sheet.Cells[1, col].Text?.Trim();
                if (string.IsNullOrEmpty(header))
                    continue;

                var normalized = Normalize(header);
                var spec = Columns.FirstOrDefault(c => Matches(c, normalized))
                           ?? (Matches(MedicalAllowanceColumn, normalized) ? MedicalAllowanceColumn : null);

                if (spec == null)
                {
                    result.IgnoredColumns.Add($"{header} (column {ColumnLetter(col)})");
                    continue;
                }

                if (map.TryGetValue(spec.Header, out var firstCol))
                {
                    result.Errors.Add(new(1, $"Column {ColumnLetter(col)}",
                        $"\"{header}\" appears twice (columns {ColumnLetter(firstCol)} and {ColumnLetter(col)}). Keep only one {spec.Header} column.", header));
                    continue;
                }
                map[spec.Header] = col;
            }

            var missing = Columns.Where(c => !map.ContainsKey(c.Header)).Select(c => c.Header).ToList();
            if (missing.Count == Columns.Length)
            {
                result.IgnoredColumns.Clear(); // row 1 is data, not headers
                result.Errors.Add(new(1, null,
                    "Row 1 must contain the column headers (SrNo, ServiceCategory, EmployeeName, Cnic, Email, …). None were found – the headers may be missing or the data may not start in row 1. Please use the downloadable template."));
            }
            else if (missing.Count > 0)
            {
                result.Errors.Add(new(1, null,
                    $"Missing required column(s): {string.Join(", ", missing)}. Add them to the header row (row 1) exactly as in the downloadable template."));
            }

            return map;
        }

        private void ReadRows(ExcelWorksheet sheet, Dictionary<string, int> map, PayrollImportResult result)
        {
            var lastRow = sheet.Dimension.End.Row;
            var seenCnics = new Dictionary<string, int>();

            for (int row = 2; row <= lastRow && result.Errors.Count < MaxErrors; row++)
            {
                if (map.Values.All(col => string.IsNullOrWhiteSpace(sheet.Cells[row, col].Text)))
                    continue; // blank or formatting-only row

                var errors = new List<PayrollImportError>();
                string Label(string header) => $"{header} (column {ColumnLetter(map[header])})";
                object? Value(string header) => map.TryGetValue(header, out var col) ? sheet.Cells[row, col].Value : null;
                string? Text(string header) => map.TryGetValue(header, out var col) ? sheet.Cells[row, col].Text?.Trim() : null;

                int? Number(string header)
                {
                    if (!map.ContainsKey(header))
                        return 0;
                    if (TryReadNumber(Value(header), out var number, out var problem))
                        return number;
                    errors.Add(new(row, Label(header), problem!, Text(header)));
                    return 0;
                }

                var name = Text("EmployeeName");
                if (string.IsNullOrWhiteSpace(name))
                    errors.Add(new(row, Label("EmployeeName"), "Employee name is required."));

                var cnic = ReadCnic(Value("Cnic"), Text("Cnic"));
                var cnicDigits = new string((cnic ?? "").Where(char.IsDigit).ToArray());
                if (string.IsNullOrWhiteSpace(cnic))
                    errors.Add(new(row, Label("Cnic"), "CNIC is required."));
                else if (cnicDigits.Length != 13 || cnicDigits.Length != cnic.Count(c => c != '-' && c != ' '))
                    errors.Add(new(row, Label("Cnic"), "CNIC must be 13 digits, e.g. 61101-1234567-1.", cnic));
                else if (seenCnics.TryGetValue(cnicDigits, out var firstRow))
                    errors.Add(new(row, Label("Cnic"), $"The same CNIC is already used in row {firstRow}. Each employee can appear only once per sheet.", cnic));
                else
                    seenCnics[cnicDigits] = row;

                var email = Text("Email");
                if (!string.IsNullOrWhiteSpace(email) && (!new EmailAddressAttribute().IsValid(email) || !email.Contains('.')))
                    errors.Add(new(row, Label("Email"), "Email address is not valid.", email));

                string? joiningDate = null;
                if (!TryReadDate(Value("JoiningDate"), out joiningDate))
                    errors.Add(new(row, Label("JoiningDate"), "Joining date is not a valid date. Use an Excel date such as 01/08/2025.", Text("JoiningDate")));

                var monthDays = Number("MonthDays");
                var presentDays = Number("PresentDays");
                if (monthDays > 31)
                    errors.Add(new(row, Label("MonthDays"), "Month days cannot be more than 31.", Text("MonthDays")));
                if (presentDays > monthDays && monthDays > 0)
                    errors.Add(new(row, Label("PresentDays"), $"Present days ({presentDays}) cannot be more than month days ({monthDays}).", Text("PresentDays")));

                var line = new UploadFileVM
                {
                    SrNo = Number("SrNo") ?? 0,
                    ServiceCategory = Text("ServiceCategory"),
                    EmployeeName = name,
                    Cnic = cnic,
                    Email = string.IsNullOrWhiteSpace(email) ? null : email,
                    JoiningDate = joiningDate,
                    MonthDays = monthDays,
                    PresentDays = presentDays,
                    OfferedSalary = Number("OfferedSalary"),
                    LeaveDeduction = Number("LeaveDeduction"),
                    BasicPayafterDeduction = Number("BasicPayAfterDeduction"),
                    Arrears = Number("Arrears"),
                    Allowances = Number("Allowances"),
                    AdvancesLoan = Number("AdvancesLoan"),
                    GrossSalary = Number("GrossSalary"),
                    AdvancesLoanDeduction = Number("AdvancesLoanDeduction"),
                    EOBI = Number("EOBI"),
                    OtherDeduction = Number("OtherDeduction"),
                    WHITDeduction = Number("WHITDeduction"),
                    TotalDeductions = Number("TotalDeductions"),
                    NetAmount = Number("NetAmount"),
                    MedicalAllowance = map.ContainsKey(MedicalAllowanceColumn.Header) ? Number(MedicalAllowanceColumn.Header) : null
                };

                if (errors.Count == 0)
                    result.Rows.Add(line);
                else
                    result.Errors.AddRange(errors);
            }

            if (result.Errors.Count >= MaxErrors)
                result.Errors.Add(new(null, null, $"Stopped checking after {MaxErrors} problems. Fix the problems listed and upload the sheet again."));
            else if (result.IsValid && result.Rows.Count == 0)
                result.Errors.Add(new(null, null, "The sheet has a header row but no employee rows. Add the payroll data below the header row."));
        }

        private static bool TryReadNumber(object? value, out int number, out string? problem)
        {
            number = 0;
            problem = null;
            switch (value)
            {
                case null:
                    return true;
                case double d:
                    return ToWholeNumber(d, out number, out problem);
                case int or long or decimal or float:
                    return ToWholeNumber(Convert.ToDouble(value), out number, out problem);
                case string s:
                    s = s.Trim();
                    if (s is "" or "-")
                        return true; // blank or accounting dash means zero
                    if (double.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                        return ToWholeNumber(parsed, out number, out problem);
                    problem = "Must be a number (text found).";
                    return false;
                default:
                    problem = $"Must be a number ({value.GetType().Name.ToLowerInvariant()} found).";
                    return false;
            }
        }

        private static bool ToWholeNumber(double value, out int number, out string? problem)
        {
            number = 0;
            problem = null;
            if (value < 0)
            {
                problem = "Amounts and days cannot be negative.";
                return false;
            }
            if (value > int.MaxValue)
            {
                problem = "The number is too large.";
                return false;
            }
            number = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            return true;
        }

        private static string? ReadCnic(object? value, string? text) => value switch
        {
            null => null,
            double d => d.ToString("0", CultureInfo.InvariantCulture), // CNIC typed as a number
            _ => text
        };

        /// <summary>Joining dates are stored as Excel serial numbers (see UploadFile.JoinedAt).</summary>
        private static bool TryReadDate(object? value, out string? oaDate)
        {
            oaDate = null;
            switch (value)
            {
                case null:
                    return true;
                case DateTime dt:
                    oaDate = dt.ToOADate().ToString(CultureInfo.InvariantCulture);
                    return true;
                case double d when d > 0 && d < 2958466:
                    oaDate = d.ToString(CultureInfo.InvariantCulture);
                    return true;
                case string s when string.IsNullOrWhiteSpace(s):
                    return true;
                case string s when DateTime.TryParse(s, CultureInfo.GetCultureInfo("en-GB"), DateTimeStyles.None, out var parsed):
                    oaDate = parsed.ToOADate().ToString(CultureInfo.InvariantCulture);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Blank workbook with the header row the upload expects.</summary>
        public byte[] CreateTemplate()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Template");

            for (int i = 0; i < Columns.Length; i++)
                worksheet.Cells[1, i + 1].Value = Columns[i].Header;

            worksheet.Cells.Style.Font.Bold = true;
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }
    }
}
