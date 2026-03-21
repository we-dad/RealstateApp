namespace RealEstateApp.Models;

public class UnitYearStatRow
{
    public long UnitId { get; set; }
    public string UnitName { get; set; } = "";
    public int ContractsStartedThisYear { get; set; }
    public decimal ReceiptsTotalThisYear { get; set; }
}