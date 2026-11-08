using System;
using System.Collections.Generic;

namespace MacOSTahoe.Models
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
            DockIconSize = 50.0;
            DockZoomRange = 26.0;
            QuickNoteText = "• Win — Все приложения\r\n• Alt+Пробел — Командный поиск\r\n• ПКМ по иконкам — Настройка Dock";
            FavoriteApps = new List<string>();
            HiddenApps = new List<string>();
            CustomApps = new List<CustomAppConfig>();
            PinnedDockItems = new List<DockItemConfig>();
        }
    }
}
