using System;
using Supabase;
using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

public class SupabaseService
{
    // Shared with ConnectivityService so the health check pings the same project
    // instead of duplicating the URL. Not a secret: publishable key, RLS enabled.
    public const string ProjectUrl = "https://nahugsyfgkxpeagoabrx.supabase.co";
    private const string Key = "sb_publishable_QWg-mAlMGTEPcK60YZZzmQ_ij8HR2Pm";

    private Client? _client;

    public Client Client =>
        _client ?? throw new InvalidOperationException("Supabase is not initialized.");

    public async Task InitializeAsync()
    {
        _client = new Client(ProjectUrl, Key);
        await _client.InitializeAsync();
    }
}