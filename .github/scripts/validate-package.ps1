$ErrorActionPreference = 'Stop'
$buildRoot = Join-Path $PSScriptRoot '../../artifacts/build'
$archivePath = Join-Path $buildRoot 'WrathComboEnhanced/latest.zip'
if (-not (Test-Path -LiteralPath $archivePath)) { throw 'DalamudPackager did not produce the Enhanced archive.' }
$manifest = Get-Content (Join-Path $buildRoot 'WrathComboEnhanced.json') -Raw | ConvertFrom-Json
[xml]$project = Get-Content (Join-Path $PSScriptRoot '../../WrathCombo/WrathCombo.csproj')
$version = @($project.Project.PropertyGroup.Version | Where-Object { $_ })[0]
if ($manifest.InternalName -ne 'WrathComboEnhanced' -or $manifest.Name -ne 'Wrath Combo Enhanced') { throw 'Incorrect plugin identity.' }
if ($manifest.AssemblyVersion -ne $version) { throw 'Manifest version does not match the project.' }
if ($manifest.DalamudApiLevel -ne 15) { throw 'Unexpected Dalamud API version.' }
$archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path $archivePath))
try {
    $entries = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
    foreach ($required in @('WrathComboEnhanced.dll', 'WrathComboEnhanced.json', 'ECommons.dll', 'PunishLib.dll', 'WrathCombo.API.dll', 'LICENSE.txt')) {
        if ($entries -notcontains $required) { throw "Package is missing $required" }
    }
    if ($entries -contains 'WrathCombo.dll' -or $entries -contains 'WrathCombo.json') { throw 'Package contains the original plugin identity.' }
    if ($entries | Where-Object { $_ -match 'TeachingMode.json|before-enhanced|\.bak$|(^|/)\.git/|(^|/)\.\./' }) { throw 'Unexpected private or unsafe file in package.' }
    foreach ($culture in @('fr', 'de', 'ja', 'ko', 'zh-Hans', 'zh-Hant')) {
        if ($entries -notcontains "$culture/WrathComboEnhanced.resources.dll") { throw "Missing $culture localization" }
    }
    Write-Output "Verified $($entries.Count) package entries; version $version; API $($manifest.DalamudApiLevel)."
} finally { $archive.Dispose() }
$destination = Join-Path $PSScriptRoot '../../artifacts/WrathComboEnhanced.zip'
Copy-Item -LiteralPath $archivePath -Destination $destination -Force
Get-FileHash -LiteralPath $destination -Algorithm SHA256
