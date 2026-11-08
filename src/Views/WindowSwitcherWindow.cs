using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using MacOSTahoe.Core;
using MacOSTahoe.Models;
using MacOSTahoe.Native;
using MacOSTahoe.Services;
using MacOSTahoe.Theme;

namespace MacOSTahoe.Views
{
    public class WindowSwitcherWindow : Window
    {
        private WrapPanel _windowsPanel;
        private ScrollViewer _scrollViewer;

        public WindowSwitcherWindow()
        {
            Title = "WindowSwitcherWindow";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;

            Visibility = Visibility.Collapsed;
            IsHitTestVisible = false;

            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;

            BuildUi();

            SourceInitialized += (s, e) =>
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                DwmBlurHelper.ApplyBlur(hwnd, true);
                DwmBlurHelper.MakeToolWindow(hwnd);
            };

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    HideSwitcher();
                }
            };
        }

        private void BuildUi()
        {
            var root = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 15, 15, 20))
            };
            root.MouseLeftButtonUp += (s, e) =>
            {
                if (e.OriginalSource == root || e.OriginalSource == _scrollViewer)
                {
                    HideSwitcher();
                }
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) }); // Header
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Cards

            var headerText = new TextBlock
            {
                Text = "Открытые окна",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(headerText, 0);
            grid.Children.Add(headerText);

            _scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(60, 10, 60, 40)
            };

            _windowsPanel = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _scrollViewer.Content = _windowsPanel;
            Grid.SetRow(_scrollViewer, 1);
            grid.Children.Add(_scrollViewer);

            root.Children.Add(grid);
            Content = root;
        }

        public void PopulateWindows()
        {
            _windowsPanel.Children.Clear();
            var openWins = ShellController.Instance.WindowTracker.GetOpenWindows();

            if (openWins.Count == 0)
            {
                var emptyNotice = new TextBlock
                {
                    Text = "Нет открытых окон",
                    FontFamily = LiquidGlassTheme.UiFont,
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
                    Margin = new Thickness(0, 100, 0, 0)
                };
                _windowsPanel.Children.Add(emptyNotice);
                return;
            }

            foreach (var win in openWins)
            {
                var card = CreateWindowCard(win);
                _windowsPanel.Children.Add(card);
            }
        }

        private UIElement CreateWindowCard(OpenWindowEntry win)
        {
            var card = new Border
            {
                Width = 260,
                Height = 160,
                CornerRadius = new CornerRadius(14),
                Background = LiquidGlassTheme.CreateGlassBrush(140, 180),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(14),
                Padding = new Thickness(12),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header (Icon + Title + Close)
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Preview placeholder

            // Header
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            if (win.Icon != null)
            {
                var iconImg = new Image
                {
                    Width = 22,
                    Height = 22,
                    Source = win.Icon,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                Grid.SetColumn(iconImg, 0);
                headerGrid.Children.Add(iconImg);
            }

            var titleText = new TextBlock
            {
                Text = win.WindowTitle,
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleText, 1);
            headerGrid.Children.Add(titleText);

            // Close button
            var closeBtn = new Border
            {
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            var closeTxt = new TextBlock
            {
                Text = "✕",
                FontSize = 10,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            closeBtn.Child = closeTxt;
            closeBtn.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                WindowTrackerService.CloseWindow(win.Hwnd);
                _windowsPanel.Children.Remove(card);
            };
            Grid.SetColumn(closeBtn, 2);
            headerGrid.Children.Add(closeBtn);

            Grid.SetRow(headerGrid, 0);
            grid.Children.Add(headerGrid);

            // Body
            var bodyBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 8, 0, 0)
            };
            var procText = new TextBlock
            {
                Text = win.ProcessName,
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            bodyBorder.Child = procText;

            Grid.SetRow(bodyBorder, 1);
            grid.Children.Add(bodyBorder);

            card.Child = grid;

            // Hover
            card.MouseEnter += (s, e) =>
            {
                card.BorderBrush = LiquidGlassTheme.AccentBlueBrush;
            };
            card.MouseLeave += (s, e) =>
            {
                card.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush();
            };

            // Click to activate
            card.MouseLeftButtonUp += (s, e) =>
            {
                HideSwitcher();
                WindowTrackerService.ActivateWindow(win.Hwnd);
            };

            return card;
        }

        public void ShowSwitcher()
        {
            Visibility = Visibility.Visible;
            IsHitTestVisible = true;
            PopulateWindows();
            Activate();
        }

        public void HideSwitcher()
        {
            Visibility = Visibility.Collapsed;
            IsHitTestVisible = false;
        }
    }
}
