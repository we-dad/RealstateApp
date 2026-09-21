namespace RealEstateInstallmentsManager.Models;

// One line of the tenant picker (identity number and name).
public class TenantPickRowRealEstate
{
    public long Id { get; set; }
    public string IdentityNumber { get; set; } = "";
    public string Name { get; set; } = "";

    public string Display => $"{IdentityNumber} — {Name}";
}
