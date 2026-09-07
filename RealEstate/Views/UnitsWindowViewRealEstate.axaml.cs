using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class UnitsWindowViewRealEstate : Window
{
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly OwnerServiceRealEstate _ownersDB;
    private readonly UnitServiceRealEstate _unitsDB;
    private UnitRealEstate? _unit;
    private readonly long _unitID;
    private readonly SupabaseService _supabaseService;
    private readonly RealEstateSyncService _sync;

    public UnitsWindowViewRealEstate(long unitID, SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();
        _ownersDB = new OwnerServiceRealEstate(_db);
        _unitsDB = new UnitServiceRealEstate(_db);
        _sync = new RealEstateSyncService(_db, _supabaseService);

        _unitID = unitID;

        Refresh();
    }

    private void LoadOwners()
    {
        var owners = _ownersDB.GetAll();

        OwnerBox.ItemsSource = owners;
        OwnerBox.SelectedItem = owners.FirstOrDefault(o => o.Id == _unit?.OwnerId);
    }

    private void LoadUnitTypes()
    {
        UnitTypeBox.ItemsSource = new List<string>
        {
            "سكني",
            "تجاري"
        };

        UnitTypeBox.SelectedIndex = 0;
    }

    private void Update_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_unit is null) return;

            if (OwnerBox.SelectedItem is not OwnerRealEstate owner)
                return;

            var city = CityBox.Text?.Trim() ?? "";
            var unitName = UnitNameBox.Text?.Trim() ?? "";
            var district = DistrictBox.Text?.Trim() ?? "";
            var unitType = UnitTypeBox.SelectedItem as string ?? "سكني";
            var unitsCount = int.Parse(UnitsCountBox.Text?.Trim() ?? "1");
            var unitNum = int.Parse(UnitNumBox.Text?.Trim() ?? "1");

            // a building keeps its plain name, a unit gets its number back
            var finalUnitName = _unit.ParentId == 0
                ? unitName
                : unitName + "-" + unitNum;

            _unitsDB.Update(
                _unit.Id,
                owner.Id,
                finalUnitName,
                city,
                district,
                unitType,
                unitsCount,
                unitNum
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
        _unit = _unitsDB.GetById(_unitID);
        if (_unit is null) return;

        var groupId = _unit.ParentId == 0 ? _unit.Id : _unit.ParentId;
        var units = _unitsDB.GetChildren(groupId);

        // a building's own UnitState is never maintained, so work it
        // out from its units before the badge binds to it
        if (_unit.ParentId == 0 && units.Count > 0)
        {
            _unit.UnitState = units.Any(u => u.UnitState == "شاغرة")
                ? "شاغرة"
                : "مؤجرة";
        }

        DataContext = _unit;

        LoadOwners();
        LoadUnitTypes();

        UnitNameBox.Text = StripUnitNumber(_unit.UnitName);
        DistrictBox.Text = _unit.District ?? "";
        CityBox.Text = _unit.City ?? "";
        UnitTypeBox.SelectedItem = _unit.UnitType ?? "سكني";
        UnitNumBox.Text = _unit.UnitNum.ToString();
        UnitsCountBox.Text = _unit.UnitsCount.ToString();

        ResultUnitNameBox.Text = _unit.UnitName ?? "";
        ResultDistrictBox.Text = _unit.District ?? "";
        ResultCityBox.Text = _unit.City ?? "";
        ResultUnitTypeBox.Text = _unit.UnitType ?? "";

        ResultUnitNumBox.Text = _unit.ParentId == 0
            ? _unit.UnitsCount + " وحدة"
            : _unit.UnitsCount + " / " + _unit.UnitNum;

        OwnerDataGrid.ItemsSource = new List<UnitRealEstate> { _unit };
        ContractsGrid.ItemsSource = _unitsDB.GetContractsByUnitId(_unit.Id);

        ShowUnitsList(units);
    }

    // every unit under the same building, including this one
    private void ShowUnitsList(List<UnitRealEstate> units)
    {
        UnitsListGrid.ItemsSource = null;
        UnitsListGrid.ItemsSource = units;

        var vacant = units.Count(u => u.UnitState == "شاغرة");

        UnitsSummaryText.Text = units.Count == 0
            ? "لا توجد وحدات تابعة"
            : $"شاغرة {vacant} من {units.Count} — انقر مرتين لفتح الوحدة";
    }

    private void UnitsListGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (UnitsListGrid.SelectedItem is not UnitRealEstate unit)
            return;

        // already looking at it
        if (unit.Id == _unitID)
            return;

        var window = new UnitsWindowViewRealEstate(unit.Id, _supabaseService);
        window.Show();
    }

    // "عمارة الفهد-12" -> "عمارة الفهد"
    private static string StripUnitNumber(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return "";

        var dash = name.LastIndexOf('-');

        if (dash <= 0)
            return name;

        var suffix = name.Substring(dash + 1);

        return int.TryParse(suffix, out _)
            ? name.Substring(0, dash)
            : name;
    }

    private void OwnerDataGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (_unit is null) return;

        var owner = new OwnerRealEstate
        {
            Id = _unit.OwnerId
        };

        var window = new OwnersWindowViewRealEstate(owner, _supabaseService);
        window.Show();
    }

    private void ContractsGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (ContractsGrid.SelectedItem is not ContractRealEstate contract)
            return;

        var window = new ContractsWindowViewRealEstate(contract.Id, _supabaseService);
        window.Show();
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (_unit is null) return;

        try
        {
            _unitsDB.Delete(_unit.Id);

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