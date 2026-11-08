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
                    string a = args[i].ToLowerInvariant();
                    if (a == "--quit" || a == "-x" || a == "--restore" || a == "-r")
                    {
                        quitOnly = true;
                    }
                }
            }

            // P0: Register emergency crash recovery handlers to restore Taskbar & WorkArea
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
            {
                NativeMethods.RestoreAllNativeShell();
            };
            AppDomain.CurrentDomain.ProcessExit += delegate(object s, EventArgs e)
            {
                if (ShellController.Instance != null && !ShellController.Instance.IsCleanShutdown)
                {
                    NativeMethods.RestoreAllNativeShell();
                }
            };

            int myPid = Process.GetCurrentProcess().Id;
            foreach (Process p in Process.GetProcessesByName("MacOSTahoe"))
            {
                if (p.Id != myPid)
                {
                    try { p.Kill(); } catch { }
                    try { p.Dispose(); } catch { }
                }
            }

            if (quitOnly)
            {
                NativeMethods.RestoreAllNativeShell();
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
                app.DispatcherUnhandledException += delegate(object s, DispatcherUnhandledExceptionEventArgs e)
                {
                    NativeMethods.RestoreAllNativeShell();
                };

                NetworkHelper.Initialize();
                BluetoothHelper.Initialize();
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
