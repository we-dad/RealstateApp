using Avalonia.Controls;
using Avalonia.Interactivity;
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

public partial class ExpensesViewRealEstate : UserControl
{
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly ExpensesServiceRealEstate _expensesDB;
    private readonly UnitServiceRealEstate _unitsDB;
    private readonly PdfServiceRealEstate _pdfServiceRealEstate;
    private readonly SupabaseService _supabaseService;
    private readonly RealEstateSyncService _sync;

    public ExpensesViewRealEstate(SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();

        _expensesDB = new ExpensesServiceRealEstate(_db);
        _unitsDB = new UnitServiceRealEstate(_db);
        _pdfServiceRealEstate = new PdfServiceRealEstate();
        _sync = new RealEstateSyncService(_db, _supabaseService);

        Refresh();

        _ = _sync.PushAllDirtyAsync();
        _ = SyncExpensesFromCloudAsync();
    }

    private void LoadUnits()
    {
        var units = _unitsDB.GetAll();

        UnitsBox.ItemsSource = units;

        if (units.Count > 0)
            UnitsBox.SelectedIndex = 0;
    }

    private void LoadExpensesService()
    {
        ExpensesServiceBox.ItemsSource = new List<string>
        {
            "تكييف",
            "نظافة",
            "صيانة",
            "ماء",
            "كهرباء",
            "أخرى"
        };

        ExpensesServiceBox.SelectedIndex = 0;
    }

    private void LoadExpenses()
    {
        try
        {
            var data = _expensesDB.GetAll();

            ExpensesGrid.ItemsSource = null;
            ExpensesGrid.ItemsSource = data;
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
            var expensesNum = ExpensesNumBox.Text?.Trim() ?? "";
            var expensesDate = DateTime.Today;

            if (UnitsBox.SelectedItem is not UnitRealEstate unit)
                return;

            var expensesService = ExpensesServiceBox.SelectedItem as string ?? "أخرى";

            var expensesAmount = double.Parse(
                ExpensesAmountBox.Text?.Trim() ?? "0",
                CultureInfo.InvariantCulture
            );

            var expensesNote = ExpensesNoteBox.Text?.Trim() ?? "";

            _expensesDB.Add(
                expensesNum,
                expensesDate,
                unit.Id,
                expensesService,
                expensesAmount,
                expensesNote
            );

            Refresh();

            _ = _sync.PushAllDirtyAsync();
            
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private async Task SyncExpensesFromCloudAsync()
    {
        if (!AppSession.CanReadOnline)
            return;

        try
        {
            var cloudExpenses = new CloudExpensesRealEstateService(_supabaseService);
            var cloudRows = await cloudExpenses.GetExpensesAsync();

            foreach (var row in cloudRows)
            {
                var unitLocalId = _unitsDB.GetLocalIdByCloudId(row.UnitId);

                if (unitLocalId == 0)
                    continue;

                _expensesDB.UpsertFromCloud(
                    row.Id,
                    row.ExpensesNumber,
                    row.ExpensesDate,
                    unitLocalId,
                    row.ExpensesService,
                    row.ExpensesAmount,
                    row.ExpensesNote
                );
            }

            LoadExpenses();
            LoadUnits();
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping expenses cloud sync.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        Refresh();

        _ = _sync.PushAllDirtyAsync();
        _ = SyncExpensesFromCloudAsync();
    }

    private void Refresh()
    {
        LoadExpenses();
        LoadUnits();
        LoadExpensesService();

        ExpensesNumBox.Text = _expensesDB.GenerateExpensesNumber();
        ExpensesDateBox.Text = DateTime.Today.ToString("yyyy-MM-dd");
        ExpensesAmountBox.Text = "";
        ExpensesNoteBox.Text = "";
    }

    private void OpenInfoWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ExpensesRealEstate expenses)
            new ExpensesWindowViewRealEstate(expenses.Id, _supabaseService).Show();
    }

    private async Task<string?> PickSavePdfPathAsync(string expensesNumber)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel is null)
            return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "حفظ السند  (PDF)",
                SuggestedFileName = $"{expensesNumber}.pdf",
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
        if (sender is not Button btn || btn.Tag is not ExpensesRealEstate expenses)
            return;

        ExpensesRealEstate? selectedExpense = _expensesDB.GetById(expenses.Id);

        if (selectedExpense is null)
            return;

        var path = await PickSavePdfPathAsync(selectedExpense.ExpensesNumber);

        if (string.IsNullOrWhiteSpace(path))
            return;

        _pdfServiceRealEstate.GenerateExpensesPdf(selectedExpense, path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}