using Microsoft.AspNetCore.Components;
using PayslipMgtSystem.Models;

namespace PayslipMgtSystem.Pages
{
    public partial class GenerateITC
    {
        public List<TaxCertificateRequest> Items = new();
        private string message;
        [Inject]
        protected AppDbContext _context { get; set; }
        protected override async Task OnInitializedAsync()
        {
            Items = await generateItcService.GetAllRecord();
        }
        public async Task GenerateRequestAsync(TaxCertificateRequest taxCertificateRequest)
        {
            taxCertificateRequest.isRequestGenerated = true;
            await generateItcService.UpdateRecord(taxCertificateRequest);
            message = $"Request to generate Income Tax Certificates for the year '{taxCertificateRequest.Year}' has been generated";
            StateHasChanged();

            await Task.Delay(3000);
            message = string.Empty;
            StateHasChanged();
        }
    }
}