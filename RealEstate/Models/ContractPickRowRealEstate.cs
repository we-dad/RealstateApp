namespace RealEstateInstallmentsManager.Models;

// One line of the contract picker (number, tenant, unit) used by the
// receipt screens search box.
public class ContractPickRowRealEstate
{
    public string ContractNumber { get; set; } = "";
    public string TenantName { get; set; } = "";
    public string UnitName { get; set; } = "";
    public string ContractState { get; set; } = "";

    public string Display =>
        $"{ContractNumber} — {TenantName} — {UnitName}" +
        (ContractState == "منتهي" ? " (منتهي)" : "");
}
