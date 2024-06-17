using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using log4net;

namespace McHMR_Updater_v2;

public partial class App : Application
{
    public static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Log.Info("Application started.");
        DispatcherUnhandledException += AppDispatcherUnhandledException;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Info("Application exited.");
        base.OnExit(e);
    }

    private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        GlobalExceptionHandleWindow dialog = new GlobalExceptionHandleWindow(e.Exception);
        dialog.ShowDialog();
        e.Handled = true;
    }
}
