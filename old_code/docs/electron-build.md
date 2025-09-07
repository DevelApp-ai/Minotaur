# DSL Designer - Electron Build Configuration

This file contains instructions for building the DSL Designer application for different platforms.

## Prerequisites

- Node.js 14+ and npm
- For Windows builds: Windows 10+ or Windows in a virtual machine
- For macOS builds: macOS 10.15+ (Catalina or newer)
- For Linux builds: Ubuntu 20.04+ or similar

## Build Commands

### Development Mode

To run the application in development mode:

```bash
npm run electron:dev
```

This will start both the React development server and Electron, with hot reloading enabled.

### Production Builds

#### Build for all platforms (from respective OS)

```bash
npm run electron:build
```

#### Build for Windows

```bash
npm run electron:build:win
```

This will create:
- NSIS installer (.exe)
- Portable version (.exe)

#### Build for macOS

```bash
npm run electron:build:mac
```

This will create:
- DMG installer (.dmg)
- Zipped application (.zip)

#### Build for Linux

```bash
npm run electron:build:linux
```

This will create:
- AppImage (.AppImage)
- Debian package (.deb)

## Build Output

All build outputs will be placed in the `dist` directory.

## Code Signing

For production releases, you should sign your application:

1. For Windows, use a code signing certificate
2. For macOS, use an Apple Developer certificate
3. For Linux, consider using GPG signatures for your packages

Add the appropriate configuration to the `build` section in package.json.

## Distribution

- Windows: Distribute the NSIS installer or portable executable
- macOS: Distribute the DMG file
- Linux: Distribute the AppImage or deb package

## Troubleshooting

If you encounter build issues:

1. Ensure you have the latest version of electron-builder
2. Check that you're building on the correct platform
3. Verify that all dependencies are installed
4. Check the electron-builder logs for specific errors
