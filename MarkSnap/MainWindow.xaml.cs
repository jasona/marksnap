using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Markdig;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;

namespace MarkSnap
{
    public partial class MainWindow : Window
    {
        private readonly MarkdownPipeline _markdownPipeline;
        private string _currentTheme = "System";
        private CoreWebView2Environment? _webView2Environment;
        private readonly Dictionary<TabItem, TabInfo> _tabInfos = new();
        private List<string>? _pendingFilesToRestore;
        private int _pendingActiveTabIndex;

        private class TabInfo
        {
            public string FilePath { get; set; } = "";
            public WebView2 WebView { get; set; } = null!;
            public bool IsInitialized { get; set; }
            public string? PendingHtml { get; set; }
        }

        public MainWindow()
        {
            App.Log("MainWindow constructor started");

            try
            {
                InitializeComponent();
                App.Log("InitializeComponent completed");
            }
            catch (Exception ex)
            {
                App.Log($"InitializeComponent FAILED: {ex}");
                throw;
            }

            // Register with App for IPC
            App.RegisterMainWindow(this);

            // Load saved window size/position and theme
            LoadWindowSettings();

            // Configure Markdig with common extensions
            _markdownPipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseEmojiAndSmiley()
                .UseTaskLists()
                .Build();
            App.Log("Markdig pipeline created");

            // Initialize WebView2 environment and window events
            Loaded += MainWindow_Loaded;
            StateChanged += MainWindow_StateChanged;
            App.Log("MainWindow constructor completed");
        }

        #region Window Chrome Button Handlers

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            // Update maximize button icon based on window state
            MaximizeButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        }

        #endregion

        #region Theme Management

