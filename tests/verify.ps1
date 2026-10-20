param([string]$GamePath = 'S:\SteamLibrary\steamapps\common\Mad Games Tycoon 2')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$core = Join-Path $GamePath 'BepInEx\core'
$managed = Join-Path $GamePath 'Mad Games Tycoon 2_Data\Managed'
$cecil = [Reflection.Assembly]::Load([IO.File]::ReadAllBytes("$core\Mono.Cecil.dll"))
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$managed\Assembly-CSharp.dll")
$views = @{
    Menu_Dev_GameEntwicklungsbericht = 'Init'
    Menu_Dev_MarketAnalyse = 'Init'
    Menu_Dev_ShowConcept = 'Init'
    Menu_Dev_TochterfirmaGameComplete = 'Init'
    Menu_Stats_Tochterfirma_GameEntwicklungsbericht = 'Init'
    Menu_Dev_ChangePlatform = 'Update'
    Item_PubOfferSelect = 'SetData'
}
foreach ($entry in $views.GetEnumerator()) {
    $type = $game.MainModule.Types | Where-Object Name -eq $entry.Key
    $method = $type.Methods | Where-Object Name -eq $entry.Value
    $il = $method.Body.Instructions
    $matches = 0
    for ($i = 2; $i -lt $il.Count; $i++) {
        if ($il[$i].OpCode.Name -eq 'conv.i4' -and $il[$i-1].OpCode.Name -eq 'ldlen' -and
            $il[$i-2].Operand -is [Mono.Cecil.FieldReference] -and
            $il[$i-2].Operand.FullName -eq 'System.Int32[] gameScript::gamePlatform') { $matches++ }
    }
    if ($matches -ne 1) { throw "Layout incompativel: $($entry.Key) ($matches)" }
    Write-Output "OK: $($entry.Key).$($entry.Value)"
}
$plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$root\build\MorePlatforms.dll")
$patchCount = 0
foreach ($type in $plugin.MainModule.Types) {
    foreach ($attribute in $type.CustomAttributes) {
        if ($attribute.AttributeType.Name -ne 'HarmonyPatch' -or $attribute.ConstructorArguments.Count -ne 2) { continue }
        $typeName = $attribute.ConstructorArguments[0].Value.FullName
        $methodName = $attribute.ConstructorArguments[1].Value
        $target = $game.MainModule.Types | Where-Object FullName -eq $typeName
        if (!($target.Methods | Where-Object Name -eq $methodName)) { throw "Patch ausente: $typeName.$methodName" }
        $patchCount++
    }
}
Write-Output "OK: $patchCount alvos declarativos de patches encontrados."
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo "/out:$root\build\ExtraDataTests.exe" "$root\tests\ExtraDataTests.cs" "$root\src\ExtraData.cs"
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar testes.' }
& "$root\build\ExtraDataTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Falha nos testes.' }
