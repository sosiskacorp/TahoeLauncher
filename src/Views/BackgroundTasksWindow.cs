using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using IOPath = System.IO.Path;
using Microsoft.Win32;

namespace MacOSTahoe
{
    public class BackgroundAppItem
    {
        public int Pid;
        public string ProcessName;
        public string DisplayName;
        public string ExePath;
        public long MemoryMb;
        public ImageSource Icon;
        public IntPtr Hwnd;
    }

    public class BackgroundTasksWindow : Window
    {
        private StackPanel _itemsListPanel;
        private TextBlock _countText;
        private TextBlock _taskbarToggleText;

        public BackgroundTasksWindow()
        {
            Title = "Фоновые задачи и системный трей";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Width = 400;
            Height = 560;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();

            Deactivated += delegate(object s, EventArgs e)
            {
                Hide();
            };

            SourceInitialized += delegate(object s, EventArgs e)
            {
                NativeMethods.MakeToolWindow(this, true);
                // Do NOT call EnableBlur here: in transparent WPF windows DWM produces a pitch-black rectangular border
            };
        }

        private void BuildUI()
        {
            Border card = new Border();
            card.Margin = new Thickness(12);
            card.CornerRadius = new CornerRadius(22);
            card.Background = LiquidGlassTheme.CreateGlassBrush(228, 242);
            card.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush();
            card.BorderThickness = new Thickness(1);
            card.Padding = new Thickness(16, 14, 16, 14);
            card.Effect = new DropShadowEffect
            {
                BlurRadius = 28,
                ShadowDepth = 8,
                Opacity = 0.58,
                Color = Colors.Black
            };

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Header
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Native Taskbar Card
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Sub-header
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 3: Apps Scroll
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4: Footer Links

            // --- 0. HEADER ---
            Grid header = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titleCol = new StackPanel();
            TextBlock title = new TextBlock
            {
                Text = "Фоновые задачи и трей",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            _countText = new TextBlock
            {
                Text = "Загрузка процессов...",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromArgb(175, 235, 235, 245)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            titleCol.Children.Add(title);
            titleCol.Children.Add(_countText);
            Grid.SetColumn(titleCol, 0);
            header.Children.Add(titleCol);

            Border taskmgrBtn = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 5, 10, 5),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            taskmgrBtn.Child = new TextBlock
            {
                Text = "Диспетчер задач",
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White
            };
            taskmgrBtn.MouseEnter += delegate { taskmgrBtn.Background = LiquidGlassTheme.AccentBlueBrush; };
            taskmgrBtn.MouseLeave += delegate { taskmgrBtn.Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)); };
            taskmgrBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                Hide();
                ShellController.RunProcess("taskmgr.exe", "");
            };
            Grid.SetColumn(taskmgrBtn, 1);
            header.Children.Add(taskmgrBtn);

            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // --- 1. NATIVE TASKBAR / TRAY CONTROLS ---
            Border trayCard = new Border
            {
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromArgb(36, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 12)
            };

