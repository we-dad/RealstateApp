namespace RealEstateInstallmentsManager.Models;

// One line of the customer picker (identity number and name).
public class CustomerPickRowInstallment
{
    public long Id { get; set; }
    public string IdentityNumber { get; set; } = "";
    public string Name { get; set; } = "";

    public string Display => $"{IdentityNumber} — {Name}";
}
