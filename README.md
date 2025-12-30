# MarkSnap

A fast, lightweight Markdown viewer for Windows with a modern dark theme.

![Windows](https://img.shields.io/badge/platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **Instant Preview** - View Markdown files with beautiful, GitHub-style rendering
- **Dark Theme** - Easy on the eyes with a modern dark interface
- **Drag & Drop** - Simply drag `.md` files onto the window to view them
- **File Association** - Register as the default handler for `.md` and `.markdown` files
- **Live Refresh** - Quickly reload the current file to see changes
- **Window Memory** - Remembers your window size and position between sessions
- **Rich Markdown Support** - Tables, task lists, code blocks, emojis, and more

## Screenshots

*MarkSnap displaying a Markdown file with syntax highlighting and dark theme*

## Installation

### Prerequisites

- Windows 10/11
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (included with modern Windows)

### Download

Download the latest release from the [Releases](../../releases) page.

### Build from Source

```bash
git clone https://github.com/yourusername/marksnap.git
cd marksnap
dotnet build -c Release
```

The executable will be in `MarkSnap/bin/Release/net10.0-windows/`.

## Usage

### Opening Files

There are several ways to open Markdown files:

1. **Drag and Drop** - Drag any `.md` or `.markdown` file onto the MarkSnap window
2. **Open Button** - Click the "Open" button in the toolbar to browse for a file
3. **Command Line** - Run `MarkSnap.exe "path/to/file.md"`
4. **File Association** - Double-click any `.md` file (after registering MarkSnap as the default handler)

### Setting as Default Markdown Viewer

1. Open MarkSnap
2. Click "Set as Default" in the toolbar
3. Confirm the registration

Now double-clicking any `.md` file will open it in MarkSnap.

### Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Refresh current file | Click "Refresh" button |

## Markdown Support

MarkSnap uses [Markdig](https://github.com/xoofx/markdig) for Markdown parsing, supporting:

- **CommonMark** - Full CommonMark specification
- **Tables** - GitHub Flavored Markdown tables
- **Task Lists** - Interactive checkboxes
- **Code Blocks** - Syntax highlighting for code
- **Emojis** - `:emoji:` shortcodes
- **Strikethrough** - ~~deleted text~~
- **Autolinks** - Automatic URL detection
- **And more** - Advanced extensions enabled

## Configuration

MarkSnap stores its settings in:
```
%LocalAppData%\MarkSnap\settings.json
```

Currently saved settings:
- Window position (X, Y)
- Window size (Width, Height)
- Maximized state

## Tech Stack

- **Framework**: WPF (.NET 8/9/10)
- **Markdown Engine**: [Markdig](https://github.com/xoofx/markdig)
- **Rendering**: [WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (Chromium-based)
- **Language**: C# 12

## Project Structure

```
marksnap/
├── MarkSnap/
│   ├── App.xaml              # Application entry point
│   ├── App.xaml.cs           # Application startup logic
│   ├── MainWindow.xaml       # Main window UI layout
│   ├── MainWindow.xaml.cs    # Window logic and Markdown rendering
│   └── MarkSnap.csproj       # Project configuration
└── README.md
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Markdig](https://github.com/xoofx/markdig) - Fast, powerful Markdown processor for .NET
- [WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) - Modern web rendering engine
