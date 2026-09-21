namespace RealEstateInstallmentsManager.Services;

public static class AppSession
{
    public static string Role { get; set; } = "tester";

    // Supabase account id of the signed-in user (empty until login).
    public static string UserId { get; set; } = "";

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