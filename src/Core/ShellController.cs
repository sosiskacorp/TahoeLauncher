using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using IOPath = System.IO.Path;
using WpfPath = System.Windows.Shapes.Path;

namespace MacOSTahoe
{
    public class ShellController
    {
        public static ShellController Instance;
        public string BaseDir;
        public string IconsDir;
        public string WallpapersDir;
        public string ConfigPath;

        public LauncherConfig Config;

        public TopBarWindow TopBar;
        public DockWindow Dock;
        public DesktopWidgetsWindow DesktopWidgets;
        public SpotlightWindow Spotlight;
        public LaunchpadWindow Launchpad;
        public WindowSwitcherWindow WindowSwitcher;
        public LauncherSettingsWindow SettingsHub;
        public ControlCenterWindow ControlCenter;
        public CalendarWidgetWindow CalendarPopup;
        public NetworkMenuWindow NetworkMenu;
        public SoundMenuWindow SoundMenu;
        public AboutSystemWindow AboutWindow;
        public Window ScreenDimOverlay;
        public StageManagerWindow StageManager;
        public AppSwitcherWindow AppSwitcher;
        public SplitViewDividerWindow SplitViewDivider;
        public SplitViewPickerWindow SplitViewPicker;
        public BackgroundTasksWindow BackgroundTasks;
        public List<StageGroup> CurrentStageGroups = new List<StageGroup>();
        public StageGroup ActiveStageGroup = null;
        public List<List<IntPtr>> MergedStageWindows = new List<List<IntPtr>>();
        public bool IsStageDesktopRevealed = false;
        public readonly Dictionary<IntPtr, int> WindowSpaceMap = new Dictionary<IntPtr, int>();
        private readonly HashSet<IntPtr> _hiddenBySpaceManager = new HashSet<IntPtr>();

        public ImageSource DefaultAppIcon
        {
            get { return GetIcon("finder.png"); }
        }

        public IntPtr LastExternalForegroundHwnd = IntPtr.Zero;
        public string ActiveAppName = "Рабочий стол";
        public double SimulatedBrightness = 100.0;

        private Dictionary<string, BitmapImage> _iconCache = new Dictionary<string, BitmapImage>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, ImageSource> _exeIconCache = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);

        public List<DockAppItem> DockItems = new List<DockAppItem>();
        public List<AppCatalogEntry> InstalledApps = new List<AppCatalogEntry>();
        private string _lastCatalogFingerprint = "";
        private List<FileSystemWatcher> _catalogWatchers = new List<FileSystemWatcher>();
        private DispatcherTimer _catalogDebounceTimer;
        private DispatcherTimer _catalogVerifyTimer;

        public bool IsDarkMode
        {
            get { return Config != null ? Config.IsDarkMode : true; }
            set { if (Config != null) Config.IsDarkMode = value; }
        }

        public bool DockAutoHide
        {
            get { return Config != null ? Config.DockAutoHide : true; }
            set { if (Config != null) Config.DockAutoHide = value; }
        }

        public ShellController()
        {
            Instance = this;
            ResolveDirectories();
            LoadConfig();
            RefreshInstalledApps();
            if (Config.PinnedDockItems == null || Config.PinnedDockItems.Count == 0)
            {
                PopulateDefaultDockConfig();
                SaveConfig();
            }
            RebuildDockItemsFromConfig();
            SetupCatalogWatchers();
        }

