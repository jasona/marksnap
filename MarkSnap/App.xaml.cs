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
        private static SingleInstanceManager? _singleInstanceManager;

        public static MainWindow? MainWindowInstance { get; private set; }

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

            // Get file path from arguments
            string? filePath = e.Args.Length > 0 ? e.Args[0] : null;

            // Try to become the primary instance
            _singleInstanceManager = new SingleInstanceManager();

            if (!_singleInstanceManager.TryStartAsPrimary())
            {
                // Another instance is running - send file to it and exit
                Log("Secondary instance detected, forwarding file to primary");

                if (!string.IsNullOrEmpty(filePath))
                {
                    SingleInstanceManager.SendFileToPrimary(filePath);
                }

                // Shutdown this instance
                Shutdown();
                return;
            }

            Log("Primary instance started");

            // Wire up file received event
            _singleInstanceManager.FileReceived += OnFileReceivedFromSecondaryInstance;

            // Set up global exception handling
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Store file path for MainWindow to pick up
            if (!string.IsNullOrEmpty(filePath))
            {
                FileToOpen = filePath;
                Log($"FileToOpen set to: {FileToOpen}");
            }

            base.OnStartup(e);
            Log("base.OnStartup completed");
        }

        private void OnFileReceivedFromSecondaryInstance(string filePath)
        {
            Log($"File received from secondary instance: {filePath}");

            // Dispatch to UI thread
            Dispatcher.Invoke(() =>
            {
                if (MainWindowInstance != null)
                {
                    // Bring window to foreground
                    if (MainWindowInstance.WindowState == WindowState.Minimized)
                    {
                        MainWindowInstance.WindowState = WindowState.Normal;
                    }
                    MainWindowInstance.Activate();

                    // Open the file in a new tab
                    MainWindowInstance.OpenFileInNewTab(filePath);
                }
            });
        }

        public static void RegisterMainWindow(MainWindow window)
        {
            MainWindowInstance = window;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _singleInstanceManager?.Dispose();
            base.OnExit(e);
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
