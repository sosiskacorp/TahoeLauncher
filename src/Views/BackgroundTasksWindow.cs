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
using System.Windows.Shapes;

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

        private static readonly HashSet<string> SystemBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "svchost", "dwm", "conhost", "fontdrvhost", "csrss", "smss", "lsass",
            "wininit", "services", "spoolsv", "RuntimeBroker", "ApplicationFrameHost",
            "SearchHost", "StartMenuExperienceHost", "ShellExperienceHost", "ctfmon",
            "SystemSettings", "taskhostw", "TextInputHost", "sihost", "audiodg",
            "SearchIndexer", "SecurityHealthSystray", "SecurityHealthService", "wlanext",
            "smartscreen", "explorer", "MacOSTahoe", "Idle", "System", "cmd",
            "dllhost", "wmpnetwk", "SearchApp", "Widgets", "WidgetService", "LockApp",
            "SettingSyncHost", "CompPkgSrv", "backgroundTaskHost", "consent", "WmiPrvSE"
        };

        public BackgroundTasksWindow()
        {
            Title = "Фоновые задачи и системный трей";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Width = 400;
            Height = 580;
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
                NativeMethods.EnableBlur(this, 0xDD14141E);
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
                BlurRadius = 30,
                ShadowDepth = 8,
                Opacity = 0.62,
                Color = Colors.Black
            };

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Native Tray Card
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Sub-header
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Apps Scroll
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer Links

            // --- 1. HEADER ---
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

            // --- 2. NATIVE TRAY CARD ---
            Border trayCard = new Border
            {
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 12)
            };

            StackPanel trayStack = new StackPanel();
            TextBlock trayTitle = new TextBlock
            {
                Text = "Область уведомлений Windows (Трей)",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            };
            TextBlock trayDesc = new TextBlock
            {
                Text = "Нативные скрытые значки Windows 11, безопасное извлечение и службы",
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

            Border openTrayBtn = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = LiquidGlassTheme.AccentBlueBrush,
                Padding = new Thickness(8, 6, 8, 6),
                Cursor = Cursors.Hand
            };
            openTrayBtn.Child = new TextBlock
            {
                Text = "Открыть трей (Win+B)",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            openTrayBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                Hide();
                NativeMethods.OpenNativeTrayOverflow();
            };
            Grid.SetColumn(openTrayBtn, 0);
            trayBtns.Children.Add(openTrayBtn);

            Border toggleBarBtn = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 6, 8, 6),
                Cursor = Cursors.Hand
            };
            _taskbarToggleText = new TextBlock
            {
                Text = "Показать панель задач",
                FontSize = 11.5,
                FontWeight = FontWeights.Medium,
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
            Grid.SetColumn(toggleBarBtn, 2);
            trayBtns.Children.Add(toggleBarBtn);

            trayStack.Children.Add(trayBtns);
            trayCard.Child = trayStack;

            Grid.SetRow(trayCard, 1);
            root.Children.Add(trayCard);

            // --- 3. SUB-HEADER ---
            Grid subHeader = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            subHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            subHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock subTitle = new TextBlock
            {
                Text = "Активные приложения в фоне",
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

            // --- 4. APPS SCROLLVIEWER ---
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

            // --- 5. FOOTER LINKS ---
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

        public void RefreshTasks()
        {
            UpdateTaskbarToggleText();
            _itemsListPanel.Children.Clear();

            int curSession = Process.GetCurrentProcess().SessionId;
            Dictionary<string, BackgroundAppItem> apps = new Dictionary<string, BackgroundAppItem>(StringComparer.OrdinalIgnoreCase);

            Process[] procs = Process.GetProcesses();
            for (int i = 0; i < procs.Length; i++)
            {
                Process p = procs[i];
                try
                {
                    if (p.SessionId != curSession) continue;
                    string name = p.ProcessName;
                    if (string.IsNullOrEmpty(name)) continue;
                    if (SystemBlacklist.Contains(name)) continue;
                    if (name.StartsWith("ServiceHub", StringComparison.OrdinalIgnoreCase)) continue;

                    long mem = 0;
                    try { mem = p.WorkingSet64 / (1024 * 1024); } catch { }

                    BackgroundAppItem item;
                    if (apps.TryGetValue(name, out item))
                    {
                        item.MemoryMb += mem;
                        if (item.Hwnd == IntPtr.Zero && p.MainWindowHandle != IntPtr.Zero)
                        {
                            item.Hwnd = p.MainWindowHandle;
                        }
                    }
                    else
                    {
                        string desc = name;
                        string exePath = "";
                        try
                        {
                            exePath = p.MainModule.FileName;
                            string fileDesc = p.MainModule.FileVersionInfo.FileDescription;
                            if (!string.IsNullOrEmpty(fileDesc)) desc = fileDesc;
                        }
                        catch { }

                        ImageSource icon = null;
                        if (ShellController.Instance != null)
                        {
                            icon = ShellController.Instance.ResolveDynamicIcon(name, (uint)p.Id);
                        }

                        apps[name] = new BackgroundAppItem
                        {
                            Pid = p.Id,
                            ProcessName = name,
                            DisplayName = desc,
                            ExePath = exePath,
                            MemoryMb = mem,
                            Icon = icon,
                            Hwnd = p.MainWindowHandle
                        };
                    }
                }
                catch { }
                finally
                {
                    p.Dispose();
                }
            }

            List<BackgroundAppItem> list = new List<BackgroundAppItem>(apps.Values);
            list.Sort(delegate(BackgroundAppItem a, BackgroundAppItem b)
            {
                return b.MemoryMb.CompareTo(a.MemoryMb);
            });

            _countText.Text = string.Format("Найдено: {0} фоновых приложений", list.Count);

            if (list.Count == 0)
            {
                TextBlock emptyText = new TextBlock
                {
                    Text = "Нет активных фоновых приложений",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20)
                };
                _itemsListPanel.Children.Add(emptyText);
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
                img.Source = ShellController.Instance.GetIcon("finder.png");
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
                Text = string.Format("{0}.exe • {1} МБ ОЗУ", app.ProcessName, app.MemoryMb),
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

                // If window handle was not cached, search windows of this PID
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

                // If app is fully hidden in tray, launching its executable signals the instance to show window
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
