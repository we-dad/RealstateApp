using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Linq;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class DashboardViewInstallment : UserControl
{
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly DashboardServiceInstallment _dashboardDB;

    public DashboardViewInstallment()
    {
        InitializeComponent();
        _db.Initialize();
        _dashboardDB = new DashboardServiceInstallment(_db);
        LoadDashboard();
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e) => LoadDashboard();

    private void LoadDashboard()
    {
        var s = _dashboardDB.GetStats();

        TotalCollectedText.Text = s.TotalCollected.ToString("N2");
        TotalExpensesText.Text  = s.TotalExpenses.ToString("N2");
        NetIncomeText.Text      = s.NetIncome.ToString("N2");
        OutstandingText.Text    = s.OutstandingBalance.ToString("N2");

        CollectionTargetText.Text =
            $"{s.CollectedThisMonth:N0} من {s.TargetThisMonth:N0} ({s.CollectionProgressPercent}%)";
        CollectionBar.Value = s.CollectionProgressPercent;

        ActiveCountText.Text   = s.ActiveContracts.ToString();
        LateCountText.Text     = s.LateContracts.ToString();
        FinishedCountText.Text = s.FinishedContracts.ToString();
        CapitalDeployedText.Text = $"رأس المال المرتبط: {s.CapitalDeployed:N0}";

        // scale bars to the tallest value in the window (110px max)
        double max = s.MonthlyFlows.Count == 0 ? 1 :
            s.MonthlyFlows.Max(f => System.Math.Max(f.Collected, f.Spent));
        if (max <= 0) max = 1;
        foreach (var f in s.MonthlyFlows)
        {
            f.CollectedBarHeight = f.Collected / max * 110;
            f.SpentBarHeight     = f.Spent / max * 110;
        }

        FlowChart.ItemsSource       = s.MonthlyFlows;
        TopProductsList.ItemsSource = s.TopProducts;
        LateGrid.ItemsSource        = s.LatePayers;
        UpcomingGrid.ItemsSource    = s.Upcoming;
    }
}