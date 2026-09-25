using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

// New (not edit) window for Owners specifically - see this file's .axaml comment
// for why this diverges from the app's usual inline-add-on-list-screen pattern.
public partial class OwnersAddWindowViewInstallment : Window
{
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly OwnerInstallmentService _owners;
    private readonly InstallmentSyncService _sync;

    public OwnersAddWindowViewInstallment(SupabaseService supabaseService)
    {
        InitializeComponent();

        _db.Initialize();
        _owners = new OwnerInstallmentService(_db);
        _sync = new InstallmentSyncService(_db, supabaseService);
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var name = NameBox.Text?.Trim() ?? "";
            var phone = PhoneBox.Text?.Trim() ?? "";
            var identityNumber = IdentityNumberBox.Text?.Trim() ?? "";
            var address = AddressBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorText.Text = "الاسم مطلوب";
                ErrorText.IsVisible = true;
                return;
            }

            _owners.Add(name, identityNumber, phone, address);

            // OwnerInstallmentService.Add() raises DataChangeNotifier - the
            // Owners list screen's ScreenAutoRefresh already listens for that
            // and reloads itself, so nothing further is needed here for the list.
            _ = _sync.PushAllDirtyAsync();

            Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            ErrorText.Text = "تعذّرت إضافة المالك";
            ErrorText.IsVisible = true;
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
