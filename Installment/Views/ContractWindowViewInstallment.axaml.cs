using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Services;


namespace RealEstateInstallmentsManager.Views;

public partial class ContractWindowViewInstallment : Window
{
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly ContractServiceInstallment _contractsDB;
    private readonly CustomerServiceInstallment _customersDB;
    private readonly ProductServiceInstallment _productDB;
    private PdfServiceInstallment _pdfService;

    private ContractInstallment _contract;
    private long _contractID;
    private CustomerInstallment? _selectedCutomer;
    private TextBox? _customerIdSearchBox;
    private string? ContractNumber;
    private bool _isRefreshing;
    private double realMainPrice;


    public ContractWindowViewInstallment(long contract)
    {
        InitializeComponent();
        
        _contractID = contract;

        _db.Initialize();

        _contractsDB = new ContractServiceInstallment(_db);
        _customersDB = new CustomerServiceInstallment(_db);
        _productDB = new ProductServiceInstallment(_db);
        _pdfService = new PdfServiceInstallment();

        Refresh();

        _customerIdSearchBox = this.FindControl<TextBox>("CustomerIdSearchBox"); //this line becasue avalonia can't found TenantIdSearchBox it's returen null maybe because the warning message
    }

    private void LoadProducts()
    {
        var products = _productDB.GetAll();

        ProductsBox.ItemsSource = products;
        ProductsBox.SelectedItem = products.FirstOrDefault(p => p.Id == _contract.ProductId);
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
    private void Update_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var contractNumber = ContractNumber?.Trim() ?? "";

            var contractDateStart = ContractDateStartPicker.SelectedDate?.LocalDateTime ?? DateTime.Today;
            var contractDateEnd =  contractDateStart.AddMonths((int)_contract.ContractPeriod);

            if (ProductsBox.SelectedItem is not ProductInstallment product)
                return;

            if (_selectedCutomer == null)
            {
                CustomerInfoText.Text = "يرجى اختيار العميل أولاً";
                return;
            }

            var customerId = _selectedCutomer.Id;

            _contractsDB.Update(
                _contractID,
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
                customerId
            );

            Refresh();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            CustomerInfoText.Text = ex.Message;
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
            CustomerInfoText.Text = "لم يتم العثور على مستأجر بهذا الرقم";
            CustomerInfoText.Foreground = Brushes.Red;
            return;
        }

        _selectedCutomer = customer;
        var _customerInfo = $"اسم المستأجر : {customer.Name} | ";
        var _customerID = $"رقم الهوية/الإقامة : {customer.IdentityNumber}";
        CustomerInfoText.Text = _customerInfo + _customerID;
        CustomerInfoText.Foreground = Brushes.Green;
    }
    private void Refresh()
    {
       try
    {
        _isRefreshing = true;

        _contract = _contractsDB.GetById(_contractID);
        if (_contract == null)
            return;

        Console.WriteLine("GetById loaded ContractPeriod = " + _contract.ContractPeriod);

        DataContext = _contract;

        LoadProducts();

        ContractNumBox.Text = _contract.ContractNumber;
        ContractDateStartPicker.SelectedDate = _contract.ContractStartDate;
        InterestPercentBox.SelectedItem = _contract.InterestPercent;
        ManagementFeeBox.Text = _contract.ManagementFee.ToString("0.##");
        ContractPeriodBox.Text = _contract.ContractPeriod.ToString("0.##");
        DownPaymentBox.Text = _contract.DownPayment.ToString("0.##");
        MonthlyInstallmentBox.Text = _contract.MonthlyInstallment.ToString("0.##");
        CustomerIdSearchBox.Text = _contract.CustomerIdentityNumber;
        CustomerInfoText.Text = "";

        ResultContractNumBox.Text = _contract.ContractNumber;
        ResultContractDateStartBox.Text = _contract.ContractStartDate.ToString("yyyy-MM-dd");
        ResultContractDateEndBox.Text = _contract.ContractEndDate.ToString("yyyy-MM-dd");
        ResultProductTotalAmount.Text = _contract.MainTotalAmount.ToString("0.##");
        ResultInterestPercentBox.Text = _contract.InterestPercent.ToString("0.##");
        ResultContractPeriodBox.Text = _contract.ContractPeriod.ToString("0.##");
        ResultDownPaymentBox.Text = _contract.DownPayment.ToString("0.##");
        ResultManagementFeeBox.Text = _contract.ManagementFee.ToString("0.##");
        ResultMonthlyInstallmentBox.Text = _contract.MonthlyInstallment.ToString("0.##");

        ResultProductNameBox.Text = _contract.ProductName;
        ResultProductTypeBox.Text = _contract.ProductType;
        ResultProductMainPriceBox.Text = _contract.ProductMainPrice.ToString("0.##");

        ResultOwnerNameBox.Text = _contract.OwnerName;
        ResultOwnerIdentityNumberBox.Text = _contract.OwnerIdentityNumber;
        ResultOwnerPhoneBox.Text = _contract.OwnerPhone;
        ResultOwnerAddressBox.Text = _contract.OwnerAddress;

        ResultNameBox.Text = _contract.CustomerName;
        ResultIdentityNumberBox.Text = _contract.CustomerIdentityNumber;
        ResultPhoneBox.Text = _contract.CustomerPhone;
        ResultAddressBox.Text = _contract.CustomerAddress;
        ResultJobBox.Text = _contract.CustomerJob;
        
        SponsorSection.IsVisible =
            !string.IsNullOrWhiteSpace(_contract.CustomerSponserName);
        
        
        ResultSponserNameBox.Text = _contract.CustomerSponserName;
        ResultSponserIdentityNumberBox.Text = _contract.CustomerSponserIdentityNumber;
        ResultSponserPhoneBox.Text = _contract.CustomerSponserPhone;
        ResultSponserAddressBox.Text = _contract.CustomerSponserAddress;
        ResultSponserJobBox.Text = _contract.CustomerSponserJob;
    }
    finally
    {
        _isRefreshing = false;
    }
    }
    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
  
    }
    private void UpdateProductTotalAmount()
    {
        
        double total =
            _contract.ProductMainPrice + _contract.ManagementFee +
            (((_contract.ProductMainPrice * _contract.InterestPercent / 100.0)/12)*(_contract.ContractPeriod));
        
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
            _contract.ProductMainPrice = (realMainPrice - _contract.DownPayment);

            if (_contract.ProductMainPrice < 0)
                _contract.ProductMainPrice = 0;
            
            UpdateProductTotalAmount();
        }
        else
        {
            DownPaymentErrorText.Text = "قيمة غير صحيحة";
        }
    }
    private async Task<string?> PickSavePdfPathAsync(string contractNumber)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "حفظ العقد  (PDF)",
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
        if (sender is not Button btn || btn.Tag is not ContractInstallment contract) return;

        ContractInstallment? _contractPdfData = _contractsDB.GetById(contract.Id);
        if (_contractPdfData is null) return;

        var path = await PickSavePdfPathAsync(_contractPdfData.ContractNumber);
        if (string.IsNullOrWhiteSpace(path))
            return;
        

        _pdfService.GenerateContractInstallmentPdf(_contractPdfData, path);


        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
