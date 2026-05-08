using RealEstateInstallmentsManager.Models.Cloud;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

public class CloudTenantsRealEstateService
{
    private readonly SupabaseService _supabaseService;

    public CloudTenantsRealEstateService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task<long> AddTenantAsync(TenantRealEstateRow row)
    {
        var result = await _supabaseService.Client
            .From<TenantRealEstateRow>()
            .Insert(row);

        return result.Models.First().Id;
    }

    public async Task<List<TenantRealEstateRow>> GetTenantsAsync()
    {
        var result = await _supabaseService.Client
            .From<TenantRealEstateRow>()
            .Get();

        return result.Models;
    }

    public async Task<TenantRealEstateRow?> GetTenantByIdAsync(long id)
    {
        var result = await _supabaseService.Client
            .From<TenantRealEstateRow>()
            .Where(x => x.Id == id)
            .Get();

        return result.Models.FirstOrDefault();
    }

    public async Task UpdateTenantAsync(long cloudId, TenantRealEstateRow row)
    {
        row.Id = cloudId;

        await _supabaseService.Client
            .From<TenantRealEstateRow>()
            .Where(x => x.Id == cloudId)
            .Update(row);
    }

    public async Task DeleteTenantAsync(long id)
    {
        await _supabaseService.Client
            .From<TenantRealEstateRow>()
            .Where(x => x.Id == id)
            .Delete();
    }
}