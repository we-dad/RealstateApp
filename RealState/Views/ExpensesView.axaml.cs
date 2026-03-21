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

public partial class ExpensesView : UserControl
{
    private readonly DbService _db = new DbService();
    private readonly ExpensesService _ExpensesDB;
    private readonly UnitService _unitsDB;
    private PdfService _pdfService;

    public ExpensesView()
    {
        InitializeComponent();

        _db.Initialize();

        _ExpensesDB = new ExpensesService(_db);
        _unitsDB = new UnitService(_db);
        _pdfService = new PdfService();

        Refresh();
    }

    private void LoadUnits()
    {
        var Units = _unitsDB.GetAll();

        UnitsBox.ItemsSource = Units;

        if (Units.Count > 0)
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

        var data = _ExpensesDB.GetAll();

        ExpensesGrid.ItemsSource = null;
        ExpensesGrid.ItemsSource = data;
    }
    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var ExpensesNum = ExpensesNumBox.Text?.Trim() ?? "";
            var ExpensesDate = DateTime.Today;

            if (UnitsBox.SelectedItem is not Unit unit)
                return;

            var ExpensesService = ExpensesServiceBox.SelectedItem as string ?? "أخرى";

            var ExpensesAmount = double.Parse(
                ExpensesAmountBox.Text?.Trim() ?? "",
                CultureInfo.InvariantCulture
            );

            var ExpensesNote = ExpensesNoteBox.Text?.Trim() ?? "";

            _ExpensesDB.Add(ExpensesNum, ExpensesDate, unit.Id, ExpensesService, ExpensesAmount, ExpensesNote);

            Refresh();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }


    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        Refresh();
    }
    private void Refresh()
    {
        LoadExpenses();
        LoadUnits();
        LoadExpensesService();

        ExpensesNumBox.Text = _ExpensesDB.GenerateExpensesNumber();
        ExpensesDateBox.Text = DateTime.Today.ToString("yyyy-MM-dd");
        ExpensesAmountBox.Text = "";
        ExpensesNoteBox.Text = "";

    }
    private void OpenInfoWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Expenses expenses)
            new ExpensesWindowView(expenses.Id).Show();
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
        if (sender is not Button btn || btn.Tag is not Expenses expenses) return;

        Expenses? _expenses = _ExpensesDB.GetById(expenses.Id);
        if (_expenses is null) return;

        var path = await PickSavePdfPathAsync(_expenses.ExpensesNumber);
        if (string.IsNullOrWhiteSpace(path))
            return;

        _pdfService.GenerateExpensesPdf(expenses, path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
