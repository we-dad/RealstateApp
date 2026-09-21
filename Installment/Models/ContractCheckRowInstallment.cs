using System;

namespace RealEstateInstallmentsManager.Models;

// One row of the read-only contract calculation check.
public class ContractCheckRowInstallment
{
    public long Id { get; set; }
    public string ContractNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public double ProductPrice { get; set; }
    public double DownPayment { get; set; }
    public double ManagementFee { get; set; }
    public double InterestPercent { get; set; }
    public double ContractPeriod { get; set; }
    public double MainTotalAmount { get; set; }
    public double ExpectedTotal { get; set; }
    public string Status { get; set; } = "";

    // The product price this saved total corresponds to, without using today's
    // product price: R = (total - fee) / (1 + interest * period / 1200), where R is
    // the price the calculation was run on. If the down payment was deducted the
    // original price is R + down payment, otherwise it is R.
    public double ImpliedPriceIfDeducted => Math.Round(Reduced + DownPayment, 2);
    public double ImpliedPriceIfNotDeducted => Math.Round(Reduced, 2);

    private double Reduced =>
        (MainTotalAmount - ManagementFee) / (1 + InterestPercent * ContractPeriod / 1200.0);

    public double Difference => Math.Round(MainTotalAmount - ExpectedTotal, 2);
}
