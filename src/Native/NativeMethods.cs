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

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

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

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

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
}
