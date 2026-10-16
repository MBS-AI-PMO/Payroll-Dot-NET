namespace PayslipMgtSystem.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        // wwwroot folders holding generated payslips and uploaded payroll sheets
        private static readonly string[] PrivateFolders = { "/" + PdfArchive.FolderName, "/UploadFiles" };

        /// <summary>Stops employee data stored under wwwroot from being served as static files.</summary>
        public static IApplicationBuilder UsePrivateFileGuard(this IApplicationBuilder app) =>
            app.Use(async (context, next) =>
            {
                if (PrivateFolders.Any(folder => context.Request.Path.StartsWithSegments(folder, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                await next();
            });
    }
}
