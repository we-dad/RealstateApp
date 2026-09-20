using Avalonia.Controls;
using Avalonia.Interactivity;
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
        ScreenAutoRefresh.ReloadKeepingSelection<ReceiptInstallment>(ReceiptsGrid, r => r.Id, LoadReceiptCore);

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
            _selectedContract = null;
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

        _selectedContract = null;
        ContractInfoText.Text = "";
    }

    private void ShowSelectedContract(ContractInstallment contract)
    {
        _selectedContract = contract;

        ContractInfoText.Text =
            $"اسم العميل : {contract.CustomerName} | " +
            $"اسم المنتج : {contract.ProductName} | " +
            $"القسط الأساسي : {contract.MainTotalAmount} | " +
            $"المتبقي : {contract.CurrentTotalAmount} | " +
            $"القسط الشهري : {contract.MonthlyInstallment}";

        ContractInfoText.Foreground = Brushes.Green;
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
            _selectedContract = null;
            ContractInfoText.Text = candidates.Count > 1
                ? "يوجد أكثر من عقد بهذا الرقم، اكتب الرقم كاملًا: " + string.Join("، ", candidates)
                : "لم يتم العثور على عقد بهذا الرقم";
            ContractInfoText.Foreground = Brushes.Red;
            return;
        }

        ShowSelectedContract(contract);
    }

    private ScreenAutoRefresh? _autoRefresh;

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

        _selectedContract = null;
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