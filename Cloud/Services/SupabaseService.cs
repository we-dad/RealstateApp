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

        // Registered unconditionally so a previously-saved session can be found
        // and restored on the next launch; whether anything actually GETS saved
        // on sign-in is controlled separately by SessionPersistenceService.RememberMe.
        _client.Auth.SetPersistence(new SessionPersistenceService());
    }

    // Attempts to restore + refresh a session saved by a previous "تذكرني" login.
    // Returns true only if a still-valid, signed-in user now exists.
    //
    // Deliberately does NOT use Client.Auth.RetrieveSessionAsync() - decompiling
    // gotrue-csharp 4.2.7 showed it gives up (and DELETES the saved session)
    // whenever the access token looks expired via Session.Expired(), without ever
    // trying the refresh token - which happens ~1 hour after the last save, so
    // "تذكرني" would silently stop surviving anything but reopening the app
    // within about an hour. It also deletes the session on a mere network
    // failure during its internal refresh attempt. Neither matches what the
    // checkbox promises the user.
    //
    // Instead: LoadSession() (sync, just reads the file via the registered
    // persistence) to get the stored refresh token, then call the two-argument
    // RefreshToken(accessToken, refreshToken) overload directly - confirmed by
    // decompiling Client.cs that this overload never touches persistence on
    // failure (no DestroySession call anywhere in its body), so an expired/
    // invalid refresh token or a dead network at startup just leaves the saved
    // session in place for the next attempt instead of erasing it - "remember
    // me" only really goes away on an explicit logout (which does call
    // DestroySession via the SignedOut event, confirmed separately). On success
    // it updates Client.Auth.CurrentSession/CurrentUser synchronously and fires
    // the SDK's own TokenRefreshed event, which re-saves the refreshed session
    // through the same persistence - RememberMe is set to true first so that
    // save actually happens.
    public async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            Client.Auth.LoadSession();
            var loaded = Client.Auth.CurrentSession;
            if (string.IsNullOrEmpty(loaded?.RefreshToken) || string.IsNullOrEmpty(loaded?.AccessToken))
                return false;

            SessionPersistenceService.RememberMe = true;

            // Client.Auth is declared as IGotrueClient<User,Session>, which only
            // exposes the parameterless RefreshToken() and a SetSession(...)
            // overload whose own doc comment says it destroys the current session
            // first (the same "destroy before confirming success" problem this
            // whole method exists to avoid). The two-argument RefreshToken only
            // exists on the concrete Supabase.Gotrue.Client - the runtime object
            // behind Auth always IS that concrete type, so this cast is safe.
            var gotrueClient = (Supabase.Gotrue.Client)Client.Auth;
            await gotrueClient.RefreshToken(loaded.AccessToken, loaded.RefreshToken);
            return Client.Auth.CurrentUser != null;
        }
        catch (Exception ex)
        {
            // Covers both a genuinely invalid/revoked refresh token and a plain
            // network failure (gotrue-csharp wraps both as GotrueException) -
            // either way, nothing here is destroyed; the saved session is left
            // for the next attempt.
            Console.WriteLine(ex.ToString());
            return false;
        }
    }
}