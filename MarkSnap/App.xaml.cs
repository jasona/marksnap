using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace MarkSnap
{
    public partial class App : Application
    {
        public static string? FileToOpen { get; private set; }
        private static readonly string LogFile = Path.Combine(Path.GetTempPath(), "marksnap_debug.log");

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogFile, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            }
            catch { }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Clear old log
            try { File.Delete(LogFile); } catch { }

            Log("App.OnStartup called");
            Log($"Args count: {e.Args.Length}");

            // Set up global exception handling
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Check if a file path was passed as a command-line argument
            if (e.Args.Length > 0)
            {
                FileToOpen = e.Args[0];
                Log($"FileToOpen set to: {FileToOpen}");
            }

            base.OnStartup(e);
            Log("base.OnStartup completed");
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log($"DISPATCHER EXCEPTION: {e.Exception}");
            MessageBox.Show($"Unhandled error: {e.Exception.Message}\n\nSee log at: {LogFile}",
                "MarkSnap Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log($"DOMAIN EXCEPTION: {e.ExceptionObject}");
        }
    }
}
