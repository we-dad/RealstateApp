using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class CustomerViewInstallment : UserControl
{
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly CustomerServiceInstallment _customerService;
    private CustomerInstallment _customer = new CustomerInstallment();
    private readonly SupabaseService _supabaseService;
    private readonly InstallmentSyncService _sync;

    public CustomerViewInstallment(SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        DataContext = _customer;

        _db.Initialize();
        _customerService = new CustomerServiceInstallment(_db);

        LoadCustomer();

    _sync = new InstallmentSyncService(_db, _supabaseService);
    _ = _sync.PushAllDirtyAsync();
    _ = SyncCustomersFromCloudAsync();
    
    CustomerGrid.DoubleTapped += CustomerGrid_DoubleTapped;
    
    }

    private void LoadCustomer()
    {
        var data = _customerService.GetAll();

        CustomerGrid.ItemsSource = null;
        CustomerGrid.ItemsSource = data;
    }
    
    private void Add_Click(object? sender, RoutedEventArgs e)
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

            _customerService.Add(
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

            NameBox.Text = "";
            IdentityNumberBox.Text = "";
            PhoneBox.Text = "";
            AddressBox.Text = "";
            JobBox.Text = "";

            SponserNameBox.Text = "";
            SponserIdentityNumberBox.Text = "";
            SponserPhoneBox.Text = "";
            SponserAddressBox.Text = "";
            SponserJobBox.Text = "";

            LoadCustomer();


    _ = _sync.PushAllDirtyAsync();
    
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private async Task SyncCustomersFromCloudAsync()
    {
        if (!AppSession.CanReadOnline)
            return;

        try
        {
            var cloudCustomers = new CloudCustomersInstallmentService(_supabaseService);
            var rows = await cloudCustomers.GetCustomersAsync();

            foreach (var row in rows)
            {
                _customerService.UpsertFromCloud(
                    row.Id,
                    row.Name,
                    row.IdentityNumber,
                    row.Phone,
                    row.Address,
                    row.Job,
                    row.SponserName,
                    row.SponserIdentityNumber,
                    row.SponserPhone,
                    row.SponserAddress,
                    row.SponserJob
                );
            }

            LoadCustomer();
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping installment customers cloud sync.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        LoadCustomer();

    _ = _sync.PushAllDirtyAsync();
    _ = SyncCustomersFromCloudAsync();
    
    }

    private void CustomerGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (CustomerGrid.SelectedItem is CustomerInstallment customer)
            new CustomerWindowViewInstallment(customer, _supabaseService).Show();
    }
}