using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RealEstateApp.Views;

public partial class MainMenuView : UserControl
{

public event Action? OpenRealEstate;
public event Action? OpenInstallments;
    public MainMenuView()
    {
        InitializeComponent();
    }

  private void RealEstateButton_Click(object? sender, RoutedEventArgs e)
{
    OpenRealEstate?.Invoke();
}

private void InstallmentButton_Click(object? sender, RoutedEventArgs e)
{
    OpenInstallments?.Invoke();
}
}