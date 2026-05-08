using RealEstateInstallmentsManager.Models.Cloud;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

public class CloudUnitsRealEstateService
{
    private readonly SupabaseService _supabaseService;

    public CloudUnitsRealEstateService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task<long> AddUnitAsync(UnitRealEstateRow row)
    {
        var result = await _supabaseService.Client
            .From<UnitRealEstateRow>()
            .Insert(row);

        return result.Models.First().Id;
    }

    public async Task<List<UnitRealEstateRow>> GetUnitsAsync()
    {
        var result = await _supabaseService.Client
            .From<UnitRealEstateRow>()
            .Get();

        return result.Models;
    }

    public async Task<UnitRealEstateRow?> GetUnitByIdAsync(long id)
    {
        var result = await _supabaseService.Client
            .From<UnitRealEstateRow>()
            .Where(x => x.Id == id)
            .Get();

        return result.Models.FirstOrDefault();
    }

    public async Task UpdateUnitAsync(long cloudId, UnitRealEstateRow row)
    {
        row.Id = cloudId;

        await _supabaseService.Client
            .From<UnitRealEstateRow>()
            .Update(row);
    }

    public async Task DeleteUnitAsync(long cloudId)
    {
        await _supabaseService.Client.From<UnitRealEstateRow>()
            .Where(x => x.Id == cloudId).Delete();
    }
}