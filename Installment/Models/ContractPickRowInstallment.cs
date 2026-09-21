namespace RealEstateInstallmentsManager.Models;

// One line of the contract picker (number, customer, product) used by the
// receipt screen search box.
public class ContractPickRowInstallment
{
    public string ContractNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string ContractState { get; set; } = "";

    public string Display =>
        $"{ContractNumber} — {CustomerName} — {ProductName}" +
        (ContractState == "منتهي" ? " (منتهي)" : "");
}
