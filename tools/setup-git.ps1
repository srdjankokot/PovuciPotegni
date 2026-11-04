# Sets up Unity Smart Merge and Git LFS for this clone. Run once after cloning (Windows).
# Usage: powershell -ExecutionPolicy Bypass -File tools\setup-git.ps1 [path-to-UnityYAMLMerge.exe]
param([string]$MergeToolPath)

# The same quoting must work in Windows PowerShell 5.1 and PowerShell 7.
$PSNativeCommandArgumentPassing = 'Legacy'

Set-Location (Split-Path -Parent $PSScriptRoot)

function Stop-Setup([string]$Message) {
    Write-Host $Message -ForegroundColor Red
    exit 1
}

cmd /c "git lfs version >nul 2>&1"
if ($LASTEXITCODE -ne 0) {
    Stop-Setup "Git LFS nije instaliran. Instaliraj ga sa https://git-lfs.com, pa ponovo pokreni skriptu."
}

$versionLine = Select-String -Path "ProjectSettings/ProjectVersion.txt" -Pattern '^m_EditorVersion:\s*(\S+)' | Select-Object -First 1
if (-not $versionLine) {
    Stop-Setup "Ne mogu da pročitam Unity verziju iz ProjectSettings/ProjectVersion.txt."
}
$version = $versionLine.Matches[0].Groups[1].Value

if (-not $MergeToolPath) {
    $MergeToolPath = "C:/Program Files/Unity/Hub/Editor/$version/Editor/Data/Tools/UnityYAMLMerge.exe"
}

if (-not (Test-Path -LiteralPath $MergeToolPath -PathType Leaf)) {
    Stop-Setup "UnityYAMLMerge nije pronađen za Unity ${version}: $MergeToolPath`nInstaliraj tu verziju iz Unity Huba ili prosledi putanju: tools\setup-git.ps1 <putanja>"
}

$mergeTool = $MergeToolPath -replace '\\', '/'
# Git's temp files have no extension, so without --force the tool refuses to merge them.
# Without "--fallback none" a real conflict opens a GUI tool and git waits until it is closed.
$mergeArguments = ' merge -p -h --force --fallback none %O %B %A %A'
$expectedDriver = '"' + $mergeTool + '"' + $mergeArguments

git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver ('\"' + $mergeTool + '\"' + $mergeArguments)
git config merge.unityyamlmerge.recursive binary
git lfs install
if ($LASTEXITCODE -ne 0) {
    Stop-Setup "git lfs install nije uspeo."
}

$driver = git config --get merge.unityyamlmerge.driver
if ($driver -ne $expectedDriver) {
    Stop-Setup "Smart Merge driver nije dobro upisan: $driver"
}

Write-Host "Unity verzija: $version"
Write-Host "Smart Merge driver: $driver"
