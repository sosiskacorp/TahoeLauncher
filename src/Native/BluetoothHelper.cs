using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace MacOSTahoe
{
    public static class BluetoothHelper
    {
        public static bool IsBluetoothEnabled { get; private set; }
        public static event Action StateChanged;

        private static bool _initialized = false;
        private static bool _isToggling = false;

        static BluetoothHelper()
        {
            IsBluetoothEnabled = true; // default optimistic
        }

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            RefreshStateAsync();
        }

        public static void RefreshStateAsync()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    string scriptPath = GetScriptPath();
                    if (!File.Exists(scriptPath)) EnsureScriptExists(scriptPath);

                    ProcessStartInfo psi = new ProcessStartInfo("powershell.exe",
                        "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\" -Kind Bluetooth -Action Get")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        StandardOutputEncoding = Encoding.UTF8
                    };

                    using (Process p = Process.Start(psi))
                    {
                        string outStr = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(3000);
                        // Output format: RADIO_STATE|Bluetooth|On or Off
                        if (outStr.Contains("RADIO_STATE|Bluetooth|"))
                        {
                            bool on = outStr.Contains("|On");
                            IsBluetoothEnabled = on;
                            if (StateChanged != null) StateChanged();
                        }
                    }
                }
                catch { }
            });
        }

        public static void ToggleBluetooth()
        {
            SetBluetoothEnabled(!IsBluetoothEnabled);
        }

        public static void SetBluetoothEnabled(bool enable)
        {
            if (_isToggling) return;
            _isToggling = true;
            IsBluetoothEnabled = enable;
            if (StateChanged != null) StateChanged();

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    string scriptPath = GetScriptPath();
                    if (!File.Exists(scriptPath)) EnsureScriptExists(scriptPath);

                    string act = enable ? "On" : "Off";
                    ProcessStartInfo psi = new ProcessStartInfo("powershell.exe",
                        "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\" -Kind Bluetooth -Action " + act)
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        StandardOutputEncoding = Encoding.UTF8
                    };

                    using (Process p = Process.Start(psi))
                    {
                        p.WaitForExit(4000);
                    }
                }
                catch { }
                finally
                {
                    _isToggling = false;
                    RefreshStateAsync();
                }
            });
        }

        private static string GetScriptPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "scripts", "SetRadio.ps1");
        }

        private static void EnsureScriptExists(string path)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string code = @"param(
    [string]$Kind = ""Bluetooth"",
    [string]$Action = ""Get""
)
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | ? { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
function Await($winRtTask, $resultType) {
    $asTask = $asTaskGeneric.MakeGenericMethod($resultType)
    $netTask = $asTask.Invoke($null, @($winRtTask))
    $netTask.Wait(-1) | Out-Null
    $netTask.Result
}
[Windows.Devices.Radios.Radio, Windows.System.Devices, ContentType = WindowsRuntime] | Out-Null
$op = [Windows.Devices.Radios.Radio]::GetRadiosAsync()
$radios = Await $op ([System.Collections.Generic.IReadOnlyList[Windows.Devices.Radios.Radio]])
$r = $radios | Where-Object { $_.Kind -eq $Kind }
if (-not $r) {
    Write-Host ""RADIO_NOT_FOUND""
    exit
}

if ($Action -eq ""Get"") {
    Write-Host ""RADIO_STATE|$Kind|$($r.State)""
    exit
}

$targetState = $null
if ($Action -eq ""On"") {
    $targetState = [Windows.Devices.Radios.RadioState]::On
} elseif ($Action -eq ""Off"") {
    $targetState = [Windows.Devices.Radios.RadioState]::Off
} elseif ($Action -eq ""Toggle"") {
    if ($r.State -eq [Windows.Devices.Radios.RadioState]::On) {
        $targetState = [Windows.Devices.Radios.RadioState]::Off
    } else {
        $targetState = [Windows.Devices.Radios.RadioState]::On
    }
}

if ($targetState -ne $null) {
    $status = Await ($r.SetStateAsync($targetState)) ([Windows.Devices.Radios.RadioAccessStatus])
    Write-Host ""RADIO_STATE|$Kind|$targetState|$status""
}
";
                File.WriteAllText(path, code, Encoding.UTF8);
            }
            catch { }
        }
    }
}
