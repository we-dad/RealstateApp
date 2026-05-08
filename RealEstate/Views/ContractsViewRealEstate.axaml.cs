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
    private TextBox? _tenantIdSearchBox;
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
        _sync = new RealEstateSyncService(_db, _supabaseService);

        Refresh();

        _tenantIdSearchBox = this.FindControl<TextBox>("TenantIdSearchBox");

        _ = _sync.PushAllDirtyAsync();
        _ = SyncContractsFromCloudAsync();

    }

    private void LoadUnits()
    {
        var units = _unitsDB.GetAvailableUnits();

        UnitsBox.ItemsSource = units;

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

    private void LoadContract()
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

    private async Task SyncContractsFromCloudAsync()
    {
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
                    row.ContractOpligation
                );
            }

            LoadContract();
            LoadUnits();
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping contracts cloud sync.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void SearchTenant_Click(object? sender, RoutedEventArgs e)
    {
        var id = _tenantIdSearchBox?.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(id))
            return;

        var tenant = _tenantsDB.FindByIdentity(id);

        if (tenant is null)
        {
            _selectedTenant = null;
            TenantInfoText.Text = "لم يتم العثور على مستأجر بهذا الرقم";
            TenantInfoText.Foreground = Brushes.Red;
            return;
        }

        _selectedTenant = tenant;

        var tenantInfo = $"اسم المستأجر : {tenant.Name} | ";
        var tenantId = $"رقم الهوية/الإقامة : {tenant.IdentityNumber}";

        TenantInfoText.Text = tenantInfo + tenantId;
        TenantInfoText.Foreground = Brushes.Green;
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        Refresh();

        _ = _sync.PushAllDirtyAsync();
        _ = SyncContractsFromCloudAsync();
    }

    private void Refresh()
    {
        LoadContract();
        LoadUnits();
        LoadContractUnitRoomsType();
        LoadContractPayMethod();
        LoadContractObligations();

        RentAmountBox.Text = "";
        TenantIdSearchBox.Text = "";
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

    private void OpenInfoWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ContractRealEstate contract)
            new ContractsWindowViewRealEstate(contract.Id, _supabaseService).Show();
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
}