using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

public class AuthService
{
    private readonly SupabaseService _supabaseService;

    public AuthService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task<bool> SignInAsync(string email, string password)
    {
        var session = await _supabaseService.Client.Auth.SignIn(email, password);
        return session != null;
    }

    public async Task<bool> SignUpAsync(string email, string password)
    {
        var session = await _supabaseService.Client.Auth.SignUp(email, password);
        return session != null;
    }

    public string? CurrentUserId =>
        _supabaseService.Client.Auth.CurrentUser?.Id;
}