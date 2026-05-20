using System;

namespace RealEstateInstallmentsManager.Models;

public class ReceiptInstallment
{
    public long Id { get; set; }
    public long CloudId { get; set; }
    public string SyncAction { get; set; } = "";
    public long ContractCloudId { get; set; }    
    public string ReceiptNumber { get; set; } = "";
    public DateTime ReceiptDate { get; set; }
    public string PaymentMethod { get; set; } = "تحويل";
    public double Amount { get; set; }
    public double CurrentTotalAmount { get; set; }

    //Contract
    public long ContractId { get; set; }
    public string ContractNumber { get; set; } = "";

    //Customer
    public string CustomerName { get; set; } = "";
    
    public int ReceiptsCount { get; set; }


}
