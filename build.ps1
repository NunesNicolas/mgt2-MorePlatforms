param([string]$GamePath = 'S:\SteamLibrary\steamapps\common\Mad Games Tycoon 2')
$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'build-references.json') | ConvertFrom-Json
$references = $manifest.compile | ForEach-Object { Join-Path $GamePath $_ }
$argsList = @('/nologo', '/noconfig', '/nostdlib+', '/target:library', '/optimize+', '/utf8output',
    "/out:$output\MorePlatforms.dll")
foreach ($reference in $references) {
    if (!(Test-Path -LiteralPath $reference)) { throw "Referencia ausente: $reference" }
    $argsList += "/reference:$reference"
}
$argsList += "$PSScriptRoot\src\MorePlatforms.cs"
$argsList += "$PSScriptRoot\src\ExtraData.cs"
$argsList += "$PSScriptRoot\src\CampaignSettings.cs"
$argsList += "$PSScriptRoot\src\PlatformScroll.cs"
& $compiler @argsList
if ($LASTEXITCODE -ne 0) { throw 'Falha na compilacao.' }
Get-FileHash -LiteralPath "$output\MorePlatforms.dll" -Algorithm SHA256
