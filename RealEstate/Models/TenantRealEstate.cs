using System;

namespace RealEstateInstallmentsManager.Models;

public class TenantRealEstate
{
    public long Id { get; set; }
    public long CloudId { get; set; }
    public string SyncAction { get; set; } = "";
    public string Name { get; set; } = "";
    public string IdentityNumber { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    
   
    // Receipt info for tenant page
    public long ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public DateTime ReceiptDate { get; set; }
    public double ReceiptAmount { get; set; }

// Contract info for tenant page
    public long ContractId { get; set; }
    public string ContractNumber { get; set; } = "";
    public DateTime ContractStartDate { get; set; }
    public DateTime ContractEndDate { get; set; }
    public double RentAmount { get; set; }
    public string ContractState { get; set; } = "";
    
}
