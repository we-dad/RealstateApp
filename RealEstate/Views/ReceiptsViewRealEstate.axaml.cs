using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class ReceiptsViewRealEstate : UserControl
{
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly ReceiptServiceRealEstate _receiptsDB;
    private readonly ContractServiceRealEstate _contractsDB;
    private readonly PdfServiceRealEstate _pdfServiceRealEstate;
    private readonly SupabaseService _supabaseService;
    private readonly RealEstateSyncService _sync;

    private TextBox? _contractIdSearchBox;
    private ContractRealEstate? _selectedContract;

    public ReceiptsViewRealEstate(SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();

        _receiptsDB = new ReceiptServiceRealEstate(_db);
        _contractsDB = new ContractServiceRealEstate(_db);
        _pdfServiceRealEstate = new PdfServiceRealEstate();
        _sync = new RealEstateSyncService(_db, _supabaseService);

        Refresh();

        _contractIdSearchBox = this.FindControl<TextBox>("ContractNumSearchBox");

        _ = _sync.PushAllDirtyAsync();
        _ = SyncReceiptsFromCloudAsync();
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

    private void LoadReceipt()
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
                ContractInfoText.Text = "يرجى اختيار عقد أولاً";
                ContractInfoText.Foreground = Brushes.Red;
                return;
            }

            var contract = _selectedContract;
            var paymentMethod = PaymentMethodBox.SelectedItem as string ?? "كاش";

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

    private void SearchContract_Click(object? sender, RoutedEventArgs e)
    {
        var raw = _contractIdSearchBox?.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(raw))
            return;
        
        var contractNum = raw.StartsWith("Rc-", StringComparison.OrdinalIgnoreCase)
            ? raw
            : "Rc-" + raw;

        var contract = _contractsDB.FindByContractNum(contractNum);

        if (contract is null)
        {
            _selectedContract = null;
            ContractInfoText.Text = "لم يتم العثور على عقد بهذا الرقم";
            ContractInfoText.Foreground = Brushes.Red;
            return;
        }

        _selectedContract = contract;

        ContractInfoText.Text =
            $"اسم المستأجر : {contract.TenantName} | اسم الوحدة : {contract.UnitName}";

        ContractInfoText.Foreground = Brushes.Green;
    }

    private async Task SyncReceiptsFromCloudAsync()
    {
        try
        {
            var cloudReceipts = new CloudReceiptsRealEstateService(_supabaseService);
            var cloudRows = await cloudReceipts.GetReceiptsAsync();

            foreach (var row in cloudRows)
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
                    row.Amount
                );
            }

            LoadReceipt();
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping receipts cloud sync.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        Refresh();

        _ = _sync.PushAllDirtyAsync();
        _ = SyncReceiptsFromCloudAsync();
    }

    private void Refresh()
    {
        LoadReceipt();
        LoadPaymentMethod();

        ReceiptNumBox.Text = _receiptsDB.GenerateReceiptNumber();
        ReceiptDate.Text = DateTime.Today.ToString("yyyy-MM-dd");
        ContractInfoText.Text = "";
        ContractInfoText.Foreground = Brushes.Black;

        AmountBox.Text = "";
        ContractNumSearchBox.Text = "";

        _selectedContract = null;
    }

    private void OpenInfoWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ReceiptRealEstate receipt)
            new ReceiptsWindowViewRealEstate(receipt.Id, _supabaseService).Show();
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
        if (sender is not Button btn || btn.Tag is not ReceiptRealEstate r)
            return;

        var path = await PickSavePdfPathAsync(r.ReceiptNumber);

        if (string.IsNullOrWhiteSpace(path))
            return;

        _pdfServiceRealEstate.GenerateReceiptPdf(r, path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}