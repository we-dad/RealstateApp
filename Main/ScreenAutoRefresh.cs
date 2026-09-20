using System;
using Avalonia.Controls;
using Avalonia.Threading;

namespace RealEstateInstallmentsManager.Services;

// Shared by both modules (so it has no module suffix in its name).
// Reloads ONLY the data grid of a screen when the data changed (a window saved
// something, a row was added or deleted...). It never touches the input fields,
// so nothing the user is typing is lost. Several changes in a row give one reload.
// It stops listening when the screen leaves the window.
public sealed class ScreenAutoRefresh : IDisposable
{
    private readonly Action _reload;
    private readonly DispatcherTimer _debounce;
    private bool _disposed;

    public ScreenAutoRefresh(Control owner, Action reload)
    {
        _reload = reload;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _debounce.Tick += OnTick;

        DataChangeNotifier.Changed += OnChanged;
        owner.DetachedFromVisualTree += (_, _) => Dispose();
    }

    // May be raised from any thread: hop to the UI thread, then wait a moment so a
    // burst of changes becomes a single reload.
    private void OnChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_disposed)
                return;

            _debounce.Stop();
            _debounce.Start();
        });
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _debounce.Stop();

        if (_disposed)
            return;

        try
        {
            _reload();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        DataChangeNotifier.Changed -= OnChanged;
        _debounce.Stop();
        _debounce.Tick -= OnTick;
    }
}
