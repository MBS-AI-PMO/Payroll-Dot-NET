using PayslipMgtSystem.Models;
using PayslipMgtSystem.ViewModel;
using System;
using System.Collections.Generic;

namespace PayslipMgtSystem.ViewModel
{
    public class PaginationVM
    {
        public List<UploadFileVM> uploadFileVMs { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int currentPage { get; set; } = 1;

        public DateTime StartingDate { get; set; }
        public DateTime EndingDate { get; set; }
        public string Year { get; set; }
        public string Month { get; set; }

    }
}
