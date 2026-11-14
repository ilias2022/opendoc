# Publishes the app + shell extension to C:\Tools\OpenInGoogleDocs and registers the
# Windows 11 top-level right-click entry. Run from the repo root in Windows PowerShell.
# Requires Developer Mode (Settings > System > For developers), because the package is unsigned.
param([string]$InstallDir = "C:\Tools\OpenInGoogleDocs")

$ErrorActionPreference = "Stop"
$repo = $PSScriptRoot

$devMode = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock" -ErrorAction SilentlyContinue).AllowDevelopmentWithoutDevLicense
if ($devMode -ne 1) {
    throw "Turn on Developer Mode first (Settings > System > For developers), then run this again."
}

# Remove any previous registration so the files aren't locked while we overwrite them.
Get-AppxPackage OpenInGoogleDocs.ShellExtension | Remove-AppxPackage

dotnet publish "$repo\src\OpenInGoogleDocs" -c Release -r win-x64 --self-contained false -o $InstallDir
dotnet publish "$repo\src\OpenInGoogleDocs.ShellExtension" -c Release -o $InstallDir

# Manifest + a placeholder logo (Windows insists the file exists).
Copy-Item "$repo\src\OpenInGoogleDocs.ShellExtension\AppxManifest.xml" $InstallDir -Force
New-Item -ItemType Directory -Force "$InstallDir\Assets" | Out-Null
Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap 150, 150
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(66, 133, 244))
$bmp.Save("$InstallDir\Assets\Logo.png", [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()

Add-AppxPackage -Register "$InstallDir\AppxManifest.xml" -ExternalLocation $InstallDir
Write-Host "Done. Right-click a .docx: 'Open in Google Docs' should be in the main menu."
Write-Host "If it doesn't show yet, restart Explorer:  Stop-Process -Name explorer"
