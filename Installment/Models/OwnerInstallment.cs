namespace RealEstateInstallmentsManager.Models;

public class OwnerInstallment
{
    public long Id { get; set; }
    public long CloudId { get; set; }
    public string SyncAction { get; set; } = "";
    public string Name { get; set; } = "";
    public string IdentityNumber { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";

    // Computed for display only (not a stored column) - filled in by
    // OwnerInstallmentService.GetAll() with the same "active contract"/"capital
    // deployed" definition DashboardServiceInstallment already uses
    // (ContractState = 'جاري', SUM of the linked product's price), just scoped
    // to this owner's own products instead of the whole business.
    public int ActiveContractsCount { get; set; }
    public double CapitalInContracts { get; set; }
}
