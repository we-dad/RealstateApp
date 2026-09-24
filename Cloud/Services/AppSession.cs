namespace RealEstateInstallmentsManager.Services;

public static class AppSession
{
    public static string Role { get; set; } = "tester";

    // Supabase account id of the signed-in user (empty until login).
    public static string UserId { get; set; } = "";

    // Set once at login from AuthService.CurrentDisplayName - real data (Supabase
    // user_metadata "full_name"/"name" if set, else the email's local part), never
    // a placeholder. Empty until login, cleared again on logout.
    public static string DisplayName { get; set; } = "";

    public static void Clear()
    {
        Role = "tester";
        UserId = "";
        DisplayName = "";
    }

    public static bool CanReadOnline =>
        Role == "admin" ||
        Role == "editor" ||
        Role == "viewer";

    public static bool CanWriteOnline =>
        Role == "admin" ||
        Role == "editor";

    public static bool IsViewer =>
        Role == "viewer";

    public static bool IsTester =>
        Role == "tester";
}