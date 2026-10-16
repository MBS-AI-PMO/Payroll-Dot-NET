using Microsoft.EntityFrameworkCore;

namespace PayslipMgtSystem.Services.Payroll
{
    public class GenerateITCService
    {
        private readonly AppDbContext _context;
        public GenerateITCService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<TaxCertificateRequest>> GetAllRecord()
        {
            var result = await _context.ITCRequests.ToListAsync();
            return result;
        }
        public async Task UpdateRecord(TaxCertificateRequest taxCertificateRequest)
        {
            _context.Update(taxCertificateRequest);
            await _context.SaveChangesAsync();
        }
    }
}
