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

public partial class ExpensesWindowViewRealEstate : Window
{
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly ExpensesServiceRealEstate _expensesDB;
    private readonly UnitServiceRealEstate _unitsDB;
    private readonly PdfServiceRealEstate _pdfServiceRealEstate;
    private readonly SupabaseService _supabaseService;
    private readonly RealEstateSyncService _sync;

    private readonly long _expensesID;
    private ExpensesRealEstate? _expenses;

    public ExpensesWindowViewRealEstate(long expensesID, SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();

        _expensesDB = new ExpensesServiceRealEstate(_db);
        _unitsDB = new UnitServiceRealEstate(_db);
        _pdfServiceRealEstate = new PdfServiceRealEstate();
        _sync = new RealEstateSyncService(_db, _supabaseService);

        _expensesID = expensesID;

        Refresh();
    }

    private void LoadUnits()
    {
        var units = _unitsDB.GetAll();

        UnitsBox.ItemsSource = units;
        UnitsBox.SelectedItem = units.FirstOrDefault(u => u.Id == _expenses?.UnitId);
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
    
    private void Update_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var expensesNum = ExpensesNumBox.Text?.Trim() ?? "";
            var expensesDate = ExpensesDateBox.SelectedDate?.LocalDateTime ?? DateTime.Today;

            if (UnitsBox.SelectedItem is not UnitRealEstate unit)
                return;

            var expensesService = ExpensesServiceBox.SelectedItem as string ?? "أخرى";

            var expensesAmount = double.Parse(
                ExpensesAmountBox.Text?.Trim() ?? "0",
                CultureInfo.InvariantCulture
            );

            var expensesNote = ExpensesNoteBox.Text?.Trim() ?? "";

            _expensesDB.Update(
                _expensesID,
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

    private void Refresh()
    {
        _expenses = _expensesDB.GetById(_expensesID);

        if (_expenses is null)
            return;

        var unit = _unitsDB.GetById(_expenses.UnitId);

        if (unit is null)
            return;

        LoadUnits();
        LoadExpensesService();

        ExpensesNumBox.Text = _expenses.ExpensesNumber;
        ExpensesDateBox.SelectedDate = _expenses.ExpensesDate;
        ExpensesAmountBox.Text = _expenses.ExpensesAmount.ToString(CultureInfo.InvariantCulture);
        ExpensesNoteBox.Text = _expenses.ExpensesNote;
        ExpensesServiceBox.SelectedItem = _expenses.ExpensesService;

        ResultExpensesNumBox.Text = _expenses.ExpensesNumber;
        ResultExpensesDateBox.Text = _expenses.ExpensesDate.ToString("yyyy-MM-dd");
        ResultExpensesServiceBox.Text = _expenses.ExpensesService;
        ResultExpensesAmountBox.Text = _expenses.ExpensesAmount.ToString(CultureInfo.InvariantCulture);
        ResultExpensesNoteBox.Text = _expenses.ExpensesNote;

        ResultUnitNameBox.Text = unit.UnitName;
        ResultDistrictBox.Text = unit.District;
        ResultCityBox.Text = unit.City;
        ResultUnitTypeBox.Text = unit.UnitType;
        ResultUnitNumBox.Text = unit.UnitNum + " / " + unit.UnitsCount;
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
        if (_expenses is null)
            return;

        var freshExpense = _expensesDB.GetById(_expenses.Id);

        if (freshExpense is null)
            return;

        var path = await PickSavePdfPathAsync(freshExpense.ExpensesNumber);

        if (string.IsNullOrWhiteSpace(path))
            return;

        _pdfServiceRealEstate.GenerateExpensesPdf(freshExpense, path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            _expensesDB.Delete(_expensesID);

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