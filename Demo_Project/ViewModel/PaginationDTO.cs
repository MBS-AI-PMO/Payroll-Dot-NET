using System;

namespace PayslipMgtSystem.ViewModel
{
    public class PaginationDTO
    {
        public int Page { get; set; } = 1;
        public int QuantityPerPage { get; set; } = 20;
        //public string name { get; set; }
        public string Year { get; set; }
        public string Month { get; set; }
        public string StartingMonth { get; set; }
        public string EndingMonth { get; set; }
        public DateTime StartingDate { get; set; }
        public DateTime EndingDate { get; set; }
    }
}
