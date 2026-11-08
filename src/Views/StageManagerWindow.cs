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
    public class StageManagerWindow : Window
    {
        private StackPanel _shelfStack;
        private DispatcherTimer _pollTimer;
        private List<OpenWindowEntry> _recentApps = new List<OpenWindowEntry>();

        public StageManagerWindow()
        {
            Title = "Постановщик (Stage Manager)";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            Left = 4;
            Top = 38;
            Width = 92;
            Height = Math.Max(300, SystemParameters.PrimaryScreenHeight - 120);

            BuildUI();

            Visibility = Visibility.Collapsed;

            SourceInitialized += delegate
            {
                NativeMethods.MakeToolWindow(this, true);
            };

            MouseEnter += delegate
            {
                Width = 140;
                RefreshShelf();
            };

            MouseLeave += delegate
            {
                Width = 92;
                RefreshShelf();
            };

            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _pollTimer.Tick += delegate
            {
                if (IsVisible)
                {
                    RefreshShelf();
                }
            };
            _pollTimer.Start();
        }

        private void BuildUI()
        {
            ScrollViewer sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalAlignment = VerticalAlignment.Center
            };

            _shelfStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            sv.Content = _shelfStack;
            Content = sv;
        }

        public void RefreshShelf()
        {
            if (ShellController.Instance == null || !ShellController.Instance.Config.EnableStageManager)
            {
                if (IsVisible) Hide();
                return;
            }

            if (!IsVisible)
            {
                Show();
            }

            _shelfStack.Children.Clear();
            List<OpenWindowEntry> allWins = ShellController.Instance.GetOpenApplicationWindows();
            if (allWins.Count == 0) return;

            IntPtr fg = NativeMethods.GetForegroundWindow();

            // Filter out foreground window from the shelf
            List<OpenWindowEntry> backgroundWins = new List<OpenWindowEntry>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < allWins.Count; i++)
            {
                OpenWindowEntry w = allWins[i];
                if (w.Hwnd != fg && !seen.Contains(w.AppTitle))
                {
                    seen.Add(w.AppTitle);
                    backgroundWins.Add(w);
                    if (backgroundWins.Count >= 5) break; // Limit to 5 stacks
                }
            }

            _recentApps = backgroundWins;

            bool isExpanded = (Width > 100);

            for (int i = 0; i < _recentApps.Count; i++)
            {
                OpenWindowEntry app = _recentApps[i];
                Border card = CreateStageCard(app, isExpanded);
                _shelfStack.Children.Add(card);
            }
        }

        private Border CreateStageCard(OpenWindowEntry app, bool expanded)
        {
            Border card = new Border
            {
                Width = expanded ? 128 : 80,
                Height = 68,
                CornerRadius = new CornerRadius(14),
                Background = LiquidGlassTheme.CreateGlassBrush(160, 200),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(4, 6, 4, 6),
                Padding = new Thickness(8),
                Cursor = Cursors.Hand
            };
            card.Effect = new DropShadowEffect
            {
                BlurRadius = 14,
                ShadowDepth = 4,
                Opacity = 0.45,
                Color = Colors.Black
            };

            Grid grid = new Grid();
            if (expanded)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                Image icon = new Image
                {
                    Width = 34,
                    Height = 34,
                    Source = app.Icon != null ? app.Icon : ShellController.Instance.DefaultAppIcon,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.HighQuality);
                Grid.SetColumn(icon, 0);
                grid.Children.Add(icon);

                TextBlock title = new TextBlock
                {
                    Text = app.AppTitle,
                    FontSize = 11,
                    FontWeight = FontWeights.Medium,
                    Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 0, 0)
                };
                Grid.SetColumn(title, 1);
                grid.Children.Add(title);
            }
            else
            {
                Image icon = new Image
                {
                    Width = 38,
                    Height = 38,
                    Source = app.Icon != null ? app.Icon : ShellController.Instance.DefaultAppIcon,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.HighQuality);
                grid.Children.Add(icon);
            }

            card.Child = grid;

            card.MouseEnter += delegate
            {
                card.Background = LiquidGlassTheme.AccentBlueBrush;
            };
            card.MouseLeave += delegate
            {
                card.Background = LiquidGlassTheme.CreateGlassBrush(160, 200);
            };

            card.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (app.Hwnd != IntPtr.Zero)
                {
                    if (NativeMethods.IsIconic(app.Hwnd))
                    {
                        NativeMethods.ShowWindow(app.Hwnd, NativeMethods.SW_RESTORE);
                    }
                    NativeMethods.SetForegroundWindow(app.Hwnd);
                    NativeMethods.SwitchToThisWindow(app.Hwnd, true);
                    RefreshShelf();
                }
            };

            return card;
        }
    }
}
