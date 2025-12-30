using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Markdig;
using Microsoft.Win32;

namespace MarkSnap
{
    public partial class MainWindow : Window
    {
        private string? _currentFilePath;
        private readonly MarkdownPipeline _markdownPipeline;
        private bool _webViewInitialized = false;
        private string? _pendingHtml = null;
        private string _currentTheme = "System";

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

            // Load saved window size/position and theme
            LoadWindowSettings();

            // Configure Markdig with common extensions
            _markdownPipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseEmojiAndSmiley()
                .UseTaskLists()
                .Build();
            App.Log("Markdig pipeline created");

            // Initialize WebView2 after window is loaded
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

                // Update WebView2 background
                if (_webViewInitialized)
                {
                    MarkdownView.DefaultBackgroundColor = System.Drawing.Color.FromArgb(245, 245, 245);
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

                // Update WebView2 background
                if (_webViewInitialized)
                {
                    MarkdownView.DefaultBackgroundColor = System.Drawing.Color.FromArgb(30, 30, 30);
                }
            }

            // Refresh the current markdown if loaded
            if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
            {
                LoadMarkdownFile(_currentFilePath);
            }

            App.Log($"Applied theme: {theme} (effective: {effectiveTheme})");
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

            // Initialize WebView2 first
            await InitializeWebView();


            // Then check if a file was passed as argument
            if (!string.IsNullOrEmpty(App.FileToOpen) && File.Exists(App.FileToOpen))
            {
                App.Log($"Loading file from args: {App.FileToOpen}");
                LoadMarkdownFile(App.FileToOpen);
            }

            App.Log("MainWindow_Loaded completed");
        }

        private async Task InitializeWebView()
        {
            App.Log("InitializeWebView started");
            try
            {
                // Set environment to use a user data folder in temp
                var userDataFolder = Path.Combine(Path.GetTempPath(), "MarkSnap_WebView2");
                App.Log($"WebView2 user data folder: {userDataFolder}");

                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(
                    null, userDataFolder, null);
                App.Log("CoreWebView2Environment created");

                await MarkdownView.EnsureCoreWebView2Async(env);
                App.Log("EnsureCoreWebView2Async completed");

                MarkdownView.CoreWebView2.Settings.IsScriptEnabled = true;
                MarkdownView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;

                // Disable WebView2's built-in file drop handling
                MarkdownView.AllowExternalDrop = false;

                _webViewInitialized = true;

                // Apply theme to WebView2 background
                string effectiveTheme = _currentTheme == "System" ? GetSystemTheme() : _currentTheme;
                if (effectiveTheme == "Light")
                {
                    MarkdownView.DefaultBackgroundColor = System.Drawing.Color.FromArgb(245, 245, 245);
                }

                // If there's pending HTML to display, show it now
                if (_pendingHtml != null)
                {
                    MarkdownView.NavigateToString(_pendingHtml);
                    _pendingHtml = null;
                }

                StatusText.Text = "Ready";
                App.Log("WebView2 fully initialized");
            }
            catch (Exception ex)
            {
                App.Log($"WebView2 FAILED: {ex}");
                StatusText.Text = "WebView2 initialization failed";
                MessageBox.Show($"Failed to initialize WebView2: {ex.Message}\n\nPlease ensure WebView2 Runtime is installed.\n\nYou can download it from:\nhttps://developer.microsoft.com/en-us/microsoft-edge/webview2/",
                    "WebView2 Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Markdown files (*.md;*.markdown)|*.md;*.markdown|All files (*.*)|*.*",
                Title = "Open Markdown File"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadMarkdownFile(dialog.FileName);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
            {
                LoadMarkdownFile(_currentFilePath);
                StatusText.Text = "File refreshed";
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

        private void LoadMarkdownFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _currentFilePath = filePath;
                string markdown = File.ReadAllText(filePath);
                string html = Markdown.ToHtml(markdown, _markdownPipeline);

                string effectiveTheme = _currentTheme == "System" ? GetSystemTheme() : _currentTheme;
                string fullHtml = GenerateHtmlDocument(html, Path.GetFileName(filePath), effectiveTheme);

                if (_webViewInitialized)
                {
                    MarkdownView.NavigateToString(fullHtml);
                }
                else
                {
                    // Store for later when WebView2 is ready
                    _pendingHtml = fullHtml;
                }

                // Update UI
                WelcomePanel.Visibility = Visibility.Collapsed;
                FileNameText.Text = Path.GetFileName(filePath);
                Title = $"MarkSnap - {Path.GetFileName(filePath)}";
                TitleBarText.Text = $"MarkSnap - {Path.GetFileName(filePath)}";
                StatusText.Text = $"Loaded: {filePath}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error loading file";
            }
        }

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
                if (files?.Length > 0)
                {
                    App.Log($"Dropped file: {files[0]}");
                    string ext = Path.GetExtension(files[0]).ToLowerInvariant();
                    if (ext == ".md" || ext == ".markdown")
                    {
                        LoadMarkdownFile(files[0]);
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
                        App.Log($"Loaded window settings: {settings.Width}x{settings.Height} at ({settings.Left}, {settings.Top}), Theme: {_currentTheme}");
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
                var settings = new AppSettings
                {
                    Left = RestoreBounds.Left,
                    Top = RestoreBounds.Top,
                    Width = RestoreBounds.Width,
                    Height = RestoreBounds.Height,
                    IsMaximized = WindowState == WindowState.Maximized,
                    Theme = _currentTheme
                };

                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
                App.Log($"Saved window settings: {settings.Width}x{settings.Height} at ({settings.Left}, {settings.Top}), Theme: {settings.Theme}");
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
    }
}
