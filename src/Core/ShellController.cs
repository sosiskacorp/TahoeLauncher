using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using MacOSTahoe.Models;
using MacOSTahoe.Native;
using MacOSTahoe.Services;
using MacOSTahoe.Views;

namespace MacOSTahoe.Core
{
    public class ShellController
    {
        public static ShellController Instance { get; private set; }

        public const string ConfigPath = @"C:\Win11_Prep\macos\config.json";
        public const string BaseDir = @"C:\Win11_Prep\macos";
        public const string IconsDir = @"C:\Win11_Prep\macos\icons";

        public LauncherConfig Config { get; private set; }
        public AppCatalogService AppCatalog { get; private set; }
        public WindowTrackerService WindowTracker { get; private set; }

        public TopBarWindow TopBar { get; private set; }
        public DockWindow Dock { get; private set; }
        public LaunchpadWindow Launchpad { get; private set; }
        public SpotlightWindow Spotlight { get; private set; }
        public WindowSwitcherWindow WindowSwitcher { get; private set; }
        public ControlCenterWindow ControlCenter { get; private set; }
        public CalendarWidgetWindow CalendarWidget { get; private set; }
        public DesktopWidgetsWindow DesktopWidgets { get; private set; }
        public LauncherSettingsWindow SettingsWindow { get; private set; }
        public AboutSystemWindow AboutWindow { get; private set; }

        private IntPtr _hKeyboardHook = IntPtr.Zero;
        private Win32Api.LowLevelKeyboardProc _keyboardProc;
        private bool _winKeyCandidate = false;

        private HwndSource _hotkeyHwndSource;
        private const int HOTKEY_SPOTLIGHT_ID = 9001;

        public ShellController()
        {
            Instance = this;
            AppCatalog = new AppCatalogService();
            WindowTracker = new WindowTrackerService();
            LoadConfig();
        }

        public void Initialize(Application app)
        {
            AppCatalog.Initialize(app.Dispatcher);

            // Create windows
            if (Config.ShowTopBar)
            {
                TopBar = new TopBarWindow();
                TopBar.Show();
            }

            if (Config.ShowDock)
            {
                Dock = new DockWindow();
                Dock.Show();
            }

            if (Config.ShowDesktopWidgets)
            {
                DesktopWidgets = new DesktopWidgetsWindow();
                DesktopWidgets.Show();
            }

            // Lazy / Overlay windows created but kept hidden
            Launchpad = new LaunchpadWindow();
            Spotlight = new SpotlightWindow();
            WindowSwitcher = new WindowSwitcherWindow();
            ControlCenter = new ControlCenterWindow();
            CalendarWidget = new CalendarWidgetWindow();

            // Apply work area & taskbar settings safely
            ApplyTaskbarAndWorkArea();

            // Setup global keyboard hook for Win key
            SetupKeyboardHook();

            // Setup Alt+Space hotkey for Spotlight
            SetupGlobalHotkey();
        }

