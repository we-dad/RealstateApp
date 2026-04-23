using Avalonia.Controls;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class DashboardViewRealEstate : UserControl
{
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();

    public DashboardViewRealEstate()
    {
        InitializeComponent();

        try
        {
            _db.Initialize();

            // السنوات (آخر 6 سنوات)
            var nowYear = DateTime.Now.Year;
            YearCombo.ItemsSource = Enumerable.Range(nowYear - 5, 6).Reverse().ToList();
            YearCombo.SelectedItem = nowYear;

            RefreshBtn.Click += async (_, __) =>
            {
                if (YearCombo.SelectedItem is int y)
                    await LoadDashboardAsync(y);
            };

            YearCombo.SelectionChanged += async (_, __) =>
            {
                if (YearCombo.SelectedItem is int y)
                    await LoadDashboardAsync(y);
            };

            // عداداتك الحالية
            OwnersCountText.Text = new OwnerServiceRealEstate(_db).GetAll().Count.ToString();
            UnitsCountText.Text = new UnitServiceRealEstate(_db).GetAll().Count.ToString();
            TenantsCountText.Text = new TenantServiceRealEstate(_db).GetAll().Count.ToString();
            ContractsCountText.Text = new ContractServiceRealEstate(_db).GetAll().Count.ToString();
            ReceiptsCountText.Text = new ReceiptServiceRealEstate(_db).GetAll().Count.ToString();

            // تحميل أولي بعد ظهور الصفحة (أفضل من _=Load... مباشرة)
            this.AttachedToVisualTree += async (_, __) =>
            {
                if (YearCombo.SelectedItem is int y)
                    await LoadDashboardAsync(y);
            };
        }
        catch (Exception ex)
        {
            OwnersCountText.Text = "-";
            UnitsCountText.Text = "-";
            TenantsCountText.Text = "-";
            ContractsCountText.Text = "-";
            ReceiptsCountText.Text = "-";

            TotalReceiptsText.Text = "-";
            TotalExpensesText.Text = "-";
            NetText.Text = "-";
            RentedText.Text = "-";
            VacantText.Text = "-";

            Console.WriteLine(ex.ToString());
        }
    }

    private async Task LoadDashboardAsync(int year)
    {
        try
        {
            await using var con = new SqliteConnection(_db.ConnectionString);
            await con.OpenAsync();

            // KPIs
            var totalReceipts = await ScalarDecimalAsync(con, """
                SELECT IFNULL(SUM(r.Amount), 0)
                FROM Receipts r
                WHERE strftime('%Y', r.ReceiptDate) = @year;
            """, year);

            var totalExpenses = await ScalarDecimalAsync(con, """
                SELECT IFNULL(SUM(e.ExpensesAmount), 0)
                FROM Expenses e
                WHERE strftime('%Y', e.ExpensesDate) = @year;
            """, year);

            var (rented, vacant) = await UnitsCountAsync(con);

            TotalReceiptsText.Text = totalReceipts.ToString("N2", CultureInfo.InvariantCulture);
            TotalExpensesText.Text = totalExpenses.ToString("N2", CultureInfo.InvariantCulture);
            NetText.Text = (totalReceipts - totalExpenses).ToString("N2", CultureInfo.InvariantCulture);
            RentedText.Text = rented.ToString();
            VacantText.Text = vacant.ToString();

            // Monthly (قبض + صرف)
            var receiptsMonthly = await MonthTotalsAsync(con, year, """
                SELECT strftime('%Y-%m', r.ReceiptDate) AS Month, IFNULL(SUM(r.Amount),0) AS Total
                FROM Receipts r
                WHERE strftime('%Y', r.ReceiptDate) = @year
                GROUP BY Month
                ORDER BY Month;
            """);

            var expensesMonthly = await MonthTotalsAsync(con, year, """
                SELECT strftime('%Y-%m', e.ExpensesDate) AS Month, IFNULL(SUM(e.ExpensesAmount),0) AS Total
                FROM Expenses e
                WHERE strftime('%Y', e.ExpensesDate) = @year
                GROUP BY Month
                ORDER BY Month;
            """);

            BuildMonthlyChart(receiptsMonthly, expensesMonthly);
            BuildOccupancyPie(rented, vacant);

            // Units table + Top 5 chart
            var unitsStats = await GetUnitsYearStatsAsync(con, year);
            UnitsStatsGrid.ItemsSource = unitsStats;
            BuildTopUnitsBarChart(unitsStats);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void BuildMonthlyChart(List<(string month, decimal total)> receipts,
                                   List<(string month, decimal total)> expenses)
    {
        var labels = receipts.Select(x => x.month)
            .Union(expenses.Select(x => x.month))
            .OrderBy(x => x)
            .ToArray();

        double[] receiptsValues = labels.Select(m => (double)(receipts.FirstOrDefault(x => x.month == m).total)).ToArray();
        double[] expensesValues = labels.Select(m => (double)(expenses.FirstOrDefault(x => x.month == m).total)).ToArray();

        MonthlyChart.XAxes = new[] { new Axis { Labels = labels } };
        MonthlyChart.YAxes = new[] { new Axis { MinLimit = 0 } };

        MonthlyChart.Series = new ISeries[]
        {
            new LineSeries<double> { Name = "القبض", Values = receiptsValues, GeometrySize = 10 },
            new LineSeries<double> { Name = "الصرف", Values = expensesValues, GeometrySize = 10 }
        };
    }

    private void BuildOccupancyPie(int rented, int vacant)
    {
        OccupancyChart.Series = new ISeries[]
        {
            new PieSeries<double> { Name = "مؤجرة", Values = new[] { (double)rented } },
            new PieSeries<double> { Name = "شاغرة", Values = new[] { (double)vacant } }
        };
    }

    private void BuildTopUnitsBarChart(List<UnitYearStatRowRealEstate> stats)
    {
        var top = stats
            .OrderByDescending(x => x.ReceiptsTotalThisYear)
            .Take(5)
            .ToList();

        if (top.Count == 0)
        {
            TopUnitsChart.Series = Array.Empty<ISeries>();
            TopUnitsChart.XAxes = new[] { new Axis { Labels = Array.Empty<string>() } };
            TopUnitsChart.YAxes = new[] { new Axis { MinLimit = 0 } };
            return;
        }

        var labels = top.Select(x => x.UnitName).ToArray();
        var values = top.Select(x => (double)x.ReceiptsTotalThisYear).ToArray();

        TopUnitsChart.XAxes = new[] { new Axis { Labels = labels } };
        TopUnitsChart.YAxes = new[] { new Axis { MinLimit = 0 } };

        TopUnitsChart.Series = new ISeries[]
        {
            new ColumnSeries<double> { Name = "إجمالي المقبوضات", Values = values }
        };
    }

    private static async Task<decimal> ScalarDecimalAsync(SqliteConnection con, string sql, int year)
    {
        await using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@year", year.ToString());
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToDecimal(result);
    }

    private static async Task<(int rented, int vacant)> UnitsCountAsync(SqliteConnection con)
    {
        await using var cmd = con.CreateCommand();
        cmd.CommandText = """
        SELECT
          SUM(CASE WHEN UnitState = 'مؤجرة' THEN 1 ELSE 0 END) AS RentedCount,
          SUM(CASE WHEN UnitState = 'شاغرة' THEN 1 ELSE 0 END) AS VacantCount
        FROM Units;
    """;

        await using var r = await cmd.ExecuteReaderAsync();
        await r.ReadAsync();

        int rented = r.IsDBNull(0) ? 0 : r.GetInt32(0);
        int vacant = r.IsDBNull(1) ? 0 : r.GetInt32(1);
        return (rented, vacant);
    }
    private static async Task<List<(string month, decimal total)>> MonthTotalsAsync(SqliteConnection con, int year, string sql)
    {
        await using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@year", year.ToString());

        var list = new List<(string month, decimal total)>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add((reader.GetString(0), reader.GetDecimal(1)));
        }
        return list;
    }

    private static async Task<List<UnitYearStatRowRealEstate>> GetUnitsYearStatsAsync(SqliteConnection con, int year)
    {
        await using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT 
              u.Id,
              u.UnitName,
              COUNT(DISTINCT c.Id) AS ContractsStartedThisYear,
              IFNULL(SUM(r.Amount), 0) AS ReceiptsTotalThisYear
            FROM Units u
            LEFT JOIN Contracts c 
              ON c.UnitId = u.Id
              AND strftime('%Y', c.ContractStartDate) = @year
            LEFT JOIN Receipts r
              ON r.ContractId = c.Id
              AND strftime('%Y', r.ReceiptDate) = @year
            GROUP BY u.Id, u.UnitName
            ORDER BY ReceiptsTotalThisYear DESC;
        """;

        cmd.Parameters.AddWithValue("@year", year.ToString());

        var list = new List<UnitYearStatRowRealEstate>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new UnitYearStatRowRealEstate
            {
                UnitId = reader.GetInt64(0),
                UnitName = reader.GetString(1),
                ContractsStartedThisYear = reader.GetInt32(2),
                ReceiptsTotalThisYear = reader.GetDecimal(3)
            });
        }
        return list;
    }
}