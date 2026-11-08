using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MacOSTahoe.Native
{
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

    public static class ShellComInterop
    {
        public static void ResolveShortcutFile(string lnkPath, out string target, out string args, out string iconPath, out int iconIndex)
        {
            target = "";
            args = "";
            iconPath = "";
            iconIndex = 0;
            try
            {
                ShellLink link = new ShellLink();
                ((IPersistFile)link).Load(lnkPath, 0);
                IShellLinkW sl = (IShellLinkW)link;
                StringBuilder sbPath = new StringBuilder(512);
                // SLGP_RAWPATH = 4 avoids slow network/MSI resolution stalls
                sl.GetPath(sbPath, sbPath.Capacity, IntPtr.Zero, 0);
                target = sbPath.ToString();
                StringBuilder sbArgs = new StringBuilder(512);
                sl.GetArguments(sbArgs, sbArgs.Capacity);
                args = sbArgs.ToString();
                StringBuilder sbIcon = new StringBuilder(512);
                sl.GetIconLocation(sbIcon, sbIcon.Capacity, out iconIndex);
                iconPath = sbIcon.ToString();
                Marshal.ReleaseComObject(link);
            }
            catch { }
        }

        public static ImageSource ExtractHighResIcon(string filePath, int iconIndex)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

            try
            {
                IntPtr hLarge = IntPtr.Zero;
                IntPtr hSmall = IntPtr.Zero;
                int hr = Win32Api.SHDefExtractIconW(filePath, iconIndex, 0, out hLarge, out hSmall, 256);
                if (hSmall != IntPtr.Zero) Win32Api.DestroyIcon(hSmall);
                if (hr == 0 && hLarge != IntPtr.Zero)
                {
                    ImageSource src = Imaging.CreateBitmapSourceFromHIcon(
                        hLarge,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    src.Freeze();
                    Win32Api.DestroyIcon(hLarge);
                    return src;
                }
                if (hLarge != IntPtr.Zero) Win32Api.DestroyIcon(hLarge);
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
                    return src;
                }
            }
            catch { }

            return null;
        }
    }
}
