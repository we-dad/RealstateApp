using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace RealEstateInstallmentsManager.Models;

public class ProductInstallment : INotifyPropertyChanged
{
    public long Id { get; set; }
    public long CloudId { get; set; }
    public long OwnerCloudId { get; set; }
    public string SyncAction { get; set; } = "";
    public long OwnerId { get; set; }
    public string OwnerIdentityNumber { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string OwnerPhone { get; set; } = "";
    public string OwnerAddress { get; set; } = "";
    public string ProductName { get; set; } = "";
    public float ProductMainPrice { get; set; }
    
    public string CarPlateNumber { get; set; } = "";
    public string CarVIN { get; set; } = "";
    public string CarModel { get; set; } = "";
    public string CarColor { get; set; } = "";

    public string MobileStorage { get; set; } = "";
    public string MobileColor { get; set; } = "";
    
    
    public List<string> ProductTypes { get; } = new()
    {
        "جوالات",
        "سيارات"
    };
    
    private string _productType = "جوالات";
    public string ProductType
    {
        get => _productType;
        set
        {
            if (_productType != value)
            {
                _productType = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPhoneFieldsVisible));
                OnPropertyChanged(nameof(IsCarFieldsVisible));
            }
        }
    }

    public bool IsPhoneFieldsVisible => ProductType == "جوالات";

    public bool IsCarFieldsVisible => ProductType != "جوالات";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
