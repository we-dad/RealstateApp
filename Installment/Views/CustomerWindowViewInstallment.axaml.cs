using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class CustomerWindowViewInstallment : Window
{
    private readonly CustomerInstallment _customer;
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly CustomerServiceInstallment _customerDB;
    private readonly ContractServiceInstallment _contractDB;
    private readonly ReceiptServiceInstallment _receiptDB;
    private readonly PdfServiceInstallment _pdfServiceInstallment;
    private readonly SupabaseService _supabaseService;
    private readonly InstallmentSyncService _sync;
    
    public CustomerWindowViewInstallment(CustomerInstallment customer, SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        DataContext = new CustomerInstallment();

        _db.Initialize();
        _customerDB = new CustomerServiceInstallment(_db);
        _contractDB = new ContractServiceInstallment(_db);
        _receiptDB = new ReceiptServiceInstallment(_db);
        _pdfServiceInstallment = new PdfServiceInstallment();

        _customer = customer;
        
        CustomerContractsGrid.DoubleTapped += CustomerContractsGrid_DoubleTapped;
        CustomerReceiptsGrid.DoubleTapped += CustomerReceiptsGrid_DoubleTapped;

        Refresh();
        
        _sync = new InstallmentSyncService(_db, _supabaseService);
        _ = _sync.PushAllDirtyAsync();
    }
    

    private void Update_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var name = NameBox.Text?.Trim() ?? "";
            var phone = PhoneBox.Text?.Trim() ?? "";
            var identityNumber = IdentityNumberBox.Text?.Trim() ?? "";
            var address = AddressBox.Text?.Trim() ?? "";
            var job = JobBox.Text?.Trim() ?? "";

            var sponserName = SponserNameBox.Text?.Trim() ?? "";
            var sponserPhone = SponserPhoneBox.Text?.Trim() ?? "";
            var sponserIdentityNumber = SponserIdentityNumberBox.Text?.Trim() ?? "";
            var sponserAddress = SponserAddressBox.Text?.Trim() ?? "";
            var sponserJob = SponserJobBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
                return;

            _customerDB.Update(
                _customer.Id,
                name,
                identityNumber,
                phone,
                address,
                job,
                sponserName,
                sponserIdentityNumber,
                sponserPhone,
                sponserAddress,
                sponserJob
            );

            Refresh();

            _ = _sync.PushAllDirtyAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void Refresh()
    {
        var refreshedCustomer = _customerDB.GetById(_customer.Id);

        if (refreshedCustomer is null)
            return;

        NameBox.Text = refreshedCustomer.Name;
        PhoneBox.Text = refreshedCustomer.Phone;
        IdentityNumberBox.Text = refreshedCustomer.IdentityNumber;
        AddressBox.Text = refreshedCustomer.Address;
        JobBox.Text = refreshedCustomer.Job;

        BoolSponserBox.IsChecked = refreshedCustomer.BoolSponser;

        SponserNameBox.Text = refreshedCustomer.SponserName;
        SponserPhoneBox.Text = refreshedCustomer.SponserPhone;
        SponserIdentityNumberBox.Text = refreshedCustomer.SponserIdentityNumber;
        SponserAddressBox.Text = refreshedCustomer.SponserAddress;
        SponserJobBox.Text = refreshedCustomer.SponserJob;

        ResultNameBox.Text = refreshedCustomer.Name;
        ResultPhoneBox.Text = refreshedCustomer.Phone;
        ResultIdentityNumberBox.Text = refreshedCustomer.IdentityNumber;
        ResultAddressBox.Text = refreshedCustomer.Address;
        ResultJobBox.Text = refreshedCustomer.Job;

        ResultSponserNameBox.Text = refreshedCustomer.SponserName;
        ResultSponserPhoneBox.Text = refreshedCustomer.SponserPhone;
        ResultSponserIdentityNumberBox.Text = refreshedCustomer.SponserIdentityNumber;
        ResultSponserAddressBox.Text = refreshedCustomer.SponserAddress;
        ResultSponserJobBox.Text = refreshedCustomer.SponserJob;
        
        LoadCustomerRelatedData();
    }
    
    private void LoadCustomerRelatedData()
    {
        var rows = _customerDB.GetCustomerRelatedData(_customer.Id);

        CustomerContractsGrid.ItemsSource = rows
            .Where(x => x.ContractId > 0)
            .GroupBy(x => x.ContractId)
            .Select(x => x.First())
            .ToList();

        CustomerReceiptsGrid.ItemsSource = rows
            .Where(x => x.ReceiptId > 0)
            .ToList();

        var summary = rows.FirstOrDefault();

        TotalAmountText.Text = summary?.TotalAmount.ToString() ?? "0";
        PaidAmountText.Text = summary?.PaidAmount.ToString() ?? "0";
        LeftAmountText.Text = summary?.LeftAmount.ToString() ?? "0";
        ReceiptsCountText.Text = summary?.ReceiptsCount.ToString() ?? "0";

        PaymentProgressBar.Value = summary?.PaymentProgressPercent ?? 0;

        PaymentProgressText.Text =
            $"المدفوع: {summary?.PaidInstallments ?? 0} / {summary?.TotalInstallments ?? 0} | المتبقي: {summary?.LeftInstallments ?? 0}";
    }
    
    private void OpenContract_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not CustomerInstallment row) return;
        
        if (row.ContractId <= 0)
            return;
        
        var window = new ContractWindowViewInstallment(row.ContractId, _supabaseService);

        window.Show();
    }
    
    private void OpenReceipt_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not CustomerInstallment row) return;

        if (row.ReceiptId <= 0)
            return;

        var window = new ReceiptWindowViewInstallment(row.ReceiptId, _supabaseService);

        window.Show();
    }
    private async Task<string?> PickSavePdfPathAsync(string fileName)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel is null)
            return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "حفظ السجل المالي PDF",
                SuggestedFileName = $"{fileName}.pdf",
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
    
    private async void PrintFinancialRecord_Click(object? sender, RoutedEventArgs e)
    {
        var customer = _customerDB.GetById(_customer.Id);
        if (customer == null) return;

        var rows = _customerDB.GetCustomerRelatedData(_customer.Id);

        if (rows.Count == 0)
        {
            await ShowMessageAsync("تنبيه", "لا توجد عقود أو سندات لهذا العميل");
            return;
        }

        var path = await PickSavePdfPathAsync($"Financial_Record_{customer.Name}");

        if (string.IsNullOrWhiteSpace(path))
            return;

        _pdfServiceInstallment.GenerateCustomerFinancialRecordPdf(
            customer,
            rows,
            path
        );

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
    
    private async void CustomerContractsGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (CustomerContractsGrid.SelectedItem is not CustomerInstallment row) return;
        if (row.ContractId <= 0) return;

        var window = new ContractWindowViewInstallment(row.ContractId, _supabaseService);
        await window.ShowDialog(this);

        Refresh();
    }

    private async void CustomerReceiptsGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (CustomerReceiptsGrid.SelectedItem is not CustomerInstallment row) return;
        if (row.ReceiptId <= 0) return;

        var window = new ReceiptWindowViewInstallment(row.ReceiptId, _supabaseService);
        await window.ShowDialog(this);

        Refresh();
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (_customer is null)
            return;

        try
        {
            _customerDB.Delete(_customer.Id);

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