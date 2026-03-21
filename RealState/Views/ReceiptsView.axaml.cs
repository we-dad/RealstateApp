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

public partial class ReceiptsView : UserControl
{
    private readonly DbService _db = new DbService();
    private readonly ReceiptService _receiptsDB;
    private readonly ContractService _contractsDB;
    private PdfService _pdfService;
    private TextBox? _contractIdSearchBox;
    private Contract? _selectedContract;


    public ReceiptsView()
    {
        InitializeComponent();

        _db.Initialize();

        _receiptsDB = new ReceiptService(_db);
        _contractsDB = new ContractService(_db);
        _pdfService = new PdfService();

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

    private void LoadReceipt()
    {

        var data = _receiptsDB.GetAll();

        ReceiptsGrid.ItemsSource = null;
        ReceiptsGrid.ItemsSource = data;
    }
    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var _ReceiptNum = ReceiptNumBox.Text ?? "";
            var _ReceiptDate = DateTime.Today;

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

            _receiptsDB.Add(_ReceiptNum, _ReceiptDate, _contractNum, _paymentMethod, Amount);

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
        // contract.TenantName + " :رقم الهوية | " + contract.UnitName + " :اسم المستأجر";
    }
    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        Refresh();
    }
    private void Refresh()
    {

        LoadReceipt();
        LoadPaymentMethod();

        ReceiptNumBox.Text = _receiptsDB.GenerateReceiptNumber();
        ReceiptDate.Text = DateTime.Today.ToString("yyyy-MM-dd");
        ContractInfoText.Text = "";

        AmountBox.Text = "";

    }
    private void OpenInfoWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Receipt receipt)
            new ReceiptsWindowView(receipt.Id).Show();
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
        if (sender is not Button btn || btn.Tag is not Receipt r) return;

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
