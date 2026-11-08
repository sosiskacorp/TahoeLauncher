using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using MacOSTahoe.Native;

namespace MacOSTahoe.Native
{
    public static class DwmBlurHelper
    {
        public static void ApplyBlur(IntPtr hwnd, bool isDark)
        {
            try
            {
                var accent = new AccentPolicy
                {
                    AccentState = 3, // ACCENT_ENABLE_BLURBEHIND
                    AccentFlags = 2,
                    GradientColor = isDark ? 0x99181820 : 0x99F0F0F5,
                    AnimationId = 0
                };

                int accentStructSize = Marshal.SizeOf(accent);
                IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData
                {
                    Attribute = 19, // WCA_ACCENT_POLICY
                    Data = accentPtr,
                    SizeOfData = accentStructSize
                };

                Win32Api.SetWindowCompositionAttribute(hwnd, ref data);
                Marshal.FreeHGlobal(accentPtr);
            }
            catch { }
        }

        public static void MakeToolWindow(IntPtr hwnd, bool noActivate = false)
        {
            try
            {
                int exStyle = Win32Api.GetWindowLong(hwnd, NativeConstants.GWL_EXSTYLE);
                exStyle |= NativeConstants.WS_EX_TOOLWINDOW;
                if (noActivate)
                {
                    exStyle |= NativeConstants.WS_EX_NOACTIVATE;
                }
                Win32Api.SetWindowLong(hwnd, NativeConstants.GWL_EXSTYLE, exStyle);
            }
            catch { }
        }

        public static void SetWindowsTaskbarVisibility(bool visible)
        {
            try
            {
                IntPtr tray = Win32Api.FindWindow("Shell_TrayWnd", null);
                if (tray != IntPtr.Zero)
                {
                    Win32Api.ShowWindow(tray, visible ? NativeConstants.SW_SHOWNORMAL : NativeConstants.SW_HIDE);
                }

                IntPtr secTray = Win32Api.FindWindow("Shell_SecondaryTrayWnd", null);
                while (secTray != IntPtr.Zero)
                {
                    Win32Api.ShowWindow(secTray, visible ? NativeConstants.SW_SHOWNORMAL : NativeConstants.SW_HIDE);
                    secTray = Win32Api.FindWindowEx(IntPtr.Zero, secTray, "Shell_SecondaryTrayWnd", null);
                }
            }
            catch { }
        }

        public static void EnforceWorkArea(int topOffset, int bottomOffset)
        {
            try
            {
                RECT cur = new RECT();
                if (Win32Api.SystemParametersInfoRect(NativeConstants.SPI_GETWORKAREA, 0, ref cur, 0))
                {
                    int screenW = (int)SystemParameters.PrimaryScreenWidth;
                    int screenH = (int)SystemParameters.PrimaryScreenHeight;
                    int targetTop = topOffset;
                    int targetBottom = screenH - bottomOffset;

                    // Only update if work area actually differs to prevent redundant recalculations
                    if (cur.Top != targetTop || cur.Bottom != targetBottom || cur.Left != 0 || cur.Right != screenW)
                    {
                        RECT newRect = new RECT
                        {
                            Left = 0,
                            Top = targetTop,
                            Right = screenW,
                            Bottom = targetBottom
                        };
                        // CRITICAL: fWinIni MUST BE 0!
                        // Setting flag to 2 (SPIF_SENDCHANGE) synchronously broadcasts WM_SETTINGCHANGE
                        // to every window on the desktop. During Windows boot this hangs the UI thread dead!
                        Win32Api.SystemParametersInfoRect(NativeConstants.SPI_SETWORKAREA, 0, ref newRect, 0);
                    }
                }
            }
            catch { }
        }

        public static void RestoreWorkArea()
        {
            try
            {
                int screenW = (int)SystemParameters.PrimaryScreenWidth;
                int screenH = (int)SystemParameters.PrimaryScreenHeight;
                RECT r = new RECT { Left = 0, Top = 0, Right = screenW, Bottom = screenH };
                Win32Api.SystemParametersInfoRect(NativeConstants.SPI_SETWORKAREA, 0, ref r, 0);
            }
            catch { }
        }
    }
}
