using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using MacOSTahoe.Core;
using MacOSTahoe.Models;
using MacOSTahoe.Native;
using MacOSTahoe.Theme;

namespace MacOSTahoe.Views
{
    public class LauncherSettingsWindow : Window
    {
        private TabControl _tabControl;
        private CheckBox _chkTopBar;
        private CheckBox _chkDock;
        private CheckBox _chkHideTaskbar;
        private CheckBox _chkDockAutoHide;
        private CheckBox _chkWinKey;
        private CheckBox _chkDarkMode;
        private CheckBox _chkWidgets;
        private Slider _sliderIconSize;
        private Slider _sliderZoomRange;
        private TextBlock _lblIconSize;
        private TextBlock _lblZoomRange;

        public LauncherSettingsWindow()
        {
            Title = "Настройки Tahoe Launcher";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;

            Width = 620;
            Height = 440;
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2.0;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2.5;

            BuildUi();

            SourceInitialized += (s, e) =>
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                DwmBlurHelper.ApplyBlur(hwnd, true);
                DwmBlurHelper.MakeToolWindow(hwnd);
            };

            MouseLeftButtonDown += (s, e) => DragMove();
        }

        public void SelectTab(int index)
        {
            if (_tabControl != null && index >= 0 && index < _tabControl.Items.Count)
            {
                _tabControl.SelectedIndex = index;
            }
        }

        private void BuildUi()
        {
            var glass = new Border
            {
                Background = LiquidGlassTheme.CreateGlassBrush(210, 245),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(18)
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header (Close circle & Title)
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Tabs
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer buttons

            // Header
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var closeBtn = new Border
            {
                Width = 12,
                Height = 12,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromRgb(255, 95, 87)),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            closeBtn.MouseLeftButtonUp += (s, e) => Close();
            Grid.SetColumn(closeBtn, 0);
            headerGrid.Children.Add(closeBtn);

            var titleTxt = new TextBlock
            {
                Text = "Настройки Tahoe",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(titleTxt, 1);
            headerGrid.Children.Add(titleTxt);

            Grid.SetRow(headerGrid, 0);
            mainGrid.Children.Add(headerGrid);

            // TabControl
            _tabControl = new TabControl
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };

            _tabControl.Items.Add(BuildGeneralTab());
            _tabControl.Items.Add(BuildAppearanceTab());
            _tabControl.Items.Add(BuildAppsTab());
            _tabControl.Items.Add(BuildAboutTab());

            Grid.SetRow(_tabControl, 1);
            mainGrid.Children.Add(_tabControl);

            // Footer
            var footerGrid = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            var btnSave = new Border
            {
                Background = LiquidGlassTheme.AccentBlueBrush,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(18, 6, 18, 6),
                HorizontalAlignment = HorizontalAlignment.Right,
                Cursor = Cursors.Hand
            };
            var txtSave = new TextBlock
            {
                Text = "Сохранить",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };
            btnSave.Child = txtSave;
            btnSave.MouseLeftButtonUp += (s, e) => ApplyAndSaveSettings();
            footerGrid.Children.Add(btnSave);

            Grid.SetRow(footerGrid, 2);
            mainGrid.Children.Add(footerGrid);

            glass.Child = mainGrid;
            Content = glass;
        }

        private TabItem BuildGeneralTab()
        {
            var tab = new TabItem { Header = "Основные" };
            var stack = new StackPanel { Margin = new Thickness(10, 14, 10, 10) };

            var cfg = ShellController.Instance.Config;

            _chkTopBar = CreateCheckBox("Отображать верхнюю панель меню (TopBar)", cfg.ShowTopBar);
            stack.Children.Add(_chkTopBar);

            _chkDock = CreateCheckBox("Отображать панель Dock", cfg.ShowDock);
            stack.Children.Add(_chkDock);

            _chkHideTaskbar = CreateCheckBox("Скрывать панель задач Windows", cfg.HideWindowsTaskbar);
            stack.Children.Add(_chkHideTaskbar);

            _chkDockAutoHide = CreateCheckBox("Автоматически скрывать Dock при неактивности", cfg.DockAutoHide);
            stack.Children.Add(_chkDockAutoHide);

            _chkWinKey = CreateCheckBox("Открывать Launchpad по нажатию клавиши Win", cfg.InterceptWinKey);
            stack.Children.Add(_chkWinKey);

            _chkWidgets = CreateCheckBox("Отображать виджеты рабочего стола", cfg.ShowDesktopWidgets);
            stack.Children.Add(_chkWidgets);

            _chkDarkMode = CreateCheckBox("Тёмная тема интерфейса (Liquid Glass Dark)", cfg.IsDarkMode);
            stack.Children.Add(_chkDarkMode);

            tab.Content = stack;
            return tab;
        }

        private TabItem BuildAppearanceTab()
        {
            var tab = new TabItem { Header = "Внешний вид" };
            var stack = new StackPanel { Margin = new Thickness(10, 14, 10, 10) };

            var cfg = ShellController.Instance.Config;

            // Icon size
            var lbl1 = new TextBlock
            {
                Text = "Базовый размер иконок Dock:",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 12,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            };
            stack.Children.Add(lbl1);

            _sliderIconSize = new Slider
            {
                Minimum = 36,
                Maximum = 72,
                Value = cfg.DockIconSize,
                SmallChange = 2,
                LargeChange = 6
            };
            _lblIconSize = new TextBlock
            {
                Text = string.Format("{0:F0} px", _sliderIconSize.Value),
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                Margin = new Thickness(0, 2, 0, 12)
            };
            _sliderIconSize.ValueChanged += (s, e) => _lblIconSize.Text = string.Format("{0:F0} px", _sliderIconSize.Value);
            stack.Children.Add(_sliderIconSize);
            stack.Children.Add(_lblIconSize);

            // Zoom range
            var lbl2 = new TextBlock
            {
                Text = "Диапазон анимации волны Dock (Zoom):",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 12,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 4, 0, 4)
            };
            stack.Children.Add(lbl2);

            _sliderZoomRange = new Slider
            {
                Minimum = 0,
                Maximum = 40,
                Value = cfg.DockZoomRange,
                SmallChange = 2,
                LargeChange = 5
            };
            _lblZoomRange = new TextBlock
            {
                Text = string.Format("+{0:F0} px", _sliderZoomRange.Value),
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                Margin = new Thickness(0, 2, 0, 12)
            };
            _sliderZoomRange.ValueChanged += (s, e) => _lblZoomRange.Text = string.Format("+{0:F0} px", _sliderZoomRange.Value);
            stack.Children.Add(_sliderZoomRange);
            stack.Children.Add(_lblZoomRange);

            tab.Content = stack;
            return tab;
        }

