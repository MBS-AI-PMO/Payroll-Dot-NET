using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace PayslipMgtSystem.Helper
{
    public static class ExportHelper
    {
        private static readonly Dictionary<string, string> ContentTypes = new()
        {
            ["csv"] = "text/csv",
            ["xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ["pdf"] = "application/pdf"
        };

        private static void DownloadFile(byte[] buffer, string extension, string fileName, IJSRuntime jSRuntime)
        {
            jSRuntime.InvokeVoidAsync("downloadBase64File", ContentTypes[extension], Convert.ToBase64String(buffer), fileName);
        }

        public static void Export(string exportType, string exportName, IJSRuntime jSRuntime, DataTable dataTable)
        {
            if (dataTable.Rows.Count < 1) jSRuntime.InvokeVoidAsync("appToast.show", "warning", "There is no data to export for the current selection.", "Nothing to export");
            else
            {
                try
                {
                    switch (exportType)
                    {
                        case "Excel":
                            ExportToExcel(exportName, jSRuntime, dataTable);
                            break;
                        case "Csv":
                            ExportToCsv(exportName, jSRuntime, dataTable);
                            break;
                        case "Pdf":
                            ExportToPdf(exportName, jSRuntime, dataTable);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Export failed: {ex}");
                    jSRuntime.InvokeVoidAsync("appToast.show", "error", "The export could not be generated. Please try again.", "Export failed");
                }
            }
        }
        private static void ExportToCsv(string exportName, IJSRuntime jSRuntime, DataTable datatTable)
        {
            StringBuilder sb = new StringBuilder();
            IEnumerable<string> columnNames = datatTable.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in datatTable.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field =>
                  string.Concat("\"", field.ToString().Replace("\"", "\"\""), "\""));
                sb.AppendLine(string.Join(",", fields));
            }
            byte[] byteArray = ASCIIEncoding.ASCII.GetBytes(sb.ToString());
            DownloadFile(byteArray, "csv", exportName + ".csv", jSRuntime);
        }
        private static void ExportToExcel(string exportName, IJSRuntime jSRuntime, DataTable datatTable)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(exportName);
                for (int i = 0; i < datatTable.Columns.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = datatTable.Columns[i];
                }

                for (int i = 0; i < datatTable.Rows.Count; i++)
                {
                    for (int j = 0; j < datatTable.Columns.Count; j++)
                    {
                        {
                            worksheet.Cell(i + 2, j + 1).Value = datatTable.Rows[i][datatTable.Columns[j]];
                        }
                    }
                }
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var byteArray = stream.ToArray();
                DownloadFile(byteArray, "xlsx", exportName + ".xlsx", jSRuntime);
            }
        }
        private static void ExportToPdf(string exportName, IJSRuntime jSRuntime, DataTable dataTable)
        {
            // creating document object  
            MemoryStream ms = new MemoryStream();
            Document doc = new Document(PageSize.A4, 5f, 5f, 10f, 10f);
            PdfWriter writer = PdfWriter.GetInstance(doc, ms);
            doc.Open();

            Font font1 = FontFactory.GetFont(FontFactory.COURIER_BOLD, 10);
            Font font2 = FontFactory.GetFont(FontFactory.COURIER, 8);

            //Adding  PdfPTable  
            PdfPTable table = new PdfPTable(dataTable.Columns.Count)
            {
                WidthPercentage = 100
            };

            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                table.AddCell(new Phrase(dataTable.Columns[i].ToString(), font1));
            }

            table.HeaderRows = 1;

            foreach (DataRow data in dataTable.Rows)
            {
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    table.AddCell(new Phrase(data[dataTable.Columns[i]].ToString(), font2));
                }
            }

            doc.Add(table);
            doc.Close();

            byte[] result = ms.ToArray();
            DownloadFile(result, "pdf", exportName + ".pdf", jSRuntime);
        }

        public static byte[] ConvertDataTableToPdf(DataTable dataTable, int totalPresent = 0)
        {
            // creating document object  
            MemoryStream ms = new MemoryStream();
            Document doc = new Document(PageSize.A4, 5f, 5f, 10f, 10f);
            PdfWriter writer = PdfWriter.GetInstance(doc, ms);
            doc.Open();

            Font font1 = FontFactory.GetFont(FontFactory.COURIER_BOLD, 10);
            Font font2 = FontFactory.GetFont(FontFactory.COURIER, 8);

            //Adding  PdfPTable  
            PdfPTable table = new PdfPTable(dataTable.Columns.Count)
            {
                WidthPercentage = 100
            };

            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                table.AddCell(new Phrase(dataTable.Columns[i].ToString(), font1));
            }

            table.HeaderRows = 1;

            foreach (DataRow data in dataTable.Rows)
            {
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    table.AddCell(new Phrase(data[dataTable.Columns[i]].ToString(), font2));
                }
            }
            doc.Add(table);
            doc.Close();

            return ms.ToArray();
        }
        public static byte[] ConvertDataTableToExcel(string exportName, DataTable datatTable)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(exportName);
                for (int i = 0; i < datatTable.Columns.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = datatTable.Columns[i];
                }

                for (int i = 0; i < datatTable.Rows.Count; i++)
                {
                    for (int j = 0; j < datatTable.Columns.Count; j++)
                    {
                        {
                            worksheet.Cell(i + 2, j + 1).Value = datatTable.Rows[i][datatTable.Columns[j]];
                        }
                    }
                }
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var byteArray = stream.ToArray();
                return byteArray;
            }
        }
    }
}
