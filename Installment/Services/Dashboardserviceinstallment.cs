using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using RealEstateInstallmentsManager.Models;

namespace RealEstateInstallmentsManager.Services;

public class DashboardServiceInstallment
{
    private readonly DbServiceInstallment _db;

    public DashboardServiceInstallment(DbServiceInstallment db)
    {
        _db = db;
    }

    public DashboardStatsInstallment GetStats()
    {
        var stats = new DashboardStatsInstallment();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        stats.TotalCollected = ScalarDouble(con,
            "SELECT COALESCE(SUM(Amount),0) FROM ReceiptsInstallment WHERE SyncAction <> 'delete';");

        stats.TotalExpenses = ScalarDouble(con,
            "SELECT COALESCE(SUM(ExpensesAmount),0) FROM ExpensesInstallment WHERE SyncAction <> 'delete';");

        stats.OutstandingBalance = ScalarDouble(con,
            "SELECT COALESCE(SUM(CurrentTotalAmount),0) FROM ContractsInstallment WHERE SyncAction <> 'delete' AND ContractState = 'جاري';");

        stats.ActiveContracts = ScalarInt(con,
            "SELECT COUNT(*) FROM ContractsInstallment WHERE SyncAction <> 'delete' AND ContractState = 'جاري';");

        stats.FinishedContracts = ScalarInt(con,
            "SELECT COUNT(*) FROM ContractsInstallment WHERE SyncAction <> 'delete' AND ContractState = 'منتهي';");

        stats.CapitalDeployed = ScalarDouble(con, """
            SELECT COALESCE(SUM(p.ProductMainPrice),0)
            FROM ContractsInstallment c
            JOIN ProductsInstallment p ON p.Id = c.ProductId
            WHERE c.SyncAction <> 'delete' AND c.ContractState = 'جاري';
        """);

        stats.TargetThisMonth = ScalarDouble(con,
            "SELECT COALESCE(SUM(MonthlyInstallment),0) FROM ContractsInstallment WHERE SyncAction <> 'delete' AND ContractState = 'جاري';");

        stats.CollectedThisMonth = ScalarDouble(con, """
            SELECT COALESCE(SUM(Amount),0)
            FROM ReceiptsInstallment
            WHERE SyncAction <> 'delete'
              AND strftime('%Y-%m', ReceiptDate) = strftime('%Y-%m', 'now');
        """);

        stats.MonthlyFlows = GetMonthlyFlows(con);
        stats.TopProducts = GetTopProducts(con);

        var (late, upcoming, lateCount) = GetScheduleAnalysis(con);
        stats.LatePayers = late;
        stats.Upcoming = upcoming;
        stats.LateContracts = lateCount;

        return stats;
    }

    private List<MonthlyFlow> GetMonthlyFlows(SqliteConnection con)
    {
        // Build the last 6 month buckets (oldest first), then fill from DB.
        var months = new List<string>();
        var now = DateTime.Today;
        for (int i = 5; i >= 0; i--)
            months.Add(new DateTime(now.Year, now.Month, 1).AddMonths(-i).ToString("yyyy-MM"));

        var collected = SumByMonth(con,
            "SELECT strftime('%Y-%m', ReceiptDate) m, COALESCE(SUM(Amount),0) v FROM ReceiptsInstallment WHERE SyncAction <> 'delete' GROUP BY m;");
        var spent = SumByMonth(con,
            "SELECT strftime('%Y-%m', ExpensesDate) m, COALESCE(SUM(ExpensesAmount),0) v FROM ExpensesInstallment WHERE SyncAction <> 'delete' GROUP BY m;");

        return months.Select(m => new MonthlyFlow
        {
            Month = m,
            Collected = collected.TryGetValue(m, out var c) ? c : 0,
            Spent = spent.TryGetValue(m, out var s) ? s : 0
        }).ToList();
    }

