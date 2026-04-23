using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RealEstateInstallmentsManager.Models;

public class CustomerInstallment : INotifyPropertyChanged
{
    public long Id { get; set; }
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

}
