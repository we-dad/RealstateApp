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

public partial class ExpensesWindowViewInstallment : Window
{
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly ExpensesServiceInstallment _expensesDB;
    private readonly ProductServiceInstallment _productsDB;
    private readonly PdfServiceInstallment _pdfServiceInstallment;
    private readonly SupabaseService _supabaseService;
    private readonly InstallmentSyncService _sync;

    private readonly long _expensesID;
    private ExpensesInstallment? _expenses;

    public ExpensesWindowViewInstallment(long expensesID, SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();

        _expensesDB = new ExpensesServiceInstallment(_db);
        _productsDB = new ProductServiceInstallment(_db);
        _pdfServiceInstallment = new PdfServiceInstallment();

        _expensesID = expensesID;

        Refresh();
    }

    private void LoadUnits()
    {
        var products = _productsDB.GetAll();

        ProductBox.ItemsSource = products;
        ProductBox.SelectedItem = products.FirstOrDefault(p => p.Id == _expenses?.ProductId);
    }

    private void LoadExpensesService()
    {
        ExpensesServiceBox.ItemsSource = new List<string>
        {
            "أخرى"
        };

        ExpensesServiceBox.SelectedIndex = 0;
    }

    private async Task PushDirtyExpensesAsync()
    {
        try
        {
            var cloudExpenses = new CloudExpensesRealEstateService(_supabaseService);
            var dirtyRows = _expensesDB.GetDirtyRows();

            foreach (var expense in dirtyRows)
            {
                if (expense.SyncAction == "delete")
                {
                    if (expense.CloudId > 0)
                        await cloudExpenses.DeleteExpenseAsync(expense.CloudId);

                    _expensesDB.DeleteLocalPermanent(expense.Id);
                }
                else if (expense.SyncAction == "insert")
                {
                    if (expense.ProductCloudId <= 0)
                        continue;

                    var cloudId = await cloudExpenses.AddExpenseAsync(new ExpenseRealEstateRow
                    {
                        ExpensesNumber = expense.ExpensesNumber,
                        ExpensesDate = expense.ExpensesDate,
                        UnitId = expense.ProductCloudId,
                        ExpensesService = expense.ExpensesService,
                        ExpensesAmount = expense.ExpensesAmount,
                        ExpensesNote = expense.ExpensesNote
                    });

                    _expensesDB.UpdateCloudId(expense.Id, cloudId);
                }
                else if (expense.SyncAction == "update")
                {
                    if (expense.CloudId <= 0 || expense.ProductCloudId <= 0)
                        continue;

                    await cloudExpenses.UpdateExpenseAsync(expense.CloudId, new ExpenseRealEstateRow
                    {
                        ExpensesNumber = expense.ExpensesNumber,
                        ExpensesDate = expense.ExpensesDate,
                        UnitId = expense.ProductCloudId,
                        ExpensesService = expense.ExpensesService,
                        ExpensesAmount = expense.ExpensesAmount,
                        ExpensesNote = expense.ExpensesNote
                    });

                    _expensesDB.MarkSynced(expense.Id);
                }
            }
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: dirty expenses will sync later.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void Update_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var expensesNum = ExpensesNumBox.Text?.Trim() ?? "";
            var expensesDate = ExpensesDateBox.SelectedDate?.LocalDateTime ?? DateTime.Today;

            if (ProductBox.SelectedItem is not ProductInstallment product)
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
                product.Id,
                expensesService,
                expensesAmount,
                expensesNote
            );

            Refresh();

            _ = PushDirtyExpensesAsync();
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

        var product = _productsDB.GetById(_expenses.ProductId);

        if (product is null)
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

        ResultProductNameBox.Text = product.ProductName;
        ResultProductTypeBox.Text = product.ProductType;
        ResultProductMainPriceBox.Text = product.ProductMainPrice.ToString("0.##");
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

        _pdfServiceInstallment.GenerateExpensesPdf(freshExpense, path);

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

            _ = PushDirtyExpensesAsync();

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