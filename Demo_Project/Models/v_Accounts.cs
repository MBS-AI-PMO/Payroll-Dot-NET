using System.ComponentModel.DataAnnotations;

namespace PayslipMgtSystem.Models
{
    public class v_Accounts
    {
        public int id { get; set; }
        public string account_num { get; set; }    
        public string name { get; set; }
        public int parent_id {  get; set; } 
        public Int16 type {  get; set; }
        public decimal ending_balance_amt {  get; set; }  
    }
}
