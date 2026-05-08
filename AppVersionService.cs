using System.Reflection;

namespace RealEstateInstallmentsManager.Services;

public static class AppVersionService
{
    public static string GetVersion()
    {
        return Assembly
            .GetExecutingAssembly()
            .GetName()
            .Version?
            .ToString() ?? "Unknown";
    }
}