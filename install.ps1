param([string]$GamePath = 'S:\SteamLibrary\steamapps\common\Mad Games Tycoon 2')
$ErrorActionPreference = 'Stop'
if (Get-Process -Name 'Mad Games Tycoon 2' -ErrorAction SilentlyContinue) {
    throw 'Feche o jogo antes de instalar.'
}
$source = Join-Path $PSScriptRoot 'build\MorePlatforms.dll'
if (!(Test-Path -LiteralPath $source)) { throw 'Compile primeiro com build.ps1.' }
$plugins = Join-Path $GamePath 'BepInEx\plugins'
if (!(Test-Path -LiteralPath $plugins)) { throw 'Instalacao do BepInEx nao encontrada.' }
$target = Join-Path $plugins 'MorePlatforms.dll'
if (Test-Path -LiteralPath $target) {
    $backup = Join-Path $PSScriptRoot ('backups\plugin-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    Copy-Item -LiteralPath $target -Destination $backup
}
Copy-Item -LiteralPath $source -Destination $target
if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash) {
    throw 'Hash da copia nao confere.'
}
Write-Output "Instalado: $target"