            StackPanel trayStack = new StackPanel();
            TextBlock trayTitle = new TextBlock
            {
                Text = "Панель задач и область уведомлений Windows",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 3)
            };
            TextBlock trayDesc = new TextBlock
            {
                Text = "Быстрый переключатель нативной панели задач и системных настроек трея",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(160, 235, 235, 245)),
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };
            trayStack.Children.Add(trayTitle);
            trayStack.Children.Add(trayDesc);

            Grid trayBtns = new Grid();
            trayBtns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            trayBtns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            trayBtns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border toggleBarBtn = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = LiquidGlassTheme.AccentBlueBrush,
                Padding = new Thickness(8, 6, 8, 6),
                Cursor = Cursors.Hand
            };
            _taskbarToggleText = new TextBlock
            {
                Text = "Показать панель задач",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            toggleBarBtn.Child = _taskbarToggleText;
            toggleBarBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                NativeMethods.ToggleNativeTaskbar();
                UpdateTaskbarToggleText();
            };
            Grid.SetColumn(toggleBarBtn, 0);
            trayBtns.Children.Add(toggleBarBtn);

            Border settingsTrayBtn = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 6, 8, 6),
                Cursor = Cursors.Hand
            };
            settingsTrayBtn.Child = new TextBlock
            {
                Text = "Параметры трея",
                FontSize = 11.5,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            settingsTrayBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                Hide();
                ShellController.RunProcess("explorer.exe", "ms-settings:taskbar");
            };
            Grid.SetColumn(settingsTrayBtn, 2);
            trayBtns.Children.Add(settingsTrayBtn);

            trayStack.Children.Add(trayBtns);
            trayCard.Child = trayStack;

            Grid.SetRow(trayCard, 1);
            root.Children.Add(trayCard);

            // --- 2. SUB-HEADER ---
            Grid subHeader = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            subHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            subHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock subTitle = new TextBlock
            {
                Text = "Активные программы в трее",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };
            Grid.SetColumn(subTitle, 0);
            subHeader.Children.Add(subTitle);

            TextBlock refreshBtn = new TextBlock
            {
                Text = "⟳ Обновить",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(64, 156, 255)),
                Cursor = Cursors.Hand
            };
            refreshBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                RefreshTasks();
            };
            Grid.SetColumn(refreshBtn, 1);
            subHeader.Children.Add(refreshBtn);

            Grid.SetRow(subHeader, 2);
            root.Children.Add(subHeader);

            // --- 3. APPS SCROLLVIEWER ---
            ScrollViewer sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 0, 0, 10)
            };
            _itemsListPanel = new StackPanel();
            sv.Content = _itemsListPanel;

            Grid.SetRow(sv, 3);
            root.Children.Add(sv);

            // --- 4. FOOTER LINKS ---
            Grid footer = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            footer.Children.Add(CreateFooterChip("Автозагрузка", 0, delegate()
            {
                Hide();
                ShellController.RunProcess("explorer.exe", "ms-settings:startupapps");
            }));
            footer.Children.Add(CreateFooterChip("Службы", 2, delegate()
            {
                Hide();
                ShellController.RunProcess("services.msc", "");
            }));
            footer.Children.Add(CreateFooterChip("Параметры", 4, delegate()
            {
                Hide();
                ShellController.RunProcess("explorer.exe", "ms-settings:privacy-backgroundapps");
            }));

            Grid.SetRow(footer, 4);
            root.Children.Add(footer);

            card.Child = root;
            Content = card;
        }

        private Border CreateFooterChip(string label, int col, Action onClick)
        {
            Border b = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                Padding = new Thickness(4, 5, 4, 5),
                Cursor = Cursors.Hand
            };
            b.Child = new TextBlock
            {
                Text = label,
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 235, 235, 245)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            b.MouseEnter += delegate { b.Background = new SolidColorBrush(Color.FromArgb(65, 255, 255, 255)); };
            b.MouseLeave += delegate { b.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)); };
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };
            Grid.SetColumn(b, col);
            return b;
        }

        private void UpdateTaskbarToggleText()
        {
            if (_taskbarToggleText == null) return;
            bool vis = NativeMethods.IsNativeTaskbarVisible();
            _taskbarToggleText.Text = vis ? "Скрыть панель задач" : "Показать панель задач";
        }

        private static string ResolveKnownFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);

            path = path.Replace("{6D809377-6AF0-444B-8957-A3773F02200E}", progFiles);
            path = path.Replace("{7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E}", progFilesX86);
            path = path.Replace("{F38BF404-1D43-42F2-9305-67DE0B28FC23}", winDir);
            path = path.Replace("{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}", sys32);
            return path;
        }

        private static ImageSource ImageFromPngBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            try
            {
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }
            }
            catch { }
            return null;
        }

        private static ImageSource ExtractIconFromExe(string exePath)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) return null;
            try
            {
                using (System.Drawing.Icon ico = System.Drawing.Icon.ExtractAssociatedIcon(exePath))
                {
                    if (ico != null)
                    {
                        BitmapSource bs = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        bs.Freeze();
                        NativeMethods.DestroyIcon(ico.Handle);
                        return bs;
                    }
                }
            }
            catch { }
            return null;
        }

        private static string FormatFriendlyName(string exeName, string tip, string exePath)
        {
            // Known friendly mappings
            if (string.Equals(exeName, "MSIAfterburner", StringComparison.OrdinalIgnoreCase)) return "MSI Afterburner";
            if (string.Equals(exeName, "wallpaper32", StringComparison.OrdinalIgnoreCase) || string.Equals(exeName, "wallpaper64", StringComparison.OrdinalIgnoreCase)) return "Wallpaper Engine";
            if (string.Equals(exeName, "DCv2", StringComparison.OrdinalIgnoreCase)) return "MSI Center";
            if (string.Equals(exeName, "sunshine", StringComparison.OrdinalIgnoreCase)) return "Sunshine (Game Stream)";
            if (string.Equals(exeName, "steam", StringComparison.OrdinalIgnoreCase)) return "Steam";
            if (string.Equals(exeName, "Discord", StringComparison.OrdinalIgnoreCase)) return "Discord";
            if (string.Equals(exeName, "iMe", StringComparison.OrdinalIgnoreCase)) return "Telegram (iMe)";
            if (string.Equals(exeName, "Telegram", StringComparison.OrdinalIgnoreCase)) return "Telegram Desktop";
            if (string.Equals(exeName, "AnyDesk", StringComparison.OrdinalIgnoreCase)) return "AnyDesk";
            if (string.Equals(exeName, "fixnet", StringComparison.OrdinalIgnoreCase)) return "Fixnet";
            if (string.Equals(exeName, "SCSKiller", StringComparison.OrdinalIgnoreCase)) return "SCSKiller";
            if (string.Equals(exeName, "nvcontainer", StringComparison.OrdinalIgnoreCase) || string.Equals(exeName, "NVDisplay.Container", StringComparison.OrdinalIgnoreCase)) return "Настройки NVIDIA";
            if (string.Equals(exeName, "vlc", StringComparison.OrdinalIgnoreCase)) return "VLC Media Player";
            if (string.Equals(exeName, "DSX", StringComparison.OrdinalIgnoreCase)) return "DualSenseX (DSX)";
            if (string.Equals(exeName, "EpicGamesLauncher", StringComparison.OrdinalIgnoreCase)) return "Epic Games Launcher";
            if (string.Equals(exeName, "deluge", StringComparison.OrdinalIgnoreCase)) return "Deluge Torrent";
            if (string.Equals(exeName, "Creative Cloud", StringComparison.OrdinalIgnoreCase)) return "Adobe Creative Cloud";

            // If tip has a clean title, use it
            if (!string.IsNullOrEmpty(tip) && tip.Length >= 2 && !tip.Contains("?") && !tip.Contains(""))
            {
                return tip.Trim();
            }

            // File description from executable
            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                try
                {
                    FileVersionInfo vi = FileVersionInfo.GetVersionInfo(exePath);
                    if (!string.IsNullOrEmpty(vi.FileDescription) && !vi.FileDescription.Contains("?") && !vi.FileDescription.Contains(""))
                    {
                        return vi.FileDescription.Trim();
                    }
                }
                catch { }
            }

            return exeName;
        }

        public void RefreshTasks()
        {
            UpdateTaskbarToggleText();
            _itemsListPanel.Children.Clear();

            int curSession = Process.GetCurrentProcess().SessionId;
            Dictionary<string, List<Process>> runningMap = new Dictionary<string, List<Process>>(StringComparer.OrdinalIgnoreCase);

            Process[] allProcs = Process.GetProcesses();
            for (int i = 0; i < allProcs.Length; i++)
            {
                Process p = allProcs[i];
                try
                {
                    if (p.SessionId != curSession) { p.Dispose(); continue; }
                    string pn = p.ProcessName;
                    if (string.IsNullOrEmpty(pn)) { p.Dispose(); continue; }

                    List<Process> pList;
                    if (!runningMap.TryGetValue(pn, out pList))
                    {
                        pList = new List<Process>();
                        runningMap[pn] = pList;
                    }
                    pList.Add(p);
                }
                catch
                {
                    p.Dispose();
                }
            }

            Dictionary<string, BackgroundAppItem> trayApps = new Dictionary<string, BackgroundAppItem>(StringComparer.OrdinalIgnoreCase);

            // 1. Scan real Windows NotifyIconSettings registry
            try
            {
                using (RegistryKey root = Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings"))
                {
                    if (root != null)
                    {
                        string[] subKeys = root.GetSubKeyNames();
                        for (int k = 0; k < subKeys.Length; k++)
                        {
                            using (RegistryKey sk = root.OpenSubKey(subKeys[k]))
                            {
                                if (sk == null) continue;
                                string rawPath = (sk.GetValue("ExecutablePath") as string) ?? "";
                                string tip = (sk.GetValue("InitialTooltip") as string) ?? "";
                                byte[] iconBytes = sk.GetValue("IconSnapshot") as byte[];

                                string resolved = ResolveKnownFolder(rawPath);
                                string exeName = IOPath.GetFileNameWithoutExtension(resolved);
                                if (string.IsNullOrEmpty(exeName)) continue;

                                if (string.Equals(exeName, "explorer", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(exeName, "MacOSTahoe", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(exeName, "Taskmgr", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(exeName, "Antigravity", StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                List<Process> procs;
                                if (runningMap.TryGetValue(exeName, out procs) && procs.Count > 0)
                                {
                                    long mem = 0;
                                    IntPtr mainHwnd = IntPtr.Zero;
                                    int mainPid = procs[0].Id;
                                    for (int pIdx = 0; pIdx < procs.Count; pIdx++)
                                    {
                                        try
                                        {
                                            mem += procs[pIdx].WorkingSet64 / (1024 * 1024);
                                            if (mainHwnd == IntPtr.Zero && procs[pIdx].MainWindowHandle != IntPtr.Zero)
                                            {
                                                mainHwnd = procs[pIdx].MainWindowHandle;
                                                mainPid = procs[pIdx].Id;
                                            }
                                        }
                                        catch { }
                                    }

                                    ImageSource icon = ImageFromPngBytes(iconBytes);
                                    if (icon == null) icon = ExtractIconFromExe(resolved);
                                    if (icon == null && ShellController.Instance != null)
                                    {
                                        icon = ShellController.Instance.ResolveDynamicIcon(exeName, (uint)mainPid);
                                    }

                                    string friendlyName = FormatFriendlyName(exeName, tip, resolved);

                                    trayApps[exeName] = new BackgroundAppItem
                                    {
                                        Pid = mainPid,
                                        ProcessName = exeName,
                                        DisplayName = friendlyName,
                                        ExePath = resolved,
                                        MemoryMb = mem,
                                        Icon = icon,
                                        Hwnd = mainHwnd
                                    };
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // 2. Also check for active background user utilities (e.g. running apps with known paths)
            string[] knownTrayUtilities = new string[] {
                "MSIAfterburner", "sunshine", "wallpaper32", "wallpaper64", "DCv2",
                "Fixnet", "AnyDesk", "steam", "Discord", "Telegram", "iMe", "DSX", "vlc",
                "EpicGamesLauncher", "deluge", "qbittorrent", "Spotify", "RazerSynapse",
                "LogiOptionsPlus", "Voicemeeter", "ShareX", "Lightshot", "PowerToys"
            };

            for (int u = 0; u < knownTrayUtilities.Length; u++)
            {
                string utilName = knownTrayUtilities[u];
                if (trayApps.ContainsKey(utilName)) continue;

                List<Process> procs;
                if (runningMap.TryGetValue(utilName, out procs) && procs.Count > 0)
                {
                    long mem = 0;
                    IntPtr mainHwnd = IntPtr.Zero;
                    int mainPid = procs[0].Id;
                    string exePath = "";
                    for (int pIdx = 0; pIdx < procs.Count; pIdx++)
                    {
                        try
                        {
                            mem += procs[pIdx].WorkingSet64 / (1024 * 1024);
                            if (mainHwnd == IntPtr.Zero && procs[pIdx].MainWindowHandle != IntPtr.Zero)
                            {
                                mainHwnd = procs[pIdx].MainWindowHandle;
                                mainPid = procs[pIdx].Id;
                            }
                            if (string.IsNullOrEmpty(exePath))
                            {
                                exePath = procs[pIdx].MainModule.FileName;
                            }
                        }
                        catch { }
                    }

                    ImageSource icon = ExtractIconFromExe(exePath);
                    if (icon == null && ShellController.Instance != null)
                    {
                        icon = ShellController.Instance.ResolveDynamicIcon(utilName, (uint)mainPid);
                    }

                    string friendly = FormatFriendlyName(utilName, "", exePath);
                    trayApps[utilName] = new BackgroundAppItem
                    {
                        Pid = mainPid,
                        ProcessName = utilName,
                        DisplayName = friendly,
                        ExePath = exePath,
                        MemoryMb = mem,
                        Icon = icon,
                        Hwnd = mainHwnd
                    };
                }
            }

            // Cleanup process objects
            foreach (KeyValuePair<string, List<Process>> kv in runningMap)
            {
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    try { kv.Value[i].Dispose(); } catch { }
                }
            }

            List<BackgroundAppItem> list = new List<BackgroundAppItem>(trayApps.Values);
            list.Sort(delegate(BackgroundAppItem a, BackgroundAppItem b)
            {
                return b.MemoryMb.CompareTo(a.MemoryMb);
            });

            _countText.Text = string.Format("Найдено: {0} активных приложений в трее", list.Count);

            if (list.Count == 0)
            {
                Border emptyBox = new Border
                {
                    Padding = new Thickness(16, 24, 16, 24),
                    Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                    CornerRadius = new CornerRadius(12),
                    Margin = new Thickness(0, 10, 0, 10)
                };
                TextBlock emptyText = new TextBlock
                {
                    Text = "Нет активных фоновых программ в трее",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromArgb(160, 235, 235, 245)),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                emptyBox.Child = emptyText;
                _itemsListPanel.Children.Add(emptyBox);
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                _itemsListPanel.Children.Add(CreateAppRow(list[i]));
            }
        }

        private FrameworkElement CreateAppRow(BackgroundAppItem app)
        {
            Border row = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 6)
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) }); // Icon
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Info
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Actions

            // 1. Icon
            Image img = new Image
            {
                Width = 26,
                Height = 26,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            if (app.Icon != null)
            {
                img.Source = app.Icon;
            }
            else if (ShellController.Instance != null)
            {
                img.Source = ShellController.Instance.GetIcon("settings.png");
            }
            Grid.SetColumn(img, 0);
            g.Children.Add(img);

            // 2. Info
            StackPanel info = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 8, 0) };
            TextBlock nameText = new TextBlock
            {
                Text = app.DisplayName,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            TextBlock subText = new TextBlock
            {
                Text = string.Format("{0}.exe • {1} МБ ОЗУ • В трее", app.ProcessName, app.MemoryMb),
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromArgb(160, 235, 235, 245)),
                Margin = new Thickness(0, 1, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            info.Children.Add(nameText);
            info.Children.Add(subText);
            Grid.SetColumn(info, 1);
            g.Children.Add(info);

            // 3. Actions
            StackPanel actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            // "Открыть / Развернуть" button
            Border openBtn = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                Padding = new Thickness(8, 4, 8, 4),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 6, 0)
            };
            openBtn.Child = new TextBlock
            {
                Text = "Открыть",
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White
            };
            openBtn.MouseEnter += delegate { openBtn.Background = LiquidGlassTheme.AccentBlueBrush; };
            openBtn.MouseLeave += delegate { openBtn.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)); };
            openBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                RestoreBackgroundApp(app);
            };
            actions.Children.Add(openBtn);

            // "Завершить (✕)" button
            Border killBtn = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 80, 80)),
                Padding = new Thickness(7, 4, 7, 4),
                Cursor = Cursors.Hand,
                ToolTip = "Завершить процесс"
            };
            killBtn.Child = new TextBlock
            {
                Text = "✕",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 120, 120))
            };
            killBtn.MouseEnter += delegate
            {
                killBtn.Background = new SolidColorBrush(Color.FromRgb(255, 59, 48));
                ((TextBlock)killBtn.Child).Foreground = Brushes.White;
            };
            killBtn.MouseLeave += delegate
            {
                killBtn.Background = new SolidColorBrush(Color.FromArgb(40, 255, 80, 80));
                ((TextBlock)killBtn.Child).Foreground = new SolidColorBrush(Color.FromRgb(255, 120, 120));
            };
            killBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                KillBackgroundApp(app);
            };
            actions.Children.Add(killBtn);

            Grid.SetColumn(actions, 2);
            g.Children.Add(actions);

            row.Child = g;
            return row;
        }

        private void RestoreBackgroundApp(BackgroundAppItem app)
        {
            Hide();
            try
            {
                if (app.Hwnd != IntPtr.Zero && NativeMethods.IsWindow(app.Hwnd))
                {
                    NativeMethods.ShowWindow(app.Hwnd, 9); // SW_RESTORE
                    NativeMethods.ShowWindow(app.Hwnd, 5); // SW_SHOW
                    NativeMethods.SetForegroundWindow(app.Hwnd);
                    return;
                }

                bool found = false;
                NativeMethods.EnumWindows(delegate(IntPtr hWnd, IntPtr lp)
                {
                    uint pid;
                    NativeMethods.GetWindowThreadProcessId(hWnd, out pid);
                    if (pid == (uint)app.Pid)
                    {
                        NativeMethods.ShowWindow(hWnd, 9);
                        NativeMethods.ShowWindow(hWnd, 5);
                        NativeMethods.SetForegroundWindow(hWnd);
                        found = true;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);

                if (found) return;

                if (!string.IsNullOrEmpty(app.ExePath) && File.Exists(app.ExePath))
                {
                    ShellController.RunProcess(app.ExePath, "");
                }
            }
            catch { }
        }

        private void KillBackgroundApp(BackgroundAppItem app)
        {
            try
            {
                using (Process p = Process.GetProcessById(app.Pid))
                {
                    p.Kill();
                }
            }
            catch { }

            RefreshTasks();
        }
    }
}
