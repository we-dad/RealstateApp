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
    // Muted text on the dark nav bar it sits on (Main/Theme.axaml's ink background);
    // a plain system gray was too dim against that dark background.
    private static readonly IBrush NormalBrush = new SolidColorBrush(Color.Parse("#B9C4C2"));

    public static void Bind(Control owner, TextBlock target)
    {
        void Update()
        {
            target.Text = GetText();
            target.Foreground = HasProblem ? Brushes.Firebrick : NormalBrush;
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
