using Avalonia.Controls;
using Avalonia.Interactivity;
using RealEstateApp.Models;
using RealEstateApp.Services;
using System;
using System.Collections.Generic;

namespace RealEstateApp.Views;

public partial class UnitsView : UserControl
{
    private readonly DbService _db = new DbService();
    private readonly OwnerService _owners;
    private readonly UnitService _units;

    public UnitsView()
    {
        InitializeComponent();

        _db.Initialize();

        _owners = new OwnerService(_db);
        _units = new UnitService(_db);

        LoadOwners();
        LoadUnitTypes();
        LoadUnits();
    }

    private void LoadOwners()
    {
        var owners = _owners.GetAll();

        OwnerBox.ItemsSource = owners;

        if (owners.Count > 0)
            OwnerBox.SelectedIndex = 0;
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

    private void LoadUnits()
    {
        try
        {
            var data = _units.GetAll();
            UnitsGrid.ItemsSource = null;
            UnitsGrid.ItemsSource = data;
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
            if (OwnerBox.SelectedItem is not Owner owner)
                return;

            var city = CityBox.Text?.Trim() ?? "";
            var unitName = UnitNameBox.Text?.Trim() ?? "";
            var district = DistrictBox.Text?.Trim() ?? "";
            var unitType = UnitTypeBox.SelectedItem as string ?? "سكني";

            var unitsCountText = UnitsCountBox.Text?.Trim() ?? "1";
            if (!int.TryParse(unitsCountText, out var unitsCount) || unitsCount < 1)
                unitsCount = 1;
            else
                unitsCount = int.Parse(unitsCountText);


            for (int i = 1; i <= unitsCount; i++)
            {
                var unitNum = i;
                _units.Add(
                owner.Id,
                unitName + "-" + i,
                city,
                district,
                unitType,
                unitsCount,
                unitNum
            );
            }


            // Clear form
            UnitNameBox.Text = "";
            CityBox.Text = "";
            DistrictBox.Text = "";
            UnitTypeBox.SelectedIndex = 0;
            UnitsCountBox.Text = "1";

            LoadUnits();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }


    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        LoadOwners();
        LoadUnits();
    }

    private void OpenInfoWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Unit unit)
            new UnitsWindowView(unit.Id).Show();
    }
}
