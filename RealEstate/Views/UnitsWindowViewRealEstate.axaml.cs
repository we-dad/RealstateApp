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
            var finalUnitName = unitName + "-" + unitNum;

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

        DataContext = _unit;

        LoadOwners();
        LoadUnitTypes();

        var unitNameWithoutNum = _unit.UnitName;

        if (unitNameWithoutNum.Length >= 2)
            unitNameWithoutNum = unitNameWithoutNum.Substring(0, unitNameWithoutNum.Length - 2);

        UnitNameBox.Text = unitNameWithoutNum;
        DistrictBox.Text = _unit.District ?? "";
        CityBox.Text = _unit.City ?? "";
        UnitTypeBox.Text = _unit.UnitType ?? "";
        UnitNumBox.Text = _unit.UnitNum.ToString();
        UnitsCountBox.Text = _unit.UnitsCount.ToString();


        ResultUnitNameBox.Text = _unit.UnitName ?? "";
        ResultDistrictBox.Text = _unit.District ?? "";
        ResultCityBox.Text = _unit.City ?? "";
        ResultUnitTypeBox.Text = _unit.UnitType ?? "";
        ResultUnitNumBox.Text = _unit.UnitsCount + " / " + _unit.UnitNum;
        
        OwnerDataGrid.ItemsSource = new List<UnitRealEstate> { _unit };
        ContractsGrid.ItemsSource = _unitsDB.GetContractsByUnitId(_unit.Id);
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