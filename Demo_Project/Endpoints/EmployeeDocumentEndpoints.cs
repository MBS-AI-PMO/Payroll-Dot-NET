using System.Security.Claims;

namespace PayslipMgtSystem.Endpoints
{
    public static class EmployeeDocumentEndpoints
    {
        /// <summary>
        /// PDFs for the employee portal, inline (view/print) or as a download with ?download=true:
        ///   GET /payslips/{id}/pdf                 – admins can open any payslip, employees only their own
        ///   GET /tax-certificates/{startYear}/pdf  – the signed-in employee's ITC for July startYear – June startYear+1
        /// </summary>
        public static IEndpointRouteBuilder MapEmployeeDocumentEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/payslips/{id:int}/pdf", GetPayslipPdf).RequireAuthorization();
            endpoints.MapGet("/tax-certificates/{startYear:int}/pdf", GetTaxCertificatePdf).RequireAuthorization();
            return endpoints;
        }

        private static async Task<IResult> GetPayslipPdf(int id, bool? download, ClaimsPrincipal user,
            EmployeePayslipService payslips, ILogger<EmployeePayslipService> logger)
        {
            UploadFile? record;
            if (user.IsInRole(AppRoles.Admin))
            {
                record = await payslips.GetAnyPayslipAsync(id);
            }
            else
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                var cnic = userId == null ? null : await payslips.GetCnicForUserAsync(userId);
                record = string.IsNullOrWhiteSpace(cnic) ? null : await payslips.GetPayslipAsync(cnic, id);
            }

            if (record == null)
                return Results.NotFound();

            try
            {
                var bytes = payslips.RenderPdf(record);
                return download == true
                    ? Results.File(bytes, "application/pdf", EmployeePayslipService.PdfFileName(record))
                    : Results.File(bytes, "application/pdf");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate payslip PDF {Id}", id);
                return Results.Problem("The payslip could not be generated.", statusCode: 500);
            }
        }

        private static async Task<IResult> GetTaxCertificatePdf(int startYear, bool? download, ClaimsPrincipal user,
            EmployeeTaxCertificateService certificates, ILogger<EmployeeTaxCertificateService> logger)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var cnic = userId == null ? null : await certificates.GetCnicForUserAsync(userId);
            var certificate = string.IsNullOrWhiteSpace(cnic) ? null : await certificates.GetCertificateAsync(cnic, startYear);
            if (certificate == null)
                return Results.NotFound();

            try
            {
                var bytes = certificates.RenderPdf(certificate, startYear);
                return download == true
                    ? Results.File(bytes, "application/pdf", EmployeeTaxCertificateService.PdfFileName(startYear))
                    : Results.File(bytes, "application/pdf");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate tax certificate PDF {StartYear}", startYear);
                return Results.Problem("The tax certificate could not be generated.", statusCode: 500);
            }
        }
    }
}