        private void ApplyTheme(string theme)
        {
            _currentTheme = theme;
            string effectiveTheme = theme;

            if (theme == "System")
            {
                effectiveTheme = GetSystemTheme();
            }

            var resources = Application.Current.Resources;

            if (effectiveTheme == "Light")
            {
                // Light theme - update both colors and brushes
                resources["WindowBackgroundColor"] = ColorFromHex("#f5f5f5");
                resources["TitleBarBackgroundColor"] = ColorFromHex("#e8e8e8");
                resources["ToolbarBackgroundColor"] = ColorFromHex("#f0f0f0");
                resources["ButtonBackgroundColor"] = ColorFromHex("#e0e0e0");
                resources["ButtonHoverColor"] = ColorFromHex("#d0d0d0");
                resources["ButtonPressedColor"] = ColorFromHex("#c0c0c0");
                resources["TextPrimaryColor"] = ColorFromHex("#333333");
                resources["TextSecondaryColor"] = ColorFromHex("#666666");
                resources["TextMutedColor"] = ColorFromHex("#888888");
                resources["TextHeadingColor"] = ColorFromHex("#1a1a1a");
                resources["AccentColor"] = ColorFromHex("#0078d4");
                resources["BorderColor"] = ColorFromHex("#d0d0d0");
                resources["LinkColor"] = ColorFromHex("#0066cc");
                resources["CodeBackgroundColor"] = ColorFromHex("#f0f0f0");

                // Update brushes directly for immediate UI refresh
                resources["WindowBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#f5f5f5"));
                resources["TitleBarBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#e8e8e8"));
                resources["ToolbarBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#f0f0f0"));
                resources["ButtonBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#e0e0e0"));
                resources["ButtonHoverBrush"] = new SolidColorBrush(ColorFromHex("#d0d0d0"));
                resources["ButtonPressedBrush"] = new SolidColorBrush(ColorFromHex("#c0c0c0"));
                resources["TextPrimaryBrush"] = new SolidColorBrush(ColorFromHex("#333333"));
                resources["TextSecondaryBrush"] = new SolidColorBrush(ColorFromHex("#666666"));
                resources["TextMutedBrush"] = new SolidColorBrush(ColorFromHex("#888888"));
                resources["TextHeadingBrush"] = new SolidColorBrush(ColorFromHex("#1a1a1a"));
                resources["AccentBrush"] = new SolidColorBrush(ColorFromHex("#0078d4"));
                resources["BorderBrush"] = new SolidColorBrush(ColorFromHex("#d0d0d0"));
                resources["LinkBrush"] = new SolidColorBrush(ColorFromHex("#0066cc"));
                resources["CodeBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#f0f0f0"));

                // Update WebView2 backgrounds for all tabs
                foreach (var tabInfo in _tabInfos.Values)
                {
                    if (tabInfo.IsInitialized)
                    {
                        tabInfo.WebView.DefaultBackgroundColor = System.Drawing.Color.FromArgb(245, 245, 245);
                    }
                }
            }
            else
            {
                // Dark theme - update both colors and brushes
                resources["WindowBackgroundColor"] = ColorFromHex("#1e1e1e");
                resources["TitleBarBackgroundColor"] = ColorFromHex("#1e1e1e");
                resources["ToolbarBackgroundColor"] = ColorFromHex("#2d2d2d");
                resources["ButtonBackgroundColor"] = ColorFromHex("#3c3c3c");
                resources["ButtonHoverColor"] = ColorFromHex("#4c4c4c");
                resources["ButtonPressedColor"] = ColorFromHex("#5c5c5c");
                resources["TextPrimaryColor"] = ColorFromHex("#cccccc");
                resources["TextSecondaryColor"] = ColorFromHex("#888888");
                resources["TextMutedColor"] = ColorFromHex("#666666");
                resources["TextHeadingColor"] = ColorFromHex("#ffffff");
                resources["AccentColor"] = ColorFromHex("#007acc");
                resources["BorderColor"] = ColorFromHex("#404040");
                resources["LinkColor"] = ColorFromHex("#3794ff");
                resources["CodeBackgroundColor"] = ColorFromHex("#2d2d2d");

                // Update brushes directly for immediate UI refresh
                resources["WindowBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#1e1e1e"));
                resources["TitleBarBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#1e1e1e"));
                resources["ToolbarBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#2d2d2d"));
                resources["ButtonBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#3c3c3c"));
                resources["ButtonHoverBrush"] = new SolidColorBrush(ColorFromHex("#4c4c4c"));
                resources["ButtonPressedBrush"] = new SolidColorBrush(ColorFromHex("#5c5c5c"));
                resources["TextPrimaryBrush"] = new SolidColorBrush(ColorFromHex("#cccccc"));
                resources["TextSecondaryBrush"] = new SolidColorBrush(ColorFromHex("#888888"));
                resources["TextMutedBrush"] = new SolidColorBrush(ColorFromHex("#666666"));
                resources["TextHeadingBrush"] = new SolidColorBrush(ColorFromHex("#ffffff"));
                resources["AccentBrush"] = new SolidColorBrush(ColorFromHex("#007acc"));
                resources["BorderBrush"] = new SolidColorBrush(ColorFromHex("#404040"));
                resources["LinkBrush"] = new SolidColorBrush(ColorFromHex("#3794ff"));
                resources["CodeBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#2d2d2d"));

                // Update WebView2 backgrounds for all tabs
                foreach (var tabInfo in _tabInfos.Values)
                {
                    if (tabInfo.IsInitialized)
                    {
                        tabInfo.WebView.DefaultBackgroundColor = System.Drawing.Color.FromArgb(30, 30, 30);
                    }
                }
            }

            // Refresh all open tabs with new theme
            foreach (var kvp in _tabInfos)
            {
                if (!string.IsNullOrEmpty(kvp.Value.FilePath) && File.Exists(kvp.Value.FilePath))
                {
                    RefreshTabContent(kvp.Key, kvp.Value);
                }
            }

            App.Log($"Applied theme: {theme} (effective: {effectiveTheme})");
        }

        private void RefreshTabContent(TabItem tab, TabInfo tabInfo)
        {
            try
            {
                string markdown = File.ReadAllText(tabInfo.FilePath);
                string html = Markdown.ToHtml(markdown, _markdownPipeline);
                string effectiveTheme = _currentTheme == "System" ? GetSystemTheme() : _currentTheme;
                string fullHtml = GenerateHtmlDocument(html, Path.GetFileName(tabInfo.FilePath), effectiveTheme);

                if (tabInfo.IsInitialized)
                {
                    tabInfo.WebView.NavigateToString(fullHtml);
                }
                else
                {
                    tabInfo.PendingHtml = fullHtml;
                }
            }
            catch (Exception ex)
            {
                App.Log($"Error refreshing tab: {ex.Message}");
            }
        }

        private static Color ColorFromHex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }

        private static string GetSystemTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                if (value is int useLightTheme)
                {
                    return useLightTheme == 1 ? "Light" : "Dark";
                }
            }
            catch (Exception ex)
            {
                App.Log($"Failed to detect system theme: {ex.Message}");
            }
            return "Dark"; // Default to dark if detection fails
        }

        #endregion

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            App.Log("MainWindow_Loaded started");

            // Initialize shared WebView2 environment
            await InitializeWebView2Environment();

            // Check if a file was passed as argument - this takes priority
            if (!string.IsNullOrEmpty(App.FileToOpen) && File.Exists(App.FileToOpen))
            {
                App.Log($"Loading file from args: {App.FileToOpen}");
                OpenFileInNewTab(App.FileToOpen);
            }
            else if (_pendingFilesToRestore != null && _pendingFilesToRestore.Count > 0)
            {
                // Restore previously open files
                App.Log($"Restoring {_pendingFilesToRestore.Count} files from previous session");
                foreach (var filePath in _pendingFilesToRestore)
                {
                    if (File.Exists(filePath))
                    {
                        OpenFileInNewTab(filePath);
                    }
                    else
                    {
                        App.Log($"Skipping missing file: {filePath}");
                    }
                }

                // Restore active tab
                if (_pendingActiveTabIndex >= 0 && _pendingActiveTabIndex < DocumentTabs.Items.Count)
                {
                    DocumentTabs.SelectedIndex = _pendingActiveTabIndex;
                }

                _pendingFilesToRestore = null;
            }

            App.Log("MainWindow_Loaded completed");
        }

        private async Task InitializeWebView2Environment()
        {
            App.Log("InitializeWebView2Environment started");
            try
            {
                // Set environment to use a user data folder in temp
                var userDataFolder = Path.Combine(Path.GetTempPath(), "MarkSnap_WebView2");
                App.Log($"WebView2 user data folder: {userDataFolder}");

                _webView2Environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, null);
                App.Log("CoreWebView2Environment created");

                StatusText.Text = "Ready";
            }
            catch (Exception ex)
            {
                App.Log($"WebView2 environment FAILED: {ex}");
                StatusText.Text = "WebView2 initialization failed";
                MessageBox.Show($"Failed to initialize WebView2: {ex.Message}\n\nPlease ensure WebView2 Runtime is installed.\n\nYou can download it from:\nhttps://developer.microsoft.com/en-us/microsoft-edge/webview2/",
                    "WebView2 Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Tab Management

        /// <summary>
        /// Opens a file in a new tab. Called from IPC when secondary instance sends a file.
        /// </summary>
        public void OpenFileInNewTab(string filePath)
        {
            App.Log($"OpenFileInNewTab: {filePath}");

            if (!File.Exists(filePath))
            {
                MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Check if file is already open
            var existingTab = GetTabForFile(filePath);
            if (existingTab != null)
            {
                App.Log($"File already open, activating existing tab");
                DocumentTabs.SelectedItem = existingTab;
                return;
            }

            // Create new tab
            var tabItem = new TabItem
            {
                Header = Path.GetFileName(filePath),
                ToolTip = filePath
            };

            // Create WebView2 for this tab
            var webView = new WebView2
            {
                AllowExternalDrop = false
            };

            // Create tab info
            var tabInfo = new TabInfo
            {
                FilePath = filePath,
                WebView = webView,
                IsInitialized = false
            };
            _tabInfos[tabItem] = tabInfo;

            // Set WebView as tab content
            tabItem.Content = webView;

            // Add tab and select it
            DocumentTabs.Items.Add(tabItem);
            DocumentTabs.SelectedItem = tabItem;

            // Show tabs, hide welcome
            DocumentTabs.Visibility = Visibility.Visible;
            WelcomePanel.Visibility = Visibility.Collapsed;

            // Initialize WebView2 and load content
            InitializeTabWebView(tabItem, tabInfo);
        }

        private async void InitializeTabWebView(TabItem tab, TabInfo tabInfo)
        {
            App.Log($"InitializeTabWebView for {tabInfo.FilePath}");

            try
            {
                if (_webView2Environment == null)
                {
                    App.Log("WebView2 environment not ready, waiting...");
                    await InitializeWebView2Environment();
                }

                await tabInfo.WebView.EnsureCoreWebView2Async(_webView2Environment);

                tabInfo.WebView.CoreWebView2.Settings.IsScriptEnabled = true;
                tabInfo.WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                tabInfo.IsInitialized = true;

                // Apply theme to WebView2 background
                string effectiveTheme = _currentTheme == "System" ? GetSystemTheme() : _currentTheme;
                if (effectiveTheme == "Light")
                {
                    tabInfo.WebView.DefaultBackgroundColor = System.Drawing.Color.FromArgb(245, 245, 245);
                }
                else
                {
                    tabInfo.WebView.DefaultBackgroundColor = System.Drawing.Color.FromArgb(30, 30, 30);
                }

                // Load content
                LoadMarkdownIntoTab(tabInfo);

                // If there was pending HTML, display it now
                if (tabInfo.PendingHtml != null)
                {
                    tabInfo.WebView.NavigateToString(tabInfo.PendingHtml);
                    tabInfo.PendingHtml = null;
                }

                App.Log($"Tab WebView2 initialized for {tabInfo.FilePath}");
            }
            catch (Exception ex)
            {
                App.Log($"Tab WebView2 initialization failed: {ex.Message}");
                StatusText.Text = "Failed to initialize tab";
            }
        }

        private void LoadMarkdownIntoTab(TabInfo tabInfo)
        {
            try
            {
                string markdown = File.ReadAllText(tabInfo.FilePath);
                string html = Markdown.ToHtml(markdown, _markdownPipeline);
                string effectiveTheme = _currentTheme == "System" ? GetSystemTheme() : _currentTheme;
                string fullHtml = GenerateHtmlDocument(html, Path.GetFileName(tabInfo.FilePath), effectiveTheme);

                if (tabInfo.IsInitialized)
                {
                    tabInfo.WebView.NavigateToString(fullHtml);
                }
                else
                {
                    tabInfo.PendingHtml = fullHtml;
                }
            }
            catch (Exception ex)
            {
                App.Log($"Error loading markdown into tab: {ex.Message}");
            }
        }

        private TabItem? GetTabForFile(string filePath)
        {
            string normalizedPath = Path.GetFullPath(filePath).ToLowerInvariant();
            foreach (var kvp in _tabInfos)
            {
                if (Path.GetFullPath(kvp.Value.FilePath).ToLowerInvariant() == normalizedPath)
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        private void CloseTab(TabItem tab)
        {
            App.Log($"CloseTab");

            if (_tabInfos.TryGetValue(tab, out var tabInfo))
            {
                // Dispose WebView2
                tabInfo.WebView.Dispose();
                _tabInfos.Remove(tab);
            }

            DocumentTabs.Items.Remove(tab);

            // If no more tabs, show welcome panel
            if (DocumentTabs.Items.Count == 0)
            {
                DocumentTabs.Visibility = Visibility.Collapsed;
                WelcomePanel.Visibility = Visibility.Visible;
                FileNameText.Text = "No file loaded";
                Title = "MarkSnap - Markdown Viewer";
                TitleBarText.Text = "MarkSnap";
                StatusText.Text = "Ready";
            }
        }

        private void DocumentTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DocumentTabs.SelectedItem is TabItem selectedTab && _tabInfos.TryGetValue(selectedTab, out var tabInfo))
            {
                string fileName = Path.GetFileName(tabInfo.FilePath);
                FileNameText.Text = fileName;
                Title = $"MarkSnap - {fileName}";
                TitleBarText.Text = $"MarkSnap - {fileName}";
                StatusText.Text = $"Loaded: {tabInfo.FilePath}";
            }
        }

        private void TabCloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // Find the parent TabItem
                var tabItem = FindParent<TabItem>(button);
                if (tabItem != null)
                {
                    CloseTab(tabItem);
                }
            }
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Close the currently selected tab
            if (DocumentTabs.SelectedItem is TabItem selectedTab)
            {
                CloseTab(selectedTab);
            }
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T typedParent)
                    return typedParent;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        #endregion

        #region Keyboard Shortcuts

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Handle Ctrl+Tab and Ctrl+Shift+Tab for tab navigation
            if (e.Key == System.Windows.Input.Key.Tab &&
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                if (DocumentTabs.Items.Count > 1)
                {
                    bool shiftPressed = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) == System.Windows.Input.ModifierKeys.Shift;

                    if (shiftPressed)
                    {
                        // Previous tab
                        int newIndex = DocumentTabs.SelectedIndex - 1;
                        if (newIndex < 0) newIndex = DocumentTabs.Items.Count - 1;
                        DocumentTabs.SelectedIndex = newIndex;
                    }
                    else
                    {
                        // Next tab
                        int newIndex = DocumentTabs.SelectedIndex + 1;
                        if (newIndex >= DocumentTabs.Items.Count) newIndex = 0;
                        DocumentTabs.SelectedIndex = newIndex;
                    }
                }
                e.Handled = true;
            }
        }

        private void OpenCommand_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            OpenButton_Click(sender, e);
        }

        private void CloseCommand_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            CloseTabButton_Click(sender, e);
        }

        private void RefreshCommand_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            RefreshButton_Click(sender, e);
        }

        #endregion

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Markdown files (*.md;*.markdown)|*.md;*.markdown|All files (*.*)|*.*",
                Title = "Open Markdown File"
            };

            if (dialog.ShowDialog() == true)
            {
                OpenFileInNewTab(dialog.FileName);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (DocumentTabs.SelectedItem is TabItem selectedTab && _tabInfos.TryGetValue(selectedTab, out var tabInfo))
            {
                if (!string.IsNullOrEmpty(tabInfo.FilePath) && File.Exists(tabInfo.FilePath))
                {
                    LoadMarkdownIntoTab(tabInfo);
                    StatusText.Text = "File refreshed";
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_currentTheme)
            {
                Owner = this
            };

            if (settingsWindow.ShowDialog() == true)
            {
                ApplyTheme(settingsWindow.SelectedTheme);
                SaveWindowSettings();
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                App.Log("RegisterButton_Click started");
                RegisterFileAssociation();
                MessageBox.Show("MarkSnap has been registered as a handler for .md files!\n\nIf .md files don't open with MarkSnap by default, right-click a .md file → Open with → Choose another app → Select MarkSnap.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusText.Text = "File association registered successfully";
            }
            catch (Exception ex)
            {
                App.Log($"RegisterButton_Click error: {ex}");
                MessageBox.Show($"Failed to register file association: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegisterFileAssociation()
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath == null)
            {
                throw new InvalidOperationException("Could not determine executable path");
            }

            App.Log($"Registering file association for: {exePath}");

            string progId = "MarkSnap.md";
            string appName = "MarkSnap";

            // Register in HKEY_CURRENT_USER (no admin required)
            // Register the ProgID under HKCU\Software\Classes
            using (var classesKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes"))
            {
                if (classesKey == null) throw new InvalidOperationException("Could not open Classes key");

                // Create ProgID
                using (var progIdKey = classesKey.CreateSubKey(progId))
                {
                    if (progIdKey == null) throw new InvalidOperationException("Could not create ProgID key");

                    progIdKey.SetValue("", "Markdown Document");
                    progIdKey.SetValue("FriendlyTypeName", "Markdown Document");

                    using (var iconKey = progIdKey.CreateSubKey("DefaultIcon"))
                    {
                        iconKey?.SetValue("", $"\"{exePath}\",0");
                    }

                    using (var shellKey = progIdKey.CreateSubKey(@"shell\open\command"))
                    {
                        shellKey?.SetValue("", $"\"{exePath}\" \"%1\"");
                    }
                }

                // Register .md extension
                using (var extKey = classesKey.CreateSubKey(".md"))
                {
                    if (extKey == null) throw new InvalidOperationException("Could not create .md key");
                    extKey.SetValue("", progId);

                    // Add to OpenWithProgids
                    using (var openWithKey = extKey.CreateSubKey("OpenWithProgids"))
                    {
                        openWithKey?.SetValue(progId, new byte[0], RegistryValueKind.None);
                    }
                }

                // Register .markdown extension
                using (var extKey = classesKey.CreateSubKey(".markdown"))
                {
                    if (extKey == null) throw new InvalidOperationException("Could not create .markdown key");
                    extKey.SetValue("", progId);

                    using (var openWithKey = extKey.CreateSubKey("OpenWithProgids"))
                    {
                        openWithKey?.SetValue(progId, new byte[0], RegistryValueKind.None);
                    }
                }
            }

            // Register application in App Paths (helps Windows find the app)
            using (var appPathsKey = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Windows\CurrentVersion\App Paths\MarkSnap.exe"))
            {
                appPathsKey?.SetValue("", exePath);
                appPathsKey?.SetValue("Path", Path.GetDirectoryName(exePath) ?? "");
            }

            // Register in RegisteredApplications
            using (var regAppsKey = Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications"))
            {
                regAppsKey?.SetValue(appName, $@"Software\Classes\Applications\MarkSnap.exe\Capabilities");
            }

            // Register application capabilities
            using (var capKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\Applications\MarkSnap.exe\Capabilities"))
            {
                if (capKey != null)
                {
                    capKey.SetValue("ApplicationName", appName);
                    capKey.SetValue("ApplicationDescription", "Markdown file viewer");

                    using (var assocKey = capKey.CreateSubKey("FileAssociations"))
                    {
                        assocKey?.SetValue(".md", progId);
                        assocKey?.SetValue(".markdown", progId);
                    }
                }
            }

            // Also register under Applications
            using (var appsKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Applications\MarkSnap.exe\shell\open\command"))
            {
                appsKey?.SetValue("", $"\"{exePath}\" \"%1\"");
            }

            App.Log("Registry entries created, notifying shell");

            // Notify shell of changes
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);

            App.Log("File association registration complete");
        }

        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const int SHCNF_IDLIST = 0x0000;

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private string GenerateHtmlDocument(string bodyHtml, string title, string theme)
        {
            bool isDark = theme == "Dark";

            string bgColor = isDark ? "#1e1e1e" : "#f5f5f5";
            string textColor = isDark ? "#d4d4d4" : "#333333";
            string headingColor = isDark ? "#ffffff" : "#1a1a1a";
            string linkColor = isDark ? "#3794ff" : "#0066cc";
            string codeBg = isDark ? "#2d2d2d" : "#f0f0f0";
            string borderColor = isDark ? "#404040" : "#d0d0d0";
            string blockquoteBorder = isDark ? "#007acc" : "#0078d4";
            string blockquoteBg = isDark ? "rgba(0, 122, 204, 0.1)" : "rgba(0, 120, 212, 0.08)";
            string scrollbarThumb = isDark ? "#555" : "#c0c0c0";
            string scrollbarThumbHover = isDark ? "#666" : "#a0a0a0";
            string tableRowAlt = isDark ? "rgba(255, 255, 255, 0.02)" : "rgba(0, 0, 0, 0.02)";
            string markBg = isDark ? "#5c5c00" : "#fff3cd";
            string markColor = isDark ? "#fff" : "#664d03";

            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>{System.Web.HttpUtility.HtmlEncode(title)}</title>
    <style>
        :root {{
            --bg-color: {bgColor};
            --text-color: {textColor};
            --heading-color: {headingColor};
            --link-color: {linkColor};
            --code-bg: {codeBg};
            --border-color: {borderColor};
            --blockquote-border: {blockquoteBorder};
        }}

        * {{
            box-sizing: border-box;
        }}

        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            font-size: 16px;
            line-height: 1.7;
            color: var(--text-color);
            background-color: var(--bg-color);
            max-width: 900px;
            margin: 0 auto;
            padding: 40px 50px;
        }}

        h1, h2, h3, h4, h5, h6 {{
            color: var(--heading-color);
            margin-top: 1.5em;
            margin-bottom: 0.5em;
            font-weight: 600;
            line-height: 1.3;
        }}

        h1 {{
            font-size: 2.2em;
            border-bottom: 2px solid var(--border-color);
            padding-bottom: 0.3em;
        }}

        h2 {{
            font-size: 1.7em;
            border-bottom: 1px solid var(--border-color);
            padding-bottom: 0.2em;
        }}

        h3 {{ font-size: 1.4em; }}
        h4 {{ font-size: 1.2em; }}
        h5 {{ font-size: 1.1em; }}
        h6 {{ font-size: 1em; color: {(isDark ? "#888" : "#666")}; }}

        p {{
            margin: 1em 0;
        }}

        a {{
            color: var(--link-color);
            text-decoration: none;
        }}

        a:hover {{
            text-decoration: underline;
        }}

        code {{
            font-family: 'Cascadia Code', 'Fira Code', Consolas, 'Courier New', monospace;
            background-color: var(--code-bg);
            padding: 0.2em 0.4em;
            border-radius: 4px;
            font-size: 0.9em;
        }}

        pre {{
            background-color: var(--code-bg);
            padding: 16px;
            border-radius: 8px;
            overflow-x: auto;
            border: 1px solid var(--border-color);
        }}

        pre code {{
            background: none;
            padding: 0;
            font-size: 0.9em;
            line-height: 1.5;
        }}

        blockquote {{
            margin: 1em 0;
            padding: 0.5em 1em;
            border-left: 4px solid var(--blockquote-border);
            background-color: {blockquoteBg};
            color: {(isDark ? "#b0b0b0" : "#555555")};
        }}

        blockquote p {{
            margin: 0.5em 0;
        }}

        ul, ol {{
            padding-left: 2em;
            margin: 1em 0;
        }}

        li {{
            margin: 0.3em 0;
        }}

        li > ul, li > ol {{
            margin: 0.2em 0;
        }}

        /* Task lists */
        ul.contains-task-list {{
            list-style: none;
            padding-left: 1.5em;
        }}

        li.task-list-item {{
            position: relative;
        }}

        input[type=""checkbox""] {{
            margin-right: 0.5em;
            accent-color: var(--link-color);
        }}

        table {{
            border-collapse: collapse;
            width: 100%;
            margin: 1em 0;
        }}

        th, td {{
            border: 1px solid var(--border-color);
            padding: 10px 14px;
            text-align: left;
        }}

        th {{
            background-color: var(--code-bg);
            font-weight: 600;
            color: var(--heading-color);
        }}

        tr:nth-child(even) {{
            background-color: {tableRowAlt};
        }}

        hr {{
            border: none;
            border-top: 1px solid var(--border-color);
            margin: 2em 0;
        }}

        img {{
            max-width: 100%;
            height: auto;
            border-radius: 8px;
            margin: 1em 0;
        }}

        /* Inline formatting */
        strong {{
            color: var(--heading-color);
            font-weight: 600;
        }}

        em {{
            font-style: italic;
        }}

        del {{
            text-decoration: line-through;
            opacity: 0.7;
        }}

        mark {{
            background-color: {markBg};
            color: {markColor};
            padding: 0.1em 0.3em;
            border-radius: 3px;
        }}

        /* Scrollbar styling */
        ::-webkit-scrollbar {{
            width: 10px;
            height: 10px;
        }}

        ::-webkit-scrollbar-track {{
            background: var(--bg-color);
        }}

        ::-webkit-scrollbar-thumb {{
            background: {scrollbarThumb};
            border-radius: 5px;
        }}

        ::-webkit-scrollbar-thumb:hover {{
            background: {scrollbarThumbHover};
        }}
    </style>
</head>
<body>
{bodyHtml}
</body>
</html>";
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            App.Log($"DragOver from {sender?.GetType().Name}");
            bool isValidFile = false;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files?.Length > 0)
                {
                    string ext = Path.GetExtension(files[0]).ToLowerInvariant();
                    if (ext == ".md" || ext == ".markdown")
                    {
                        isValidFile = true;
                    }
                }
            }

            if (isValidFile)
            {
                e.Effects = DragDropEffects.Copy;
                // Show drop zone with visual feedback
                DropZone.IsHitTestVisible = true;
                DropZone.Background = new SolidColorBrush(Color.FromArgb(40, 0, 122, 204));
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            // Hide drop zone visual feedback
            DropZone.IsHitTestVisible = false;
            DropZone.Background = Brushes.Transparent;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            App.Log($"Drop from {sender?.GetType().Name}");

            // Hide drop zone visual feedback
            DropZone.IsHitTestVisible = false;
            DropZone.Background = Brushes.Transparent;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null)
                {
                    // Open each dropped markdown file in a new tab
                    foreach (var file in files)
                    {
                        App.Log($"Dropped file: {file}");
                        string ext = Path.GetExtension(file).ToLowerInvariant();
                        if (ext == ".md" || ext == ".markdown")
                        {
                            OpenFileInNewTab(file);
                        }
                    }
                }
            }
            e.Handled = true;
        }

        #region Window Settings Persistence

        private static string SettingsFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MarkSnap",
            "settings.json");

        private void LoadWindowSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        Left = settings.Left;
                        Top = settings.Top;
                        Width = settings.Width;
                        Height = settings.Height;
                        WindowState = settings.IsMaximized ? WindowState.Maximized : WindowState.Normal;
                        _currentTheme = settings.Theme ?? "System";
                        ApplyTheme(_currentTheme);

                        // Store open files to restore after window is loaded
                        if (settings.OpenFiles != null && settings.OpenFiles.Count > 0)
                        {
                            _pendingFilesToRestore = settings.OpenFiles;
                            _pendingActiveTabIndex = settings.ActiveTabIndex;
                        }

                        App.Log($"Loaded window settings: {settings.Width}x{settings.Height} at ({settings.Left}, {settings.Top}), Theme: {_currentTheme}, OpenFiles: {settings.OpenFiles?.Count ?? 0}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                App.Log($"Failed to load window settings: {ex.Message}");
            }

            // No settings found - center window on primary screen and apply default theme
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            Left = (screenWidth - Width) / 2;
            Top = (screenHeight - Height) / 2;
            ApplyTheme("System");
            App.Log("No saved settings, centering window on screen");
        }

        private void SaveWindowSettings()
        {
            try
            {
                // Collect open file paths
                var openFiles = _tabInfos.Values
                    .Select(ti => ti.FilePath)
                    .Where(f => !string.IsNullOrEmpty(f))
                    .ToList();

                // Get active tab index
                int activeIndex = DocumentTabs.SelectedIndex;

                var settings = new AppSettings
                {
                    Left = RestoreBounds.Left,
                    Top = RestoreBounds.Top,
                    Width = RestoreBounds.Width,
                    Height = RestoreBounds.Height,
                    IsMaximized = WindowState == WindowState.Maximized,
                    Theme = _currentTheme,
                    OpenFiles = openFiles,
                    ActiveTabIndex = activeIndex >= 0 ? activeIndex : 0
                };

                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
                App.Log($"Saved window settings: {settings.Width}x{settings.Height} at ({settings.Left}, {settings.Top}), Theme: {settings.Theme}, OpenFiles: {openFiles.Count}");
            }
            catch (Exception ex)
            {
                App.Log($"Failed to save window settings: {ex.Message}");
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowSettings();
            base.OnClosing(e);
        }

        #endregion
    }

    internal class AppSettings
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsMaximized { get; set; }
        public string? Theme { get; set; }
        public List<string>? OpenFiles { get; set; }
        public int ActiveTabIndex { get; set; }
    }
}
