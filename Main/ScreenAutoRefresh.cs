using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace RealEstateInstallmentsManager.Services;

// Shared by both modules (so it has no module suffix in its name).
// 1) Reloads ONLY the data grid of a screen when the data changed (a window saved
//    something, a row was added or deleted...). It never touches the input fields,
//    so nothing the user is typing is lost. Several changes in a row give one reload.
// 2) Optionally runs the screen's own sync (push then pull) every few minutes, only
//    while the app window is active, so other users' changes show up by themselves.
// It stops listening and stops both timers when the screen leaves the window.
public sealed class ScreenAutoRefresh : IDisposable
{
    private readonly Control _owner;
    private readonly Action _reload;
    private readonly DispatcherTimer _debounce;
    private readonly Func<Task>? _periodicSync;
    private readonly DispatcherTimer? _periodic;
    private bool _disposed;

    public ScreenAutoRefresh(Control owner, Action reload, Func<Task>? periodicSync = null, TimeSpan? interval = null)
    {
        _owner = owner;
        _reload = reload;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _debounce.Tick += OnTick;

        if (periodicSync is not null)
        {
            _periodicSync = periodicSync;
            _periodic = new DispatcherTimer { Interval = interval ?? TimeSpan.FromMinutes(2) };
            _periodic.Tick += OnPeriodicTick;
            _periodic.Start();
        }

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

    private async void OnPeriodicTick(object? sender, EventArgs e)
    {
        if (_disposed || _periodicSync is null)
            return;

        // No point pulling while the app is in the background.
        if (TopLevel.GetTopLevel(_owner) is not Window window || !window.IsActive)
            return;

        try
        {
            await _periodicSync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    // Reloads a data grid and keeps the same row selected (by id) and in view.
    public static void ReloadKeepingSelection<T>(DataGrid grid, Func<T, long> getId, Action load, Action? afterLoad = null)
        where T : class
    {
        long? selectedId = grid.SelectedItem is T selected ? getId(selected) : null;

        load();
        afterLoad?.Invoke();

        RestoreSelection(grid, getId, selectedId);
    }

    private static void RestoreSelection<T>(DataGrid grid, Func<T, long> getId, long? selectedId)
        where T : class
    {
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

        if (_periodic is not null)
        {
            _periodic.Stop();
            _periodic.Tick -= OnPeriodicTick;
        }
    }
}
