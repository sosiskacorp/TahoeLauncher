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
    public class WindowSwitcherWindow : Window
    {
        private WrapPanel _windowsWrap;
        private TextBlock _subTitle;

        public WindowSwitcherWindow()
        {
            Title = "Обзор окон";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
            Background = new SolidColorBrush(Color.FromArgb(200, 10, 12, 20));
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();

            SourceInitialized += delegate
            {
                NativeMethods.EnableBlur(this, 0xBB0E0E16);
            };

            PreviewKeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    Hide();
                }
            };
        }

        private void BuildUI()
        {
            Grid root = new Grid { Background = Brushes.Transparent };
            root.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.OriginalSource == root) Hide();
            };

            StackPanel container = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 56, 0, 0),
                Width = Math.Min(1240, Width - 100)
            };

            Grid header = new Grid { Margin = new Thickness(12, 0, 12, 20) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titles = new StackPanel();
            titles.Children.Add(new TextBlock
            {
                Text = "Обзор активных окон",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            _subTitle = new TextBlock
            {
                Text = "Выберите окно для переключения или закройте ненужные приложения",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromArgb(175, 235, 235, 245)),
                Margin = new Thickness(0, 3, 0, 0)
            };
            titles.Children.Add(_subTitle);
            Grid.SetColumn(titles, 0);
            header.Children.Add(titles);

            StackPanel actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Border minAllBtn = CreateActionPill("🗕 Свернуть все окна", delegate
            {
                List<OpenWindowEntry> list = ShellController.Instance.GetOpenApplicationWindows();
                for (int i = 0; i < list.Count; i++)
                {
                    NativeMethods.ShowWindow(list[i].Hwnd, NativeMethods.SW_SHOWMINIMIZED);
                }
                Hide();
            });
            actions.Children.Add(minAllBtn);

            Border closeBtn = CreateActionPill("✕ Закрыть обзор (Esc)", delegate { Hide(); });
            closeBtn.Margin = new Thickness(10, 0, 0, 0);
            actions.Children.Add(closeBtn);

            Grid.SetColumn(actions, 1);
            header.Children.Add(actions);
            container.Children.Add(header);

            ScrollViewer sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = Height - 210
            };
            _windowsWrap = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };
            sv.Content = _windowsWrap;
            container.Children.Add(sv);

            root.Children.Add(container);
            Content = root;
        }

        private Border CreateActionPill(string text, Action onClick)
        {
            Border b = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(65, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 7, 14, 7),
                Cursor = Cursors.Hand
            };
            b.Child = new TextBlock { Text = text, FontSize = 12.5, FontWeight = FontWeights.Medium, Foreground = Brushes.White };
            b.MouseEnter += delegate { b.Background = LiquidGlassTheme.AccentBlueBrush; };
            b.MouseLeave += delegate { b.Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)); };
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };
            return b;
        }

        public void ShowAndRefresh()
        {
            PopulateWindows();
            Show();
            Activate();
        }

        private void PopulateWindows()
        {
            _windowsWrap.Children.Clear();
            List<OpenWindowEntry> wins = ShellController.Instance.GetOpenApplicationWindows();
            _subTitle.Text = string.Format("Открыто окон: {0} — нажмите на карточку, чтобы развернуть и активировать окно", wins.Count);

            if (wins.Count == 0)
            {
                Border emptyCard = new Border
                {
                    Width = 440,
                    CornerRadius = new CornerRadius(20),
                    Background = LiquidGlassTheme.CreateGlassBrush(200, 220),
                    BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(24),
                    Margin = new Thickness(0, 60, 0, 0)
                };
                emptyCard.Child = new TextBlock
                {
                    Text = "Нет открытых окон приложений.\r\nОткройте любое приложение из Dock или меню «Приложения» (Win).",
                    FontSize = 14,
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center
                };
                _windowsWrap.Children.Add(emptyCard);
                return;
            }

            for (int i = 0; i < wins.Count; i++)
            {
                OpenWindowEntry w = wins[i];
                Border card = new Border
                {
                    Width = 285,
                    Height = 148,
                    Margin = new Thickness(10),
                    CornerRadius = new CornerRadius(18),
                    Background = LiquidGlassTheme.CreateGlassBrush(215, 235),
                    BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(14),
                    Cursor = Cursors.Hand,
                    Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 6, Opacity = 0.5, Color = Colors.Black }
                };

                Grid g = new Grid();
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid topRow = new Grid();
                topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Image ico = new Image
                {
                    Source = w.Icon ?? ShellController.Instance.GetIcon("finder.png"),
                    Width = 34,
                    Height = 34,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                RenderOptions.SetBitmapScalingMode(ico, BitmapScalingMode.HighQuality);
                Grid.SetColumn(ico, 0);
                topRow.Children.Add(ico);

                StackPanel appInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                appInfo.Children.Add(new TextBlock
                {
                    Text = w.AppTitle,
                    FontSize = 13.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                appInfo.Children.Add(new TextBlock
                {
                    Text = w.IsMinimized ? "Свёрнуто" : "Активно",
                    FontSize = 10.5,
                    Foreground = w.IsMinimized
                        ? new SolidColorBrush(Color.FromRgb(255, 214, 10))
                        : new SolidColorBrush(Color.FromRgb(48, 209, 88))
                });
                Grid.SetColumn(appInfo, 1);
                topRow.Children.Add(appInfo);

                Border closeWinBtn = new Border
                {
                    Width = 24,
                    Height = 24,
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush(Color.FromArgb(45, 255, 95, 87)),
                    VerticalAlignment = VerticalAlignment.Top,
                    ToolTip = "Закрыть окно"
                };
                closeWinBtn.Child = new TextBlock
                {
                    Text = "✕",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                OpenWindowEntry capW = w;
                closeWinBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    NativeMethods.PostMessage(capW.Hwnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    DispatcherTimer dt = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
                    dt.Tick += delegate
                    {
                        dt.Stop();
                        PopulateWindows();
                    };
                    dt.Start();
                };
                Grid.SetColumn(closeWinBtn, 2);
                topRow.Children.Add(closeWinBtn);

                Grid.SetRow(topRow, 0);
                g.Children.Add(topRow);

                TextBlock winTitleBlock = new TextBlock
                {
                    Text = w.WindowTitle,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromArgb(215, 235, 235, 245)),
                    TextWrapping = TextWrapping.Wrap,
                    MaxHeight = 42,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 10, 0, 8)
                };
                Grid.SetRow(winTitleBlock, 1);
                g.Children.Add(winTitleBlock);

                Border switchPill = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255)),
                    Padding = new Thickness(8, 4, 8, 4),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                switchPill.Child = new TextBlock
                {
                    Text = "Переключиться на окно →",
                    FontSize = 11,
                    FontWeight = FontWeights.Medium,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(switchPill, 2);
                g.Children.Add(switchPill);

                card.Child = g;
                card.MouseEnter += delegate { card.BorderBrush = LiquidGlassTheme.AccentBlueBrush; };
                card.MouseLeave += delegate { card.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(); };
                card.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    Hide();
                    if (NativeMethods.IsIconic(capW.Hwnd))
                    {
                        NativeMethods.ShowWindow(capW.Hwnd, NativeMethods.SW_RESTORE);
                    }
                    NativeMethods.SwitchToThisWindow(capW.Hwnd, true);
                    NativeMethods.SetForegroundWindow(capW.Hwnd);
                };

                _windowsWrap.Children.Add(card);
            }
        }
    }

    // =========================================================================
    // 8. FULL GRAPHICAL LAUNCHER SETTINGS HUB (Настройки лаунчера — 4 вкладки)
    // =========================================================================
}