        private void ResolveDirectories()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(exeDir))
                exeDir = exeDir.TrimEnd('\\', '/');

            if (!string.IsNullOrEmpty(exeDir) && Directory.Exists(IOPath.Combine(exeDir, "icons_png")))
            {
                BaseDir = exeDir;
            }
            else if (!string.IsNullOrEmpty(exeDir) && Directory.Exists(IOPath.Combine(exeDir, "..\\icons_png")))
            {
                BaseDir = IOPath.GetFullPath(IOPath.Combine(exeDir, ".."));
            }
            else if (!string.IsNullOrEmpty(exeDir) && (Directory.Exists(IOPath.Combine(exeDir, "icons_png")) || Directory.Exists(IOPath.Combine(exeDir, "icons"))))
            {
                BaseDir = exeDir;
            }
            else
            {
                BaseDir = exeDir;
            }

            IconsDir = Directory.Exists(IOPath.Combine(BaseDir, "icons_png")) ? IOPath.Combine(BaseDir, "icons_png") : IOPath.Combine(BaseDir, "icons");
            WallpapersDir = IOPath.Combine(BaseDir, "wallpapers");
            ConfigPath = IOPath.Combine(BaseDir, "config.json");
        }

        public void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath, Encoding.UTF8);
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    LauncherConfig loaded = ser.Deserialize<LauncherConfig>(json);
                    if (loaded != null)
                    {
                        if (loaded.FavoriteApps == null) loaded.FavoriteApps = new List<string>();
                        if (loaded.HiddenApps == null) loaded.HiddenApps = new List<string>();
                        if (loaded.CustomApps == null) loaded.CustomApps = new List<CustomAppConfig>();
                        if (loaded.PinnedDockItems == null) loaded.PinnedDockItems = new List<DockItemConfig>();
                        if (loaded.DockIconSize < 36.0 || loaded.DockIconSize > 72.0) loaded.DockIconSize = 50.0;
                        if (loaded.DockZoomRange < 0.0 || loaded.DockZoomRange > 40.0) loaded.DockZoomRange = 26.0;
                        Config = loaded;
                        return;
                    }
                }
            }
            catch { }

            Config = new LauncherConfig();
        }

        public void SaveConfig()
        {
            try
            {
                JavaScriptSerializer ser = new JavaScriptSerializer();
                string json = FormatJson(ser.Serialize(Config));
                File.WriteAllText(ConfigPath, json, Encoding.UTF8);
            }
            catch { }
        }

        private static string FormatJson(string input)
        {
            if (string.IsNullOrEmpty(input)) return "{}";
            StringBuilder sb = new StringBuilder();
            int indent = 0;
            bool inQuotes = false;
            for (int i = 0; i < input.Length; i++)
            {
                char ch = input[i];
                if (ch == '"' && (i == 0 || input[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                    sb.Append(ch);
                }
                else if (!inQuotes)
                {
                    if (ch == '{' || ch == '[')
                    {
                        sb.Append(ch);
                        sb.Append("\r\n");
                        indent++;
                        sb.Append(new string(' ', indent * 2));
                    }
                    else if (ch == '}' || ch == ']')
                    {
                        sb.Append("\r\n");
                        indent = Math.Max(0, indent - 1);
                        sb.Append(new string(' ', indent * 2));
                        sb.Append(ch);
                    }
                    else if (ch == ',')
                    {
                        sb.Append(ch);
                        sb.Append("\r\n");
                        sb.Append(new string(' ', indent * 2));
                    }
                    else if (ch == ':')
                    {
                        sb.Append(": ");
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        private void PopulateDefaultDockConfig()
        {
            Config.PinnedDockItems.Clear();

            Config.PinnedDockItems.Add(new DockItemConfig
            {
                Id = "explorer",
                Name = "Проводник",
                IconFile = "finder.png",
                LaunchCommand = "explorer.exe",
                LaunchArgs = "",
                ProcessNames = new string[] { "explorer" },
                IsExplorer = true
            });

            Config.PinnedDockItems.Add(new DockItemConfig
            {
                Id = "apps",
                Name = "Приложения",
                IconFile = "launchpad.png",
                LaunchCommand = "@launchpad",
                LaunchArgs = "",
                ProcessNames = new string[0],
                IsAppsLauncher = true
            });

            Config.PinnedDockItems.Add(new DockItemConfig
            {
                Id = "windows_overview",
                Name = "Обзор окон",
                IconFile = "mission-control.png",
                LaunchCommand = "@windows",
                LaunchArgs = "",
                ProcessNames = new string[0],
                IsWindowSwitcher = true
            });

            string chromePath = ResolveCandidatePath(
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\Application\chrome.exe"),
                "chrome.exe");
            if (!string.IsNullOrEmpty(chromePath))
            {
                Config.PinnedDockItems.Add(new DockItemConfig
                {
                    Id = "chrome",
                    Name = "Google Chrome",
                    IconFile = "chrome.png",
                    LaunchCommand = chromePath,
                    LaunchArgs = "--profile-directory=\"Profile 1\"",
                    ProcessNames = new string[] { "chrome" }
                });
            }

            string agyPath = ResolveCandidatePath(
                IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\antigravity\Antigravity.exe"),
                "Antigravity.exe");
            if (!string.IsNullOrEmpty(agyPath) && File.Exists(agyPath))
            {
                Config.PinnedDockItems.Add(new DockItemConfig
                {
                    Id = "antigravity",
                    Name = "Antigravity",
                    IconFile = "antigravity.png",
                    LaunchCommand = agyPath,
                    LaunchArgs = "",
                    ProcessNames = new string[] { "Antigravity" }
                });
            }

            string openCodePath = ResolveCandidatePath(
                IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\ai.opencode.desktop\OpenCode.exe"),
                @"C:\Program Files\OpenCode\OpenCode.exe",
                "OpenCode.exe");
            if (!string.IsNullOrEmpty(openCodePath) && File.Exists(openCodePath))
            {
                Config.PinnedDockItems.Add(new DockItemConfig
                {
                    Id = "opencode",
                    Name = "OpenCode",
                    IconFile = "opencode.png",
                    LaunchCommand = openCodePath,
                    LaunchArgs = "",
                    ProcessNames = new string[] { "OpenCode", "opencode-cli" }
                });
            }

            string imePath = ResolveCandidatePath(
                IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"iMe\iMe.exe"),
                IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Telegram Desktop\Telegram.exe"),
                "iMe.exe");
            if (!string.IsNullOrEmpty(imePath) && File.Exists(imePath))
            {
                Config.PinnedDockItems.Add(new DockItemConfig
                {
                    Id = "ime",
                    Name = IOPath.GetFileNameWithoutExtension(imePath),
                    IconFile = "telegram.png",
                    LaunchCommand = imePath,
                    LaunchArgs = "",
                    ProcessNames = new string[] { "iMe", "Telegram" }
                });
            }

            string musicPath = ResolveCandidatePath(
                IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\YandexMusic\Яндекс Музыка.exe"),
                "Яндекс Музыка.exe");
            if (!string.IsNullOrEmpty(musicPath) && File.Exists(musicPath))
            {
                Config.PinnedDockItems.Add(new DockItemConfig
                {
                    Id = "music",
                    Name = "Яндекс Музыка",
                    IconFile = "music.png",
                    LaunchCommand = musicPath,
                    LaunchArgs = "",
                    ProcessNames = new string[] { "Яндекс Музыка" }
                });
            }

            string steamPath = ResolveCandidatePath(
                @"C:\Program Files (x86)\Steam\steam.exe",
                @"C:\Program Files\Steam\steam.exe",
                "steam.exe");
            if (!string.IsNullOrEmpty(steamPath) && File.Exists(steamPath))
            {
                Config.PinnedDockItems.Add(new DockItemConfig
                {
                    Id = "steam",
                    Name = "Steam",
                    IconFile = "steam.png",
                    LaunchCommand = steamPath,
                    LaunchArgs = "",
                    ProcessNames = new string[] { "steamwebhelper", "steam" }
                });
            }

            Config.PinnedDockItems.Add(new DockItemConfig
            {
                Id = "terminal",
                Name = "Терминал",
                IconFile = "terminal.png",
                LaunchCommand = "wt.exe",
                LaunchArgs = "",
                ProcessNames = new string[] { "WindowsTerminal", "powershell", "cmd" }
            });

            Config.PinnedDockItems.Add(new DockItemConfig
            {
                Id = "settings",
                Name = "Параметры",
                IconFile = "settings.png",
                LaunchCommand = "explorer.exe",
                LaunchArgs = "ms-settings:",
                ProcessNames = new string[] { "SystemSettings" }
            });

            Config.PinnedDockItems.Add(new DockItemConfig
            {
                Id = "sep1",
                IsSeparator = true
            });

            Config.PinnedDockItems.Add(new DockItemConfig
            {
                Id = "desktop",
                Name = "Папка Рабочий стол",
                IconFile = "desktop.png",
                LaunchCommand = "explorer.exe",
                LaunchArgs = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                ProcessNames = new string[0],
                IsDesktopFolder = true
            });

            Config.PinnedDockItems.Add(new DockItemConfig
            {
                Id = "trash",
                Name = "Корзина",
                IconFile = "trash.png",
                LaunchCommand = "explorer.exe",
                LaunchArgs = "shell:RecycleBinFolder",
                ProcessNames = new string[0],
                IsTrash = true
            });

            if (Config.FavoriteApps.Count == 0)
            {
                Config.FavoriteApps.Add("Проводник");
                Config.FavoriteApps.Add("Google Chrome");
                Config.FavoriteApps.Add("Antigravity");
                Config.FavoriteApps.Add("OpenCode");
                Config.FavoriteApps.Add("Терминал");
                Config.FavoriteApps.Add("Параметры");
            }
        }

        private string ResolveCandidatePath(params string[] candidates)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                string c = candidates[i];
                if (string.IsNullOrEmpty(c)) continue;
                if (c.IndexOf('\\') >= 0 && File.Exists(c)) return c;
                for (int j = 0; j < InstalledApps.Count; j++)
                {
                    if (string.Equals(IOPath.GetFileName(InstalledApps[j].Cmd), c, StringComparison.OrdinalIgnoreCase))
                        return InstalledApps[j].Cmd;
                }
            }
            return candidates.Length > 0 ? candidates[0] : "";
        }

        public void RebuildDockItemsFromConfig()
        {
            DockItems.Clear();
            for (int i = 0; i < Config.PinnedDockItems.Count; i++)
            {
                DockItemConfig cfg = Config.PinnedDockItems[i];
                ImageSource customSrc = null;
                if (!cfg.IsSeparator && !string.IsNullOrEmpty(cfg.LaunchCommand) && cfg.LaunchCommand.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(cfg.IconFile) || cfg.IconFile == "finder.png")
                    {
                        string known = MapProcessToKnownIcon(IOPath.GetFileNameWithoutExtension(cfg.LaunchCommand));
                        if (known != null) cfg.IconFile = known;
                        else if (File.Exists(cfg.LaunchCommand)) customSrc = ExtractHighResIcon(cfg.LaunchCommand, 0);
                    }
                }
                DockItems.Add(new DockAppItem
                {
                    Id = cfg.Id ?? ("pin_" + i.ToString()),
                    Name = cfg.Name ?? "Приложение",
                    IconFile = cfg.IconFile ?? "finder.png",
                    CustomImageSource = customSrc,
                    LaunchCommand = cfg.LaunchCommand ?? "",
                    LaunchArgs = cfg.LaunchArgs ?? "",
                    ProcessNames = cfg.ProcessNames ?? new string[0],
                    IsPinned = true,
                    IsSeparator = cfg.IsSeparator,
                    IsTrash = cfg.IsTrash,
                    IsAppsLauncher = cfg.IsAppsLauncher,
                    IsDesktopFolder = cfg.IsDesktopFolder,
                    IsExplorer = cfg.IsExplorer,
                    IsWindowSwitcher = cfg.IsWindowSwitcher,
                    CurrentSize = Config.DockIconSize,
                    TargetSize = Config.DockIconSize
                });
            }
            if (Dock != null)
            {
                Dock.RebuildDockVisuals();
            }
        }

        public bool IsAppPinnedInDock(string cmdOrTitle)
        {
            if (string.IsNullOrEmpty(cmdOrTitle)) return false;
            string exeName = IOPath.GetFileNameWithoutExtension(cmdOrTitle);
            for (int i = 0; i < Config.PinnedDockItems.Count; i++)
            {
                DockItemConfig c = Config.PinnedDockItems[i];
                if (c.IsSeparator) continue;
                if (string.Equals(c.Name, cmdOrTitle, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.LaunchCommand, cmdOrTitle, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(exeName) && string.Equals(IOPath.GetFileNameWithoutExtension(c.LaunchCommand ?? ""), exeName, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
            return false;
        }

        public void PinAppToDock(string title, string cmd, string args)
        {
            if (IsAppPinnedInDock(cmd) || IsAppPinnedInDock(title)) return;
            string exeBase = IOPath.GetFileNameWithoutExtension(cmd ?? title);
            string knownIcon = MapProcessToKnownIcon(exeBase) ?? "finder.png";

            int insertIdx = Config.PinnedDockItems.Count;
            for (int i = 0; i < Config.PinnedDockItems.Count; i++)
            {
                if (Config.PinnedDockItems[i].IsSeparator)
                {
                    insertIdx = i;
                    break;
                }
            }

            Config.PinnedDockItems.Insert(insertIdx, new DockItemConfig
            {
                Id = "pin_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                Name = title,
                IconFile = knownIcon,
                LaunchCommand = cmd,
                LaunchArgs = args ?? "",
                ProcessNames = !string.IsNullOrEmpty(exeBase) ? new string[] { exeBase } : new string[0]
            });

            SaveConfig();
            RebuildDockItemsFromConfig();
        }

        public void UnpinAppFromDock(string cmdOrIdOrTitle)
        {
            for (int i = Config.PinnedDockItems.Count - 1; i >= 0; i--)
            {
                DockItemConfig c = Config.PinnedDockItems[i];
                if (c.IsSeparator || c.IsExplorer || c.IsAppsLauncher || c.IsTrash) continue;
                if (string.Equals(c.Id, cmdOrIdOrTitle, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.Name, cmdOrIdOrTitle, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.LaunchCommand, cmdOrIdOrTitle, StringComparison.OrdinalIgnoreCase))
                {
                    Config.PinnedDockItems.RemoveAt(i);
                    break;
                }
            }
            SaveConfig();
            RebuildDockItemsFromConfig();
        }

        public void MovePinnedDockItem(string id, int delta)
        {
            int idx = -1;
            int sepIdx = Config.PinnedDockItems.Count;
            for (int i = 0; i < Config.PinnedDockItems.Count; i++)
            {
                if (Config.PinnedDockItems[i].IsSeparator && sepIdx == Config.PinnedDockItems.Count)
                    sepIdx = i;
                if (string.Equals(Config.PinnedDockItems[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    idx = i;
            }
            if (idx < 0) return;
            int targetIdx = idx + delta;
            if (targetIdx < 1 || targetIdx >= sepIdx) return;

            DockItemConfig item = Config.PinnedDockItems[idx];
            Config.PinnedDockItems.RemoveAt(idx);
            Config.PinnedDockItems.Insert(targetIdx, item);
            SaveConfig();
            RebuildDockItemsFromConfig();
        }

        public bool IsFavoriteApp(string title)
        {
            for (int i = 0; i < Config.FavoriteApps.Count; i++)
            {
                if (string.Equals(Config.FavoriteApps[i], title, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public void ToggleFavoriteApp(string title)
        {
            for (int i = Config.FavoriteApps.Count - 1; i >= 0; i--)
            {
                if (string.Equals(Config.FavoriteApps[i], title, StringComparison.OrdinalIgnoreCase))
                {
                    Config.FavoriteApps.RemoveAt(i);
                    SaveConfig();
                    if (DesktopWidgets != null) DesktopWidgets.RefreshFavorites();
                    return;
                }
            }
            Config.FavoriteApps.Add(title);
            SaveConfig();
            if (DesktopWidgets != null) DesktopWidgets.RefreshFavorites();
        }

        public void HideAppFromCatalog(string title)
        {
            if (string.IsNullOrEmpty(title)) return;
            for (int i = 0; i < Config.HiddenApps.Count; i++)
            {
                if (string.Equals(Config.HiddenApps[i], title, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            Config.HiddenApps.Add(title);
            SaveConfig();
            RefreshInstalledApps();
            if (Launchpad != null && Launchpad.IsVisible) Launchpad.OnCatalogUpdated();
        }

        public bool IsAutostartEnabled()
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
                if (rk != null)
                {
                    object v = rk.GetValue("macOS_Tahoe_Apply");
                    object v2 = rk.GetValue("TahoeLauncher");
                    rk.Close();
                    return (v != null || v2 != null);
                }
            }
            catch { }
            return false;
        }

        public void SetAutostartEnabled(bool enable)
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (rk != null)
                {
                    if (enable)
                    {
                        string applyScript = IOPath.Combine(BaseDir, "apply.ps1");
                        if (File.Exists(applyScript))
                        {
                            rk.SetValue("macOS_Tahoe_Apply", string.Format("powershell.exe -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{0}\"", applyScript));
                        }
                        else
                        {
                            string exePath = Assembly.GetExecutingAssembly().Location;
                            rk.SetValue("TahoeLauncher", string.Format("\"{0}\"", exePath));
                        }
                    }
                    else
                    {
                        rk.DeleteValue("macOS_Tahoe_Apply", false);
                        rk.DeleteValue("TahoeLauncher", false);
                    }
                    rk.Close();
                }
            }
            catch { }
        }

        public ImageSource ExtractHighResIcon(string filePath, int iconIndex)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;
            string cacheKey = filePath + ":" + iconIndex.ToString();
            ImageSource cached;
            if (_exeIconCache.TryGetValue(cacheKey, out cached)) return cached;

            try
            {
                IntPtr hLarge = IntPtr.Zero;
                IntPtr hSmall = IntPtr.Zero;
                int hr = NativeMethods.SHDefExtractIconW(filePath, iconIndex, 0, out hLarge, out hSmall, 256);
                if (hSmall != IntPtr.Zero) NativeMethods.DestroyIcon(hSmall);
                if (hr == 0 && hLarge != IntPtr.Zero)
                {
                    ImageSource src = Imaging.CreateBitmapSourceFromHIcon(
                        hLarge,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    src.Freeze();
                    NativeMethods.DestroyIcon(hLarge);
                    _exeIconCache[cacheKey] = src;
                    return src;
                }
                if (hLarge != IntPtr.Zero) NativeMethods.DestroyIcon(hLarge);
            }
            catch { }

            try
            {
                System.Drawing.Icon ico = System.Drawing.Icon.ExtractAssociatedIcon(filePath);
                if (ico != null)
                {
                    ImageSource src = Imaging.CreateBitmapSourceFromHIcon(
                        ico.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    src.Freeze();
                    ico.Dispose();
                    _exeIconCache[cacheKey] = src;
                    return src;
                }
            }
            catch { }

            return null;
        }

        public ImageSource ResolveDynamicIcon(string procName, uint pid)
        {
            string mapped = MapProcessToKnownIcon(procName);
            if (mapped != null)
            {
                return GetIcon(mapped);
            }

            ImageSource cached;
            if (_exeIconCache.TryGetValue(procName, out cached)) return cached;

            try
            {
                Process p = Process.GetProcessById((int)pid);
                string exePath = p.MainModule.FileName;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    ImageSource src = ExtractHighResIcon(exePath, 0);
                    if (src != null)
                    {
                        _exeIconCache[procName] = src;
                        return src;
                    }
                }
            }
            catch { }

            return GetIcon("finder.png");
        }

        public static string MapProcessToKnownIcon(string pn)
        {
            if (string.IsNullOrEmpty(pn)) return null;
            if (string.Equals(pn, "chrome", StringComparison.OrdinalIgnoreCase)) return "chrome.png";
            if (string.Equals(pn, "Antigravity", StringComparison.OrdinalIgnoreCase)) return "antigravity.png";
            if (string.Equals(pn, "OpenCode", StringComparison.OrdinalIgnoreCase)) return "opencode.png";
            if (string.Equals(pn, "iMe", StringComparison.OrdinalIgnoreCase) || string.Equals(pn, "Telegram", StringComparison.OrdinalIgnoreCase)) return "telegram.png";
            if (pn.IndexOf("Яндекс Музыка", StringComparison.OrdinalIgnoreCase) >= 0) return "music.png";
            if (string.Equals(pn, "steam", StringComparison.OrdinalIgnoreCase)) return "steam.png";
            if (string.Equals(pn, "NVIDIA App", StringComparison.OrdinalIgnoreCase)) return "nvidia.png";
            if (string.Equals(pn, "Code", StringComparison.OrdinalIgnoreCase)) return "vscode.png";
            if (string.Equals(pn, "Discord", StringComparison.OrdinalIgnoreCase)) return "discord.png";
            if (string.Equals(pn, "vlc", StringComparison.OrdinalIgnoreCase)) return "vlc.png";
            if (string.Equals(pn, "msedge", StringComparison.OrdinalIgnoreCase)) return "edge.png";
            if (string.Equals(pn, "AnyDesk", StringComparison.OrdinalIgnoreCase)) return "anydesk.png";
            if (string.Equals(pn, "Taskmgr", StringComparison.OrdinalIgnoreCase)) return "activity-monitor.png";
            if (pn.IndexOf("calc", StringComparison.OrdinalIgnoreCase) >= 0) return "calculator.png";
            if (string.Equals(pn, "notepad", StringComparison.OrdinalIgnoreCase)) return "notes.png";
            if (string.Equals(pn, "Docker Desktop", StringComparison.OrdinalIgnoreCase)) return "docker.png";
            if (string.Equals(pn, "dbeaver", StringComparison.OrdinalIgnoreCase)) return "dbeaver.png";
            if (string.Equals(pn, "WINWORD", StringComparison.OrdinalIgnoreCase)) return "word.png";
            if (string.Equals(pn, "EXCEL", StringComparison.OrdinalIgnoreCase)) return "excel.png";
            if (string.Equals(pn, "POWERPNT", StringComparison.OrdinalIgnoreCase)) return "powerpoint.png";
            return null;
        }

        public static string ClassifyAppCategory(string title, string cmd, bool isGameShortcut)
        {
            if (isGameShortcut) return "Игры";
            string s = ((title ?? "") + " " + (cmd ?? "")).ToLowerInvariant();
            if (s.IndexOf("steam") >= 0 || s.IndexOf("epic games") >= 0 || s.IndexOf("minecraft") >= 0 ||
                s.IndexOf("curseforge") >= 0 || s.IndexOf("plutonium") >= 0 || s.IndexOf("vortex") >= 0 ||
                s.IndexOf("rockstar") >= 0 || s.IndexOf("riot") >= 0 || s.IndexOf("игры") >= 0 ||
                s.IndexOf("games") >= 0 || s.IndexOf("sidequest") >= 0 || s.IndexOf("rpcs3") >= 0)
            {
                return "Игры";
            }
            if (s.IndexOf("code") >= 0 || s.IndexOf("antigravity") >= 0 || s.IndexOf("opencode") >= 0 ||
                s.IndexOf("docker") >= 0 || s.IndexOf("dbeaver") >= 0 || s.IndexOf("git") >= 0 ||
                s.IndexOf("putty") >= 0 || s.IndexOf("winscp") >= 0 || s.IndexOf("filezilla") >= 0 ||
                s.IndexOf("npcap") >= 0 || s.IndexOf("wireshark") >= 0 || s.IndexOf("fiddler") >= 0 ||
                s.IndexOf("postman") >= 0 || s.IndexOf("терминал") >= 0 || s.IndexOf("wt.exe") >= 0 ||
                s.IndexOf("studio") >= 0 || s.IndexOf("vmware") >= 0 || s.IndexOf("virtualbox") >= 0 ||
                s.IndexOf("adb") >= 0 || s.IndexOf("sdk") >= 0)
            {
                return "Разработка";
            }
            if (s.IndexOf("chrome") >= 0 || s.IndexOf("edge") >= 0 || s.IndexOf("firefox") >= 0 ||
                s.IndexOf("browser") >= 0 || s.IndexOf("браузер") >= 0 || s.IndexOf("ime") >= 0 ||
                s.IndexOf("telegram") >= 0 || s.IndexOf("discord") >= 0 || s.IndexOf("vpn") >= 0 ||
                s.IndexOf("anydesk") >= 0 || s.IndexOf("rustdesk") >= 0 || s.IndexOf("teamviewer") >= 0 ||
                s.IndexOf("qbittorrent") >= 0 || s.IndexOf("torrserver") >= 0 || s.IndexOf("zoom") >= 0 ||
                s.IndexOf("skype") >= 0)
            {
                return "Интернет";
            }
            if (s.IndexOf("музыка") >= 0 || s.IndexOf("music") >= 0 || s.IndexOf("spotify") >= 0 ||
                s.IndexOf("vlc") >= 0 || s.IndexOf("obs") >= 0 || s.IndexOf("word") >= 0 ||
                s.IndexOf("excel") >= 0 || s.IndexOf("powerpoint") >= 0 || s.IndexOf("acrobat") >= 0 ||
                s.IndexOf("paint") >= 0 || s.IndexOf("photoshop") >= 0 || s.IndexOf("blender") >= 0 ||
                s.IndexOf("audacity") >= 0 || s.IndexOf("handbrake") >= 0 || s.IndexOf("kdenlive") >= 0)
            {
                return "Медиа и Офис";
            }
            if (s.IndexOf("проводник") >= 0 || s.IndexOf("параметры") >= 0 || s.IndexOf("диспетчер") >= 0 ||
                s.IndexOf("калькулятор") >= 0 || s.IndexOf("блокнот") >= 0 || s.IndexOf("winrar") >= 0 ||
                s.IndexOf("7-zip") >= 0 || s.IndexOf("ccleaner") >= 0 || s.IndexOf("treesize") >= 0 ||
                s.IndexOf("cpu-z") >= 0 || s.IndexOf("gpu-z") >= 0 || s.IndexOf("hwmonitor") >= 0 ||
                s.IndexOf("aida64") >= 0 || s.IndexOf("rufus") >= 0 || s.IndexOf("nvidia") >= 0)
            {
                return "Система";
            }
            return "Программы";
        }

        private static void ResolveShortcutFile(string lnkPath, out string target, out string args, out string iconPath, out int iconIndex)
        {
            target = "";
            args = "";
            iconPath = "";
            iconIndex = 0;
            try
            {
                NativeMethods.ShellLink link = new NativeMethods.ShellLink();
                ((IPersistFile)link).Load(lnkPath, 0);
                NativeMethods.IShellLinkW sl = (NativeMethods.IShellLinkW)link;
                StringBuilder sbPath = new StringBuilder(512);
                sl.GetPath(sbPath, sbPath.Capacity, IntPtr.Zero, 0);
                target = sbPath.ToString();
                StringBuilder sbArgs = new StringBuilder(512);
                sl.GetArguments(sbArgs, sbArgs.Capacity);
                args = sbArgs.ToString();
                StringBuilder sbIcon = new StringBuilder(512);
                sl.GetIconLocation(sbIcon, sbIcon.Capacity, out iconIndex);
                iconPath = sbIcon.ToString();
            }
            catch { }
        }

        private void SetupCatalogWatchers()
        {
            _catalogDebounceTimer = new DispatcherTimer();
            _catalogDebounceTimer.Interval = TimeSpan.FromMilliseconds(600);
            _catalogDebounceTimer.Tick += delegate
            {
                _catalogDebounceTimer.Stop();
                if (RefreshInstalledApps() && Launchpad != null && Launchpad.IsVisible)
                {
                    Launchpad.OnCatalogUpdated();
                }
            };

            string[] watchDirs = new string[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            };

            foreach (string dir in watchDirs)
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                try
                {
                    FileSystemWatcher fsw = new FileSystemWatcher(dir);
                    fsw.IncludeSubdirectories = true;
                    fsw.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite;
                    FileSystemEventHandler onFsChange = delegate(object s, FileSystemEventArgs e)
                    {
                        if (Application.Current != null)
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                            {
                                _catalogDebounceTimer.Stop();
                                _catalogDebounceTimer.Start();
                            }));
                        }
                    };
                    RenamedEventHandler onFsRename = delegate(object s, RenamedEventArgs e)
                    {
                        if (Application.Current != null)
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                            {
                                _catalogDebounceTimer.Stop();
                                _catalogDebounceTimer.Start();
                            }));
                        }
                    };
                    fsw.Created += onFsChange;
                    fsw.Deleted += onFsChange;
                    fsw.Changed += onFsChange;
                    fsw.Renamed += onFsRename;
                    fsw.EnableRaisingEvents = true;
                    _catalogWatchers.Add(fsw);
                }
                catch { }
            }

            _catalogVerifyTimer = new DispatcherTimer();
            _catalogVerifyTimer.Interval = TimeSpan.FromSeconds(120); // 2 min fallback, FileSystemWatcher handles live updates
            _catalogVerifyTimer.Tick += delegate
            {
                if (RefreshInstalledApps() && Launchpad != null && Launchpad.IsVisible)
                {
                    Launchpad.OnCatalogUpdated();
                }
            };
            _catalogVerifyTimer.Start();
        }

        public bool RefreshInstalledApps()
        {
            try
            {
                List<AppCatalogEntry> list = new List<AppCatalogEntry>();
                HashSet<string> seenTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> hiddenSet = new HashSet<string>(Config != null && Config.HiddenApps != null ? Config.HiddenApps : new List<string>(), StringComparer.OrdinalIgnoreCase);

                // 1. Core System Apps
                AddCoreApp(list, seenNames, hiddenSet, "Проводник", "explorer.exe", "", "finder.png", "Система");
                AddCoreApp(list, seenNames, hiddenSet, "Параметры", "explorer.exe", "ms-settings:", "settings.png", "Система");
                AddCoreApp(list, seenNames, hiddenSet, "Терминал", "wt.exe", "", "terminal.png", "Разработка");
                AddCoreApp(list, seenNames, hiddenSet, "Диспетчер задач", "taskmgr.exe", "", "activity-monitor.png", "Система");
                AddCoreApp(list, seenNames, hiddenSet, "Калькулятор", "calc.exe", "", "calculator.png", "Система");
                AddCoreApp(list, seenNames, hiddenSet, "Блокнот", "notepad.exe", "", "notes.png", "Система");

                // 2. Custom user-added apps from config.json
                if (Config != null && Config.CustomApps != null)
                {
                    for (int i = 0; i < Config.CustomApps.Count; i++)
                    {
                        CustomAppConfig ca = Config.CustomApps[i];
                        if (ca == null || string.IsNullOrEmpty(ca.Title) || string.IsNullOrEmpty(ca.Cmd)) continue;
                        if (hiddenSet.Contains(ca.Title) || seenNames.Contains(ca.Title)) continue;
                        seenNames.Add(ca.Title);
                        ImageSource ico = File.Exists(ca.Cmd) ? ExtractHighResIcon(ca.Cmd, 0) : null;
                        if (ico == null) ico = GetIcon("finder.png");
                        list.Add(new AppCatalogEntry
                        {
                            Title = ca.Title,
                            Cmd = ca.Cmd,
                            Args = ca.Args ?? "",
                            VerifyPath = ca.Cmd,
                            Category = !string.IsNullOrEmpty(ca.Category) ? ca.Category : ClassifyAppCategory(ca.Title, ca.Cmd, false),
                            IconSource = ico
                        });
                    }
                }

                // 3. Discover installed applications from Start Menu & Desktop
                List<string> lnkFiles = new List<string>();
                string commonStart = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);
                string userStart = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

                if (!string.IsNullOrEmpty(userStart) && Directory.Exists(userStart))
                {
                    try { lnkFiles.AddRange(Directory.GetFiles(userStart, "*.lnk", SearchOption.AllDirectories)); } catch { }
                }
                if (!string.IsNullOrEmpty(commonStart) && Directory.Exists(commonStart))
                {
                    try { lnkFiles.AddRange(Directory.GetFiles(commonStart, "*.lnk", SearchOption.AllDirectories)); } catch { }
                }
                if (!string.IsNullOrEmpty(desktop) && Directory.Exists(desktop))
                {
                    try { lnkFiles.AddRange(Directory.GetFiles(desktop, "*.lnk", SearchOption.TopDirectoryOnly)); } catch { }
                    string[] deskSubfolders = new string[] { "Программы", "Игры" };
                    for (int i = 0; i < deskSubfolders.Length; i++)
                    {
                        string subDir = IOPath.Combine(desktop, deskSubfolders[i]);
                        if (Directory.Exists(subDir))
                        {
                            try { lnkFiles.AddRange(Directory.GetFiles(subDir, "*.lnk", SearchOption.TopDirectoryOnly)); } catch { }
                        }
                    }
                }

                string[] blacklistedFolders = new string[]
                {
                    "\\Administrative Tools\\", "\\System Tools\\", "\\Accessibility\\", "\\Accessories\\", "\\Startup\\",
                    "\\Windows Kits\\", "\\Visual Studio\\", "\\Visual Studio 2026\\", "\\Windows PowerShell\\",
                    "\\LibUSB-Win32\\", "\\x264vfw\\", "\\Java\\", "\\Node.js\\", "\\Python ", "\\SP Driver\\",
                    "\\Nefarius Software Solutions\\", "\\OpenVPN\\Shortcuts\\", "\\OpenVPN\\Utilities\\",
                    "\\Средства Microsoft Office\\", "\\Kernel stuff\\", "\\Trackers\\", "\\Seelen\\"
                };

                string[] blacklistedKeywords = new string[]
                {
                    "uninstall", "unins0", "uninst", "деинсталл", "удаление", "удалить", "setup", "install",
                    "msiexec", "rundll32", "help", "manual", "readme", "license", "website", "documentation",
                    "release notes", "справка", "руководство", "что нового", "написать", "reset ", "skinned",
                    "tutorial", "wizard", "configure", "update", "about ", "reload ", "stackbuilder", "pg_ctl",
                    "linguist", "acrodist", "wslsettings", "wsl.exe", "cmd.exe", "powershell", "ceregreset",
                    "kernelmodule", "pageant", "psftp", "puttygen", "appverif", "appcertui", "setlang", "msoev",
                    "ptm6500pushscan", "overwolflauncher", "administrative tools", "control.exe",
                    "(32-bit)", "(64-bit sse4-avx2)", "(x86)", "(wow)", "saves", "samples", "удаленного сканирования"
                };

                List<AppCatalogEntry> discovered = new List<AppCatalogEntry>();

                for (int i = 0; i < lnkFiles.Count; i++)
                {
                    string lnk = lnkFiles[i];
                    bool skipFolder = false;
                    for (int f = 0; f < blacklistedFolders.Length; f++)
                    {
                        if (lnk.IndexOf(blacklistedFolders[f], StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            skipFolder = true;
                            break;
                        }
                    }
                    if (skipFolder) continue;

                    string target, args, iconLoc;
                    int iconIdx;
                    ResolveShortcutFile(lnk, out target, out args, out iconLoc, out iconIdx);

                    if (string.IsNullOrEmpty(target) || !target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!File.Exists(target))
                        continue;

                    string rawName = IOPath.GetFileNameWithoutExtension(lnk);
                    string exeName = IOPath.GetFileName(target);
                    string combined = (rawName + " " + exeName).ToLowerInvariant();

                    bool skipWord = false;
                    for (int w = 0; w < blacklistedKeywords.Length; w++)
                    {
                        string kw = blacklistedKeywords[w];
                        if (combined.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (kw == "overwolflauncher" && string.Equals(rawName, "CurseForge", StringComparison.OrdinalIgnoreCase))
                                continue;
                            skipWord = true;
                            break;
                        }
                    }
                    if (skipWord) continue;

                    string cleanName = Regex.Replace(rawName, @"\s*\((64|32)-?bit\)\s*", "", RegexOptions.IgnoreCase);
                    cleanName = Regex.Replace(cleanName, @"(\.exe)?\s*[-—]\s*ярлык$", "", RegexOptions.IgnoreCase).Trim();
                    if (string.Equals(cleanName, "TorrServer-windows-amd64", StringComparison.OrdinalIgnoreCase))
                        cleanName = "TorrServer";
                    else if (string.Equals(cleanName, "Hалогоплательщик ЮЛ", StringComparison.OrdinalIgnoreCase))
                        cleanName = "Налогоплательщик ЮЛ";
                    else if (cleanName.StartsWith("Приложение сканирования Pantum", StringComparison.OrdinalIgnoreCase))
                        cleanName = "Pantum Сканер";

                    if (hiddenSet.Contains(cleanName) || seenTargets.Contains(target) || seenNames.Contains(cleanName))
                        continue;

                    seenTargets.Add(target);
                    seenNames.Add(cleanName);

                    ImageSource iconSrc = null;
                    string exeBase = IOPath.GetFileNameWithoutExtension(target);
                    string knownPng = MapProcessToKnownIcon(exeBase);
                    if (knownPng != null)
                    {
                        iconSrc = GetIcon(knownPng);
                    }
                    if (iconSrc == null && !string.IsNullOrEmpty(iconLoc) && File.Exists(iconLoc))
                    {
                        iconSrc = ExtractHighResIcon(iconLoc, iconIdx);
                    }
                    if (iconSrc == null)
                    {
                        iconSrc = ExtractHighResIcon(target, 0);
                    }
                    if (iconSrc == null)
                    {
                        iconSrc = GetIcon("finder.png");
                    }

                    bool inGamesFolder = lnk.IndexOf("\\Игры\\", StringComparison.OrdinalIgnoreCase) >= 0;

                    discovered.Add(new AppCatalogEntry
                    {
                        Title = cleanName,
                        Cmd = target,
                        Args = args,
                        VerifyPath = target,
                        Category = ClassifyAppCategory(cleanName, target, inGamesFolder),
                        IconSource = iconSrc
                    });
                }

                // 4. Scan Steam / Epic Games .url shortcuts in Desktop & Desktop\Игры
                List<string> urlFiles = new List<string>();
                if (!string.IsNullOrEmpty(desktop) && Directory.Exists(desktop))
                {
                    try { urlFiles.AddRange(Directory.GetFiles(desktop, "*.url", SearchOption.TopDirectoryOnly)); } catch { }
                    string gamesDir = IOPath.Combine(desktop, "Игры");
                    if (Directory.Exists(gamesDir))
                    {
                        try { urlFiles.AddRange(Directory.GetFiles(gamesDir, "*.url", SearchOption.TopDirectoryOnly)); } catch { }
                    }
                }

                for (int i = 0; i < urlFiles.Count; i++)
                {
                    string urlFile = urlFiles[i];
                    string gameName = IOPath.GetFileNameWithoutExtension(urlFile).Trim();
                    if (hiddenSet.Contains(gameName) || seenNames.Contains(gameName)) continue;

                    string launchUrl = "";
                    string iconFile = "";
                    int iconIdx = 0;
                    try
                    {
                        string[] lines = File.ReadAllLines(urlFile);
                        for (int l = 0; l < lines.Length; l++)
                        {
                            string line = lines[l].Trim();
                            if (line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))
                                launchUrl = line.Substring(4).Trim();
                            else if (line.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase))
                                iconFile = line.Substring(9).Trim();
                            else if (line.StartsWith("IconIndex=", StringComparison.OrdinalIgnoreCase))
                                int.TryParse(line.Substring(10).Trim(), out iconIdx);
                        }
                    }
                    catch { }

                    if (string.IsNullOrEmpty(launchUrl)) continue;
                    bool isGameProtocol = launchUrl.StartsWith("steam://rungameid/", StringComparison.OrdinalIgnoreCase) ||
                                          launchUrl.StartsWith("com.epicgames.launcher://", StringComparison.OrdinalIgnoreCase);
                    if (!isGameProtocol) continue;
                    if (string.IsNullOrEmpty(iconFile) || !File.Exists(iconFile)) continue;

                    seenNames.Add(gameName);
                    ImageSource gameIcon = ExtractHighResIcon(iconFile, iconIdx);
                    if (gameIcon == null) gameIcon = GetIcon("games.png");

                    discovered.Add(new AppCatalogEntry
                    {
                        Title = gameName,
                        Cmd = urlFile,
                        Args = "",
                        VerifyPath = iconFile,
                        Category = "Игры",
                        IconSource = gameIcon
                    });
                }

                discovered.Sort(delegate(AppCatalogEntry a, AppCatalogEntry b)
                {
                    return string.Compare(a.Title, b.Title, StringComparison.CurrentCultureIgnoreCase);
                });

                list.AddRange(discovered);

                StringBuilder fp = new StringBuilder();
                for (int i = 0; i < list.Count; i++)
                {
                    fp.Append(list[i].Title).Append('|').Append(list[i].Cmd).Append(';');
                }
                string newFp = fp.ToString();
                if (newFp != _lastCatalogFingerprint)
                {
                    _lastCatalogFingerprint = newFp;
                    InstalledApps = list;
                    return true;
                }
            }
            catch { }
            return false;
        }

        private void AddCoreApp(List<AppCatalogEntry> list, HashSet<string> seenNames, HashSet<string> hiddenSet, string title, string cmd, string args, string iconPng, string category)
        {
            if (hiddenSet.Contains(title)) return;
            seenNames.Add(title);
            list.Add(new AppCatalogEntry
            {
                Title = title,
                Cmd = cmd,
                Args = args,
                VerifyPath = "",
                Category = category,
                IconSource = GetIcon(iconPng)
            });
        }

        public BitmapImage GetIcon(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) fileName = "finder.png";
            BitmapImage cached;
            if (_iconCache.TryGetValue(fileName, out cached)) return cached;

            string fullPath = IOPath.Combine(IconsDir, fileName);
            if (!File.Exists(fullPath))
            {
                fullPath = IOPath.Combine(IconsDir, "finder.png");
            }
            if (File.Exists(fullPath))
            {
                try
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.DecodePixelWidth = 96; // Downscale to 96px, reducing memory by ~90%
                    bmp.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bmp.EndInit();
                    bmp.Freeze();
                    _iconCache[fileName] = bmp;
                    return bmp;
                }
                catch { }
            }
            return null;
        }

        public void Start(string[] cliArgs)
        {
            // Always active core shell
            TopBar = new TopBarWindow();
            Dock = new DockWindow();
            if (Config.ShowDesktopWidgets) DesktopWidgets = new DesktopWidgetsWindow();

            ApplyModuleVisibility();

            if (Config.EnableStageManager)
            {
                if (StageManager == null) StageManager = new StageManagerWindow();
                StageManager.StartPoll();
                StageManager.RefreshShelf();
                StageManager.Show();
            }

            if (cliArgs != null)
            {
                for (int i = 0; i < cliArgs.Length; i++)
                {
                    string a = cliArgs[i].ToLowerInvariant();
                    if (a == "--settings" || a == "-s") ShowLauncherSettings();
                    else if (a == "--launchpad" || a == "-l") ToggleLaunchpad();
                    else if (a == "--search" || a == "-q") ToggleSpotlight();
                    else if (a == "--windows" || a == "-w") ToggleWindowSwitcher();
                }
            }
        }

        public void ApplyModuleVisibility()
        {
            if (TopBar != null)
            {
                if (Config.ShowTopBar && !TopBar.IsVisible) TopBar.Show();
                else if (!Config.ShowTopBar && TopBar.IsVisible) TopBar.Hide();
                TopBar.EnforceWorkAreaPublic();
            }
            if (Dock != null)
            {
                if (Config.ShowDock && !Dock.IsVisible) Dock.Show();
                else if (!Config.ShowDock && Dock.IsVisible) Dock.Hide();
            }
            if (DesktopWidgets != null)
            {
                if (Config.ShowDesktopWidgets && !DesktopWidgets.IsVisible)
                {
                    DesktopWidgets.Show();
                    NativeMethods.SendWindowToBottom(DesktopWidgets);
                }
                else if (!Config.ShowDesktopWidgets && DesktopWidgets.IsVisible)
                {
                    DesktopWidgets.Hide();
                }
            }
            if (Config.HideWindowsTaskbar)
                NativeMethods.HideNativeTaskbars();
            else
                NativeMethods.RestoreNativeTaskbars();
        }

        public bool IsCleanShutdown = false;

        public void ShutdownAndRestoreWindows()
        {
            IsCleanShutdown = true;
            try
            {
                if (TopBar != null) TopBar.UnregisterAndRestoreWorkArea();
                NativeMethods.RestoreNativeTaskbars();
                NativeMethods.RECT fullRect = new NativeMethods.RECT
                {
                    Left = 0,
                    Top = 0,
                    Right = (int)SystemParameters.PrimaryScreenWidth,
                    Bottom = (int)SystemParameters.PrimaryScreenHeight
                };
                NativeMethods.SystemParametersInfoRect(0x002F, 0, ref fullRect, 0);
            }
            catch { }
            Application.Current.Shutdown();
        }

        public void CloseAllPopups()
        {
            if (ControlCenter != null && ControlCenter.IsVisible) ControlCenter.HideAnimated();
            if (CalendarPopup != null && CalendarPopup.IsVisible) CalendarPopup.HideAnimated();
            if (NetworkMenu != null && NetworkMenu.IsVisible) NetworkMenu.HideAnimated();
            if (SoundMenu != null && SoundMenu.IsVisible) SoundMenu.HideAnimated();
            if (Spotlight != null && Spotlight.IsVisible) Spotlight.HideAnimated();
            if (Launchpad != null && Launchpad.IsVisible) Launchpad.HideWithAnimation();
            if (WindowSwitcher != null && WindowSwitcher.IsVisible) WindowSwitcher.HideAnimated();
            if (AppSwitcher != null && AppSwitcher.IsVisible) AppSwitcher.HideAnimated();
            if (SplitViewPicker != null && SplitViewPicker.IsVisible) SplitViewPicker.Hide();
            if (BackgroundTasks != null && BackgroundTasks.IsVisible) BackgroundTasks.HideAnimated();
        }

        public void ToggleSpotlight()
        {
            if (Spotlight == null) Spotlight = new SpotlightWindow();
            if (Spotlight.IsVisible)
            {
                Spotlight.HideAnimated();
            }
            else
            {
                CloseAllPopups();
                Spotlight.ShowAnimated();
            }
        }

        public void ToggleLaunchpad()
        {
            if (Launchpad == null) Launchpad = new LaunchpadWindow();
            if (Launchpad == null) Launchpad = new LaunchpadWindow();
            if (Launchpad.IsVisible)
            {
                Launchpad.HideWithAnimation();
            }
            else
            {
                CloseAllPopups();
                Launchpad.ShowWithAnimation();
            }
        }

        public void ToggleWindowSwitcher()
        {
            if (WindowSwitcher == null) WindowSwitcher = new WindowSwitcherWindow();
            if (WindowSwitcher.IsVisible)
            {
                WindowSwitcher.HideAnimated();
            }
            else
            {
                CloseAllPopups();
                WindowSwitcher.ShowAnimated();
            }
        }

        public void ToggleStageManager()
        {
            if (StageManager == null) StageManager = new StageManagerWindow();
            Config.EnableStageManager = !Config.EnableStageManager;
            SaveConfig();
            if (Config.EnableStageManager)
            {
                StageManager.StartPoll();
                StageManager.RefreshShelf();
                StageManager.Show();
                if (ActiveStageGroup != null && ActiveStageGroup.Windows.Count > 0 && Config.StageManagerAutoCenter)
                {
                    CenterWindowOnStage(ActiveStageGroup.Windows[0].Hwnd);
                }
            }
            else
            {
                StageManager.StopPoll();
                StageManager.Hide();
                RestoreAllStageWindows();
            }
            if (ControlCenter != null && ControlCenter.IsVisible)
            {
                ControlCenter.RefreshState();
            }
        }

        public void RestoreAllStageWindows()
        {
            try
            {
                IsStageDesktopRevealed = false;
                ActiveStageGroup = null;
                MergedStageWindows.Clear();

                // Restore all open application windows that were docked/minimized by Stage Manager
                List<OpenWindowEntry> openWins = GetOpenApplicationWindows();
                for (int i = 0; i < openWins.Count; i++)
                {
                    IntPtr h = openWins[i].Hwnd;
                    if (h != IntPtr.Zero && NativeMethods.IsWindow(h))
                    {
                        if (NativeMethods.IsIconic(h))
                        {
                            NativeMethods.ShowWindow(h, NativeMethods.SW_RESTORE);
                        }
                    }
                }
            }
            catch { }
        }

        public List<StageGroup> RefreshStageGroups()
        {
            List<OpenWindowEntry> openWins = GetOpenApplicationWindows();
            List<StageGroup> groups = new List<StageGroup>();

            for (int i = MergedStageWindows.Count - 1; i >= 0; i--)
            {
                List<IntPtr> list = MergedStageWindows[i];
                for (int j = list.Count - 1; j >= 0; j--)
                {
                    if (!NativeMethods.IsWindow(list[j])) list.RemoveAt(j);
                }
                if (list.Count <= 1) MergedStageWindows.RemoveAt(i);
            }

            HashSet<IntPtr> processed = new HashSet<IntPtr>();

            for (int i = 0; i < MergedStageWindows.Count; i++)
            {
                List<IntPtr> list = MergedStageWindows[i];
                StageGroup g = new StageGroup();
                g.Id = "merged_" + i.ToString();
                for (int j = 0; j < openWins.Count; j++)
                {
                    if (list.Contains(openWins[j].Hwnd))
                    {
                        g.Windows.Add(openWins[j]);
                        processed.Add(openWins[j].Hwnd);
                    }
                }
                if (g.Windows.Count > 0)
                {
                    g.Title = g.Windows[0].AppTitle;
                    g.ProcessName = g.Windows[0].ProcessName;
                    g.Icon = g.Windows[0].Icon != null ? g.Windows[0].Icon : DefaultAppIcon;
                    groups.Add(g);
                }
            }

            if (Config.StageManagerGroupSameApp)
            {
                Dictionary<string, StageGroup> byApp = new Dictionary<string, StageGroup>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < openWins.Count; i++)
                {
                    OpenWindowEntry w = openWins[i];
                    if (processed.Contains(w.Hwnd)) continue;

                    string key = !string.IsNullOrEmpty(w.ProcessName) ? w.ProcessName : w.AppTitle;
                    StageGroup g;
                    if (!byApp.TryGetValue(key, out g))
                    {
                        g = new StageGroup();
                        g.Id = "app_" + key;
                        g.Title = w.AppTitle;
                        g.ProcessName = w.ProcessName;
                        g.Icon = w.Icon != null ? w.Icon : DefaultAppIcon;
                        byApp[key] = g;
                        groups.Add(g);
                    }
                    g.Windows.Add(w);
                    processed.Add(w.Hwnd);
                }
            }
            else
            {
                for (int i = 0; i < openWins.Count; i++)
                {
                    OpenWindowEntry w = openWins[i];
                    if (processed.Contains(w.Hwnd)) continue;

                    StageGroup g = new StageGroup();
                    g.Id = "win_" + w.Hwnd.ToString();
                    g.Title = w.AppTitle;
                    g.ProcessName = w.ProcessName;
                    g.Icon = w.Icon != null ? w.Icon : DefaultAppIcon;
                    g.Windows.Add(w);
                    groups.Add(g);
                    processed.Add(w.Hwnd);
                }
            }

            IntPtr fg = NativeMethods.GetForegroundWindow();
            ActiveStageGroup = null;
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].HasHwnd(fg))
                {
                    ActiveStageGroup = groups[i];
                    break;
                }
            }

            CurrentStageGroups = groups;
            return groups;
        }

        public void SwitchToStageGroup(StageGroup target)
        {
            if (target == null) return;
            IsStageDesktopRevealed = false;

            for (int i = 0; i < CurrentStageGroups.Count; i++)
            {
                StageGroup other = CurrentStageGroups[i];
                if (other != target)
                {
                    for (int j = 0; j < other.Windows.Count; j++)
                    {
                        IntPtr h = other.Windows[j].Hwnd;
                        if (!target.HasHwnd(h) && NativeMethods.IsWindow(h))
                        {
                            NativeMethods.ShowWindow(h, NativeMethods.SW_SHOWMINIMIZED);
                        }
                    }
                }
            }

            for (int i = 0; i < target.Windows.Count; i++)
            {
                IntPtr h = target.Windows[i].Hwnd;
                if (NativeMethods.IsWindow(h))
                {
                    NativeMethods.ForceForegroundWindow(h);
                }
            }

            if (Config.StageManagerAutoCenter && target.Windows.Count == 1)
            {
                CenterWindowOnStage(target.Windows[0].Hwnd);
            }

            ActiveStageGroup = target;
            if (StageManager != null) StageManager.RefreshShelf();
        }

        public void AddGroupToActiveStage(StageGroup group)
        {
            if (group == null) return;
            IsStageDesktopRevealed = false;

            if (ActiveStageGroup == null || ActiveStageGroup.Windows.Count == 0)
            {
                SwitchToStageGroup(group);
                return;
            }

            List<IntPtr> merged = null;
            for (int i = 0; i < MergedStageWindows.Count; i++)
            {
                if (MergedStageWindows[i].Contains(ActiveStageGroup.Windows[0].Hwnd))
                {
                    merged = MergedStageWindows[i];
                    break;
                }
            }
            if (merged == null)
            {
                merged = new List<IntPtr>();
                for (int i = 0; i < ActiveStageGroup.Windows.Count; i++)
                {
                    merged.Add(ActiveStageGroup.Windows[i].Hwnd);
                }
                MergedStageWindows.Add(merged);
            }

            for (int i = 0; i < group.Windows.Count; i++)
            {
                IntPtr h = group.Windows[i].Hwnd;
                if (!merged.Contains(h)) merged.Add(h);
                if (NativeMethods.IsWindow(h))
                {
                    NativeMethods.ForceForegroundWindow(h);
                }
            }

            RefreshStageGroups();
            if (StageManager != null) StageManager.RefreshShelf();
        }

        public void CycleStageGroupWindows(StageGroup group)
        {
            if (group == null || group.Windows.Count <= 1) return;
            OpenWindowEntry first = group.Windows[0];
            group.Windows.RemoveAt(0);
            group.Windows.Add(first);

            IntPtr topHwnd = group.Windows[0].Hwnd;
            if (NativeMethods.IsWindow(topHwnd))
            {
                if (NativeMethods.IsIconic(topHwnd)) NativeMethods.ShowWindow(topHwnd, NativeMethods.SW_RESTORE);
                NativeMethods.SetForegroundWindow(topHwnd);
                NativeMethods.SwitchToThisWindow(topHwnd, false);
            }
            if (StageManager != null) StageManager.RefreshShelf();
        }

        public void HideActiveStageToDesktop()
        {
            if (ActiveStageGroup == null) return;
            IsStageDesktopRevealed = !IsStageDesktopRevealed;

            if (IsStageDesktopRevealed)
            {
                for (int i = 0; i < ActiveStageGroup.Windows.Count; i++)
                {
                    IntPtr h = ActiveStageGroup.Windows[i].Hwnd;
                    if (NativeMethods.IsWindow(h))
                    {
                        NativeMethods.ShowWindow(h, NativeMethods.SW_SHOWMINIMIZED);
                    }
                }
            }
            else
            {
                for (int i = 0; i < ActiveStageGroup.Windows.Count; i++)
                {
                    IntPtr h = ActiveStageGroup.Windows[i].Hwnd;
                    if (NativeMethods.IsWindow(h))
                    {
                        NativeMethods.ShowWindow(h, NativeMethods.SW_RESTORE);
                        NativeMethods.SetForegroundWindow(h);
                    }
                }
            }
            if (StageManager != null) StageManager.RefreshShelf();
        }

        public void CenterWindowOnStage(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd) || !Config.EnableStageManager) return;
            if (!Config.StageManagerAutoCenter) return;
            if (NativeMethods.IsZoomed(hwnd)) return;

            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;
            double top = (Config.ShowTopBar && TopBar != null) ? 32.0 : 0.0;
            double dockH = (Config.ShowDock && !Config.DockAutoHide && Dock != null) ? 76.0 : 0.0;
            double leftShelfW = (!Config.StageManagerAutoHide) ? 148.0 : 0.0;

            double usableW = screenW - leftShelfW;
            double usableH = screenH - top - dockH;

            NativeMethods.RECT r;
            if (NativeMethods.GetWindowRect(hwnd, out r))
            {
                int currentW = r.Right - r.Left;
                int currentH = r.Bottom - r.Top;

                int targetW = Math.Min(currentW, (int)(usableW * 0.92));
                int targetH = Math.Min(currentH, (int)(usableH * 0.94));
                if (targetW < 400) targetW = (int)(usableW * 0.78);
                if (targetH < 300) targetH = (int)(usableH * 0.84);

                int targetX = (int)(leftShelfW + (usableW - targetW) / 2.0);
                int targetY = (int)(top + (usableH - targetH) / 2.0);

                NativeMethods.MoveWindow(hwnd, targetX, targetY, targetW, targetH, true);
            }
        }

        public void ShowAppSwitcher(bool backward)
        {
            if (AppSwitcher == null) AppSwitcher = new AppSwitcherWindow();
            AppSwitcher.ShowAndStep(backward);
        }

        public void CommitAppSwitcher()
        {
            if (AppSwitcher != null && AppSwitcher.IsVisible)
            {
                AppSwitcher.CommitSelection();
            }
            NativeMethods.keybd_event(0x12, 0, 2, UIntPtr.Zero);
            NativeMethods.keybd_event(0xA4, 0, 2, UIntPtr.Zero);
            NativeMethods.keybd_event(0xA5, 0, 2, UIntPtr.Zero);
        }

        public void AppSwitcherQuit()
        {
            if (AppSwitcher != null && AppSwitcher.IsVisible)
            {
                AppSwitcher.QuitSelectedApp();
            }
        }

        public void AppSwitcherHide()
        {
            if (AppSwitcher != null && AppSwitcher.IsVisible)
            {
                AppSwitcher.HideSelectedApp();
            }
        }

        public void CancelAppSwitcher()
        {
            if (AppSwitcher != null && AppSwitcher.IsVisible)
            {
                AppSwitcher.Cancel();
            }
        }

        public void StartSplitViewWithPicker(IntPtr firstHwnd, bool isLeft)
        {
            if (firstHwnd == IntPtr.Zero) firstHwnd = NativeMethods.GetForegroundWindow();
            if (firstHwnd == IntPtr.Zero || !NativeMethods.IsWindow(firstHwnd)) return;

            double top = (Config.ShowTopBar && TopBar != null) ? 32.0 : 0.0;
            double dockH = (Config.ShowDock && !Config.DockAutoHide && Dock != null) ? 76.0 : 0.0;
            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;
            double usableH = screenH - top - dockH;
            double halfW = screenW / 2.0;

            NativeMethods.ShowWindow(firstHwnd, NativeMethods.SW_RESTORE);
            if (isLeft)
                NativeMethods.MoveWindow(firstHwnd, 0, (int)top, (int)halfW, (int)usableH, true);
            else
                NativeMethods.MoveWindow(firstHwnd, (int)halfW, (int)top, (int)halfW, (int)usableH, true);

            if (SplitViewPicker == null) SplitViewPicker = new SplitViewPickerWindow();
            SplitViewPicker.ShowPicker(firstHwnd, isLeft);
        }

        public void EnterSplitView(IntPtr leftHwnd, IntPtr rightHwnd, double ratio)
        {
            if (SplitViewPicker != null && SplitViewPicker.IsVisible) SplitViewPicker.Hide();
            if (BackgroundTasks != null && BackgroundTasks.IsVisible) BackgroundTasks.Hide();
            if (SplitViewDivider == null) SplitViewDivider = new SplitViewDividerWindow();
            SplitViewDivider.LeftHwnd = leftHwnd;
            SplitViewDivider.RightHwnd = rightHwnd;
            SplitViewDivider.SplitRatio = ratio;
            SplitViewDivider.IsolateAndApplyLayout();
            SplitViewDivider.Show();
        }

        public void ExitSplitView()
        {
            if (SplitViewPicker != null && SplitViewPicker.IsVisible) SplitViewPicker.Hide();
            if (BackgroundTasks != null && BackgroundTasks.IsVisible) BackgroundTasks.Hide();
            if (SplitViewDivider != null && SplitViewDivider.IsVisible)
            {
                SplitViewDivider.RestoreIsolatedWindows();
                SplitViewDivider.Hide();
            }
        }

        public void SnapActiveWindow(bool left)
        {
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return;
            double top = (Config.ShowTopBar && TopBar != null) ? 32.0 : 0.0;
            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;
            double h = screenH - top;
            double w = screenW / 2.0;
            double x = left ? 0.0 : w;

            NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
            NativeMethods.MoveWindow(hwnd, (int)x, (int)top, (int)w, (int)h, true);
        }

        public void SnapActiveWindowRatio(bool left, double ratio)
        {
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return;
            double top = (Config.ShowTopBar && TopBar != null) ? 32.0 : 0.0;
            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;
            double h = screenH - top;
            double w = screenW * ratio;
            double x = left ? 0.0 : (screenW - w);

            NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
            NativeMethods.MoveWindow(hwnd, (int)x, (int)top, (int)w, (int)h, true);
        }

        public void CreateNewSpace()
        {
            if (Config.Spaces == null) Config.Spaces = new List<string>();
            int newIndex = Config.Spaces.Count + 1;
            Config.Spaces.Add("Рабочий стол " + newIndex.ToString());
            SaveConfig();
            SwitchToSpace(Config.Spaces.Count - 1);
        }

        public void SwitchToSpace(int index)
        {
            if (Config.Spaces == null || index < 0 || index >= Config.Spaces.Count) return;
            int previousSpace = Config.CurrentSpaceIndex;
            if (previousSpace == index) return;

            // 1. Map any currently untracked open windows to previousSpace
            List<OpenWindowEntry> openWins = GetOpenApplicationWindows();
            for (int i = 0; i < openWins.Count; i++)
            {
                IntPtr h = openWins[i].Hwnd;
                if (!WindowSpaceMap.ContainsKey(h))
                {
                    int assigned = GetAppSpaceAssignment(openWins[i].ProcessName);
                    WindowSpaceMap[h] = (assigned >= 0 && assigned < Config.Spaces.Count) ? assigned : previousSpace;
                }
            }

            // 2. Perform true space window isolation: reveal target space windows, hide other space windows
            List<IntPtr> tracked = new List<IntPtr>(WindowSpaceMap.Keys);
            for (int i = 0; i < tracked.Count; i++)
            {
                IntPtr h = tracked[i];
                if (!NativeMethods.IsWindow(h))
                {
                    WindowSpaceMap.Remove(h);
                    _hiddenBySpaceManager.Remove(h);
                    continue;
                }

                int space = WindowSpaceMap[h];
                if (space == index)
                {
                    if (_hiddenBySpaceManager.Contains(h))
                    {
                        NativeMethods.ShowWindow(h, NativeMethods.SW_SHOWNOACTIVATE);
                        _hiddenBySpaceManager.Remove(h);
                    }
                }
                else
                {
                    if (NativeMethods.IsWindowVisible(h))
                    {
                        _hiddenBySpaceManager.Add(h);
                        NativeMethods.ShowWindow(h, NativeMethods.SW_HIDE);
                    }
                }
            }

            Config.CurrentSpaceIndex = index;
            SaveConfig();

            if (Config.SpaceWallpapers != null && index < Config.SpaceWallpapers.Count)
            {
                string wp = Config.SpaceWallpapers[index];
                if (!string.IsNullOrEmpty(wp) && File.Exists(wp)) ApplyWallpaperFile(wp);
            }
        }

        public void SetSpaceWallpaper(int spaceIndex, string wpPath)
        {
            if (Config.SpaceWallpapers == null) Config.SpaceWallpapers = new List<string>();
            while (Config.SpaceWallpapers.Count <= spaceIndex) Config.SpaceWallpapers.Add("");
            Config.SpaceWallpapers[spaceIndex] = wpPath;
            SaveConfig();
            if (spaceIndex == Config.CurrentSpaceIndex && File.Exists(wpPath)) ApplyWallpaperFile(wpPath);
        }

        public void MoveWindowToSpace(IntPtr hwnd, int targetSpaceIndex)
        {
            if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd)) return;
            WindowSpaceMap[hwnd] = targetSpaceIndex;
            if (targetSpaceIndex != Config.CurrentSpaceIndex)
            {
                _hiddenBySpaceManager.Add(hwnd);
                NativeMethods.ShowWindow(hwnd, NativeMethods.SW_HIDE);
            }
            else
            {
                _hiddenBySpaceManager.Remove(hwnd);
                NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOW);
                NativeMethods.SetForegroundWindow(hwnd);
            }
        }

        public void AssignAppToSpace(string appName, int spaceIndex)
        {
            if (string.IsNullOrEmpty(appName)) return;
            if (Config.AppSpaceAssignments == null)
                Config.AppSpaceAssignments = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Config.AppSpaceAssignments[appName] = spaceIndex;
            SaveConfig();
        }

        public int GetAppSpaceAssignment(string appName)
        {
            if (string.IsNullOrEmpty(appName) || Config.AppSpaceAssignments == null) return -1;
            int s;
            if (Config.AppSpaceAssignments.TryGetValue(appName, out s)) return s;
            return -1;
        }

        public void ClearAppSpaceAssignment(string appName)
        {
            if (string.IsNullOrEmpty(appName) || Config.AppSpaceAssignments == null) return;
            Config.AppSpaceAssignments.Remove(appName);
            SaveConfig();
        }

        private DateTime _lastHotCornerTrigger = DateTime.MinValue;
        public void CheckHotCorners(int mouseX, int mouseY)
        {
            if (!Config.EnableHotCorners) return;
            if ((DateTime.Now - _lastHotCornerTrigger).TotalMilliseconds < 1200) return;

            int screenW = NativeMethods.GetSystemMetrics(0 /* SM_CXSCREEN */);
            int screenH = NativeMethods.GetSystemMetrics(1 /* SM_CYSCREEN */);
            if (screenW <= 0) screenW = (int)SystemParameters.PrimaryScreenWidth;
            if (screenH <= 0) screenH = (int)SystemParameters.PrimaryScreenHeight;

            if (mouseX <= 2 && mouseY <= 2)
            {
                _lastHotCornerTrigger = DateTime.Now;
                ToggleWindowSwitcher();
            }
            else if (mouseX >= screenW - 3 && mouseY <= 2)
            {
                _lastHotCornerTrigger = DateTime.Now;
                ToggleControlCenter();
            }
            else if (mouseX <= 2 && mouseY >= screenH - 3)
            {
                _lastHotCornerTrigger = DateTime.Now;
                ToggleLaunchpad();
            }
            else if (mouseX >= screenW - 3 && mouseY >= screenH - 3)
            {
                _lastHotCornerTrigger = DateTime.Now;
                HideActiveStageToDesktop();
            }
        }

        public void ToggleAppExpose()
        {
            Config.MissionControlGroupByApp = true;
            ToggleWindowSwitcher();
        }

        public void CloseSpace(int index)
        {
            if (Config.Spaces == null || Config.Spaces.Count <= 1 || index < 0 || index >= Config.Spaces.Count) return;

            int fallbackSpace = Math.Max(0, index - 1);
            List<IntPtr> keys = new List<IntPtr>(WindowSpaceMap.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                IntPtr h = keys[i];
                if (WindowSpaceMap[h] == index)
                {
                    WindowSpaceMap[h] = fallbackSpace;
                }
                else if (WindowSpaceMap[h] > index)
                {
                    WindowSpaceMap[h]--;
                }
            }

            Config.Spaces.RemoveAt(index);
            if (Config.SpaceWallpapers != null && index < Config.SpaceWallpapers.Count)
            {
                Config.SpaceWallpapers.RemoveAt(index);
            }
            if (Config.CurrentSpaceIndex >= Config.Spaces.Count)
                Config.CurrentSpaceIndex = Config.Spaces.Count - 1;
            SaveConfig();

            SwitchToSpace(Config.CurrentSpaceIndex);
        }

        public void ShowLauncherSettings()
        {
            CloseAllPopups();
            if (SettingsHub == null || !SettingsHub.IsLoaded)
            {
                SettingsHub = new LauncherSettingsWindow();
            }
            SettingsHub.RefreshAllTabs();
            SettingsHub.Show();
            SettingsHub.Activate();
        }

        public void ToggleBackgroundTasks()
        {
            if (BackgroundTasks == null) BackgroundTasks = new BackgroundTasksWindow();
            if (BackgroundTasks.IsVisible)
            {
                BackgroundTasks.HideAnimated();
            }
            else
            {
                CloseAllPopups();
                BackgroundTasks.ShowAnimated();
            }
        }


        public void ToggleNetworkMenu()
        {
            if (NetworkMenu == null) NetworkMenu = new NetworkMenuWindow();
            if (NetworkMenu.IsVisible)
            {
                NetworkMenu.HideAnimated();
            }
            else
            {
                CloseAllPopups();
                NetworkMenu.ShowAnimated();
            }
        }

        public void ToggleSoundMenu()
        {
            if (SoundMenu == null) SoundMenu = new SoundMenuWindow();
            if (SoundMenu.IsVisible)
            {
                SoundMenu.HideAnimated();
            }
            else
            {
                CloseAllPopups();
                SoundMenu.ShowAnimated();
            }
        }

        public void ToggleControlCenter()
        {
            if (ControlCenter == null) ControlCenter = new ControlCenterWindow();
            if (ControlCenter.IsVisible)
            {
                ControlCenter.HideAnimated();
            }
            else
            {
                CloseAllPopups();
                ControlCenter.ShowAnimated();
            }
        }

        public void ToggleCalendarPopup()
        {
            if (CalendarPopup == null) CalendarPopup = new CalendarWidgetWindow();
            if (CalendarPopup.IsVisible)
            {
                CalendarPopup.HideAnimated();
            }
            else
            {
                CloseAllPopups();
                CalendarPopup.ShowAnimated();
            }
        }

        public void ShowAboutSystem()
        {
            CloseAllPopups();
            if (AboutWindow == null || !AboutWindow.IsLoaded)
            {
                AboutWindow = new AboutSystemWindow();
            }
            AboutWindow.Show();
            AboutWindow.Activate();
        }

        public void ToggleDarkMode()
        {
            IsDarkMode = !IsDarkMode;
            SaveConfig();
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true);
                if (key != null)
                {
                    int val = IsDarkMode ? 0 : 1;
                    key.SetValue("AppsUseLightTheme", val, RegistryValueKind.DWord);
                    key.SetValue("SystemUsesLightTheme", val, RegistryValueKind.DWord);
                    key.Close();
                }
                string wp = IOPath.Combine(WallpapersDir, IsDarkMode ? "26-Tahoe-Dark-6K.jpg" : "26-Tahoe-Light-6K.jpg");
                ApplyWallpaperFile(wp);
            }
            catch { }
        }

        public void ApplyWallpaperFile(string wpPath)
        {
            if (string.IsNullOrEmpty(wpPath) || !File.Exists(wpPath)) return;
            try
            {
                RegistryKey dkey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
                if (dkey != null)
                {
                    dkey.SetValue("WallPaper", wpPath, RegistryValueKind.String);
                    dkey.Close();
                }
                NativeMethods.SystemParametersInfo(0x0014, 0, wpPath, 3);
            }
            catch { }
        }

        public void SetBrightnessOverlay(double percent)
        {
            SimulatedBrightness = Math.Max(20.0, Math.Min(100.0, percent));
            double dimOpacity = (100.0 - SimulatedBrightness) / 115.0;
            if (dimOpacity <= 0.02)
            {
                if (ScreenDimOverlay != null && ScreenDimOverlay.IsVisible)
                    ScreenDimOverlay.Hide();
                return;
            }
            if (ScreenDimOverlay == null)
            {
                ScreenDimOverlay = new Window();
                ScreenDimOverlay.WindowStyle = WindowStyle.None;
                ScreenDimOverlay.AllowsTransparency = true;
                ScreenDimOverlay.Background = Brushes.Black;
                ScreenDimOverlay.Topmost = true;
                ScreenDimOverlay.ShowInTaskbar = false;
                ScreenDimOverlay.IsHitTestVisible = false;
                ScreenDimOverlay.Left = 0;
                ScreenDimOverlay.Top = 0;
                ScreenDimOverlay.Width = SystemParameters.PrimaryScreenWidth;
                ScreenDimOverlay.Height = SystemParameters.PrimaryScreenHeight;
                ScreenDimOverlay.SourceInitialized += delegate(object s, EventArgs e)
                {
                    WindowInteropHelper h = new WindowInteropHelper(ScreenDimOverlay);
                    int ex = NativeMethods.GetWindowLong(h.Handle, NativeMethods.GWL_EXSTYLE);
                    NativeMethods.SetWindowLong(h.Handle, NativeMethods.GWL_EXSTYLE,
                        ex | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TRANSPARENT);
                };
            }
            ScreenDimOverlay.Opacity = dimOpacity;
            if (!ScreenDimOverlay.IsVisible) ScreenDimOverlay.Show();
        }

        public List<OpenWindowEntry> GetOpenApplicationWindows()
        {
            List<OpenWindowEntry> result = new List<OpenWindowEntry>();
            uint selfPid = (uint)Process.GetCurrentProcess().Id;

            NativeMethods.EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
            {
                if (!NativeMethods.IsWindowVisible(hWnd)) return true;

                int cloaked = 0;
                NativeMethods.DwmGetWindowAttribute(hWnd, 14, out cloaked, sizeof(int));
                if (cloaked != 0) return true;

                int exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
                if ((exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0) return true;
                if (NativeMethods.GetWindow(hWnd, 4) != IntPtr.Zero) return true;

                StringBuilder sbTitle = new StringBuilder(256);
                NativeMethods.GetWindowText(hWnd, sbTitle, sbTitle.Capacity);
                string winTitle = sbTitle.ToString();

                StringBuilder sbClass = new StringBuilder(128);
                NativeMethods.GetClassName(hWnd, sbClass, sbClass.Capacity);
                string cls = sbClass.ToString();
                if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd" ||
                    cls == "Windows.UI.Core.CoreWindow" || cls == "ApplicationFrameWindow" || cls == "CEF-OSC-WIDGET")
                    return true;

                uint pid;
                NativeMethods.GetWindowThreadProcessId(hWnd, out pid);
                if (pid == selfPid) return true;

                try
                {
                    Process p = Process.GetProcessById((int)pid);
                    string pn = p.ProcessName;
                    if (string.IsNullOrEmpty(winTitle)) winTitle = pn;
                    if (string.Equals(pn, "seelen-ui", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(pn, "TextInputHost", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(pn, "SearchHost", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(pn, "ShellExperienceHost", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(pn, "StartMenuExperienceHost", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    bool isExplorerWindow = (cls == "CabinetWClass" || cls == "ExploreWClass");
                    ImageSource icon = isExplorerWindow ? GetIcon("finder.png") : ResolveDynamicIcon(pn, pid);

                    result.Add(new OpenWindowEntry
                    {
                        Hwnd = hWnd,
                        Pid = pid,
                        ProcessName = pn,
                        AppTitle = isExplorerWindow ? "Проводник" : pn,
                        WindowTitle = winTitle,
                        IsMinimized = NativeMethods.IsIconic(hWnd),
                        Icon = icon
                    });
                }
                catch { }
                return true;
            }, IntPtr.Zero);

            return result;
        }

        public void LaunchOrFocusDockItem(DockAppItem item)
        {
            if (item == null || item.IsSeparator) return;

            if (Dock != null)
            {
                item.BounceStartTime = Dock.ElapsedSeconds;
            }

            if (item.IsAppsLauncher || string.Equals(item.LaunchCommand, "@launchpad", StringComparison.OrdinalIgnoreCase))
            {
                ToggleLaunchpad();
                return;
            }

            if (item.IsWindowSwitcher || string.Equals(item.LaunchCommand, "@windows", StringComparison.OrdinalIgnoreCase))
            {
                ToggleWindowSwitcher();
                return;
            }

            if (item.IsTrash || item.IsDesktopFolder)
            {
                RunProcess(item.LaunchCommand, item.LaunchArgs);
                return;
            }

            if (item.ActiveHwnd != IntPtr.Zero && NativeMethods.IsWindow(item.ActiveHwnd))
            {
                IntPtr fg = NativeMethods.GetForegroundWindow();
                if (fg == item.ActiveHwnd && !NativeMethods.IsIconic(item.ActiveHwnd))
                {
                    NativeMethods.ShowWindow(item.ActiveHwnd, NativeMethods.SW_SHOWMINIMIZED);
                }
                else
                {
                    NativeMethods.ForceForegroundWindow(item.ActiveHwnd);
                }
                return;
            }

            // Fallback: check if application is already running and find its window
            if (item.ProcessNames != null && item.ProcessNames.Length > 0)
            {
                for (int p = 0; p < item.ProcessNames.Length; p++)
                {
                    IntPtr foundWnd = FindProcessWindow(item.ProcessNames[p]);
                    if (foundWnd != IntPtr.Zero)
                    {
                        item.ActiveHwnd = foundWnd;
                        NativeMethods.ForceForegroundWindow(foundWnd);
                        return;
                    }
                }
            }

            RunProcess(item.LaunchCommand, item.LaunchArgs);
        }

        public static void RunProcess(string cmd, string args)
        {
            RunProcessEx(cmd, args, false);
        }

        public static IntPtr FindProcessWindow(string processName)
        {
            IntPtr found = IntPtr.Zero;
            try
            {
                NativeMethods.EnumWindows(delegate(IntPtr hWnd, IntPtr lp)
                {
                    if (!NativeMethods.IsWindowVisible(hWnd)) return true;
                    int exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
                    if ((exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0) return true;
                    if (NativeMethods.GetWindow(hWnd, 4) != IntPtr.Zero) return true;

                    uint pid;
                    NativeMethods.GetWindowThreadProcessId(hWnd, out pid);
                    try
                    {
                        Process p = Process.GetProcessById((int)pid);
                        if (string.Equals(p.ProcessName, processName, StringComparison.OrdinalIgnoreCase))
                        {
                            found = hWnd;
                            return false;
                        }
                    }
                    catch { }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
            return found;
        }

        public static void RunProcessEx(string cmd, string args, bool asAdmin)
        {
            try
            {
                if (string.IsNullOrEmpty(cmd)) return;
                if (string.Equals(cmd, "@launchpad", StringComparison.OrdinalIgnoreCase))
                {
                    if (Instance != null) Instance.ToggleLaunchpad();
                    return;
                }
                if (string.Equals(cmd, "@windows", StringComparison.OrdinalIgnoreCase))
                {
                    if (Instance != null) Instance.ToggleWindowSwitcher();
                    return;
                }
                if (string.Equals(cmd, "@settings", StringComparison.OrdinalIgnoreCase))
                {
                    if (Instance != null) Instance.ShowLauncherSettings();
                    return;
                }

                if (cmd.IndexOf('\\') >= 0 && !File.Exists(cmd) && Instance != null)
                {
                    string exeName = IOPath.GetFileName(cmd);
                    for (int i = 0; i < Instance.InstalledApps.Count; i++)
                    {
                        AppCatalogEntry entry = Instance.InstalledApps[i];
                        if (string.Equals(IOPath.GetFileName(entry.Cmd), exeName, StringComparison.OrdinalIgnoreCase))
                        {
                            cmd = entry.Cmd;
                            if (string.IsNullOrEmpty(args)) args = entry.Args;
                            break;
                        }
                    }
                }
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = cmd;
                if (!string.IsNullOrEmpty(args)) psi.Arguments = args;
                psi.UseShellExecute = true;
                if (asAdmin) psi.Verb = "runas";
                Process.Start(psi);
            }
            catch { }
        }

        public static void OpenFileLocation(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                if (File.Exists(path))
                {
                    RunProcess("explorer.exe", "/select,\"" + path + "\"");
                }
                else if (Directory.Exists(path))
                {
                    RunProcess("explorer.exe", "\"" + path + "\"");
                }
            }
            catch { }
        }
    }

}
