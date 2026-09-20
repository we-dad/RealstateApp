using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class ContractsViewRealEstate : UserControl
{
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly ContractServiceRealEstate _contractsDB;
    private readonly TenantServiceRealEstate _tenantsDB;
    private readonly UnitServiceRealEstate _unitsDB;
    private readonly PdfServiceRealEstate _pdfServiceRealEstate;
    private readonly SupabaseService _supabaseService;
    private readonly RealEstateSyncService _sync;

    private TenantRealEstate? _selectedTenant;
    private AutoCompleteBox? _tenantIdSearchBox;
    private string? ContractNumber;

    public ContractsViewRealEstate(SupabaseService supabaseService)
    {
        InitializeComponent();

        _supabaseService = supabaseService;

        _db.Initialize();

        _contractsDB = new ContractServiceRealEstate(_db);
        _tenantsDB = new TenantServiceRealEstate(_db);
        _unitsDB = new UnitServiceRealEstate(_db);
        _pdfServiceRealEstate = new PdfServiceRealEstate();
        // Reload the grid (only) when data changes: an add, an edit or a delete, also
        // from the details window, so there is no need to press "تحديث".
        _autoRefresh = new ScreenAutoRefresh(
            this,
            LoadContract,
            periodicSync: PeriodicSyncAsync,
            interval: TimeSpan.FromMinutes(2));

        _sync = new RealEstateSyncService(_db, _supabaseService);

        // The suggestions list matches the identity number or the name. Wired here
        // (not in the XAML) so nothing fires while the window is being built.
        TenantIdSearchBox.ItemFilter = (search, item) =>
            item is TenantPickRowRealEstate row
            && !string.IsNullOrWhiteSpace(search)
            && row.Display.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase);
        TenantIdSearchBox.ItemSelector = (search, item) =>
            (item as TenantPickRowRealEstate)?.IdentityNumber ?? search;
        TenantIdSearchBox.SelectionChanged += TenantIdSearchBox_SelectionChanged;
        TenantIdSearchBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == AutoCompleteBox.TextProperty)
                TenantIdSearchBox_TextChanged();
        };

        Refresh();

        _tenantIdSearchBox = this.FindControl<AutoCompleteBox>("TenantIdSearchBox");

        _ = SyncAsync();

    }

    // keepSelection is true only for the periodic sync, which reloads this list while
    // the user may be filling the form: the chosen item stays chosen, or nothing is
    // selected if it is gone (never a silent jump to another item). Everything else
    // (open, the refresh button, after an add) keeps the old behaviour: first item selected.
    private void LoadUnits(bool keepSelection = false)
    {
        var selectedId = (UnitsBox.SelectedItem as UnitRealEstate)?.Id;
        var units = _unitsDB.GetAvailableUnits();

        UnitsBox.ItemsSource = units;

        if (keepSelection && selectedId is long id)
        {
            UnitsBox.SelectedItem = units.FirstOrDefault(x => x.Id == id);
            return;
        }

        if (units.Count > 0)
            UnitsBox.SelectedIndex = 0;
    }

    private void LoadContractPayMethod()
    {
        ContractPayMethodBox.ItemsSource = new List<string>
        {
            "شهري",
            "سنوي",
            "3 شهور",
            "6 شهور",
            "أخرى"
        };

        ContractPayMethodBox.SelectedIndex = 0;
    }

    private void LoadContractUnitRoomsType()
    {
        ContractApartmentTypeBox.ItemsSource = new List<string>
        {
            "غرفة مفروشة",
            "شقة سكنية"
        };

        ContractApartmentTypeBox.SelectedIndex = 0;
    }

    private void LoadContractObligations()
    {
        ContractObligationsBox.ItemsSource = new List<string>
        {
            "يتحمل المؤجر مسؤولية الصيانة كاملة, يتحمل المؤجر فواتير الكهرباء والماء",
            "لا يتحمل المؤجر مسؤولية الصيانة كاملة, لا يتحمل المؤجر فواتير الكهرباء والماء"
        };

        ContractObligationsBox.SelectedIndex = 0;
    }

    // Every reload (open, add, pull, timer) keeps the selected row selected, chosen at
    // the moment the grid is replaced, so a row picked while a sync runs is not undone.
    private void LoadContract() =>
        ScreenAutoRefresh.ReloadKeepingSelection<ContractRealEstate>(ContractGrid, r => r.Id, LoadContractCore);

    private void LoadContractCore()
    {
        try
        {
            _contractsDB.UpdateContractStates();
            _unitsDB.UpdateUnitStates();

            var data = _contractsDB.GetAll();

            ContractGrid.ItemsSource = null;
            ContractGrid.ItemsSource = data;
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
            var contractNumber = ContractNumber?.Trim() ?? "";

            var contractDateStart =
                ContractDateStartPicker.SelectedDate?.LocalDateTime
                ?? DateTime.Today;

            var contractDateEnd =
                ContractDateEndPicker.SelectedDate?.LocalDateTime
                ?? DateTime.Today.AddYears(1);

            if (UnitsBox.SelectedItem is not UnitRealEstate unit)
                return;

            if (_selectedTenant == null)
            {
                TenantInfoText.Text = "يرجى اختيار مستأجر أولاً";
                TenantInfoText.Foreground = Brushes.Red;
                return;
            }

            var tenant = _selectedTenant;

            var rentAmount = double.Parse(
                RentAmountBox.Text?.Trim() ?? "0",
                CultureInfo.InvariantCulture
            );

            var contractPayMethod =
                ContractPayMethodBox.SelectedItem as string ?? "شهري";

            var contractApartmentType =
                ContractApartmentTypeBox.SelectedItem as string ?? "غرفة مفروشة";

            var contractObligations =
                ContractObligationsBox.SelectedItem as string ?? "";

            var contractUnitRoomsNum = int.Parse(
                ContractUnitRoomsNumBox.Text?.Trim() ?? "0",
                CultureInfo.InvariantCulture
            );

            var contractUnitFloorNum = int.Parse(
                ContractUnitFloorNumBox.Text?.Trim() ?? "0",
                CultureInfo.InvariantCulture
            );

            _contractsDB.Add(
                contractNumber,
                contractDateStart,
                contractDateEnd,
                unit.Id,
                tenant.Id,
                rentAmount,
                contractPayMethod,
                contractApartmentType,
                contractUnitRoomsNum,
                contractUnitFloorNum,
                contractObligations
            );

            Refresh();

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
            await SyncContractsFromCloudAsync();
        }
        finally
        {
            _syncGate.Release();
        }
    }

    // Every 2 minutes (while the app is active); skipped while a sync is already running.
    private Task PeriodicSyncAsync() =>
        _syncGate.CurrentCount == 0 ? Task.CompletedTask : SyncAsync();

    private async Task SyncContractsFromCloudAsync()
    {
        if (!AppSession.CanReadOnline)
            return;

        try
        {
            var cloudContracts = new CloudContractsRealEstateService(_supabaseService);
            var cloudRows = await cloudContracts.GetContractsAsync();

            foreach (var row in cloudRows)
            {
                var unitLocalId = _unitsDB.GetLocalIdByCloudId(row.UnitId);
                var tenantLocalId = _tenantsDB.GetLocalIdByCloudId(row.TenantId);

                if (unitLocalId == 0 || tenantLocalId == 0)
                    continue;

                _contractsDB.UpsertFromCloud(
                    row.Id,
                    row.ContractNumber,
                    row.ContractStartDate,
                    row.ContractEndDate,
                    unitLocalId,
                    tenantLocalId,
                    row.RentAmount,
                    row.ContractState,
                    row.ContractPayMethod,
                    row.ContractApartmentType,
                    row.ContractUnitRoomsNum,
                    row.ContractUnitFloorNum,
                    row.ContractOpligation,
                    row.SignatureCloudPath,
                    row.SignatureFileName,
                    row.SignatureFileType
                );
            }

            LoadContract();
            LoadUnits(keepSelection: true);

            SyncStatusService.ReportPull(true);
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping contracts cloud sync.");
            SyncStatusService.ReportPull(false, offline: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            SyncStatusService.ReportPull(false);
        }
    }

    // Picking a line from the suggestions selects that exact tenant (by id: two
    // people could share an identity number).
    private void TenantIdSearchBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TenantIdSearchBox.SelectedItem is not TenantPickRowRealEstate row)
            return;

        TenantIdSearchBox.Text = row.IdentityNumber;

        var tenant = _tenantsDB.GetById(row.Id);

        if (tenant is null)
        {
            _selectedTenant = null;
            TenantInfoText.Text = "لم يتم العثور على مستأجر بهذا الرقم";
            TenantInfoText.Foreground = Brushes.Red;
            return;
        }

        ShowSelectedTenant(tenant);
    }

    // If the text no longer points at the chosen tenant, forget it, so a contract
    // can never be saved for a person the box does not show.
    private void TenantIdSearchBox_TextChanged()
    {
        if (_selectedTenant is null)
            return;

        var typed = TenantIdSearchBox.Text?.Trim() ?? "";

        if (typed.Equals(_selectedTenant.IdentityNumber?.Trim() ?? "", StringComparison.OrdinalIgnoreCase))
            return;

        _selectedTenant = null;
        TenantInfoText.Text = "";
    }

    private void ShowSelectedTenant(TenantRealEstate tenant)
    {
        _selectedTenant = tenant;

        var tenantInfo = $"اسم المستأجر : {tenant.Name} | ";
        var tenantId = $"رقم الهوية/الإقامة : {tenant.IdentityNumber}";

        TenantInfoText.Text = tenantInfo + tenantId;
        TenantInfoText.Foreground = Brushes.Green;
    }

    private void SearchTenant_Click(object? sender, RoutedEventArgs e)
    {
        var id = _tenantIdSearchBox?.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(id))
            return;

        // Already chosen (from the suggestions or loaded with the contract): keep that
        // exact person, a search by identity could return another one with the same number.
        if (_selectedTenant is not null && (_selectedTenant.IdentityNumber ?? "").Trim() == id)
            return;

        var tenant = _tenantsDB.FindByIdentity(id);

        if (tenant is null)
        {
            _selectedTenant = null;
            TenantInfoText.Text = "لم يتم العثور على مستأجر بهذا الرقم";
            TenantInfoText.Foreground = Brushes.Red;
            return;
        }

        ShowSelectedTenant(tenant);
    }

    private ScreenAutoRefresh? _autoRefresh;

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        Refresh();

        _ = SyncAsync();
    }

    private void Refresh()
    {
        LoadContract();
        LoadUnits();
        LoadContractUnitRoomsType();
        LoadContractPayMethod();
        LoadContractObligations();

        RentAmountBox.Text = "";
        TenantIdSearchBox.SelectedItem = null;
        TenantIdSearchBox.Text = "";
        TenantIdSearchBox.ItemsSource = _tenantsDB.GetPickRows();
        TenantInfoText.Text = "";
        TenantInfoText.Foreground = Brushes.Black;

        _selectedTenant = null;

        ContractNumber = _contractsDB.GenerateContractNumber();
        ContractNumBox.Text = ContractNumber;

        ContractDateStartPicker.SelectedDate = DateTime.Today;
        ContractDateEndPicker.SelectedDate = DateTime.Today.AddYears(1);

        ContractUnitRoomsNumBox.Text = "";
        ContractUnitFloorNumBox.Text = "";
    }

    private void ContractGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (ContractGrid.SelectedItem is not ContractRealEstate contract)
            return;

        var window = new ContractsWindowViewRealEstate(contract.Id, _supabaseService);
        window.Show();
    }

    private async Task<string?> PickSavePdfPathAsync(string contractNumber)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel is null)
            return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "حفظ العقد  (PDF)",
                SuggestedFileName = $"{contractNumber}.pdf",
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
        if (sender is not Button btn || btn.Tag is not ContractRealEstate contract)
            return;

        ContractRealEstate? selectedContract = _contractsDB.GetById(contract.Id);

        if (selectedContract is null)
            return;

        var path = await PickSavePdfPathAsync(selectedContract.ContractNumber);

        if (string.IsNullOrWhiteSpace(path))
            return;

        _pdfServiceRealEstate.GenerateContractPdf(selectedContract, path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
    
    private async void UploadSignature_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn)
            return;

        if (btn.Tag is not ContractRealEstate contract)
            return;

        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel == null)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "اختر ملف العقد",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("PDF / Images")
                    {
                        Patterns = ["*.pdf", "*.jpg", "*.jpeg", "*.png"]
                    }
                ]
            });

        if (files.Count == 0)
            return;

        var file = files[0];

        var extension = Path.GetExtension(file.Name);

        var fileName = $"real-estate-contract-{contract.Id}{extension}";

        var cloudPath =
            $"real-estate/contracts/{contract.Id}/{fileName}";
        
        if (!string.IsNullOrWhiteSpace(contract.SignatureCloudPath))
        {
            await _supabaseService.Client.Storage
                .From("Rcontract-signatures")
                .Remove(
                [
                    contract.SignatureCloudPath
                ]);
        }
        
        await _supabaseService.Client.Storage
            .From("Rcontract-signatures")
            .Upload(
                file.Path!.LocalPath,
                cloudPath,
                new Supabase.Storage.FileOptions
                {
                    Upsert = true,
                    CacheControl = "3600"
                });

        _contractsDB.UpdateSignatureCloudInfo(
            contract.Id,
            cloudPath,
            file.Name,
            extension);

        LoadContract();
    }
    
    private async void OpenSignature_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button btn)
                return;

            if (btn.Tag is not ContractRealEstate contract)
                return;

            var currentContract = _contractsDB.GetById(contract.Id);
            var cloudPath = currentContract?.SignatureCloudPath ?? "";

            if (string.IsNullOrWhiteSpace(cloudPath))
                return;

            var signedUrl = await _supabaseService.Client.Storage
                .From("Rcontract-signatures")
                .CreateSignedUrl(cloudPath, 60);

            Process.Start(new ProcessStartInfo
            {
                FileName = signedUrl,
                UseShellExecute = true
            });
        }
        catch (Supabase.Storage.Exceptions.SupabaseStorageException)
        {
            Console.WriteLine("File not found in Supabase Storage.");
        }
    }
}