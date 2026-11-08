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
    public class LauncherSettingsWindow : Window
    {
        private StackPanel _navStack;
        private Border _contentHost;
        private int _activeTab = 0;

        public LauncherSettingsWindow()
        {
            Title = "Настройки Tahoe Launcher";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Width = 780;
            Height = 560;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();
        }

        private void BuildUI()
        {
            Border outer = new Border
            {
                Margin = new Thickness(16),
                CornerRadius = new CornerRadius(24),
                Background = LiquidGlassTheme.CreateGlassBrush(236, 248),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                Effect = new DropShadowEffect { BlurRadius = 32, ShadowDepth = 10, Opacity = 0.65, Color = Colors.Black }
            };

            Grid mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left Sidebar
            Border sidebar = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(45, 0, 0, 0)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)),
                BorderThickness = new Thickness(0, 0, 1, 0),
                CornerRadius = new CornerRadius(24, 0, 0, 24),
                Padding = new Thickness(16, 16, 14, 16)
            };
            sidebar.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    try { DragMove(); } catch { }
                }
            };

            StackPanel sideStack = new StackPanel();

            StackPanel traffic = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2, 0, 0, 18) };
            Ellipse closeBtn = new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Color.FromRgb(255, 95, 87)), Margin = new Thickness(0, 0, 8, 0), Cursor = Cursors.Hand, ToolTip = "Закрыть" };
            closeBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; Hide(); };
            Ellipse minBtn = new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Color.FromRgb(254, 188, 46)), Margin = new Thickness(0, 0, 8, 0) };
            Ellipse maxBtn = new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Color.FromRgb(40, 200, 64)) };
            traffic.Children.Add(closeBtn);
            traffic.Children.Add(minBtn);
            traffic.Children.Add(maxBtn);
            sideStack.Children.Add(traffic);

            sideStack.Children.Add(new TextBlock
            {
                Text = "Tahoe Launcher",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(4, 0, 0, 2)
            });
            sideStack.Children.Add(new TextBlock
            {
                Text = "Liquid Glass Shell v26.2",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromArgb(155, 235, 235, 245)),
                Margin = new Thickness(4, 0, 0, 18)
            });

            _navStack = new StackPanel();
            sideStack.Children.Add(_navStack);
            sidebar.Child = sideStack;
            Grid.SetColumn(sidebar, 0);
            mainGrid.Children.Add(sidebar);

            // Right Content Area
            _contentHost = new Border
            {
                Padding = new Thickness(22, 18, 22, 18)
            };
            Grid.SetColumn(_contentHost, 1);
            mainGrid.Children.Add(_contentHost);

            outer.Child = mainGrid;
            Content = outer;

            SwitchTab(0);
        }

        public void RefreshAllTabs()
        {
            SwitchTab(_activeTab);
        }

        private void SwitchTab(int tabIdx)
        {
            _activeTab = tabIdx;
            _navStack.Children.Clear();

            string[] tabs = new string[]
            {
                "⚙  Оболочка и вид",
                "📌  Панель Dock",
                "🚀  Каталог программ",
                "⌨  Хоткеи и система"
            };

            for (int i = 0; i < tabs.Length; i++)
            {
                int idx = i;
                bool active = (i == _activeTab);
                Border navItem = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Background = active ? LiquidGlassTheme.AccentBlueBrush : Brushes.Transparent,
                    Padding = new Thickness(12, 9, 12, 9),
                    Margin = new Thickness(0, 2, 0, 2),
                    Cursor = Cursors.Hand
                };
                navItem.Child = new TextBlock
                {
                    Text = tabs[i],
                    FontSize = 13,
                    FontWeight = active ? FontWeights.SemiBold : FontWeights.Medium,
                    Foreground = Brushes.White
                };
                navItem.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    SwitchTab(idx);
                };
                _navStack.Children.Add(navItem);
            }

            if (_activeTab == 0) _contentHost.Child = BuildAppearanceTab();
            else if (_activeTab == 1) _contentHost.Child = BuildDockManagerTab();
            else if (_activeTab == 2) _contentHost.Child = BuildCatalogTab();
            else _contentHost.Child = BuildSystemTab();
        }

        private UIElement BuildAppearanceTab()
        {
            LauncherConfig cfg = ShellController.Instance.Config;
            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            StackPanel sp = new StackPanel();

            sp.Children.Add(CreateSectionHeader("Модули оболочки и рабочего стола"));
            sp.Children.Add(CreateToggleRow("Верхняя строка меню (Top Bar)", "Глобальное меню, индикатор раскладки, ОЗУ и часы", cfg.ShowTopBar, delegate(bool v)
            {
                cfg.ShowTopBar = v;
                ShellController.Instance.SaveConfig();
                ShellController.Instance.ApplyModuleVisibility();
            }));
            sp.Children.Add(CreateToggleRow("Нижняя панель Dock", "Интерактивный Dock с физикой волны 120+ Гц", cfg.ShowDock, delegate(bool v)
            {
                cfg.ShowDock = v;
                ShellController.Instance.SaveConfig();
                ShellController.Instance.ApplyModuleVisibility();
            }));
            sp.Children.Add(CreateToggleRow("Виджеты рабочего стола", "Виджеты времени, телеметрии ПК, избранного и заметок", cfg.ShowDesktopWidgets, delegate(bool v)
            {
                cfg.ShowDesktopWidgets = v;
                ShellController.Instance.SaveConfig();
                ShellController.Instance.ApplyModuleVisibility();
            }));
            sp.Children.Add(CreateToggleRow("Скрывать панель задач Windows 11", "Убирает стандартный таскбар для чистого вида", cfg.HideWindowsTaskbar, delegate(bool v)
            {
                cfg.HideWindowsTaskbar = v;
                ShellController.Instance.SaveConfig();
                ShellController.Instance.ApplyModuleVisibility();
            }));
            sp.Children.Add(CreateToggleRow("Открывать «Приложения» по клавише Win", "Одиночное нажатие Win открывает лаунчер (Win+E/R/S работают штатно)", cfg.InterceptWinKey, delegate(bool v)
            {
                cfg.InterceptWinKey = v;
                ShellController.Instance.SaveConfig();
            }));
            sp.Children.Add(CreateToggleRow("Автозапуск вместе с Windows", "Автоматически запускать Tahoe Launcher при входе в систему", ShellController.Instance.IsAutostartEnabled(), delegate(bool v)
            {
                ShellController.Instance.SetAutostartEnabled(v);
            }));

            sp.Children.Add(CreateSectionHeader("Постановщик (Stage Manager)"));
            sp.Children.Add(CreateToggleRow("Включить Постановщик (Stage Manager)", "Упорядочивает открытые окна, удерживая активное по центру, а остальные слева", cfg.EnableStageManager, delegate(bool v)
            {
                if (ShellController.Instance.Config.EnableStageManager != v)
                {
                    ShellController.Instance.ToggleStageManager();
                }
            }));
            sp.Children.Add(CreateToggleRow("Скрывать боковую панель миниатюр", "Панель появляется только при подведении курсора к левому краю экрана", cfg.StageManagerAutoHide, delegate(bool v)
            {
                cfg.StageManagerAutoHide = v;
                ShellController.Instance.SaveConfig();
                if (ShellController.Instance.StageManager != null) ShellController.Instance.StageManager.ApplyAutoHideSetting();
            }));
            sp.Children.Add(CreateToggleRow("Группировать окна одного приложения", "Собирает несколько окон одной программы в аккуратную стопку", cfg.StageManagerGroupSameApp, delegate(bool v)
            {
                cfg.StageManagerGroupSameApp = v;
                ShellController.Instance.SaveConfig();
                if (ShellController.Instance.StageManager != null) ShellController.Instance.StageManager.RefreshShelf();
            }));
            sp.Children.Add(CreateToggleRow("Центрировать активное окно", "Автоматически размещает рабочее приложение по центру экрана", cfg.StageManagerAutoCenter, delegate(bool v)
            {
                cfg.StageManagerAutoCenter = v;
                ShellController.Instance.SaveConfig();
            }));

            sp.Children.Add(CreateSectionHeader("Размеры и анимация Dock"));
            sp.Children.Add(CreateSliderRow("Базовый размер иконок Dock", 38, 68, cfg.DockIconSize, "px", delegate(double v)
            {
                cfg.DockIconSize = Math.Round(v);
                ShellController.Instance.SaveConfig();
                if (ShellController.Instance.Dock != null) ShellController.Instance.Dock.RebuildDockVisuals();
            }));
            sp.Children.Add(CreateSliderRow("Сила увеличения при наведении (Zoom)", 0, 36, cfg.DockZoomRange, "px", delegate(double v)
            {
                cfg.DockZoomRange = Math.Round(v);
                ShellController.Instance.SaveConfig();
            }));

            sp.Children.Add(CreateSectionHeader("Обои рабочего стола 6K"));
            WrapPanel wpWrap = new WrapPanel { Margin = new Thickness(0, 4, 0, 8) };
            if (Directory.Exists(ShellController.Instance.WallpapersDir))
            {
                string[] files = Directory.GetFiles(ShellController.Instance.WallpapersDir, "*.jpg");
                for (int i = 0; i < files.Length; i++)
                {
                    string f = files[i];
                    string name = IOPath.GetFileNameWithoutExtension(f);
                    Border wpBtn = CreateSmallButton("🖼 " + name, delegate { ShellController.Instance.ApplyWallpaperFile(f); });
                    wpBtn.Margin = new Thickness(0, 0, 6, 6);
                    wpWrap.Children.Add(wpBtn);
                }
            }
            Border customWpBtn = CreateSmallButton("📂 Выбрать свой файл...", delegate
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Title = "Выберите изображение для обоев",
                    Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
                };
                if (ofd.ShowDialog() == true)
                {
                    ShellController.Instance.ApplyWallpaperFile(ofd.FileName);
                }
            });
            customWpBtn.Margin = new Thickness(0, 0, 6, 6);
            wpWrap.Children.Add(customWpBtn);
            sp.Children.Add(wpWrap);

            sv.Content = sp;
            return sv;
        }

        private UIElement BuildDockManagerTab()
        {
            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock header = CreateSectionHeader("Закреплённые приложения в Dock");
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 6, 0, 10) };
            StackPanel listStack = new StackPanel();

            List<DockItemConfig> items = ShellController.Instance.Config.PinnedDockItems;
            for (int i = 0; i < items.Count; i++)
            {
                DockItemConfig item = items[i];
                if (item.IsSeparator) continue;

                Border row = new Border
                {
                    CornerRadius = new CornerRadius(11),
                    Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 3, 0, 3)
                };

                Grid g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                StackPanel info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                info.Children.Add(new TextBlock { Text = item.Name, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                info.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrEmpty(item.LaunchCommand) ? "Системный элемент" : item.LaunchCommand,
                    FontSize = 10.5,
                    Foreground = new SolidColorBrush(Color.FromArgb(150, 235, 235, 245)),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                Grid.SetColumn(info, 0);
                g.Children.Add(info);

                StackPanel btns = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                if (!item.IsExplorer && !item.IsAppsLauncher && !item.IsTrash && !item.IsDesktopFolder)
                {
                    string capId = item.Id;
                    btns.Children.Add(CreateSmallButton("↑", delegate
                    {
                        ShellController.Instance.MovePinnedDockItem(capId, -1);
                        SwitchTab(1);
                    }));
                    Border downBtn = CreateSmallButton("↓", delegate
                    {
                        ShellController.Instance.MovePinnedDockItem(capId, 1);
                        SwitchTab(1);
                    });
                    downBtn.Margin = new Thickness(4, 0, 4, 0);
                    btns.Children.Add(downBtn);

                    btns.Children.Add(CreateSmallButton("Удалить", delegate
                    {
                        ShellController.Instance.UnpinAppFromDock(capId);
                        SwitchTab(1);
                    }));
                }
                else
                {
                    btns.Children.Add(new TextBlock
                    {
                        Text = "Системный",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromArgb(130, 235, 235, 245)),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }

                Grid.SetColumn(btns, 1);
                g.Children.Add(btns);
                row.Child = g;
                listStack.Children.Add(row);
            }

            sv.Content = listStack;
            Grid.SetRow(sv, 1);
            root.Children.Add(sv);

            StackPanel bottomBar = new StackPanel { Orientation = Orientation.Horizontal };
            bottomBar.Children.Add(CreateSmallButton("➕ Добавить .exe в Dock...", delegate
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Title = "Выберите программу (.exe) для закрепления в Dock",
                    Filter = "Программы (*.exe)|*.exe"
                };
                if (ofd.ShowDialog() == true)
                {
                    string title = IOPath.GetFileNameWithoutExtension(ofd.FileName);
                    ShellController.Instance.PinAppToDock(title, ofd.FileName, "");
                    SwitchTab(1);
                }
            }));

            Border resetBtn = CreateSmallButton("↺ Сбросить Dock по умолчанию", delegate
            {
                ShellController.Instance.Config.PinnedDockItems.Clear();
                ShellController.Instance.SaveConfig();
                ShellController.Instance.LoadConfig();
                // Repopulate defaults
                ShellController.Instance.RebuildDockItemsFromConfig();
                SwitchTab(1);
            });
            resetBtn.Margin = new Thickness(10, 0, 0, 0);
            bottomBar.Children.Add(resetBtn);

            Grid.SetRow(bottomBar, 2);
            root.Children.Add(bottomBar);
            return root;
        }

        private UIElement BuildCatalogTab()
        {
            StackPanel sp = new StackPanel();
            sp.Children.Add(CreateSectionHeader(string.Format("Каталог приложений ({0} найдено)", ShellController.Instance.InstalledApps.Count)));

            TextBlock desc = new TextBlock
            {
                Text = "Лаунчер автоматически отслеживает установку и удаление программ в меню «Пуск» и на Рабочем столе в реальном времени. Вы также можете добавить свою портативную программу или вернуть скрытые приложения.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromArgb(190, 235, 235, 245)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 14)
            };
            sp.Children.Add(desc);

            StackPanel actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 18) };
            actions.Children.Add(CreateSmallButton("➕ Добавить портативную программу (.exe)...", delegate
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Title = "Добавить приложение в каталог лаунчера",
                    Filter = "Программы (*.exe)|*.exe"
                };
                if (ofd.ShowDialog() == true)
                {
                    string title = IOPath.GetFileNameWithoutExtension(ofd.FileName);
                    ShellController.Instance.Config.CustomApps.Add(new CustomAppConfig
                    {
                        Title = title,
                        Cmd = ofd.FileName,
                        Args = "",
                        Category = "Программы"
                    });
                    ShellController.Instance.SaveConfig();
                    ShellController.Instance.RefreshInstalledApps();
                    SwitchTab(2);
                }
            }));

            Border rescanBtn = CreateSmallButton("↻ Пересканировать сейчас", delegate
            {
                ShellController.Instance.RefreshInstalledApps();
                SwitchTab(2);
            });
            rescanBtn.Margin = new Thickness(10, 0, 0, 0);
            actions.Children.Add(rescanBtn);
            sp.Children.Add(actions);

            sp.Children.Add(CreateSectionHeader("Скрытые приложения (" + ShellController.Instance.Config.HiddenApps.Count + ")"));
            if (ShellController.Instance.Config.HiddenApps.Count == 0)
            {
                sp.Children.Add(new TextBlock
                {
                    Text = "Нет скрытых приложений. Чтобы скрыть приложение, нажмите по нему правой кнопкой мыши в меню «Приложения».",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromArgb(150, 235, 235, 245))
                });
            }
            else
            {
                WrapPanel hiddenWrap = new WrapPanel();
                for (int i = 0; i < ShellController.Instance.Config.HiddenApps.Count; i++)
                {
                    string hName = ShellController.Instance.Config.HiddenApps[i];
                    Border chip = CreateSmallButton("👁 Вернуть «" + hName + "»", delegate
                    {
                        ShellController.Instance.Config.HiddenApps.Remove(hName);
                        ShellController.Instance.SaveConfig();
                        ShellController.Instance.RefreshInstalledApps();
                        SwitchTab(2);
                    });
                    chip.Margin = new Thickness(0, 0, 6, 6);
                    hiddenWrap.Children.Add(chip);
                }
                sp.Children.Add(hiddenWrap);
            }

            return sp;
        }

        private UIElement BuildSystemTab()
        {
            StackPanel sp = new StackPanel();
            sp.Children.Add(CreateSectionHeader("Горячие клавиши Tahoe Launcher"));

            sp.Children.Add(CreateShortcutRow("Win (одиночное нажатие)", "Открыть / закрыть полноэкранное меню «Приложения»"));
            sp.Children.Add(CreateShortcutRow("Alt + Пробел", "Командная палитра, поиск программ, калькулятор и терминал"));
            sp.Children.Add(CreateShortcutRow("Win + Shift + A", "Открыть окно «Настройки лаунчера»"));
            sp.Children.Add(CreateShortcutRow("ПКМ по иконке в Dock / Лаунчере", "Закрепить, открепить, запуск от Администратора, папка файла"));
            sp.Children.Add(CreateShortcutRow("Колесо мыши / Стрелки в Лаунчере", "Быстрое переключение страниц приложений"));

            sp.Children.Add(CreateSectionHeader("Управление процессом и конфигурацией"));
            StackPanel sysBtns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 16) };
            sysBtns.Children.Add(CreateSmallButton("📂 Открыть config.json", delegate
            {
                ShellController.OpenFileLocation(ShellController.Instance.ConfigPath);
            }));

            Border openDirBtn = CreateSmallButton("📁 Папка лаунчера", delegate
            {
                ShellController.RunProcess("explorer.exe", ShellController.Instance.BaseDir);
            });
            openDirBtn.Margin = new Thickness(8, 0, 0, 0);
            sysBtns.Children.Add(openDirBtn);
            sp.Children.Add(sysBtns);

            Border exitCard = new Border
            {
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromArgb(45, 255, 69, 58)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 69, 58)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 10, 0, 0)
            };
            StackPanel exitStack = new StackPanel();
            exitStack.Children.Add(new TextBlock
            {
                Text = "Выход из Tahoe Launcher",
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            exitStack.Children.Add(new TextBlock
            {
                Text = "Завершает работу оболочки и мгновенно возвращает стандартную панель задач Windows 11.",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 235, 235, 245)),
                Margin = new Thickness(0, 2, 0, 10)
            });
            Border quitBtn = CreateSmallButton("⏻ Выключить лаунчер и вернуть панель Windows", delegate
            {
                ShellController.Instance.ShutdownAndRestoreWindows();
            });
            quitBtn.HorizontalAlignment = HorizontalAlignment.Left;
            exitStack.Children.Add(quitBtn);
            exitCard.Child = exitStack;
            sp.Children.Add(exitCard);

            return sp;
        }

        private TextBlock CreateSectionHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 14.5,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 8, 0, 8)
            };
        }

        private FrameworkElement CreateToggleRow(string title, string sub, bool initial, Action<bool> onChanged)
        {
            Border row = new Border
            {
                CornerRadius = new CornerRadius(11),
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 3, 0, 3)
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            texts.Children.Add(new TextBlock { Text = title, FontSize = 12.5, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
            texts.Children.Add(new TextBlock { Text = sub, FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(165, 235, 235, 245)) });
            Grid.SetColumn(texts, 0);
            g.Children.Add(texts);

            CheckBox cb = new CheckBox
            {
                IsChecked = initial,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            cb.Checked += delegate { if (onChanged != null) onChanged(true); };
            cb.Unchecked += delegate { if (onChanged != null) onChanged(false); };
            Grid.SetColumn(cb, 1);
            g.Children.Add(cb);

            row.Child = g;
            return row;
        }

        private FrameworkElement CreateSliderRow(string title, double min, double max, double val, string unit, Action<double> onChanged)
        {
            Border row = new Border
            {
                CornerRadius = new CornerRadius(11),
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 3, 0, 3)
            };

            StackPanel sp = new StackPanel();
            Grid top = new Grid();
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            top.Children.Add(new TextBlock { Text = title, FontSize = 12.5, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
            TextBlock valText = new TextBlock { Text = string.Format("{0:0} {1}", val, unit), FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
            Grid.SetColumn(valText, 1);
            top.Children.Add(valText);
            sp.Children.Add(top);

            Slider sl = new Slider
            {
                Minimum = min,
                Maximum = max,
                Value = val,
                Margin = new Thickness(0, 6, 0, 0),
                Cursor = Cursors.Hand
            };
            sl.ValueChanged += delegate(object s, RoutedPropertyChangedEventArgs<double> e)
            {
                valText.Text = string.Format("{0:0} {1}", e.NewValue, unit);
                if (onChanged != null) onChanged(e.NewValue);
            };
            sp.Children.Add(sl);
            row.Child = sp;
            return row;
        }

        private FrameworkElement CreateShortcutRow(string key, string desc)
        {
            Border row = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)),
                Padding = new Thickness(12, 7, 12, 7),
                Margin = new Thickness(0, 2, 0, 2)
            };
            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock k = new TextBlock { Text = key, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(100, 210, 255)) };
            Grid.SetColumn(k, 0);
            g.Children.Add(k);

            TextBlock d = new TextBlock { Text = desc, FontSize = 12, Foreground = Brushes.White };
            Grid.SetColumn(d, 1);
            g.Children.Add(d);

            row.Child = g;
            return row;
        }

        private Border CreateSmallButton(string text, Action onClick)
        {
            Border b = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(48, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(65, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(11, 5, 11, 5),
                Cursor = Cursors.Hand
            };
            b.Child = new TextBlock { Text = text, FontSize = 12, FontWeight = FontWeights.Medium, Foreground = Brushes.White };
            b.MouseEnter += delegate { b.Background = LiquidGlassTheme.AccentBlueBrush; };
            b.MouseLeave += delegate { b.Background = new SolidColorBrush(Color.FromArgb(48, 255, 255, 255)); };
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };
            return b;
        }
    }

    // =========================================================================
    // 9. CALENDAR POPUP
    // =========================================================================
}
