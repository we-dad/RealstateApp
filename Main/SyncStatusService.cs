using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace RealEstateInstallmentsManager.Services;

// Shared by both modules (so it has no module suffix in its name).
// Remembers whether the last push (upload) and the last pull (download) worked,
// so the main window can show it. Before this, a failed sync only wrote a line
// to the console and the user could not tell.
public static class SyncStatusService
{
    private static readonly object Lock = new object();

    private static bool _pushFailed;
    private static bool _pullFailed;
    private static bool _pushOffline;
    private static bool _pullOffline;
    private static DateTime? _lastSuccess;

    public static event Action? Changed;

    public static void ReportPush(bool ok, bool offline = false) =>
        Report(ref _pushFailed, ref _pushOffline, ok, offline);

    public static void ReportPull(bool ok, bool offline = false) =>
        Report(ref _pullFailed, ref _pullOffline, ok, offline);

    private static void Report(ref bool failedFlag, ref bool offlineFlag, bool ok, bool offline)
    {
        lock (Lock)
        {
            failedFlag = !ok;
            offlineFlag = !ok && offline;

            if (ok && !_pushFailed && !_pullFailed)
                _lastSuccess = DateTime.Now;
        }

        try
        {
            Changed?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    // Shows the status in a TextBlock and keeps it up to date until the owner leaves
    // the window. Reports may come from any thread, so the update hops to the UI thread.
    // Muted text on a dark nav bar (Main/Theme.axaml's ink background) needs a much
    // lighter color than the same text sitting on the app's light page background -
    // a plain system gray was too dim on dark, and this same light gray would be
    // nearly invisible on white.
    private static readonly IBrush NormalBrushOnDark = new SolidColorBrush(Color.Parse("#B9C4C2"));
    private static readonly IBrush NormalBrushOnLight = new SolidColorBrush(Color.Parse("#5B6B80"));

    // onDarkBackground: true for a TextBlock still sitting on the old dark nav bar
    // (RealEstate's MainWindowRealEstate, not yet restructured); false once it's
    // been moved onto the light page background (Installment's MainWindowInstallment).
    public static void Bind(Control owner, TextBlock target, bool onDarkBackground = true)
    {
        var normalBrush = onDarkBackground ? NormalBrushOnDark : NormalBrushOnLight;

        void Update()
        {
            target.Text = GetText();
            target.Foreground = HasProblem ? Brushes.Firebrick : normalBrush;
        }

        void OnChanged() => Dispatcher.UIThread.Post(Update);

        Changed += OnChanged;
        owner.DetachedFromVisualTree += (_, _) => Changed -= OnChanged;

        Update();
    }

    // True when something is wrong and the user should look at the message.
    public static bool HasProblem
    {
        get
        {
            lock (Lock)
                return AppSession.CanReadOnline && (_pushFailed || _pullFailed);
        }
    }

    public static string GetText()
    {
        lock (Lock)
        {
            if (!AppSession.CanReadOnline)
                return "الحساب تجريبي: لا توجد مزامنة مع السحابة";

            var last = _lastSuccess is DateTime t
                ? $"آخر مزامنة ناجحة الساعة {t:HH:mm}"
                : "لم تنجح مزامنة بعد";

            if (_pushFailed)
                return (_pushOffline ? "⚠ لا يوجد اتصال: " : "⚠ ") +
                       $"تعذّر رفع تعديلاتك للسحابة، وستُعاد المحاولة تلقائيًا. {last}";

            if (_pullFailed)
                return (_pullOffline ? "⚠ لا يوجد اتصال: " : "⚠ ") +
                       $"تعذّر جلب التحديثات من السحابة. {last}";

            return _lastSuccess is null ? "بانتظار أول مزامنة" : last;
        }
    }
}
