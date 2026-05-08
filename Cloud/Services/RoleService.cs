using RealEstateInstallmentsManager.Models.Cloud;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

public class RoleService
{
    private readonly SupabaseService _supabaseService;

    public RoleService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task<string?> GetMyRoleAsync()
    {
        var userId = _supabaseService.Client.Auth.CurrentUser?.Id;
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var result = await _supabaseService.Client
            .From<UserRoleRow>()
            .Where(x => x.UserId == userId)
            .Get();

        return result.Models.FirstOrDefault()?.Role;
    }
}