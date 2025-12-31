using System;
using System.IO;
using System.Windows;
using Serilog;

namespace DeepSeeArch
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Logging konfigurieren
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "DeepSeeArch",
                "Logs",
                $"log-{DateTime.Now:yyyyMMdd}.txt"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("DeepSeeArch started");

            // Globale Exception-Handler
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("DeepSeeArch exiting");
            Log.CloseAndFlush();
            base.OnExit(e);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception");
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Unhandled dispatcher exception");
            MessageBox.Show($"Ein Fehler ist aufgetreten:\n\n{e.Exception.Message}", 
                "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}