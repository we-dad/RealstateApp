using RealEstateInstallmentsManager.Models.Cloud;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

public class CloudContractsInstallmentService
{
    private readonly SupabaseService _supabaseService;

    public CloudContractsInstallmentService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task<long> AddContractAsync(ContractInstallmentRow row)
    {
        var result = await _supabaseService.Client
            .From<ContractInstallmentRow>()
            .Insert(row);

        return result.Models.First().Id;
    }

    public async Task<List<ContractInstallmentRow>> GetContractsAsync()
    {
        var result = await _supabaseService.Client
            .From<ContractInstallmentRow>()
            .Get();

        return result.Models;
    }

    public async Task<ContractInstallmentRow?> GetContractByIdAsync(long id)
    {
        var result = await _supabaseService.Client
            .From<ContractInstallmentRow>()
            .Where(x => x.Id == id)
            .Get();

        return result.Models.FirstOrDefault();
    }

    public async Task UpdateContractAsync(long cloudId, ContractInstallmentRow row)
    {
        row.Id = cloudId;

        await _supabaseService.Client
            .From<ContractInstallmentRow>()
            .Where(x => x.Id == cloudId)
            .Update(row);
    }

    public async Task DeleteContractAsync(long cloudId)
    {
        await _supabaseService.Client.From<ContractInstallmentRow>()
            .Where(x => x.Id == cloudId).Delete();
    }

}