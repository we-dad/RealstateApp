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
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class ReceiptsWindowViewRealEstate : Window
{
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly ReceiptServiceRealEstate _receiptsDB;
    private readonly ContractServiceRealEstate _contractsDB;
    private PdfServiceRealEstate _pdfServiceRealEstate;

    private ReceiptRealEstate? _receipt;
    private long _receiptID;
    private TextBox? _contractIdSearchBox;
    private ContractRealEstate? _selectedContract;


    public ReceiptsWindowViewRealEstate(long receiptID)
    {
        InitializeComponent();

        _db.Initialize();

        _receiptsDB = new ReceiptServiceRealEstate(_db);
        _contractsDB = new ContractServiceRealEstate(_db);
        _pdfServiceRealEstate = new PdfServiceRealEstate();

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
                ContractInfoText.Text = "يرجى اختيار مستأجر أولاً";
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
        var contractNum = _contractIdSearchBox?.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(contractNum))
            return;

        var contract = _contractsDB.FindByContractNum(contractNum);

        if (contract is null)
        {
            _selectedContract = null;
            ContractInfoText.Text = "لم يتم العثور على مستأجر بهذا الرقم";
            ContractInfoText.Foreground = Brushes.Red;
            return;
        }

        _selectedContract = contract;
        var _tenantInfo = $"اسم المستأجر : {contract.TenantName} | ";
        var _UnitName = $"اسم الوحدة : {contract.UnitName}";
        ContractInfoText.Text = _tenantInfo + _UnitName;
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
        ContractNumSearchBox.Text = _contract.ContractNumber;
        ContractInfoText.Text = "";
        AmountBox.Text = _receipt.Amount.ToString();
        PaymentMethodBox.SelectedItem = _receipt.PaymentMethod;

        //Receipt Info
        ResultReceiptNumBox.Text = _receipt.ReceiptNumber;
        ResultReceiptDateBox.Text = _receipt.ReceiptDate.ToString("yyyy-MM-dd");
        ResultAmountBox.Text = _receipt.Amount.ToString();
        ResultPaymentMethodBox.Text = _receipt.PaymentMethod;

        //Contract Info
        ResultContractNumBox.Text = _contract.ContractNumber;
        ResultContractDateStartBox.Text = _contract.ContractStartDate.ToString("yyyy-MM-dd");
        ResultContractDateEndBox.Text = _contract.ContractEndDate.ToString("yyyy-MM-dd");
        ResultRentAmountBox.Text = _contract.RentAmount.ToString();

        //Unit Info
        ResultUnitNameBox.Text = _contract.UnitName;
        ResultDistrictBox.Text = _contract.District;
        ResultCityBox.Text = _contract.City;
        ResultUnitTypeBox.Text = _contract.UnitType;
        ResultUnitNumBox.Text = _contract.UnitNum.ToString() + " / " + _contract.UnitsCount.ToString();

        //Owner Info
        ResultOwnerNameBox.Text = _contract.OwnerName;
        ResultOwnerIdentityNumberBox.Text = _contract.OwnerIdentityNumber;
        ResultOwnerPhoneBox.Text = _contract.OwnerPhone;
        ResultOwnerAddressBox.Text = _contract.OwnerAddress;

        //Tenant Info
        ResultTenantNameBox.Text = _contract.TenantName;
        ResultTenantIdentityNumberBox.Text = _contract.TenantIdentityNumber;
        ResultTenantPhoneBox.Text = _contract.TenantPhone;
        ResultTenantAddressBox.Text = _contract.TenantAddress;
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

        _pdfServiceRealEstate.GenerateReceiptPdf(_receipt, path);

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
