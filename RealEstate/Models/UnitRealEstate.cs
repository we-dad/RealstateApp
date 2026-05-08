using Avalonia.Media;

namespace RealEstateInstallmentsManager.Models;

public class UnitRealEstate
{
    public long Id { get; set; }
    public long CloudId { get; set; }
    public long OwnerCloudId { get; set; }
    public string SyncAction { get; set; } = "";
    public long OwnerId { get; set; }
    public string OwnerIdentityNumber { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string OwnerPhone { get; set; } = "";
    public string OwnerAddress { get; set; } = "";
    public string UnitName { get; set; } = "";
    public string City { get; set; } = "";
    public string District { get; set; } = "";
    public string UnitState { get; set; } = "شاغرة";
    public string UnitType { get; set; } = "سكني"; // سكني / تجاري
    public int UnitsCount { get; set; } = 1;
    public int UnitNum { get; set; } = 1;

    //for state box color
    public IBrush StateColor =>
        UnitState == "مؤجرة" ? Brushes.Red : Brushes.Green;
}
