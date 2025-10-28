# Minotaur Electron Desktop Application

This is the Electron-based desktop application wrapper for the Minotaur Grammar Tool, providing a native Linux desktop experience for the Blazor-based UI.

## Features

- Native Linux desktop application
- Integrated Blazor Server UI
- Full menu integration
- File system access for grammar files
- Automatic Blazor server management
- Cross-platform build support (AppImage, deb, rpm)

## Prerequisites

- Node.js 18+ and npm
- .NET 8 SDK
- Electron dependencies for Linux

## Installation

```bash
cd src/Minotaur.UI.Electron
npm install
```

## Development

### Running in Development Mode

```bash
npm run dev
```

This will:
1. Start the Blazor Server on http://localhost:5000
2. Wait for the server to be ready
3. Launch the Electron application

### Building for Production

```bash
# Build for current platform
npm run build

# Build specifically for Linux
npm run build-linux

# Create distributable package
npm run dist
```

## Project Structure

```
Minotaur.UI.Electron/
├── main.js              # Main Electron process
├── preload.js           # Preload script for secure IPC
├── renderer.js          # Renderer process integration
├── package.json         # Project dependencies and scripts
├── assets/              # Application assets (icons, etc.)
└── build/               # Build resources
```

## Key Components

### main.js
- Main Electron process
- Manages application lifecycle
- Starts and monitors Blazor Server
- Provides native menus and dialogs

### preload.js
- Secure bridge between Electron and web content
- Exposes limited IPC API to renderer
- Follows security best practices

### renderer.js
- Integration layer for Blazor
- Handles menu events
- Provides helper functions for file operations

## Menu Actions

The application provides comprehensive menu integration:

### File Menu
- New Grammar (Ctrl+N)
- Open Grammar (Ctrl+O)
- Save Grammar (Ctrl+S)
- Save Grammar As (Ctrl+Shift+S)
- Import Sample grammars
- Export to SVG/PNG/HTML
- Quit (Ctrl+Q)

### Edit Menu
- Standard editing operations (Cut, Copy, Paste, etc.)
- Find (Ctrl+F)
- Replace (Ctrl+H)

### View Menu
- Navigate between tabs (Ctrl+1-4)
- Toggle Dark Theme (Ctrl+T)
- Zoom controls
- Developer Tools
- Full screen

### Grammar Menu
- Parse Grammar (F5)
- Validate Grammar (F7)
- Generate Railroad Diagram (F6)
- Generate Parse Tree (F8)
- Format Grammar (Ctrl+Shift+F)

### Help Menu
- Documentation
- Report Issue
- About Dialog

## Build Targets

### Linux
- **AppImage**: Universal Linux package
- **deb**: Debian/Ubuntu package
- **rpm**: RedHat/Fedora package

Both x64 and arm64 architectures are supported.

## Configuration

Edit `package.json` to customize:
- Application name and version
- Build targets and architectures
- Publishing configuration
- Icon and asset paths

## Security

The application follows Electron security best practices:
- Context isolation enabled
- Node integration disabled in renderer
- Preload script for controlled IPC
- CSP headers
- Localhost-only connections for Blazor Server

## Troubleshooting

### Blazor Server Won't Start
- Ensure .NET 8 SDK is installed
- Check if port 5000 is available
- Review logs in electron-log output

### Port Already in Use
The application will automatically try port 5001 if 5000 is occupied.

### Build Fails
- Ensure all dependencies are installed: `npm install`
- Check electron-builder logs
- Verify icon and asset paths exist

## License

AGPL-3.0-or-later - See LICENSE file for details.

## Development by DevelApp AI

For more information, visit: https://github.com/DevelApp-ai/Minotaur
