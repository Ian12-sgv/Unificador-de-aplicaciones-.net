using Appinventario.Data;
using Appinventario.Services;
using Appinventario.Views;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Appinventario;

public partial class App : Application
{
    private static DatabaseQuery database;

    public static DatabaseQuery Database
    {
        get
        {
            if (database == null)
            {
                database = new DatabaseQuery(Path.Combine(FileSystem.AppDataDirectory, "Inventary.db3"));
            }

            return database;
        }
    }

    public App()
    {
        InitializeComponent();
        RegisterGlobalExceptionHandlers();
        UserAppTheme = AppTheme.Light;
        DependencyService.Register<MockDataStore>();
        MainPage = AuthService.TryRestoreSession()
            ? new AppShell()
            : new LoginPage();
    }

    private void RegisterGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _ = CrashReportService.LogExceptionAsync(ex, "AppDomain.CurrentDomain.UnhandledException");
        }
    }

    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        _ = CrashReportService.LogExceptionAsync(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved();
    }
}
