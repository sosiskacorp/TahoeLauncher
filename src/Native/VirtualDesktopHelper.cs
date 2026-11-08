using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace MacOSTahoe
{
    public static class VirtualDesktopHelper
    {
        public static event Action DesktopChanged;

        public static int GetDesktopCount()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops"))
                {
                    if (key != null)
                    {
                        byte[] ids = key.GetValue("VirtualDesktopIDs") as byte[];
                        if (ids != null && ids.Length >= 16)
                        {
                            return Math.Max(1, ids.Length / 16);
                        }
                    }
                }
            }
            catch { }
            return 1;
        }

        public static int GetCurrentDesktopIndex()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops"))
                {
                    if (key != null)
                    {
                        byte[] ids = key.GetValue("VirtualDesktopIDs") as byte[];
                        byte[] cur = key.GetValue("CurrentVirtualDesktop") as byte[];
                        if (ids != null && cur != null && ids.Length >= 16 && cur.Length == 16)
                        {
                            int count = ids.Length / 16;
                            for (int i = 0; i < count; i++)
                            {
                                bool match = true;
                                for (int b = 0; b < 16; b++)
                                {
                                    if (ids[i * 16 + b] != cur[b]) { match = false; break; }
                                }
                                if (match) return i;
                            }
                        }
                    }
                }
            }
            catch { }
            return 0;
        }

        public static List<string> GetDesktopNames()
        {
            List<string> list = new List<string>();
            try
            {
                int count = GetDesktopCount();
                for (int i = 0; i < count; i++)
                {
                    list.Add("Рабочий стол " + (i + 1).ToString());
                }
            }
            catch { }
            if (list.Count == 0) list.Add("Рабочий стол 1");
            return list;
        }

        public static void CreateNewDesktop()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                // Win + Ctrl + D creates a new virtual desktop in Windows
                NativeMethods.keybd_event(0x5B, 0, 0, UIntPtr.Zero);
                NativeMethods.keybd_event(0x11, 0, 0, UIntPtr.Zero);
                NativeMethods.keybd_event(0x44, 0, 0, UIntPtr.Zero);
                Thread.Sleep(50);
                NativeMethods.keybd_event(0x44, 0, 2, UIntPtr.Zero);
                NativeMethods.keybd_event(0x11, 0, 2, UIntPtr.Zero);
                NativeMethods.keybd_event(0x5B, 0, 2, UIntPtr.Zero);

                Thread.Sleep(300);
                if (DesktopChanged != null) DesktopChanged();
            });
        }

        public static void CloseCurrentDesktop()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                // Win + Ctrl + F4 closes the current virtual desktop in Windows
                NativeMethods.keybd_event(0x5B, 0, 0, UIntPtr.Zero);
                NativeMethods.keybd_event(0x11, 0, 0, UIntPtr.Zero);
                NativeMethods.keybd_event(0x73, 0, 0, UIntPtr.Zero);
                Thread.Sleep(50);
                NativeMethods.keybd_event(0x73, 0, 2, UIntPtr.Zero);
                NativeMethods.keybd_event(0x11, 0, 2, UIntPtr.Zero);
                NativeMethods.keybd_event(0x5B, 0, 2, UIntPtr.Zero);

                Thread.Sleep(300);
                if (DesktopChanged != null) DesktopChanged();
            });
        }

        public static void SwitchToDesktop(int targetIndex)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                int current = GetCurrentDesktopIndex();
                int count = GetDesktopCount();

                // If target desktop does not exist in Windows, create enough desktops!
                if (targetIndex >= count)
                {
                    int toCreate = targetIndex - count + 1;
                    for (int i = 0; i < toCreate; i++)
                    {
                        NativeMethods.keybd_event(0x5B, 0, 0, UIntPtr.Zero);
                        NativeMethods.keybd_event(0x11, 0, 0, UIntPtr.Zero);
                        NativeMethods.keybd_event(0x44, 0, 0, UIntPtr.Zero);
                        Thread.Sleep(50);
                        NativeMethods.keybd_event(0x44, 0, 2, UIntPtr.Zero);
                        NativeMethods.keybd_event(0x11, 0, 2, UIntPtr.Zero);
                        NativeMethods.keybd_event(0x5B, 0, 2, UIntPtr.Zero);
                        Thread.Sleep(200);
                    }
                    if (DesktopChanged != null) DesktopChanged();
                    return;
                }

                if (targetIndex == current) return;

                if (targetIndex > current)
                {
                    int steps = targetIndex - current;
                    for (int s = 0; s < steps; s++)
                    {
                        NativeMethods.keybd_event(0x5B, 0, 0, UIntPtr.Zero);
                        NativeMethods.keybd_event(0x11, 0, 0, UIntPtr.Zero);
                        NativeMethods.keybd_event(0x27, 0, 0, UIntPtr.Zero); // Right
                        Thread.Sleep(50);
                        NativeMethods.keybd_event(0x27, 0, 2, UIntPtr.Zero);
                        NativeMethods.keybd_event(0x11, 0, 2, UIntPtr.Zero);
                        NativeMethods.keybd_event(0x5B, 0, 2, UIntPtr.Zero);
                        Thread.Sleep(150);
                    }
                }
                else
                {
                    int steps = current - targetIndex;
                    for (int s = 0; s < steps; s++)
                    {
                        NativeMethods.keybd_event(0x5B, 0, 0, UIntPtr.Zero);
                        NativeMethods.keybd_event(0x11, 0, 0, UIntPtr.Zero);
                        NativeMethods.keybd_event(0x25, 0, 0, UIntPtr.Zero); // Left
                        Thread.Sleep(50);
                        NativeMethods.keybd_event(0x25, 0, 2, UIntPtr.Zero);
                        NativeMethods.keybd_event(0x11, 0, 2, UIntPtr.Zero);
                        NativeMethods.keybd_event(0x5B, 0, 2, UIntPtr.Zero);
                        Thread.Sleep(150);
                    }
                }

                Thread.Sleep(200);
                if (DesktopChanged != null) DesktopChanged();
            });
        }
    }
}
