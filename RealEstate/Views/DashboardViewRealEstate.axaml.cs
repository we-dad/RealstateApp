using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    private readonly SupabaseService _supabaseService;
    private readonly TenantServiceRealEstate _tenants;

    // kept so the PDF export can reuse the last computed figures
    private int _year;
    private decimal _totalReceipts, _totalExpenses;
    private int _rented, _vacant;
    private List<UnitYearStatRowRealEstate> _buildingStats = new();
    private List<UpcomingPaymentRowRealEstate> _upcoming = new();
    private List<LateTenantRowRealEstate> _late = new();
    private List<(string month, decimal receipts, decimal expenses)> _monthly = new();

    // parameterless ctor kept for the designer / existing callers
    public DashboardViewRealEstate() : this(null) { }

    public DashboardViewRealEstate(SupabaseService? supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService!;
        _tenants = new TenantServiceRealEstate(_db);

        try
        {
            _db.Initialize();

            var nowYear = DateTime.Now.Year;
            // "الكل" (all) plus the last 6 years
            var years = new List<object> { "الكل" };
            years.AddRange(Enumerable.Range(nowYear - 5, 6).Reverse().Select(y => (object)y));
            YearCombo.ItemsSource = years;
            YearCombo.SelectedItem = nowYear;

            RefreshBtn.Click += async (_, __) => await LoadForSelectedYearAsync();

            YearCombo.SelectionChanged += async (_, __) => await LoadForSelectedYearAsync();

            // counters — units now count real units only (ParentId <> 0)
            OwnersCountText.Text = new OwnerServiceRealEstate(_db).GetAll().Count.ToString();
            UnitsCountText.Text = CountRealUnits().ToString();
            TenantsCountText.Text = new TenantServiceRealEstate(_db).GetAll().Count.ToString();
            ContractsCountText.Text = new ContractServiceRealEstate(_db).GetAll().Count.ToString();
            ReceiptsCountText.Text = new ReceiptServiceRealEstate(_db).GetAll().Count.ToString();

            if (ExportPdfBtn != null)
                ExportPdfBtn.Click += async (_, __) => await ExportPdfAsync();

            this.AttachedToVisualTree += async (_, __) => await LoadForSelectedYearAsync();
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

    // resolve the combo selection: a real year, or 0 for "الكل"
    private async Task LoadForSelectedYearAsync()
    {
        var year = YearCombo.SelectedItem is int y ? y : 0;   // "الكل" -> 0
        await LoadDashboardAsync(year);
    }

    // label used in the PDF filename / header
    private string YearLabel => _year == 0 ? "الكل" : _year.ToString();

    private int CountRealUnits()
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM UnitsRealEstate WHERE ParentId <> 0 AND SyncAction <> 'delete';";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private async Task LoadDashboardAsync(int year)
    {
        try
        {
            _year = year;

            await using var con = new SqliteConnection(_db.ConnectionString);
            await con.OpenAsync();

            _totalReceipts = await ScalarDecimalAsync(con, """
                SELECT IFNULL(SUM(r.Amount), 0)
                FROM ReceiptsRealEstate r
                WHERE strftime('%Y', r.ReceiptDate) LIKE @year
                  AND r.SyncAction <> 'delete';
            """, year);

            _totalExpenses = await ScalarDecimalAsync(con, """
                SELECT IFNULL(SUM(e.ExpensesAmount), 0)
                FROM ExpensesRealEstate e
                WHERE strftime('%Y', e.ExpensesDate) LIKE @year
                  AND e.SyncAction <> 'delete';
            """, year);

            (_rented, _vacant) = await UnitsCountAsync(con);

            TotalReceiptsText.Text = _totalReceipts.ToString("N2", CultureInfo.InvariantCulture);
            TotalExpensesText.Text = _totalExpenses.ToString("N2", CultureInfo.InvariantCulture);
            NetText.Text = (_totalReceipts - _totalExpenses).ToString("N2", CultureInfo.InvariantCulture);
            RentedText.Text = _rented.ToString();
            VacantText.Text = _vacant.ToString();

            var receiptsMonthly = await MonthTotalsAsync(con, year, """
                SELECT strftime('%Y-%m', r.ReceiptDate) AS Month, IFNULL(SUM(r.Amount),0) AS Total
                FROM ReceiptsRealEstate r
                WHERE strftime('%Y', r.ReceiptDate) LIKE @year
                  AND r.SyncAction <> 'delete'
                GROUP BY Month
                ORDER BY Month;
            """);

            var expensesMonthly = await MonthTotalsAsync(con, year, """
                SELECT strftime('%Y-%m', e.ExpensesDate) AS Month, IFNULL(SUM(e.ExpensesAmount),0) AS Total
                FROM ExpensesRealEstate e
                WHERE strftime('%Y', e.ExpensesDate) LIKE @year
                  AND e.SyncAction <> 'delete'
                GROUP BY Month
                ORDER BY Month;
            """);

            BuildMonthlyChart(receiptsMonthly, expensesMonthly);
            BuildOccupancyPie(_rented, _vacant);

            // merge monthly receipts + expenses into one series for the PDF
            var allMonths = receiptsMonthly.Select(x => x.month)
                .Union(expensesMonthly.Select(x => x.month))
                .OrderBy(m => m)
                .ToList();
            _monthly = allMonths.Select(m => (
                m,
                receiptsMonthly.FirstOrDefault(x => x.month == m).total,
                expensesMonthly.FirstOrDefault(x => x.month == m).total
            )).ToList();

            _buildingStats = await GetBuildingYearStatsAsync(con, year);
            UnitsStatsGrid.ItemsSource = _buildingStats;
            BuildTopUnitsBarChart(_buildingStats);

            _upcoming = await GetUpcomingThisMonthAsync(con);
            UpcomingGrid.ItemsSource = _upcoming;

            _late = await GetLateTenantsAsync(con);
            LateGrid.ItemsSource = _late;

            if (UpcomingCountText != null)
                UpcomingCountText.Text = _upcoming.Count.ToString();
            if (LateCountText != null)
                LateCountText.Text = _late.Count.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    // ---------- charts ----------

    private void BuildMonthlyChart(List<(string month, decimal total)> receipts,
                                   List<(string month, decimal total)> expenses)
    {
        var labels = receipts.Select(x => x.month)
            .Union(expenses.Select(x => x.month))
            .OrderBy(x => x)
            .ToArray();

        double[] receiptsValues = labels.Select(m => (double)(receipts.FirstOrDefault(x => x.month == m).total)).ToArray();
        double[] expensesValues = labels.Select(m => (double)(expenses.FirstOrDefault(x => x.month == m).total)).ToArray();

        MonthlyChart.XAxes = new[]
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = 45,          // slant so long lists don't overlap
                ForceStepToMin = true,
                MinStep = 1                   // show every month, don't skip
            }
        };
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

    // ---------- queries ----------

    private static async Task<decimal> ScalarDecimalAsync(SqliteConnection con, string sql, int year)
    {
        await using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        // year 0 means "all" -> wildcard matches every year
        cmd.Parameters.AddWithValue("@year", year == 0 ? "%" : year.ToString());
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToDecimal(result);
    }

    // occupancy counts REAL units only, never the parent rows
    private static async Task<(int rented, int vacant)> UnitsCountAsync(SqliteConnection con)
    {
        await using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT
              SUM(CASE WHEN UnitState = 'مؤجرة' THEN 1 ELSE 0 END) AS RentedCount,
              SUM(CASE WHEN UnitState = 'شاغرة' THEN 1 ELSE 0 END) AS VacantCount
            FROM UnitsRealEstate
            WHERE ParentId <> 0
              AND SyncAction <> 'delete';
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
        cmd.Parameters.AddWithValue("@year", year == 0 ? "%" : year.ToString());

        var list = new List<(string month, decimal total)>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add((reader.GetString(0), reader.GetDecimal(1)));
        return list;
    }

    // BUILDING-level stats: group by the parent, roll up its units'
    // contracts, receipts and expenses. Subqueries, not joins, so
    // receipts and expenses can't multiply each other's rows.
    private static async Task<List<UnitYearStatRowRealEstate>> GetBuildingYearStatsAsync(SqliteConnection con, int year)
    {
        await using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT
              p.Id,
              p.UnitName,

              (SELECT COUNT(*) FROM UnitsRealEstate u
                WHERE u.ParentId = p.Id AND u.SyncAction <> 'delete')                       AS UnitsCount,
              (SELECT COUNT(*) FROM UnitsRealEstate u
                WHERE u.ParentId = p.Id AND u.SyncAction <> 'delete'
                  AND u.UnitState = 'مؤجرة')                                                 AS RentedCount,
              (SELECT COUNT(*) FROM UnitsRealEstate u
                WHERE u.ParentId = p.Id AND u.SyncAction <> 'delete'
                  AND u.UnitState = 'شاغرة')                                                 AS VacantCount,

              (SELECT COUNT(DISTINCT c.Id)
                 FROM ContractsRealEstate c
                 JOIN UnitsRealEstate u ON u.Id = c.UnitId
                WHERE u.ParentId = p.Id
                  AND c.SyncAction <> 'delete'
                  AND strftime('%Y', c.ContractStartDate) LIKE @year)                          AS ContractsThisYear,

              (SELECT IFNULL(SUM(r.Amount), 0)
                 FROM ReceiptsRealEstate r
                 JOIN ContractsRealEstate c ON c.Id = r.ContractId
                 JOIN UnitsRealEstate u     ON u.Id = c.UnitId
                WHERE u.ParentId = p.Id
                  AND r.SyncAction <> 'delete'
                  AND strftime('%Y', r.ReceiptDate) LIKE @year)                                AS ReceiptsThisYear,

              (SELECT IFNULL(SUM(e.ExpensesAmount), 0)
                 FROM ExpensesRealEstate e
                 JOIN UnitsRealEstate u ON u.Id = e.UnitId
                WHERE u.ParentId = p.Id
                  AND e.SyncAction <> 'delete'
                  AND strftime('%Y', e.ExpensesDate) LIKE @year)                               AS ExpensesThisYear

            FROM UnitsRealEstate p
            WHERE p.ParentId = 0
              AND p.SyncAction <> 'delete'
            ORDER BY ReceiptsThisYear DESC;
        """;

        cmd.Parameters.AddWithValue("@year", year == 0 ? "%" : year.ToString());

        var list = new List<UnitYearStatRowRealEstate>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new UnitYearStatRowRealEstate
            {
                UnitId = reader.GetInt64(0),
                UnitName = reader.GetString(1),
                UnitsCount = reader.GetInt32(2),
                RentedCount = reader.GetInt32(3),
                VacantCount = reader.GetInt32(4),
                ContractsStartedThisYear = reader.GetInt32(5),
                ReceiptsTotalThisYear = reader.GetDecimal(6),
                ExpensesTotalThisYear = reader.GetDecimal(7)
            });
        }
        return list;
    }

    // periods-per-year implied by the pay method string
    private static int PeriodsPerYear(string payMethod)
    {
        if (string.IsNullOrWhiteSpace(payMethod)) return 12;
        if (payMethod.Contains("شهري")) return 12;
        if (payMethod.Contains("6") || payMethod.Contains("نصف")) return 2;
        if (payMethod.Contains("3") || payMethod.Contains("ربع")) return 4;
        if (payMethod.Contains("سنوي") || payMethod.Contains("سنة")) return 1;
        return 12;
    }

    // months in one payment period
    private static int MonthsPerPeriod(string payMethod) => 12 / PeriodsPerYear(payMethod);

    // Active contracts whose next payment period boundary falls inside
    // the current calendar month.
    private static async Task<List<UpcomingPaymentRowRealEstate>> GetUpcomingThisMonthAsync(SqliteConnection con)
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        await using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT
              c.Id, c.ContractNumber, c.ContractStartDate, c.ContractEndDate,
              c.RentAmount, c.ContractPayMethod,
              t.Name, t.Id,
              u.UnitName,
              IFNULL(p.UnitName, '')
            FROM ContractsRealEstate c
            JOIN TenantsRealEstate t   ON t.Id = c.TenantId
            JOIN UnitsRealEstate u     ON u.Id = c.UnitId
            LEFT JOIN UnitsRealEstate p ON p.Id = u.ParentId
            WHERE c.SyncAction <> 'delete'
              AND c.ContractState = 'جاري';
        """;

        var list = new List<UpcomingPaymentRowRealEstate>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var start = reader.GetDateTime(2);
            var end = reader.GetDateTime(3);
            var rent = reader.GetDecimal(4);
            var payMethod = reader.GetString(5);
            var step = MonthsPerPeriod(payMethod);

            // walk period boundaries from the start date; find the first
            // one that lands in this month and before the contract ends
            var due = start;
            while (due < monthStart && due <= end)
                due = due.AddMonths(step);

            if (due >= monthStart && due <= monthEnd && due <= end)
            {
                list.Add(new UpcomingPaymentRowRealEstate
                {
                    ContractId = reader.GetInt64(0),
                    ContractNumber = reader.GetString(1),
                    TenantName = reader.GetString(6),
                    TenantId = reader.GetInt64(7),
                    UnitName = reader.GetString(8),
                    BuildingName = reader.GetString(9),
                    DueDate = due,
                    Amount = rent,
                    DaysUntilDue = (due - today).Days
                });
            }
        }

        return list.OrderBy(x => x.DueDate).ToList();
    }

    // Active contracts where money expected-to-date exceeds money paid.
    //
    // NOTE ON METHOD: receipts store only date + amount, never which month
    // they cover, and amounts don't always equal the rent (tenants pay
    // several periods at once). So "late" cannot be known exactly - it is
    // inferred from the running balance: expected-to-date = periods elapsed
    // x rent; if total paid is less, the tenant is behind by the shortfall.
    private static async Task<List<LateTenantRowRealEstate>> GetLateTenantsAsync(SqliteConnection con)
    {
        var today = DateTime.Today;

        await using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT
              c.Id, c.ContractNumber, c.ContractStartDate, c.ContractEndDate,
              c.RentAmount, c.ContractPayMethod,
              t.Name, t.Phone,
              u.UnitName,
              IFNULL(p.UnitName, ''),
              (SELECT IFNULL(SUM(r.Amount),0) FROM ReceiptsRealEstate r
                WHERE r.ContractId = c.Id AND r.SyncAction <> 'delete'),
              (SELECT MAX(date(r.ReceiptDate)) FROM ReceiptsRealEstate r
                WHERE r.ContractId = c.Id AND r.SyncAction <> 'delete'),
              t.Id
            FROM ContractsRealEstate c
            JOIN TenantsRealEstate t   ON t.Id = c.TenantId
            JOIN UnitsRealEstate u     ON u.Id = c.UnitId
            LEFT JOIN UnitsRealEstate p ON p.Id = u.ParentId
            WHERE c.SyncAction <> 'delete'
              AND c.ContractState = 'جاري';
        """;

        var list = new List<LateTenantRowRealEstate>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var start = reader.GetDateTime(2);
            var end = reader.GetDateTime(3);
            var rent = reader.GetDecimal(4);
            var payMethod = reader.GetString(5);
            var paid = reader.GetDecimal(10);
            var lastPay = reader.IsDBNull(11) ? (DateTime?)null : DateTime.Parse(reader.GetString(11));

            var step = MonthsPerPeriod(payMethod);

            // whole payment periods elapsed since the contract start
            var monthsElapsed = (today.Year - start.Year) * 12 + today.Month - start.Month;
            if (today.Day < start.Day) monthsElapsed -= 1;
            if (monthsElapsed < 0) monthsElapsed = 0;

            // cap at the contract length - no rent expected past its end
            var contractMonths = (end.Year - start.Year) * 12 + end.Month - start.Month;
            if (monthsElapsed > contractMonths) monthsElapsed = contractMonths;

            var periodsElapsed = (monthsElapsed / step) + 1; // first period due at start
            var expected = rent * periodsElapsed;

            if (paid + 0.01m < expected)
            {
                var shortfall = expected - paid;
                var periodsBehind = rent == 0 ? 0 : (int)Math.Ceiling((double)(shortfall / rent));

                list.Add(new LateTenantRowRealEstate
                {
                    ContractId = reader.GetInt64(0),
                    ContractNumber = reader.GetString(1),
                    TenantName = reader.GetString(6),
                    TenantPhone = reader.GetString(7),
                    UnitName = reader.GetString(8),
                    BuildingName = reader.GetString(9),
                    PayMethod = payMethod,
                    RentPerPeriod = rent,
                    ExpectedToDate = expected,
                    PaidToDate = paid,
                    PeriodsBehind = periodsBehind,
                    LastPaymentDate = lastPay,
                    TenantId = reader.GetInt64(12)
                });
            }
        }

        return list.OrderByDescending(x => x.Outstanding).ToList();
    }

    // ---------- row double-click -> tenant window ----------

    private void LateGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (LateGrid.SelectedItem is LateTenantRowRealEstate row)
            OpenTenant(row.TenantId);
    }

    private void UpcomingGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (UpcomingGrid.SelectedItem is UpcomingPaymentRowRealEstate row)
            OpenTenant(row.TenantId);
    }

    private void OpenTenant(long tenantId)
    {
        try
        {
            var tenant = _tenants.GetById(tenantId);
            if (tenant is null) return;

            new TenantsWindowViewRealEstate(tenant, _supabaseService).Show();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    // ---------- PDF ----------

    private async Task ExportPdfAsync()
    {
        await SaveAndOpenAsync("التقرير العقاري", path =>
            new PdfServiceRealEstate().ExportDashboard(
                path, _year,
                _totalReceipts, _totalExpenses,
                _rented, _vacant,
                _buildingStats, _monthly,
                OwnersCountText.Text ?? "-",
                UnitsCountText.Text ?? "-",
                TenantsCountText.Text ?? "-",
                ContractsCountText.Text ?? "-",
                ReceiptsCountText.Text ?? "-"));
    }

    private async void ExportLate_Click(object? sender, RoutedEventArgs e)
    {
        await SaveAndOpenAsync("المتأخرون للعقار", path =>
            new PdfServiceRealEstate().ExportLateTenants(path, YearLabel, _late));
    }

    private async void ExportUpcoming_Click(object? sender, RoutedEventArgs e)
    {
        await SaveAndOpenAsync("الدفعات_المستحقة للعقار", path =>
            new PdfServiceRealEstate().ExportUpcoming(path, YearLabel, _upcoming));
    }

    private async Task SaveAndOpenAsync(string namePrefix, Action<string> export)
    {
        try
        {
            var top = TopLevel.GetTopLevel(this);
            if (top is null) return;

            var file = await top.StorageProvider.SaveFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "حفظ التقرير",
                    SuggestedFileName = $"{namePrefix}_{YearLabel}_{DateTime.Now:yyyy-MM-dd}.pdf",
                    DefaultExtension = "pdf"
                });

            if (file is null) return;

            var path = file.Path.LocalPath;
            export(path);
            OpenFile(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    // open the saved PDF in the OS default viewer
    private static void OpenFile(string path)
    {
        try
        {
            // the file must exist and be flushed before we try to open it
            for (int i = 0; i < 20 && !System.IO.File.Exists(path); i++)
                System.Threading.Thread.Sleep(50);

            if (!System.IO.File.Exists(path))
            {
                Console.WriteLine("PDF not found to open: " + path);
                return;
            }

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "open",
                    ArgumentList = { path },
                    UseShellExecute = false
                });
            }
            else
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "xdg-open",
                    ArgumentList = { path },
                    UseShellExecute = false
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Could not open PDF: " + ex.Message);
        }
    }
}