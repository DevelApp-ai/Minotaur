# Minotaur (develapp.minotaur) — M6 choco lane (SBOB-Forge TDS 10.3).
#
# Removes the `minotaur` shim. The SBoB payload under tools/sbob/ is removed
# with the package directory by Chocolatey itself; this script never touches it.
$ErrorActionPreference = 'Stop'

Uninstall-BinFile -Name 'minotaur' -Path (Join-Path $PSScriptRoot 'minotaur\Minotaur.Demo.exe')
