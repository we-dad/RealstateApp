using System;
using System.Collections.Generic;
using System.Linq;
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

    // Reloads a data grid and keeps the same row selected (by id) and in view.
    public static void ReloadKeepingSelection<T>(DataGrid grid, Func<T, long> getId, Action load)
        where T : class
    {
        long? selectedId = grid.SelectedItem is T selected ? getId(selected) : null;

        load();

        if (selectedId is not long id || grid.ItemsSource is not IEnumerable<T> rows)
            return;

        var row = rows.FirstOrDefault(r => getId(r) == id);
        grid.SelectedItem = row;

        // A new list scrolls the grid to the top: keep the chosen row in view.
        if (row is not null)
            grid.ScrollIntoView(row, null);
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
