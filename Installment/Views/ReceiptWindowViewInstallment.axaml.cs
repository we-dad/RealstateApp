using Avalonia;
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

public partial class ReceiptWindowViewInstallment : Window
{
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly ReceiptServiceInstallment _receiptsDB;
    private readonly ContractServiceInstallment _contractsDB;
    private readonly PdfServiceInstallment _pdfService;
    private readonly SupabaseService _supabaseService;
    private readonly InstallmentSyncService _sync;

    private ReceiptInstallment? _receipt;
    private readonly long _receiptID;
    private TextBox? _contractIdSearchBox;
    private ContractInstallment? _selectedContract;

    public ReceiptWindowViewInstallment(long receiptID, SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();

        _receiptsDB = new ReceiptServiceInstallment(_db);
        _contractsDB = new ContractServiceInstallment(_db);
        _pdfService = new PdfServiceInstallment();

        _receiptID = receiptID;

        Refresh();

        _contractIdSearchBox = this.FindControl<TextBox>("ContractNumSearchBox");
        
        _sync = new InstallmentSyncService(_db, _supabaseService);
        _ = _sync.PushAllDirtyAsync();
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

    
    private void Update_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var receiptNum = ReceiptNumBox.Text?.Trim() ?? "";
            var receiptDate = ReceiptDate.SelectedDate?.LocalDateTime ?? DateTime.Today;

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

            _receiptsDB.Update(
                _receiptID,
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
            ContractInfoText.Text = ex.Message;
            ContractInfoText.Foreground = Brushes.Red;
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

    private void Refresh()
    {
        _receipt = _receiptsDB.GetById(_receiptID);

        if (_receipt is null)
            return;

        var contract = _contractsDB.GetById(_receipt.ContractId);

        if (contract is null)
            return;

        _selectedContract = contract;

        LoadPaymentMethod();

        ReceiptNumBox.Text = _receipt.ReceiptNumber;
        ReceiptDate.SelectedDate = _receipt.ReceiptDate;

        ContractNumSearchBox.Text = contract.ContractNumber.StartsWith("Ic-")
            ? contract.ContractNumber[3..]
            : contract.ContractNumber;

        ContractInfoText.Text =
            $"اسم العميل : {contract.CustomerName} | " +
            $"اسم المنتج : {contract.ProductName} | " +
            $"القسط الأساسي : {contract.MainTotalAmount} | " +
            $"المتبقي : {contract.CurrentTotalAmount} | " +
            $"القسط الشهري : {contract.MonthlyInstallment}";
        ContractInfoText.Foreground = Brushes.Green;

        AmountBox.Text = _receipt.Amount.ToString(CultureInfo.InvariantCulture);
        PaymentMethodBox.SelectedItem = _receipt.PaymentMethod;

        ResultReceiptNumBox.Text = _receipt.ReceiptNumber;
        ResultReceiptDateBox.Text = _receipt.ReceiptDate.ToString("yyyy-MM-dd");
        ResultAmountBox.Text = _receipt.Amount.ToString(CultureInfo.InvariantCulture);
        ResultPaymentMethodBox.Text = _receipt.PaymentMethod;
        ResultCurrentTotalAmountBox.Text = _receipt.CurrentTotalAmount.ToString(CultureInfo.InvariantCulture);

        ResultContractNumBox.Text = contract.ContractNumber;
        ResultContractDateStartBox.Text = contract.ContractStartDate.ToString("yyyy-MM-dd");
        ResultContractDateEndBox.Text = contract.ContractEndDate.ToString("yyyy-MM-dd");
        ResultProductTotalAmount.Text = contract.MainTotalAmount.ToString("0.##");
        ResultInterestPercentBox.Text = contract.InterestPercent.ToString("0.##");
        ResultContractPeriodBox.Text = contract.ContractPeriod.ToString("0.##");
        ResultDownPaymentBox.Text = contract.DownPayment.ToString("0.##");
        ResultManagementFeeBox.Text = contract.ManagementFee.ToString("0.##");
        ResultMonthlyInstallmentBox.Text = contract.MonthlyInstallment.ToString("0.##");

        ResultProductNameBox.Text = contract.ProductName;
        ResultProductTypeBox.Text = contract.ProductType;
        ResultProductMainPriceBox.Text = contract.ProductMainPrice.ToString("0.##");

        ResultOwnerNameBox.Text = contract.OwnerName;
        ResultOwnerIdentityNumberBox.Text = contract.OwnerIdentityNumber;
        ResultOwnerPhoneBox.Text = contract.OwnerPhone;
        ResultOwnerAddressBox.Text = contract.OwnerAddress;

        ResultNameBox.Text = contract.CustomerName;
        ResultIdentityNumberBox.Text = contract.CustomerIdentityNumber;
        ResultPhoneBox.Text = contract.CustomerPhone;
        ResultAddressBox.Text = contract.CustomerAddress;
        ResultJobBox.Text = contract.CustomerJob;

        SponsorSection.IsVisible =
            !string.IsNullOrWhiteSpace(contract.CustomerSponserName);

        ResultSponserNameBox.Text = contract.CustomerSponserName;
        ResultSponserIdentityNumberBox.Text = contract.CustomerSponserIdentityNumber;
        ResultSponserPhoneBox.Text = contract.CustomerSponserPhone;
        ResultSponserAddressBox.Text = contract.CustomerSponserAddress;
        ResultSponserJobBox.Text = contract.CustomerSponserJob;
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
        var freshReceipt = _receiptsDB.GetById(_receiptID);

        if (freshReceipt is null)
            return;

        var path = await PickSavePdfPathAsync(freshReceipt.ReceiptNumber);

        if (string.IsNullOrWhiteSpace(path))
            return;

        _pdfService.GenerateReceiptPdf(freshReceipt, path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (_receipt is null)
            return;

        try
        {
            _receiptsDB.Delete(_receipt.Id);

            _ = _sync.PushAllDirtyAsync();
            
            Close();
        }
        catch (InvalidOperationException ex)
        {
            _ = ShowMessageAsync("تنبيه", ex.Message);
        }
        catch (Exception ex)
        {
            _ = ShowMessageAsync("خطأ", ex.Message);
        }
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var ok = new Button
        {
            Content = "موافق",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        ok.Click += (_, __) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center
                },
                ok
            }
        };

        await dialog.ShowDialog(this);
    }
}