        public void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath, Encoding.UTF8);
                    var serializer = new JavaScriptSerializer();
                    Config = serializer.Deserialize<LauncherConfig>(json);
                    if (Config != null) return;
                }
            }
            catch { }

            Config = new LauncherConfig();
            SaveConfig();
        }

        public void SaveConfig()
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(Config);
                // Format json nicely if possible
                File.WriteAllText(ConfigPath, json, new UTF8Encoding(false));
            }
            catch { }
        }

        public void ApplyTaskbarAndWorkArea()
        {
            if (Config.HideWindowsTaskbar)
            {
                DwmBlurHelper.SetWindowsTaskbarVisibility(false);
            }
            else
            {
                DwmBlurHelper.SetWindowsTaskbarVisibility(true);
            }

            int topOffset = (Config.ShowTopBar && TopBar != null) ? 32 : 0;
            // When dock is not auto-hiding, reserve bottom space so windows don't overlap
            int bottomOffset = (!Config.DockAutoHide && Config.ShowDock && Dock != null) ? 76 : 0;

            DwmBlurHelper.EnforceWorkArea(topOffset, bottomOffset);
        }

        private void SetupKeyboardHook()
        {
            if (!Config.InterceptWinKey) return;

            try
            {
                _keyboardProc = LowLevelKeyboardHookCallback;
                IntPtr hModule = Win32Api.GetModuleHandle(null);
                _hKeyboardHook = Win32Api.SetWindowsHookEx(NativeConstants.WH_KEYBOARD_LL, _keyboardProc, hModule, 0);
            }
            catch { }
        }

        private IntPtr LowLevelKeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && Config.InterceptWinKey)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                int msg = wParam.ToInt32();

                // VK_LWIN = 0x5B, VK_RWIN = 0x5C
                if (vkCode == 0x5B || vkCode == 0x5C)
                {
                    if (msg == 0x0100) // WM_KEYDOWN
                    {
                        _winKeyCandidate = true;
                        return (IntPtr)1; // Suppress default Windows Start Menu
                    }
                    else if (msg == 0x0101) // WM_KEYUP
                    {
                        if (_winKeyCandidate)
                        {
                            _winKeyCandidate = false;
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                ToggleLaunchpad();
                            }));
                            return (IntPtr)1;
                        }
                    }
                }
                else
                {
                    // Any other key pressed while Win is held down (e.g. Win+R, Win+E, Win+D)
                    _winKeyCandidate = false;
                }
            }

            return Win32Api.CallNextHookEx(_hKeyboardHook, nCode, wParam, lParam);
        }

        private void SetupGlobalHotkey()
        {
            try
            {
                HwndSourceParameters parameters = new HwndSourceParameters("TahoeHotkeySource")
                {
                    Width = 0,
                    Height = 0,
                    WindowStyle = 0x800000 // WS_BORDER invisible
                };
                _hotkeyHwndSource = new HwndSource(parameters);
                _hotkeyHwndSource.AddHook(HwndHook);

                // MOD_ALT = 0x0001, VK_SPACE = 0x20
                Win32Api.RegisterHotKey(_hotkeyHwndSource.Handle, HOTKEY_SPOTLIGHT_ID, 0x0001, 0x20);
            }
            catch { }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeConstants.WM_HOTKEY)
            {
                if (wParam.ToInt32() == HOTKEY_SPOTLIGHT_ID)
                {
                    ToggleSpotlight();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void ToggleLaunchpad()
        {
            if (Launchpad == null) return;
            if (Launchpad.IsVisible)
            {
                Launchpad.HideLaunchpad();
            }
            else
            {
                CloseTransientPopups();
                Launchpad.ShowLaunchpad();
            }
        }

        public void ToggleSpotlight()
        {
            if (Spotlight == null) return;
            if (Spotlight.IsVisible)
            {
                Spotlight.HideSpotlight();
            }
            else
            {
                CloseTransientPopups();
                Spotlight.ShowSpotlight();
            }
        }

        public void ToggleWindowSwitcher()
        {
            if (WindowSwitcher == null) return;
            if (WindowSwitcher.IsVisible)
            {
                WindowSwitcher.HideSwitcher();
            }
            else
            {
                CloseTransientPopups();
                WindowSwitcher.ShowSwitcher();
            }
        }

        public void ToggleControlCenter()
        {
            if (ControlCenter == null) return;
            if (ControlCenter.IsVisible)
            {
                ControlCenter.HideControlCenter();
            }
            else
            {
                CloseTransientPopups();
                ControlCenter.ShowControlCenter();
            }
        }

        public void ToggleCalendar()
        {
            if (CalendarWidget == null) return;
            if (CalendarWidget.IsVisible)
            {
                CalendarWidget.HideCalendar();
            }
            else
            {
                CloseTransientPopups();
                CalendarWidget.ShowCalendar();
            }
        }

        public void OpenSettings(int tabIndex = 0)
        {
            CloseTransientPopups();
            if (SettingsWindow == null || !SettingsWindow.IsLoaded)
            {
                SettingsWindow = new LauncherSettingsWindow();
            }
            SettingsWindow.SelectTab(tabIndex);
            SettingsWindow.Show();
            SettingsWindow.Activate();
        }

        public void OpenAbout()
        {
            CloseTransientPopups();
            if (AboutWindow == null || !AboutWindow.IsLoaded)
            {
                AboutWindow = new AboutSystemWindow();
            }
            AboutWindow.Show();
            AboutWindow.Activate();
        }

        public void CloseTransientPopups()
        {
            if (ControlCenter != null) ControlCenter.HideControlCenter();
            if (CalendarWidget != null) CalendarWidget.HideCalendar();
        }

        public void LaunchApp(string command, string args)
        {
            if (string.IsNullOrEmpty(command)) return;

            if (command.Equals("@launchpad", StringComparison.OrdinalIgnoreCase))
            {
                ToggleLaunchpad();
                return;
            }

            if (command.Equals("@windows", StringComparison.OrdinalIgnoreCase))
            {
                ToggleWindowSwitcher();
                return;
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args ?? "",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось запустить приложение:\n" + command + "\n\n" + ex.Message, "Tahoe Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void ShutdownLauncher()
        {
            if (_hKeyboardHook != IntPtr.Zero)
            {
                Win32Api.UnhookWindowsHookEx(_hKeyboardHook);
                _hKeyboardHook = IntPtr.Zero;
            }

            if (_hotkeyHwndSource != null)
            {
                Win32Api.UnregisterHotKey(_hotkeyHwndSource.Handle, HOTKEY_SPOTLIGHT_ID);
                _hotkeyHwndSource.Dispose();
                _hotkeyHwndSource = null;
            }

            DwmBlurHelper.SetWindowsTaskbarVisibility(true);
            DwmBlurHelper.RestoreWorkArea();

            Application.Current.Shutdown();
        }
    }
}
