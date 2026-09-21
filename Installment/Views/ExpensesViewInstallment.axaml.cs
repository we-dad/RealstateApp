using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
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
        // Search box above the grid: shows the rows that contain every word typed.
        _gridSearch = new GridSearch<ExpensesInstallment>(ExpensesGrid, ExpensesGridSearchBox);
        _supabaseService = supabaseService;

        _db.Initialize();

        _expensesDB = new ExpensesServiceInstallment(_db);
        _productsDB = new ProductServiceInstallment(_db);
        _pdfService = new PdfServiceInstallment();

        ExpensesGrid.DoubleTapped += ExpensesGrid_DoubleTapped;

        Refresh();

        // Reload the grid (only) when data changes: an add, an edit or a delete, also
        // from the details window, so there is no need to press "تحديث".
        _autoRefresh = new ScreenAutoRefresh(
            this,
            LoadExpenses,
            periodicSync: PeriodicSyncAsync,
            interval: TimeSpan.FromMinutes(2));

        _sync = new InstallmentSyncService(_db, _supabaseService);
        _ = SyncAsync();
    }

    // keepSelection is true only for the periodic sync, which reloads this list while
    // the user may be filling the form: the chosen item stays chosen, or nothing is
    // selected if it is gone (never a silent jump to another item). Everything else
    // (open, the refresh button, after an add) keeps the old behaviour: first item selected.
    private void LoadProducts(bool keepSelection = false)
    {
        var selectedId = (ProductBox.SelectedItem as ProductInstallment)?.Id;
        var products = _productsDB.GetAll();

        ProductBox.ItemsSource = products;

        if (keepSelection && selectedId is long id)
        {
            ProductBox.SelectedItem = products.FirstOrDefault(x => x.Id == id);
            return;
        }

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

    // Every reload (open, add, pull, timer) keeps the selected row selected, chosen at
    // the moment the grid is replaced, so a row picked while a sync runs is not undone.
    private void LoadExpenses() =>
        ScreenAutoRefresh.ReloadKeepingSelection<ExpensesInstallment>(ExpensesGrid, r => r.Id, LoadExpensesCore, () => _gridSearch?.AfterLoad());

    private void LoadExpensesCore()
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

    // push must finish before the pull, or the pull re-reads rows the
    // push has not written CloudIds for yet and duplicates them
    //
    // One sync at a time for this screen (static: also across an old and a new instance
    // of the screen). Two overlapping pulls could both insert the same new cloud row.
    // A request from the user (opening the screen, the refresh button) WAITS for its
    // turn, so it is never lost.
    private static readonly SemaphoreSlim _syncGate = new SemaphoreSlim(1, 1);

    private async Task SyncAsync()
    {
        await _syncGate.WaitAsync();

        try
        {
            await _sync.PushAllDirtyAsync();
            await SyncExpensesFromCloudAsync();
        }
        finally
        {
            _syncGate.Release();
        }
    }

    // Every 2 minutes (while the app is active); skipped while a sync is already running.
    private Task PeriodicSyncAsync() =>
        _syncGate.CurrentCount == 0 ? Task.CompletedTask : SyncAsync();

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
            LoadProducts(keepSelection: true);

            SyncStatusService.ReportPull(true);
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping installment expenses cloud sync.");
            SyncStatusService.ReportPull(false, offline: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            SyncStatusService.ReportPull(false);
        }
    }

    private ScreenAutoRefresh? _autoRefresh;
    private GridSearch<ExpensesInstallment>? _gridSearch;

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        Refresh();

        _ = SyncAsync();

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

    private void ExpensesGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (ExpensesGrid.SelectedItem is ExpensesInstallment expense)
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