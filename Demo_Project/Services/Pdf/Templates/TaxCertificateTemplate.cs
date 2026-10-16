namespace PayslipMgtSystem.Services.Pdf.Templates
{
    /// <summary>HTML for the yearly tax certificate generated in the background from the aggregated payroll rows.</summary>
    public static class TaxCertificateTemplate
    {
        public static string Render(string CNIC, string Name, string ServiceCategory, int GrossSalary, int AdvancePay, int LeaveDeduction, int WHIT, int EOBI, int Deductions, int NetPay, int Year)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                         h2{{text-decoration:underline; margin-top:100px;}}
                        .header {{ text-align: center; margin-top: 20px; }}
                        .header p {{ margin: 0; }}
                        .header {{display: flex;justify-content: space-between;}}
                        .table {{ width: 100%; border-collapse: collapse; }}
                        .table th, .table td {{ border: 0px solid black; padding: 8px; text-align: left; }}
                        .right-align {{ text-align: right; }}
                        .left-align {{ text-align: left; }}
                        .no-border {{ border-style: none; }}
                        .underline {{ text-decoration: underline; }}
                        span{{margin-left:100px;}}
                    </style>
                </head>
                <body>
                <div class='header' style='display: flex; justify-content: space-between; padding: 10px;'>
                                <div>
                            <img src='cid:time-and-stamp' alt='Logo' style='width: 100px;' />
                                </div>
                                <div style='text-align: right;'>
                                    <p>Solochoicez (Pvt) Ltd.</p>
                                    <p>Office # 9,2nd Floor,<br/>  VIP Plaza, I-8 Markaz,<br/>  Islamabad, Pakistan.</p>
                                    <p>Tel: +92 51 8446629</p>
                                    <p>Email: info@solochoicez.com</p>
                                    <p>Web: www.solochoicez.com</p>
                                </div>
                            </div>
                <div style='padding:20px;'>
                   <div class='header'>
                        <h2>INCOME TAX CERTIFICATE</h2>
                   </div>

                <div>
                 <p>It is to certify that an amount of Rs. <strong>{(GrossSalary == 0 ? "-" : GrossSalary.ToString("N0"))}</strong> has been paid to Mr. <strong>{Name}</strong> “{ServiceCategory}” CNIC No. <strong>{CNIC}</strong> as salary during the financial Year <strong>{Year}-{Year + 1}</strong>.</p>
                 <p style='text-align:left'>Break up is given below:</p>
             
                 <table class='table'>
                <tr>
                    <th style='text-align:center; border: 1px solid black;' colspan='2'>Allowances</th>
                    <th style='text-align:center; border: 1px solid black;' colspan='2'>Deductions</th>
                </tr>
                <tr>
                    <td class='left-align' style='font-weight:bold'>Description</td>
                    <td style='text-align:right; font-weight:bold'>Amount</td>
                    <td class='left-align' style='font-weight:bold'>Description</td>
                    <td style='text-align:right; font-weight:bold'>Amount</td>
 
                </tr>
                <tr>
                    <td class='left-align'>Gross Pay</td>
                    <td style='text-align:right;'>{(GrossSalary == 0 ? "-" : GrossSalary.ToString("N0"))}</td>
                    <td class='left-align'>Advance Pay</td>
                    <td style='text-align:right;'>{(AdvancePay == 0 ? "-" : AdvancePay.ToString("N0"))}</td>
                </tr>
                <tr>
                    <td class='left-align no-border'></td>
                    <td style='text-align:right;'></td>
                    <td class='left-align'>Leave Without Pay</td>
                    <td style='text-align:right;'>{(AdvancePay == 0 ? "-" : LeaveDeduction.ToString("N0"))}</td>
                </tr>
                <tr>
                    <td class='left-align no-border'></td>
                    <td style='text-align:right;'></td>
                    <td class='left-align'>Income Tax</td>
                    <td style='text-align:right;'>{(WHIT == 0 ? "-" : WHIT.ToString("N0"))}</td>
                </tr>
                <tr>
                    <td style='text-align:right;padding-bottom:50px;border:none;'></td>
                    <td style='text-align:right;padding-bottom:50px;'></td>
                    <td style='text-align:left;padding-bottom:50px;'>EOBI</td>
                    <td style='text-align:right; padding-bottom:50px;'>{(EOBI == 0 ? "-" : EOBI.ToString("N0"))}</td>
                </tr>
                <tr>
                    <td style='font-weight:bold; text-align:right; border: 1px solid black; border-right: 0px solid black;'>Total:</td>
                    <td style='font-weight:bold; text-align:right; border: 1px solid black; border-left: 0px solid black;border-right: 0px solid black;'>{(GrossSalary == 0 ? "-" : GrossSalary.ToString("N0"))}</td>
                    <td style='font-weight:bold; text-align:right; border: 1px solid black; border-left: 0px solid black;border-right: 0px solid black;'>Deduction:</td>
                    <td style='font-weight:bold; text-align:right; border: 1px solid black;border-left: 0px solid black;'>{(Deductions == 0 ? "-" : Deductions.ToString("N0"))}</td>
                </tr>
                </table>


    <div style='text-align: right; margin-top:20px;'>
    <strong>
    Net Pay: <span>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{(NetPay == 0 ? "-" : NetPay.ToString("N0"))}</span>
    </strong>
    </div>
          

                <p>This is to certify that an Amount of <strong>Rs. {WHIT.ToString("N0")} </strong> has been deducted as income Tax vide section <strong> No. 149</strong> from salary during the above mentioned Financial Year and deposited to income tax department.</p>
                </div>
            

    <hr style='margin-top:220px'/>
    <p style='font-size:10px; text-align:center;'>*** This is a computer generated Income Tax Certificate, there is no need for a Sign/Stamp. ***</p>
    </div>
                </body>
            </html>";
        }
    }
}
