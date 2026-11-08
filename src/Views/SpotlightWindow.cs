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
    public class SpotlightResultItem
    {
        public string Title;
        public string Subtitle;
        public string Badge;
        public ImageSource Icon;
        public Action OnExecute;
        public Action OnAdminExecute;
        public string FileLocation;
    }

    public class SpotlightWindow : Window
    {
        private TextBox _searchBox;
        private TextBlock _hintText;
        private StackPanel _resultsPanel;
        private List<SpotlightResultItem> _currentItems = new List<SpotlightResultItem>();
        private List<Border> _resultRows = new List<Border>();
        private int _selectedIndex = 0;

        public SpotlightWindow()
        {
            Title = "Быстрый поиск";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Width = 680;
            Height = 470;
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2.0;
            Top = SystemParameters.PrimaryScreenHeight * 0.20;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();

            Deactivated += delegate { Hide(); };
            PreviewKeyDown += OnWindowPreviewKeyDown;
        }

        private void BuildUI()
        {
            Border card = new Border();
            card.Margin = new Thickness(16);
            card.CornerRadius = new CornerRadius(22);
            card.Background = LiquidGlassTheme.CreateGlassBrush(230, 244);
            card.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush();
            card.BorderThickness = new Thickness(1);
            card.Padding = new Thickness(16);
            card.Effect = new DropShadowEffect
            {
                BlurRadius = 32,
                ShadowDepth = 10,
                Opacity = 0.65,
                Color = Colors.Black
            };

            StackPanel root = new StackPanel();

            Grid searchHeader = new Grid();
            searchHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            searchHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock lens = new TextBlock
            {
                Text = "🔍",
                FontSize = 18,
                Foreground = new SolidColorBrush(Color.FromArgb(190, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 12, 0)
            };
            Grid.SetColumn(lens, 0);
            searchHeader.Children.Add(lens);

            _hintText = new TextBlock
            {
                Text = "Программы, >команды, математика (24*15), ?веб-поиск, $терминал...",
                FontSize = 15,
                Foreground = new SolidColorBrush(Color.FromArgb(120, 235, 235, 245)),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            Grid.SetColumn(_hintText, 1);
            searchHeader.Children.Add(_hintText);

            _searchBox = new TextBox();
            _searchBox.FontSize = 19;
            _searchBox.Foreground = Brushes.White;
            _searchBox.CaretBrush = LiquidGlassTheme.AccentBlueBrush;
            _searchBox.Background = Brushes.Transparent;
            _searchBox.BorderThickness = new Thickness(0);
            _searchBox.VerticalAlignment = VerticalAlignment.Center;
            _searchBox.TextChanged += OnSearchTextChanged;
            Grid.SetColumn(_searchBox, 1);
            searchHeader.Children.Add(_searchBox);

            Border footerHint = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)),
                Padding = new Thickness(8, 3, 8, 3),
                VerticalAlignment = VerticalAlignment.Center
            };
            footerHint.Child = new TextBlock
            {
                Text = "↑↓ выбор • Enter запуск • Tab папка",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 235, 235, 245))
            };
            Grid.SetColumn(footerHint, 2);
            searchHeader.Children.Add(footerHint);

            root.Children.Add(searchHeader);

            Border sep = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Margin = new Thickness(0, 12, 0, 10)
            };
            root.Children.Add(sep);

            _resultsPanel = new StackPanel();
            root.Children.Add(_resultsPanel);

            card.Child = root;
            Content = card;

            PopulateResults("");
        }

        public void ShowAndFocus()
        {
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2.0;
            Top = SystemParameters.PrimaryScreenHeight * 0.20;
            _searchBox.Text = "";
            _hintText.Visibility = Visibility.Visible;
            PopulateResults("");
            Show();
            Activate();
            _searchBox.Focus();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _hintText.Visibility = string.IsNullOrEmpty(_searchBox.Text) ? Visibility.Visible : Visibility.Hidden;
            PopulateResults(_searchBox.Text.Trim());
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Hide();
            }
            else if (e.Key == Key.Down)
            {
                if (_currentItems.Count > 0)
                {
                    e.Handled = true;
                    _selectedIndex = (_selectedIndex + 1) % _currentItems.Count;
                    UpdateSelectionVisuals();
                }
            }
            else if (e.Key == Key.Up)
            {
                if (_currentItems.Count > 0)
                {
                    e.Handled = true;
                    _selectedIndex = (_selectedIndex - 1 + _currentItems.Count) % _currentItems.Count;
                    UpdateSelectionVisuals();
                }
            }
            else if (e.Key == Key.Tab)
            {
                if (_selectedIndex >= 0 && _selectedIndex < _currentItems.Count)
                {
                    string loc = _currentItems[_selectedIndex].FileLocation;
                    if (!string.IsNullOrEmpty(loc))
                    {
                        e.Handled = true;
                        Hide();
                        ShellController.OpenFileLocation(loc);
                    }
                }
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (_selectedIndex >= 0 && _selectedIndex < _currentItems.Count)
                {
                    SpotlightResultItem sel = _currentItems[_selectedIndex];
                    bool ctrlShift = (Keyboard.Modifiers & ModifierKeys.Control) != 0 && (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
                    Hide();
                    if (ctrlShift && sel.OnAdminExecute != null)
                        sel.OnAdminExecute();
                    else if (sel.OnExecute != null)
                        sel.OnExecute();
                }
            }
        }

        private void UpdateSelectionVisuals()
        {
            for (int i = 0; i < _resultRows.Count; i++)
            {
                _resultRows[i].Background = (i == _selectedIndex)
                    ? new SolidColorBrush(Color.FromArgb(125, 10, 132, 255))
                    : Brushes.Transparent;
            }
        }

        private void PopulateResults(string query)
        {
            _resultsPanel.Children.Clear();
            _currentItems.Clear();
            _resultRows.Clear();
            _selectedIndex = 0;

            bool cmdMode = query.StartsWith(">");
            string cleanQuery = cmdMode ? query.Substring(1).Trim() : query;

            // 1. Math expression evaluation
            if (!cmdMode && !string.IsNullOrEmpty(query) && !query.StartsWith("?") && !query.StartsWith("$"))
            {
                try
                {
                    System.Data.DataTable dt = new System.Data.DataTable();
                    object val = dt.Compute(query, "");
                    if (val != null && !(val is DBNull))
                    {
                        double num;
                        if (double.TryParse(val.ToString(), out num))
                        {
                            string resStr = val.ToString();
                            AddResultItem(new SpotlightResultItem
                            {
                                Title = query + " = " + resStr,
                                Subtitle = "Нажмите Enter, чтобы скопировать результат в буфер обмена",
                                Badge = "Калькулятор",
                                Icon = ShellController.Instance.GetIcon("calculator.png"),
                                OnExecute = delegate { try { Clipboard.SetText(resStr); } catch { } }
                            });
                        }
                    }
                }
                catch { }
            }

            // 2. Terminal command ($...)
            if (query.StartsWith("$") && query.Length > 1)
            {
                string termCmd = query.Substring(1).Trim();
                AddResultItem(new SpotlightResultItem
                {
                    Title = "Выполнить в Терминале: " + termCmd,
                    Subtitle = "PowerShell / Windows Terminal",
                    Badge = "Терминал",
                    Icon = ShellController.Instance.GetIcon("terminal.png"),
                    OnExecute = delegate { ShellController.RunProcess("powershell.exe", "-NoExit -Command \"" + termCmd + "\""); }
                });
            }

            // 3. Web search (?...)
            if (query.StartsWith("?") && query.Length > 1)
            {
                string webQ = query.Substring(1).Trim();
                AddResultItem(new SpotlightResultItem
                {
                    Title = "Найти в Google: «" + webQ + "»",
                    Subtitle = "Открыть в браузере по умолчанию",
                    Badge = "Веб-поиск",
                    Icon = ShellController.Instance.GetIcon("chrome.png"),
                    OnExecute = delegate { ShellController.RunProcess("https://www.google.com/search?q=" + Uri.EscapeDataString(webQ), ""); }
                });
            }

            // 4. Built-in Launcher & System Commands
            AddMatchingCommand(cleanQuery, cmdMode, "Настройки лаунчера", "Управление панелями, Dock, виджетами и автозапуском", "settings.png",
                delegate { ShellController.Instance.ShowLauncherSettings(); });
            AddMatchingCommand(cleanQuery, cmdMode, "Все приложения", "Открыть полноэкранный каталог программ (Win)", "launchpad.png",
                delegate { ShellController.Instance.ToggleLaunchpad(); });
            AddMatchingCommand(cleanQuery, cmdMode, "Обзор активных окон", "Показать все открытые окна и переключиться", "mission-control.png",
                delegate { ShellController.Instance.ToggleWindowSwitcher(); });
            AddMatchingCommand(cleanQuery, cmdMode, "Переключить тему (Тёмная / Светлая)", "Смена оформления Windows и обоев 6K", "wallpaper.png",
                delegate { ShellController.Instance.ToggleDarkMode(); });
            AddMatchingCommand(cleanQuery, cmdMode, "Показать / Скрыть виджеты рабочего стола", "Виджеты часов, ресурсов ПК и заметок", "control-center.png",
                delegate
                {
                    ShellController.Instance.Config.ShowDesktopWidgets = !ShellController.Instance.Config.ShowDesktopWidgets;
                    ShellController.Instance.SaveConfig();
                    ShellController.Instance.ApplyModuleVisibility();
                });
            AddMatchingCommand(cleanQuery, cmdMode, "Показать / Скрыть значки рабочего стола", "Переключение видимости ярлыков стола", "desktop.png",
                delegate { NativeMethods.ToggleDesktopIcons(); });
            AddMatchingCommand(cleanQuery, cmdMode, "Очистить Корзину", "Удалить все файлы из Корзины Windows", "trash-full.png",
                delegate { NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null, 0); });
            AddMatchingCommand(cleanQuery, cmdMode, "Выход из лаунчера (вернуть панель Windows)", "Закрыть Tahoe Launcher и вернуть стандартный интерфейс", "activity-monitor.png",
                delegate { ShellController.Instance.ShutdownAndRestoreWindows(); });

            // 5. Installed Applications Search
            if (!cmdMode && !query.StartsWith("?") && !query.StartsWith("$"))
            {
                List<AppCatalogEntry> apps = ShellController.Instance.InstalledApps;
                for (int i = 0; i < apps.Count && _currentItems.Count < 7; i++)
                {
                    AppCatalogEntry item = apps[i];
                    if (!string.IsNullOrEmpty(cleanQuery) &&
                        item.Title.IndexOf(cleanQuery, StringComparison.OrdinalIgnoreCase) < 0 &&
                        IOPath.GetFileNameWithoutExtension(item.Cmd).IndexOf(cleanQuery, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    AppCatalogEntry cap = item;
                    AddResultItem(new SpotlightResultItem
                    {
                        Title = cap.Title,
                        Subtitle = cap.Category + " • " + IOPath.GetFileName(cap.Cmd),
                        Badge = "Приложение",
                        Icon = cap.IconSource ?? ShellController.Instance.GetIcon("finder.png"),
                        FileLocation = cap.Cmd,
                        OnExecute = delegate { ShellController.RunProcess(cap.Cmd, cap.Args); },
                        OnAdminExecute = delegate { ShellController.RunProcessEx(cap.Cmd, cap.Args, true); }
                    });
                }
            }

            // 6. Fallback web search if query didn't match or user wants web search
            if (!string.IsNullOrEmpty(cleanQuery) && _currentItems.Count < 7 && !cmdMode)
            {
                string qCopy = cleanQuery;
                AddResultItem(new SpotlightResultItem
                {
                    Title = "Искать в Интернете: «" + qCopy + "»",
                    Subtitle = "Открыть результаты поиска Google в браузере",
                    Badge = "Интернет",
                    Icon = ShellController.Instance.GetIcon("chrome.png"),
                    OnExecute = delegate { ShellController.RunProcess("https://www.google.com/search?q=" + Uri.EscapeDataString(qCopy), ""); }
                });
            }

            UpdateSelectionVisuals();
        }

        private void AddMatchingCommand(string query, bool cmdMode, string title, string subtitle, string iconFile, Action action)
        {
            if (_currentItems.Count >= 7) return;
            if (!cmdMode && string.IsNullOrEmpty(query)) return;
            if (!string.IsNullOrEmpty(query) &&
                title.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
                subtitle.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                return;

            AddResultItem(new SpotlightResultItem
            {
                Title = title,
                Subtitle = subtitle,
                Badge = "Команда",
                Icon = ShellController.Instance.GetIcon(iconFile),
                OnExecute = action
            });
        }

        private void AddResultItem(SpotlightResultItem item)
        {
            if (_currentItems.Count >= 7) return;
            int idx = _currentItems.Count;
            _currentItems.Add(item);

            Border row = new Border
            {
                CornerRadius = new CornerRadius(11),
                Padding = new Thickness(10, 7, 10, 7),
                Margin = new Thickness(0, 1, 0, 1),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Image icon = new Image
            {
                Source = item.Icon ?? ShellController.Instance.GetIcon("finder.png"),
                Width = 30,
                Height = 30,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.HighQuality);
            Grid.SetColumn(icon, 0);
            g.Children.Add(icon);

            StackPanel texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            texts.Children.Add(new TextBlock
            {
                Text = item.Title,
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            if (!string.IsNullOrEmpty(item.Subtitle))
            {
                texts.Children.Add(new TextBlock
                {
                    Text = item.Subtitle,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromArgb(165, 235, 235, 245)),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
            }
            Grid.SetColumn(texts, 1);
            g.Children.Add(texts);

            if (!string.IsNullOrEmpty(item.Badge))
            {
                Border badge = new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Background = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255)),
                    Padding = new Thickness(8, 2, 8, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                badge.Child = new TextBlock
                {
                    Text = item.Badge,
                    FontSize = 10.5,
                    Foreground = new SolidColorBrush(Color.FromArgb(200, 235, 235, 245))
                };
                Grid.SetColumn(badge, 2);
                g.Children.Add(badge);
            }

            row.Child = g;

            int capturedIdx = idx;
            SpotlightResultItem capturedItem = item;
            row.MouseEnter += delegate
            {
                _selectedIndex = capturedIdx;
                UpdateSelectionVisuals();
            };
            row.MouseLeftButtonDown += delegate
            {
                Hide();
                if (capturedItem.OnExecute != null) capturedItem.OnExecute();
            };

            _resultRows.Add(row);
            _resultsPanel.Children.Add(row);
        }
    }
    // =========================================================================
    // 6. FULLSCREEN APPS LAUNCHER (Приложения — Категории, ПКМ-меню, Живой каталог)
    // =========================================================================
}
