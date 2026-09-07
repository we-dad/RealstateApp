using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class MainMenuView : UserControl
{
    private readonly MainWindow _mainWindow;
    public string AppVersion => $"Version {AppVersionService.GetVersion()}";

    public MainMenuView(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        DataContext = this;
    }

    private async void InstallmentButton_Click(object? sender, RoutedEventArgs e)
    {
        _mainWindow.ShowInstallmentPage();
    }

    private async void RealEstateButton_Click(object? sender, RoutedEventArgs e)
    {
        _mainWindow.ShowRealEstatePage();
    }
}