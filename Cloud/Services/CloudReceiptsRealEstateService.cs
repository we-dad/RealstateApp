using RealEstateInstallmentsManager.Models.Cloud;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

public class CloudReceiptsRealEstateService
{
    private readonly SupabaseService _supabaseService;

    public CloudReceiptsRealEstateService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task<long> AddReceiptAsync(ReceiptRealEstateRow row)
    {
        var result = await _supabaseService.Client
            .From<ReceiptRealEstateRow>()
            .Insert(row);

        return result.Models.First().Id;
    }

    public async Task<List<ReceiptRealEstateRow>> GetReceiptsAsync()
    {
        var result = await _supabaseService.Client
            .From<ReceiptRealEstateRow>()
            .Get();

        return result.Models;
    }

    public async Task<ReceiptRealEstateRow?> GetReceiptByIdAsync(long id)
    {
        var result = await _supabaseService.Client
            .From<ReceiptRealEstateRow>()
            .Where(x => x.Id == id)
            .Get();

        return result.Models.FirstOrDefault();
    }

    public async Task UpdateReceiptAsync(long cloudId, ReceiptRealEstateRow row)
    {
        row.Id = cloudId;

        await _supabaseService.Client
            .From<ReceiptRealEstateRow>()
            .Where(x => x.Id == cloudId)
            .Update(row);
    }

    public async Task DeleteReceiptAsync(long cloudId)
    {
        await _supabaseService.Client.From<ReceiptRealEstateRow>()
            .Where(x => x.Id == cloudId).Delete();
    }
}