using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MacOSTahoe
{
    public enum NetworkKind
    {
        Disconnected,
        Ethernet,
        WiFi,
        Other
    }

    public class NetworkStatusInfo
    {
        public NetworkKind Kind = NetworkKind.Disconnected;
        public string InterfaceName = "Сеть";
        public string ConnectionTitle = "Нет подключения";
        public string IpAddress = "";
        public string Speed = "";
        public bool IsConnected = false;
        public string Ssid = "";
        public string SignalStrength = "";
        public bool IsWifiEnabled = true;
    }

    public static class NetworkHelper
    {
        public static bool IsWifiEnabled { get; private set; }
        public static event Action WifiStateChanged;

        private static bool _initialized = false;
        private static bool _isToggling = false;

        static NetworkHelper()
        {
            IsWifiEnabled = true;
        }

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            RefreshWifiStateAsync();
        }

        public static void RefreshWifiStateAsync()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    string scriptPath = GetScriptPath();
                    if (!File.Exists(scriptPath)) EnsureScriptExists(scriptPath);

                    ProcessStartInfo psi = new ProcessStartInfo("powershell.exe",
                        "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\" -Kind WiFi -Action Get")
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
                        if (outStr.Contains("RADIO_STATE|WiFi|"))
                        {
                            bool on = outStr.Contains("|On");
                            IsWifiEnabled = on;
                            if (WifiStateChanged != null) WifiStateChanged();
                        }
                    }
                }
                catch { }
            });
        }

        public static void ToggleWifi()
        {
            SetWifiEnabled(!IsWifiEnabled);
        }

        public static void SetWifiEnabled(bool enable)
        {
            if (_isToggling) return;
            _isToggling = true;
            IsWifiEnabled = enable;
            if (WifiStateChanged != null) WifiStateChanged();

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    string scriptPath = GetScriptPath();
                    if (!File.Exists(scriptPath)) EnsureScriptExists(scriptPath);

                    string act = enable ? "On" : "Off";
                    ProcessStartInfo psi = new ProcessStartInfo("powershell.exe",
                        "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\" -Kind WiFi -Action " + act)
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
                    RefreshWifiStateAsync();
                }
            });
        }

        public static NetworkStatusInfo GetCurrentStatus()
        {
            NetworkStatusInfo info = new NetworkStatusInfo();
            info.IsWifiEnabled = IsWifiEnabled;
            try
            {
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                NetworkInterface active = null;

                // 1. Prefer operational Ethernet interface
                for (int i = 0; i < interfaces.Length; i++)
                {
                    NetworkInterface ni = interfaces[i];
                    if (ni.OperationalStatus == OperationalStatus.Up &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    {
                        if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                            ni.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet ||
                            ni.Description.ToLower().Contains("ethernet") ||
                            ni.Description.ToLower().Contains("pcie") ||
                            ni.Description.ToLower().Contains("realtek") ||
                            ni.Description.ToLower().Contains("intel(r) ethernet"))
                        {
                            active = ni;
                            info.Kind = NetworkKind.Ethernet;
                            break;
                        }
                    }
                }

                // 2. If no active Ethernet, check for operational Wi-Fi
                if (active == null)
                {
                    for (int i = 0; i < interfaces.Length; i++)
                    {
                        NetworkInterface ni = interfaces[i];
                        if (ni.OperationalStatus == OperationalStatus.Up &&
                            ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                            ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                        {
                            if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                                ni.Description.ToLower().Contains("wi-fi") ||
                                ni.Description.ToLower().Contains("wireless"))
                            {
                                active = ni;
                                info.Kind = NetworkKind.WiFi;
                                break;
                            }
                        }
                    }
                }

                // 3. Fallback to any operational non-loopback interface
                if (active == null)
                {
                    for (int i = 0; i < interfaces.Length; i++)
                    {
                        NetworkInterface ni = interfaces[i];
                        if (ni.OperationalStatus == OperationalStatus.Up &&
                            ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                            ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                        {
                            active = ni;
                            info.Kind = NetworkKind.Other;
                            break;
                        }
                    }
                }

                if (active != null)
                {
                    info.IsConnected = true;
                    info.InterfaceName = active.Name;
                    if (info.Kind == NetworkKind.Ethernet)
                    {
                        info.ConnectionTitle = "Проводная сеть (Ethernet)";
                    }
                    else if (info.Kind == NetworkKind.WiFi)
                    {
                        string ssid;
                        string signal;
                        GetWifiDetails(out ssid, out signal);
                        info.Ssid = ssid;
                        info.SignalStrength = signal;
                        info.ConnectionTitle = !string.IsNullOrEmpty(info.Ssid) ? ("Wi-Fi: " + info.Ssid) : "Беспроводная сеть (Wi-Fi)";
                    }
                    else
                    {
                        info.ConnectionTitle = active.Name;
                    }

                    // Speed
                    long spd = active.Speed;
                    if (spd >= 1000000000) info.Speed = (spd / 1000000000) + " Гбит/с";
                    else if (spd >= 1000000) info.Speed = (spd / 1000000) + " Мбит/с";
                    else if (spd > 0) info.Speed = (spd / 1000) + " Кбит/с";

                    // IPv4
                    try
                    {
                        IPInterfaceProperties ipProps = active.GetIPProperties();
                        foreach (UnicastIPAddressInformation uip in ipProps.UnicastAddresses)
                        {
                            if (uip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                info.IpAddress = uip.Address.ToString();
                                break;
                            }
                        }
                    }
                    catch { }
                }
                else
                {
                    info.ConnectionTitle = IsWifiEnabled ? "Сеть отключена" : "Wi-Fi выключен";
                }
            }
            catch { }
            return info;
        }

        public static void GetWifiDetails(out string ssid, out string signal)
        {
            ssid = "";
            signal = "";
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("netsh", "wlan show interfaces")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.GetEncoding(866)
                };
                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(1000);
                    string[] lines = output.Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        if (line.StartsWith("SSID") && !line.StartsWith("SSID BSSID"))
                        {
                            int colon = line.IndexOf(':');
                            if (colon >= 0 && colon + 1 < line.Length)
                            {
                                string s = line.Substring(colon + 1).Trim();
                                if (!string.IsNullOrEmpty(s) && string.IsNullOrEmpty(ssid)) ssid = s;
                            }
                        }
                        else if (line.StartsWith("Сигнал") || line.StartsWith("Signal"))
                        {
                            int colon = line.IndexOf(':');
                            if (colon >= 0 && colon + 1 < line.Length)
                            {
                                signal = line.Substring(colon + 1).Trim();
                            }
                        }
                    }
                }
            }
            catch { }
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
    [string]$Kind = ""WiFi"",
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
