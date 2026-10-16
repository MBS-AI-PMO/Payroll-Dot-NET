using Microsoft.AspNetCore.HttpOverrides;
using OfficeOpenXml;
using PayslipMgtSystem.Endpoints;
using PayslipMgtSystem.Extensions;
using Syncfusion.Blazor;

var builder = WebApplication.CreateBuilder(args);

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(builder.Configuration["Syncfusion:LicenseKey"]);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options => options.DetailedErrors = builder.Environment.IsDevelopment());
builder.Services.AddSyncfusionBlazor();
builder.Services.AddPortalIdentity(builder.Configuration);
builder.Services.AddPayslipPortal(builder.Configuration);

// Behind a reverse proxy (Coolify/Traefik) that terminates HTTPS: trust its X-Forwarded-* headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UsePrivateFileGuard();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapEmployeeDocumentEndpoints();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
