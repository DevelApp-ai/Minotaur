#!/bin/bash

# Minotaur Electron Development Launcher
# This script helps developers quickly start the application

set -e  # Exit on error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

echo "╔════════════════════════════════════════════════════════════════╗"
echo "║       Minotaur Grammar Tool - Electron Development Mode       ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

# Check if dependencies are installed
if [ ! -d "$SCRIPT_DIR/node_modules" ]; then
    echo "📦 Installing dependencies..."
    npm install
    echo ""
fi

# Check if .NET is available
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found. Please install .NET 8 SDK."
    exit 1
fi

# Check if Node.js is available
if ! command -v node &> /dev/null; then
    echo "❌ Node.js not found. Please install Node.js 18+."
    exit 1
fi

echo "🚀 Starting Minotaur Electron App..."
echo ""
echo "This will:"
echo "  1. Start Blazor Server on http://localhost:5000"
echo "  2. Launch Electron desktop application"
echo ""
echo "Press Ctrl+C to stop the application"
echo ""

# Run the application
npm run dev
