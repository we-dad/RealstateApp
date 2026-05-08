using RealEstateInstallmentsManager.Models.Cloud;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

public class CloudExpensesInstallmentService
{
    private readonly SupabaseService _supabaseService;

    public CloudExpensesInstallmentService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task<long> AddExpenseAsync(ExpenseInstallmentRow row)
    {
        var result = await _supabaseService.Client
            .From<ExpenseInstallmentRow>()
            .Insert(row);

        return result.Models.First().Id;
    }

    public async Task<List<ExpenseInstallmentRow>> GetExpensesAsync()
    {
        var result = await _supabaseService.Client
            .From<ExpenseInstallmentRow>()
            .Get();

        return result.Models;
    }

    public async Task<ExpenseInstallmentRow?> GetExpenseByIdAsync(long id)
    {
        var result = await _supabaseService.Client
            .From<ExpenseInstallmentRow>()
            .Where(x => x.Id == id)
            .Get();

        return result.Models.FirstOrDefault();
    }

    public async Task UpdateExpenseAsync(ExpenseInstallmentRow row)
    {
        await _supabaseService.Client
            .From<ExpenseInstallmentRow>()
            .Update(row);
    }

    public async Task DeleteExpenseAsync(long id)
    {
        await _supabaseService.Client
            .From<ExpenseInstallmentRow>()
            .Where(x => x.Id == id)
            .Delete();
    }
}