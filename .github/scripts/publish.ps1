$ErrorActionPreference = 'Stop'
$manifest = Get-Content artifacts/build/WrathComboEnhanced.json -Raw | ConvertFrom-Json
$version = [string]$manifest.AssemblyVersion
$tag = "enhanced-v$version"
$repository = $env:GITHUB_REPOSITORY
if ($repository -ne 'Arceuid731/WrathCombo') { throw 'Publication is limited to the Enhanced repository.' }
gh release view $tag --repo $repository --json tagName > $null 2>&1
if ($LASTEXITCODE -eq 0) {
    $tagCommit = git rev-list -n 1 $tag
    if ($tagCommit -ne $env:GITHUB_SHA) { throw 'This version is already published. Bump the project version before releasing changes.' }
    Write-Output "$tag already exists; keeping the immutable release."
} else {
    gh release create $tag artifacts/WrathComboEnhanced.zip --repo $repository --target $env:GITHUB_SHA --title "Wrath Combo Enhanced $version" --notes-file release-notes.md
    if ($LASTEXITCODE -ne 0) { throw 'GitHub release creation failed.' }
}
$download = "https://github.com/$repository/releases/download/$tag/WrathComboEnhanced.zip"
$asset = gh release view $tag --repo $repository --json assets | ConvertFrom-Json
if (-not ($asset.assets | Where-Object { $_.name -eq 'WrathComboEnhanced.zip' })) { throw 'Release asset is missing.' }
foreach ($key in @('DownloadLinkInstall', 'DownloadLinkUpdate', 'DownloadLinkTesting')) {
    $manifest | Add-Member -NotePropertyName $key -NotePropertyValue $download -Force
}
$manifest | Add-Member -NotePropertyName LastUpdate -NotePropertyValue ([DateTimeOffset]::UtcNow.ToUnixTimeSeconds()) -Force
$manifest | ConvertTo-Json -Depth 20 -AsArray | Set-Content repo.json -Encoding utf8
git config user.name 'Wrath Combo Enhanced Release'
git config user.email 'actions@users.noreply.github.com'
git add repo.json
git diff --cached --quiet
if ($LASTEXITCODE -ne 0) {
    git commit -m "Publish Wrath Combo Enhanced $version Dalamud manifest"
    if ($LASTEXITCODE -ne 0) { throw 'Manifest commit failed.' }
    git push origin HEAD:main
    if ($LASTEXITCODE -ne 0) { throw 'Manifest push failed.' }
}
