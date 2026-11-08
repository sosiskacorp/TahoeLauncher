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
using System.Windows.Shapes;

namespace MacOSTahoe
{
    public class SplitViewDividerWindow : Window
    {
        public IntPtr LeftHwnd { get; set; }
        public IntPtr RightHwnd { get; set; }
        public double SplitRatio { get; set; }

        private bool _isDragging = false;
        private Point _dragStartPoint;
        private double _startRatio;
        private List<IntPtr> _isolatedMinWindows = new List<IntPtr>();
        private Popup _menuPopup;

        public SplitViewDividerWindow()
        {
            Title = "SplitViewDivider";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Background = Brushes.Transparent;
            Cursor = Cursors.SizeWE;
            SplitRatio = 0.5;

            Width = 14;
            BuildUI();

            Visibility = Visibility.Collapsed;

            SourceInitialized += delegate
            {
                NativeMethods.MakeToolWindow(this, true);
            };

            MouseLeftButtonDown += OnMouseDown;
            MouseLeftButtonUp += OnMouseUp;
            MouseMove += OnMouseMove;
            MouseDoubleClick += OnMouseDoubleClick;
            MouseRightButtonUp += OnMouseRightButtonUp;

            PreviewKeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    ShellController.Instance.ExitSplitView();
                }
            };
        }

        private void BuildUI()
        {
            Border root = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(240, 12, 14, 20)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1, 0, 1, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            Border pill = new Border
            {
                Width = 4,
                Height = 48,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(Color.FromArgb(235, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            pill.Effect = new DropShadowEffect
            {
                BlurRadius = 8,
                ShadowDepth = 0,
                Opacity = 0.5,
                Color = Colors.Black
            };
            root.Child = pill;
            Content = root;
        }

        public void IsolateAndApplyLayout()
        {
            _isolatedMinWindows.Clear();
            List<OpenWindowEntry> allWins = ShellController.Instance.GetOpenApplicationWindows();
            for (int i = 0; i < allWins.Count; i++)
            {
                IntPtr h = allWins[i].Hwnd;
                if (h != LeftHwnd && h != RightHwnd && NativeMethods.IsWindow(h) && !NativeMethods.IsIconic(h))
                {
                    _isolatedMinWindows.Add(h);
                    NativeMethods.ShowWindow(h, NativeMethods.SW_SHOWMINIMIZED);
                }
            }
            ApplyLayout();
        }

        public void RestoreIsolatedWindows()
        {
            for (int i = 0; i < _isolatedMinWindows.Count; i++)
            {
                IntPtr h = _isolatedMinWindows[i];
                if (NativeMethods.IsWindow(h))
                {
                    NativeMethods.ShowWindow(h, NativeMethods.SW_RESTORE);
                }
            }
            _isolatedMinWindows.Clear();
        }

        public void ApplyLayout()
        {
            int screenW = (int)SystemParameters.PrimaryScreenWidth;
            int screenH = (int)SystemParameters.PrimaryScreenHeight;
            int topBarH = (ShellController.Instance.TopBar != null && ShellController.Instance.Config.ShowTopBar) ? 32 : 0;
            int dockH = (ShellController.Instance.Dock != null && ShellController.Instance.Config.ShowDock && !ShellController.Instance.Config.DockAutoHide) ? 76 : 0;
            int usableH = screenH - topBarH - dockH;

            int splitX = (int)(screenW * SplitRatio);

            // Update divider window position
            Left = splitX - 7;
            Top = topBarH;
            Height = usableH;
            Width = 14;

            // Update left window
            if (LeftHwnd != IntPtr.Zero && NativeMethods.IsWindow(LeftHwnd))
            {
                if (NativeMethods.IsIconic(LeftHwnd)) NativeMethods.ShowWindow(LeftHwnd, NativeMethods.SW_RESTORE);
                NativeMethods.MoveWindow(LeftHwnd, 0, topBarH, splitX - 3, usableH, true);
            }

            // Update right window
            if (RightHwnd != IntPtr.Zero && NativeMethods.IsWindow(RightHwnd))
            {
                if (NativeMethods.IsIconic(RightHwnd)) NativeMethods.ShowWindow(RightHwnd, NativeMethods.SW_RESTORE);
                NativeMethods.MoveWindow(RightHwnd, splitX + 3, topBarH, screenW - splitX - 3, usableH, true);
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(this);
                _startRatio = SplitRatio;
                CaptureMouse();
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ReleaseMouseCapture();

                // If dragged near edges, exit Split View
                if (SplitRatio < 0.15 || SplitRatio > 0.85)
                {
                    ShellController.Instance.ExitSplitView();
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                NativeMethods.POINT pt;
                if (NativeMethods.GetCursorPos(out pt))
                {
                    double screenW = SystemParameters.PrimaryScreenWidth;
                    double newRatio = pt.X / screenW;
                    if (newRatio >= 0.20 && newRatio <= 0.80)
                    {
                        SplitRatio = newRatio;
                        ApplyLayout();
                    }
                }
            }
        }

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to 50/50 on double click
            SplitRatio = 0.5;
            ApplyLayout();
        }

        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ShowPresetsMenu();
        }

        private void ShowPresetsMenu()
        {
            if (_menuPopup != null && _menuPopup.IsOpen)
            {
                _menuPopup.IsOpen = false;
            }

            Popup popup = new Popup
            {
                AllowsTransparency = true,
                PlacementTarget = this,
                Placement = PlacementMode.MousePoint,
                StaysOpen = false,
                PopupAnimation = PopupAnimation.Fade
            };

            Border card = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = LiquidGlassTheme.CreateGlassBrush(230, 245),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                MinWidth = 180
            };
            card.Effect = new DropShadowEffect { BlurRadius = 18, ShadowDepth = 6, Opacity = 0.55, Color = Colors.Black };

            StackPanel sp = new StackPanel();
            sp.Children.Add(CreateMenuRow("◫ 50 / 50 Пополам", delegate { SplitRatio = 0.5; ApplyLayout(); }));
            sp.Children.Add(CreateMenuRow("◧ 70 / 30 Слева шире", delegate { SplitRatio = 0.7; ApplyLayout(); }));
            sp.Children.Add(CreateMenuRow("◨ 30 / 70 Справа шире", delegate { SplitRatio = 0.3; ApplyLayout(); }));
            sp.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), Margin = new Thickness(4, 3, 4, 3) });
            sp.Children.Add(CreateMenuRow("✕ Выйти из Split View (Esc)", delegate { ShellController.Instance.ExitSplitView(); }));

            card.Child = sp;
            popup.Child = card;
            _menuPopup = popup;
            popup.IsOpen = true;
        }

        private Border CreateMenuRow(string text, Action onClick)
        {
            Border b = new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 5, 10, 5),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand
            };
            b.Child = new TextBlock { Text = text, FontSize = 12, Foreground = Brushes.White };
            b.MouseEnter += delegate { b.Background = LiquidGlassTheme.AccentBlueBrush; };
            b.MouseLeave += delegate { b.Background = Brushes.Transparent; };
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (_menuPopup != null) _menuPopup.IsOpen = false;
                if (onClick != null) onClick();
            };
            return b;
        }
    }
}