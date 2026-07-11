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

public partial class ReceiptViewInstallment : UserControl
{
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly ReceiptServiceInstallment _receiptsDB;
    private readonly ContractServiceInstallment _contractsDB;
    private readonly PdfServiceInstallment _pdfService;
    private readonly SupabaseService _supabaseService;
    private readonly InstallmentSyncService _sync;

    private TextBox? _contractIdSearchBox;
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

        Refresh();

        _contractIdSearchBox = this.FindControl<TextBox>("ContractNumSearchBox");

        _sync = new InstallmentSyncService(_db, _supabaseService);
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
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping installment receipts cloud sync.");
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

        var contractNum = raw.StartsWith("Ic-", StringComparison.OrdinalIgnoreCase)
            ? raw
            : "Ic-" + raw;

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
            $"اسم العميل : {contract.CustomerName} | " +
            $"اسم المنتج : {contract.ProductName} | " +
            $"القسط الأساسي : {contract.MainTotalAmount} | " +
            $"المتبقي : {contract.CurrentTotalAmount} | " +
            $"القسط الشهري : {contract.MonthlyInstallment}";

        ContractInfoText.Foreground = Brushes.Green;
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

        ContractNumSearchBox.Text = "";
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