using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RealEstateInstallmentsManager.Models;

public class CustomerInstallment : INotifyPropertyChanged
{
    public long Id { get; set; }
    public long CloudId { get; set; }
    public string SyncAction { get; set; } = "";
    public string Name { get; set; } = "";
    public string IdentityNumber { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string Job { get; set; } = "";
    private bool _boolSponser = false;
    public bool BoolSponser
    {
        get => _boolSponser;
        set
        {
            if (_boolSponser != value)
            {
                _boolSponser = value;
                OnPropertyChanged();
            }
        }
    }
    public string SponserName { get; set; } = "";
    public string SponserIdentityNumber { get; set; } = "";
    public string SponserPhone { get; set; } = "";
    public string SponserAddress { get; set; } = "";
    public string SponserJob { get; set; } = "";
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    
    // Financial summary
    public double TotalAmount { get; set; }
    public double PaidAmount { get; set; }
    public double LeftAmount { get; set; }
    public int ReceiptsCount { get; set; }

// Progress
    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public int LeftInstallments { get; set; }
    public double PaymentProgressPercent { get; set; }

// Receipt info
    public long ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public DateTime ReceiptDate { get; set; }
    public double ReceiptAmount { get; set; }
    public string PaymentMethod { get; set; } = "";

// Contract info
    public long ContractId { get; set; }
    public string ContractNumber { get; set; } = "";
    public double ContractAmount { get; set; }
    public double ContractPeriod { get; set; }
    public string ContractState { get; set; } = "";
    public DateTime ContractStartDate { get; set; }
    public DateTime ContractEndDate { get; set; }
}
