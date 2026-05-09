namespace RealEstateInstallmentsManager.Services;

public static class AppSession
{
    public static string Role { get; set; } = "tester";

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