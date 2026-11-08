using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using MacOSTahoe.Core;
using MacOSTahoe.Native;
using MacOSTahoe.Theme;

namespace MacOSTahoe.Views
{
    public class ControlCenterWindow : Window
    {
        private Border _toggleDarkMode;
        private Border _toggleDockHide;
        private TextBlock _txtUptime;
        private TextBlock _txtRam;

        public ControlCenterWindow()
        {
            Title = "ControlCenterWindow";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;

            Visibility = Visibility.Collapsed;
            IsHitTestVisible = false;

            Width = 320;
            Height = 360;
            Left = SystemParameters.PrimaryScreenWidth - Width - 14;
            Top = 40;

            BuildUi();

            SourceInitialized += (s, e) =>
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                DwmBlurHelper.ApplyBlur(hwnd, true);
                DwmBlurHelper.MakeToolWindow(hwnd);
            };

            Deactivated += (s, e) =>
            {
                HideControlCenter();
            };
        }

        private void BuildUi()
        {
            var glass = new Border
            {
                Background = LiquidGlassTheme.CreateGlassBrush(180, 220),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(14)
            };

            var stack = new StackPanel { Orientation = Orientation.Vertical };

            // Title
            var title = new TextBlock
            {
                Text = "Пункт управления",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Margin = new Thickness(4, 0, 0, 10)
            };
            stack.Children.Add(title);

            // 2x2 Quick toggles grid
            var togglesGrid = new Grid();
            togglesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            togglesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            togglesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });
            togglesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });

            var btnWifi = CreateToggleCard("📶", "Wi-Fi", "Сеть", () => Process.Start("ms-settings:network-wifi"));
            Grid.SetRow(btnWifi, 0); Grid.SetColumn(btnWifi, 0);
            togglesGrid.Children.Add(btnWifi);

            var btnBt = CreateToggleCard("⚡", "Bluetooth", "Устройства", () => Process.Start("ms-settings:bluetooth"));
            Grid.SetRow(btnBt, 0); Grid.SetColumn(btnBt, 1);
            togglesGrid.Children.Add(btnBt);

            _toggleDarkMode = CreateToggleCard("🌙", "Тёмная тема", "Вкл", () =>
            {
                var cfg = ShellController.Instance.Config;
                cfg.IsDarkMode = !cfg.IsDarkMode;
                ShellController.Instance.SaveConfig();
                RefreshToggles();
            });
            Grid.SetRow(_toggleDarkMode, 1); Grid.SetColumn(_toggleDarkMode, 0);
            togglesGrid.Children.Add(_toggleDarkMode);

            _toggleDockHide = CreateToggleCard("⚓", "Dock", "Автоскрытие", () =>
            {
                var cfg = ShellController.Instance.Config;
                cfg.DockAutoHide = !cfg.DockAutoHide;
                ShellController.Instance.SaveConfig();
                ShellController.Instance.ApplyTaskbarAndWorkArea();
                RefreshToggles();
            });
            Grid.SetRow(_toggleDockHide, 1); Grid.SetColumn(_toggleDockHide, 1);
            togglesGrid.Children.Add(_toggleDockHide);

            stack.Children.Add(togglesGrid);

            // Sound slider
            var soundCard = CreateSliderCard("🔊 Звук", 75);
            soundCard.Margin = new Thickness(0, 10, 0, 0);
            stack.Children.Add(soundCard);

            // Telemetry Info Card
            var infoCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 10, 0, 0)
            };
            var infoStack = new StackPanel();

            _txtRam = new TextBlock
            {
                Text = "Память: RAM --%",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 12,
                Foreground = Brushes.White
            };
            infoStack.Children.Add(_txtRam);

            _txtUptime = new TextBlock
            {
                Text = "Время работы системы: --",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                Margin = new Thickness(0, 4, 0, 0)
            };
            infoStack.Children.Add(_txtUptime);
            infoCard.Child = infoStack;
            stack.Children.Add(infoCard);

            // Settings button at bottom
            var btnSettings = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(0, 8, 0, 8),
                Margin = new Thickness(0, 10, 0, 0),
                Cursor = Cursors.Hand
            };
            var txtSettings = new TextBlock
            {
                Text = "Настройки Tahoe Launcher",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            btnSettings.Child = txtSettings;
            btnSettings.MouseLeftButtonUp += (s, e) =>
            {
                HideControlCenter();
                ShellController.Instance.OpenSettings();
            };
            stack.Children.Add(btnSettings);

            glass.Child = stack;
            Content = glass;
        }

        private Border CreateToggleCard(string icon, string title, string subtitle, Action onClick)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(4),
                Padding = new Thickness(8),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var txtIcon = new TextBlock
            {
                Text = icon,
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(txtIcon, 0);
            grid.Children.Add(txtIcon);

            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var txtTitle = new TextBlock
            {
                Text = title,
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White
            };
            textStack.Children.Add(txtTitle);

            var txtSub = new TextBlock
            {
                Text = subtitle,
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255))
            };
            textStack.Children.Add(txtSub);
            Grid.SetColumn(textStack, 1);
            grid.Children.Add(textStack);

            card.Child = grid;
            card.MouseLeftButtonUp += (s, e) =>
            {
                if (onClick != null) onClick();
            };

            return card;
        }

        private Border CreateSliderCard(string label, double defaultValue)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 8, 10, 8)
            };

            var stack = new StackPanel();
            var lbl = new TextBlock
            {
                Text = label,
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 12,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            };
            stack.Children.Add(lbl);

            var slider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                Value = defaultValue,
                Foreground = LiquidGlassTheme.AccentBlueBrush
            };
            stack.Children.Add(slider);

            card.Child = stack;
            return card;
        }

        private void RefreshToggles()
        {
            // Update RAM & Uptime
            try
            {
                var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)) };
                if (Win32Api.GlobalMemoryStatusEx(ref mem))
                {
                    _txtRam.Text = string.Format("Память: {0}% занято ({1:F1} ГБ свободно)", mem.dwMemoryLoad, mem.ullAvailPhys / (1024.0 * 1024.0 * 1024.0));
                }

                TimeSpan uptime = TimeSpan.FromMilliseconds(Environment.TickCount);
                _txtUptime.Text = string.Format("Время работы: {0} ч {1} мин", (int)uptime.TotalHours, uptime.Minutes);
            }
            catch { }
        }

        public void ShowControlCenter()
        {
            Visibility = Visibility.Visible;
            IsHitTestVisible = true;
            RefreshToggles();
            Activate();
        }

        public void HideControlCenter()
        {
            Visibility = Visibility.Collapsed;
            IsHitTestVisible = false;
        }
    }
}
