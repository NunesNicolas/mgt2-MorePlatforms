param(
    [string]$GamePath = 'S:\SteamLibrary\steamapps\common\Mad Games Tycoon 2',
    [string]$ExpectedVersion = ''
)
$ErrorActionPreference = 'Stop'
$source = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'src\MorePlatforms.cs')
$match = [regex]::Match($source, '\[BepInPlugin\(Id,\s*"[^"]+",\s*"(\d+\.\d+\.\d+)"\)\]')
if (!$match.Success) { throw 'Versao do plugin nao encontrada.' }
$version = $match.Groups[1].Value
if ($ExpectedVersion -and $ExpectedVersion -ne $version) {
    throw "Tag $ExpectedVersion nao corresponde a versao $version do plugin."
}
& (Join-Path $PSScriptRoot 'build.ps1') -GamePath $GamePath
& (Join-Path $PSScriptRoot 'tests\verify.ps1') -GamePath $GamePath

$dist = Join-Path $PSScriptRoot 'dist'
$staging = Join-Path $PSScriptRoot ('build\package-' + [guid]::NewGuid().ToString('N'))
$payload = Join-Path $staging 'mod'
$pluginDir = Join-Path $payload 'BepInEx\plugins'
New-Item -ItemType Directory -Force -Path $pluginDir, $dist | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'build\MorePlatforms.dll') -Destination $pluginDir
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs\INSTALL.txt') -Destination (Join-Path $payload 'MorePlatforms-README.txt')
$metadata = [ordered]@{
    name = 'More Platforms'; version = $version; pluginId = 'br.sapo.mgt2.moreplatforms'
    game = 'Mad Games Tycoon 2'; gameBuildTested = '20855306'
    loaderTested = 'BepInEx 5.4.23.3 Windows x64'
    dllSha256 = (Get-FileHash -LiteralPath (Join-Path $pluginDir 'MorePlatforms.dll')).Hash
}
$metadata | ConvertTo-Json | Set-Content -Encoding UTF8 (Join-Path $payload 'MorePlatforms-manifest.json')
$modZip = Join-Path $dist "MorePlatforms-$version.zip"
Compress-Archive -Path (Join-Path $payload '*') -DestinationPath $modZip -Force

# Explicit source allowlist: never archive the whole workspace (game analysis/saves).
$sourcePayload = Join-Path $staging 'source'
New-Item -ItemType Directory -Force -Path $sourcePayload | Out-Null
foreach ($relative in @('src', 'tests', 'scripts', 'docs', '.github', '.gitignore',
    'README.md', 'VALIDATION.md', 'build-references.json', 'build.ps1', 'install.ps1', 'package.ps1')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $relative) -Destination $sourcePayload -Recurse
}
# .NET ZipFile includes .github and .gitignore, including on PowerShell versions
# where Compress-Archive silently omits hidden entries.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$sourceZip = Join-Path $dist "MorePlatforms-source-$version.zip"
$temporaryZip = Join-Path $staging 'source.zip'
[IO.Compression.ZipFile]::CreateFromDirectory($sourcePayload, $temporaryZip)
Copy-Item -LiteralPath $temporaryZip -Destination $sourceZip -Force

# Verify the public mod ZIP contains only the intended payload.
$archive = [IO.Compression.ZipFile]::OpenRead($modZip)
try {
    $expected = @('BepInEx/plugins/MorePlatforms.dll', 'MorePlatforms-README.txt', 'MorePlatforms-manifest.json')
    $files = @($archive.Entries | Where-Object { $_.Name } | ForEach-Object { $_.FullName.Replace('\', '/') })
    if (@(Compare-Object ($expected | Sort-Object) ($files | Sort-Object)).Count -ne 0) {
        throw 'Conteudo inesperado no ZIP publico.'
    }
} finally { $archive.Dispose() }
$sums = @($modZip, $sourceZip) | ForEach-Object {
    '{0}  {1}' -f (Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant(), (Split-Path $_ -Leaf)
}
$sums | Set-Content -Encoding ASCII (Join-Path $dist 'SHA256SUMS.txt')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs\RELEASE-NOTES.md') -Destination $dist -Force
Write-Output "Pacote Nexus: $modZip"
Write-Output "Codigo-fonte para GitHub: $sourceZip"
