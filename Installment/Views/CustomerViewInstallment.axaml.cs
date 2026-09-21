using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Linq;
using System.Threading;
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

        // Reload the grid (only) whenever a customer is added, edited or deleted,
        // also from the details window.
        _autoRefresh = new ScreenAutoRefresh(
            this,
            LoadCustomer,
            periodicSync: PeriodicSyncAsync,
            interval: TimeSpan.FromMinutes(2));

    _sync = new InstallmentSyncService(_db, _supabaseService);
    _ = SyncAsync();
    
    CustomerGrid.DoubleTapped += CustomerGrid_DoubleTapped;
    
    }

    private ScreenAutoRefresh? _autoRefresh;

    // Every reload (open, add, pull, timer) keeps the selected row selected, chosen at
    // the moment the grid is replaced, so a row picked while a sync runs is not undone.
    private void LoadCustomer() =>
        ScreenAutoRefresh.ReloadKeepingSelection<CustomerInstallment>(CustomerGrid, r => r.Id, LoadCustomerCore);

    private void LoadCustomerCore()
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

    // push must finish before the pull, or the pull re-reads rows the
    // push has not written CloudIds for yet and duplicates them
    //
    // One sync at a time for this screen (static: also across an old and a new instance
    // of the screen). Two overlapping pulls could both insert the same new cloud row.
    // A request from the user (opening the screen, the refresh button) WAITS for its
    // turn, so it is never lost.
    private static readonly SemaphoreSlim _syncGate = new SemaphoreSlim(1, 1);

    private async Task SyncAsync()
    {
        await _syncGate.WaitAsync();

        try
        {
            await _sync.PushAllDirtyAsync();
            await SyncCustomersFromCloudAsync();
        }
        finally
        {
            _syncGate.Release();
        }
    }

    // Every 2 minutes (while the app is active); skipped while a sync is already running.
    private Task PeriodicSyncAsync() =>
        _syncGate.CurrentCount == 0 ? Task.CompletedTask : SyncAsync();

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

            SyncStatusService.ReportPull(true);
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping installment customers cloud sync.");
            SyncStatusService.ReportPull(false, offline: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            SyncStatusService.ReportPull(false);
        }
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        LoadCustomer();

    _ = SyncAsync();
    
    }

    private void CustomerGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (CustomerGrid.SelectedItem is CustomerInstallment customer)
            new CustomerWindowViewInstallment(customer, _supabaseService).Show();
    }
}