    private List<TopProduct> GetTopProducts(SqliteConnection con)
    {
        var list = new List<TopProduct>();
        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT p.ProductName, COUNT(*) cnt
            FROM ContractsInstallment c
            JOIN ProductsInstallment p ON p.Id = c.ProductId
            WHERE c.SyncAction <> 'delete'
            GROUP BY p.ProductName
            ORDER BY cnt DESC
            LIMIT 6;
        """;
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new TopProduct
            {
                ProductName = r.IsDBNull(0) ? "" : r.GetString(0),
                ContractCount = r.GetInt32(1)
            });
        return list;
    }

    // Expected-vs-actual model: for each active contract,
    // expected = MonthlyInstallment * whole months elapsed since start (capped at ContractPeriod),
    // actual   = SUM of that contract's receipts.
    private (List<LatePayer>, List<UpcomingInstallment>, int) GetScheduleAnalysis(SqliteConnection con)
    {
        var late = new List<LatePayer>();
        var upcoming = new List<UpcomingInstallment>();
        var today = DateTime.Today;

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT
                c.Id,
                c.ContractNumber,
                c.ContractStartDate,
                c.MonthlyInstallment,
                c.ContractPeriod,
                cus.Name,
                COALESCE((SELECT SUM(r.Amount) FROM ReceiptsInstallment r
                          WHERE r.ContractId = c.Id AND r.SyncAction <> 'delete'), 0) AS paid
            FROM ContractsInstallment c
            JOIN CustomersInstallment cus ON cus.Id = c.CustomerId
            WHERE c.SyncAction <> 'delete' AND c.ContractState = 'جاري';
        """;

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var id = r.GetInt64(0);
            var number = r.IsDBNull(1) ? "" : r.GetString(1);
            var start = r.GetDateTime(2);
            var monthly = r.IsDBNull(3) ? 0 : Convert.ToDouble(r.GetValue(3));
            var period = r.IsDBNull(4) ? 0 : Convert.ToDouble(r.GetValue(4));
            var name = r.IsDBNull(5) ? "" : r.GetString(5);
            var paid = r.IsDBNull(6) ? 0 : Convert.ToDouble(r.GetValue(6));

            if (monthly <= 0) continue;

            // whole months elapsed since start, capped at the contract length
            int monthsElapsed = ((today.Year - start.Year) * 12) + (today.Month - start.Month);
            if (today.Day < start.Day) monthsElapsed--;              // not yet reached this month's due day
            monthsElapsed = Math.Max(0, monthsElapsed);
            if (period > 0) monthsElapsed = (int)Math.Min(monthsElapsed, period);

            double expected = monthly * monthsElapsed;

            if (paid + 0.01 < expected)   // behind (tiny epsilon guards float noise)
            {
                int behind = (int)Math.Ceiling((expected - paid) / monthly);
                late.Add(new LatePayer
                {
                    ContractId = id,
                    CustomerName = name,
                    ContractNumber = number,
                    ExpectedPaid = Math.Round(expected, 2),
                    ActualPaid = Math.Round(paid, 2),
                    PaymentsBehind = behind
                });
            }

            // next due date = start + (payments made + 1) months, roughly
            int paymentsMade = monthly > 0 ? (int)Math.Floor(paid / monthly) : 0;
            if (period > 0 && paymentsMade >= period) continue;      // fully paid schedule
            var nextDue = start.AddMonths(paymentsMade + 1);
            int daysUntil = (nextDue.Date - today).Days;

            if (daysUntil >= 0 && daysUntil <= 7)
            {
                upcoming.Add(new UpcomingInstallment
                {
                    ContractId = id,
                    CustomerName = name,
                    ContractNumber = number,
                    DueDate = nextDue,
                    Amount = Math.Round(monthly, 2),
                    DaysUntilDue = daysUntil
                });
            }
        }

        int lateCount = late.Count;
        late = late.OrderByDescending(x => x.PaymentsBehind).ThenByDescending(x => x.ShortfallAmount).ToList();
        upcoming = upcoming.OrderBy(x => x.DaysUntilDue).ToList();
        return (late, upcoming, lateCount);
    }

    private Dictionary<string, double> SumByMonth(SqliteConnection con, string sql)
    {
        var dict = new Dictionary<string, double>();
        using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            if (r.IsDBNull(0)) continue;
            dict[r.GetString(0)] = r.IsDBNull(1) ? 0 : Convert.ToDouble(r.GetValue(1));
        }
        return dict;
    }

    private double ScalarDouble(SqliteConnection con, string sql)
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        var v = cmd.ExecuteScalar();
        return v is null or DBNull ? 0 : Convert.ToDouble(v);
    }

    private int ScalarInt(SqliteConnection con, string sql)
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        var v = cmd.ExecuteScalar();
        return v is null or DBNull ? 0 : Convert.ToInt32(v);
    }
}