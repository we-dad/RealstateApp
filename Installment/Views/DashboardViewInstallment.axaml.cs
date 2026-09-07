using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class DashboardViewInstallment : UserControl
{
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly DashboardServiceInstallment _dashboardDB;
    private readonly CustomerServiceInstallment _customers;
    private readonly SupabaseService _supabaseService;

    private DashboardStatsInstallment? _last;   // kept for the PDF export
    private int? _selectedYear;

    // parameterless ctor kept for the designer / existing callers
    public DashboardViewInstallment() : this(null) { }

    public DashboardViewInstallment(SupabaseService? supabaseService)
    {
        InitializeComponent();
        _db.Initialize();
        _dashboardDB = new DashboardServiceInstallment(_db);
        _customers = new CustomerServiceInstallment(_db);
        _supabaseService = supabaseService!;

        var nowYear = DateTime.Now.Year;
        // "الكل" (all years) plus the last 6 years
        var years = new System.Collections.Generic.List<object> { "الكل" };
        years.AddRange(Enumerable.Range(nowYear - 5, 6).Reverse().Select(y => (object)y));
        YearCombo.ItemsSource = years;

        // default to the current year (index 0 is "الكل", index 1 is nowYear)
        YearCombo.SelectedItem = nowYear;

        YearCombo.SelectionChanged += (_, __) => LoadDashboard();

        LoadDashboard();
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e) => LoadDashboard();

    private void LoadDashboard()
    {
        _selectedYear = YearCombo.SelectedItem is int y ? y : (int?)null;

        var s = _dashboardDB.GetStats(_selectedYear);
        _last = s;

        TotalCollectedText.Text = s.TotalCollected.ToString("N2");
        TotalExpensesText.Text  = s.TotalExpenses.ToString("N2");
        NetIncomeText.Text      = s.NetIncome.ToString("N2");
        OutstandingText.Text    = s.OutstandingBalance.ToString("N2");

        OwnersCountText.Text    = s.OwnersCount.ToString();
        CustomersCountText.Text = s.CustomersCount.ToString();
        ProductsCountText.Text  = s.ProductsCount.ToString();
        ContractsCountText.Text = s.ContractsCount.ToString();
        ReceiptsCountText.Text  = s.ReceiptsCount.ToString();
        ExpensesCountText.Text  = s.ExpensesCount.ToString();

        CollectionTargetText.Text =
            $"{s.CollectedThisMonth:N0} من {s.TargetThisMonth:N0} ({s.CollectionProgressPercent}%)";
        CollectionBar.Value = s.CollectionProgressPercent;

        ActiveCountText.Text   = s.ActiveContracts.ToString();
        LateCountText.Text     = s.LateContracts.ToString();
        FinishedCountText.Text = s.FinishedContracts.ToString();

        // ---- income vs expenses line chart (same style as real-estate) ----
        var labels = s.MonthlyFlows.Select(f => f.Month).ToArray();
        var income = s.MonthlyFlows.Select(f => f.Collected).ToArray();
        var expenses = s.MonthlyFlows.Select(f => f.Spent).ToArray();

        FlowChart.XAxes = new[] { new Axis { Labels = labels } };
        FlowChart.YAxes = new[] { new Axis { MinLimit = 0 } };
        FlowChart.Series = new ISeries[]
        {
            new LineSeries<double> { Name = "الدخل", Values = income, GeometrySize = 10 },
            new LineSeries<double> { Name = "المصروفات", Values = expenses, GeometrySize = 10 }
        };

        TopProductsList.ItemsSource = s.TopProducts;
        LateGrid.ItemsSource        = s.LatePayers;
        UpcomingGrid.ItemsSource    = s.Upcoming;

        LateListCountText.Text      = s.LatePayers.Count.ToString();
        UpcomingListCountText.Text  = s.Upcoming.Count.ToString();
    }

    private async void ExportPdf_Click(object? sender, RoutedEventArgs e)
    {
        await SavePdfAsync("تقرير_التقسيط", (path, label, stats) =>
            new PdfServiceInstallment().ExportDashboard(path, label, stats));
    }

    private async void ExportLate_Click(object? sender, RoutedEventArgs e)
    {
        await SavePdfAsync("المتأخرون للأقساط", (path, label, stats) =>
            new PdfServiceInstallment().ExportLatePayers(path, label, stats));
    }

    private async void ExportUpcoming_Click(object? sender, RoutedEventArgs e)
    {
        await SavePdfAsync("الأقساط_المستحقة", (path, label, stats) =>
            new PdfServiceInstallment().ExportUpcoming(path, label, stats));
    }

    private async Task SavePdfAsync(
        string namePrefix,
        Action<string, string, DashboardStatsInstallment> export)
    {
        try
        {
            if (_last is null) return;

            var top = TopLevel.GetTopLevel(this);
            if (top is null) return;

            var label = _selectedYear?.ToString() ?? "الكل";
            var today = DateTime.Now.ToString("yyyy-MM-dd");

            var file = await top.StorageProvider.SaveFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "حفظ التقرير",
                    SuggestedFileName = $"{namePrefix}_{label}_{today}.pdf",
                    DefaultExtension = "pdf"
                });

            if (file is null) return;

            var path = file.Path.LocalPath;
            export(path, label, _last);
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
            for (int i = 0; i < 20 && !System.IO.File.Exists(path); i++)
                System.Threading.Thread.Sleep(50);

            if (!System.IO.File.Exists(path))
            {
                Console.WriteLine("PDF not found to open: " + path);
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start(new ProcessStartInfo { FileName = "open", ArgumentList = { path }, UseShellExecute = false });
            else
                Process.Start(new ProcessStartInfo { FileName = "xdg-open", ArgumentList = { path }, UseShellExecute = false });
        }
        catch (Exception ex)
        {
            Console.WriteLine("Could not open PDF: " + ex.Message);
        }
    }

    // open the customer window from a late-payer row
    private void LateGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (LateGrid.SelectedItem is LatePayer row)
            OpenCustomer(row.CustomerId);
    }

    // open the customer window from an upcoming-payment row
    private void UpcomingGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (UpcomingGrid.SelectedItem is UpcomingInstallment row)
            OpenCustomer(row.CustomerId);
    }

    private void OpenCustomer(long customerId)
    {
        try
        {
            var customer = _customers.GetById(customerId);
            if (customer is null) return;

            new CustomerWindowViewInstallment(customer, _supabaseService).Show();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}