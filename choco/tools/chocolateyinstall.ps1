# Minotaur (develapp.minotaur) — M6 choco lane (SBOB-Forge TDS 10.3).
#
# Installs the `minotaur` shim for the Minotaur.Demo CLI entry point. The SBoB
# that ships under tools/sbob/ is DATA: this script never reads, verifies, or
# acts on it (TDS 10.3 — the behavior declaration is inert payload; the
# contract is verified by the registry pipeline, not at install time).
$ErrorActionPreference = 'Stop'

$exePath = Join-Path $PSScriptRoot 'minotaur\Minotaur.Demo.exe'
if (-not (Test-Path $exePath)) {
    throw "minotaur payload not found at $exePath — the package layout is broken"
}

Install-BinFile -Name 'minotaur' -Path $exePath
