using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace MacOSTahoe.Models
{
    public class DockAppItem
    {
        public string Id;
        public string Name;
        public string IconFile;
        public string LaunchCommand;
        public string LaunchArgs;
        public string[] ProcessNames;
        public bool IsPinned;
        public bool IsSeparator;
        public bool IsTrash;
        public bool IsAppsLauncher;
        public bool IsDesktopFolder;
        public bool IsExplorer;
        public bool IsWindowSwitcher;
        public bool IsDynamic;
        public ImageSource CustomImageSource;

        public IntPtr ActiveHwnd;
        public bool IsRunning;

        // UI & deterministic wave physics state
        public Border RootContainer;
        public Grid IconWrapper;
        public Image IconImage;
        public Border RunningDot;
        public double CurrentSize = 50.0;
        public double TargetSize = 50.0;
        public double CurrentLift = 0.0;
        public double TargetLift = 0.0;
        public double BounceStartTime = -100.0;
    }
}
