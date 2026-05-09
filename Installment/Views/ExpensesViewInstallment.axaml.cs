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

public partial class ExpensesViewInstallment : UserControl
{
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly ExpensesServiceInstallment _expensesDB;
    private readonly ProductServiceInstallment _productsDB;
    private readonly PdfServiceInstallment _pdfService;
    private readonly SupabaseService _supabaseService;
    private readonly InstallmentSyncService _sync;

    public ExpensesViewInstallment(SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();

        _expensesDB = new ExpensesServiceInstallment(_db);
        _productsDB = new ProductServiceInstallment(_db);
        _pdfService = new PdfServiceInstallment();

        Refresh();

        _sync = new InstallmentSyncService(_db, _supabaseService);
        _ = _sync.PushAllDirtyAsync();
        _ = SyncExpensesFromCloudAsync();
    }

    private void LoadProducts()
    {
        var products = _productsDB.GetAll();

        ProductBox.ItemsSource = products;

        if (products.Count > 0)
            ProductBox.SelectedIndex = 0;
    }

    private void LoadExpensesService()
    {
        ExpensesServiceBox.ItemsSource = new List<string>
        {
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

            if (ProductBox.SelectedItem is not ProductInstallment product)
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
                product.Id,
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
            var cloudExpenses = new CloudExpensesInstallmentService(_supabaseService);
            var rows = await cloudExpenses.GetExpensesAsync();

            foreach (var row in rows)
            {
                var productLocalId = _productsDB.GetLocalIdByCloudId(row.ProductId);

                if (productLocalId == 0)
                    continue;

                _expensesDB.UpsertFromCloud(
                    row.Id,
                    row.ExpensesNumber,
                    row.ExpensesDate,
                    productLocalId,
                    row.ExpensesService,
                    row.ExpensesAmount,
                    row.ExpensesNote
                );
            }

            LoadExpenses();
            LoadProducts();
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping installment expenses cloud sync.");
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
        LoadProducts();
        LoadExpensesService();

        ExpensesNumBox.Text = _expensesDB.GenerateExpensesNumber();
        ExpensesDateBox.Text = DateTime.Today.ToString("yyyy-MM-dd");
        ExpensesAmountBox.Text = "";
        ExpensesNoteBox.Text = "";
    }

    private void OpenInfoWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ExpensesInstallment expense)
            new ExpensesWindowViewInstallment(expense.Id, _supabaseService).Show();
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
        if (sender is not Button btn || btn.Tag is not ExpensesInstallment expenses)
            return;

        ExpensesInstallment? selectedExpense = _expensesDB.GetById(expenses.Id);

        if (selectedExpense is null)
            return;

        var path = await PickSavePdfPathAsync(selectedExpense.ExpensesNumber);

        if (string.IsNullOrWhiteSpace(path))
            return;

        _pdfService.GenerateExpensesPdf(selectedExpense, path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}