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
    public class DockItemConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string IconFile { get; set; }
        public string LaunchCommand { get; set; }
        public string LaunchArgs { get; set; }
        public string[] ProcessNames { get; set; }
        public bool IsSeparator { get; set; }
        public bool IsTrash { get; set; }
        public bool IsAppsLauncher { get; set; }
        public bool IsDesktopFolder { get; set; }
        public bool IsExplorer { get; set; }
        public bool IsWindowSwitcher { get; set; }
    }

    public class CustomAppConfig
    {
        public string Title { get; set; }
        public string Cmd { get; set; }
        public string Args { get; set; }
        public string Category { get; set; }
    }

    public class LauncherConfig
    {
        public bool ShowTopBar { get; set; }
        public bool ShowDock { get; set; }
        public bool ShowDesktopWidgets { get; set; }
        public bool HideWindowsTaskbar { get; set; }
        public bool DockAutoHide { get; set; }
        public bool InterceptWinKey { get; set; }
        public bool IsDarkMode { get; set; }
        public bool EnableStageManager { get; set; }
        public bool StageManagerAutoHide { get; set; }
        public bool StageManagerShowDesktopIcons { get; set; }
        public bool StageManagerGroupSameApp { get; set; }
        public bool StageManagerAutoCenter { get; set; }
        public List<string> Spaces { get; set; }
        public int CurrentSpaceIndex { get; set; }
        public List<string> SpaceWallpapers { get; set; }
        public Dictionary<string, int> AppSpaceAssignments { get; set; }
        public bool MissionControlGroupByApp { get; set; }
        public bool EnableHotCorners { get; set; }
        public string HotCornerTopLeft { get; set; }
        public string HotCornerTopRight { get; set; }
        public string HotCornerBottomLeft { get; set; }
        public string HotCornerBottomRight { get; set; }
        public double DockIconSize { get; set; }
        public double DockZoomRange { get; set; }
        public string QuickNoteText { get; set; }
        public List<string> FavoriteApps { get; set; }
        public List<string> HiddenApps { get; set; }
        public List<CustomAppConfig> CustomApps { get; set; }
        public List<DockItemConfig> PinnedDockItems { get; set; }

        public LauncherConfig()
        {
            ShowTopBar = true;
            ShowDock = true;
            ShowDesktopWidgets = true;
            HideWindowsTaskbar = true;
            DockAutoHide = true;
            InterceptWinKey = true;
            IsDarkMode = true;
            EnableStageManager = false;
            StageManagerAutoHide = false;
            StageManagerShowDesktopIcons = false;
            StageManagerGroupSameApp = true;
            StageManagerAutoCenter = false;
            Spaces = new List<string> { "Рабочий стол 1", "Рабочий стол 2" };
            CurrentSpaceIndex = 0;
            SpaceWallpapers = new List<string>();
            AppSpaceAssignments = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            MissionControlGroupByApp = false;
            EnableHotCorners = true;
            HotCornerTopLeft = "Mission Control";
            HotCornerTopRight = "Пункт управления";
            HotCornerBottomLeft = "Приложения";
            HotCornerBottomRight = "Рабочий стол";
            DockIconSize = 50.0;
            DockZoomRange = 26.0;
            QuickNoteText = "• Win — Все приложения\r\n• Alt+Пробел — Командный поиск\r\n• ПКМ по иконкам — Настройка Dock";
            FavoriteApps = new List<string>();
            HiddenApps = new List<string>();
            CustomApps = new List<CustomAppConfig>();
            PinnedDockItems = new List<DockItemConfig>();
        }
    }

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

        // UI & physics state
        public Border RootContainer;
        public Grid IconWrapper;
        public Image IconImage;
        public ScaleTransform IconScaleTransform;
        public TranslateTransform IconTranslateTransform;
        public Border RunningDot;
        public double CurrentSize = 50.0;
        public double TargetSize = 50.0;
        public double CurrentLift = 0.0;
        public double TargetLift = 0.0;
        public double BounceStartTime = -100.0;
    }

    public class AppCatalogEntry
    {
        public string Title;
        public string Cmd;
        public string Args;
        public string VerifyPath;
        public string Category;
        public ImageSource IconSource;
    }

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

    // =========================================================================
    // CORE SHELL & LAUNCHER CONTROLLER
    // =========================================================================

    public class StageGroup
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string ProcessName { get; set; }
        public List<OpenWindowEntry> Windows { get; set; }
        public ImageSource Icon { get; set; }

        public StageGroup()
        {
            Windows = new List<OpenWindowEntry>();
        }

        public bool HasHwnd(IntPtr hwnd)
        {
            if (Windows == null) return false;
            for (int i = 0; i < Windows.Count; i++)
            {
                if (Windows[i].Hwnd == hwnd) return true;
            }
            return false;
        }
    }
}