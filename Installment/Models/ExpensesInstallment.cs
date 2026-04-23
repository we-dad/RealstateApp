using System;

namespace RealEstateInstallmentsManager.Models;

public class ExpensesInstallment
{
    public long Id { get; set; }
    public string ExpensesNumber { get; set; } = "";
    public DateTime ExpensesDate { get; set; }
    public string ExpensesService { get; set; } = "أخرى";
    public double ExpensesAmount { get; set; }
    public string ExpensesNote { get; set; } = "";

    //Product
    public long ProductId { get; set; }
    public string ProductName { get; set; } = "";

}