        private TabItem BuildAppsTab()
        {
            var tab = new TabItem { Header = "Приложения" };
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(6) };
            var listStack = new StackPanel();

            var apps = ShellController.Instance.AppCatalog.GetCatalog();
            foreach (var app in apps)
            {
                var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var txt = new TextBlock
                {
                    Text = app.Title + " (" + app.Category + ")",
                    FontFamily = LiquidGlassTheme.UiFont,
                    FontSize = 12,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(txt, 0);
                row.Children.Add(txt);

                var btnPin = new Button
                {
                    Content = "В Dock",
                    FontSize = 10,
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(4, 0, 0, 0)
                };
                btnPin.Click += (s, e) =>
                {
                    var newConfig = new DockItemConfig
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Name = app.Title,
                        LaunchCommand = app.Cmd,
                        LaunchArgs = app.Args,
                        ProcessNames = new[] { Path.GetFileNameWithoutExtension(app.Cmd) }
                    };
                    ShellController.Instance.Config.PinnedDockItems.Add(newConfig);
                    ShellController.Instance.SaveConfig();
                    if (ShellController.Instance.Dock != null)
                    {
                        ShellController.Instance.Dock.LoadDockItems();
                    }
                };
                Grid.SetColumn(btnPin, 1);
                row.Children.Add(btnPin);

                listStack.Children.Add(row);
            }

            scroll.Content = listStack;
            tab.Content = scroll;
            return tab;
        }

        private TabItem BuildAboutTab()
        {
            var tab = new TabItem { Header = "О программе" };
            var stack = new StackPanel { Margin = new Thickness(10, 20, 10, 10), HorizontalAlignment = HorizontalAlignment.Center };

            var title = new TextBlock
            {
                Text = "Tahoe Liquid Glass Launcher",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            stack.Children.Add(title);

            var ver = new TextBlock
            {
                Text = "Версия 26.2 (Модульная архитектура)\nWindows 11 Tahoe Shell Replacement",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stack.Children.Add(ver);

            var btnRestart = new Button
            {
                Content = "Перезапустить Tahoe Launcher",
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(0, 0, 0, 10)
            };
            btnRestart.Click += (s, e) =>
            {
                string exe = Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(exe, "/restart");
                ShellController.Instance.ShutdownLauncher();
            };
            stack.Children.Add(btnRestart);

            tab.Content = stack;
            return tab;
        }

        private CheckBox CreateCheckBox(string text, bool isChecked)
        {
            var chk = new CheckBox
            {
                Content = text,
                IsChecked = isChecked,
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 12,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 4, 0, 6)
            };
            return chk;
        }

        private void ApplyAndSaveSettings()
        {
            var cfg = ShellController.Instance.Config;
            cfg.ShowTopBar = _chkTopBar.IsChecked ?? true;
            cfg.ShowDock = _chkDock.IsChecked ?? true;
            cfg.HideWindowsTaskbar = _chkHideTaskbar.IsChecked ?? true;
            cfg.DockAutoHide = _chkDockAutoHide.IsChecked ?? true;
            cfg.InterceptWinKey = _chkWinKey.IsChecked ?? true;
            cfg.ShowDesktopWidgets = _chkWidgets.IsChecked ?? true;
            cfg.IsDarkMode = _chkDarkMode.IsChecked ?? true;

            if (_sliderIconSize != null) cfg.DockIconSize = _sliderIconSize.Value;
            if (_sliderZoomRange != null) cfg.DockZoomRange = _sliderZoomRange.Value;

            ShellController.Instance.SaveConfig();
            ShellController.Instance.ApplyTaskbarAndWorkArea();

            if (ShellController.Instance.Dock != null)
            {
                ShellController.Instance.Dock.LoadDockItems();
            }

            Close();
        }
    }
}
