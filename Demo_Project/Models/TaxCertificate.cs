using System.ComponentModel.DataAnnotations;

namespace PayslipMgtSystem.Models
{
    public class TaxCertificateRequest
    {
        [Key]
        public int Id { get; set; }
        public int Year { get; set; }   
        public bool isRequestGenerated {  get; set; }
        public bool isPdfGenerated {  get; set; }
    }
}
