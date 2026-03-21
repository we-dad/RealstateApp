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

public partial class ContractsView : UserControl
{
    private readonly DbService _db = new DbService();
    private readonly ContractService _contractsDB;
    private readonly TenantService _TenantsDB;
    private readonly UnitService _unitsDB;
    private PdfService _pdfService;

    private Tenant? _selectedTenant;
    private TextBox? _tenantIdSearchBox;
    private string? ContractNumber;

    public ContractsView()
    {
        InitializeComponent();

        _db.Initialize();

        _contractsDB = new ContractService(_db);
        _TenantsDB = new TenantService(_db);
        _unitsDB = new UnitService(_db);
        _pdfService = new PdfService();

        Refresh();

        _tenantIdSearchBox = this.FindControl<TextBox>("TenantIdSearchBox"); //this line becasue avalonia can't found TenantIdSearchBox it's returen null maybe because the warning message

    }

    private void LoadUnits()
    {
        var Units = _unitsDB.GetAvailableUnits();

        UnitsBox.ItemsSource = Units;

        if (Units.Count > 0)
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
        _contractsDB.UpdateContractStates();
        _unitsDB.UpdateUnitStates();

        var data = _contractsDB.GetAll();

        ContractGrid.ItemsSource = null;
        ContractGrid.ItemsSource = data;
    }
    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var _contractNumber = ContractNumber?.Trim() ?? "";
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


            _contractsDB.Add(_contractNumber, ContractDateStart, ContractDateEnd, unit.Id, tenantId, RentAmount, ContractPayMethod, ContractApartmentType, ContractUnitRoomsNum, ContractUnitFloorNum, ContractObligations);

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
    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        Refresh();
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

        ContractNumber = _contractsDB.GenerateContractNumber();
        ContractNumBox.Text = ContractNumber.ToString();

        ContractDateStartPicker.SelectedDate = DateTime.Today;
        ContractDateEndPicker.SelectedDate = DateTime.Today.AddYears(1);

        ContractUnitRoomsNumBox.Text = "";
        ContractUnitFloorNumBox.Text = "";
    }
    private void OpenInfoWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Contract contract)
            new ContractsWindowView(contract.Id).Show();
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
        if (sender is not Button btn || btn.Tag is not Contract contract) return;

        Contract? _contract = _contractsDB.GetById(contract.Id);
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
}
