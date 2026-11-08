# ==============================================================================
# Tahoe Liquid Glass Launcher & Shell (v26.2) — Apply & Start Script
# Run: powershell -ExecutionPolicy Bypass -File .\apply.ps1
# ==============================================================================

$ErrorActionPreference = 'SilentlyContinue'
$rootDir = if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path }

# 0. Stop conflicting shells (e.g. Seelen UI) if present
Stop-Process -Name 'seelen-ui' -Force -ErrorAction SilentlyContinue
Stop-Process -Name 'slu-service' -Force -ErrorAction SilentlyContinue

# 1. Windows 11 Dark Mode + Transparency
$personalize = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize'
Set-ItemProperty -Path $personalize -Name 'AppsUseLightTheme' -Value 0 -Type DWord
Set-ItemProperty -Path $personalize -Name 'SystemUsesLightTheme' -Value 0 -Type DWord
Set-ItemProperty -Path $personalize -Name 'EnableTransparency' -Value 1 -Type DWord
Set-ItemProperty -Path $personalize -Name 'ColorPrevalence' -Value 0 -Type DWord

# 2. Optional macOS Cursors (if bundled in cursors/)
$curSrc = Join-Path $rootDir 'cursors'
$curDst = Join-Path $env:LOCALAPPDATA 'Microsoft\Windows\Cursors\macOS'
if (Test-Path $curSrc) {
    New-Item -ItemType Directory -Path $curDst -Force | Out-Null
    Copy-Item "$curSrc\*" -Destination $curDst -Force
    $curReg = 'HKCU:\Control Panel\Cursors'
    $cursorMap = @{
        'Arrow'       = 'normal.cur'
        'Help'        = 'help.cur'
        'AppStarting' = 'busy.ani'
        'Wait'        = 'wait.ani'
        'Crosshair'   = 'precision.cur'
        'IBeam'       = 'text.cur'
        'NWPen'       = 'handwriting.cur'
        'No'          = 'unavailable.cur'
        'SizeNS'      = 'vertical.cur'
        'SizeWE'      = 'horizontal.cur'
        'SizeNWSE'    = 'diagonal1.cur'
        'SizeNESW'    = 'diagonal2.cur'
        'SizeAll'     = 'move.cur'
        'UpArrow'     = 'alternate.cur'
        'Hand'        = 'link.cur'
        'Pin'         = 'person.cur'
        'Person'      = 'pin.cur'
    }
    Set-ItemProperty -Path $curReg -Name '(default)' -Value 'macOS'
    Set-ItemProperty -Path $curReg -Name 'Scheme Source' -Value 1 -Type DWord
    foreach ($k in $cursorMap.Keys) {
        $file = Join-Path $curDst $cursorMap[$k]
        if (Test-Path $file) {
            Set-ItemProperty -Path $curReg -Name $k -Value $file
        }
    }
}

# 3. 6K Tahoe Wallpaper
$wp = Join-Path $rootDir 'wallpapers\26-Tahoe-Dark-6K.jpg'
if (Test-Path $wp) {
    Set-ItemProperty -Path 'HKCU:\Control Panel\Desktop' -Name 'WallPaper' -Value $wp
    Set-ItemProperty -Path 'HKCU:\Control Panel\Desktop' -Name 'WallpaperStyle' -Value '10'
    Set-ItemProperty -Path 'HKCU:\Control Panel\Desktop' -Name 'TileWallpaper' -Value '0'
}

# 4. Auto-hide native Windows 11 taskbar
$adv = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced'
Set-ItemProperty -Path $adv -Name 'ShowTaskViewButton' -Value 0 -Type DWord
Set-ItemProperty -Path $adv -Name 'TaskbarDa' -Value 0 -Type DWord

$srPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3'
if (Test-Path $srPath) {
    $sr = (Get-ItemProperty -Path $srPath -Name 'Settings').Settings
    if ($sr -and $sr.Length -ge 9 -and $sr[8] -ne 3) {
        $sr[8] = 3
        Set-ItemProperty -Path $srPath -Name 'Settings' -Value $sr
    }
}

# 5. Apply system parameters & hide native taskbar
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class MacOSNative {
  [DllImport("user32.dll", CharSet=CharSet.Unicode)]
  public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);
  [DllImport("user32.dll")]
  public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
  [DllImport("user32.dll")]
  public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
"@

if (Test-Path $wp) {
    [MacOSNative]::SystemParametersInfo(0x0014, 0, $wp, 3) | Out-Null
}
[MacOSNative]::SystemParametersInfo(0x0057, 0, $null, 3) | Out-Null

$trayWnd = [MacOSNative]::FindWindow("Shell_TrayWnd", $null)
if ($trayWnd -ne [IntPtr]::Zero) {
    [MacOSNative]::ShowWindow($trayWnd, 0) | Out-Null
}
$secTrayWnd = [MacOSNative]::FindWindow("Shell_SecondaryTrayWnd", $null)
if ($secTrayWnd -ne [IntPtr]::Zero) {
    [MacOSNative]::ShowWindow($secTrayWnd, 0) | Out-Null
}

# 6. Optimize instant startup (0 delay) and register direct binary in Run
$serPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize'
if (-not (Test-Path $serPath)) { New-Item -Path $serPath -Force | Out-Null }
Set-ItemProperty -Path $serPath -Name 'StartupDelayInMSec' -Value 0 -Type DWord

$tahoeExe = Join-Path $rootDir 'MacOSTahoe.exe'
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'macOS_Tahoe_Apply' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'TahoeLauncher' -Value $tahoeExe

# 7. Build if MacOSTahoe.exe is missing, then launch
$buildScript = Join-Path $rootDir 'build.ps1'
if (-not (Test-Path $tahoeExe) -and (Test-Path $buildScript)) {
    & powershell -ExecutionPolicy Bypass -File $buildScript
}

if ((Test-Path $tahoeExe) -and -not (Get-Process -Name 'MacOSTahoe' -ErrorAction SilentlyContinue)) {
    Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{CommandLine = $tahoeExe; CurrentDirectory = $rootDir} | Out-Null
}
