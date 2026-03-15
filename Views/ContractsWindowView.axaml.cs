using Avalonia;
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
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateApp.Views;

public partial class ContractsWindowView : Window
{
    private readonly DbService _db = new DbService();
    private readonly ContractService _contarctDB;
    private readonly TenantService _TenantsDB;
    private readonly OwnerService _ownersDB;
    private readonly UnitService _unitsDB;
    private PdfService _pdfService;

    private Contract? _contract;
    private long _contractID;
    private Tenant? _selectedTenant;
    private TextBox? _tenantIdSearchBox;

    public ContractsWindowView(long contractID)
    {
        InitializeComponent();

        _db.Initialize();

        _contarctDB = new ContractService(_db);
        _TenantsDB = new TenantService(_db);
        _ownersDB = new OwnerService(_db);
        _unitsDB = new UnitService(_db);
        _pdfService = new PdfService();

        _contractID = contractID;

        Refresh();

        _tenantIdSearchBox = this.FindControl<TextBox>("TenantIdSearchBox"); //this line becasue avalonia can't found TenantIdSearchBox it's returen null maybe because the warning message
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
            if (_contract is null) return;

            var ContractDateStart = ContractDateStartPicker.SelectedDate?.LocalDateTime
                     ?? DateTime.Today;
            var ContractDateEnd = ContractDateEndPicker.SelectedDate?.LocalDateTime
                     ?? DateTime.Today.AddYears(1);

            if (UnitsBox.SelectedItem is not Unit unit)
                return;

            if (_selectedTenant == null)
            {
                TenantInfoText.Text = "يرجى اختيار مستأجر أولاً";
                return;
            }
            var tenantId = _selectedTenant.Id;

            var RentAmount = double.Parse(
                RentAmountBox.Text?.Trim() ?? "",
                CultureInfo.InvariantCulture
            );

            var ContractPayMethod = ContractPayMethodBox.SelectedItem as string ?? "شهري";
            var ContractApartmentType = ContractApartmentTypeBox.SelectedItem as string ?? "غرفة مفروشة";
            var ContractObligations = ContractObligationsBox.SelectedItem as string ?? "غرفة مفروشة";

            var ContractUnitRoomsNum = int.Parse(ContractUnitRoomsNumBox.Text?.Trim() ?? "", CultureInfo.InvariantCulture);
            var ContractUnitFloorNum = int.Parse(ContractUnitFloorNumBox.Text?.Trim() ?? "", CultureInfo.InvariantCulture);


            _contarctDB.Update(_contract.Id, ContractDateStart, ContractDateEnd, unit.Id, tenantId, RentAmount, ContractPayMethod, ContractApartmentType, ContractUnitRoomsNum, ContractUnitFloorNum, ContractObligations);

            Refresh();
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

        var tenant = _TenantsDB.FindByIdentity(id);

        if (tenant is null)
        {
            _selectedTenant = null;
            TenantInfoText.Text = "لم يتم العثور على مستأجر بهذا الرقم";
            TenantInfoText.Foreground = Brushes.Red;
            return;
        }

        _selectedTenant = tenant;
        var _tenantInfo = $"اسم المستأجر : {tenant.Name} | ";
        var _tanentID = $"رقم الهوية/الإقامة : {tenant.IdentityNumber}";
        TenantInfoText.Text = _tenantInfo + _tanentID;
        TenantInfoText.Foreground = Brushes.Green;
    }
    private void Refresh()
    {
        _contract = _contarctDB.GetById(_contractID) ?? new Contract();
        if (_contract is null) return;

        DataContext = _contract;

        LoadContractPayMethod();
        LoadContractUnitRoomsType();
        LoadContractObligations();
        LoadUnits();

        //Contract Field Info
        ContractNumBox.Text = _contract.ContractNumber;
        RentAmountBox.Text = _contract.RentAmount.ToString();
        TenantIdSearchBox.Text = _contract.TenantIdentityNumber;
        ContractDateStartPicker.SelectedDate = _contract.ContractStartDate;
        ContractDateEndPicker.SelectedDate = _contract.ContractEndDate;
        ContractPayMethodBox.SelectedItem = _contract.ContractPayMethod;
        ContractApartmentTypeBox.SelectedItem = _contract.ContractApartmentType;
        ContractObligationsBox.SelectedItem = _contract.ContractOpligation;
        ContractUnitRoomsNumBox.Text = _contract.ContractUnitRoomsNum.ToString();
        ContractUnitFloorNumBox.Text = _contract.ContractUnitFloorNum.ToString();

        //Contract Info
        ResultContractNumBox.Text = _contract.ContractNumber;
        ResultContractDateStartBox.Text = _contract.ContractStartDate.ToString("yyyy-MM-dd");
        ResultContractDateEndBox.Text = _contract.ContractEndDate.ToString("yyyy-MM-dd");
        ResultRentAmountBox.Text = _contract.RentAmount.ToString();
        ResultContractPayMethodBox.Text = _contract.ContractPayMethod;
        ResultContractApartmentTypeBox.Text = _contract.ContractApartmentType;
        ResultContractUnitDetealsBox.Text = "غرف " + _contract.ContractUnitRoomsNum.ToString() + "دور " + _contract.ContractUnitFloorNum.ToString();
        ResultContractOpligationBox.Text = _contract.ContractOpligation;

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
        if (_contract is null) return;

        var path = await PickSavePdfPathAsync(_contract.ContractNumber);
        if (string.IsNullOrWhiteSpace(path))
            return;

        _pdfService.GenerateContractPdf(_contract, path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
    private async void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (_contract is null) return;
        try
        {
            _contarctDB.Delete(_contract.Id);
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