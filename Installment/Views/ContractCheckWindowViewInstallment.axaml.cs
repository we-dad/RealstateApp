using System;
using System.Linq;
using Avalonia.Controls;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

// Read-only report: lists contracts whose saved total does not match the
// calculation. It never writes to the database.
public partial class ContractCheckWindowViewInstallment : Window
{
    public ContractCheckWindowViewInstallment()
    {
        InitializeComponent();

        try
        {
            var db = new DbServiceInstallment();
            db.Initialize();

            var (total, mismatches) = new ContractServiceInstallment(db).GetCalculationCheckRows();

            var withoutDeduction = mismatches.Count(r => r.Status == ContractServiceInstallment.NoDownPaymentDeducted);

            SummaryText.Text =
                $"عدد العقود: {total} | لا تطابق المعادلة: {mismatches.Count} | " +
                $"منها بدون خصم الدفعة المقدمة: {withoutDeduction}";

            CheckGrid.ItemsSource = mismatches;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            SummaryText.Text = $"تعذّر إجراء الفحص: {ex.Message}";
        }
    }
}
