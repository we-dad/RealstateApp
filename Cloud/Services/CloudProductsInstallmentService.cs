using RealEstateInstallmentsManager.Models.Cloud;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

public class CloudProductsInstallmentService
{
    private readonly SupabaseService _supabaseService;

    public CloudProductsInstallmentService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task<long> AddProductAsync(ProductInstallmentRow row)
    {
        var result = await _supabaseService.Client
            .From<ProductInstallmentRow>()
            .Insert(row);

        return result.Models.First().Id;
    }
    public async Task<List<ProductInstallmentRow>> GetProductsAsync()
    {
        var result = await _supabaseService.Client
            .From<ProductInstallmentRow>()
            .Get();

        return result.Models;
    }

    public async Task<ProductInstallmentRow?> GetProductByIdAsync(long id)
    {
        var result = await _supabaseService.Client
            .From<ProductInstallmentRow>()
            .Where(x => x.Id == id)
            .Get();

        return result.Models.FirstOrDefault();
    }

    public async Task UpdateProductAsync(long cloudId, ProductInstallmentRow row)
    {
        row.Id = cloudId;

        await _supabaseService.Client
            .From<ProductInstallmentRow>()
            .Where(x => x.Id == cloudId)
            .Update(row);
    }

    public async Task DeleteProductAsync(long cloudId)
    {
        await _supabaseService.Client.From<ProductInstallmentRow>()
            .Where(x => x.Id == cloudId).Delete();
    }

}