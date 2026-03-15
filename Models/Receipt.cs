using System;

namespace RealEstateApp.Models;

public class Receipt
{
    public long Id { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public DateTime ReceiptDate { get; set; }
    public string PaymentMethod { get; set; } = "تحويل";
    public double Amount { get; set; }

    //Contract
    public long ContractId { get; set; }
    public string ContractNumber { get; set; } = "";

    //Tenant
    public string TenantName { get; set; } = "";


}
