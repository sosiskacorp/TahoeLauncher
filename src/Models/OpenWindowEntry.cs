using System;
using System.Windows.Media;

namespace MacOSTahoe.Models
{
    public class OpenWindowEntry
    {
        public IntPtr Hwnd;
        public uint Pid;
        public string ProcessName;
        public string AppTitle;
        public string WindowTitle;
        public bool IsMinimized;
        public ImageSource Icon;
    }
}
