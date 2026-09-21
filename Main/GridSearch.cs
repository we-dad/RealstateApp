using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia.Controls;

namespace RealEstateInstallmentsManager.Services;

// Shared by both modules (so it has no module suffix in its name).
// A search box above a data grid: shows only the rows that contain every word typed
// (name, number, date, amount... any text of the row, ignoring case, Arabic letter
// variants and Arabic-Indic digits). The screen calls AfterLoad() each time it has
// loaded the full list into the grid, so the filter is applied again after every
// reload (add, sync, the automatic refresh).
public sealed class GridSearch<T> where T : class
{
    private static readonly PropertyInfo[] Props = typeof(T)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(IsSearchable)
        .ToArray();

    private readonly DataGrid _grid;
    private readonly TextBox _box;
    private List<(T Item, string Text)> _rows = new();
    private List<T> _full = new();
    private object? _shown;   // the list this class put in the grid last

    public GridSearch(DataGrid grid, TextBox box)
    {
        _grid = grid;
        _box = box;
        _box.TextChanged += (_, _) => Apply();
    }

    // Call right after the loader put the FULL list into the grid.
    public void AfterLoad()
    {
        // If the grid still holds the list WE put there (the load failed and did not
        // replace it), it is the filtered list, not the full one: keep what we have.
        if (!ReferenceEquals(_grid.ItemsSource, _shown))
        {
            _full = (_grid.ItemsSource as IEnumerable<T>)?.ToList() ?? new List<T>();
            _rows = _full.Select(item => (item, Normalize(RowText(item)))).ToList();
        }

        Apply();
    }

    private void Apply()
    {
        var words = Normalize(_box.Text ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var selected = _grid.SelectedItem as T;

        var shown = words.Length == 0
            ? _full
            : _rows.Where(r => words.All(w => r.Text.Contains(w))).Select(r => r.Item).ToList();

        _shown = shown;
        _grid.ItemsSource = shown;

        // Typing must not drop the chosen row (or send the grid back to the top).
        if (selected is not null && shown.Contains(selected))
        {
            _grid.SelectedItem = selected;
            _grid.ScrollIntoView(selected, null);
        }
    }

    // Text properties only: ids, sync flags and signature paths are not searchable.
    private static bool IsSearchable(PropertyInfo p)
    {
        if (!p.CanRead || p.GetIndexParameters().Length > 0)
            return false;

        var name = p.Name;
        if (name == "IsDirty" || name == "SyncAction" || name.EndsWith("Id") ||
            name.StartsWith("Signature") || name.EndsWith("Path"))
            return false;

        var type = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
        return type == typeof(string) || type == typeof(int) || type == typeof(long) ||
               type == typeof(double) || type == typeof(decimal) || type == typeof(float) ||
               type == typeof(DateTime);
    }

    private static string RowText(T item)
    {
        var sb = new StringBuilder();

        foreach (var p in Props)
        {
            var value = p.GetValue(item);
            if (value is null)
                continue;

            sb.Append(value switch
            {
                DateTime d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                // both "1500" and "1500.00" (the grid shows N2, "1,500.00")
                double d => d.ToString("0.##", CultureInfo.InvariantCulture) + " " + d.ToString("0.00", CultureInfo.InvariantCulture),
                float f => f.ToString("0.##", CultureInfo.InvariantCulture) + " " + f.ToString("0.00", CultureInfo.InvariantCulture),
                decimal m => m.ToString("0.##", CultureInfo.InvariantCulture) + " " + m.ToString("0.00", CultureInfo.InvariantCulture),
                _ => value.ToString()
            }).Append(' ');
        }

        return sb.ToString();
    }

    // Same letter for the Arabic variants, no diacritics, Western digits.
    private static string Normalize(string text)
    {
        var sb = new StringBuilder(text.Length);

        foreach (var ch in text.ToLowerInvariant())
        {
            // diacritics, tatweel and the thousands comma ("1,500" finds 1500)
            if (ch >= 'ً' && ch <= 'ْ' || ch == 'ـ' || ch == ',' || ch == '،')
                continue;

            sb.Append(ch switch
            {
                'أ' or 'إ' or 'آ' => 'ا',
                'ى' => 'ي',
                'ة' => 'ه',
                'ؤ' => 'و',
                'ئ' => 'ي',
                >= '٠' and <= '٩' => (char)('0' + (ch - '٠')),
                >= '۰' and <= '۹' => (char)('0' + (ch - '۰')),
                _ => ch
            });
        }

        return sb.ToString();
    }
}
