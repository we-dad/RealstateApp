using RealEstateInstallmentsManager.Models.Cloud;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

public class CloudReceiptsInstallmentService
{
    private readonly SupabaseService _supabaseService;

    public CloudReceiptsInstallmentService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task<long> AddReceiptAsync(ReceiptInstallmentRow row)
    {
        var result = await _supabaseService.Client
            .From<ReceiptInstallmentRow>()
            .Insert(row);

        return result.Models.First().Id;
    }

    public async Task<List<ReceiptInstallmentRow>> GetReceiptsAsync()
    {
        var result = await _supabaseService.Client
            .From<ReceiptInstallmentRow>()
            .Get();

        return result.Models;
    }

    public async Task<ReceiptInstallmentRow?> GetReceiptByIdAsync(long id)
    {
        var result = await _supabaseService.Client
            .From<ReceiptInstallmentRow>()
            .Where(x => x.Id == id)
            .Get();

        return result.Models.FirstOrDefault();
    }

    public async Task UpdateReceiptAsync(long cloudId, ReceiptInstallmentRow row)
    {
        row.Id = cloudId;

        await _supabaseService.Client
            .From<ReceiptInstallmentRow>()
            .Where(x => x.Id == cloudId)
            .Update(row);
    }

    public async Task DeleteReceiptAsync(long cloudId)
    {
        await _supabaseService.Client.From<ReceiptInstallmentRow>()
            .Where(x => x.Id == cloudId).Delete();
    }
}