using System;

namespace RealEstateInstallmentsManager.Models;

public class ReceiptRealEstate
{
    public long Id { get; set; }
    public long CloudId { get; set; }
    public string SyncAction { get; set; } = "";
    public long ContractCloudId { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public DateTime ReceiptDate { get; set; }
    public string PaymentMethod { get; set; } = "تحويل";
    public double Amount { get; set; }

    //Contract
    public long ContractId { get; set; }
    public string ContractNumber { get; set; } = "";

    //Tenant
    public string TenantName { get; set; } = "";
    
    public int ReceiptsCount { get; set; }

}
