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
                NativeMethods.SystemParametersInfoRect(0x002F, 0, ref fullRect, 0);
                return;
            }

            Mutex mutex = null;
            bool hasHandle = false;
            try
            {
                mutex = new Mutex(false, "Local\\MacOSTahoeLauncher_SingleInstance");
                try { hasHandle = mutex.WaitOne(300, false); }
                catch (AbandonedMutexException) { hasHandle = true; }

                if (!hasHandle) return;

                Application app = new Application();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                ShellController controller = new ShellController();
                controller.Start(args);
                app.Run();
            }
            finally
            {
                if (hasHandle && mutex != null)
                {
                    try { mutex.ReleaseMutex(); } catch { }
                }
                if (mutex != null) mutex.Dispose();
            }
        }
    }
}
