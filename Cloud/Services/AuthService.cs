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

    public async Task SignOutAsync()
    {
        await _supabaseService.Client.Auth.SignOut();
    }

    // Shared by both the manual login flow and the silent "تذكرني" restore -
    // fills AppSession from whatever the current signed-in Supabase user is.
    public async Task PopulateAppSessionAsync()
    {
        var roleService = new RoleService(_supabaseService);
        AppSession.Role = await roleService.GetMyRoleAsync();
        AppSession.UserId = CurrentUserId ?? "";
        AppSession.DisplayName = CurrentDisplayName;
    }

    public string? CurrentUserId =>
        _supabaseService.Client.Auth.CurrentUser?.Id;

    // Real data only, never a placeholder: the developer can set a "full_name"
    // (or "name") key in the user's Supabase user_metadata to control what shows
    // up in greetings; otherwise this falls back to the part of the email before
    // "@", and to "" only if even the email is missing.
    public string CurrentDisplayName
    {
        get
        {
            var user = _supabaseService.Client.Auth.CurrentUser;
            if (user == null) return "";

            if (user.UserMetadata != null)
            {
                foreach (var key in new[] { "full_name", "name", "display_name" })
                {
                    if (user.UserMetadata.TryGetValue(key, out var value) &&
                        value is string name && !string.IsNullOrWhiteSpace(name))
                        return name;
                }
            }

            var email = user.Email;
            if (string.IsNullOrWhiteSpace(email)) return "";

            var at = email.IndexOf('@');
            return at > 0 ? email[..at] : email;
        }
    }
}