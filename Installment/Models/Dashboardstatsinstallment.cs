using System;
using System.Collections.Generic;

namespace RealEstateInstallmentsManager.Models;

// Aggregated numbers for the installment dashboard.
// Money fields are double to match the rest of the codebase (REAL columns);
// see the note in the pattern file about migrating these to decimal later.
public class DashboardStatsInstallment
{
    // Top summary cards
    public double TotalCollected { get; set; }      // SUM(Receipts.Amount)
    public double TotalExpenses { get; set; }        // SUM(Expenses.ExpensesAmount)
    public double NetIncome => TotalCollected - TotalExpenses;
    public double OutstandingBalance { get; set; }   // SUM(active contracts CurrentTotalAmount)

    // Portfolio health
    public int ActiveContracts { get; set; }         // ContractState = جاري
    public int FinishedContracts { get; set; }       // ContractState = منتهي
    public int LateContracts { get; set; }           // contracts behind schedule
    public double CapitalDeployed { get; set; }      // product value currently out on active contracts

    // This-month collection vs target
    public double CollectedThisMonth { get; set; }
    public double TargetThisMonth { get; set; }      // SUM(MonthlyInstallment) of active contracts
    public double CollectionProgressPercent =>
        TargetThisMonth <= 0 ? 0 : Math.Round(CollectedThisMonth / TargetThisMonth * 100, 1);

    // Charts / lists
    public List<MonthlyFlow> MonthlyFlows { get; set; } = new();   // last 6 months
    public List<TopProduct> TopProducts { get; set; } = new();     // best sellers
    public List<LatePayer> LatePayers { get; set; } = new();       // behind schedule
    public List<UpcomingInstallment> Upcoming { get; set; } = new(); // due in next 7 days
    
    public int OwnersCount { get; set; }
    public int CustomersCount { get; set; }
    public int ProductsCount { get; set; }
    public int ContractsCount { get; set; }
    public int ReceiptsCount { get; set; }
    public int ExpensesCount { get; set; }
}

public class MonthlyFlow
{
    public string Month { get; set; } = "";   // "2026-07"
    public double Collected { get; set; }
    public double Spent { get; set; }
    
    // add to MonthlyFlow in DashboardStatsInstallment.cs
    public double CollectedBarHeight { get; set; }
    public double SpentBarHeight { get; set; }
}

public class TopProduct
{
    public string ProductName { get; set; } = "";
    public int ContractCount { get; set; }
}

public class LatePayer
{
    public long ContractId { get; set; }
    public string CustomerName { get; set; } = "";
    public string ContractNumber { get; set; } = "";
    public double ExpectedPaid { get; set; }
    public double ActualPaid { get; set; }
    public double ShortfallAmount => Math.Max(ExpectedPaid - ActualPaid, 0);
    public int PaymentsBehind { get; set; }
    public string BehindText => $"متأخر {PaymentsBehind} قسط";
    
    public double MonthlyInstallment { get; set; }
    public int PaidCount { get; set; }
    public int DueCount { get; set; }
    public System.DateTime? LastPaymentDate { get; set; }

    public string LastPaymentText =>
        LastPaymentDate.HasValue ? LastPaymentDate.Value.ToString("yyyy-MM-dd") : "لا يوجد";

    public string ProgressText => $"دفع {PaidCount} من {DueCount}";
    
    public long CustomerId { get; set; }
}

public class UpcomingInstallment
{
    public long ContractId { get; set; }
    public string CustomerName { get; set; } = "";
    public string ContractNumber { get; set; } = "";
    public DateTime DueDate { get; set; }
    public double Amount { get; set; }
    public int DaysUntilDue { get; set; }
    public string DueText => $"بعد {DaysUntilDue} يوم — {Amount:N0}";
    
    public long CustomerId { get; set; }
}