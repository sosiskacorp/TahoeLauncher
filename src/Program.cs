using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using MacOSTahoe.Core;
using MacOSTahoe.Native;

namespace MacOSTahoe
{
    public static class Program
    {
        private const string MutexName = "Local\\MacOSTahoeLauncher_SingleInstance";

        [STAThread]
        public static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                string firstArg = args[0].ToLowerInvariant();
                if (firstArg == "/kill" || firstArg == "/stop" || firstArg == "--kill")
                {
                    KillRunningInstances();
                    DwmBlurHelper.SetWindowsTaskbarVisibility(true);
                    DwmBlurHelper.RestoreWorkArea();
                    return;
                }
                else if (firstArg == "/restart" || firstArg == "--restart")
                {
                    KillRunningInstances(true);
                    Thread.Sleep(600);
                }
            }

            Mutex mutex = null;
            bool hasHandle = false;
            try
            {
                mutex = new Mutex(false, MutexName);
                try
                {
                    hasHandle = mutex.WaitOne(300, false);
                }
                catch (AbandonedMutexException)
                {
                    hasHandle = true;
                }

                if (!hasHandle)
                {
                    return;
                }

                try
                {
                    Win32Api.SetProcessDpiAwareness(2);
                }
                catch
                {
                    try { Win32Api.SetProcessDPIAware(); } catch { }
                }

                var app = new Application
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };

                var controller = new ShellController();
                controller.Initialize(app);

                if (controller.TopBar != null)
                {
                    app.MainWindow = controller.TopBar;
                }

                app.Run();
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(@"C:\Win11_Prep\macos\crash.log", DateTime.Now + ": " + ex + "\r\n");
                }
                catch { }
            }
            finally
            {
                if (hasHandle && mutex != null)
                {
                    try { mutex.ReleaseMutex(); } catch { }
                }
                if (mutex != null)
                {
                    mutex.Dispose();
                }
            }
        }

        private static void KillRunningInstances(bool excludeSelf = false)
        {
            int currentPid = Process.GetCurrentProcess().Id;
            var running = Process.GetProcessesByName("MacOSTahoe");
            foreach (var p in running)
            {
                try
                {
                    if (excludeSelf && p.Id == currentPid) continue;
                    p.Kill();
                    p.WaitForExit(1500);
                }
                catch { }
            }
        }
    }
}
