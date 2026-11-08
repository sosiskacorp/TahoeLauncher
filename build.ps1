# ==============================================================================
# Tahoe Liquid Glass Launcher & Shell (v26.2) — Build Script
# Compiles src\MacOSTahoe.cs using the built-in Windows .NET Framework 4.x compiler
# ==============================================================================

$ErrorActionPreference = 'Stop'
$rootDir = if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path }
$srcFile = Join-Path $rootDir 'src\MacOSTahoe.cs'
$outExe  = Join-Path $rootDir 'MacOSTahoe.exe'
$iconIco = Join-Path $rootDir 'icons\launchpad.ico'

$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    throw "Compiler not found at $csc"
}

$wpfDir = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF'

cmd /c "taskkill /F /IM MacOSTahoe.exe >nul 2>&1"
Stop-Process -Name 'MacOSTahoe' -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

$cscArgs = @(
    '/nologo',
    '/target:winexe',
    '/optimize+',
    "/out:$outExe",
    "/r:$wpfDir\PresentationCore.dll",
    "/r:$wpfDir\PresentationFramework.dll",
    "/r:$wpfDir\WindowsBase.dll",
    '/r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Xaml.dll',
    '/r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Drawing.dll',
    '/r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Data.dll',
    '/r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Xml.dll',
    '/r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll'
)

if (Test-Path $iconIco) {
    $cscArgs += "/win32icon:$iconIco"
}

$cscArgs += $srcFile

Write-Host "Building Tahoe Liquid Glass Launcher -> $outExe ..." -ForegroundColor Cyan
& $csc @cscArgs
if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}
Write-Host "Build succeeded: $outExe ($((Get-Item $outExe).Length) bytes)" -ForegroundColor Green
