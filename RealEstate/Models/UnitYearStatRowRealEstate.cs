namespace RealEstateInstallmentsManager.Models;

// One building's performance for the selected year.
// (Reuses the existing name so the DataGrid binding keeps working;
//  "unit" here now means the parent building.)
public class UnitYearStatRowRealEstate
{
    public long UnitId { get; set; }             // parent Id
    public string UnitName { get; set; } = "";   // building name
    public int UnitsCount { get; set; }          // how many units it holds
    public int RentedCount { get; set; }         // currently rented
    public int VacantCount { get; set; }         // currently vacant
    public int ContractsStartedThisYear { get; set; }
    public decimal ReceiptsTotalThisYear { get; set; }
    public decimal ExpensesTotalThisYear { get; set; }

    public decimal NetThisYear => ReceiptsTotalThisYear - ExpensesTotalThisYear;

    // "3 / 8" rented-of-total, handy in the table
    public string Occupancy => $"{RentedCount} / {UnitsCount}";
}

// A rent payment that is coming due (contract active, next month boundary).
public class UpcomingPaymentRowRealEstate
{
    public long ContractId { get; set; }
    public long TenantId { get; set; }
    public string ContractNumber { get; set; } = "";
    public string TenantName { get; set; } = "";
    public string UnitName { get; set; } = "";
    public string BuildingName { get; set; } = "";
    public System.DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public int DaysUntilDue { get; set; }
}

// A tenant whose contract is active but who is behind on payments.
public class LateTenantRowRealEstate
{
    public long ContractId { get; set; }
    public long TenantId { get; set; }
    public string ContractNumber { get; set; } = "";
    public string TenantName { get; set; } = "";
    public string TenantPhone { get; set; } = "";
    public string UnitName { get; set; } = "";
    public string BuildingName { get; set; } = "";
    public string PayMethod { get; set; } = "";
    public decimal RentPerPeriod { get; set; }
    public decimal ExpectedToDate { get; set; }
    public decimal PaidToDate { get; set; }
    public decimal Outstanding => ExpectedToDate - PaidToDate;
    public int PeriodsBehind { get; set; }
    public System.DateTime? LastPaymentDate { get; set; }

    public string LastPaymentText =>
        LastPaymentDate.HasValue ? LastPaymentDate.Value.ToString("yyyy-MM-dd") : "لا يوجد";

    // "متأخر شهرين" / "متأخر دفعتين"
    public string BehindText => PeriodsBehind <= 0 ? "" : $"متأخر {PeriodsBehind} دفعة";
}