namespace RealEstateInstallmentsManager.Models;

public class OwnerRealEstate
{
    public long Id { get; set; }
    public long CloudId { get; set; }
    public string SyncAction { get; set; } = "";
    public string Name { get; set; } = "";
    public string IdentityNumber { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
}
