using RealEstateInstallmentsManager.Models.Cloud;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

public class CloudOwnersRealEstateService
{
    private readonly SupabaseService _supabaseService;

    public CloudOwnersRealEstateService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    // CREATE
    public async Task<long> AddOwnerAsync(OwnerRealEstateRow row)
    {
        var result = await _supabaseService.Client
            .From<OwnerRealEstateRow>()
            .Insert(row);

        return result.Models.First().Id;
    }

    // READ ALL
    public async Task<List<OwnerRealEstateRow>> GetOwnersAsync()
    {
        var result = await _supabaseService.Client
            .From<OwnerRealEstateRow>()
            .Get();

        return result.Models;
    }

    // READ ONE
    public async Task<OwnerRealEstateRow?> GetOwnerByIdAsync(long id)
    {
        var result = await _supabaseService.Client
            .From<OwnerRealEstateRow>()
            .Where(x => x.Id == id)
            .Get();

        return result.Models.FirstOrDefault();
    }

    // UPDATE
    public async Task UpdateOwnerAsync(long cloudId, OwnerRealEstateRow row)
    {
        row.Id = cloudId;

        await _supabaseService.Client
            .From<OwnerRealEstateRow>()
            .Where(x => x.Id == cloudId)
            .Update(row);
    }

    // DELETE
    public async Task DeleteOwnerAsync(long cloudId)
    {
        await _supabaseService.Client.From<OwnerRealEstateRow>()
            .Where(x => x.Id == cloudId).Delete();
    }
}