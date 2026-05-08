using RealEstateInstallmentsManager.Models.Cloud;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

public class CloudContractsRealEstateService
{
    private readonly SupabaseService _supabaseService;

    public CloudContractsRealEstateService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task<long> AddContractAsync(ContractRealEstateRow row)
    {
        var result = await _supabaseService.Client
            .From<ContractRealEstateRow>()
            .Insert(row);

        return result.Models.First().Id;
    }

    public async Task<List<ContractRealEstateRow>> GetContractsAsync()
    {
        var result = await _supabaseService.Client
            .From<ContractRealEstateRow>()
            .Get();

        return result.Models;
    }

    public async Task<ContractRealEstateRow?> GetContractByIdAsync(long id)
    {
        var result = await _supabaseService.Client
            .From<ContractRealEstateRow>()
            .Where(x => x.Id == id)
            .Get();

        return result.Models.FirstOrDefault();
    }

    public async Task UpdateContractAsync(long cloudId, ContractRealEstateRow row)
    {
        row.Id = cloudId;

        await _supabaseService.Client
            .From<ContractRealEstateRow>()
            .Where(x => x.Id == cloudId)
            .Update(row);
    }

    public async Task DeleteContractAsync(long cloudId)
    {
        await _supabaseService.Client.From<ContractRealEstateRow>()
            .Where(x => x.Id == cloudId).Delete();
    }
}