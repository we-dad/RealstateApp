using RealEstateInstallmentsManager.Models.Cloud;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

public class CloudCustomersInstallmentService
{
    private readonly SupabaseService _supabaseService;

    public CloudCustomersInstallmentService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task<long> AddCustomerAsync(CustomerInstallmentRow row)
    {
        var result = await _supabaseService.Client
            .From<CustomerInstallmentRow>()
            .Insert(row);

        return result.Models.First().Id;
    }

    public async Task<List<CustomerInstallmentRow>> GetCustomersAsync()
    {
        var result = await _supabaseService.Client
            .From<CustomerInstallmentRow>()
            .Get();

        return result.Models;
    }

    public async Task<CustomerInstallmentRow?> GetCustomerByIdAsync(long id)
    {
        var result = await _supabaseService.Client
            .From<CustomerInstallmentRow>()
            .Where(x => x.Id == id)
            .Get();

        return result.Models.FirstOrDefault();
    }

    public async Task UpdateCustomerAsync(long cloudId, CustomerInstallmentRow row)
    {
        row.Id = cloudId;

        await _supabaseService.Client
            .From<CustomerInstallmentRow>()
            .Where(x => x.Id == cloudId)
            .Update(row);
    }

    public async Task DeleteCustomerAsync(long cloudId)
    {
        await _supabaseService.Client.From<CustomerInstallmentRow>()
            .Where(x => x.Id == cloudId).Delete();
    }

}