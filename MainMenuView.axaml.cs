using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RealEstateInstallmentsManager.Views;

public partial class MainMenuView : UserControl
{
    private readonly MainWindow _mainWindow;

    public MainMenuView(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
    }

    private void InstallmentButton_Click(object? sender, RoutedEventArgs e)
    {
        _mainWindow.ShowInstallmentPage();
    }

    private void RealEstateButton_Click(object? sender, RoutedEventArgs e)
    {
        _mainWindow.ShowRealEstatePage();
    }
}