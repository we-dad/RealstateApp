using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using RealEstateApp.Models;
using RealEstateApp.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

namespace RealEstateApp.Views;

public partial class ReceiptWindowViewInstallment : Window
{
    private readonly InstallmentDbService _db = new InstallmentDbService();
    private readonly ReceiptServiceInstallment _receiptsDB;
    private readonly ContractServiceInstallment _contractsDB;
    private PdfServiceInstallment _pdfService;

    private ReceiptInstallment? _receipt;
    private long _receiptID;
    private TextBox? _contractIdSearchBox;
    private ContractInstallment? _selectedContract;


    public ReceiptWindowViewInstallment(long receiptID)
    {
        InitializeComponent();

        _db.Initialize();

        _receiptsDB = new ReceiptServiceInstallment(_db);
        _contractsDB = new ContractServiceInstallment(_db);
        _pdfService = new PdfServiceInstallment();

        _receiptID = receiptID;

        Refresh();

        _contractIdSearchBox = this.FindControl<TextBox>("ContractNumSearchBox"); //this line becasue avalonia can't found TenantIdSearchBox it's returen null maybe because the warning message

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
            var _ReceiptNum = ReceiptNumBox.Text ?? "";
            var _ReceiptDate = ReceiptDate.SelectedDate?.LocalDateTime
                     ?? DateTime.Today;

            if (_selectedContract == null)
            {
                ContractInfoText.Text = "يرجى اختيار العقد أولاً";
                return;
            }
            var _contractNum = _selectedContract.Id;

            var _paymentMethod = PaymentMethodBox.SelectedItem as string ?? "سكني";

            var Amount = double.Parse(
                AmountBox.Text?.Trim() ?? "",
                CultureInfo.InvariantCulture
            );

            _receiptsDB.Update(_receiptID, _ReceiptNum, _ReceiptDate, _contractNum, _paymentMethod, Amount);

            Refresh();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void SearchContract_Click(object? sender, RoutedEventArgs e)
    {
        var contractNum = "Ic-"+_contractIdSearchBox?.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(contractNum))
            return;

        var contract = _contractsDB.FindByContractNum(contractNum);

        if (contract is null)
        {
            _selectedContract = null;
            ContractInfoText.Text = "لم يتم العثور على عقد بهذا الرقم";
            ContractInfoText.Foreground = Brushes.Red;
            return;
        }

        _selectedContract = contract;
        var _contracttInfo = $"اسم العميل : {contract.CustomerName} | ";
        var _productName = $"اسم المنتج : {contract.ProductName} | ";
        var _mainTotalAmout = $"القسط الأساسي : {contract.MainTotalAmount} | ";
        var _totalAmout = $"المتبقي : {contract.CurrentTotalAmount} | ";
        var _monthlyInstallment = $"القسط الشهري : {contract.MonthlyInstallment}";
        ContractInfoText.Text = _contracttInfo + _productName + _mainTotalAmout + _totalAmout + _monthlyInstallment;
        ContractInfoText.Foreground = Brushes.Green;
    }
    private void Refresh()
    {
        _receipt = _receiptsDB.GetById(_receiptID);
        if (_receipt is null) return;

        var _contract = _contractsDB.GetById(_receipt.ContractId);
        if (_contract is null) return;

        LoadPaymentMethod();

        //Receipt Field Info
        ReceiptNumBox.Text = _receipt.ReceiptNumber;
        ReceiptDate.SelectedDate = _receipt.ReceiptDate;
        ContractNumSearchBox.Text = _contract.ContractNumber.StartsWith("Ic-")
            ? _contract.ContractNumber[3..]
            : _contract.ContractNumber;
        ContractInfoText.Text = "";
        AmountBox.Text = _receipt.Amount.ToString();
        PaymentMethodBox.SelectedItem = _receipt.PaymentMethod;

        //Receipt Info
        ResultReceiptNumBox.Text = _receipt.ReceiptNumber;
        ResultReceiptDateBox.Text = _receipt.ReceiptDate.ToString("yyyy-MM-dd");
        ResultAmountBox.Text = _receipt.Amount.ToString();
        ResultPaymentMethodBox.Text = _receipt.PaymentMethod;
        ResultCurrentTotalAmountBox.Text = _receipt.CurrentTotalAmount.ToString();

        ResultContractNumBox.Text = _contract.ContractNumber;
        ResultContractDateStartBox.Text = _contract.ContractStartDate.ToString("yyyy-MM-dd");
        ResultContractDateEndBox.Text = _contract.ContractEndDate.ToString("yyyy-MM-dd");
        ResultProductTotalAmount.Text = _contract.MainTotalAmount.ToString("0.##");
        ResultInterestPercentBox.Text = _contract.InterestPercent.ToString("0.##");
        ResultContractPeriodBox.Text = _contract.ContractPeriod.ToString("0.##");
        ResultDownPaymentBox.Text = _contract.DownPayment.ToString("0.##");
        ResultManagementFeeBox.Text = _contract.ManagementFee.ToString("0.##");
        ResultMonthlyInstallmentBox.Text = _contract.MonthlyInstallment.ToString("0.##");

        ResultProductNameBox.Text = _contract.ProductName;
        ResultProductTypeBox.Text = _contract.ProductType;
        ResultProductMainPriceBox.Text = _contract.ProductMainPrice.ToString("0.##");

        ResultOwnerNameBox.Text = _contract.OwnerName;
        ResultOwnerIdentityNumberBox.Text = _contract.OwnerIdentityNumber;
        ResultOwnerPhoneBox.Text = _contract.OwnerPhone;
        ResultOwnerAddressBox.Text = _contract.OwnerAddress;

        ResultNameBox.Text = _contract.CustomerName;
        ResultIdentityNumberBox.Text = _contract.CustomerIdentityNumber;
        ResultPhoneBox.Text = _contract.CustomerPhone;
        ResultAddressBox.Text = _contract.CustomerAddress;
        ResultJobBox.Text = _contract.CustomerJob;
        
        SponsorSection.IsVisible =
            !string.IsNullOrWhiteSpace(_contract.CustomerSponserName);
        
        
        ResultSponserNameBox.Text = _contract.CustomerSponserName;
        ResultSponserIdentityNumberBox.Text = _contract.CustomerSponserIdentityNumber;
        ResultSponserPhoneBox.Text = _contract.CustomerSponserPhone;
        ResultSponserAddressBox.Text = _contract.CustomerSponserAddress;
        ResultSponserJobBox.Text = _contract.CustomerSponserJob;
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
        if (_receipt is null) return;

        var path = await PickSavePdfPathAsync(_receipt.ReceiptNumber);
        if (string.IsNullOrWhiteSpace(path))
            return;

        _pdfService.GenerateReceiptPdf(_receipt, path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private async void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (_receipt is null) return;
        try
        {
            _receiptsDB.Delete(_receipt.Id);
            Close();
        }
        catch (InvalidOperationException ex)
        {
            await ShowMessageAsync("تنبيه", ex.Message);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("خطأ", ex.Message);
        }
    }
    private async System.Threading.Tasks.Task ShowMessageAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var ok = new Button { Content = "موافق", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };

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
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                TextAlignment = Avalonia.Media.TextAlignment.Center
            },
            ok
        }
        };

        await dialog.ShowDialog(this);
    }
}
