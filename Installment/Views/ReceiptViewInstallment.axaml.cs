using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class ReceiptViewInstallment : UserControl
{
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly ReceiptServiceInstallment _receiptsDB;
    private readonly ContractServiceInstallment _contractsDB;
    private readonly PdfServiceInstallment _pdfService;
    private readonly SupabaseService _supabaseService;
    private readonly InstallmentSyncService _sync;

    private AutoCompleteBox? _contractIdSearchBox;
    private ContractInstallment? _selectedContract;

    public ReceiptViewInstallment(SupabaseService supabaseService)
    {
        InitializeComponent();
        // Search box above the grid: shows the rows that contain every word typed.
        _gridSearch = new GridSearch<ReceiptInstallment>(ReceiptsGrid, ReceiptsGridSearchBox);
        _supabaseService = supabaseService;

        _db.Initialize();

        _receiptsDB = new ReceiptServiceInstallment(_db);
        _contractsDB = new ContractServiceInstallment(_db);
        _pdfService = new PdfServiceInstallment();

        ReceiptsGrid.DoubleTapped += ReceiptsGrid_DoubleTapped;

        // The suggestions list matches the number, the customer name or the product.
        // Wired here (not in the XAML) so nothing fires while the window is being built.
        ContractNumSearchBox.ItemFilter = (search, item) =>
            item is ContractPickRowInstallment row
            && !string.IsNullOrWhiteSpace(search)
            && row.Display.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase);
        ContractNumSearchBox.ItemSelector = (search, item) =>
            (item as ContractPickRowInstallment)?.ContractNumber ?? search;
        ContractNumSearchBox.SelectionChanged += ContractNumSearchBox_SelectionChanged;
        ContractNumSearchBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == AutoCompleteBox.TextProperty)
                ContractNumSearchBox_TextChanged();
        };

        Refresh();

        _contractIdSearchBox = this.FindControl<AutoCompleteBox>("ContractNumSearchBox");

        // Reload the grid (only) when data changes: an add, an edit or a delete, also
        // from the details window, so there is no need to press "تحديث".
        _autoRefresh = new ScreenAutoRefresh(
            this,
            LoadReceipt,
            periodicSync: PeriodicSyncAsync,
            interval: TimeSpan.FromMinutes(2));

        _sync = new InstallmentSyncService(_db, _supabaseService);
        _ = SyncAsync();
    }

    private void LoadPaymentMethod()
    {
        PaymentMethodBox.ItemsSource = new List<string>
        {
            "تحويل",
            "كاش"
        };

        PaymentMethodBox.SelectedIndex = 0;
    }

    // Every reload (open, add, pull, timer) keeps the selected row selected, chosen at
    // the moment the grid is replaced, so a row picked while a sync runs is not undone.
    private void LoadReceipt() =>
        ScreenAutoRefresh.ReloadKeepingSelection<ReceiptInstallment>(ReceiptsGrid, r => r.Id, LoadReceiptCore, () => _gridSearch?.AfterLoad());

    private void LoadReceiptCore()
    {
        try
        {
            var data = _receiptsDB.GetAll();

            ReceiptsGrid.ItemsSource = null;
            ReceiptsGrid.ItemsSource = data;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var receiptNum = ReceiptNumBox.Text?.Trim() ?? "";
            var receiptDate = DateTime.Today;

            if (_selectedContract == null)
            {
                ContractInfoText.Text = "يرجى اختيار العقد أولاً";
                ContractInfoText.Foreground = Brushes.Red;
                return;
            }

            var contract = _selectedContract;
            var paymentMethod = PaymentMethodBox.SelectedItem as string ?? "تحويل";

            var amount = double.Parse(
                AmountBox.Text?.Trim() ?? "0",
                CultureInfo.InvariantCulture
            );

            _receiptsDB.Add(
                receiptNum,
                receiptDate,
                contract.Id,
                paymentMethod,
                amount
            );

            Refresh();

            _ = _sync.PushAllDirtyAsync();

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    // push must finish before the pull, or the pull re-reads rows the
    // push has not written CloudIds for yet and duplicates them
    //
    // One sync at a time for this screen (static: also across an old and a new instance
    // of the screen). Two overlapping pulls could both insert the same new cloud row.
    // A request from the user (opening the screen, the refresh button) WAITS for its
    // turn, so it is never lost.
    private static readonly SemaphoreSlim _syncGate = new SemaphoreSlim(1, 1);

    private async Task SyncAsync()
    {
        await _syncGate.WaitAsync();

        try
        {
            await _sync.PushAllDirtyAsync();
            await SyncReceiptsFromCloudAsync();
        }
        finally
        {
            _syncGate.Release();
        }
    }

    // Every 2 minutes (while the app is active); skipped while a sync is already running.
    private Task PeriodicSyncAsync() =>
        _syncGate.CurrentCount == 0 ? Task.CompletedTask : SyncAsync();

    private async Task SyncReceiptsFromCloudAsync()
    {
        if (!AppSession.CanReadOnline)
            return;

        try
        {
            var cloudReceipts = new CloudReceiptsInstallmentService(_supabaseService);
            var rows = await cloudReceipts.GetReceiptsAsync();

            foreach (var row in rows)
            {
                var contractLocalId = _contractsDB.GetLocalIdByCloudId(row.ContractId);

                if (contractLocalId == 0)
                    continue;

                _receiptsDB.UpsertFromCloud(
                    row.Id,
                    row.ReceiptNumber,
                    row.ReceiptDate,
                    contractLocalId,
                    row.PaymentMethod,
                    row.Amount,
                    row.CurrentTotalAmount
                );
            }

            LoadReceipt();

            SyncStatusService.ReportPull(true);
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping installment receipts cloud sync.");
            SyncStatusService.ReportPull(false, offline: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            SyncStatusService.ReportPull(false);
        }
    }

    // Picking a line from the suggestions selects that exact contract, looked up by
    // its full stored number (a short search could match several contracts).
    private void ContractNumSearchBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ContractNumSearchBox.SelectedItem is not ContractPickRowInstallment row)
            return;

        ContractNumSearchBox.Text = row.ContractNumber;

        var contract = _contractsDB.FindByContractNum(row.ContractNumber);

        if (contract is null)
        {
            ResetContractSelection();
            ContractInfoText.Text = "لم يتم العثور على عقد بهذا الرقم";
            ContractInfoText.Foreground = Brushes.Red;
            return;
        }

        ShowSelectedContract(contract);
    }

    // If the text no longer points at the chosen contract, forget that contract,
    // so a receipt can never be saved on a contract the box does not show.
    private void ContractNumSearchBox_TextChanged()
    {
        if (_selectedContract is null)
            return;

        var typed = ContractNumSearchBox.Text?.Trim() ?? "";

        if (typed.Equals(_selectedContract.ContractNumber, StringComparison.OrdinalIgnoreCase))
            return;

        ResetContractSelection();
        ContractInfoText.Text = "";
    }

    private void ShowSelectedContract(ContractInstallment contract)
    {
        _selectedContract = contract;

        ContractInfoText.Text = $"تم اختيار عقد {contract.CustomerName}";
        ContractInfoText.Foreground = Brushes.Green;

        NoContractPlaceholder.IsVisible = false;
        ContractDetailsPanel.IsVisible = true;

        DetailsChipText.Text = contract.ContractState;
        DetailsChip.Classes.Set("ok", contract.IsStateOk);
        DetailsChip.Classes.Set("late", contract.IsStateLate);

        DetailsContractNumber.Text = $"عقد {contract.ContractNumber}";
        DetailsContractDates.Text =
            $"من {contract.ContractStartDate:yyyy-MM-dd} إلى {contract.ContractEndDate:yyyy-MM-dd} · " +
            $"{contract.ContractPeriod:0.#} قسط شهري";

        DetailsStatTotal.Text = contract.TotalWithDownPayment.ToString("N2");
        DetailsStatPaid.Text = Math.Max(0, contract.MainTotalAmount - contract.CurrentTotalAmount).ToString("N2");
        DetailsStatRemaining.Text = contract.CurrentTotalAmount.ToString("N2");
        DetailsStatMonthly.Text = contract.MonthlyInstallment.ToString("N2");

        DetailsProductName.Text = string.IsNullOrWhiteSpace(contract.ProductName) ? "—" : contract.ProductName;
        DetailsProductPrice.Text = contract.ProductMainPrice.ToString("N2");

        DetailsCustomerName.Text = string.IsNullOrWhiteSpace(contract.CustomerName) ? "—" : contract.CustomerName;
        DetailsCustomerPhone.Text = string.IsNullOrWhiteSpace(contract.CustomerPhone) ? "—" : contract.CustomerPhone;

        DetailsOwnerName.Text = string.IsNullOrWhiteSpace(contract.OwnerName) ? "—" : contract.OwnerName;
        DetailsOwnerPhone.Text = string.IsNullOrWhiteSpace(contract.OwnerPhone) ? "—" : contract.OwnerPhone;

        UpdatePreviewAndStrip();
    }

    // Hides the contract details panel and forgets the selection. Called whenever the
    // chosen contract stops being valid (search cleared, not found, screen reset) - a
    // receipt can then never be saved against a contract the screen is not showing.
    private void ResetContractSelection()
    {
        _selectedContract = null;

        ContractDetailsPanel.IsVisible = false;
        NoContractPlaceholder.IsVisible = true;
        InstallmentStripPanel.Children.Clear();
        DetailsDueText.Text = "";

        UpdatePreviewAndStrip();
    }

    private void SearchContract_Click(object? sender, RoutedEventArgs e)
    {
        var raw = _contractIdSearchBox?.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(raw))
            return;

        var contractNum = raw.StartsWith("Ic-", StringComparison.OrdinalIgnoreCase)
            ? raw
            : "Ic-" + raw;

        var contract = _contractsDB.FindByTypedNumber(contractNum, out var candidates);

        if (contract is null)
        {
            ResetContractSelection();
            ContractInfoText.Text = candidates.Count > 1
                ? "يوجد أكثر من عقد بهذا الرقم، اكتب الرقم كاملًا: " + string.Join("، ", candidates)
                : "لم يتم العثور على عقد بهذا الرقم";
            ContractInfoText.Foreground = Brushes.Red;
            return;
        }

        ShowSelectedContract(contract);
    }

    // ===================== Live preview + installment progress strip =====================
    // Both are display only: neither writes to the database. The amount that actually
    // gets saved is read straight from AmountBox by Add_Click, exactly as before.

    private double TypedAmount() =>
        double.TryParse(AmountBox.Text?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : 0;

    private void AmountBox_TextChanged(object? sender, TextChangedEventArgs e) => UpdatePreviewAndStrip();

    // ContractPeriod is a free-typed number of months with no real upper bound in the
    // contract screen. A very large value would push ContractStartDate.AddMonths(i+1)
    // past year 9999 and throw. 50 years covers any real contract; anything above that
    // is bad data, and the strip should not crash the screen over it.
    private const int MaxStripMonths = 600;

    private static int SafePeriod(ContractInstallment contract) =>
        Math.Clamp((int)Math.Round(contract.ContractPeriod), 0, MaxStripMonths);

    private void UpdatePreviewAndStrip()
    {
        var contract = _selectedContract;

        if (contract is null)
        {
            PreviewCoverText.Text = "—";
            PreviewRemainingText.Text = "—";
            PreviewStateText.Text = "—";
            InstallmentStripPanel.Children.Clear();
            return;
        }

        var amount = TypedAmount();
        var monthly = contract.MonthlyInstallment;
        var remainingBefore = contract.CurrentTotalAmount;
        var remainingAfter = Math.Max(0, remainingBefore - amount);

        PreviewRemainingText.Text = remainingAfter.ToString("N2") + " ريال";

        PreviewCoverText.Text = monthly > 0
            ? amount / monthly >= 0.995
                ? $"{Math.Round(amount / monthly, 1):0.#} قسط"
                : $"{amount:N2} ريال من قسط"
            : amount.ToString("N2") + " ريال";

        var period = SafePeriod(contract);
        var paidSoFar = Math.Max(0, contract.MainTotalAmount - remainingBefore);
        var afterThisReceipt = paidSoFar + amount;

        var dueCount = 0;
        var lateAfter = 0;
        var today = DateTime.Today;

        for (var i = 0; i < period && monthly > 0; i++)
        {
            var dueDate = contract.ContractStartDate.AddMonths(i + 1);
            var isDue = dueDate <= today;

            if (isDue)
            {
                dueCount++;

                var afterFrac = Math.Clamp((afterThisReceipt - i * monthly) / monthly, 0, 1);
                if (afterFrac < 0.999)
                    lateAfter++;
            }
        }

        PreviewStateText.Text = remainingAfter <= 0.01
            ? "منتهي"
            : lateAfter > 0
                ? $"متأخر {lateAfter} قسط"
                : "منتظم";

        BuildInstallmentStrip(contract, paidSoFar, afterThisReceipt, dueCount);
    }

    private static readonly string[] ArabicMonths =
    {
        "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
        "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
    };

    // period is estimated from the contract's start date and equal monthly installments,
    // not a real per-month schedule (this app does not keep one) - close enough to show
    // progress, not exact accounting.
    private void BuildInstallmentStrip(ContractInstallment contract, double paidSoFar, double afterThisReceipt, int dueCount)
    {
        InstallmentStripPanel.Children.Clear();

        var monthly = contract.MonthlyInstallment;
        var period = SafePeriod(contract);

        DetailsDueText.Text = period > 0 && monthly > 0 ? $"مستحق حتى اليوم: {dueCount} أقساط" : "";

        if (monthly <= 0 || period <= 0)
            return;

        var today = DateTime.Today;

        for (var i = 0; i < period; i++)
        {
            var monthStart = contract.ContractStartDate.AddMonths(i);
            var dueDate = contract.ContractStartDate.AddMonths(i + 1);

            var paidFrac = Math.Clamp((paidSoFar - i * monthly) / monthly, 0, 1);
            var afterFrac = Math.Clamp((afterThisReceipt - i * monthly) / monthly, 0, 1);
            var addFrac = Math.Max(0, afterFrac - paidFrac);
            var isLate = dueDate <= today && afterFrac < 0.999;

            var label = ArabicMonths[((monthStart.Month - 1) % 12 + 12) % 12];

            InstallmentStripPanel.Children.Add(BuildMonthCell(paidFrac, addFrac, isLate, label));
        }
    }

    private static Border BuildMonthCell(double paidFrac, double addFrac, bool late, string label)
    {
        var emptyFrac = Math.Max(0, 1 - paidFrac - addFrac);

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition(emptyFrac, GridUnitType.Star));
        grid.RowDefinitions.Add(new RowDefinition(addFrac, GridUnitType.Star));
        grid.RowDefinitions.Add(new RowDefinition(paidFrac, GridUnitType.Star));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        var paidBar = new Border { Background = ThemeBrush("BrushOk") };
        Grid.SetRow(paidBar, 2);

        var addBar = new Border { Background = ThemeBrush("BrushGold") };
        Grid.SetRow(addBar, 1);

        var labelText = new TextBlock
        {
            Text = label,
            FontSize = 10,
            Foreground = ThemeBrush("BrushMuted"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0)
        };
        Grid.SetRow(labelText, 3);

        grid.Children.Add(paidBar);
        grid.Children.Add(addBar);
        grid.Children.Add(labelText);

        var cell = new Border
        {
            Width = 44,
            Height = 58,
            Padding = new Thickness(2),
            Child = grid,
            Classes = { "stripCell" }
        };

        if (late)
            cell.Classes.Add("late");

        return cell;
    }

    // Looks the color up the same way DynamicResource does at runtime, so it always
    // matches Main/Theme.axaml; a plain gray if the app resources are somehow missing.
    private static IBrush ThemeBrush(string key) =>
        Application.Current?.TryFindResource(key, out var value) == true && value is IBrush brush
            ? brush
            : Brushes.Gray;

    private ScreenAutoRefresh? _autoRefresh;
    private GridSearch<ReceiptInstallment>? _gridSearch;

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        Refresh();

        _ = SyncAsync();

    }

    private void Refresh()
    {
        LoadReceipt();
        LoadPaymentMethod();

        ReceiptNumBox.Text = _receiptsDB.GenerateReceiptNumber();
        ReceiptDate.Text = DateTime.Today.ToString("yyyy-MM-dd");
        ContractInfoText.Text = "";
        ContractInfoText.Foreground = Brushes.Black;

        ContractNumSearchBox.SelectedItem = null;
        ContractNumSearchBox.Text = "";
        ContractNumSearchBox.ItemsSource = _contractsDB.GetPickRows();
        AmountBox.Text = "";

        ResetContractSelection();
    }

    private void ReceiptsGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (ReceiptsGrid.SelectedItem is ReceiptInstallment receipt)
            new ReceiptWindowViewInstallment(receipt.Id, _supabaseService).Show();
    }

    private async Task<string?> PickSavePdfPathAsync(string receiptNumber)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel is null)
            return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "حفظ سند القبض (PDF)",
                SuggestedFileName = $"{receiptNumber}.pdf",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PDF")
                    {
                        Patterns = new[] { "*.pdf" }
                    }
                }
            });

        return file?.Path.LocalPath;
    }

    private async void Print_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not ReceiptInstallment r)
            return;

        var path = await PickSavePdfPathAsync(r.ReceiptNumber);

        if (string.IsNullOrWhiteSpace(path))
            return;

        _pdfService.GenerateReceiptPdf(r, path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
