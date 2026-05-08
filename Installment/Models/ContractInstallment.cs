using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace RealEstateInstallmentsManager.Models;

public class ContractInstallment : INotifyPropertyChanged
{
    public long Id { get; set; }
    public long CloudId { get; set; }
    public long ProductCloudId { get; set; }
    public long CustomerCloudId { get; set; }
    public string SyncAction { get; set; } = "";
    public string ContractNumber { get; set; } = "";
    public DateTime ContractStartDate { get; set; } = DateTime.Now;
    public DateTime ContractEndDate { get; set; } = DateTime.Now;

    public List<double> InterestPercentOptions { get; } = new()
    {
        12.5, 15, 18, 22, 25
    };
    public double CurrentTotalAmount  { get; set; }

    private double _contractPeriod;
    public double ContractPeriod
    {
        get => _contractPeriod;
        set
        {
            if (_contractPeriod != value)
            {
                _contractPeriod = value;
                OnPropertyChanged();
            }
        }
    }

    private double _downPayment = 0;
    public double DownPayment
    {
        get => _downPayment;
        set
        {
            if (_downPayment != value)
            {
                _downPayment = value;
                OnPropertyChanged();
            }
        }
    }

    private double _monthlyInstallment;
    public double MonthlyInstallment
    {
        get => _monthlyInstallment;
        set
        {
            if (_monthlyInstallment != value)
            {
                _monthlyInstallment = value;
                OnPropertyChanged();
            }
        }
    }

    private double _mainTotalAmount;
    public double MainTotalAmount
    {
        get => _mainTotalAmount;
        set
        {
            if (_mainTotalAmount != value)
            {
                _mainTotalAmount = value;
                OnPropertyChanged();
            }
        }
    }

    private double _managementFee = 1;
    public double ManagementFee
    {
        get => _managementFee;
        set
        {
            if (_managementFee != value)
            {
                _managementFee = value;
                OnPropertyChanged();
            }
        }
    }

    private double _interestPercent = 12.5;
    public double InterestPercent
    {
        get => _interestPercent;
        set
        {
            if (_interestPercent != value)
            {
                _interestPercent = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _boolDownPayment = false;
    public bool BoolDownPayment
    {
        get => _boolDownPayment;
        set
        {
            if (_boolDownPayment != value)
            {
                _boolDownPayment = value;
                OnPropertyChanged();

                if (!_boolDownPayment)
                    DownPayment = 0;
            }
        }
    }

    public string ContractState { get; set; } = "جاري";

    public long ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string ProductType { get; set; } = "جوالات";
    public string ProductCarPlateNumber { get; set; } = "";
    public string ProductCarVIN { get; set; } = "";
    public string ProductCarModel { get; set; } = "";
    public string ProductCarColor { get; set; } = "";

    public string ProductMobileStorage { get; set; } = "";
    public string ProductMobileColor { get; set; } = "";

    private double _productMainPrice = 1;
    public double ProductMainPrice
    {
        get => _productMainPrice;
        set
        {
            if (_productMainPrice != value)
            {
                _productMainPrice = value;
                OnPropertyChanged();
            }
        }
    }

    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerIdentityNumber { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string CustomerAddress { get; set; } = "";
    public string CustomerJob { get; set; } = "";
    public string CustomerSponserName { get; set; } = "";
    public string CustomerSponserIdentityNumber { get; set; } = "";
    public string CustomerSponserPhone { get; set; } = "";
    public string CustomerSponserAddress { get; set; } = "";
    public string CustomerSponserJob { get; set; } = "";

    public long OwnerId { get; set; }
    public string OwnerIdentityNumber { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string OwnerPhone { get; set; } = "";
    public string OwnerAddress { get; set; } = "";

    public IBrush StateColor =>
        ContractState == "منتهي" ? Brushes.Red : Brushes.Green;
    

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}