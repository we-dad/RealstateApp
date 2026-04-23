using System;

namespace RealEstateInstallmentsManager.Models;

public class ExpensesRealEstate
{
    public long Id { get; set; }
    public string ExpensesNumber { get; set; } = "";
    public DateTime ExpensesDate { get; set; }
    public string ExpensesService { get; set; } = "أخرى";
    public double ExpensesAmount { get; set; }
    public string ExpensesNote { get; set; } = "";

    //Contract
    public long UnitId { get; set; }
    public string UnitName { get; set; } = "";

}
