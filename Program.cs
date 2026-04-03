using System.Windows.Forms;

namespace AutoHwp2Pdf;

internal static class Program
{
    private static Mutex? _instanceMutex;

    [STAThread]
    private static void Main(string[] args)
    {
        var startMinimized = args.Any(static arg => string.Equals(arg, "--minimized", StringComparison.OrdinalIgnoreCase));

        if (!TryAcquireSingleInstance())
        {
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            using var controller = new AppController(startMinimized);
            Application.Run(controller);
        }
        finally
        {
            _instanceMutex?.ReleaseMutex();
            _instanceMutex?.Dispose();
        }
    }

    private static bool TryAcquireSingleInstance()
    {
        _instanceMutex = new Mutex(initiallyOwned: true, @"Local\AutoHwp2Anything.Singleton", out var createdNew);

        if (!createdNew)
        {
            _instanceMutex.Dispose();
            _instanceMutex = null;
        }

        return createdNew;
    }
}
