using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

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
    }

    public static class NetworkHelper
    {
        public static NetworkStatusInfo GetCurrentStatus()
        {
            NetworkStatusInfo info = new NetworkStatusInfo();
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
                        info.ConnectionTitle = "Проводное подключение (Ethernet)";
                    }
                    else if (info.Kind == NetworkKind.WiFi)
                    {
                        info.Ssid = GetCurrentWifiSsid();
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
            }
            catch { }
            return info;
        }

        public static string GetCurrentWifiSsid()
        {
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
                                string ssid = line.Substring(colon + 1).Trim();
                                if (!string.IsNullOrEmpty(ssid)) return ssid;
                            }
                        }
                    }
                }
            }
            catch { }
            return "";
        }
    }
}
