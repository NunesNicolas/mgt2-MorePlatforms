param([string]$GamePath = 'S:\SteamLibrary\steamapps\common\Mad Games Tycoon 2')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$destination = Join-Path $root 'private-build-references'
$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'build-references.json') | ConvertFrom-Json
$hashes = @()
foreach ($relative in @($manifest.compile) + @($manifest.verification)) {
    $source = Join-Path $GamePath $relative
    if (!(Test-Path -LiteralPath $source)) { throw "Referencia ausente: $relative" }
    $target = Join-Path $destination $relative
    New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
    Copy-Item -LiteralPath $source -Destination $target
    $hashes += [ordered]@{ path = $relative; sha256 = (Get-FileHash -LiteralPath $target).Hash }
}
$hashes | ConvertTo-Json | Set-Content -Encoding UTF8 (Join-Path $destination 'manifest.json')
'PRIVATE BUILD INPUTS ONLY. Do not publish these files or add them to mod downloads.' |
    Set-Content -Encoding UTF8 (Join-Path $destination 'README.txt')
Write-Output "Referencias exportadas para uso privado: $destination"
