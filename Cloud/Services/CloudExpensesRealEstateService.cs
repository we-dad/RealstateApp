using RealEstateInstallmentsManager.Models.Cloud;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

public class CloudExpensesRealEstateService
{
    private readonly SupabaseService _supabaseService;

    public CloudExpensesRealEstateService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task<long> AddExpenseAsync(ExpenseRealEstateRow row)
    {
        var result = await _supabaseService.Client
            .From<ExpenseRealEstateRow>()
            .Insert(row);

        return result.Models.First().Id;
    }

    public async Task<List<ExpenseRealEstateRow>> GetExpensesAsync()
    {
        var result = await _supabaseService.Client
            .From<ExpenseRealEstateRow>()
            .Get();

        return result.Models;
    }

    public async Task<ExpenseRealEstateRow?> GetExpenseByIdAsync(long id)
    {
        var result = await _supabaseService.Client
            .From<ExpenseRealEstateRow>()
            .Where(x => x.Id == id)
            .Get();

        return result.Models.FirstOrDefault();
    }

    public async Task UpdateExpenseAsync(long cloudId, ExpenseRealEstateRow row)
    {
        row.Id = cloudId;

        await _supabaseService.Client
            .From<ExpenseRealEstateRow>()
            .Where(x => x.Id == cloudId)
            .Update(row);
    }

    public async Task DeleteExpenseAsync(long cloudId)
    {
        await _supabaseService.Client.From<ExpenseRealEstateRow>()
            .Where(x => x.Id == cloudId).Delete();
    }
}