#!/bin/bash

echo "🚀 Testing Minotaur Electron Setup"
echo ""

# Test .NET installation
echo "📋 Checking .NET SDK..."
if command -v dotnet &> /dev/null; then
    dotnet --version
    echo "✅ .NET SDK found"
else
    echo "❌ .NET SDK not found"
    exit 1
fi

# Test Node.js
echo ""
echo "📋 Checking Node.js..."
if command -v node &> /dev/null; then
    node --version
    echo "✅ Node.js found"
else
    echo "❌ Node.js not found"
    exit 1
fi

# Test npm
echo ""
echo "📋 Checking npm..."
if command -v npm &> /dev/null; then
    npm --version
    echo "✅ npm found"
else
    echo "❌ npm not found"
    exit 1
fi

echo ""
echo "📋 Checking Electron project files..."

if [ -f "package.json" ]; then
    echo "✅ package.json found"
else
    echo "❌ package.json not found"
    exit 1
fi

if [ -f "main.js" ]; then
    echo "✅ main.js found"
else
    echo "❌ main.js not found"
    exit 1
fi

if [ -f "preload.js" ]; then
    echo "✅ preload.js found"
else
    echo "❌ preload.js not found"
    exit 1
fi

if [ -f "renderer.js" ]; then
    echo "✅ renderer.js found"
else
    echo "❌ renderer.js not found"
    exit 1
fi

if [ -f "assets/icon.png" ]; then
    echo "✅ icon.png found"
else
    echo "❌ icon.png not found"
    exit 1
fi

echo ""
echo "📋 Checking Blazor UI project..."
if [ -d "../Minotaur.UI.Blazor" ]; then
    echo "✅ Blazor UI project found"
else
    echo "❌ Blazor UI project not found"
    exit 1
fi

echo ""
echo "🎉 All prerequisites and files are in place!"
echo ""
echo "Next steps:"
echo "  1. Run 'npm install' to install dependencies"
echo "  2. Run 'npm run dev' to start the application in development mode"
echo "  3. Run 'npm run build-linux' to create Linux distributable packages"
