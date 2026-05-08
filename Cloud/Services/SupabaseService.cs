using System;
using Supabase;
using System.Threading.Tasks;

namespace RealEstateInstallmentsManager.Services;

public class SupabaseService
{
    private Client? _client;

    public Client Client =>
        _client ?? throw new InvalidOperationException("Supabase is not initialized.");

    public async Task InitializeAsync()
    {
        var url = "https://nahugsyfgkxpeagoabrx.supabase.co";
        var key = "sb_publishable_QWg-mAlMGTEPcK60YZZzmQ_ij8HR2Pm";

        _client = new Client(url, key);
        await _client.InitializeAsync();
    }
}