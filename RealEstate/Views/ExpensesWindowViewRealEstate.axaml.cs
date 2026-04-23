using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class ExpensesWindowViewRealEstate : Window
{
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly ExpensesServiceRealEstate _ExpensesDB;
    private readonly UnitServiceRealEstate _unitsDB;
    private PdfServiceRealEstate _pdfServiceRealEstate;

    private readonly long _expensesID;
    private ExpensesRealEstate? _expenses;

    public ExpensesWindowViewRealEstate(long expensesID)
    {
        InitializeComponent();

        _db.Initialize();

        _ExpensesDB = new ExpensesServiceRealEstate(_db);
        _unitsDB = new UnitServiceRealEstate(_db);
        _pdfServiceRealEstate = new PdfServiceRealEstate();

        _expensesID = expensesID;

        Refresh();
    }

    private void LoadUnits()
    {
        var Units = _unitsDB.GetAll();

        UnitsBox.ItemsSource = Units;

        UnitsBox.SelectedItem = Units.FirstOrDefault(u => u.Id == _expenses?.UnitId);
    }
    private void LoadExpensesService()
    {
        ExpensesServiceBox.ItemsSource = new List<string>
        {
            "تكييف",
            "نظافة",
            "صيانة",
            "أخرى"
        };

        ExpensesServiceBox.SelectedIndex = 0;
    }

    private void Update_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var ExpensesNum = ExpensesNumBox.Text?.Trim() ?? "";
            var ExpensesDate = ExpensesDateBox.SelectedDate?.LocalDateTime
                     ?? DateTime.Today;

            if (UnitsBox.SelectedItem is not UnitRealEstate unit)
                return;

            var ExpensesService = ExpensesServiceBox.SelectedItem as string ?? "أخرى";

            var ExpensesAmount = double.Parse(
                ExpensesAmountBox.Text?.Trim() ?? "",
                CultureInfo.InvariantCulture
            );

            var ExpensesNote = ExpensesNoteBox.Text?.Trim() ?? "";

            _ExpensesDB.Update(_expensesID, ExpensesNum, ExpensesDate, unit.Id, ExpensesService, ExpensesAmount, ExpensesNote);

            Refresh();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
    private void Refresh()
    {

        _expenses = _ExpensesDB.GetById(_expensesID);
        if (_expenses is null) return;

        var _unit = _unitsDB.GetById(_expenses.UnitId);
        if (_unit is null) return;

        LoadUnits();
        LoadExpensesService();

        ExpensesNumBox.Text = _expenses.ExpensesNumber;
        ExpensesDateBox.SelectedDate = _expenses.ExpensesDate;
        ExpensesAmountBox.Text = _expenses.ExpensesAmount.ToString();
        ExpensesNoteBox.Text = _expenses.ExpensesNote;
        ExpensesServiceBox.SelectedItem = _expenses.ExpensesService;

        //Expenses Info
        ResultExpensesNumBox.Text = _expenses.ExpensesNumber;
        ResultExpensesDateBox.Text = _expenses.ExpensesDate.ToString("yyyy-MM-dd");
        ResultExpensesServiceBox.Text = _expenses.ExpensesService;
        ResultExpensesAmountBox.Text = _expenses.ExpensesAmount.ToString();
        ResultExpensesNoteBox.Text = _expenses.ExpensesNote;

        //Unit Info
        ResultUnitNameBox.Text = _unit.UnitName;
        ResultDistrictBox.Text = _unit.District;
        ResultCityBox.Text = _unit.City;
        ResultUnitTypeBox.Text = _unit.UnitType;
        ResultUnitNumBox.Text = _unit.UnitNum.ToString() + " / " + _unit.UnitsCount.ToString();

    }

    private async Task<string?> PickSavePdfPathAsync(string contractNumber)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "حفظ السند  (PDF)",
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
        if (sender is not Button btn || btn.Tag is not ExpensesRealEstate expenses) return;

        ExpensesRealEstate? _expenses = _ExpensesDB.GetById(expenses.Id);
        if (_expenses is null) return;

        var path = await PickSavePdfPathAsync(_expenses.ExpensesNumber);
        if (string.IsNullOrWhiteSpace(path))
            return;

        _pdfServiceRealEstate.GenerateExpensesPdf(expenses, path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
    private async void Delete_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            _ExpensesDB.Delete(_expensesID);
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
