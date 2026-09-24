using System;
using System.IO;
using System.Text.Json;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace RealEstateInstallmentsManager.Services;

/// <summary>
/// Backs the "تذكرني على هذا الجهاز" checkbox with a real, working session save/
/// restore, using gotrue-csharp's own IGotrueSessionPersistence hook (the SDK
/// calls SaveSession/LoadSession/DestroySession automatically at the right
/// times - sign-in, token refresh, sign-out).
///
/// Security note (the developer should know this plainly): the session file is
/// a PLAIN, unencrypted JSON file containing the account's refresh token, stored
/// under LocalApplicationData next to the local SQLite database - not the OS's
/// secure credential vault. Anyone with file-system access to this Windows/macOS
/// user profile could read it and use it until the token is revoked or expires.
/// This matches what the checkbox already promises ("remember me on THIS
/// device"), and is a reasonable default for a small internal business tool, but
/// it is not the same guarantee as a browser's or OS's encrypted credential
/// store. Upgrading to OS-native secure storage is a valid future improvement,
/// not done here to keep this change small.
/// </summary>
public class SessionPersistenceService : IGotrueSessionPersistence<Session>
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RealEstateInstallmentsManager",
        "session.json");

    // The SDK calls SaveSession on every successful sign-in/refresh regardless of
    // the checkbox (it has no concept of "remember me" itself) - this flag is how
    // LoginView tells it whether to actually write the file this time. Defaults
    // to false, so a session is never persisted unless the checkbox was checked
    // at the moment of signing in.
    public static bool RememberMe { get; set; } = false;

    public void SaveSession(Session session)
    {
        if (!RememberMe) return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(session));
        }
        catch (Exception ex)
        {
            // Saving is a convenience (auto sign-in next time), never something
            // that should block an otherwise-successful sign-in.
            Console.WriteLine(ex.ToString());
        }
    }

    public Session? LoadSession()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            return JsonSerializer.Deserialize<Session>(File.ReadAllText(FilePath));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return null;
        }
    }

    public void DestroySession()
    {
        try
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}
