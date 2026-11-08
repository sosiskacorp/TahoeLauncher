# ==============================================================================
# Tahoe Liquid Glass Launcher & Shell (v26.2) — Revert & Restore Windows 11
# Run: powershell -ExecutionPolicy Bypass -File .\revert.ps1
# ==============================================================================

$ErrorActionPreference = 'SilentlyContinue'
$rootDir = if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path }
$tahoeExe = Join-Path $rootDir 'MacOSTahoe.exe'

if (Test-Path $tahoeExe) {
    Start-Process -FilePath $tahoeExe -ArgumentList '--quit' -Wait -WindowStyle Hidden
}
cmd /c "taskkill /F /IM MacOSTahoe.exe >nul 2>&1"
Stop-Process -Name 'MacOSTahoe' -Force -ErrorAction SilentlyContinue

Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'macOS_Tahoe_Apply' -ErrorAction SilentlyContinue
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'TahoeLauncher' -ErrorAction SilentlyContinue

Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name 'HideIcons' -Value 0 -Type DWord

$srPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3'
if (Test-Path $srPath) {
    $sr = (Get-ItemProperty -Path $srPath -Name 'Settings').Settings
    if ($sr -and $sr.Length -ge 9) {
        $sr[8] = 2
        Set-ItemProperty -Path $srPath -Name 'Settings' -Value $sr
    }
}

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class RevertNative {
  [DllImport("user32.dll", CharSet=CharSet.Unicode)]
  public static extern bool SystemParametersInfo(uint a, uint b, string c, uint d);
  [DllImport("user32.dll")]
  public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
  [DllImport("user32.dll")]
  public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
"@

$trayWnd = [RevertNative]::FindWindow("Shell_TrayWnd", $null)
if ($trayWnd -ne [IntPtr]::Zero) {
    [RevertNative]::ShowWindow($trayWnd, 5) | Out-Null
}
$secTrayWnd = [RevertNative]::FindWindow("Shell_SecondaryTrayWnd", $null)
if ($secTrayWnd -ne [IntPtr]::Zero) {
    [RevertNative]::ShowWindow($secTrayWnd, 5) | Out-Null
}

Stop-Process -Name explorer -Force
