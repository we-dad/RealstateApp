using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class ContractsWindowViewRealEstate : Window
{
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly ContractServiceRealEstate _contractDB;
    private readonly TenantServiceRealEstate _tenantsDB;
    private readonly UnitServiceRealEstate _unitsDB;
    private readonly PdfServiceRealEstate _pdfServiceRealEstate;
    private readonly SupabaseService _supabaseService;
    private readonly RealEstateSyncService _sync;

    private ContractRealEstate? _contract;
    private readonly long _contractID;
    private TenantRealEstate? _selectedTenant;
    private TextBox? _tenantIdSearchBox;

    public ContractsWindowViewRealEstate(long contractID, SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();

        _contractDB = new ContractServiceRealEstate(_db);
        _tenantsDB = new TenantServiceRealEstate(_db);
        _unitsDB = new UnitServiceRealEstate(_db);
        _pdfServiceRealEstate = new PdfServiceRealEstate();
        _sync = new RealEstateSyncService(_db, _supabaseService);

        _contractID = contractID;

        Refresh();

        _tenantIdSearchBox = this.FindControl<TextBox>("TenantIdSearchBox");
    }

    private void LoadUnits()
    {
        var units = _unitsDB.GetAvailableUnitsIncluding(_contract?.UnitId);

        UnitsBox.ItemsSource = units;
        UnitsBox.SelectedItem = units.FirstOrDefault(u => u.Id == _contract?.UnitId);
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

    private void Update_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_contract is null)
                return;

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

            _contractDB.Update(
                _contract.Id,
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

        TenantInfoText.Text =
            $"اسم المستأجر : {tenant.Name} | رقم الهوية/الإقامة : {tenant.IdentityNumber}";

        TenantInfoText.Foreground = Brushes.Green;
    }

    private void Refresh()
    {
        _contract = _contractDB.GetById(_contractID);

        if (_contract is null)
            return;

        DataContext = _contract;

        LoadContractPayMethod();
        LoadContractUnitRoomsType();
        LoadContractObligations();
        LoadUnits();

        _selectedTenant = new TenantRealEstate
        {
            Id = _contract.TenantId,
            Name = _contract.TenantName,
            IdentityNumber = _contract.TenantIdentityNumber,
            Phone = _contract.TenantPhone,
            Address = _contract.TenantAddress
        };

        ContractNumBox.Text = _contract.ContractNumber;
        RentAmountBox.Text = _contract.RentAmount.ToString(CultureInfo.InvariantCulture);
        TenantIdSearchBox.Text = _contract.TenantIdentityNumber;
        TenantInfoText.Text =
            $"اسم المستأجر : {_contract.TenantName} | رقم الهوية/الإقامة : {_contract.TenantIdentityNumber}";
        TenantInfoText.Foreground = Brushes.Green;

        ContractDateStartPicker.SelectedDate = _contract.ContractStartDate;
        ContractDateEndPicker.SelectedDate = _contract.ContractEndDate;
        ContractPayMethodBox.SelectedItem = _contract.ContractPayMethod;
        ContractApartmentTypeBox.SelectedItem = _contract.ContractApartmentType;
        ContractObligationsBox.SelectedItem = _contract.ContractOpligation;
        ContractUnitRoomsNumBox.Text = _contract.ContractUnitRoomsNum.ToString();
        ContractUnitFloorNumBox.Text = _contract.ContractUnitFloorNum.ToString();

        ResultContractNumBox.Text = _contract.ContractNumber;
        ResultContractDateStartBox.Text = _contract.ContractStartDate.ToString("yyyy-MM-dd");
        ResultContractDateEndBox.Text = _contract.ContractEndDate.ToString("yyyy-MM-dd");
        ResultRentAmountBox.Text = _contract.RentAmount.ToString(CultureInfo.InvariantCulture);
        ResultContractPayMethodBox.Text = _contract.ContractPayMethod;
        ResultContractApartmentTypeBox.Text = _contract.ContractApartmentType;
        ResultContractUnitDetealsBox.Text =
            "غرف " + _contract.ContractUnitRoomsNum + " دور " + _contract.ContractUnitFloorNum;
        ResultContractOpligationBox.Text = _contract.ContractOpligation;

        ResultUnitNameBox.Text = _contract.UnitName;
        ResultDistrictBox.Text = _contract.District;
        ResultCityBox.Text = _contract.City;
        ResultUnitTypeBox.Text = _contract.UnitType;
        ResultUnitNumBox.Text = _contract.UnitNum + " / " + _contract.UnitsCount;

        ResultOwnerNameBox.Text = _contract.OwnerName;
        ResultOwnerIdentityNumberBox.Text = _contract.OwnerIdentityNumber;
        ResultOwnerPhoneBox.Text = _contract.OwnerPhone;
        ResultOwnerAddressBox.Text = _contract.OwnerAddress;

        ResultTenantNameBox.Text = _contract.TenantName;
        ResultTenantIdentityNumberBox.Text = _contract.TenantIdentityNumber;
        ResultTenantPhoneBox.Text = _contract.TenantPhone;
        ResultTenantAddressBox.Text = _contract.TenantAddress;
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
        if (_contract is null)
            return;

        var path = await PickSavePdfPathAsync(_contract.ContractNumber);

        if (string.IsNullOrWhiteSpace(path))
            return;

        _pdfServiceRealEstate.GenerateContractPdf(_contract, path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (_contract is null)
            return;

        try
        {
            _contractDB.Delete(_contract.Id);

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