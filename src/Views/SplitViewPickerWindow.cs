using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace MacOSTahoe
{
    public class SplitViewPickerWindow : Window
    {
        private IntPtr _firstHwnd;
        private bool _firstIsLeft;
        private WrapPanel _cardsWrap;

        public SplitViewPickerWindow()
        {
            Title = "Выбор окна для Split View";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();

            Visibility = Visibility.Collapsed;

            SourceInitialized += delegate
            {
                NativeMethods.MakeToolWindow(this, true);
                NativeMethods.EnableBlur(this, 0xDD10121A);
            };

            PreviewKeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    Cancel();
                }
            };
        }

        private void BuildUI()
        {
            Border outer = new Border
            {
                Background = LiquidGlassTheme.CreateGlassBrush(210, 235),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(18),
                Margin = new Thickness(10)
            };
            outer.Effect = new DropShadowEffect
            {
                BlurRadius = 32,
                ShadowDepth = 8,
                Opacity = 0.65,
                Color = Colors.Black
            };

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Header
            Grid header = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titleStack = new StackPanel();
            titleStack.Children.Add(new TextBlock
            {
                Text = "◫ Разделение экрана (Split View)",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = "Выберите второе окно для заполнения экрана",
                FontSize = 12.5,
                Foreground = new SolidColorBrush(Color.FromArgb(170, 235, 235, 245)),
                Margin = new Thickness(0, 3, 0, 0)
            });
            Grid.SetColumn(titleStack, 0);
            header.Children.Add(titleStack);

            Border cancelBtn = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 5, 10, 5),
                Cursor = Cursors.Hand
            };
            cancelBtn.Child = new TextBlock { Text = "✕ Отмена (Esc)", FontSize = 12, Foreground = Brushes.White };
            cancelBtn.MouseEnter += delegate { cancelBtn.Background = LiquidGlassTheme.AccentBlueBrush; };
            cancelBtn.MouseLeave += delegate { cancelBtn.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)); };
            cancelBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                Cancel();
            };
            Grid.SetColumn(cancelBtn, 1);
            header.Children.Add(cancelBtn);

            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // Cards Viewer
            ScrollViewer sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            _cardsWrap = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };
            sv.Content = _cardsWrap;
            Grid.SetRow(sv, 1);
            root.Children.Add(sv);

            outer.Child = root;
            Content = outer;
        }

        public void ShowPicker(IntPtr firstHwnd, bool firstIsLeft)
        {
            _firstHwnd = firstHwnd;
            _firstIsLeft = firstIsLeft;

            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;
            double topBarH = (ShellController.Instance != null && ShellController.Instance.Config.ShowTopBar) ? 32.0 : 0.0;
            double dockH = (ShellController.Instance != null && ShellController.Instance.Config.ShowDock && !ShellController.Instance.Config.DockAutoHide) ? 76.0 : 0.0;
            double usableH = screenH - topBarH - dockH;
            double halfW = screenW / 2.0;

            if (firstIsLeft)
            {
                Left = halfW;
                Top = topBarH;
                Width = halfW;
                Height = usableH;
            }
            else
            {
                Left = 0;
                Top = topBarH;
                Width = halfW;
                Height = usableH;
            }

            _cardsWrap.Children.Clear();
            List<OpenWindowEntry> allWins = ShellController.Instance.GetOpenApplicationWindows();
            int candidateCount = 0;

            for (int i = 0; i < allWins.Count; i++)
            {
                OpenWindowEntry w = allWins[i];
                if (w.Hwnd == _firstHwnd) continue;

                Border card = CreateWindowCard(w);
                _cardsWrap.Children.Add(card);
                candidateCount++;
            }

            if (candidateCount == 0)
            {
                Border noWins = new Border
                {
                    CornerRadius = new CornerRadius(14),
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    Padding = new Thickness(20),
                    Margin = new Thickness(20)
                };
                noWins.Child = new TextBlock
                {
                    Text = "Нет других открытых окон для Split View.\r\nЗапустите вторую программу и повторите.",
                    FontSize = 13,
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center
                };
                _cardsWrap.Children.Add(noWins);
            }

            Show();
            Activate();
        }

        private Border CreateWindowCard(OpenWindowEntry win)
        {
            Border card = new Border
            {
                Width = 200,
                Height = 130,
                CornerRadius = new CornerRadius(14),
                Background = LiquidGlassTheme.CreateGlassBrush(170, 200),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10),
                Margin = new Thickness(8),
                Cursor = Cursors.Hand
            };
            card.Effect = new DropShadowEffect
            {
                BlurRadius = 14,
                ShadowDepth = 4,
                Opacity = 0.45,
                Color = Colors.Black
            };

            Grid g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Row 0: App Icon + Title
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Image icon = new Image
            {
                Width = 20,
                Height = 20,
                Source = win.Icon != null ? win.Icon : ShellController.Instance.DefaultAppIcon,
                Margin = new Thickness(0, 0, 6, 0)
            };
            RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.HighQuality);
            sp.Children.Add(icon);
            sp.Children.Add(new TextBlock
            {
                Text = win.AppTitle,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 150
            });
            Grid.SetRow(sp, 0);
            g.Children.Add(sp);

            // Row 1: Window title
            Border body = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 4, 0, 0)
            };
            body.Child = new TextBlock
            {
                Text = win.WindowTitle,
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromArgb(190, 235, 235, 245)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(body, 1);
            g.Children.Add(body);

            card.Child = g;

            card.MouseEnter += delegate
            {
                card.BorderBrush = LiquidGlassTheme.AccentBlueBrush;
                card.Background = LiquidGlassTheme.CreateGlassBrush(200, 230);
            };
            card.MouseLeave += delegate
            {
                card.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush();
                card.Background = LiquidGlassTheme.CreateGlassBrush(170, 200);
            };

            card.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                Hide();
                if (_firstIsLeft)
                {
                    ShellController.Instance.EnterSplitView(_firstHwnd, win.Hwnd, 0.5);
                }
                else
                {
                    ShellController.Instance.EnterSplitView(win.Hwnd, _firstHwnd, 0.5);
                }
            };

            return card;
        }

        public void Cancel()
        {
            Hide();
        }
    }
}
