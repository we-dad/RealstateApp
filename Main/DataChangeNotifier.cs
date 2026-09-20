using System;

namespace RealEstateInstallmentsManager.Services;

// Shared by both modules (so it has no module suffix in its name).
// A services raises Changed after it saves, updates or deletes something the user did;
// screens that show that data listen (through ScreenAutoRefresh) and reload their grid.
public static class DataChangeNotifier
{
    public static event Action? Changed;

    public static void Notify() => Changed?.Invoke();
}
