using System;

namespace RealEstateInstallmentsManager.Models;

// One row of the read-only contract calculation check.
public class ContractCheckRowInstallment
{
    public long Id { get; set; }
    public string ContractNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public double DownPayment { get; set; }
    public double MainTotalAmount { get; set; }
    public double ExpectedTotal { get; set; }
    public string Status { get; set; } = "";

    public double Difference => Math.Round(MainTotalAmount - ExpectedTotal, 2);
}
