using System.Globalization;
using PayslipMgtSystem.Models;
using PayslipMgtSystem.ViewModel;

namespace PayslipMgtSystem.Services.Pdf.Templates
{
    /// <summary>HTML for the official monthly salary slip (the layout emailed to employees).</summary>
    public static class PayslipTemplate
    {
        public static string FileName(UploadFileVM file) => $"Payslip_{file.Year}_{file.Month:D2}_{file.EmployeeName}_{file.Cnic}.pdf";

        public static string Render(UploadFileVM file)
        {
            var total =
                (file.BasicPayafterDeduction ?? 0) +
                (file.Allowances ?? 0) +
                (file.Arrears ?? 0) +
                (file.TravellingAllowance ?? 0) +
                (file.MedicalAllowance ?? 0) +
                (file.EquipmentAllowance ?? 0) +
                (file.CommunicationAllowance ?? 0) +
                (file.AdvancesLoan ?? 0);
            var totalDeduction = file.EOBI + file.WHITDeduction + file.AdvancesLoanDeduction;
            var totalNetPay = file.GrossSalary - totalDeduction;
            return $@"
                                <!DOCTYPE html>
                                <html>
                                <head>
                                    <style>
                                        body {{ font-family: Arial, sans-serif; }}
                                        .justify-content-between {{display: flex;justify-content: space-between;}}
                                        .table {{ width: 100%; border-collapse: collapse; }}
                                        .table th, .table {{ border: 1px solid black; padding: 8px; text-align: left; }}
                                        .table td{{padding:8px;}}
                                        .table th, .table .td1 {{ border: 1px solid black; padding: 8px; text-align: left; }}
                                        .table td{{border-right: 1px solid black;border-left: 1px solid black;}}
                                        //.table th {{ background-color: #f2f2f2; }}
                                        .header {{ text-align: center; margin-top: 20px; }}
                                        .header p {{ margin: 0; }}
                                        .header {{display: flex;justify-content: space-between;
                                         align-items: center; /* Optional: Centers items vertically */
                                         padding: 10px;/*background-color: #f0f0f0;*/ /* Optional: Background color */}}
                                        .header {{display: flex;justify-content: space-between;
                                         align-items: center;padding: 10px;/*background-color: #f0f0f0;*/}}
                                        .header > div {{display: flex;justify-content: space-between;}}
                                        .float-left{{text-align:left;}}
                                        .float-right{{text-align:right;}}
                                        .h1{{display:inline-block; justify-content:flex;}}
                                        .h2{{align-items:end; float:inline-end;}}
                                        .td1{{text-align:right;}}
                                        .footer{{margin-top:200px;}}
                                    </style>
                                </head>
                                <body style='border-left:5px solid orange;'>
                                    <div class='header' style='display: flex; justify-content: space-between; padding: 10px;'>
                                        <div>
                                    <img src='cid:time-and-stamp' alt='Logo' style='width: 100px;' />
                                        </div>
                                        <div style='text-align: right; font-size:12px;'>
                                            <p style='margin-right:3px;'>Solochoicez (Pvt) Ltd. Office # 9,2nd Floor,VIP</p>
                                            <p style='margin-right:43px;'> Plaza, I-8 Markaz,Islamabad, Pakistan.</p>
                                            <p>Tel:051 8446629 Email: info@solochoicez.com</p>
                                            <p style='margin-right:104px;'>Web:www.solochoicez.com</p>
                                        </div>
                                    </div>

                                    <h2 style='text-align: center;color:#0f2a65;'>Salary Slip</h2>
                                    <p style='text-align: center; color:#0f2a65;'>For the Month {@CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(file.Month)}- {file.Year}</p>
                        <hr/>        <div class='justify-content-between'>                                    
                                         <div class='d2'>
                                             <strong>Name:</strong> {file.EmployeeName}
                                         </div>
                                     </div>

                        <hr/>
                                    <p>Designation : {file.ServiceCategory}</p>
                                    <p>CNIC: {file.Cnic}</p>
                                    <p>Offered Salary: {file.OfferedSalary}</p>
                                    <table class='table'>
                                        <tr style='background-color:#0f2a65;color:white;'><th>ADDITIONS</th><th>Amount (Rs.)</th><th>DEDUCTIONS</th><th>Amount (Rs.)</th></tr>
                                        <tr><td>Basic Pay</td><td style='text-align:right'>{(file.BasicPayafterDeduction == 0 ? "-" : file.BasicPayafterDeduction?.ToString("N0"))}</td><td>Advances Deduction</td><td style='text-align:right'>{(file.AdvancesLoanDeduction == 0 ? "-" : file.AdvancesLoanDeduction?.ToString("N0"))}</td></tr>
                                        <tr><td>Arrears</td><td style='text-align:right'>{(file.Arrears == 0 ? "-" : file.Arrears?.ToString("N0"))}</td><td>Other Deductions</td><td style='text-align:right'>{(file.OtherDeduction == 0 ? "-" : file.OtherDeduction?.ToString("N0"))}</td></tr>

                                        <tr><td>Allowances</td><td style='text-align:right'>{(file.Allowances == 0 ? "-" : file.Allowances?.ToString("N0"))}</td><td>Income Tax</td><td style='text-align:right'>{(file.WHITDeduction == 0 ? "-" : file.WHITDeduction?.ToString("N0"))}</td></tr>
                                        <tr><td>Travelling Allowance</td><td style='text-align:right'>-</td><td>EOBI</td><td style='text-align:right'>{(file.EOBI == 0 ? "-" : file.EOBI?.ToString("N0"))}</td></tr>
                                        <tr><td>Medical Allowance</td><td style='text-align:right'>{(file.MedicalAllowance == 0 ? "-" : file.MedicalAllowance?.ToString("N0"))}</td><td></td><td></td></tr>
                                        <tr><td>Equipment Allowance</td><td style='text-align:right'>-</td><td></td><td style='text-align:right'></td></tr>
                                        <tr><td>Communication Allowance</td><td style='text-align:right'>-</td><td></td><td style='text-align:right'></td></tr>
                                        <tr><td>Advances</td><td style='text-align:right'>{(file.AdvancesLoan == 0 ? "-" : file.AdvancesLoan?.ToString("N0"))}</td><td></td><td style='text-align:right'></td></tr>
                                        <tr><td class='td1'>Total Additions</td><td class='td1' style='text-align:right;'>{(total.ToString("N0"))}</td><td class='td1'>Total Deductions</td><td class='td1' style='text-align:right'>{(totalDeduction == 0 ? "-" : totalDeduction?.ToString("N0"))}</td></tr>
                                        <tr><td colspan='3' style='text-align:right;'>Net Pay</td><td style='text-align:right'>{(file.NetAmount == 0 ? "-" : file.NetAmount?.ToString("N0"))}</td></tr>
                                    </table>

    <p style='font-size:11px; color:red; margin-top:150px'>Note: Please Double Check your CNIC as this will be used to deposit Income tax, if your CNIC is not correct please communicate within 10 days otherwise Solochoicez Will not be able to reverse any deposit on wrong CNIC</p>
           <hr/>

                  <p style='font-size:10px; text-align:center;'> *** This is a computer generated Payslip, there is no need for a Sign/Stamp. ***</p>
    </body>
                    </html>";
        }
    }
}
