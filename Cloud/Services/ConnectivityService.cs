using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

/// <summary>
/// A real, live check of whether the server is reachable - used only to show an
/// honest "متصل بالخادم" / "غير متصل بالإنترنت" status. It never signs in and never
/// touches auth/session state; a short-timeout GET to Supabase's own health
/// endpoint.
///
/// This checks REACHABILITY, not authorization: any HTTP response at all (even a
/// 401/403/404) proves the network round-trip to the server succeeded, so it
/// counts as "online". Only a genuine network failure (no route, DNS error,
/// timeout, connection refused - an exception, not a status code) counts as
/// "offline". The first version only accepted 2xx via IsSuccessStatusCode, which
/// reported "offline" even while connected whenever the health endpoint replied
/// with a non-2xx status (e.g. missing an API key header) - caught by the
/// developer testing on a known-good connection.
/// </summary>
public static class ConnectivityService
{
    private static readonly HttpClient Http = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public static async Task<bool> IsServerReachableAsync()
    {
        try
        {
            using var response = await Http.GetAsync($"{SupabaseService.ProjectUrl}/auth/v1/health");
            return true;
        }
        catch
        {
            return false;
        }
    }
}
