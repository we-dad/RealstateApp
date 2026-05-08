using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class ContractViewInstallment : UserControl
{
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly ContractServiceInstallment _contractsDB;
    private readonly CustomerServiceInstallment _customersDB;
    private readonly ProductServiceInstallment _productDB;
    private readonly PdfServiceInstallment _pdfService;
    private readonly SupabaseService _supabaseService;
    private readonly InstallmentSyncService _sync;

    private ContractInstallment _contract;
    private CustomerInstallment? _selectedCutomer;
    private TextBox? _customerIdSearchBox;
    private string? ContractNumber;
    private bool _isRefreshing;
    private double realMainPrice;

    public ContractViewInstallment(SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _contract = new ContractInstallment();
        DataContext = _contract;

        _db.Initialize();

        _contractsDB = new ContractServiceInstallment(_db);
        _customersDB = new CustomerServiceInstallment(_db);
        _productDB = new ProductServiceInstallment(_db);
        _pdfService = new PdfServiceInstallment();

        Refresh();

        _customerIdSearchBox = this.FindControl<TextBox>("CustomerIdSearchBox");

        _sync = new InstallmentSyncService(_db, _supabaseService);
        _ = _sync.PushAllDirtyAsync();
        _ = SyncContractsFromCloudAsync();
    }

    private void LoadProducts()
    {
        var products = _productDB.GetAll();

        ProductsBox.ItemsSource = products;

        if (products.Count > 0)
            ProductsBox.SelectedIndex = 0;
    }

    private void LoadContract()
    {
        try
        {
            _contractsDB.UpdateContractStates();

            var data = _contractsDB.GetAll();

            ContractGrid.ItemsSource = null;
            ContractGrid.ItemsSource = data;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
    
    private void ProductsBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ProductsBox.SelectedItem is ProductInstallment product)
        {
            _contract.ProductId = product.Id;
            _contract.ProductName = product.ProductName;
            _contract.ProductMainPrice = product.ProductMainPrice;
            realMainPrice = product.ProductMainPrice;

            UpdateProductTotalAmount();
        }
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var contractNumber = ContractNumber?.Trim() ?? "";
            var contractDateStart = ContractDateStartPicker.SelectedDate?.LocalDateTime ?? DateTime.Today;

            if (!int.TryParse(ContractPeriodBox.Text, out var period))
                period = 0;

            _contract.ContractPeriod = period;
            var contractDateEnd = contractDateStart.AddMonths(period);

            if (ProductsBox.SelectedItem is not ProductInstallment product)
                return;

            if (_selectedCutomer == null)
            {
                CustomerInfoText.Text = "يرجى اختيار العميل أولاً";
                CustomerInfoText.Foreground = Brushes.Red;
                return;
            }

            var customer = _selectedCutomer;

            _contractsDB.Add(
                contractNumber,
                contractDateStart,
                contractDateEnd,
                _contract.MainTotalAmount,
                _contract.MainTotalAmount,
                _contract.ContractPeriod,
                _contract.DownPayment,
                _contract.MonthlyInstallment,
                (float)_contract.ManagementFee,
                (float)_contract.InterestPercent,
                product.Id,
                customer.Id
            );

            Refresh();

            _ = _sync.PushAllDirtyAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            CustomerInfoText.Text = ex.Message;
            CustomerInfoText.Foreground = Brushes.Red;
        }
    }

    private void SearchCustomer_Click(object? sender, RoutedEventArgs e)
    {
        var id = _customerIdSearchBox?.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(id))
            return;

        var customer = _customersDB.FindByIdentity(id);

        if (customer is null)
        {
            _selectedCutomer = null;
            CustomerInfoText.Text = "لم يتم العثور على عميل بهذا الرقم";
            CustomerInfoText.Foreground = Brushes.Red;
            return;
        }

        _selectedCutomer = customer;

        CustomerInfoText.Text =
            $"اسم العميل : {customer.Name} | رقم الهوية/الإقامة : {customer.IdentityNumber}";

        CustomerInfoText.Foreground = Brushes.Green;
    }

    private async Task SyncContractsFromCloudAsync()
    {
        try
        {
            var cloud = new CloudContractsInstallmentService(_supabaseService);
            var rows = await cloud.GetContractsAsync();

            foreach (var row in rows)
            {
                var productLocalId = _productDB.GetLocalIdByCloudId(row.ProductId);
                var customerLocalId = _customersDB.GetLocalIdByCloudId(row.CustomerId);

                if (productLocalId == 0 || customerLocalId == 0)
                    continue;

                _contractsDB.UpsertFromCloud(
                    row.Id,
                    row.ContractNumber,
                    row.ContractStartDate,
                    row.ContractEndDate,
                    row.MainTotalAmount,
                    row.CurrentTotalAmount,
                    row.ContractPeriod,
                    row.DownPayment,
                    row.MonthlyInstallment,
                    row.ManagementFee,
                    row.InterestPercent,
                    row.ContractState,
                    productLocalId,
                    customerLocalId
                );
            }

            LoadContract();
            LoadProducts();
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping installment contracts cloud sync.");
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
        _ = SyncContractsFromCloudAsync();
    }

    private void Refresh()
    {
        try
        {
            _isRefreshing = true;

            LoadContract();
            LoadProducts();

            ManagementFeeBox.Text = "0";
            ContractPeriodBox.Text = "1";
            DownPaymentBox.Text = "0";
            CustomerIdSearchBox.Text = "";
            CustomerInfoText.Text = "";
            CustomerInfoText.Foreground = Brushes.Black;

            _selectedCutomer = null;

            ContractNumber = _contractsDB.GenerateContractNumber();
            ContractNumBox.Text = ContractNumber;

            ContractDateStartPicker.SelectedDate = DateTime.Today;
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void OpenInfoWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ContractInstallment contract)
            new ContractWindowViewInstallment(contract.Id, _supabaseService).Show();
    }

    private void UpdateProductTotalAmount()
    {
        double total =
            _contract.ProductMainPrice + _contract.ManagementFee +
            (((_contract.ProductMainPrice * _contract.InterestPercent / 100.0) / 12) * (_contract.ContractPeriod));

        _contract.MainTotalAmount = Math.Round(total, 2);

        UpdateInstallmentAfterTotalChanged();
    }

    private void UpdateInstallmentAfterTotalChanged()
    {
        if (!string.IsNullOrWhiteSpace(ContractPeriodBox.Text) &&
            double.TryParse(ContractPeriodBox.Text, out double period) &&
            period > 0)
        {
            _contract.ContractPeriod = period;
            _contract.MonthlyInstallment = Math.Round(_contract.MainTotalAmount / period, 2);
            MonthlyInstallmentBox.Text = _contract.MonthlyInstallment.ToString("0.##");
        }
        else if (!string.IsNullOrWhiteSpace(MonthlyInstallmentBox.Text) &&
                 double.TryParse(MonthlyInstallmentBox.Text, out double monthly) &&
                 monthly > 0)
        {
            _contract.MonthlyInstallment = monthly;
            _contract.ContractPeriod = Math.Ceiling(_contract.MainTotalAmount / monthly);
            ContractPeriodBox.Text = _contract.ContractPeriod.ToString("0");
        }
    }

    private void InterestPercentBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing) return;

        if (InterestPercentBox.SelectedItem != null &&
            double.TryParse(InterestPercentBox.SelectedItem.ToString(), out double value))
        {
            _contract.InterestPercent = value;
            UpdateProductTotalAmount();
        }
    }

    private void ManagementFeeBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isRefreshing) return;

        var text = ManagementFeeBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(text))
        {
            ManagementFeeErrorText.Text = "عليك وضع قيمة هنا";
            _contract.ManagementFee = 0;
            return;
        }

        if (double.TryParse(text, out double value))
        {
            ManagementFeeErrorText.Text = "";
            _contract.ManagementFee = value;
            UpdateProductTotalAmount();
        }
        else
        {
            ManagementFeeErrorText.Text = "قيمة غير صحيحة";
        }
    }

    private void ContractPeriodBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isRefreshing) return;

        var text = ContractPeriodBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(text))
        {
            ContractPeriodErrorText.Text = "";
            _contract.ContractPeriod = 0;
            MonthlyInstallmentBox.Text = "0";
            _contract.MonthlyInstallment = 0;
            return;
        }

        if (double.TryParse(text, out double value) && value > 0)
        {
            ContractPeriodErrorText.Text = "";
            _contract.ContractPeriod = value;

            double monthly = _contract.MainTotalAmount / value;
            _contract.MonthlyInstallment = Math.Round(monthly, 2);

            MonthlyInstallmentBox.Text = _contract.MonthlyInstallment.ToString("0.##");
            UpdateProductTotalAmount();
        }
        else
        {
            ContractPeriodErrorText.Text = "قيمة غير صحيحة";
        }
    }

    private void DownPaymentBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isRefreshing) return;

        var text = DownPaymentBox.Text?.Trim() ?? "";

        if (!_contract.BoolDownPayment)
        {
            DownPaymentErrorText.Text = "";
            _contract.DownPayment = 0;
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            DownPaymentErrorText.Text = "عليك وضع قيمة هنا";
            _contract.DownPayment = 0;
            return;
        }

        if (double.TryParse(text, out double value))
        {
            DownPaymentErrorText.Text = "";
            _contract.DownPayment = value;
            _contract.ProductMainPrice = realMainPrice - _contract.DownPayment;

            if (_contract.ProductMainPrice < 0)
                _contract.ProductMainPrice = 0;

            UpdateProductTotalAmount();
        }
        else
        {
            DownPaymentErrorText.Text = "قيمة غير صحيحة";
        }
    }

    private async Task<string?> PickSavePdfPathAsync(string contractNumber, string title, string fileName)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel is null)
            return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = $"{fileName + contractNumber}.pdf",
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
        if (sender is not Button btn || btn.Tag is not ContractInstallment contract)
            return;

        ContractInstallment? contractPdfData = _contractsDB.GetById(contract.Id);

        if (contractPdfData is null)
            return;

        var path = await PickSavePdfPathAsync(contractPdfData.ContractNumber, "حفظ العقد (PDF)", "");

        if (string.IsNullOrWhiteSpace(path))
            return;

        _pdfService.GenerateContractInstallmentPdf(contractPdfData, path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private async void PrintSanadAmr_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not ContractInstallment contract)
            return;

        ContractInstallment? contractPdfData = _contractsDB.GetById(contract.Id);

        if (contractPdfData is null)
            return;

        var path = await PickSavePdfPathAsync(contractPdfData.ContractNumber, "حفظ سند لأمر (PDF)", "SanadAmr_");

        if (string.IsNullOrWhiteSpace(path))
            return;

        _pdfService.GenerateSanadLeAmrPdf(contractPdfData, path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}