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

[assembly: AssemblyTitle("Tahoe Liquid Glass Launcher & Shell for Windows 11")]
[assembly: AssemblyProduct("Tahoe Liquid Glass Launcher")]
[assembly: AssemblyDescription("Native GPU-accelerated macOS 26 Tahoe Desktop Launcher, Dock, TopBar, Omni-Search & Widgets for Windows 10/11")]
[assembly: AssemblyVersion("26.2.0.0")]
[assembly: AssemblyFileVersion("26.2.0.0")]

namespace MacOSTahoe
{
    public static class NativeMethods
    {
        [DllImport("shcore.dll")]
        public static extern int SetProcessDpiAwareness(int value);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
        public static extern bool SystemParametersInfoRect(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        public static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("shell32.dll")]
        public static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint flags);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHDefExtractIconW(string pszIconFile, int iIndex, uint uFlags, out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIconSize);

        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr hIcon);

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        public class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        public interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [DllImport("user32.dll")]
        public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AccentPolicy
        {
            public int AccentState;
            public int AccentFlags;
            public uint GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TRANSPARENT = 0x00000020;

        public const int SW_HIDE = 0;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_MAXIMIZE = 3;
        public const int SW_RESTORE = 9;

        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;

        public const uint ABM_NEW = 0x00000000;
        public const uint ABM_REMOVE = 0x00000001;
        public const uint ABM_QUERYPOS = 0x00000002;
        public const uint ABM_SETPOS = 0x00000003;
        public const uint ABE_TOP = 1;

        public const uint WM_CLOSE = 0x0010;
        public const uint WM_COMMAND = 0x0111;
        public const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;

        public static void EnableBlur(Window window, uint tintColor)
        {
            try
            {
                WindowInteropHelper helper = new WindowInteropHelper(window);
                if (helper.Handle == IntPtr.Zero) return;
                AccentPolicy accent = new AccentPolicy();
                accent.AccentState = 3; // ACCENT_ENABLE_BLURBEHIND
                accent.AccentFlags = 2;
                accent.GradientColor = tintColor;

                int size = Marshal.SizeOf(accent);
                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(accent, ptr, false);

                WindowCompositionAttributeData data = new WindowCompositionAttributeData();
                data.Attribute = 19; // WCA_ACCENT_POLICY
                data.SizeOfData = size;
                data.Data = ptr;

                SetWindowCompositionAttribute(helper.Handle, ref data);
                Marshal.FreeHGlobal(ptr);
            }
            catch { }
        }

        public static void MakeToolWindow(Window window, bool noActivate)
        {
            try
            {
                WindowInteropHelper helper = new WindowInteropHelper(window);
                if (helper.Handle == IntPtr.Zero) return;
                int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
                exStyle |= WS_EX_TOOLWINDOW;
                if (noActivate) exStyle |= WS_EX_NOACTIVATE;
                SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle);
            }
            catch { }
        }

        public static void SendWindowToBottom(Window window)
        {
            try
            {
                WindowInteropHelper helper = new WindowInteropHelper(window);
                if (helper.Handle == IntPtr.Zero) return;
                SetWindowPos(helper.Handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
            catch { }
        }

        public static IntPtr GetShellDefView()
        {
            IntPtr progman = FindWindow("Progman", null);
            IntPtr defView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView != IntPtr.Zero) return defView;
            EnumWindows(delegate(IntPtr h, IntPtr l)
            {
                IntPtr d = FindWindowEx(h, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (d != IntPtr.Zero)
                {
                    defView = d;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return defView;
        }

        public static bool AreDesktopIconsVisible()
        {
            IntPtr defView = GetShellDefView();
            if (defView == IntPtr.Zero) return false;
            IntPtr listView = FindWindowEx(defView, IntPtr.Zero, "SysListView32", null);
            if (listView == IntPtr.Zero) return false;
            return IsWindowVisible(listView);
        }

        public static void ToggleDesktopIcons()
        {
            IntPtr defView = GetShellDefView();
            if (defView != IntPtr.Zero)
            {
                SendMessage(defView, WM_COMMAND, (IntPtr)0x7402, IntPtr.Zero);
            }
        }

        public static void HideNativeTaskbars()
        {
            try
            {
                if (ShellController.Instance != null && !ShellController.Instance.Config.HideWindowsTaskbar)
                    return;

                IntPtr tray = FindWindow("Shell_TrayWnd", null);
                if (tray != IntPtr.Zero && IsWindowVisible(tray))
                {
                    ShowWindow(tray, SW_HIDE);
                }
                IntPtr secTray = FindWindow("Shell_SecondaryTrayWnd", null);
                if (secTray != IntPtr.Zero && IsWindowVisible(secTray))
                {
                    ShowWindow(secTray, SW_HIDE);
                }
            }
            catch { }
        }

        public static void RestoreNativeTaskbars()
        {
            try
            {
                IntPtr tray = FindWindow("Shell_TrayWnd", null);
                if (tray != IntPtr.Zero)
                {
                    ShowWindow(tray, SW_SHOWNORMAL);
                }
                IntPtr secTray = FindWindow("Shell_SecondaryTrayWnd", null);
                if (secTray != IntPtr.Zero)
                {
                    ShowWindow(secTray, SW_SHOWNORMAL);
                }
            }
            catch { }
        }
    }

    // =========================================================================
    // PERSISTENT LAUNCHER CONFIGURATION MODEL (config.json)
    // =========================================================================
    public class DockItemConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string IconFile { get; set; }
        public string LaunchCommand { get; set; }
        public string LaunchArgs { get; set; }
        public string[] ProcessNames { get; set; }
        public bool IsSeparator { get; set; }
        public bool IsTrash { get; set; }
        public bool IsAppsLauncher { get; set; }
        public bool IsDesktopFolder { get; set; }
        public bool IsExplorer { get; set; }
        public bool IsWindowSwitcher { get; set; }
    }

    public class CustomAppConfig
    {
        public string Title { get; set; }
        public string Cmd { get; set; }
        public string Args { get; set; }
        public string Category { get; set; }
    }

    public class LauncherConfig
    {
        public bool ShowTopBar { get; set; }
        public bool ShowDock { get; set; }
        public bool ShowDesktopWidgets { get; set; }
        public bool HideWindowsTaskbar { get; set; }
        public bool DockAutoHide { get; set; }
        public bool InterceptWinKey { get; set; }
        public bool IsDarkMode { get; set; }
        public double DockIconSize { get; set; }
        public double DockZoomRange { get; set; }
        public string QuickNoteText { get; set; }
        public List<string> FavoriteApps { get; set; }
        public List<string> HiddenApps { get; set; }
        public List<CustomAppConfig> CustomApps { get; set; }
        public List<DockItemConfig> PinnedDockItems { get; set; }

        public LauncherConfig()
        {
            ShowTopBar = true;
            ShowDock = true;
            ShowDesktopWidgets = true;
            HideWindowsTaskbar = true;
            DockAutoHide = true;
            InterceptWinKey = true;
            IsDarkMode = true;
            DockIconSize = 50.0;
            DockZoomRange = 26.0;
            QuickNoteText = "• Win — Все приложения\r\n• Alt+Пробел — Командный поиск\r\n• ПКМ по иконкам — Настройка Dock";
            FavoriteApps = new List<string>();
            HiddenApps = new List<string>();
            CustomApps = new List<CustomAppConfig>();
            PinnedDockItems = new List<DockItemConfig>();
        }
    }

    public class DockAppItem
    {
        public string Id;
        public string Name;
        public string IconFile;
        public string LaunchCommand;
        public string LaunchArgs;
        public string[] ProcessNames;
        public bool IsPinned;
        public bool IsSeparator;
        public bool IsTrash;
        public bool IsAppsLauncher;
        public bool IsDesktopFolder;
        public bool IsExplorer;
        public bool IsWindowSwitcher;
        public bool IsDynamic;
        public ImageSource CustomImageSource;

        public IntPtr ActiveHwnd;
        public bool IsRunning;

        // UI & physics state
        public Border RootContainer;
        public Grid IconWrapper;
        public Image IconImage;
        public Border RunningDot;
        public double CurrentSize = 50.0;
        public double TargetSize = 50.0;
        public double CurrentLift = 0.0;
        public double TargetLift = 0.0;
        public double BounceStartTime = -100.0;
    }

    public class AppCatalogEntry
    {
        public string Title;
        public string Cmd;
        public string Args;
        public string VerifyPath;
        public string Category;
        public ImageSource IconSource;
    }

    public class OpenWindowEntry
    {
        public IntPtr Hwnd;
        public uint Pid;
        public string ProcessName;
        public string AppTitle;
        public string WindowTitle;
        public bool IsMinimized;
        public ImageSource Icon;
    }

    // =========================================================================
    // CORE SHELL & LAUNCHER CONTROLLER
    // =========================================================================
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
        public AboutSystemWindow AboutWindow;
        public Window ScreenDimOverlay;

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
            else if (Directory.Exists(@"C:\Win11_Prep\macos"))
            {
                BaseDir = @"C:\Win11_Prep\macos";
            }
            else
            {
                BaseDir = exeDir;
            }

            IconsDir = IOPath.Combine(BaseDir, "icons_png");
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
            _catalogVerifyTimer.Interval = TimeSpan.FromSeconds(8);
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
            TopBar = new TopBarWindow();
            Dock = new DockWindow();
            DesktopWidgets = new DesktopWidgetsWindow();
            Spotlight = new SpotlightWindow();
            Launchpad = new LaunchpadWindow();
            WindowSwitcher = new WindowSwitcherWindow();
            ControlCenter = new ControlCenterWindow();
            CalendarPopup = new CalendarWidgetWindow();

            ApplyModuleVisibility();

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

        public void ShutdownAndRestoreWindows()
        {
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
                NativeMethods.SystemParametersInfoRect(0x002F, 0, ref fullRect, 0x0002);
            }
            catch { }
            Application.Current.Shutdown();
        }

        public void CloseAllPopups()
        {
            if (ControlCenter != null && ControlCenter.IsVisible) ControlCenter.Hide();
            if (CalendarPopup != null && CalendarPopup.IsVisible) CalendarPopup.Hide();
            if (Spotlight != null && Spotlight.IsVisible) Spotlight.Hide();
            if (Launchpad != null && Launchpad.IsVisible) Launchpad.Hide();
            if (WindowSwitcher != null && WindowSwitcher.IsVisible) WindowSwitcher.Hide();
        }

        public void ToggleSpotlight()
        {
            if (Spotlight == null) Spotlight = new SpotlightWindow();
            if (Spotlight.IsVisible)
            {
                Spotlight.Hide();
            }
            else
            {
                CloseAllPopups();
                Spotlight.ShowAndFocus();
            }
        }

        public void ToggleLaunchpad()
        {
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
                WindowSwitcher.Hide();
            }
            else
            {
                CloseAllPopups();
                WindowSwitcher.ShowAndRefresh();
            }
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

        public void ToggleControlCenter()
        {
            if (ControlCenter == null) ControlCenter = new ControlCenterWindow();
            if (ControlCenter.IsVisible)
            {
                ControlCenter.Hide();
            }
            else
            {
                if (CalendarPopup != null && CalendarPopup.IsVisible) CalendarPopup.Hide();
                if (Spotlight != null && Spotlight.IsVisible) Spotlight.Hide();
                if (WindowSwitcher != null && WindowSwitcher.IsVisible) WindowSwitcher.Hide();
                ControlCenter.RefreshState();
                ControlCenter.Left = SystemParameters.PrimaryScreenWidth - ControlCenter.Width - 12;
                ControlCenter.Top = Config.ShowTopBar ? 34 : 12;
                ControlCenter.Show();
                ControlCenter.Activate();
            }
        }

        public void ToggleCalendarPopup()
        {
            if (CalendarPopup == null) CalendarPopup = new CalendarWidgetWindow();
            if (CalendarPopup.IsVisible)
            {
                CalendarPopup.Hide();
            }
            else
            {
                if (ControlCenter != null && ControlCenter.IsVisible) ControlCenter.Hide();
                if (Spotlight != null && Spotlight.IsVisible) Spotlight.Hide();
                if (WindowSwitcher != null && WindowSwitcher.IsVisible) WindowSwitcher.Hide();
                CalendarPopup.RefreshState();
                CalendarPopup.Left = SystemParameters.PrimaryScreenWidth - CalendarPopup.Width - 12;
                CalendarPopup.Top = Config.ShowTopBar ? 34 : 12;
                CalendarPopup.Show();
                CalendarPopup.Activate();
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
                if (NativeMethods.GetWindowText(hWnd, sbTitle, sbTitle.Capacity) <= 0) return true;
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

            if (item.ActiveHwnd != IntPtr.Zero && NativeMethods.IsWindowVisible(item.ActiveHwnd))
            {
                IntPtr fg = NativeMethods.GetForegroundWindow();
                if (fg == item.ActiveHwnd && !NativeMethods.IsIconic(item.ActiveHwnd))
                {
                    NativeMethods.ShowWindow(item.ActiveHwnd, NativeMethods.SW_SHOWMINIMIZED);
                }
                else
                {
                    if (NativeMethods.IsIconic(item.ActiveHwnd))
                    {
                        NativeMethods.ShowWindow(item.ActiveHwnd, NativeMethods.SW_RESTORE);
                    }
                    NativeMethods.SwitchToThisWindow(item.ActiveHwnd, true);
                    NativeMethods.SetForegroundWindow(item.ActiveHwnd);
                }
                return;
            }

            RunProcess(item.LaunchCommand, item.LaunchArgs);
        }

        public static void RunProcess(string cmd, string args)
        {
            RunProcessEx(cmd, args, false);
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

    public static class LiquidGlassTheme
    {
        public static readonly FontFamily UiFont = new FontFamily("-apple-system, SF Pro Display, SF Pro Text, Segoe UI Variable Display, Segoe UI");

        public static LinearGradientBrush CreateGlassBrush(byte alphaTop, byte alphaBottom)
        {
            LinearGradientBrush b = new LinearGradientBrush();
            b.StartPoint = new Point(0, 0);
            b.EndPoint = new Point(1, 1);
            b.GradientStops.Add(new GradientStop(Color.FromArgb(alphaTop, 40, 40, 52), 0.0));
            b.GradientStops.Add(new GradientStop(Color.FromArgb(alphaBottom, 20, 20, 28), 1.0));
            b.Freeze();
            return b;
        }

        public static LinearGradientBrush CreateSpecularBorderBrush()
        {
            LinearGradientBrush b = new LinearGradientBrush();
            b.StartPoint = new Point(0, 0);
            b.EndPoint = new Point(0, 1);
            b.GradientStops.Add(new GradientStop(Color.FromArgb(118, 255, 255, 255), 0.0));
            b.GradientStops.Add(new GradientStop(Color.FromArgb(40, 255, 255, 255), 1.0));
            b.Freeze();
            return b;
        }

        public static Brush AccentBlueBrush
        {
            get
            {
                SolidColorBrush b = new SolidColorBrush(Color.FromRgb(10, 132, 255));
                b.Freeze();
                return b;
            }
        }
    }

    // =========================================================================
    // 1. TOP MENU BAR WINDOW (Tahoe Liquid Glass)
    // =========================================================================
    public class TopBarWindow : Window
    {
        private TextBlock _activeAppText;
        private TextBlock _clockText;
        private TextBlock _langText;
        private TextBlock _cpuMemText;
        private Popup _activeMenuPopup;
        private bool _appBarRegistered = false;
        private uint _appBarCallbackMsg = 0;
        private DispatcherTimer _pollTimer;
        private IntPtr _kbHookHandle = IntPtr.Zero;
        private NativeMethods.LowLevelKeyboardProc _kbHookProc;
        private bool _winKeyDown = false;
        private bool _otherKeyWhileWin = false;

        public TopBarWindow()
        {
            Title = "Tahoe Menu Bar";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = 28;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();

            SourceInitialized += OnSourceInitialized;
            Closed += OnClosed;

            _pollTimer = new DispatcherTimer();
            _pollTimer.Interval = TimeSpan.FromMilliseconds(200);
            _pollTimer.Tick += OnPollTick;
            _pollTimer.Start();
            OnPollTick(null, EventArgs.Empty);
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            NativeMethods.HideNativeTaskbars();
            NativeMethods.MakeToolWindow(this, true);
            NativeMethods.EnableBlur(this, 0x9916161E);
            RegisterAppBar();
            EnforceWorkArea();

            WindowInteropHelper helper = new WindowInteropHelper(this);
            HwndSource src = HwndSource.FromHwnd(helper.Handle);
            if (src != null) src.AddHook(WndProc);
            // Register Alt+Space (9001) for Quick Search, Win+Shift+A (9002) for Settings Hub
            NativeMethods.RegisterHotKey(helper.Handle, 9001, 0x0001, 0x20);
            NativeMethods.RegisterHotKey(helper.Handle, 9002, 0x0008 | 0x0004, 0x41);

            _kbHookProc = new NativeMethods.LowLevelKeyboardProc(KeyboardHookCallback);
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _kbHookHandle = NativeMethods.SetWindowsHookEx(13, _kbHookProc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && ShellController.Instance != null && ShellController.Instance.Config.InterceptWinKey)
            {
                int msg = wParam.ToInt32();
                int vkCode = Marshal.ReadInt32(lParam);
                int flags = Marshal.ReadInt32(lParam, 8);
                bool injected = (flags & 0x10) != 0;

                if (!injected)
                {
                    bool isWinKey = (vkCode == 0x5B || vkCode == 0x5C);
                    bool isKeyDown = (msg == 0x0100 || msg == 0x0104);
                    bool isKeyUp = (msg == 0x0101 || msg == 0x0105);

                    if (isWinKey)
                    {
                        if (isKeyDown)
                        {
                            if (!_winKeyDown)
                            {
                                _winKeyDown = true;
                                _otherKeyWhileWin = false;
                            }
                        }
                        else if (isKeyUp)
                        {
                            bool soloTap = (_winKeyDown && !_otherKeyWhileWin);
                            _winKeyDown = false;
                            _otherKeyWhileWin = false;
                            if (soloTap)
                            {
                                NativeMethods.keybd_event(0xFF, 0, 0, UIntPtr.Zero);
                                NativeMethods.keybd_event(0xFF, 0, 2, UIntPtr.Zero);
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    ShellController.Instance.ToggleLaunchpad();
                                }));
                            }
                        }
                    }
                    else if (isKeyDown && _winKeyDown)
                    {
                        _otherKeyWhileWin = true;
                    }
                }
            }
            return NativeMethods.CallNextHookEx(_kbHookHandle, nCode, wParam, lParam);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312)
            {
                int id = wParam.ToInt32();
                if (id == 9001)
                {
                    ShellController.Instance.ToggleSpotlight();
                    handled = true;
                }
                else if (id == 9002)
                {
                    ShellController.Instance.ShowLauncherSettings();
                    handled = true;
                }
            }
            else if (_appBarCallbackMsg != 0 && (uint)msg == _appBarCallbackMsg)
            {
                if (wParam.ToInt32() == 1)
                {
                    EnforceWorkArea();
                }
            }
            return IntPtr.Zero;
        }

        private void RegisterAppBar()
        {
            try
            {
                WindowInteropHelper helper = new WindowInteropHelper(this);
                if (helper.Handle == IntPtr.Zero) return;
                if (_appBarCallbackMsg == 0)
                {
                    _appBarCallbackMsg = NativeMethods.RegisterWindowMessage("TahoeAppBarCallbackMsg");
                }
                NativeMethods.APPBARDATA abd = new NativeMethods.APPBARDATA();
                abd.cbSize = Marshal.SizeOf(abd);
                abd.hWnd = helper.Handle;
                abd.uCallbackMessage = _appBarCallbackMsg;
                NativeMethods.SHAppBarMessage(NativeMethods.ABM_NEW, ref abd);
                _appBarRegistered = true;
            }
            catch { }
        }

        public void EnforceWorkAreaPublic()
        {
            EnforceWorkArea();
        }

        private void EnforceWorkArea()
        {
            try
            {
                NativeMethods.HideNativeTaskbars();
                WindowInteropHelper helper = new WindowInteropHelper(this);
                if (helper.Handle == IntPtr.Zero) return;

                PresentationSource source = PresentationSource.FromVisual(this);
                double dpiScale = 1.0;
                if (source != null && source.CompositionTarget != null)
                {
                    dpiScale = source.CompositionTarget.TransformToDevice.M11;
                }
                int pixelWidth = (int)Math.Round(SystemParameters.PrimaryScreenWidth * dpiScale);
                int screenPixelHeight = (int)Math.Round(SystemParameters.PrimaryScreenHeight * dpiScale);
                bool showBar = ShellController.Instance == null || ShellController.Instance.Config.ShowTopBar;
                int barPixelHeight = showBar ? (int)Math.Round(28 * dpiScale) : 0;

                if (_appBarRegistered)
                {
                    NativeMethods.APPBARDATA abd = new NativeMethods.APPBARDATA();
                    abd.cbSize = Marshal.SizeOf(abd);
                    abd.hWnd = helper.Handle;
                    abd.uEdge = NativeMethods.ABE_TOP;
                    abd.rc.Left = 0;
                    abd.rc.Top = 0;
                    abd.rc.Right = pixelWidth;
                    abd.rc.Bottom = barPixelHeight;
                    NativeMethods.SHAppBarMessage(NativeMethods.ABM_QUERYPOS, ref abd);
                    abd.rc.Left = 0;
                    abd.rc.Top = 0;
                    abd.rc.Right = pixelWidth;
                    abd.rc.Bottom = barPixelHeight;
                    NativeMethods.SHAppBarMessage(NativeMethods.ABM_SETPOS, ref abd);
                }

                NativeMethods.RECT workRect = new NativeMethods.RECT();
                workRect.Left = 0;
                workRect.Top = barPixelHeight;
                workRect.Right = pixelWidth;
                workRect.Bottom = screenPixelHeight;
                NativeMethods.SystemParametersInfoRect(0x002F, 0, ref workRect, 0x0002);
                NativeMethods.HideNativeTaskbars();
            }
            catch { }
        }

        public void UnregisterAndRestoreWorkArea()
        {
            try
            {
                if (_kbHookHandle != IntPtr.Zero)
                {
                    NativeMethods.UnhookWindowsHookEx(_kbHookHandle);
                    _kbHookHandle = IntPtr.Zero;
                }
                WindowInteropHelper helper = new WindowInteropHelper(this);
                NativeMethods.UnregisterHotKey(helper.Handle, 9001);
                NativeMethods.UnregisterHotKey(helper.Handle, 9002);
                if (_appBarRegistered)
                {
                    NativeMethods.APPBARDATA abd = new NativeMethods.APPBARDATA();
                    abd.cbSize = Marshal.SizeOf(abd);
                    abd.hWnd = helper.Handle;
                    NativeMethods.SHAppBarMessage(NativeMethods.ABM_REMOVE, ref abd);
                    _appBarRegistered = false;
                }
            }
            catch { }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            UnregisterAndRestoreWorkArea();
            NativeMethods.RestoreNativeTaskbars();
        }

        private void BuildUI()
        {
            Border barBorder = new Border();
            barBorder.Background = LiquidGlassTheme.CreateGlassBrush(180, 200);
            barBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));
            barBorder.BorderThickness = new Thickness(0, 0, 0, 1);
            barBorder.Padding = new Thickness(8, 0, 10, 0);

            Grid rootGrid = new Grid();
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel leftStack = new StackPanel();
            leftStack.Orientation = Orientation.Horizontal;
            leftStack.VerticalAlignment = VerticalAlignment.Center;

            Border sysMenuBtn = CreateBarButton(CreateAppleLogoPath(), delegate(object s, MouseButtonEventArgs e)
            {
                ShowMenuDropdown((FrameworkElement)s, BuildSystemMenuItems());
            });
            sysMenuBtn.Padding = new Thickness(9, 3, 9, 3);
            leftStack.Children.Add(sysMenuBtn);

            _activeAppText = new TextBlock();
            _activeAppText.Text = "Рабочий стол";
            _activeAppText.FontWeight = FontWeights.Bold;
            _activeAppText.FontSize = 13;
            _activeAppText.Foreground = Brushes.White;
            _activeAppText.VerticalAlignment = VerticalAlignment.Center;
            Border appBtn = CreateBarButton(_activeAppText, delegate(object s, MouseButtonEventArgs e)
            {
                ShowMenuDropdown((FrameworkElement)s, BuildActiveAppMenuItems());
            });
            leftStack.Children.Add(appBtn);

            leftStack.Children.Add(CreateTextMenuButton("Файл", BuildFileMenuItems));
            leftStack.Children.Add(CreateTextMenuButton("Правка", BuildEditMenuItems));
            leftStack.Children.Add(CreateTextMenuButton("Вид", BuildViewMenuItems));
            leftStack.Children.Add(CreateTextMenuButton("Переход", BuildGoMenuItems));
            leftStack.Children.Add(CreateTextMenuButton("Окно", BuildWindowMenuItems));
            leftStack.Children.Add(CreateTextMenuButton("Справка", BuildHelpMenuItems));

            Grid.SetColumn(leftStack, 0);
            rootGrid.Children.Add(leftStack);

            StackPanel rightStack = new StackPanel();
            rightStack.Orientation = Orientation.Horizontal;
            rightStack.VerticalAlignment = VerticalAlignment.Center;

            _cpuMemText = new TextBlock();
            _cpuMemText.Text = "ОЗУ 42%";
            _cpuMemText.FontSize = 11.5;
            _cpuMemText.FontWeight = FontWeights.Medium;
            _cpuMemText.Foreground = new SolidColorBrush(Color.FromArgb(220, 245, 245, 247));
            _cpuMemText.VerticalAlignment = VerticalAlignment.Center;
            Border sysBtn = CreateBarButton(_cpuMemText, delegate(object s, MouseButtonEventArgs e)
            {
                ShellController.RunProcess("taskmgr.exe", "");
            });
            sysBtn.ToolTip = "Открыть Диспетчер задач";
            rightStack.Children.Add(sysBtn);

            Border langBadge = new Border();
            langBadge.BorderBrush = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255));
            langBadge.BorderThickness = new Thickness(1);
            langBadge.CornerRadius = new CornerRadius(3.5);
            langBadge.Padding = new Thickness(4, 0.5, 4, 0.5);
            langBadge.VerticalAlignment = VerticalAlignment.Center;
            _langText = new TextBlock();
            _langText.Text = "RU";
            _langText.FontSize = 10.5;
            _langText.FontWeight = FontWeights.Bold;
            _langText.Foreground = Brushes.White;
            langBadge.Child = _langText;

            Border langBtn = CreateBarButton(langBadge, delegate(object s, MouseButtonEventArgs e)
            {
                SwitchKeyboardLayout();
            });
            langBtn.ToolTip = "Переключить раскладку клавиатуры";
            rightStack.Children.Add(langBtn);

            Border wifiBtn = CreateBarButton(CreateVectorIcon("M1,6 C5.5,2 12.5,2 17,6 M3.8,9.2 C7,6.4 11,6.4 14.2,9.2 M6.8,12.2 C8.2,11 9.8,11 11.2,12.2 M9,15 A1,1 0 1,1 9.01,15", 16, 15, true),
                delegate(object s, MouseButtonEventArgs e)
                {
                    ShellController.Instance.ToggleControlCenter();
                });
            wifiBtn.ToolTip = "Сеть и Интернет";
            rightStack.Children.Add(wifiBtn);

            Border soundBtn = CreateBarButton(CreateVectorIcon("M2,6 L5,6 L9,2.5 L9,13.5 L5,10 L2,10 Z M11.5,5 C12.8,6.5 12.8,9.5 11.5,11 M13.8,3 C16,5.8 16,10.2 13.8,13", 16, 15, true),
                delegate(object s, MouseButtonEventArgs e)
                {
                    ShellController.Instance.ToggleControlCenter();
                });
            soundBtn.ToolTip = "Звук и мультимедиа";
            rightStack.Children.Add(soundBtn);

            Border searchBtn = CreateBarButton(CreateVectorIcon("M10.5,6.5 A4.5,4.5 0 1,1 1.5,6.5 A4.5,4.5 0 1,1 10.5,6.5 M9.8,9.8 L14,14", 15, 15, true),
                delegate(object s, MouseButtonEventArgs e)
                {
                    ShellController.Instance.ToggleSpotlight();
                });
            searchBtn.ToolTip = "Быстрый поиск (Alt+Пробел)";
            rightStack.Children.Add(searchBtn);

            Border ccBtn = CreateBarButton(CreateControlCenterIcon(),
                delegate(object s, MouseButtonEventArgs e)
                {
                    ShellController.Instance.ToggleControlCenter();
                });
            ccBtn.ToolTip = "Пункт управления";
            rightStack.Children.Add(ccBtn);

            _clockText = new TextBlock();
            _clockText.Text = "Вс, 8 нояб.  15:00";
            _clockText.FontSize = 12.5;
            _clockText.FontWeight = FontWeights.Medium;
            _clockText.Foreground = Brushes.White;
            _clockText.VerticalAlignment = VerticalAlignment.Center;
            Border clockBtn = CreateBarButton(_clockText, delegate(object s, MouseButtonEventArgs e)
            {
                ShellController.Instance.ToggleCalendarPopup();
            });
            clockBtn.Padding = new Thickness(8, 3, 6, 3);
            clockBtn.ToolTip = "Календарь";
            rightStack.Children.Add(clockBtn);

            Grid.SetColumn(rightStack, 2);
            rootGrid.Children.Add(rightStack);

            barBorder.Child = rootGrid;
            Content = barBorder;
        }

        private UIElement CreateAppleLogoPath()
        {
            WpfPath p = new WpfPath();
            p.Data = Geometry.Parse("M 11.1,5.2 C 10.4,5.2 9.4,5.8 8.9,5.8 C 8.4,5.8 7.5,5.2 6.6,5.2 C 5.1,5.2 3.5,6.4 3.5,8.9 C 3.5,11.6 5.6,14.8 7.0,14.8 C 7.7,14.8 8.1,14.3 9.0,14.3 C 9.9,14.3 10.2,14.8 11.0,14.8 C 12.4,14.8 13.7,12.5 14.1,11.3 C 12.3,10.5 12.0,7.7 13.9,6.6 C 13.2,5.6 12.1,5.2 11.1,5.2 Z M 10.6,2.2 C 9.6,2.3 8.5,3.2 8.7,4.6 C 9.8,4.7 10.9,3.8 10.6,2.2 Z");
            p.Fill = Brushes.White;
            p.Stretch = Stretch.Uniform;
            p.Width = 13;
            p.Height = 15;
            p.VerticalAlignment = VerticalAlignment.Center;
            return p;
        }

        private UIElement CreateVectorIcon(string pathData, double w, double h, bool strokeOnly)
        {
            WpfPath p = new WpfPath();
            p.Data = Geometry.Parse(pathData);
            if (strokeOnly)
            {
                p.Stroke = Brushes.White;
                p.StrokeThickness = 1.5;
                p.StrokeStartLineCap = PenLineCap.Round;
                p.StrokeEndLineCap = PenLineCap.Round;
            }
            else
            {
                p.Fill = Brushes.White;
            }
            p.Width = w;
            p.Height = h;
            p.Stretch = Stretch.Uniform;
            p.VerticalAlignment = VerticalAlignment.Center;
            return p;
        }

        private UIElement CreateControlCenterIcon()
        {
            Canvas c = new Canvas();
            c.Width = 16;
            c.Height = 14;
            c.VerticalAlignment = VerticalAlignment.Center;

            Rectangle r1 = new Rectangle { Width = 15, Height = 5.5, RadiusX = 2.75, RadiusY = 2.75, Stroke = Brushes.White, StrokeThickness = 1.2 };
            Canvas.SetLeft(r1, 0.5);
            Canvas.SetTop(r1, 1);
            Ellipse e1 = new Ellipse { Width = 4.5, Height = 4.5, Fill = Brushes.White };
            Canvas.SetLeft(e1, 10.2);
            Canvas.SetTop(e1, 1.5);

            Rectangle r2 = new Rectangle { Width = 15, Height = 5.5, RadiusX = 2.75, RadiusY = 2.75, Stroke = Brushes.White, StrokeThickness = 1.2 };
            Canvas.SetLeft(r2, 0.5);
            Canvas.SetTop(r2, 7.8);
            Ellipse e2 = new Ellipse { Width = 4.5, Height = 4.5, Fill = Brushes.White };
            Canvas.SetLeft(e2, 1.3);
            Canvas.SetTop(e2, 8.3);

            c.Children.Add(r1);
            c.Children.Add(e1);
            c.Children.Add(r2);
            c.Children.Add(e2);
            return c;
        }

        private Border CreateTextMenuButton(string text, Func<List<FrameworkElement>> menuBuilder)
        {
            TextBlock tb = new TextBlock();
            tb.Text = text;
            tb.FontSize = 13;
            tb.FontWeight = FontWeights.Medium;
            tb.Foreground = new SolidColorBrush(Color.FromArgb(238, 255, 255, 255));
            tb.VerticalAlignment = VerticalAlignment.Center;
            return CreateBarButton(tb, delegate(object s, MouseButtonEventArgs e)
            {
                ShowMenuDropdown((FrameworkElement)s, menuBuilder());
            });
        }

        private Border CreateBarButton(UIElement child, MouseButtonEventHandler onClick)
        {
            Border b = new Border();
            b.CornerRadius = new CornerRadius(5);
            b.Padding = new Thickness(7, 3, 7, 3);
            b.Margin = new Thickness(1, 2, 1, 2);
            b.Background = Brushes.Transparent;
            b.Cursor = Cursors.Arrow;
            b.Child = child;

            b.MouseEnter += delegate(object s, MouseEventArgs e)
            {
                b.Background = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255));
            };
            b.MouseLeave += delegate(object s, MouseEventArgs e)
            {
                b.Background = Brushes.Transparent;
            };
            b.MouseLeftButtonDown += onClick;
            return b;
        }

        private void ShowMenuDropdown(FrameworkElement anchor, List<FrameworkElement> items)
        {
            if (_activeMenuPopup != null && _activeMenuPopup.IsOpen)
            {
                _activeMenuPopup.IsOpen = false;
                if (_activeMenuPopup.PlacementTarget == anchor)
                {
                    _activeMenuPopup = null;
                    return;
                }
            }

            Popup popup = new Popup();
            popup.AllowsTransparency = true;
            popup.PlacementTarget = anchor;
            popup.Placement = PlacementMode.Bottom;
            popup.VerticalOffset = 3;
            popup.StaysOpen = false;
            popup.PopupAnimation = PopupAnimation.Fade;

            Border outer = new Border();
            outer.Margin = new Thickness(10, 4, 10, 14);
            outer.Background = LiquidGlassTheme.CreateGlassBrush(234, 246);
            outer.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush();
            outer.BorderThickness = new Thickness(1);
            outer.CornerRadius = new CornerRadius(12);
            outer.Padding = new Thickness(5);
            outer.MinWidth = 245;
            outer.Effect = new DropShadowEffect
            {
                BlurRadius = 22,
                ShadowDepth = 6,
                Opacity = 0.55,
                Color = Colors.Black
            };

            StackPanel stack = new StackPanel();
            foreach (FrameworkElement el in items)
            {
                stack.Children.Add(el);
            }
            outer.Child = stack;
            popup.Child = outer;

            _activeMenuPopup = popup;
            popup.IsOpen = true;
        }

        private FrameworkElement CreateDropdownItem(string label, string shortcut, Action action)
        {
            Border itemBorder = new Border();
            itemBorder.CornerRadius = new CornerRadius(6);
            itemBorder.Padding = new Thickness(10, 5, 10, 5);
            itemBorder.Background = Brushes.Transparent;

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock lbl = new TextBlock
            {
                Text = label,
                FontSize = 13,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lbl, 0);
            g.Children.Add(lbl);

            if (!string.IsNullOrEmpty(shortcut))
            {
                TextBlock sc = new TextBlock
                {
                    Text = shortcut,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromArgb(145, 235, 235, 245)),
                    Margin = new Thickness(20, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(sc, 1);
                g.Children.Add(sc);
            }

            itemBorder.Child = g;

            itemBorder.MouseEnter += delegate(object s, MouseEventArgs e)
            {
                itemBorder.Background = LiquidGlassTheme.AccentBlueBrush;
            };
            itemBorder.MouseLeave += delegate(object s, MouseEventArgs e)
            {
                itemBorder.Background = Brushes.Transparent;
            };
            itemBorder.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (_activeMenuPopup != null) _activeMenuPopup.IsOpen = false;
                if (action != null) action();
            };
            return itemBorder;
        }

        private FrameworkElement CreateDropdownSeparator()
        {
            Border sep = new Border();
            sep.Height = 1;
            sep.Background = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255));
            sep.Margin = new Thickness(8, 4, 8, 4);
            return sep;
        }

        private List<FrameworkElement> BuildSystemMenuItems()
        {
            List<FrameworkElement> list = new List<FrameworkElement>();
            list.Add(CreateDropdownItem("О системе", "", delegate() { ShellController.Instance.ShowAboutSystem(); }));
            list.Add(CreateDropdownItem("Настройки лаунчера...", "Win+Shift+A", delegate() { ShellController.Instance.ShowLauncherSettings(); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Параметры Windows...", "Win+I", delegate() { ShellController.RunProcess("explorer.exe", "ms-settings:"); }));
            list.Add(CreateDropdownItem("Microsoft Store...", "", delegate() { ShellController.RunProcess("explorer.exe", "ms-windows-store:"); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Обзор активных окон", "", delegate() { ShellController.Instance.ToggleWindowSwitcher(); }));
            list.Add(CreateDropdownItem("Диспетчер задач...", "Ctrl+Shift+Esc", delegate() { ShellController.RunProcess("taskmgr.exe", ""); }));
            list.Add(CreateDropdownItem("Закрыть активное окно", "Alt+F4", delegate()
            {
                IntPtr h = ShellController.Instance.LastExternalForegroundHwnd;
                if (h != IntPtr.Zero) NativeMethods.PostMessage(h, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Спящий режим", "", delegate() { ShellController.RunProcess("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0"); }));
            list.Add(CreateDropdownItem("Перезагрузка...", "", delegate() { ShellController.RunProcess("shutdown.exe", "/r /t 0"); }));
            list.Add(CreateDropdownItem("Выключение...", "", delegate() { ShellController.RunProcess("shutdown.exe", "/s /t 0"); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Заблокировать экран", "Win+L", delegate() { NativeMethods.LockWorkStation(); }));
            list.Add(CreateDropdownItem("Выход из лаунчера", "", delegate() { ShellController.Instance.ShutdownAndRestoreWindows(); }));
            return list;
        }

        private List<FrameworkElement> BuildActiveAppMenuItems()
        {
            string app = ShellController.Instance.ActiveAppName;
            List<FrameworkElement> list = new List<FrameworkElement>();
            list.Add(CreateDropdownItem("Настройки лаунчера...", "Win+Shift+A", delegate() { ShellController.Instance.ShowLauncherSettings(); }));
            list.Add(CreateDropdownItem("О системе", "", delegate() { ShellController.Instance.ShowAboutSystem(); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Свернуть «" + app + "»", "", delegate()
            {
                IntPtr h = ShellController.Instance.LastExternalForegroundHwnd;
                if (h != IntPtr.Zero) NativeMethods.ShowWindow(h, NativeMethods.SW_SHOWMINIMIZED);
            }));
            list.Add(CreateDropdownItem("Все приложения", "Win", delegate() { ShellController.Instance.ToggleLaunchpad(); }));
            list.Add(CreateDropdownItem("Обзор окон", "", delegate() { ShellController.Instance.ToggleWindowSwitcher(); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Закрыть «" + app + "»", "Alt+F4", delegate()
            {
                IntPtr h = ShellController.Instance.LastExternalForegroundHwnd;
                if (h != IntPtr.Zero) NativeMethods.PostMessage(h, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }));
            return list;
        }

        private List<FrameworkElement> BuildFileMenuItems()
        {
            List<FrameworkElement> list = new List<FrameworkElement>();
            list.Add(CreateDropdownItem("Новое окно Проводника", "Win+E", delegate() { ShellController.RunProcess("explorer.exe", ""); }));
            list.Add(CreateDropdownItem("Открыть Терминал", "", delegate() { ShellController.RunProcess("wt.exe", ""); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Папка «Рабочий стол»", "", delegate()
            {
                ShellController.RunProcess("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            }));
            list.Add(CreateDropdownItem("Скриншот области...", "Win+Shift+S", delegate()
            {
                ShellController.RunProcess("explorer.exe", "ms-screenclip:");
            }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Очистить Корзину...", "", delegate()
            {
                NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null, 0);
            }));
            list.Add(CreateDropdownItem("Закрыть окно", "Alt+F4", delegate()
            {
                IntPtr h = ShellController.Instance.LastExternalForegroundHwnd;
                if (h != IntPtr.Zero) NativeMethods.PostMessage(h, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }));
            return list;
        }

        private void SendShortcutToLastWindow(byte vkWithCtrl)
        {
            IntPtr h = ShellController.Instance.LastExternalForegroundHwnd;
            if (h != IntPtr.Zero) NativeMethods.SetForegroundWindow(h);
            Thread.Sleep(40);
            NativeMethods.keybd_event(0x11, 0, 0, UIntPtr.Zero);
            NativeMethods.keybd_event(vkWithCtrl, 0, 0, UIntPtr.Zero);
            NativeMethods.keybd_event(vkWithCtrl, 0, 2, UIntPtr.Zero);
            NativeMethods.keybd_event(0x11, 0, 2, UIntPtr.Zero);
        }

        private List<FrameworkElement> BuildEditMenuItems()
        {
            List<FrameworkElement> list = new List<FrameworkElement>();
            list.Add(CreateDropdownItem("Отменить", "Ctrl+Z", delegate() { SendShortcutToLastWindow(0x5A); }));
            list.Add(CreateDropdownItem("Повторить", "Ctrl+Y", delegate() { SendShortcutToLastWindow(0x59); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Вырезать", "Ctrl+X", delegate() { SendShortcutToLastWindow(0x58); }));
            list.Add(CreateDropdownItem("Копировать", "Ctrl+C", delegate() { SendShortcutToLastWindow(0x43); }));
            list.Add(CreateDropdownItem("Вставить", "Ctrl+V", delegate() { SendShortcutToLastWindow(0x56); }));
            list.Add(CreateDropdownItem("Выделить всё", "Ctrl+A", delegate() { SendShortcutToLastWindow(0x41); }));
            return list;
        }

        private List<FrameworkElement> BuildViewMenuItems()
        {
            List<FrameworkElement> list = new List<FrameworkElement>();
            list.Add(CreateDropdownItem("Все приложения", "Win", delegate() { ShellController.Instance.ToggleLaunchpad(); }));
            list.Add(CreateDropdownItem("Быстрый поиск", "Alt+Пробел", delegate() { ShellController.Instance.ToggleSpotlight(); }));
            list.Add(CreateDropdownItem("Обзор активных окон", "", delegate() { ShellController.Instance.ToggleWindowSwitcher(); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem(
                ShellController.Instance.IsDarkMode ? "Переключить на Светлую тему" : "Переключить на Тёмную тему",
                "",
                delegate() { ShellController.Instance.ToggleDarkMode(); }));
            list.Add(CreateDropdownItem(
                ShellController.Instance.Config.ShowDesktopWidgets ? "Скрыть виджеты рабочего стола" : "Показать виджеты рабочего стола",
                "",
                delegate()
                {
                    ShellController.Instance.Config.ShowDesktopWidgets = !ShellController.Instance.Config.ShowDesktopWidgets;
                    ShellController.Instance.SaveConfig();
                    ShellController.Instance.ApplyModuleVisibility();
                }));
            list.Add(CreateDropdownItem(
                NativeMethods.AreDesktopIconsVisible() ? "Скрыть значки рабочего стола" : "Показать значки рабочего стола",
                "",
                delegate() { NativeMethods.ToggleDesktopIcons(); }));
            list.Add(CreateDropdownItem(
                ShellController.Instance.DockAutoHide ? "Закрепить Dock (всегда виден)" : "Скрывать Dock в полноэкранных окнах",
                "",
                delegate()
                {
                    ShellController.Instance.DockAutoHide = !ShellController.Instance.DockAutoHide;
                    ShellController.Instance.SaveConfig();
                }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Настройки лаунчера...", "Win+Shift+A", delegate() { ShellController.Instance.ShowLauncherSettings(); }));
            return list;
        }

        private List<FrameworkElement> BuildGoMenuItems()
        {
            List<FrameworkElement> list = new List<FrameworkElement>();
            list.Add(CreateDropdownItem("Рабочий стол", "", delegate()
            {
                ShellController.RunProcess("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            }));
            list.Add(CreateDropdownItem("Загрузки", "", delegate()
            {
                string dl = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                ShellController.RunProcess("explorer.exe", dl);
            }));
            list.Add(CreateDropdownItem("Документы", "", delegate()
            {
                ShellController.RunProcess("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            }));
            list.Add(CreateDropdownItem("Папка пользователя", "", delegate()
            {
                ShellController.RunProcess("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            }));
            list.Add(CreateDropdownItem("Этот компьютер", "", delegate()
            {
                ShellController.RunProcess("explorer.exe", "shell:MyComputerFolder");
            }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Корзина", "", delegate()
            {
                ShellController.RunProcess("explorer.exe", "shell:RecycleBinFolder");
            }));
            return list;
        }

        private List<FrameworkElement> BuildWindowMenuItems()
        {
            List<FrameworkElement> list = new List<FrameworkElement>();
            list.Add(CreateDropdownItem("Обзор всех окон...", "", delegate() { ShellController.Instance.ToggleWindowSwitcher(); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Свернуть окно", "", delegate()
            {
                IntPtr h = ShellController.Instance.LastExternalForegroundHwnd;
                if (h != IntPtr.Zero) NativeMethods.ShowWindow(h, NativeMethods.SW_SHOWMINIMIZED);
            }));
            list.Add(CreateDropdownItem("Развернуть / Восстановить", "", delegate()
            {
                IntPtr h = ShellController.Instance.LastExternalForegroundHwnd;
                if (h != IntPtr.Zero)
                {
                    if (NativeMethods.IsZoomed(h))
                        NativeMethods.ShowWindow(h, NativeMethods.SW_RESTORE);
                    else
                        NativeMethods.ShowWindow(h, NativeMethods.SW_MAXIMIZE);
                }
            }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Пункт управления", "", delegate() { ShellController.Instance.ToggleControlCenter(); }));
            list.Add(CreateDropdownItem("Календарь", "", delegate() { ShellController.Instance.ToggleCalendarPopup(); }));
            return list;
        }

        private List<FrameworkElement> BuildHelpMenuItems()
        {
            List<FrameworkElement> list = new List<FrameworkElement>();
            list.Add(CreateDropdownItem("Командная палитра и поиск...", "Alt+Пробел", delegate() { ShellController.Instance.ToggleSpotlight(); }));
            list.Add(CreateDropdownItem("Настройки лаунчера...", "Win+Shift+A", delegate() { ShellController.Instance.ShowLauncherSettings(); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("О системе", "", delegate() { ShellController.Instance.ShowAboutSystem(); }));
            return list;
        }

        private void SwitchKeyboardLayout()
        {
            IntPtr h = ShellController.Instance.LastExternalForegroundHwnd;
            if (h == IntPtr.Zero) h = NativeMethods.GetForegroundWindow();
            if (h != IntPtr.Zero)
            {
                NativeMethods.PostMessage(h, NativeMethods.WM_INPUTLANGCHANGEREQUEST, (IntPtr)2, IntPtr.Zero);
            }
        }

        private void OnPollTick(object sender, EventArgs e)
        {
            NativeMethods.HideNativeTaskbars();
            if (_appBarRegistered && ShellController.Instance.Config.ShowTopBar && SystemParameters.WorkArea.Top < 26)
            {
                EnforceWorkArea();
            }

            DateTime now = DateTime.Now;
            CultureInfo ru = new CultureInfo("ru-RU");
            string dayName = now.ToString("ddd", ru);
            if (dayName.Length > 0) dayName = char.ToUpper(dayName[0]) + dayName.Substring(1);
            string monthName = now.ToString("MMM", ru).TrimEnd('.');
            _clockText.Text = string.Format("{0}, {1} {2}.  {3:HH:mm}", dayName, now.Day, monthName, now);

            NativeMethods.MEMORYSTATUSEX mem = new NativeMethods.MEMORYSTATUSEX();
            mem.dwLength = (uint)Marshal.SizeOf(mem);
            if (NativeMethods.GlobalMemoryStatusEx(ref mem))
            {
                _cpuMemText.Text = string.Format("ОЗУ {0}%", mem.dwMemoryLoad);
            }

            IntPtr fg = NativeMethods.GetForegroundWindow();
            if (fg != IntPtr.Zero)
            {
                uint pid;
                uint tid = NativeMethods.GetWindowThreadProcessId(fg, out pid);
                IntPtr hkl = NativeMethods.GetKeyboardLayout(tid);
                int langId = hkl.ToInt32() & 0xFFFF;
                _langText.Text = (langId == 0x0419) ? "RU" : "EN";

                if (pid != (uint)Process.GetCurrentProcess().Id)
                {
                    StringBuilder fgCls = new StringBuilder(128);
                    NativeMethods.GetClassName(fg, fgCls, fgCls.Capacity);
                    string clsName = fgCls.ToString();
                    if (clsName != "Shell_TrayWnd" && clsName != "Shell_SecondaryTrayWnd" && clsName != "CEF-OSC-WIDGET")
                    {
                        ShellController.Instance.LastExternalForegroundHwnd = fg;
                        string appName = ResolveFriendlyAppName(fg, pid);
                        ShellController.Instance.ActiveAppName = appName;
                        _activeAppText.Text = appName;
                    }
                }
            }
        }

        private string ResolveFriendlyAppName(IntPtr hwnd, uint pid)
        {
            try
            {
                StringBuilder cls = new StringBuilder(128);
                NativeMethods.GetClassName(hwnd, cls, cls.Capacity);
                string clsName = cls.ToString();
                if (clsName == "Progman" || clsName == "WorkerW" || clsName == "Shell_TrayWnd")
                {
                    return "Рабочий стол";
                }
                if (clsName == "CabinetWClass" || clsName == "ExploreWClass")
                {
                    return "Проводник";
                }

                Process p = Process.GetProcessById((int)pid);
                string pn = p.ProcessName;
                if (string.Equals(pn, "explorer", StringComparison.OrdinalIgnoreCase)) return "Проводник";
                if (string.Equals(pn, "chrome", StringComparison.OrdinalIgnoreCase)) return "Chrome";
                if (string.Equals(pn, "Antigravity", StringComparison.OrdinalIgnoreCase)) return "Antigravity";
                if (string.Equals(pn, "OpenCode", StringComparison.OrdinalIgnoreCase)) return "OpenCode";
                if (string.Equals(pn, "iMe", StringComparison.OrdinalIgnoreCase) || string.Equals(pn, "Telegram", StringComparison.OrdinalIgnoreCase)) return "iMe";
                if (pn.IndexOf("Яндекс", StringComparison.OrdinalIgnoreCase) >= 0) return "Яндекс Музыка";
                if (pn.IndexOf("steam", StringComparison.OrdinalIgnoreCase) >= 0) return "Steam";
                if (string.Equals(pn, "WindowsTerminal", StringComparison.OrdinalIgnoreCase) || string.Equals(pn, "powershell", StringComparison.OrdinalIgnoreCase)) return "Терминал";
                if (string.Equals(pn, "SystemSettings", StringComparison.OrdinalIgnoreCase)) return "Параметры";
                if (string.Equals(pn, "Taskmgr", StringComparison.OrdinalIgnoreCase)) return "Диспетчер задач";
                if (string.Equals(pn, "Code", StringComparison.OrdinalIgnoreCase)) return "VS Code";
                return pn;
            }
            catch
            {
                return "Рабочий стол";
            }
        }
    }
    // =========================================================================
    // 2. DOCK WINDOW (120+ FPS Deterministic Wave Physics, Pin/Unpin/Reorder)
    // =========================================================================
    public class DockWindow : Window
    {
        private Grid _outerRoot;
        private Border _glassShelf;
        private StackPanel _itemsPanel;
        private Border _tooltipPill;
        private TextBlock _tooltipText;
        private TranslateTransform _dockSlideTransform;
        private Popup _dockContextMenuPopup;

        private Stopwatch _stopwatch = Stopwatch.StartNew();
        private double _lastFrameTime = 0.0;
        private double _currentSlideY = 0.0;
        private double _targetSlideY = 0.0;
        private bool _activeWindowIsMaximized = false;
        private bool _dockRevealedByMouse = false;
        private DispatcherTimer _windowScanTimer;

        private const double ItemMarginX = 3.5;

        public double ElapsedSeconds
        {
            get { return _stopwatch.Elapsed.TotalSeconds; }
        }

        private double BaseIconSize
        {
            get { return ShellController.Instance != null ? ShellController.Instance.Config.DockIconSize : 50.0; }
        }

        private double MaxIconZoom
        {
            get { return ShellController.Instance != null ? ShellController.Instance.Config.DockZoomRange : 26.0; }
        }

        public DockWindow()
        {
            Title = "Tahoe Dock";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Background = null;
            FontFamily = LiquidGlassTheme.UiFont;

            Width = Math.Min(1600, SystemParameters.PrimaryScreenWidth - 40);
            Height = 152;
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2.0;
            Top = SystemParameters.PrimaryScreenHeight - Height;

            BuildUI();

            SourceInitialized += delegate(object s, EventArgs e)
            {
                NativeMethods.MakeToolWindow(this, true);
            };

            CompositionTarget.Rendering += OnRenderFrame;

            _windowScanTimer = new DispatcherTimer();
            _windowScanTimer.Interval = TimeSpan.FromMilliseconds(600);
            _windowScanTimer.Tick += OnScanRunningWindows;
            _windowScanTimer.Start();
            OnScanRunningWindows(null, EventArgs.Empty);
        }

        private void BuildUI()
        {
            _outerRoot = new Grid();
            _outerRoot.Background = null;
            _outerRoot.ClipToBounds = false;
            _dockSlideTransform = new TranslateTransform(0, 0);
            _outerRoot.RenderTransform = _dockSlideTransform;

            _glassShelf = new Border();
            _glassShelf.Height = BaseIconSize + 14;
            _glassShelf.HorizontalAlignment = HorizontalAlignment.Center;
            _glassShelf.VerticalAlignment = VerticalAlignment.Bottom;
            _glassShelf.Margin = new Thickness(0, 0, 0, 8);
            _glassShelf.CornerRadius = new CornerRadius(22);
            _glassShelf.Background = LiquidGlassTheme.CreateGlassBrush(168, 196);
            _glassShelf.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush();
            _glassShelf.BorderThickness = new Thickness(1);
            _glassShelf.Effect = new DropShadowEffect
            {
                BlurRadius = 26,
                ShadowDepth = 8,
                Opacity = 0.56,
                Color = Colors.Black
            };
            _outerRoot.Children.Add(_glassShelf);

            _itemsPanel = new StackPanel();
            _itemsPanel.Orientation = Orientation.Horizontal;
            _itemsPanel.HorizontalAlignment = HorizontalAlignment.Center;
            _itemsPanel.VerticalAlignment = VerticalAlignment.Bottom;
            _itemsPanel.Margin = new Thickness(0, 0, 0, 14);
            _itemsPanel.ClipToBounds = false;

            RebuildDockVisuals();
            _outerRoot.Children.Add(_itemsPanel);

            _tooltipPill = new Border();
            _tooltipPill.HorizontalAlignment = HorizontalAlignment.Left;
            _tooltipPill.VerticalAlignment = VerticalAlignment.Bottom;
            _tooltipPill.Background = LiquidGlassTheme.CreateGlassBrush(228, 242);
            _tooltipPill.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush();
            _tooltipPill.BorderThickness = new Thickness(1);
            _tooltipPill.CornerRadius = new CornerRadius(8);
            _tooltipPill.Padding = new Thickness(11, 4, 11, 4);
            _tooltipPill.Visibility = Visibility.Hidden;
            _tooltipPill.IsHitTestVisible = false;
            _tooltipPill.Effect = new DropShadowEffect
            {
                BlurRadius = 14,
                ShadowDepth = 4,
                Opacity = 0.45,
                Color = Colors.Black
            };

            _tooltipText = new TextBlock();
            _tooltipText.Foreground = Brushes.White;
            _tooltipText.FontSize = 12.5;
            _tooltipText.FontWeight = FontWeights.Medium;
            _tooltipPill.Child = _tooltipText;
            _outerRoot.Children.Add(_tooltipPill);

            Content = _outerRoot;
        }

        public void RebuildDockVisuals()
        {
            if (_itemsPanel == null) return;
            _itemsPanel.Children.Clear();
            if (_glassShelf != null) _glassShelf.Height = BaseIconSize + 14;
            foreach (DockAppItem item in ShellController.Instance.DockItems)
            {
                _itemsPanel.Children.Add(CreateDockItemElement(item));
            }
            OnScanRunningWindows(null, EventArgs.Empty);
        }

        private FrameworkElement CreateDockItemElement(DockAppItem item)
        {
            if (item.IsSeparator)
            {
                Border sep = new Border();
                sep.Width = 1;
                sep.Height = Math.Max(28, BaseIconSize - 12);
                sep.VerticalAlignment = VerticalAlignment.Bottom;
                sep.Margin = new Thickness(7, 0, 7, 6);
                sep.Background = new SolidColorBrush(Color.FromArgb(62, 255, 255, 255));
                item.RootContainer = sep;
                return sep;
            }

            Border container = new Border();
            container.Background = Brushes.Transparent;
            container.Margin = new Thickness(ItemMarginX, 0, ItemMarginX, 0);
            container.VerticalAlignment = VerticalAlignment.Bottom;
            container.Cursor = Cursors.Arrow;
            container.ClipToBounds = false;

            Grid wrapper = new Grid();
            wrapper.Width = BaseIconSize;
            wrapper.Height = BaseIconSize;
            wrapper.VerticalAlignment = VerticalAlignment.Bottom;
            wrapper.ClipToBounds = false;

            Image img = new Image();
            img.Source = (item.CustomImageSource != null)
                ? item.CustomImageSource
                : ShellController.Instance.GetIcon(item.IconFile);
            img.Stretch = Stretch.Uniform;
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
            img.Effect = new DropShadowEffect
            {
                BlurRadius = 10,
                ShadowDepth = 4,
                Opacity = 0.42,
                Color = Colors.Black
            };
            wrapper.Children.Add(img);

            Border dot = new Border();
            dot.Width = 4.5;
            dot.Height = 4.5;
            dot.CornerRadius = new CornerRadius(2.5);
            dot.Background = Brushes.White;
            dot.HorizontalAlignment = HorizontalAlignment.Center;
            dot.VerticalAlignment = VerticalAlignment.Bottom;
            dot.Margin = new Thickness(0, 0, 0, -6);
            dot.Visibility = item.IsRunning ? Visibility.Visible : Visibility.Hidden;
            dot.Effect = new DropShadowEffect
            {
                BlurRadius = 6,
                ShadowDepth = 0,
                Opacity = 0.9,
                Color = Colors.White
            };
            wrapper.Children.Add(dot);

            container.Child = wrapper;

            item.RootContainer = container;
            item.IconWrapper = wrapper;
            item.IconImage = img;
            item.RunningDot = dot;
            item.CurrentSize = BaseIconSize;
            item.TargetSize = BaseIconSize;

            container.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                ShellController.Instance.LaunchOrFocusDockItem(item);
            };

            container.MouseRightButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                ShowDockContextMenu(item);
            };

            return container;
        }

        private void ShowDockContextMenu(DockAppItem item)
        {
            if (_dockContextMenuPopup != null && _dockContextMenuPopup.IsOpen)
            {
                _dockContextMenuPopup.IsOpen = false;
            }

            Popup popup = new Popup();
            popup.AllowsTransparency = true;
            popup.PlacementTarget = item.RootContainer;
            popup.Placement = PlacementMode.Top;
            popup.VerticalOffset = -10;
            popup.StaysOpen = false;
            popup.PopupAnimation = PopupAnimation.Fade;

            Border outer = new Border
            {
                Margin = new Thickness(10),
                Background = LiquidGlassTheme.CreateGlassBrush(234, 246),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(5),
                MinWidth = 220,
                Effect = new DropShadowEffect { BlurRadius = 22, ShadowDepth = 6, Opacity = 0.55, Color = Colors.Black }
            };

            StackPanel stack = new StackPanel();
            stack.Children.Add(CreateDockMenuRow("▶  Открыть «" + item.Name + "»", delegate
            {
                popup.IsOpen = false;
                ShellController.Instance.LaunchOrFocusDockItem(item);
            }));

            if (!item.IsAppsLauncher && !item.IsWindowSwitcher && !item.IsTrash)
            {
                stack.Children.Add(CreateDockMenuRow("➕  Новое окно", delegate
                {
                    popup.IsOpen = false;
                    ShellController.RunProcess(item.LaunchCommand, item.LaunchArgs);
                }));

                if (!item.IsDesktopFolder && !item.IsExplorer)
                {
                    stack.Children.Add(CreateDockMenuRow("🛡  Запустить от имени администратора", delegate
                    {
                        popup.IsOpen = false;
                        ShellController.RunProcessEx(item.LaunchCommand, item.LaunchArgs, true);
                    }));
                }

                if (!string.IsNullOrEmpty(item.LaunchCommand) && item.LaunchCommand.IndexOf('\\') >= 0 && File.Exists(item.LaunchCommand))
                {
                    stack.Children.Add(CreateDockMenuRow("📂  Открыть расположение файла", delegate
                    {
                        popup.IsOpen = false;
                        ShellController.OpenFileLocation(item.LaunchCommand);
                    }));
                }
            }

            if (item.IsTrash)
            {
                stack.Children.Add(CreateDockMenuRow("🗑  Очистить Корзину", delegate
                {
                    popup.IsOpen = false;
                    NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null, 0);
                }));
            }

            // Pin / Unpin / Move options
            if (item.IsDynamic)
            {
                stack.Children.Add(CreateDockSeparator());
                stack.Children.Add(CreateDockMenuRow("📌  Закрепить в Dock", delegate
                {
                    popup.IsOpen = false;
                    string exePath = item.LaunchCommand;
                    try
                    {
                        if (item.ActiveHwnd != IntPtr.Zero)
                        {
                            uint pid;
                            NativeMethods.GetWindowThreadProcessId(item.ActiveHwnd, out pid);
                            Process p = Process.GetProcessById((int)pid);
                            exePath = p.MainModule.FileName;
                        }
                    }
                    catch { }
                    ShellController.Instance.PinAppToDock(item.Name, exePath, "");
                }));
            }
            else if (item.IsPinned && !item.IsExplorer && !item.IsAppsLauncher && !item.IsTrash && !item.IsDesktopFolder)
            {
                stack.Children.Add(CreateDockSeparator());
                stack.Children.Add(CreateDockMenuRow("◀  Переместить влево", delegate
                {
                    popup.IsOpen = false;
                    ShellController.Instance.MovePinnedDockItem(item.Id, -1);
                }));
                stack.Children.Add(CreateDockMenuRow("▶  Переместить вправо", delegate
                {
                    popup.IsOpen = false;
                    ShellController.Instance.MovePinnedDockItem(item.Id, 1);
                }));
                stack.Children.Add(CreateDockMenuRow("❌  Открепить из Dock", delegate
                {
                    popup.IsOpen = false;
                    ShellController.Instance.UnpinAppFromDock(item.Id);
                }));
            }

            if (item.ActiveHwnd != IntPtr.Zero)
            {
                stack.Children.Add(CreateDockSeparator());
                stack.Children.Add(CreateDockMenuRow("🗕  Свернуть / Показать", delegate
                {
                    popup.IsOpen = false;
                    ShellController.Instance.LaunchOrFocusDockItem(item);
                }));
                stack.Children.Add(CreateDockMenuRow("✕  Закрыть окно", delegate
                {
                    popup.IsOpen = false;
                    NativeMethods.PostMessage(item.ActiveHwnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }));
            }

            stack.Children.Add(CreateDockSeparator());
            stack.Children.Add(CreateDockMenuRow("⚙  Настройки Dock и лаунчера...", delegate
            {
                popup.IsOpen = false;
                ShellController.Instance.ShowLauncherSettings();
            }));

            outer.Child = stack;
            popup.Child = outer;
            _dockContextMenuPopup = popup;
            popup.IsOpen = true;
        }

        private FrameworkElement CreateDockSeparator()
        {
            return new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Margin = new Thickness(8, 4, 8, 4)
            };
        }

        private FrameworkElement CreateDockMenuRow(string text, Action onClick)
        {
            Border row = new Border
            {
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(10, 5, 10, 5),
                Background = Brushes.Transparent
            };
            row.Child = new TextBlock
            {
                Text = text,
                FontSize = 13,
                Foreground = Brushes.White
            };
            row.MouseEnter += delegate { row.Background = LiquidGlassTheme.AccentBlueBrush; };
            row.MouseLeave += delegate { row.Background = Brushes.Transparent; };
            row.MouseLeftButtonDown += delegate { if (onClick != null) onClick(); };
            return row;
        }

        private void OnRenderFrame(object sender, EventArgs e)
        {
            double now = ElapsedSeconds;
            double dt = now - _lastFrameTime;
            if (dt <= 0.0 || dt > 0.1) dt = 0.016;
            _lastFrameTime = now;

            NativeMethods.POINT pt;
            NativeMethods.GetCursorPos(out pt);

            PresentationSource src = PresentationSource.FromVisual(this);
            double dpiX = 1.0;
            double dpiY = 1.0;
            if (src != null && src.CompositionTarget != null)
            {
                dpiX = src.CompositionTarget.TransformToDevice.M11;
                dpiY = src.CompositionTarget.TransformToDevice.M22;
            }

            double cursorScreenX = pt.X / dpiX;
            double cursorScreenY = pt.Y / dpiY;

            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;

            double baseSize = BaseIconSize;
            double maxZoom = MaxIconZoom;

            double unmagnifiedRowWidth = 0.0;
            List<DockAppItem> items = ShellController.Instance.DockItems;
            for (int i = 0; i < items.Count; i++)
            {
                unmagnifiedRowWidth += items[i].IsSeparator ? 15.0 : (baseSize + ItemMarginX * 2.0);
            }
            double rowStartLeft = (screenW - unmagnifiedRowWidth) / 2.0;

            bool cursorWithinHorizontalBounds = (cursorScreenX >= rowStartLeft - 45.0) &&
                                                (cursorScreenX <= rowStartLeft + unmagnifiedRowWidth + 45.0);

            if (cursorScreenY >= screenH - 10.0 && cursorWithinHorizontalBounds)
            {
                _dockRevealedByMouse = true;
            }
            else if (cursorScreenY < screenH - 120.0 || !cursorWithinHorizontalBounds)
            {
                _dockRevealedByMouse = false;
            }

            bool popupOpen = (_dockContextMenuPopup != null && _dockContextMenuPopup.IsOpen);

            if (ShellController.Instance.DockAutoHide && _activeWindowIsMaximized && !_dockRevealedByMouse && !popupOpen)
            {
                _targetSlideY = baseSize + 38.0;
            }
            else
            {
                _targetSlideY = 0.0;
            }

            double slideLerp = 1.0 - Math.Exp(-16.0 * dt);
            _currentSlideY += (_targetSlideY - _currentSlideY) * slideLerp;
            _dockSlideTransform.Y = _currentSlideY;

            bool dockActiveHover = (_currentSlideY < 35.0) &&
                                   cursorWithinHorizontalBounds &&
                                   (cursorScreenY >= screenH - (baseSize + 48.0)) &&
                                   (cursorScreenY <= screenH);

            DockAppItem hoveredClosest = null;
            double closestDist = 9999.0;

            double totalShelfWidth = 22.0;
            double sizeLerp = 1.0 - Math.Exp(-25.0 * dt);
            double curBaseX = rowStartLeft;

            for (int i = 0; i < items.Count; i++)
            {
                DockAppItem item = items[i];
                if (item.IsSeparator)
                {
                    curBaseX += 15.0;
                    totalShelfWidth += 15.0;
                    continue;
                }

                double slotWidth = baseSize + ItemMarginX * 2.0;
                double baseCenterX = curBaseX + (slotWidth / 2.0);
                curBaseX += slotWidth;

                double dx = Math.Abs(cursorScreenX - baseCenterX);

                if (dockActiveHover && dx < 130.0)
                {
                    double norm = dx / 130.0;
                    double wave = (1.0 + Math.Cos(Math.PI * norm)) * 0.5;
                    wave = wave * wave;
                    item.TargetSize = baseSize + maxZoom * wave;
                    item.TargetLift = 9.0 * wave;

                    if (dx < closestDist && dx < 32.0)
                    {
                        closestDist = dx;
                        hoveredClosest = item;
                    }
                }
                else
                {
                    item.TargetSize = baseSize;
                    item.TargetLift = 0.0;
                }

                double bounceOffset = 0.0;
                double bounceElapsed = now - item.BounceStartTime;
                if (bounceElapsed >= 0.0 && bounceElapsed < 1.2)
                {
                    bounceOffset = Math.Abs(Math.Sin(bounceElapsed * Math.PI * 2.4)) * Math.Exp(-bounceElapsed * 1.8) * 24.0;
                }

                item.CurrentSize += (item.TargetSize - item.CurrentSize) * sizeLerp;
                item.CurrentLift += ((item.TargetLift + bounceOffset) - item.CurrentLift) * sizeLerp;

                if (item.IconWrapper != null)
                {
                    item.IconWrapper.Width = item.CurrentSize;
                    item.IconWrapper.Height = item.CurrentSize;
                }
                if (item.IconImage != null)
                {
                    item.IconImage.Margin = new Thickness(0, 0, 0, item.CurrentLift);
                }

                totalShelfWidth += item.CurrentSize + (ItemMarginX * 2.0);
            }

            _glassShelf.Height = baseSize + 14;
            _glassShelf.Width = Math.Max(200.0, totalShelfWidth);

            if (hoveredClosest != null && dockActiveHover && !popupOpen)
            {
                _tooltipText.Text = hoveredClosest.Name;
                _tooltipPill.Visibility = Visibility.Visible;
                _tooltipPill.UpdateLayout();
                Point centerPt = hoveredClosest.RootContainer.TranslatePoint(
                    new Point(hoveredClosest.RootContainer.ActualWidth / 2.0, 0), _outerRoot);
                double pillLeft = centerPt.X - (_tooltipPill.ActualWidth / 2.0);
                double pillBottom = 18.0 + hoveredClosest.CurrentSize + hoveredClosest.CurrentLift;
                _tooltipPill.Margin = new Thickness(pillLeft, 0, 0, pillBottom);
            }
            else
            {
                if (_tooltipPill.Visibility != Visibility.Hidden)
                    _tooltipPill.Visibility = Visibility.Hidden;
            }
        }

        private void OnScanRunningWindows(object sender, EventArgs e)
        {
            try
            {
                Dictionary<string, IntPtr> procWindows = new Dictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, uint> procPids = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, string> procTitles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                IntPtr explorerFolderHwnd = IntPtr.Zero;
                uint selfPid = (uint)Process.GetCurrentProcess().Id;

                IntPtr fg = NativeMethods.GetForegroundWindow();
                bool fgMaximized = false;
                if (fg != IntPtr.Zero)
                {
                    StringBuilder fgCls = new StringBuilder(128);
                    NativeMethods.GetClassName(fg, fgCls, fgCls.Capacity);
                    string c = fgCls.ToString();
                    if (c != "Progman" && c != "WorkerW" && c != "Shell_TrayWnd")
                    {
                        fgMaximized = NativeMethods.IsZoomed(fg);
                    }
                }
                _activeWindowIsMaximized = fgMaximized;

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
                    if (NativeMethods.GetWindowText(hWnd, sbTitle, sbTitle.Capacity) <= 0) return true;
                    string winTitle = sbTitle.ToString();

                    StringBuilder sbClass = new StringBuilder(128);
                    NativeMethods.GetClassName(hWnd, sbClass, sbClass.Capacity);
                    string cls = sbClass.ToString();
                    if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd" ||
                        cls == "Windows.UI.Core.CoreWindow" || cls == "ApplicationFrameWindow" || cls == "CEF-OSC-WIDGET")
                        return true;

                    if (cls == "CabinetWClass" || cls == "ExploreWClass")
                    {
                        if (explorerFolderHwnd == IntPtr.Zero)
                            explorerFolderHwnd = hWnd;
                        return true;
                    }

                    uint pid;
                    NativeMethods.GetWindowThreadProcessId(hWnd, out pid);
                    if (pid == selfPid) return true;

                    try
                    {
                        Process p = Process.GetProcessById((int)pid);
                        string pn = p.ProcessName;
                        if (string.Equals(pn, "seelen-ui", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(pn, "TextInputHost", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(pn, "SearchHost", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(pn, "ShellExperienceHost", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(pn, "StartMenuExperienceHost", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                        if (!procWindows.ContainsKey(pn))
                        {
                            procWindows[pn] = hWnd;
                            procPids[pn] = pid;
                            procTitles[pn] = winTitle;
                        }
                    }
                    catch { }
                    return true;
                }, IntPtr.Zero);

                NativeMethods.SHQUERYRBINFO rbInfo = new NativeMethods.SHQUERYRBINFO();
                rbInfo.cbSize = Marshal.SizeOf(rbInfo);
                bool trashHasItems = false;
                if (NativeMethods.SHQueryRecycleBin(null, ref rbInfo) == 0 && rbInfo.i64NumItems > 0)
                {
                    trashHasItems = true;
                }

                HashSet<string> claimedProcs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                List<DockAppItem> items = ShellController.Instance.DockItems;

                for (int i = 0; i < items.Count; i++)
                {
                    DockAppItem item = items[i];
                    if (item.IsSeparator || item.IsDynamic) continue;
                    if (item.IsTrash)
                    {
                        string targetTrashIcon = trashHasItems ? "trash-full.png" : "trash.png";
                        if (item.IconFile != targetTrashIcon)
                        {
                            item.IconFile = targetTrashIcon;
                            if (item.IconImage != null)
                                item.IconImage.Source = ShellController.Instance.GetIcon(targetTrashIcon);
                        }
                        continue;
                    }
                    if (item.IsExplorer)
                    {
                        item.ActiveHwnd = explorerFolderHwnd;
                        item.IsRunning = (explorerFolderHwnd != IntPtr.Zero);
                        if (item.RunningDot != null)
                        {
                            item.RunningDot.Visibility = item.IsRunning ? Visibility.Visible : Visibility.Hidden;
                        }
                        claimedProcs.Add("explorer");
                        continue;
                    }

                    IntPtr matchedHwnd = IntPtr.Zero;
                    if (item.ProcessNames != null)
                    {
                        foreach (string pn in item.ProcessNames)
                        {
                            claimedProcs.Add(pn);
                            if (matchedHwnd == IntPtr.Zero && procWindows.TryGetValue(pn, out matchedHwnd))
                            {
                            }
                        }
                    }
                    item.ActiveHwnd = matchedHwnd;
                    item.IsRunning = (matchedHwnd != IntPtr.Zero);
                    if (item.RunningDot != null)
                    {
                        item.RunningDot.Visibility = item.IsRunning ? Visibility.Visible : Visibility.Hidden;
                    }
                }

                for (int i = items.Count - 1; i >= 0; i--)
                {
                    DockAppItem dyn = items[i];
                    if (!dyn.IsDynamic) continue;
                    string dynProc = dyn.ProcessNames[0];
                    IntPtr latestHwnd;
                    if (procWindows.TryGetValue(dynProc, out latestHwnd))
                    {
                        dyn.ActiveHwnd = latestHwnd;
                        claimedProcs.Add(dynProc);
                    }
                    else
                    {
                        _itemsPanel.Children.Remove(dyn.RootContainer);
                        items.RemoveAt(i);
                    }
                }

                int sepIndex = -1;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].IsSeparator)
                    {
                        sepIndex = i;
                        break;
                    }
                }
                if (sepIndex < 0) sepIndex = items.Count;

                foreach (KeyValuePair<string, IntPtr> kv in procWindows)
                {
                    string pn = kv.Key;
                    if (claimedProcs.Contains(pn)) continue;
                    if (items.Count >= 20) break;

                    uint pid = procPids[pn];
                    DockAppItem dynItem = new DockAppItem
                    {
                        Id = "dyn_" + pn,
                        Name = pn,
                        IconFile = ShellController.MapProcessToKnownIcon(pn) ?? "finder.png",
                        CustomImageSource = ShellController.Instance.ResolveDynamicIcon(pn, pid),
                        LaunchCommand = pn,
                        LaunchArgs = "",
                        ProcessNames = new string[] { pn },
                        IsPinned = false,
                        IsDynamic = true,
                        IsRunning = true,
                        ActiveHwnd = kv.Value
                    };

                    FrameworkElement elem = CreateDockItemElement(dynItem);
                    items.Insert(sepIndex, dynItem);
                    _itemsPanel.Children.Insert(sepIndex, elem);
                    sepIndex++;
                }
            }
            catch { }
        }
    }

    // =========================================================================
    // 3. DESKTOP WIDGETS LAYER (Настольные виджеты рабочего стола)
    // =========================================================================
    public class DesktopWidgetsWindow : Window
    {
        private TextBlock _timeText;
        private TextBlock _dateText;
        private Border _ramFillBar;
        private TextBlock _ramLabel;
        private Border _diskFillBar;
        private TextBlock _diskLabel;
        private Border _appsFillBar;
        private TextBlock _appsLabel;
        private WrapPanel _favWrap;
        private TextBox _noteBox;
        private DispatcherTimer _timer;

        public DesktopWidgetsWindow()
        {
            Title = "Tahoe Desktop Widgets";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = false;
            ShowInTaskbar = false;
            Width = 340;
            Height = 640;
            Left = SystemParameters.PrimaryScreenWidth - Width - 18;
            Top = 44;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();

            SourceInitialized += delegate
            {
                NativeMethods.MakeToolWindow(this, true);
                NativeMethods.SendWindowToBottom(this);
            };

            Activated += delegate
            {
                NativeMethods.SendWindowToBottom(this);
            };

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Tick += OnTick;
            _timer.Start();
            OnTick(null, EventArgs.Empty);
        }

        private void BuildUI()
        {
            StackPanel root = new StackPanel();

            // WIDGET 1: Clock & System Telemetry Card
            Border clockCard = CreateWidgetCard();
            clockCard.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    try { DragMove(); } catch { }
                }
            };

            StackPanel clockStack = new StackPanel();
            Grid headerRow = new Grid();
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _timeText = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm"),
                FontSize = 38,
                FontWeight = FontWeights.Light,
                Foreground = Brushes.White
            };
            Grid.SetColumn(_timeText, 0);
            headerRow.Children.Add(_timeText);

            Border settingsChip = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255)),
                Padding = new Thickness(9, 4, 9, 4),
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = Cursors.Hand,
                ToolTip = "Открыть Настройки лаунчера"
            };
            settingsChip.Child = new TextBlock { Text = "⚙ Лаунчер", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
            settingsChip.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                ShellController.Instance.ShowLauncherSettings();
            };
            Grid.SetColumn(settingsChip, 1);
            headerRow.Children.Add(settingsChip);
            clockStack.Children.Add(headerRow);

            CultureInfo ru = new CultureInfo("ru-RU");
            _dateText = new TextBlock
            {
                Text = DateTime.Now.ToString("dddd, d MMMM", ru),
                FontSize = 12.5,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromArgb(185, 235, 235, 245)),
                Margin = new Thickness(0, 0, 0, 12)
            };
            clockStack.Children.Add(_dateText);

            clockStack.Children.Add(CreateMetricBar("Память ОЗУ", out _ramLabel, out _ramFillBar, Color.FromRgb(10, 132, 255)));
            clockStack.Children.Add(CreateMetricBar("Диск C:", out _diskLabel, out _diskFillBar, Color.FromRgb(48, 209, 88)));
            clockStack.Children.Add(CreateMetricBar("Активные окна", out _appsLabel, out _appsFillBar, Color.FromRgb(191, 90, 242)));

            clockCard.Child = clockStack;
            root.Children.Add(clockCard);

            // WIDGET 2: Quick Launch / Favorites Card
            Border favCard = CreateWidgetCard();
            StackPanel favStack = new StackPanel();
            Grid favHeader = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            favHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            favHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            favHeader.Children.Add(new TextBlock
            {
                Text = "★ Быстрый запуск",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            });

            TextBlock allAppsLink = new TextBlock
            {
                Text = "Все →",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 210, 255)),
                Cursor = Cursors.Hand
            };
            allAppsLink.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                ShellController.Instance.ToggleLaunchpad();
            };
            Grid.SetColumn(allAppsLink, 1);
            favHeader.Children.Add(allAppsLink);
            favStack.Children.Add(favHeader);

            _favWrap = new WrapPanel();
            favStack.Children.Add(_favWrap);
            favCard.Child = favStack;
            root.Children.Add(favCard);

            // WIDGET 3: Quick Notes Card
            Border noteCard = CreateWidgetCard();
            StackPanel noteStack = new StackPanel();
            noteStack.Children.Add(new TextBlock
            {
                Text = "📝 Заметки рабочего стола",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 6)
            });

            _noteBox = new TextBox
            {
                Text = ShellController.Instance.Config.QuickNoteText ?? "",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 86,
                Background = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Foreground = new SolidColorBrush(Color.FromArgb(235, 255, 255, 255)),
                CaretBrush = Brushes.White,
                FontSize = 12,
                Padding = new Thickness(8, 6, 8, 6)
            };
            _noteBox.TextChanged += delegate
            {
                ShellController.Instance.Config.QuickNoteText = _noteBox.Text;
                ShellController.Instance.SaveConfig();
            };
            noteStack.Children.Add(_noteBox);
            noteCard.Child = noteStack;
            root.Children.Add(noteCard);

            Content = root;
            RefreshFavorites();
        }

        private Border CreateWidgetCard()
        {
            return new Border
            {
                Margin = new Thickness(8, 6, 8, 6),
                CornerRadius = new CornerRadius(20),
                Background = LiquidGlassTheme.CreateGlassBrush(185, 210),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 12, 14, 12),
                Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 6, Opacity = 0.45, Color = Colors.Black }
            };
        }

        private FrameworkElement CreateMetricBar(string title, out TextBlock valBlock, out Border fillBorder, Color accent)
        {
            StackPanel sp = new StackPanel { Margin = new Thickness(0, 4, 0, 4) };
            Grid row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            row.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(190, 235, 235, 245))
            });
            valBlock = new TextBlock
            {
                Text = "0%",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };
            Grid.SetColumn(valBlock, 1);
            row.Children.Add(valBlock);
            sp.Children.Add(row);

            Border track = new Border
            {
                Height = 6,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Margin = new Thickness(0, 3, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            fillBorder = new Border
            {
                Height = 6,
                Width = 80,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(accent),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            track.Child = fillBorder;
            sp.Children.Add(track);
            return sp;
        }

        public void RefreshFavorites()
        {
            if (_favWrap == null || ShellController.Instance == null) return;
            _favWrap.Children.Clear();

            List<string> favs = ShellController.Instance.Config.FavoriteApps;
            List<AppCatalogEntry> catalog = ShellController.Instance.InstalledApps;
            int shown = 0;

            for (int i = 0; i < favs.Count && shown < 6; i++)
            {
                string favTitle = favs[i];
                AppCatalogEntry matched = null;
                for (int j = 0; j < catalog.Count; j++)
                {
                    if (string.Equals(catalog[j].Title, favTitle, StringComparison.OrdinalIgnoreCase) ||
                        catalog[j].Title.IndexOf(favTitle, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matched = catalog[j];
                        break;
                    }
                }
                if (matched == null) continue;

                AppCatalogEntry cap = matched;
                Border chip = new Border
                {
                    Width = 92,
                    Height = 66,
                    Margin = new Thickness(3),
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                    ToolTip = "Открыть " + cap.Title
                };

                StackPanel sp = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Image img = new Image
                {
                    Source = cap.IconSource ?? ShellController.Instance.GetIcon("finder.png"),
                    Width = 30,
                    Height = 30
                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                sp.Children.Add(img);
                sp.Children.Add(new TextBlock
                {
                    Text = cap.Title,
                    FontSize = 10.5,
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 84,
                    Margin = new Thickness(0, 4, 0, 0)
                });
                chip.Child = sp;

                chip.MouseEnter += delegate { chip.Background = new SolidColorBrush(Color.FromArgb(65, 10, 132, 255)); };
                chip.MouseLeave += delegate { chip.Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)); };
                chip.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    ShellController.RunProcess(cap.Cmd, cap.Args);
                };

                _favWrap.Children.Add(chip);
                shown++;
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!IsVisible) return;
            NativeMethods.SendWindowToBottom(this);

            CultureInfo ru = new CultureInfo("ru-RU");
            _timeText.Text = DateTime.Now.ToString("HH:mm");
            _dateText.Text = DateTime.Now.ToString("dddd, d MMMM", ru);

            double trackWidth = 280.0;
            NativeMethods.MEMORYSTATUSEX mem = new NativeMethods.MEMORYSTATUSEX();
            mem.dwLength = (uint)Marshal.SizeOf(mem);
            if (NativeMethods.GlobalMemoryStatusEx(ref mem))
            {
                double usedGb = (mem.ullTotalPhys - mem.ullAvailPhys) / (1024.0 * 1024.0 * 1024.0);
                double totalGb = mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                _ramLabel.Text = string.Format("{0:0.0} / {1:0} ГБ ({2}%)", usedGb, totalGb, mem.dwMemoryLoad);
                _ramFillBar.Width = Math.Max(10, trackWidth * (mem.dwMemoryLoad / 100.0));
            }

            try
            {
                DriveInfo di = new DriveInfo("C");
                if (di.IsReady)
                {
                    double freeGb = di.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    double totalGb = di.TotalSize / (1024.0 * 1024.0 * 1024.0);
                    double usedPct = ((totalGb - freeGb) / totalGb) * 100.0;
                    _diskLabel.Text = string.Format("Свободно {0:0} ГБ", freeGb);
                    _diskFillBar.Width = Math.Max(10, trackWidth * (usedPct / 100.0));
                }
            }
            catch { }

            if (ShellController.Instance != null)
            {
                int winCount = ShellController.Instance.GetOpenApplicationWindows().Count;
                _appsLabel.Text = string.Format("{0} активных", winCount);
                _appsFillBar.Width = Math.Max(12, Math.Min(trackWidth, trackWidth * (winCount / 14.0)));
            }
        }
    }

    // =========================================================================
    // 4. CONTROL CENTER WINDOW (Пункт управления)
    // =========================================================================
    public class ControlCenterWindow : Window
    {
        private Border _darkModeTile;
        private TextBlock _darkModeSub;
        private Border _iconsTile;
        private TextBlock _iconsSub;
        private Border _widgetsTile;
        private TextBlock _widgetsSub;
        private Border _dockHideTile;
        private TextBlock _dockHideSub;

        public ControlCenterWindow()
        {
            Title = "Пункт управления";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Width = 350;
            Height = 490;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();

            Deactivated += delegate(object s, EventArgs e)
            {
                Hide();
            };
        }

        private void BuildUI()
        {
            Border card = new Border();
            card.Margin = new Thickness(12);
            card.CornerRadius = new CornerRadius(24);
            card.Background = LiquidGlassTheme.CreateGlassBrush(226, 240);
            card.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush();
            card.BorderThickness = new Thickness(1);
            card.Padding = new Thickness(14);
            card.Effect = new DropShadowEffect
            {
                BlurRadius = 28,
                ShadowDepth = 8,
                Opacity = 0.6,
                Color = Colors.Black
            };

            StackPanel root = new StackPanel();

            Grid topGrid = new Grid();
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border connCard = CreateGlassTile();
            connCard.Padding = new Thickness(12, 10, 12, 10);
            StackPanel connStack = new StackPanel();
            connStack.Children.Add(CreateConnRow("Wi-Fi", "Подключено", true, delegate() { ShellController.RunProcess("explorer.exe", "ms-settings:network-wifi"); }));
            connStack.Children.Add(CreateConnRow("Bluetooth", "Вкл.", true, delegate() { ShellController.RunProcess("explorer.exe", "ms-settings:bluetooth"); }));
            connStack.Children.Add(CreateConnRow("Лаунчер", "Настройки", true, delegate() { Hide(); ShellController.Instance.ShowLauncherSettings(); }));
            connCard.Child = connStack;
            Grid.SetColumn(connCard, 0);
            topGrid.Children.Add(connCard);

            StackPanel rightCol = new StackPanel();
            _darkModeTile = CreateActionTile("Оформление", "Тёмная тема", true, out _darkModeSub, delegate()
            {
                ShellController.Instance.ToggleDarkMode();
                RefreshState();
            });
            _darkModeTile.Margin = new Thickness(0, 0, 0, 10);
            rightCol.Children.Add(_darkModeTile);

            _iconsTile = CreateActionTile("Значки стола", "Показаны", true, out _iconsSub, delegate()
            {
                NativeMethods.ToggleDesktopIcons();
                RefreshState();
            });
            rightCol.Children.Add(_iconsTile);

            Grid.SetColumn(rightCol, 2);
            topGrid.Children.Add(rightCol);
            root.Children.Add(topGrid);

            _widgetsTile = CreateActionTile("Виджеты рабочего стола", "Включены", true, out _widgetsSub, delegate()
            {
                ShellController.Instance.Config.ShowDesktopWidgets = !ShellController.Instance.Config.ShowDesktopWidgets;
                ShellController.Instance.SaveConfig();
                ShellController.Instance.ApplyModuleVisibility();
                RefreshState();
            });
            _widgetsTile.Margin = new Thickness(0, 10, 0, 0);
            root.Children.Add(_widgetsTile);

            _dockHideTile = CreateActionTile("Режим Dock", "Скрывать в полноэкранных окнах", true, out _dockHideSub, delegate()
            {
                ShellController.Instance.DockAutoHide = !ShellController.Instance.DockAutoHide;
                ShellController.Instance.SaveConfig();
                RefreshState();
            });
            _dockHideTile.Margin = new Thickness(0, 10, 0, 0);
            root.Children.Add(_dockHideTile);

            Border dispCard = CreateGlassTile();
            dispCard.Margin = new Thickness(0, 10, 0, 0);
            dispCard.Padding = new Thickness(12, 10, 12, 10);
            StackPanel dispStack = new StackPanel();
            dispStack.Children.Add(new TextBlock
            {
                Text = "Яркость экрана",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 6)
            });
            Slider brightSlider = new Slider
            {
                Minimum = 25,
                Maximum = 100,
                Value = 100,
                Cursor = Cursors.Hand
            };
            brightSlider.ValueChanged += delegate(object s, RoutedPropertyChangedEventArgs<double> e)
            {
                if (ShellController.Instance != null)
                    ShellController.Instance.SetBrightnessOverlay(e.NewValue);
            };
            dispStack.Children.Add(brightSlider);
            dispCard.Child = dispStack;
            root.Children.Add(dispCard);

            Border soundCard = CreateGlassTile();
            soundCard.Margin = new Thickness(0, 10, 0, 0);
            soundCard.Padding = new Thickness(12, 10, 12, 10);
            StackPanel soundStack = new StackPanel();
            soundStack.Children.Add(new TextBlock
            {
                Text = "Звук и мультимедиа",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 6)
            });

            StackPanel mediaBtns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            mediaBtns.Children.Add(CreateMediaChip("⏮", delegate() { NativeMethods.keybd_event(0xB1, 0, 0, UIntPtr.Zero); }));
            mediaBtns.Children.Add(CreateMediaChip("⏯ Пауза/Пуск", delegate() { NativeMethods.keybd_event(0xB3, 0, 0, UIntPtr.Zero); }));
            mediaBtns.Children.Add(CreateMediaChip("⏭", delegate() { NativeMethods.keybd_event(0xB0, 0, 0, UIntPtr.Zero); }));
            mediaBtns.Children.Add(CreateMediaChip("🔊-", delegate() { NativeMethods.keybd_event(0xAE, 0, 0, UIntPtr.Zero); }));
            mediaBtns.Children.Add(CreateMediaChip("🔊+", delegate() { NativeMethods.keybd_event(0xAF, 0, 0, UIntPtr.Zero); }));
            soundStack.Children.Add(mediaBtns);
            soundCard.Child = soundStack;
            root.Children.Add(soundCard);

            card.Child = root;
            Content = card;
        }

        private Border CreateGlassTile()
        {
            Border b = new Border();
            b.CornerRadius = new CornerRadius(16);
            b.Background = new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));
            b.BorderBrush = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255));
            b.BorderThickness = new Thickness(1);
            return b;
        }

        private FrameworkElement CreateConnRow(string title, string sub, bool active, Action onClick)
        {
            Border row = new Border();
            row.CornerRadius = new CornerRadius(10);
            row.Padding = new Thickness(4, 5, 4, 5);
            row.Background = Brushes.Transparent;
            row.Cursor = Cursors.Hand;

            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            Border dot = new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(13),
                Background = active ? LiquidGlassTheme.AccentBlueBrush : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                Margin = new Thickness(0, 0, 8, 0)
            };
            TextBlock iconTxt = new TextBlock
            {
                Text = title.Substring(0, 1),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            dot.Child = iconTxt;
            sp.Children.Add(dot);

            StackPanel labels = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            labels.Children.Add(new TextBlock { Text = title, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
            labels.Children.Add(new TextBlock { Text = sub, FontSize = 10.5, Foreground = new SolidColorBrush(Color.FromArgb(170, 235, 235, 245)) });
            sp.Children.Add(labels);

            row.Child = sp;
            row.MouseLeftButtonDown += delegate { if (onClick != null) onClick(); };
            return row;
        }

        private Border CreateActionTile(string title, string sub, bool active, out TextBlock subBlock, Action onClick)
        {
            Border tile = CreateGlassTile();
            tile.Padding = new Thickness(12, 9, 12, 9);
            tile.Cursor = Cursors.Hand;

            StackPanel sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = title, FontSize = 12.5, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
            subBlock = new TextBlock { Text = sub, FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(185, 235, 235, 245)), Margin = new Thickness(0, 2, 0, 0) };
            sp.Children.Add(subBlock);
            tile.Child = sp;

            tile.MouseLeftButtonDown += delegate { if (onClick != null) onClick(); };
            return tile;
        }

        private Border CreateMediaChip(string text, Action onClick)
        {
            Border chip = new Border();
            chip.CornerRadius = new CornerRadius(8);
            chip.Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255));
            chip.Padding = new Thickness(9, 5, 9, 5);
            chip.Margin = new Thickness(3, 0, 3, 0);
            chip.Cursor = Cursors.Hand;
            chip.Child = new TextBlock { Text = text, Foreground = Brushes.White, FontSize = 11.5, FontWeight = FontWeights.SemiBold };
            chip.MouseLeftButtonDown += delegate { if (onClick != null) onClick(); };
            return chip;
        }

        public void RefreshState()
        {
            bool dark = ShellController.Instance.IsDarkMode;
            _darkModeSub.Text = dark ? "Тёмная тема" : "Светлая тема";
            _darkModeTile.Background = dark
                ? new SolidColorBrush(Color.FromArgb(115, 10, 132, 255))
                : new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));

            bool iconsVis = NativeMethods.AreDesktopIconsVisible();
            _iconsSub.Text = iconsVis ? "Показаны" : "Скрыты";
            _iconsTile.Background = iconsVis
                ? new SolidColorBrush(Color.FromArgb(115, 10, 132, 255))
                : new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));

            bool widgetsVis = ShellController.Instance.Config.ShowDesktopWidgets;
            _widgetsSub.Text = widgetsVis ? "Показаны на рабочем столе" : "Скрыты";
            _widgetsTile.Background = widgetsVis
                ? new SolidColorBrush(Color.FromArgb(115, 10, 132, 255))
                : new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));

            bool autoHide = ShellController.Instance.DockAutoHide;
            _dockHideSub.Text = autoHide ? "Скрывать в полноэкранных окнах" : "Всегда показывать поверх окон";
            _dockHideTile.Background = autoHide
                ? new SolidColorBrush(Color.FromArgb(115, 10, 132, 255))
                : new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));
        }
    }

    // =========================================================================
    // 5. OMNI-LAUNCHER & COMMAND PALETTE (Alt+Space / Быстрый поиск)
    // =========================================================================
    public class SpotlightResultItem
    {
        public string Title;
        public string Subtitle;
        public string Badge;
        public ImageSource Icon;
        public Action OnExecute;
        public Action OnAdminExecute;
        public string FileLocation;
    }

    public class SpotlightWindow : Window
    {
        private TextBox _searchBox;
        private TextBlock _hintText;
        private StackPanel _resultsPanel;
        private List<SpotlightResultItem> _currentItems = new List<SpotlightResultItem>();
        private List<Border> _resultRows = new List<Border>();
        private int _selectedIndex = 0;

        public SpotlightWindow()
        {
            Title = "Быстрый поиск";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Width = 680;
            Height = 470;
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2.0;
            Top = SystemParameters.PrimaryScreenHeight * 0.20;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();

            Deactivated += delegate { Hide(); };
            PreviewKeyDown += OnWindowPreviewKeyDown;
        }

        private void BuildUI()
        {
            Border card = new Border();
            card.Margin = new Thickness(16);
            card.CornerRadius = new CornerRadius(22);
            card.Background = LiquidGlassTheme.CreateGlassBrush(230, 244);
            card.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush();
            card.BorderThickness = new Thickness(1);
            card.Padding = new Thickness(16);
            card.Effect = new DropShadowEffect
            {
                BlurRadius = 32,
                ShadowDepth = 10,
                Opacity = 0.65,
                Color = Colors.Black
            };

            StackPanel root = new StackPanel();

            Grid searchHeader = new Grid();
            searchHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            searchHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock lens = new TextBlock
            {
                Text = "🔍",
                FontSize = 18,
                Foreground = new SolidColorBrush(Color.FromArgb(190, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 12, 0)
            };
            Grid.SetColumn(lens, 0);
            searchHeader.Children.Add(lens);

            _hintText = new TextBlock
            {
                Text = "Программы, >команды, математика (24*15), ?веб-поиск, $терминал...",
                FontSize = 15,
                Foreground = new SolidColorBrush(Color.FromArgb(120, 235, 235, 245)),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            Grid.SetColumn(_hintText, 1);
            searchHeader.Children.Add(_hintText);

            _searchBox = new TextBox();
            _searchBox.FontSize = 19;
            _searchBox.Foreground = Brushes.White;
            _searchBox.CaretBrush = LiquidGlassTheme.AccentBlueBrush;
            _searchBox.Background = Brushes.Transparent;
            _searchBox.BorderThickness = new Thickness(0);
            _searchBox.VerticalAlignment = VerticalAlignment.Center;
            _searchBox.TextChanged += OnSearchTextChanged;
            Grid.SetColumn(_searchBox, 1);
            searchHeader.Children.Add(_searchBox);

            Border footerHint = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)),
                Padding = new Thickness(8, 3, 8, 3),
                VerticalAlignment = VerticalAlignment.Center
            };
            footerHint.Child = new TextBlock
            {
                Text = "↑↓ выбор • Enter запуск • Tab папка",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 235, 235, 245))
            };
            Grid.SetColumn(footerHint, 2);
            searchHeader.Children.Add(footerHint);

            root.Children.Add(searchHeader);

            Border sep = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Margin = new Thickness(0, 12, 0, 10)
            };
            root.Children.Add(sep);

            _resultsPanel = new StackPanel();
            root.Children.Add(_resultsPanel);

            card.Child = root;
            Content = card;

            PopulateResults("");
        }

        public void ShowAndFocus()
        {
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2.0;
            Top = SystemParameters.PrimaryScreenHeight * 0.20;
            _searchBox.Text = "";
            _hintText.Visibility = Visibility.Visible;
            PopulateResults("");
            Show();
            Activate();
            _searchBox.Focus();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _hintText.Visibility = string.IsNullOrEmpty(_searchBox.Text) ? Visibility.Visible : Visibility.Hidden;
            PopulateResults(_searchBox.Text.Trim());
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Hide();
            }
            else if (e.Key == Key.Down)
            {
                if (_currentItems.Count > 0)
                {
                    e.Handled = true;
                    _selectedIndex = (_selectedIndex + 1) % _currentItems.Count;
                    UpdateSelectionVisuals();
                }
            }
            else if (e.Key == Key.Up)
            {
                if (_currentItems.Count > 0)
                {
                    e.Handled = true;
                    _selectedIndex = (_selectedIndex - 1 + _currentItems.Count) % _currentItems.Count;
                    UpdateSelectionVisuals();
                }
            }
            else if (e.Key == Key.Tab)
            {
                if (_selectedIndex >= 0 && _selectedIndex < _currentItems.Count)
                {
                    string loc = _currentItems[_selectedIndex].FileLocation;
                    if (!string.IsNullOrEmpty(loc))
                    {
                        e.Handled = true;
                        Hide();
                        ShellController.OpenFileLocation(loc);
                    }
                }
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (_selectedIndex >= 0 && _selectedIndex < _currentItems.Count)
                {
                    SpotlightResultItem sel = _currentItems[_selectedIndex];
                    bool ctrlShift = (Keyboard.Modifiers & ModifierKeys.Control) != 0 && (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
                    Hide();
                    if (ctrlShift && sel.OnAdminExecute != null)
                        sel.OnAdminExecute();
                    else if (sel.OnExecute != null)
                        sel.OnExecute();
                }
            }
        }

        private void UpdateSelectionVisuals()
        {
            for (int i = 0; i < _resultRows.Count; i++)
            {
                _resultRows[i].Background = (i == _selectedIndex)
                    ? new SolidColorBrush(Color.FromArgb(125, 10, 132, 255))
                    : Brushes.Transparent;
            }
        }

        private void PopulateResults(string query)
        {
            _resultsPanel.Children.Clear();
            _currentItems.Clear();
            _resultRows.Clear();
            _selectedIndex = 0;

            bool cmdMode = query.StartsWith(">");
            string cleanQuery = cmdMode ? query.Substring(1).Trim() : query;

            // 1. Math expression evaluation
            if (!cmdMode && !string.IsNullOrEmpty(query) && !query.StartsWith("?") && !query.StartsWith("$"))
            {
                try
                {
                    System.Data.DataTable dt = new System.Data.DataTable();
                    object val = dt.Compute(query, "");
                    if (val != null && !(val is DBNull))
                    {
                        double num;
                        if (double.TryParse(val.ToString(), out num))
                        {
                            string resStr = val.ToString();
                            AddResultItem(new SpotlightResultItem
                            {
                                Title = query + " = " + resStr,
                                Subtitle = "Нажмите Enter, чтобы скопировать результат в буфер обмена",
                                Badge = "Калькулятор",
                                Icon = ShellController.Instance.GetIcon("calculator.png"),
                                OnExecute = delegate { try { Clipboard.SetText(resStr); } catch { } }
                            });
                        }
                    }
                }
                catch { }
            }

            // 2. Terminal command ($...)
            if (query.StartsWith("$") && query.Length > 1)
            {
                string termCmd = query.Substring(1).Trim();
                AddResultItem(new SpotlightResultItem
                {
                    Title = "Выполнить в Терминале: " + termCmd,
                    Subtitle = "PowerShell / Windows Terminal",
                    Badge = "Терминал",
                    Icon = ShellController.Instance.GetIcon("terminal.png"),
                    OnExecute = delegate { ShellController.RunProcess("powershell.exe", "-NoExit -Command \"" + termCmd + "\""); }
                });
            }

            // 3. Web search (?...)
            if (query.StartsWith("?") && query.Length > 1)
            {
                string webQ = query.Substring(1).Trim();
                AddResultItem(new SpotlightResultItem
                {
                    Title = "Найти в Google: «" + webQ + "»",
                    Subtitle = "Открыть в браузере по умолчанию",
                    Badge = "Веб-поиск",
                    Icon = ShellController.Instance.GetIcon("chrome.png"),
                    OnExecute = delegate { ShellController.RunProcess("https://www.google.com/search?q=" + Uri.EscapeDataString(webQ), ""); }
                });
            }

            // 4. Built-in Launcher & System Commands
            AddMatchingCommand(cleanQuery, cmdMode, "Настройки лаунчера", "Управление панелями, Dock, виджетами и автозапуском", "settings.png",
                delegate { ShellController.Instance.ShowLauncherSettings(); });
            AddMatchingCommand(cleanQuery, cmdMode, "Все приложения", "Открыть полноэкранный каталог программ (Win)", "launchpad.png",
                delegate { ShellController.Instance.ToggleLaunchpad(); });
            AddMatchingCommand(cleanQuery, cmdMode, "Обзор активных окон", "Показать все открытые окна и переключиться", "mission-control.png",
                delegate { ShellController.Instance.ToggleWindowSwitcher(); });
            AddMatchingCommand(cleanQuery, cmdMode, "Переключить тему (Тёмная / Светлая)", "Смена оформления Windows и обоев 6K", "wallpaper.png",
                delegate { ShellController.Instance.ToggleDarkMode(); });
            AddMatchingCommand(cleanQuery, cmdMode, "Показать / Скрыть виджеты рабочего стола", "Виджеты часов, ресурсов ПК и заметок", "control-center.png",
                delegate
                {
                    ShellController.Instance.Config.ShowDesktopWidgets = !ShellController.Instance.Config.ShowDesktopWidgets;
                    ShellController.Instance.SaveConfig();
                    ShellController.Instance.ApplyModuleVisibility();
                });
            AddMatchingCommand(cleanQuery, cmdMode, "Показать / Скрыть значки рабочего стола", "Переключение видимости ярлыков стола", "desktop.png",
                delegate { NativeMethods.ToggleDesktopIcons(); });
            AddMatchingCommand(cleanQuery, cmdMode, "Очистить Корзину", "Удалить все файлы из Корзины Windows", "trash-full.png",
                delegate { NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null, 0); });
            AddMatchingCommand(cleanQuery, cmdMode, "Выход из лаунчера (вернуть панель Windows)", "Закрыть Tahoe Launcher и вернуть стандартный интерфейс", "activity-monitor.png",
                delegate { ShellController.Instance.ShutdownAndRestoreWindows(); });

            // 5. Installed Applications Search
            if (!cmdMode && !query.StartsWith("?") && !query.StartsWith("$"))
            {
                List<AppCatalogEntry> apps = ShellController.Instance.InstalledApps;
                for (int i = 0; i < apps.Count && _currentItems.Count < 7; i++)
                {
                    AppCatalogEntry item = apps[i];
                    if (!string.IsNullOrEmpty(cleanQuery) &&
                        item.Title.IndexOf(cleanQuery, StringComparison.OrdinalIgnoreCase) < 0 &&
                        IOPath.GetFileNameWithoutExtension(item.Cmd).IndexOf(cleanQuery, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    AppCatalogEntry cap = item;
                    AddResultItem(new SpotlightResultItem
                    {
                        Title = cap.Title,
                        Subtitle = cap.Category + " • " + IOPath.GetFileName(cap.Cmd),
                        Badge = "Приложение",
                        Icon = cap.IconSource ?? ShellController.Instance.GetIcon("finder.png"),
                        FileLocation = cap.Cmd,
                        OnExecute = delegate { ShellController.RunProcess(cap.Cmd, cap.Args); },
                        OnAdminExecute = delegate { ShellController.RunProcessEx(cap.Cmd, cap.Args, true); }
                    });
                }
            }

            // 6. Fallback web search if query didn't match or user wants web search
            if (!string.IsNullOrEmpty(cleanQuery) && _currentItems.Count < 7 && !cmdMode)
            {
                string qCopy = cleanQuery;
                AddResultItem(new SpotlightResultItem
                {
                    Title = "Искать в Интернете: «" + qCopy + "»",
                    Subtitle = "Открыть результаты поиска Google в браузере",
                    Badge = "Интернет",
                    Icon = ShellController.Instance.GetIcon("chrome.png"),
                    OnExecute = delegate { ShellController.RunProcess("https://www.google.com/search?q=" + Uri.EscapeDataString(qCopy), ""); }
                });
            }

            UpdateSelectionVisuals();
        }

        private void AddMatchingCommand(string query, bool cmdMode, string title, string subtitle, string iconFile, Action action)
        {
            if (_currentItems.Count >= 7) return;
            if (!cmdMode && string.IsNullOrEmpty(query)) return;
            if (!string.IsNullOrEmpty(query) &&
                title.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
                subtitle.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                return;

            AddResultItem(new SpotlightResultItem
            {
                Title = title,
                Subtitle = subtitle,
                Badge = "Команда",
                Icon = ShellController.Instance.GetIcon(iconFile),
                OnExecute = action
            });
        }

        private void AddResultItem(SpotlightResultItem item)
        {
            if (_currentItems.Count >= 7) return;
            int idx = _currentItems.Count;
            _currentItems.Add(item);

            Border row = new Border
            {
                CornerRadius = new CornerRadius(11),
                Padding = new Thickness(10, 7, 10, 7),
                Margin = new Thickness(0, 1, 0, 1),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Image icon = new Image
            {
                Source = item.Icon ?? ShellController.Instance.GetIcon("finder.png"),
                Width = 30,
                Height = 30,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.HighQuality);
            Grid.SetColumn(icon, 0);
            g.Children.Add(icon);

            StackPanel texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            texts.Children.Add(new TextBlock
            {
                Text = item.Title,
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            if (!string.IsNullOrEmpty(item.Subtitle))
            {
                texts.Children.Add(new TextBlock
                {
                    Text = item.Subtitle,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromArgb(165, 235, 235, 245)),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
            }
            Grid.SetColumn(texts, 1);
            g.Children.Add(texts);

            if (!string.IsNullOrEmpty(item.Badge))
            {
                Border badge = new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Background = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255)),
                    Padding = new Thickness(8, 2, 8, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                badge.Child = new TextBlock
                {
                    Text = item.Badge,
                    FontSize = 10.5,
                    Foreground = new SolidColorBrush(Color.FromArgb(200, 235, 235, 245))
                };
                Grid.SetColumn(badge, 2);
                g.Children.Add(badge);
            }

            row.Child = g;

            int capturedIdx = idx;
            SpotlightResultItem capturedItem = item;
            row.MouseEnter += delegate
            {
                _selectedIndex = capturedIdx;
                UpdateSelectionVisuals();
            };
            row.MouseLeftButtonDown += delegate
            {
                Hide();
                if (capturedItem.OnExecute != null) capturedItem.OnExecute();
            };

            _resultRows.Add(row);
            _resultsPanel.Children.Add(row);
        }
    }
    // =========================================================================
    // 6. FULLSCREEN APPS LAUNCHER (Приложения — Категории, ПКМ-меню, Живой каталог)
    // =========================================================================
    public class LaunchpadWindow : Window
    {
        private const int ColumnsPerPage = 8;
        private const int RowsPerPage = 4;
        private const int AppsPerPage = ColumnsPerPage * RowsPerPage;

        private UniformGrid _gridPanel;
        private TranslateTransform _gridTranslate;
        private TextBox _filterBox;
        private TextBlock _placeholderText;
        private StackPanel _categoryPanel;
        private StackPanel _dotsPanel;
        private ScaleTransform _scaleTransform;
        private Popup _appContextMenuPopup;

        private string _activeCategory = "Все";
        private int _currentPage = 0;
        private int _totalPages = 1;
        private List<AppCatalogEntry> _filteredApps = new List<AppCatalogEntry>();

        private static readonly string[] Categories = new string[]
        {
            "Все", "★ Избранное", "Игры", "Разработка", "Интернет", "Медиа и Офис", "Система", "Программы"
        };

        public LaunchpadWindow()
        {
            Title = "Приложения";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
            Background = new SolidColorBrush(Color.FromArgb(198, 12, 12, 20));
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();

            SourceInitialized += delegate
            {
                NativeMethods.EnableBlur(this, 0xBB0E0E16);
            };

            PreviewKeyDown += OnPreviewKeyDown;
            PreviewMouseWheel += OnPreviewMouseWheel;
        }

        private void BuildUI()
        {
            Grid root = new Grid();
            root.Background = Brushes.Transparent;
            _scaleTransform = new ScaleTransform(1.0, 1.0, Width / 2.0, Height / 2.0);
            root.RenderTransform = _scaleTransform;

            StackPanel container = new StackPanel();
            container.HorizontalAlignment = HorizontalAlignment.Center;
            container.VerticalAlignment = VerticalAlignment.Top;
            container.Margin = new Thickness(0, 44, 0, 0);

            root.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.OriginalSource == root || e.OriginalSource == container || e.OriginalSource == _gridPanel)
                {
                    HideWithAnimation();
                }
            };

            // Top Header Row: Search Input + Quick Actions
            StackPanel topHeaderRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 14)
            };

            Border searchBorder = new Border
            {
                Width = 360,
                Height = 38,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(48, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 4, 12, 4)
            };

            Grid searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock searchIcon = new TextBlock
            {
                Text = "🔍",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(searchIcon, 0);
            searchGrid.Children.Add(searchIcon);

            _placeholderText = new TextBlock
            {
                Text = "Поиск приложений...",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromArgb(140, 235, 235, 245)),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            Grid.SetColumn(_placeholderText, 1);
            searchGrid.Children.Add(_placeholderText);

            _filterBox = new TextBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                CaretBrush = Brushes.White,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            _filterBox.TextChanged += delegate
            {
                _placeholderText.Visibility = string.IsNullOrEmpty(_filterBox.Text) ? Visibility.Visible : Visibility.Hidden;
                _currentPage = 0;
                RenderAppGrid(_filterBox.Text.Trim(), 0);
            };
            Grid.SetColumn(_filterBox, 1);
            searchGrid.Children.Add(_filterBox);
            searchBorder.Child = searchGrid;
            topHeaderRow.Children.Add(searchBorder);

            Border settingsBtn = CreateHeaderActionButton("⚙ Настройки лаунчера", delegate
            {
                HideWithAnimation();
                ShellController.Instance.ShowLauncherSettings();
            });
            settingsBtn.Margin = new Thickness(12, 0, 0, 0);
            topHeaderRow.Children.Add(settingsBtn);

            Border winOverviewBtn = CreateHeaderActionButton("🗔 Активные окна", delegate
            {
                HideWithAnimation();
                ShellController.Instance.ToggleWindowSwitcher();
            });
            winOverviewBtn.Margin = new Thickness(8, 0, 0, 0);
            topHeaderRow.Children.Add(winOverviewBtn);

            container.Children.Add(topHeaderRow);

            // Category Filter Pills Bar
            _categoryPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 18)
            };
            container.Children.Add(_categoryPanel);
            RebuildCategoryPills();

            // 8x4 UniformGrid
            _gridPanel = new UniformGrid
            {
                Columns = ColumnsPerPage,
                Rows = RowsPerPage,
                Width = Math.Min(1280, Width - 120),
                Height = Math.Min(560, Height - 275),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _gridTranslate = new TranslateTransform(0, 0);
            _gridPanel.RenderTransform = _gridTranslate;
            container.Children.Add(_gridPanel);

            // Page Dots & Navigation Bar
            _dotsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 16, 0, 0)
            };
            container.Children.Add(_dotsPanel);

            root.Children.Add(container);
            Content = root;

            RenderAppGrid("", 0);
        }

        private Border CreateHeaderActionButton(string label, Action onClick)
        {
            Border b = new Border
            {
                Height = 38,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(65, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 0, 14, 0),
                Cursor = Cursors.Hand
            };
            b.Child = new TextBlock
            {
                Text = label,
                FontSize = 12.5,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            b.MouseEnter += delegate { b.Background = new SolidColorBrush(Color.FromArgb(75, 10, 132, 255)); };
            b.MouseLeave += delegate { b.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)); };
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };
            return b;
        }

        private void RebuildCategoryPills()
        {
            if (_categoryPanel == null) return;
            _categoryPanel.Children.Clear();
            for (int i = 0; i < Categories.Length; i++)
            {
                string cat = Categories[i];
                bool active = string.Equals(cat, _activeCategory, StringComparison.OrdinalIgnoreCase);
                Border pill = new Border
                {
                    CornerRadius = new CornerRadius(14),
                    Background = active
                        ? LiquidGlassTheme.AccentBlueBrush
                        : new SolidColorBrush(Color.FromArgb(34, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(active ? (byte)150 : (byte)45, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(14, 5, 14, 5),
                    Margin = new Thickness(4, 0, 4, 0),
                    Cursor = Cursors.Hand
                };
                pill.Child = new TextBlock
                {
                    Text = cat,
                    FontSize = 12.5,
                    FontWeight = active ? FontWeights.SemiBold : FontWeights.Medium,
                    Foreground = Brushes.White
                };
                string capturedCat = cat;
                pill.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    _activeCategory = capturedCat;
                    _currentPage = 0;
                    RebuildCategoryPills();
                    RenderAppGrid(_filterBox.Text.Trim(), 0);
                };
                _categoryPanel.Children.Add(pill);
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                HideWithAnimation();
            }
            else if (e.Key == Key.Enter)
            {
                if (_filteredApps.Count > 0)
                {
                    e.Handled = true;
                    AppCatalogEntry first = _filteredApps[0];
                    HideWithAnimation();
                    ShellController.RunProcess(first.Cmd, first.Args);
                }
            }
            else if (e.Key == Key.PageDown || (e.Key == Key.Right && string.IsNullOrEmpty(_filterBox.Text)))
            {
                if (_currentPage < _totalPages - 1)
                {
                    e.Handled = true;
                    SwitchToPage(_currentPage + 1, 1);
                }
            }
            else if (e.Key == Key.PageUp || (e.Key == Key.Left && string.IsNullOrEmpty(_filterBox.Text)))
            {
                if (_currentPage > 0)
                {
                    e.Handled = true;
                    SwitchToPage(_currentPage - 1, -1);
                }
            }
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0 && _currentPage < _totalPages - 1)
            {
                SwitchToPage(_currentPage + 1, 1);
            }
            else if (e.Delta > 0 && _currentPage > 0)
            {
                SwitchToPage(_currentPage - 1, -1);
            }
        }

        private void SwitchToPage(int newPage, int direction)
        {
            if (newPage < 0 || newPage >= _totalPages || newPage == _currentPage) return;
            _currentPage = newPage;
            RenderAppGrid(_filterBox.Text.Trim(), direction);
        }

        public void OnCatalogUpdated()
        {
            RenderAppGrid(_filterBox.Text.Trim(), 0);
        }

        private void RenderAppGrid(string filter, int slideDirection)
        {
            _gridPanel.Children.Clear();
            _filteredApps.Clear();

            List<AppCatalogEntry> allApps = ShellController.Instance != null
                ? ShellController.Instance.InstalledApps
                : new List<AppCatalogEntry>();

            _placeholderText.Text = string.Format("Поиск среди {0} приложений (ПКМ — меню)...", allApps.Count);

            for (int i = 0; i < allApps.Count; i++)
            {
                AppCatalogEntry entry = allApps[i];
                if (!string.IsNullOrEmpty(filter))
                {
                    if (entry.Title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                        (entry.Category ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }
                else if (_activeCategory != "Все")
                {
                    if (_activeCategory == "★ Избранное")
                    {
                        if (!ShellController.Instance.IsFavoriteApp(entry.Title)) continue;
                    }
                    else if (!string.Equals(entry.Category, _activeCategory, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }
                _filteredApps.Add(entry);
            }

            _totalPages = Math.Max(1, (int)Math.Ceiling(_filteredApps.Count / (double)AppsPerPage));
            if (_currentPage >= _totalPages) _currentPage = _totalPages - 1;
            if (_currentPage < 0) _currentPage = 0;

            int startIdx = _currentPage * AppsPerPage;
            int endIdx = Math.Min(startIdx + AppsPerPage, _filteredApps.Count);

            for (int i = startIdx; i < endIdx; i++)
            {
                AppCatalogEntry entry = _filteredApps[i];
                bool isFav = ShellController.Instance != null && ShellController.Instance.IsFavoriteApp(entry.Title);

                Border itemBox = new Border
                {
                    Margin = new Thickness(6, 4, 6, 4),
                    CornerRadius = new CornerRadius(16),
                    Background = Brushes.Transparent,
                    Cursor = Cursors.Hand
                };

                StackPanel sp = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid iconWrap = new Grid { Width = 78, Height = 78 };
                Image img = new Image
                {
                    Source = entry.IconSource ?? ShellController.Instance.GetIcon("finder.png"),
                    Width = 72,
                    Height = 72,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                img.Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 5, Opacity = 0.48, Color = Colors.Black };
                iconWrap.Children.Add(img);

                if (isFav)
                {
                    Border starBadge = new Border
                    {
                        Width = 18,
                        Height = 18,
                        CornerRadius = new CornerRadius(9),
                        Background = new SolidColorBrush(Color.FromArgb(210, 255, 195, 0)),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 2, 2, 0)
                    };
                    starBadge.Child = new TextBlock
                    {
                        Text = "★",
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Black,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    iconWrap.Children.Add(starBadge);
                }
                sp.Children.Add(iconWrap);

                TextBlock titleBlock = new TextBlock
                {
                    Text = entry.Title,
                    Foreground = Brushes.White,
                    FontSize = 12.5,
                    FontWeight = FontWeights.Medium,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 136,
                    Margin = new Thickness(0, 6, 0, 0)
                };
                sp.Children.Add(titleBlock);

                itemBox.Child = sp;

                AppCatalogEntry cap = entry;
                Border capBox = itemBox;
                itemBox.MouseEnter += delegate
                {
                    img.Width = 78;
                    img.Height = 78;
                    capBox.Background = new SolidColorBrush(Color.FromArgb(26, 255, 255, 255));
                };
                itemBox.MouseLeave += delegate
                {
                    img.Width = 72;
                    img.Height = 72;
                    capBox.Background = Brushes.Transparent;
                };
                itemBox.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    HideWithAnimation();
                    ShellController.RunProcess(cap.Cmd, cap.Args);
                };
                itemBox.MouseRightButtonUp += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    ShowAppContextMenu(capBox, cap);
                };

                _gridPanel.Children.Add(itemBox);
            }

            _dotsPanel.Children.Clear();
            if (_totalPages > 1)
            {
                Border prevBtn = CreatePageArrow("‹", _currentPage > 0, delegate { SwitchToPage(_currentPage - 1, -1); });
                _dotsPanel.Children.Add(prevBtn);

                for (int p = 0; p < _totalPages; p++)
                {
                    int pageIndex = p;
                    bool active = (p == _currentPage);
                    Border dot = new Border
                    {
                        Width = active ? 20 : 8,
                        Height = 8,
                        CornerRadius = new CornerRadius(4),
                        Background = active
                            ? Brushes.White
                            : new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
                        Margin = new Thickness(5, 0, 5, 0),
                        Cursor = Cursors.Hand
                    };
                    dot.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                    {
                        e.Handled = true;
                        SwitchToPage(pageIndex, pageIndex > _currentPage ? 1 : -1);
                    };
                    _dotsPanel.Children.Add(dot);
                }

                Border nextBtn = CreatePageArrow("›", _currentPage < _totalPages - 1, delegate { SwitchToPage(_currentPage + 1, 1); });
                _dotsPanel.Children.Add(nextBtn);
            }

            if (slideDirection != 0 && _gridTranslate != null)
            {
                DoubleAnimation slideAnim = new DoubleAnimation(slideDirection * 48.0, 0.0, TimeSpan.FromMilliseconds(160));
                _gridTranslate.BeginAnimation(TranslateTransform.XProperty, slideAnim);
            }
        }

        private void ShowAppContextMenu(FrameworkElement anchor, AppCatalogEntry app)
        {
            if (_appContextMenuPopup != null && _appContextMenuPopup.IsOpen)
            {
                _appContextMenuPopup.IsOpen = false;
            }

            Popup popup = new Popup
            {
                AllowsTransparency = true,
                PlacementTarget = anchor,
                Placement = PlacementMode.Bottom,
                VerticalOffset = -8,
                StaysOpen = false,
                PopupAnimation = PopupAnimation.Fade
            };

            Border outer = new Border
            {
                Margin = new Thickness(10),
                Background = LiquidGlassTheme.CreateGlassBrush(236, 248),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(5),
                MinWidth = 225,
                Effect = new DropShadowEffect { BlurRadius = 22, ShadowDepth = 6, Opacity = 0.58, Color = Colors.Black }
            };

            StackPanel stack = new StackPanel();
            stack.Children.Add(CreateMenuRow("▶  Открыть «" + app.Title + "»", delegate
            {
                popup.IsOpen = false;
                HideWithAnimation();
                ShellController.RunProcess(app.Cmd, app.Args);
            }));

            stack.Children.Add(CreateMenuRow("🛡  Запустить от имени администратора", delegate
            {
                popup.IsOpen = false;
                HideWithAnimation();
                ShellController.RunProcessEx(app.Cmd, app.Args, true);
            }));

            bool pinned = ShellController.Instance.IsAppPinnedInDock(app.Cmd) || ShellController.Instance.IsAppPinnedInDock(app.Title);
            stack.Children.Add(CreateMenuRow(pinned ? "❌  Открепить из Dock" : "📌  Закрепить в Dock", delegate
            {
                popup.IsOpen = false;
                if (pinned)
                    ShellController.Instance.UnpinAppFromDock(app.Title);
                else
                    ShellController.Instance.PinAppToDock(app.Title, app.Cmd, app.Args);
            }));

            bool isFav = ShellController.Instance.IsFavoriteApp(app.Title);
            stack.Children.Add(CreateMenuRow(isFav ? "☆  Убрать из Избранного" : "★  Добавить в Избранное", delegate
            {
                popup.IsOpen = false;
                ShellController.Instance.ToggleFavoriteApp(app.Title);
                RenderAppGrid(_filterBox.Text.Trim(), 0);
            }));

            if (!string.IsNullOrEmpty(app.Cmd) && app.Cmd.IndexOf('\\') >= 0 && File.Exists(app.Cmd))
            {
                stack.Children.Add(CreateMenuRow("📂  Открыть расположение файла", delegate
                {
                    popup.IsOpen = false;
                    HideWithAnimation();
                    ShellController.OpenFileLocation(app.Cmd);
                }));
            }

            stack.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Margin = new Thickness(8, 4, 8, 4)
            });

            stack.Children.Add(CreateMenuRow("👁  Скрыть из лаунчера", delegate
            {
                popup.IsOpen = false;
                ShellController.Instance.HideAppFromCatalog(app.Title);
            }));

            outer.Child = stack;
            popup.Child = outer;
            _appContextMenuPopup = popup;
            popup.IsOpen = true;
        }

        private FrameworkElement CreateMenuRow(string text, Action onClick)
        {
            Border row = new Border
            {
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(10, 5, 10, 5),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand
            };
            row.Child = new TextBlock
            {
                Text = text,
                FontSize = 13,
                Foreground = Brushes.White
            };
            row.MouseEnter += delegate { row.Background = LiquidGlassTheme.AccentBlueBrush; };
            row.MouseLeave += delegate { row.Background = Brushes.Transparent; };
            row.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };
            return row;
        }

        private Border CreatePageArrow(string text, bool enabled, Action onClick)
        {
            Border b = new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(13),
                Background = new SolidColorBrush(Color.FromArgb(enabled ? (byte)42 : (byte)15, 255, 255, 255)),
                Margin = new Thickness(10, 0, 10, 0),
                Cursor = enabled ? Cursors.Hand : Cursors.Arrow
            };
            b.Child = new TextBlock
            {
                Text = text,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(enabled ? (byte)240 : (byte)80, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -2, 0, 0)
            };
            if (enabled)
            {
                b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    if (onClick != null) onClick();
                };
            }
            return b;
        }

        public void ShowWithAnimation()
        {
            if (ShellController.Instance != null)
            {
                ShellController.Instance.RefreshInstalledApps();
            }
            _filterBox.Text = "";
            _placeholderText.Visibility = Visibility.Visible;
            _currentPage = 0;
            RebuildCategoryPills();
            RenderAppGrid("", 0);
            Show();
            Activate();
            _filterBox.Focus();
            DoubleAnimation anim = new DoubleAnimation(1.06, 1.0, TimeSpan.FromMilliseconds(170));
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        public void HideWithAnimation()
        {
            if (_appContextMenuPopup != null) _appContextMenuPopup.IsOpen = false;
            Hide();
        }
    }

    // =========================================================================
    // 7. WINDOW SWITCHER / MISSION CONTROL (Обзор активных окон)
    // =========================================================================
    public class WindowSwitcherWindow : Window
    {
        private WrapPanel _windowsWrap;
        private TextBlock _subTitle;

        public WindowSwitcherWindow()
        {
            Title = "Обзор окон";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
            Background = new SolidColorBrush(Color.FromArgb(200, 10, 12, 20));
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();

            SourceInitialized += delegate
            {
                NativeMethods.EnableBlur(this, 0xBB0E0E16);
            };

            PreviewKeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    Hide();
                }
            };
        }

        private void BuildUI()
        {
            Grid root = new Grid { Background = Brushes.Transparent };
            root.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.OriginalSource == root) Hide();
            };

            StackPanel container = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 56, 0, 0),
                Width = Math.Min(1240, Width - 100)
            };

            Grid header = new Grid { Margin = new Thickness(12, 0, 12, 20) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titles = new StackPanel();
            titles.Children.Add(new TextBlock
            {
                Text = "Обзор активных окон",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            _subTitle = new TextBlock
            {
                Text = "Выберите окно для переключения или закройте ненужные приложения",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromArgb(175, 235, 235, 245)),
                Margin = new Thickness(0, 3, 0, 0)
            };
            titles.Children.Add(_subTitle);
            Grid.SetColumn(titles, 0);
            header.Children.Add(titles);

            StackPanel actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Border minAllBtn = CreateActionPill("🗕 Свернуть все окна", delegate
            {
                List<OpenWindowEntry> list = ShellController.Instance.GetOpenApplicationWindows();
                for (int i = 0; i < list.Count; i++)
                {
                    NativeMethods.ShowWindow(list[i].Hwnd, NativeMethods.SW_SHOWMINIMIZED);
                }
                Hide();
            });
            actions.Children.Add(minAllBtn);

            Border closeBtn = CreateActionPill("✕ Закрыть обзор (Esc)", delegate { Hide(); });
            closeBtn.Margin = new Thickness(10, 0, 0, 0);
            actions.Children.Add(closeBtn);

            Grid.SetColumn(actions, 1);
            header.Children.Add(actions);
            container.Children.Add(header);

            ScrollViewer sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = Height - 210
            };
            _windowsWrap = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };
            sv.Content = _windowsWrap;
            container.Children.Add(sv);

            root.Children.Add(container);
            Content = root;
        }

        private Border CreateActionPill(string text, Action onClick)
        {
            Border b = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(65, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 7, 14, 7),
                Cursor = Cursors.Hand
            };
            b.Child = new TextBlock { Text = text, FontSize = 12.5, FontWeight = FontWeights.Medium, Foreground = Brushes.White };
            b.MouseEnter += delegate { b.Background = LiquidGlassTheme.AccentBlueBrush; };
            b.MouseLeave += delegate { b.Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)); };
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };
            return b;
        }

        public void ShowAndRefresh()
        {
            PopulateWindows();
            Show();
            Activate();
        }

        private void PopulateWindows()
        {
            _windowsWrap.Children.Clear();
            List<OpenWindowEntry> wins = ShellController.Instance.GetOpenApplicationWindows();
            _subTitle.Text = string.Format("Открыто окон: {0} — нажмите на карточку, чтобы развернуть и активировать окно", wins.Count);

            if (wins.Count == 0)
            {
                Border emptyCard = new Border
                {
                    Width = 440,
                    CornerRadius = new CornerRadius(20),
                    Background = LiquidGlassTheme.CreateGlassBrush(200, 220),
                    BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(24),
                    Margin = new Thickness(0, 60, 0, 0)
                };
                emptyCard.Child = new TextBlock
                {
                    Text = "Нет открытых окон приложений.\r\nОткройте любое приложение из Dock или меню «Приложения» (Win).",
                    FontSize = 14,
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center
                };
                _windowsWrap.Children.Add(emptyCard);
                return;
            }

            for (int i = 0; i < wins.Count; i++)
            {
                OpenWindowEntry w = wins[i];
                Border card = new Border
                {
                    Width = 285,
                    Height = 148,
                    Margin = new Thickness(10),
                    CornerRadius = new CornerRadius(18),
                    Background = LiquidGlassTheme.CreateGlassBrush(215, 235),
                    BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(14),
                    Cursor = Cursors.Hand,
                    Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 6, Opacity = 0.5, Color = Colors.Black }
                };

                Grid g = new Grid();
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid topRow = new Grid();
                topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Image ico = new Image
                {
                    Source = w.Icon ?? ShellController.Instance.GetIcon("finder.png"),
                    Width = 34,
                    Height = 34,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                RenderOptions.SetBitmapScalingMode(ico, BitmapScalingMode.HighQuality);
                Grid.SetColumn(ico, 0);
                topRow.Children.Add(ico);

                StackPanel appInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                appInfo.Children.Add(new TextBlock
                {
                    Text = w.AppTitle,
                    FontSize = 13.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                appInfo.Children.Add(new TextBlock
                {
                    Text = w.IsMinimized ? "Свёрнуто" : "Активно",
                    FontSize = 10.5,
                    Foreground = w.IsMinimized
                        ? new SolidColorBrush(Color.FromRgb(255, 214, 10))
                        : new SolidColorBrush(Color.FromRgb(48, 209, 88))
                });
                Grid.SetColumn(appInfo, 1);
                topRow.Children.Add(appInfo);

                Border closeWinBtn = new Border
                {
                    Width = 24,
                    Height = 24,
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush(Color.FromArgb(45, 255, 95, 87)),
                    VerticalAlignment = VerticalAlignment.Top,
                    ToolTip = "Закрыть окно"
                };
                closeWinBtn.Child = new TextBlock
                {
                    Text = "✕",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                OpenWindowEntry capW = w;
                closeWinBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    NativeMethods.PostMessage(capW.Hwnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    DispatcherTimer dt = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
                    dt.Tick += delegate
                    {
                        dt.Stop();
                        PopulateWindows();
                    };
                    dt.Start();
                };
                Grid.SetColumn(closeWinBtn, 2);
                topRow.Children.Add(closeWinBtn);

                Grid.SetRow(topRow, 0);
                g.Children.Add(topRow);

                TextBlock winTitleBlock = new TextBlock
                {
                    Text = w.WindowTitle,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromArgb(215, 235, 235, 245)),
                    TextWrapping = TextWrapping.Wrap,
                    MaxHeight = 42,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 10, 0, 8)
                };
                Grid.SetRow(winTitleBlock, 1);
                g.Children.Add(winTitleBlock);

                Border switchPill = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255)),
                    Padding = new Thickness(8, 4, 8, 4),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                switchPill.Child = new TextBlock
                {
                    Text = "Переключиться на окно →",
                    FontSize = 11,
                    FontWeight = FontWeights.Medium,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(switchPill, 2);
                g.Children.Add(switchPill);

                card.Child = g;
                card.MouseEnter += delegate { card.BorderBrush = LiquidGlassTheme.AccentBlueBrush; };
                card.MouseLeave += delegate { card.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(); };
                card.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    Hide();
                    if (NativeMethods.IsIconic(capW.Hwnd))
                    {
                        NativeMethods.ShowWindow(capW.Hwnd, NativeMethods.SW_RESTORE);
                    }
                    NativeMethods.SwitchToThisWindow(capW.Hwnd, true);
                    NativeMethods.SetForegroundWindow(capW.Hwnd);
                };

                _windowsWrap.Children.Add(card);
            }
        }
    }

    // =========================================================================
    // 8. FULL GRAPHICAL LAUNCHER SETTINGS HUB (Настройки лаунчера — 4 вкладки)
    // =========================================================================
    public class LauncherSettingsWindow : Window
    {
        private StackPanel _navStack;
        private Border _contentHost;
        private int _activeTab = 0;

        public LauncherSettingsWindow()
        {
            Title = "Настройки Tahoe Launcher";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Width = 780;
            Height = 560;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();
        }

        private void BuildUI()
        {
            Border outer = new Border
            {
                Margin = new Thickness(16),
                CornerRadius = new CornerRadius(24),
                Background = LiquidGlassTheme.CreateGlassBrush(236, 248),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                Effect = new DropShadowEffect { BlurRadius = 32, ShadowDepth = 10, Opacity = 0.65, Color = Colors.Black }
            };

            Grid mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left Sidebar
            Border sidebar = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(45, 0, 0, 0)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)),
                BorderThickness = new Thickness(0, 0, 1, 0),
                CornerRadius = new CornerRadius(24, 0, 0, 24),
                Padding = new Thickness(16, 16, 14, 16)
            };
            sidebar.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    try { DragMove(); } catch { }
                }
            };

            StackPanel sideStack = new StackPanel();

            StackPanel traffic = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2, 0, 0, 18) };
            Ellipse closeBtn = new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Color.FromRgb(255, 95, 87)), Margin = new Thickness(0, 0, 8, 0), Cursor = Cursors.Hand, ToolTip = "Закрыть" };
            closeBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; Hide(); };
            Ellipse minBtn = new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Color.FromRgb(254, 188, 46)), Margin = new Thickness(0, 0, 8, 0) };
            Ellipse maxBtn = new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Color.FromRgb(40, 200, 64)) };
            traffic.Children.Add(closeBtn);
            traffic.Children.Add(minBtn);
            traffic.Children.Add(maxBtn);
            sideStack.Children.Add(traffic);

            sideStack.Children.Add(new TextBlock
            {
                Text = "Tahoe Launcher",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(4, 0, 0, 2)
            });
            sideStack.Children.Add(new TextBlock
            {
                Text = "Liquid Glass Shell v26.2",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromArgb(155, 235, 235, 245)),
                Margin = new Thickness(4, 0, 0, 18)
            });

            _navStack = new StackPanel();
            sideStack.Children.Add(_navStack);
            sidebar.Child = sideStack;
            Grid.SetColumn(sidebar, 0);
            mainGrid.Children.Add(sidebar);

            // Right Content Area
            _contentHost = new Border
            {
                Padding = new Thickness(22, 18, 22, 18)
            };
            Grid.SetColumn(_contentHost, 1);
            mainGrid.Children.Add(_contentHost);

            outer.Child = mainGrid;
            Content = outer;

            SwitchTab(0);
        }

        public void RefreshAllTabs()
        {
            SwitchTab(_activeTab);
        }

        private void SwitchTab(int tabIdx)
        {
            _activeTab = tabIdx;
            _navStack.Children.Clear();

            string[] tabs = new string[]
            {
                "⚙  Оболочка и вид",
                "📌  Панель Dock",
                "🚀  Каталог программ",
                "⌨  Хоткеи и система"
            };

            for (int i = 0; i < tabs.Length; i++)
            {
                int idx = i;
                bool active = (i == _activeTab);
                Border navItem = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Background = active ? LiquidGlassTheme.AccentBlueBrush : Brushes.Transparent,
                    Padding = new Thickness(12, 9, 12, 9),
                    Margin = new Thickness(0, 2, 0, 2),
                    Cursor = Cursors.Hand
                };
                navItem.Child = new TextBlock
                {
                    Text = tabs[i],
                    FontSize = 13,
                    FontWeight = active ? FontWeights.SemiBold : FontWeights.Medium,
                    Foreground = Brushes.White
                };
                navItem.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    SwitchTab(idx);
                };
                _navStack.Children.Add(navItem);
            }

            if (_activeTab == 0) _contentHost.Child = BuildAppearanceTab();
            else if (_activeTab == 1) _contentHost.Child = BuildDockManagerTab();
            else if (_activeTab == 2) _contentHost.Child = BuildCatalogTab();
            else _contentHost.Child = BuildSystemTab();
        }

        private UIElement BuildAppearanceTab()
        {
            LauncherConfig cfg = ShellController.Instance.Config;
            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            StackPanel sp = new StackPanel();

            sp.Children.Add(CreateSectionHeader("Модули оболочки и рабочего стола"));
            sp.Children.Add(CreateToggleRow("Верхняя строка меню (Top Bar)", "Глобальное меню, индикатор раскладки, ОЗУ и часы", cfg.ShowTopBar, delegate(bool v)
            {
                cfg.ShowTopBar = v;
                ShellController.Instance.SaveConfig();
                ShellController.Instance.ApplyModuleVisibility();
            }));
            sp.Children.Add(CreateToggleRow("Нижняя панель Dock", "Интерактивный Dock с физикой волны 120+ Гц", cfg.ShowDock, delegate(bool v)
            {
                cfg.ShowDock = v;
                ShellController.Instance.SaveConfig();
                ShellController.Instance.ApplyModuleVisibility();
            }));
            sp.Children.Add(CreateToggleRow("Виджеты рабочего стола", "Виджеты времени, телеметрии ПК, избранного и заметок", cfg.ShowDesktopWidgets, delegate(bool v)
            {
                cfg.ShowDesktopWidgets = v;
                ShellController.Instance.SaveConfig();
                ShellController.Instance.ApplyModuleVisibility();
            }));
            sp.Children.Add(CreateToggleRow("Скрывать панель задач Windows 11", "Убирает стандартный таскбар для чистого вида", cfg.HideWindowsTaskbar, delegate(bool v)
            {
                cfg.HideWindowsTaskbar = v;
                ShellController.Instance.SaveConfig();
                ShellController.Instance.ApplyModuleVisibility();
            }));
            sp.Children.Add(CreateToggleRow("Открывать «Приложения» по клавише Win", "Одиночное нажатие Win открывает лаунчер (Win+E/R/S работают штатно)", cfg.InterceptWinKey, delegate(bool v)
            {
                cfg.InterceptWinKey = v;
                ShellController.Instance.SaveConfig();
            }));
            sp.Children.Add(CreateToggleRow("Автозапуск вместе с Windows", "Автоматически запускать Tahoe Launcher при входе в систему", ShellController.Instance.IsAutostartEnabled(), delegate(bool v)
            {
                ShellController.Instance.SetAutostartEnabled(v);
            }));

            sp.Children.Add(CreateSectionHeader("Размеры и анимация Dock"));
            sp.Children.Add(CreateSliderRow("Базовый размер иконок Dock", 38, 68, cfg.DockIconSize, "px", delegate(double v)
            {
                cfg.DockIconSize = Math.Round(v);
                ShellController.Instance.SaveConfig();
                if (ShellController.Instance.Dock != null) ShellController.Instance.Dock.RebuildDockVisuals();
            }));
            sp.Children.Add(CreateSliderRow("Сила увеличения при наведении (Zoom)", 0, 36, cfg.DockZoomRange, "px", delegate(double v)
            {
                cfg.DockZoomRange = Math.Round(v);
                ShellController.Instance.SaveConfig();
            }));

            sp.Children.Add(CreateSectionHeader("Обои рабочего стола 6K"));
            WrapPanel wpWrap = new WrapPanel { Margin = new Thickness(0, 4, 0, 8) };
            if (Directory.Exists(ShellController.Instance.WallpapersDir))
            {
                string[] files = Directory.GetFiles(ShellController.Instance.WallpapersDir, "*.jpg");
                for (int i = 0; i < files.Length; i++)
                {
                    string f = files[i];
                    string name = IOPath.GetFileNameWithoutExtension(f);
                    Border wpBtn = CreateSmallButton("🖼 " + name, delegate { ShellController.Instance.ApplyWallpaperFile(f); });
                    wpBtn.Margin = new Thickness(0, 0, 6, 6);
                    wpWrap.Children.Add(wpBtn);
                }
            }
            Border customWpBtn = CreateSmallButton("📂 Выбрать свой файл...", delegate
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Title = "Выберите изображение для обоев",
                    Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
                };
                if (ofd.ShowDialog() == true)
                {
                    ShellController.Instance.ApplyWallpaperFile(ofd.FileName);
                }
            });
            customWpBtn.Margin = new Thickness(0, 0, 6, 6);
            wpWrap.Children.Add(customWpBtn);
            sp.Children.Add(wpWrap);

            sv.Content = sp;
            return sv;
        }

        private UIElement BuildDockManagerTab()
        {
            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock header = CreateSectionHeader("Закреплённые приложения в Dock");
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 6, 0, 10) };
            StackPanel listStack = new StackPanel();

            List<DockItemConfig> items = ShellController.Instance.Config.PinnedDockItems;
            for (int i = 0; i < items.Count; i++)
            {
                DockItemConfig item = items[i];
                if (item.IsSeparator) continue;

                Border row = new Border
                {
                    CornerRadius = new CornerRadius(11),
                    Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 3, 0, 3)
                };

                Grid g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                StackPanel info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                info.Children.Add(new TextBlock { Text = item.Name, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                info.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrEmpty(item.LaunchCommand) ? "Системный элемент" : item.LaunchCommand,
                    FontSize = 10.5,
                    Foreground = new SolidColorBrush(Color.FromArgb(150, 235, 235, 245)),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                Grid.SetColumn(info, 0);
                g.Children.Add(info);

                StackPanel btns = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                if (!item.IsExplorer && !item.IsAppsLauncher && !item.IsTrash && !item.IsDesktopFolder)
                {
                    string capId = item.Id;
                    btns.Children.Add(CreateSmallButton("↑", delegate
                    {
                        ShellController.Instance.MovePinnedDockItem(capId, -1);
                        SwitchTab(1);
                    }));
                    Border downBtn = CreateSmallButton("↓", delegate
                    {
                        ShellController.Instance.MovePinnedDockItem(capId, 1);
                        SwitchTab(1);
                    });
                    downBtn.Margin = new Thickness(4, 0, 4, 0);
                    btns.Children.Add(downBtn);

                    btns.Children.Add(CreateSmallButton("Удалить", delegate
                    {
                        ShellController.Instance.UnpinAppFromDock(capId);
                        SwitchTab(1);
                    }));
                }
                else
                {
                    btns.Children.Add(new TextBlock
                    {
                        Text = "Системный",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromArgb(130, 235, 235, 245)),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }

                Grid.SetColumn(btns, 1);
                g.Children.Add(btns);
                row.Child = g;
                listStack.Children.Add(row);
            }

            sv.Content = listStack;
            Grid.SetRow(sv, 1);
            root.Children.Add(sv);

            StackPanel bottomBar = new StackPanel { Orientation = Orientation.Horizontal };
            bottomBar.Children.Add(CreateSmallButton("➕ Добавить .exe в Dock...", delegate
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Title = "Выберите программу (.exe) для закрепления в Dock",
                    Filter = "Программы (*.exe)|*.exe"
                };
                if (ofd.ShowDialog() == true)
                {
                    string title = IOPath.GetFileNameWithoutExtension(ofd.FileName);
                    ShellController.Instance.PinAppToDock(title, ofd.FileName, "");
                    SwitchTab(1);
                }
            }));

            Border resetBtn = CreateSmallButton("↺ Сбросить Dock по умолчанию", delegate
            {
                ShellController.Instance.Config.PinnedDockItems.Clear();
                ShellController.Instance.SaveConfig();
                ShellController.Instance.LoadConfig();
                // Repopulate defaults
                ShellController.Instance.RebuildDockItemsFromConfig();
                SwitchTab(1);
            });
            resetBtn.Margin = new Thickness(10, 0, 0, 0);
            bottomBar.Children.Add(resetBtn);

            Grid.SetRow(bottomBar, 2);
            root.Children.Add(bottomBar);
            return root;
        }

        private UIElement BuildCatalogTab()
        {
            StackPanel sp = new StackPanel();
            sp.Children.Add(CreateSectionHeader(string.Format("Каталог приложений ({0} найдено)", ShellController.Instance.InstalledApps.Count)));

            TextBlock desc = new TextBlock
            {
                Text = "Лаунчер автоматически отслеживает установку и удаление программ в меню «Пуск» и на Рабочем столе в реальном времени. Вы также можете добавить свою портативную программу или вернуть скрытые приложения.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromArgb(190, 235, 235, 245)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 14)
            };
            sp.Children.Add(desc);

            StackPanel actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 18) };
            actions.Children.Add(CreateSmallButton("➕ Добавить портативную программу (.exe)...", delegate
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Title = "Добавить приложение в каталог лаунчера",
                    Filter = "Программы (*.exe)|*.exe"
                };
                if (ofd.ShowDialog() == true)
                {
                    string title = IOPath.GetFileNameWithoutExtension(ofd.FileName);
                    ShellController.Instance.Config.CustomApps.Add(new CustomAppConfig
                    {
                        Title = title,
                        Cmd = ofd.FileName,
                        Args = "",
                        Category = "Программы"
                    });
                    ShellController.Instance.SaveConfig();
                    ShellController.Instance.RefreshInstalledApps();
                    SwitchTab(2);
                }
            }));

            Border rescanBtn = CreateSmallButton("↻ Пересканировать сейчас", delegate
            {
                ShellController.Instance.RefreshInstalledApps();
                SwitchTab(2);
            });
            rescanBtn.Margin = new Thickness(10, 0, 0, 0);
            actions.Children.Add(rescanBtn);
            sp.Children.Add(actions);

            sp.Children.Add(CreateSectionHeader("Скрытые приложения (" + ShellController.Instance.Config.HiddenApps.Count + ")"));
            if (ShellController.Instance.Config.HiddenApps.Count == 0)
            {
                sp.Children.Add(new TextBlock
                {
                    Text = "Нет скрытых приложений. Чтобы скрыть приложение, нажмите по нему правой кнопкой мыши в меню «Приложения».",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromArgb(150, 235, 235, 245))
                });
            }
            else
            {
                WrapPanel hiddenWrap = new WrapPanel();
                for (int i = 0; i < ShellController.Instance.Config.HiddenApps.Count; i++)
                {
                    string hName = ShellController.Instance.Config.HiddenApps[i];
                    Border chip = CreateSmallButton("👁 Вернуть «" + hName + "»", delegate
                    {
                        ShellController.Instance.Config.HiddenApps.Remove(hName);
                        ShellController.Instance.SaveConfig();
                        ShellController.Instance.RefreshInstalledApps();
                        SwitchTab(2);
                    });
                    chip.Margin = new Thickness(0, 0, 6, 6);
                    hiddenWrap.Children.Add(chip);
                }
                sp.Children.Add(hiddenWrap);
            }

            return sp;
        }

        private UIElement BuildSystemTab()
        {
            StackPanel sp = new StackPanel();
            sp.Children.Add(CreateSectionHeader("Горячие клавиши Tahoe Launcher"));

            sp.Children.Add(CreateShortcutRow("Win (одиночное нажатие)", "Открыть / закрыть полноэкранное меню «Приложения»"));
            sp.Children.Add(CreateShortcutRow("Alt + Пробел", "Командная палитра, поиск программ, калькулятор и терминал"));
            sp.Children.Add(CreateShortcutRow("Win + Shift + A", "Открыть окно «Настройки лаунчера»"));
            sp.Children.Add(CreateShortcutRow("ПКМ по иконке в Dock / Лаунчере", "Закрепить, открепить, запуск от Администратора, папка файла"));
            sp.Children.Add(CreateShortcutRow("Колесо мыши / Стрелки в Лаунчере", "Быстрое переключение страниц приложений"));

            sp.Children.Add(CreateSectionHeader("Управление процессом и конфигурацией"));
            StackPanel sysBtns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 16) };
            sysBtns.Children.Add(CreateSmallButton("📂 Открыть config.json", delegate
            {
                ShellController.OpenFileLocation(ShellController.Instance.ConfigPath);
            }));

            Border openDirBtn = CreateSmallButton("📁 Папка лаунчера", delegate
            {
                ShellController.RunProcess("explorer.exe", ShellController.Instance.BaseDir);
            });
            openDirBtn.Margin = new Thickness(8, 0, 0, 0);
            sysBtns.Children.Add(openDirBtn);
            sp.Children.Add(sysBtns);

            Border exitCard = new Border
            {
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromArgb(45, 255, 69, 58)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 69, 58)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 10, 0, 0)
            };
            StackPanel exitStack = new StackPanel();
            exitStack.Children.Add(new TextBlock
            {
                Text = "Выход из Tahoe Launcher",
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            exitStack.Children.Add(new TextBlock
            {
                Text = "Завершает работу оболочки и мгновенно возвращает стандартную панель задач Windows 11.",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 235, 235, 245)),
                Margin = new Thickness(0, 2, 0, 10)
            });
            Border quitBtn = CreateSmallButton("⏻ Выключить лаунчер и вернуть панель Windows", delegate
            {
                ShellController.Instance.ShutdownAndRestoreWindows();
            });
            quitBtn.HorizontalAlignment = HorizontalAlignment.Left;
            exitStack.Children.Add(quitBtn);
            exitCard.Child = exitStack;
            sp.Children.Add(exitCard);

            return sp;
        }

        private TextBlock CreateSectionHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 14.5,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 8, 0, 8)
            };
        }

        private FrameworkElement CreateToggleRow(string title, string sub, bool initial, Action<bool> onChanged)
        {
            Border row = new Border
            {
                CornerRadius = new CornerRadius(11),
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 3, 0, 3)
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            texts.Children.Add(new TextBlock { Text = title, FontSize = 12.5, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
            texts.Children.Add(new TextBlock { Text = sub, FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(165, 235, 235, 245)) });
            Grid.SetColumn(texts, 0);
            g.Children.Add(texts);

            CheckBox cb = new CheckBox
            {
                IsChecked = initial,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            cb.Checked += delegate { if (onChanged != null) onChanged(true); };
            cb.Unchecked += delegate { if (onChanged != null) onChanged(false); };
            Grid.SetColumn(cb, 1);
            g.Children.Add(cb);

            row.Child = g;
            return row;
        }

        private FrameworkElement CreateSliderRow(string title, double min, double max, double val, string unit, Action<double> onChanged)
        {
            Border row = new Border
            {
                CornerRadius = new CornerRadius(11),
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 3, 0, 3)
            };

            StackPanel sp = new StackPanel();
            Grid top = new Grid();
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            top.Children.Add(new TextBlock { Text = title, FontSize = 12.5, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
            TextBlock valText = new TextBlock { Text = string.Format("{0:0} {1}", val, unit), FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
            Grid.SetColumn(valText, 1);
            top.Children.Add(valText);
            sp.Children.Add(top);

            Slider sl = new Slider
            {
                Minimum = min,
                Maximum = max,
                Value = val,
                Margin = new Thickness(0, 6, 0, 0),
                Cursor = Cursors.Hand
            };
            sl.ValueChanged += delegate(object s, RoutedPropertyChangedEventArgs<double> e)
            {
                valText.Text = string.Format("{0:0} {1}", e.NewValue, unit);
                if (onChanged != null) onChanged(e.NewValue);
            };
            sp.Children.Add(sl);
            row.Child = sp;
            return row;
        }

        private FrameworkElement CreateShortcutRow(string key, string desc)
        {
            Border row = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)),
                Padding = new Thickness(12, 7, 12, 7),
                Margin = new Thickness(0, 2, 0, 2)
            };
            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock k = new TextBlock { Text = key, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(100, 210, 255)) };
            Grid.SetColumn(k, 0);
            g.Children.Add(k);

            TextBlock d = new TextBlock { Text = desc, FontSize = 12, Foreground = Brushes.White };
            Grid.SetColumn(d, 1);
            g.Children.Add(d);

            row.Child = g;
            return row;
        }

        private Border CreateSmallButton(string text, Action onClick)
        {
            Border b = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(48, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(65, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(11, 5, 11, 5),
                Cursor = Cursors.Hand
            };
            b.Child = new TextBlock { Text = text, FontSize = 12, FontWeight = FontWeights.Medium, Foreground = Brushes.White };
            b.MouseEnter += delegate { b.Background = LiquidGlassTheme.AccentBlueBrush; };
            b.MouseLeave += delegate { b.Background = new SolidColorBrush(Color.FromArgb(48, 255, 255, 255)); };
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };
            return b;
        }
    }

    // =========================================================================
    // 9. CALENDAR POPUP
    // =========================================================================
    public class CalendarWidgetWindow : Window
    {
        private TextBlock _bigTime;
        private TextBlock _bigDate;

        public CalendarWidgetWindow()
        {
            Title = "Календарь";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Width = 340;
            Height = 390;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();
            Deactivated += delegate { Hide(); };
        }

        private void BuildUI()
        {
            Border card = new Border
            {
                Margin = new Thickness(12),
                CornerRadius = new CornerRadius(24),
                Background = LiquidGlassTheme.CreateGlassBrush(226, 242),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16),
                Effect = new DropShadowEffect { BlurRadius = 28, ShadowDepth = 8, Opacity = 0.6, Color = Colors.Black }
            };

            StackPanel root = new StackPanel();

            _bigTime = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm"),
                FontSize = 44,
                FontWeight = FontWeights.Light,
                Foreground = Brushes.White
            };
            root.Children.Add(_bigTime);

            CultureInfo ru = new CultureInfo("ru-RU");
            _bigDate = new TextBlock
            {
                Text = DateTime.Now.ToString("dddd, d MMMM yyyy г.", ru),
                FontSize = 13.5,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromArgb(195, 235, 235, 245)),
                Margin = new Thickness(0, 0, 0, 14)
            };
            root.Children.Add(_bigDate);

            Border calCard = new Border
            {
                CornerRadius = new CornerRadius(16),
                Background = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 10, 12, 10)
            };

            UniformGrid calGrid = new UniformGrid { Columns = 7 };
            string[] days = new string[] { "ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ", "ВС" };
            for (int i = 0; i < 7; i++)
            {
                calGrid.Children.Add(new TextBlock
                {
                    Text = days[i],
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = (i >= 5)
                        ? new SolidColorBrush(Color.FromArgb(180, 255, 105, 97))
                        : new SolidColorBrush(Color.FromArgb(150, 235, 235, 245)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 6)
                });
            }

            DateTime now = DateTime.Now;
            DateTime firstDay = new DateTime(now.Year, now.Month, 1);
            int offset = ((int)firstDay.DayOfWeek + 6) % 7;
            int daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);

            for (int b = 0; b < offset; b++)
            {
                calGrid.Children.Add(new Border { Width = 28, Height = 28 });
            }

            for (int d = 1; d <= daysInMonth; d++)
            {
                bool isToday = (d == now.Day);
                Border dayPill = new Border
                {
                    Width = 28,
                    Height = 28,
                    CornerRadius = new CornerRadius(14),
                    Background = isToday ? new SolidColorBrush(Color.FromRgb(255, 69, 58)) : Brushes.Transparent,
                    Margin = new Thickness(1)
                };
                dayPill.Child = new TextBlock
                {
                    Text = d.ToString(),
                    FontSize = 12,
                    FontWeight = isToday ? FontWeights.Bold : FontWeights.Medium,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                calGrid.Children.Add(dayPill);
            }

            calCard.Child = calGrid;
            root.Children.Add(calCard);

            card.Child = root;
            Content = card;
        }

        public void RefreshState()
        {
            CultureInfo ru = new CultureInfo("ru-RU");
            _bigTime.Text = DateTime.Now.ToString("HH:mm");
            _bigDate.Text = DateTime.Now.ToString("dddd, d MMMM yyyy г.", ru);
        }
    }

    // =========================================================================
    // 10. ABOUT SYSTEM WINDOW (О системе — Динамическое определение железа ПК)
    // =========================================================================
    public class AboutSystemWindow : Window
    {
        public AboutSystemWindow()
        {
            Title = "О системе";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Width = 360;
            Height = 430;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();
        }

        private static string DetectCpuName()
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"HARDWARE\Description\System\CentralProcessor\0");
                if (rk != null)
                {
                    object val = rk.GetValue("ProcessorNameString");
                    rk.Close();
                    if (val != null) return val.ToString().Trim();
                }
            }
            catch { }
            return string.Format("Процессор ({0} потоков)", Environment.ProcessorCount);
        }

        private static string DetectGpuName()
        {
            try
            {
                RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
                if (baseKey != null)
                {
                    string[] subs = baseKey.GetSubKeyNames();
                    for (int i = 0; i < subs.Length; i++)
                    {
                        if (!subs[i].StartsWith("0")) continue;
                        RegistryKey sub = baseKey.OpenSubKey(subs[i]);
                        if (sub != null)
                        {
                            object desc = sub.GetValue("DriverDesc");
                            sub.Close();
                            if (desc != null && !string.IsNullOrEmpty(desc.ToString()))
                                return desc.ToString().Trim();
                        }
                    }
                    baseKey.Close();
                }
            }
            catch { }
            return "DirectX 12 GPU";
        }

        private static string DetectRamSize()
        {
            try
            {
                NativeMethods.MEMORYSTATUSEX mem = new NativeMethods.MEMORYSTATUSEX();
                mem.dwLength = (uint)Marshal.SizeOf(mem);
                if (NativeMethods.GlobalMemoryStatusEx(ref mem))
                {
                    double gb = Math.Round(mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0));
                    return string.Format("{0:0} ГБ ОЗУ", gb);
                }
            }
            catch { }
            return "16 ГБ ОЗУ";
        }

        private void BuildUI()
        {
            Border card = new Border
            {
                Margin = new Thickness(14),
                CornerRadius = new CornerRadius(22),
                Background = LiquidGlassTheme.CreateGlassBrush(235, 248),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(20, 14, 20, 20),
                Effect = new DropShadowEffect { BlurRadius = 30, ShadowDepth = 10, Opacity = 0.65, Color = Colors.Black }
            };

            card.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ButtonState == MouseButtonState.Pressed) DragMove();
            };

            StackPanel root = new StackPanel();

            StackPanel traffic = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
            Ellipse closeBtn = new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Color.FromRgb(255, 95, 87)), Margin = new Thickness(0, 0, 8, 0), Cursor = Cursors.Hand };
            closeBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; Close(); };
            Ellipse minBtn = new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Color.FromRgb(254, 188, 46)), Margin = new Thickness(0, 0, 8, 0) };
            Ellipse maxBtn = new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Color.FromRgb(40, 200, 64)) };
            traffic.Children.Add(closeBtn);
            traffic.Children.Add(minBtn);
            traffic.Children.Add(maxBtn);
            root.Children.Add(traffic);

            Image img = new Image
            {
                Source = ShellController.Instance.GetIcon("wallpaper.png"),
                Width = 82,
                Height = 82,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
            root.Children.Add(img);

            root.Children.Add(new TextBlock
            {
                Text = Environment.MachineName,
                FontSize = 21,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            root.Children.Add(new TextBlock
            {
                Text = "Windows 11 • Tahoe Liquid Glass v26.2",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromArgb(165, 235, 235, 245)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 16)
            });

            root.Children.Add(CreateSpecRow("Процессор", DetectCpuName()));
            root.Children.Add(CreateSpecRow("Графика", DetectGpuName()));
            root.Children.Add(CreateSpecRow("Память", DetectRamSize()));
            root.Children.Add(CreateSpecRow("Каталог", string.Format("{0} приложений", ShellController.Instance.InstalledApps.Count)));

            StackPanel btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };

            Border settingsBtn = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = LiquidGlassTheme.AccentBlueBrush,
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            settingsBtn.Child = new TextBlock { Text = "Настройки лаунчера", Foreground = Brushes.White, FontSize = 12, FontWeight = FontWeights.SemiBold };
            settingsBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                Close();
                ShellController.Instance.ShowLauncherSettings();
            };
            btnRow.Children.Add(settingsBtn);

            Border moreBtn = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(55, 255, 255, 255)),
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = Cursors.Hand
            };
            moreBtn.Child = new TextBlock { Text = "Параметры ОС...", Foreground = Brushes.White, FontSize = 12, FontWeight = FontWeights.Medium };
            moreBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                ShellController.RunProcess("explorer.exe", "ms-settings:about");
                Close();
            };
            btnRow.Children.Add(moreBtn);

            root.Children.Add(btnRow);

            card.Child = root;
            Content = card;
        }

        private FrameworkElement CreateSpecRow(string key, string val)
        {
            Grid g = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock k = new TextBlock
            {
                Text = key,
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromArgb(165, 235, 235, 245)),
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 0, 12, 0)
            };
            Grid.SetColumn(k, 0);
            g.Children.Add(k);

            TextBlock v = new TextBlock
            {
                Text = val,
                FontSize = 11.5,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(v, 1);
            g.Children.Add(v);
            return g;
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try { NativeMethods.SetProcessDpiAwareness(2); }
            catch { try { NativeMethods.SetProcessDPIAware(); } catch { } }

            bool quitOnly = false;
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "--quit", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[i], "-x", StringComparison.OrdinalIgnoreCase))
                    {
                        quitOnly = true;
                    }
                }
            }

            int myPid = Process.GetCurrentProcess().Id;
            foreach (Process p in Process.GetProcessesByName("MacOSTahoe"))
            {
                if (p.Id != myPid)
                {
                    try { p.Kill(); } catch { }
                }
            }

            if (quitOnly)
            {
                NativeMethods.RestoreNativeTaskbars();
                NativeMethods.RECT fullRect = new NativeMethods.RECT
                {
                    Left = 0,
                    Top = 0,
                    Right = (int)SystemParameters.PrimaryScreenWidth,
                    Bottom = (int)SystemParameters.PrimaryScreenHeight
                };
                NativeMethods.SystemParametersInfoRect(0x002F, 0, ref fullRect, 0x0002);
                return;
            }

            Application app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            ShellController controller = new ShellController();
            controller.Start(args);
            app.Run();
        }
    }
}
