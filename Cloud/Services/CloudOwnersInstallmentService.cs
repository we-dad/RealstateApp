using RealEstateInstallmentsManager.Models.Cloud;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

public class CloudOwnersInstallmentService
{
    private readonly SupabaseService _supabaseService;

    public CloudOwnersInstallmentService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task<long> AddOwnerAsync(OwnerInstallmentRow row)
    {
        var result = await _supabaseService.Client
            .From<OwnerInstallmentRow>()
            .Insert(row);

        return result.Models.First().Id;
    }

    public async Task<List<OwnerInstallmentRow>> GetOwnersAsync()
    {
        var result = await _supabaseService.Client
            .From<OwnerInstallmentRow>()
            .Get();

        return result.Models;
    }

    public async Task<OwnerInstallmentRow?> GetOwnerByIdAsync(long id)
    {
        var result = await _supabaseService.Client
            .From<OwnerInstallmentRow>()
            .Where(x => x.Id == id)
            .Get();

        return result.Models.FirstOrDefault();
    }

    public async Task UpdateOwnerAsync(long cloudId, OwnerInstallmentRow row)
    {
        row.Id = cloudId;

        await _supabaseService.Client
            .From<OwnerInstallmentRow>()
            .Where(x => x.Id == cloudId)
            .Update(row);
    }

    public async Task DeleteOwnerAsync(long cloudId)
    {
        await _supabaseService.Client.From<OwnerInstallmentRow>()
            .Where(x => x.Id == cloudId).Delete();
    }

}