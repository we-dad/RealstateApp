using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RealEstateApp.Models;
using RealEstateApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RealEstateApp.Views;

public partial class UnitsWindowView : Window
{

    private readonly DbService _db = new DbService();
    private readonly OwnerService _ownersDB;
    private readonly UnitService _unitsDB;
    private Unit? _unit;
    private long _unitID;


    public UnitsWindowView(long unitID)
    {
        InitializeComponent();

        _db.Initialize();
        _ownersDB = new OwnerService(_db);
        _unitsDB = new UnitService(_db);

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

            if (OwnerBox.SelectedItem is not Owner owner)
                return;

            var city = CityBox.Text?.Trim() ?? "";
            var unitName = UnitNameBox.Text?.Trim() ?? "";
            var district = DistrictBox.Text?.Trim() ?? "";
            var unitType = UnitTypeBox.SelectedItem as string ?? "سكني";
            var unitsCount = int.Parse(UnitsCountBox.Text?.Trim() ?? "1");
            var unitNum = int.Parse(UnitNumBox.Text?.Trim() ?? "1");


            _unitsDB.Update(
            _unit.Id,
            owner.Id,
            unitName + "-" + unitNum,
            city,
            district,
            unitType,
            unitsCount,
            unitNum
        );

            Refresh();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void Refresh()
    {
        _unit = _unitsDB.GetById(_unitID) ?? new Unit();
        if (_unit is null) return;

        DataContext = _unit;

        LoadOwners();
        LoadUnitTypes();

        //unit name
        string unitNameWithotNum = _unit.UnitName;
        if (unitNameWithotNum.Length >= 2)
        {
            unitNameWithotNum = unitNameWithotNum.Substring(0, unitNameWithotNum.Length - 2);
        }
        UnitNameBox.Text = unitNameWithotNum ?? "";

        DistrictBox.Text = _unit.District ?? "";
        CityBox.Text = _unit.City ?? "";
        UnitTypeBox.Text = _unit.UnitType ?? "";
        UnitNumBox.Text = _unit.UnitNum.ToString() ?? "";
        UnitsCountBox.Text = _unit.UnitsCount.ToString() ?? "";

        ResultNameBox.Text = _unit.OwnerName ?? "";
        ResultIdentityNumberBox.Text = _unit.OwnerIdentityNumber ?? "";
        ResultPhoneBox.Text = _unit.OwnerPhone ?? "";
        ResultAddressBox.Text = _unit.OwnerAddress ?? "";

        ResultUnitNameBox.Text = _unit.UnitName ?? "";
        ResultDistrictBox.Text = _unit.District ?? "";
        ResultCityBox.Text = _unit.City ?? "";
        ResultUnitTypeBox.Text = _unit.UnitType ?? "";
        ResultUnitNumBox.Text = _unit.UnitsCount.ToString() + " / " + _unit.UnitNum.ToString() ?? "";
    }
    private async void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (_unit is null) return;
        try
        {
            _unitsDB.Delete(_unit.Id);
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
