using Avalonia.Controls;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia;
using Avalonia.Platform.Storage;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class TenantsWindowViewRealEstate : Window
{
    private readonly TenantRealEstate _tenantRealEstate;
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly PdfServiceRealEstate _pdfServiceRealEstate;
    private readonly TenantServiceRealEstate _tenantDB;
    private readonly ContractServiceRealEstate _contractDB;
    private readonly ReceiptServiceRealEstate _receiptDB;
    private readonly SupabaseService _supabaseService;
    private readonly RealEstateSyncService _sync;

    public TenantsWindowViewRealEstate(TenantRealEstate tenantRealEstate, SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();
        _tenantDB = new TenantServiceRealEstate(_db);
        _sync = new RealEstateSyncService(_db, _supabaseService);
        _contractDB = new ContractServiceRealEstate(_db);
        _receiptDB = new ReceiptServiceRealEstate(_db);
        _pdfServiceRealEstate = new PdfServiceRealEstate();

        _tenantRealEstate = tenantRealEstate;

        Refresh();
    }

    private void Update_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var name = NameBox.Text?.Trim() ?? "";
            var phone = PhoneBox.Text?.Trim() ?? "";
            var identityNumber = IdentityNumberBox.Text?.Trim() ?? "";
            var address = AddressBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
                return;

            _tenantDB.Update(_tenantRealEstate.Id, name, identityNumber, phone, address);

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
        var refreshTenant = _tenantDB.GetById(_tenantRealEstate.Id);
        if (refreshTenant is null) return;

        NameBox.Text = refreshTenant.Name;
        PhoneBox.Text = refreshTenant.Phone;
        IdentityNumberBox.Text = refreshTenant.IdentityNumber;
        AddressBox.Text = refreshTenant.Address;

        ResultNameBox.Text = refreshTenant.Name;
        ResultPhoneBox.Text = refreshTenant.Phone;
        ResultIdentityNumberBox.Text = refreshTenant.IdentityNumber;
        ResultAddressBox.Text = refreshTenant.Address;

        LoadTenantRelatedData();
    }
    
    private void LoadTenantRelatedData()
    {
        var rows = _tenantDB.GetTenantRelatedData(_tenantRealEstate.Id);

        TenantContractsGrid.ItemsSource = rows
            .Where(x => x.ContractId > 0)
            .GroupBy(x => x.ContractId)
            .Select(x => x.First())
            .ToList();

        TenantReceiptsGrid.ItemsSource = rows
            .Where(x => x.ReceiptId > 0)
            .ToList();
    }
  
    
    private void TenantContractsGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (TenantContractsGrid.SelectedItem is not ContractRealEstate contract)
            return;

        var window = new ContractsWindowViewRealEstate(contract.Id, _supabaseService);
        window.Show();
    }

    private void TenantReceiptsGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (TenantReceiptsGrid.SelectedItem is not ReceiptRealEstate receipt)
            return;

        var window = new ReceiptsWindowViewRealEstate(receipt.Id, _supabaseService);
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
    
    private async void PrintAllReceipts_Click(object? sender, RoutedEventArgs e)
    {
        var tenant = _tenantDB.GetById(_tenantRealEstate.Id);
        if (tenant == null) return;

        var receipts = _tenantDB.GetTenantRelatedData(_tenantRealEstate.Id)
            .Where(x => x.ReceiptId > 0)
            .GroupBy(x => x.ReceiptId)
            .Select(x => x.First())
            .ToList();

        if (receipts.Count == 0)
        {
            await ShowMessageAsync("تنبيه", "لا توجد سندات قبض لهذا المستأجر");
            return;
        }

        var path = await PickSavePdfPathAsync($"Financial_Record_{tenant.Name}");

        if (string.IsNullOrWhiteSpace(path))
            return;

        _pdfServiceRealEstate.GenerateTenantReceiptsRecordPdf(tenant, receipts, path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (_tenantRealEstate is null) return;

        try
        {
            _tenantDB.Delete(_tenantRealEstate.Id);

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
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    TextAlignment = Avalonia.Media.TextAlignment.Center
                },
                ok
            }
        };

        await dialog.ShowDialog(this);
    }
}