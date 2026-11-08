using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using MacOSTahoe.Models;
using MacOSTahoe.Native;

namespace MacOSTahoe.Services
{
    public class WindowTrackerService
    {
        private static readonly HashSet<string> IgnoredClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Progman",
            "WorkerW",
            "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd",
            "Windows.UI.Core.CoreWindow",
            "ApplicationFrameWindow", // we handle UWP child or check title
            "TopBarWindow",
            "DockWindow",
            "LaunchpadWindow",
            "SpotlightWindow",
            "ControlCenterWindow",
            "CalendarWidgetWindow",
            "DesktopWidgetsWindow",
            "WindowSwitcherWindow",
            "AboutSystemWindow",
            "LauncherSettingsWindow"
        };

        public List<OpenWindowEntry> GetOpenWindows()
        {
            var windows = new List<OpenWindowEntry>();

            Win32Api.EnumWindows((hWnd, lParam) =>
            {
                if (!Win32Api.IsWindowVisible(hWnd)) return true;

                int exStyle = Win32Api.GetWindowLong(hWnd, NativeConstants.GWL_EXSTYLE);
                if ((exStyle & NativeConstants.WS_EX_TOOLWINDOW) != 0) return true;

                StringBuilder sbClass = new StringBuilder(256);
                Win32Api.GetClassName(hWnd, sbClass, sbClass.Capacity);
                string className = sbClass.ToString();

                if (IgnoredClasses.Contains(className)) return true;

                StringBuilder sbText = new StringBuilder(512);
                Win32Api.GetWindowText(hWnd, sbText, sbText.Capacity);
                string title = sbText.ToString().Trim();

                if (string.IsNullOrWhiteSpace(title)) return true;

                RECT rect;
                if (!Win32Api.GetWindowRect(hWnd, out rect)) return true;
                if (rect.Right - rect.Left < 60 || rect.Bottom - rect.Top < 60) return true;

                uint pid = 0;
                Win32Api.GetWindowThreadProcessId(hWnd, out pid);
                if (pid == 0) return true;

                string procName = "";
                string exePath = GetProcessPath(pid, out procName);

                ImageSource icon = null;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    icon = ShellComInterop.ExtractHighResIcon(exePath, 0);
                }

                windows.Add(new OpenWindowEntry
                {
                    Hwnd = hWnd,
                    Pid = pid,
                    ProcessName = procName,
                    AppTitle = !string.IsNullOrEmpty(procName) ? procName : title,
                    WindowTitle = title,
                    IsMinimized = Win32Api.IsIconic(hWnd),
                    Icon = icon
                });

                return true;
            }, IntPtr.Zero);

            return windows;
        }

        public static string GetProcessPath(uint pid, out string processName)
        {
            processName = "";
            IntPtr hProcess = Win32Api.OpenProcess(NativeConstants.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (hProcess == IntPtr.Zero) return "";

            try
            {
                StringBuilder sb = new StringBuilder(1024);
                int size = sb.Capacity;
                if (Win32Api.QueryFullProcessImageNameW(hProcess, 0, sb, ref size))
                {
                    string fullPath = sb.ToString();
                    processName = Path.GetFileNameWithoutExtension(fullPath);
                    return fullPath;
                }
            }
            finally
            {
                Win32Api.CloseHandle(hProcess);
            }
            return "";
        }

        public static void ActivateWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;

            if (Win32Api.IsIconic(hWnd))
            {
                Win32Api.ShowWindow(hWnd, NativeConstants.SW_RESTORE);
            }

            Win32Api.SetForegroundWindow(hWnd);
            Win32Api.SwitchToThisWindow(hWnd, true);
        }

        public static void MinimizeWindow(IntPtr hWnd)
        {
            if (hWnd != IntPtr.Zero)
            {
                Win32Api.ShowWindow(hWnd, NativeConstants.SW_SHOWMINIMIZED);
            }
        }

        public static void CloseWindow(IntPtr hWnd)
        {
            if (hWnd != IntPtr.Zero)
            {
                Win32Api.PostMessage(hWnd, NativeConstants.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }
    }
}
