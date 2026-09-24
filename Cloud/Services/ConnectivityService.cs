using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

/// <summary>
/// A real, live check of whether the server is reachable - used only to show an
/// honest "متصل بالخادم" / "غير متصل بالإنترنت" status on the login screen. It never
/// signs in and never touches auth/session state; a short-timeout GET to Supabase's
/// own health endpoint, treated as "offline" on any failure (no network, DNS error,
/// timeout, server error...).
/// </summary>
public static class ConnectivityService
{
    private static readonly HttpClient Http = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(3)
    };

    public static async Task<bool> IsServerReachableAsync()
    {
        try
        {
            using var response = await Http.GetAsync($"{SupabaseService.ProjectUrl}/auth/v1/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
