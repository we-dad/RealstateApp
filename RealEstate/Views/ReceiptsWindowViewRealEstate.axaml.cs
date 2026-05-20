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

public partial class ReceiptsWindowViewRealEstate : Window
{
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly ReceiptServiceRealEstate _receiptsDB;
    private readonly ContractServiceRealEstate _contractsDB;
    private readonly PdfServiceRealEstate _pdfServiceRealEstate;
    private readonly SupabaseService _supabaseService;
    private readonly RealEstateSyncService _sync;

    private ReceiptRealEstate? _receipt;
    private readonly long _receiptID;
    private TextBox? _contractIdSearchBox;
    private ContractRealEstate? _selectedContract;

    public ReceiptsWindowViewRealEstate(long receiptID, SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();

        _receiptsDB = new ReceiptServiceRealEstate(_db);
        _contractsDB = new ContractServiceRealEstate(_db);
        _pdfServiceRealEstate = new PdfServiceRealEstate();
        _sync = new RealEstateSyncService(_db, _supabaseService);

        _receiptID = receiptID;

        Refresh();

        _contractIdSearchBox = this.FindControl<TextBox>("ContractNumSearchBox");
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
            if (_receipt is null)
                return;

            var receiptDate = ReceiptDate.SelectedDate?.LocalDateTime ?? DateTime.Today;

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

            _receiptsDB.Update(
                _receiptID,
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
        var contractNum = _contractIdSearchBox?.Text?.Trim() ?? "";

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

        ContractInfoText.Text =
            $"اسم المستأجر : {contract.TenantName} | اسم الوحدة : {contract.UnitName}";

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
        
        ContractDataGrid.ItemsSource = new List<ContractRealEstate> { contract };
        UnitDataGrid.ItemsSource = new List<ContractRealEstate> { contract };
        OwnerDataGrid.ItemsSource = new List<ContractRealEstate> { contract };
        TenantDataGrid.ItemsSource = new List<ContractRealEstate> { contract };

        LoadPaymentMethod();

        ReceiptNumBox.Text = _receipt.ReceiptNumber;
        ReceiptDate.SelectedDate = _receipt.ReceiptDate;
        ContractNumSearchBox.Text = contract.ContractNumber;

        ContractInfoText.Text =
            $"اسم المستأجر : {contract.TenantName} | اسم الوحدة : {contract.UnitName}";
        ContractInfoText.Foreground = Brushes.Green;

        AmountBox.Text = _receipt.Amount.ToString(CultureInfo.InvariantCulture);
        PaymentMethodBox.SelectedItem = _receipt.PaymentMethod;

        ResultReceiptNumBox.Text = _receipt.ReceiptNumber;
        ResultReceiptDateBox.Text = _receipt.ReceiptDate.ToString("yyyy-MM-dd");
        ResultAmountBox.Text = _receipt.Amount.ToString(CultureInfo.InvariantCulture);
        ResultPaymentMethodBox.Text = _receipt.PaymentMethod;
        
    }
    
    private void ContractDataGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (_selectedContract is null) return;

        var window = new ContractsWindowViewRealEstate(_selectedContract.Id, _supabaseService);
        window.Show();
    }

    private void UnitDataGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (_selectedContract is null) return;

        var window = new UnitsWindowViewRealEstate(_selectedContract.UnitId, _supabaseService);
        window.Show();
    }

    private void OwnerDataGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (_selectedContract is null) return;

        var owner = new OwnerRealEstate { Id = _selectedContract.OwnerId };
        var window = new OwnersWindowViewRealEstate(owner, _supabaseService);
        window.Show();
    }

    private void TenantDataGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (_selectedContract is null) return;

        var tenant = new TenantRealEstate { Id = _selectedContract.TenantId };
        var window = new TenantsWindowViewRealEstate(tenant, _supabaseService);
        window.Show();
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
        if (_receipt is null)
            return;

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