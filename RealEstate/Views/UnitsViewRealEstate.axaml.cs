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

public partial class UnitsViewRealEstate : UserControl
{
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly OwnerServiceRealEstate _owners;
    private readonly UnitServiceRealEstate _units;
    private readonly SupabaseService _supabaseService;
    private readonly RealEstateSyncService _sync;

    public UnitsViewRealEstate(SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();

        _owners = new OwnerServiceRealEstate(_db);
        _units = new UnitServiceRealEstate(_db);
        _sync = new RealEstateSyncService(_db, _supabaseService);

        LoadOwners();
        LoadUnitTypes();
        LoadUnits();

        _ = SyncAsync();
    }

    // push must finish before the pull, or the pull re-reads rows the
    // push has not written CloudIds for yet and duplicates them
    private async Task SyncAsync()
    {
        await _sync.PushAllDirtyAsync();
        await SyncUnitsFromCloudAsync();
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

    // (2) CHANGED: load buildings, attach their units
    private void LoadUnits()
    {
        try
        {
            // refresh شاغرة / مؤجرة first, or a contract that ended
            // yesterday would still show its unit as rented
            _units.UpdateUnitStates();

            var data = _units.GetAll();

            UnitsGrid.ItemsSource = null;
            UnitsGrid.ItemsSource = data;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    // (3) CHANGED: create the parent row first, then its units
    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        if (!AppSession.CanReadOnline)
            return;

        try
        {
            if (OwnerBox.SelectedItem is not OwnerRealEstate owner)
                return;

            var city = CityBox.Text?.Trim() ?? "";
            var unitName = UnitNameBox.Text?.Trim() ?? "";
            var district = DistrictBox.Text?.Trim() ?? "";
            var unitType = UnitTypeBox.SelectedItem as string ?? "سكني";

            if (string.IsNullOrWhiteSpace(unitName))
                return;

            var unitsCountText = UnitsCountBox.Text?.Trim() ?? "1";

            if (!int.TryParse(unitsCountText, out var unitsCount) || unitsCount < 1)
                unitsCount = 1;

            // the parent: plain name, no suffix, UnitNum 0
            var parentId = _units.Add(
                owner.Id,
                0,
                unitName,
                city,
                district,
                unitType,
                unitsCount,
                0
            );

            // the real units: name-1, name-2, ...
            for (int i = 1; i <= unitsCount; i++)
            {
                var unitNum = i;
                var finalUnitName = unitName + "-" + i;

                _units.Add(
                    owner.Id,
                    parentId,
                    finalUnitName,
                    city,
                    district,
                    unitType,
                    unitsCount,
                    unitNum
                );
            }

            UnitNameBox.Text = "";
            CityBox.Text = "";
            DistrictBox.Text = "";
            UnitTypeBox.SelectedIndex = 0;
            UnitsCountBox.Text = "1";

            LoadUnits();

            _ = SyncAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private async Task SyncUnitsFromCloudAsync()
    {
        try
        {
            var cloudUnits = new CloudUnitsRealEstateService(_supabaseService);
            var cloudRows = await cloudUnits.GetUnitsAsync();

            // buildings first, so a unit can always find its parent locally
            foreach (var row in cloudRows.OrderBy(r => r.ParentId == 0 ? 0 : 1))
            {
                var ownerLocalId = _owners.GetLocalIdByCloudId(row.OwnerId);

                if (ownerLocalId == 0)
                    continue;

                var parentLocalId = row.ParentId == 0
                    ? 0
                    : _units.GetLocalIdByCloudId(row.ParentId);

                _units.UpsertFromCloud(
                    row.Id,
                    ownerLocalId,
                    parentLocalId,
                    row.UnitName,
                    row.City,
                    row.District,
                    row.UnitType,
                    row.UnitState,
                    row.UnitsCount,
                    row.UnitNum
                );
            }

            LoadUnits();
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping units cloud sync.");
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

        _ = SyncAsync();
    }

    // (4) CHANGED: double-tap opens the window for any row.
    // A building shows its units there; a unit shows its own details.
    private void UnitsGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (UnitsGrid.SelectedItem is not UnitRealEstate unit)
            return;

        var window = new UnitsWindowViewRealEstate(unit.Id, _supabaseService);
        window.Show();
    }
}