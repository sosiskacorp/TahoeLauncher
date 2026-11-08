using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace MacOSTahoe
{
    public static class SnapHelper
    {
        // Trigger Windows 11 native Snap Layouts menu on target or foreground window
        public static void TriggerSnapLayouts(IntPtr targetHwnd = default(IntPtr))
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                if (targetHwnd != IntPtr.Zero && NativeMethods.IsWindow(targetHwnd))
                {
                    NativeMethods.ForceForegroundWindow(targetHwnd);
                    Thread.Sleep(100);
                }

                // Win + Z
                NativeMethods.keybd_event(0x5B, 0, 0, UIntPtr.Zero);
                NativeMethods.keybd_event(0x5A, 0, 0, UIntPtr.Zero);
                Thread.Sleep(40);
                NativeMethods.keybd_event(0x5A, 0, 2, UIntPtr.Zero);
                NativeMethods.keybd_event(0x5B, 0, 2, UIntPtr.Zero);
            });
        }

        // Snap target window to left half (triggers Windows native Snap Assist on the right)
        public static void SnapLeft(IntPtr targetHwnd = default(IntPtr))
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                if (targetHwnd != IntPtr.Zero && NativeMethods.IsWindow(targetHwnd))
                {
                    NativeMethods.ForceForegroundWindow(targetHwnd);
                    Thread.Sleep(100);
                }

                // Win + Left
                NativeMethods.keybd_event(0x5B, 0, 0, UIntPtr.Zero);
                NativeMethods.keybd_event(0x25, 0, 0, UIntPtr.Zero);
                Thread.Sleep(40);
                NativeMethods.keybd_event(0x25, 0, 2, UIntPtr.Zero);
                NativeMethods.keybd_event(0x5B, 0, 2, UIntPtr.Zero);
            });
        }

        // Snap target window to right half (triggers Windows native Snap Assist on the left)
        public static void SnapRight(IntPtr targetHwnd = default(IntPtr))
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                if (targetHwnd != IntPtr.Zero && NativeMethods.IsWindow(targetHwnd))
                {
                    NativeMethods.ForceForegroundWindow(targetHwnd);
                    Thread.Sleep(100);
                }

                // Win + Right
                NativeMethods.keybd_event(0x5B, 0, 0, UIntPtr.Zero);
                NativeMethods.keybd_event(0x27, 0, 0, UIntPtr.Zero);
                Thread.Sleep(40);
                NativeMethods.keybd_event(0x27, 0, 2, UIntPtr.Zero);
                NativeMethods.keybd_event(0x5B, 0, 2, UIntPtr.Zero);
            });
        }

        // Snap two windows side by side 50/50 using native desktop work area
        public static void SnapTwoWindows(IntPtr leftHwnd, IntPtr rightHwnd)
        {
            try
            {
                Rect work = SystemParameters.WorkArea;
                int halfW = (int)(work.Width / 2.0);
                int h = (int)work.Height;
                int top = (int)work.Top;
                int leftX = (int)work.Left;
                int rightX = leftX + halfW;

                if (leftHwnd != IntPtr.Zero && NativeMethods.IsWindow(leftHwnd))
                {
                    if (NativeMethods.IsIconic(leftHwnd)) NativeMethods.ShowWindow(leftHwnd, NativeMethods.SW_RESTORE);
                    NativeMethods.MoveWindow(leftHwnd, leftX, top, halfW, h, true);
                    NativeMethods.ForceForegroundWindow(leftHwnd);
                }
                if (rightHwnd != IntPtr.Zero && NativeMethods.IsWindow(rightHwnd))
                {
                    if (NativeMethods.IsIconic(rightHwnd)) NativeMethods.ShowWindow(rightHwnd, NativeMethods.SW_RESTORE);
                    NativeMethods.MoveWindow(rightHwnd, rightX, top, halfW, h, true);
                    NativeMethods.ForceForegroundWindow(rightHwnd);
                }
            }
            catch { }
        }
    }
}
