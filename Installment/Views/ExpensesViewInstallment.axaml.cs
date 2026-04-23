using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class ExpensesViewInstallment : UserControl
{
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly ExpensesServiceInstallment _ExpensesDB;
    private readonly ProductServiceInstallment _productsDB;
    private PdfServiceInstallment _pdfService;

    public ExpensesViewInstallment()
    {
        InitializeComponent();

        _db.Initialize();

        _ExpensesDB = new ExpensesServiceInstallment(_db);
        _productsDB = new ProductServiceInstallment(_db);
        _pdfService = new PdfServiceInstallment();

        Refresh();
    }

    private void LoadUnits()
    {
        var Products = _productsDB.GetAll();

        ProductBox.ItemsSource = Products;

        if (Products.Count > 0)
            ProductBox.SelectedIndex = 0;
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

            if (ProductBox.SelectedItem is not UnitRealEstate unit)
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
        if (sender is Button btn && btn.Tag is ExpensesRealEstate expenses)
            new ExpensesWindowViewRealEstate(expenses.Id).Show();
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

        // _pdfService.GenerateExpensesPdf(expenses, path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
