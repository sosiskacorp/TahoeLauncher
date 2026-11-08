using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
    public class AppSwitcherWindow : Window
    {
        private StackPanel _iconsStack;
        private TextBlock _appNameText;
        private Border _glassCard;
        private List<OpenWindowEntry> _apps = new List<OpenWindowEntry>();
        private List<Border> _itemCards = new List<Border>();
        private int _selectedIndex = 0;

        public AppSwitcherWindow()
        {
            Title = "Command + Tab";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;
            SizeToContent = SizeToContent.WidthAndHeight;

            BuildUI();

            Visibility = Visibility.Collapsed;

            SourceInitialized += delegate
            {
                NativeMethods.MakeToolWindow(this, true);
                // Do not call EnableBlur here: in WPF with AllowsTransparency=true, DWM EnableBlur
                // produces an opaque black rectangle around transparent rounded corners.
            };

            Deactivated += delegate
            {
                if (IsVisible)
                {
                    CommitSelection();
                }
            };
        }

        private void BuildUI()
        {
            _glassCard = new Border
            {
                CornerRadius = new CornerRadius(22),
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(175, 42, 45, 58), 0.0),
                        new GradientStop(Color.FromArgb(195, 24, 26, 36), 1.0)
                    }
                },
                BorderBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(140, 255, 255, 255), 0.0),
                        new GradientStop(Color.FromArgb(40, 255, 255, 255), 1.0)
                    }
                },
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16, 14, 16, 12),
                Margin = new Thickness(36), // Margin prevents drop shadow clipping
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _glassCard.Effect = new DropShadowEffect
            {
                BlurRadius = 36,
                ShadowDepth = 8,
                Opacity = 0.38,
                Color = Colors.Black
            };

            StackPanel root = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            ScrollViewer sv = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _iconsStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            sv.Content = _iconsStack;
            root.Children.Add(sv);

            _appNameText = new TextBlock
            {
                Text = "",
                FontSize = 13.0,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(245, 245, 247)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0),
                TextAlignment = TextAlignment.Center
            };
            _appNameText.Effect = new DropShadowEffect
            {
                BlurRadius = 4,
                ShadowDepth = 1,
                Opacity = 0.6,
                Color = Colors.Black
            };
            root.Children.Add(_appNameText);

            _glassCard.Child = root;
            Content = _glassCard;
        }

        public void ShowAndStep(bool backward)
        {
            if (!IsVisible)
            {
                _apps = ShellController.Instance.GetOpenApplicationWindows();
                if (_apps.Count == 0) return;

                // Group by app/process title to avoid duplicate icons in Cmd+Tab
                List<OpenWindowEntry> unique = new List<OpenWindowEntry>();
                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < _apps.Count; i++)
                {
                    string key = _apps[i].AppTitle;
                    if (!seen.Contains(key))
                    {
                        seen.Add(key);
                        unique.Add(_apps[i]);
                    }
                }
                _apps = unique;

                RebuildVisuals();

                // Select second app (most recently used) if available, like macOS Cmd+Tab
                _selectedIndex = (_apps.Count > 1) ? 1 : 0;
                HighlightSelection();

                Show();
                Activate();
                UpdateLayout();
                Left = (SystemParameters.PrimaryScreenWidth - ActualWidth) / 2.0;
                Top = (SystemParameters.PrimaryScreenHeight - ActualHeight) / 2.0;
            }
            else
            {
                if (_apps.Count == 0) return;
                if (!backward)
                {
                    _selectedIndex = (_selectedIndex + 1) % _apps.Count;
                }
                else
                {
                    _selectedIndex = (_selectedIndex - 1 + _apps.Count) % _apps.Count;
                }
                HighlightSelection();
            }
        }

        private void RebuildVisuals()
        {
            _iconsStack.Children.Clear();
            _itemCards.Clear();

            for (int i = 0; i < _apps.Count; i++)
            {
                int idx = i;
                OpenWindowEntry app = _apps[i];

                Border card = new Border
                {
                    Width = 68,
                    Height = 68,
                    CornerRadius = new CornerRadius(14),
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(4, 0, 4, 0),
                    Cursor = Cursors.Hand
                };

                Grid iconGrid = new Grid();
                Image iconImg = new Image
                {
                    Width = 50,
                    Height = 50,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(iconImg, BitmapScalingMode.HighQuality);
                iconImg.Source = app.Icon != null ? app.Icon : ShellController.Instance.DefaultAppIcon;
                iconGrid.Children.Add(iconImg);

                card.Child = iconGrid;

                card.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    _selectedIndex = idx;
                    HighlightSelection();
                    CommitSelection();
                };

                _itemCards.Add(card);
                _iconsStack.Children.Add(card);
            }
        }

        private void HighlightSelection()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _apps.Count) return;

            for (int i = 0; i < _itemCards.Count; i++)
            {
                if (i == _selectedIndex)
                {
                    _itemCards[i].Background = new SolidColorBrush(Color.FromArgb(75, 255, 255, 255));
                    _itemCards[i].BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255));
                }
                else
                {
                    _itemCards[i].Background = Brushes.Transparent;
                    _itemCards[i].BorderBrush = Brushes.Transparent;
                }
            }

            _appNameText.Text = _apps[_selectedIndex].AppTitle;
        }

        public void CommitSelection()
        {
            if (!IsVisible) return;
            Hide();

            // Release any stuck Alt key states in Windows
            NativeMethods.keybd_event(0x12, 0, 2, UIntPtr.Zero);
            NativeMethods.keybd_event(0xA4, 0, 2, UIntPtr.Zero);
            NativeMethods.keybd_event(0xA5, 0, 2, UIntPtr.Zero);

            if (_selectedIndex >= 0 && _selectedIndex < _apps.Count)
            {
                IntPtr hwnd = _apps[_selectedIndex].Hwnd;
                if (hwnd != IntPtr.Zero && NativeMethods.IsWindow(hwnd))
                {
                    if (NativeMethods.IsIconic(hwnd))
                    {
                        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
                    }
                    else
                    {
                        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOW);
                    }
                    NativeMethods.SetForegroundWindow(hwnd);
                    NativeMethods.SwitchToThisWindow(hwnd, false);
                }
            }
        }

        public void QuitSelectedApp()
        {
            if (!IsVisible || _selectedIndex < 0 || _selectedIndex >= _apps.Count) return;
            OpenWindowEntry target = _apps[_selectedIndex];
            if (target.Hwnd != IntPtr.Zero && NativeMethods.IsWindow(target.Hwnd))
            {
                NativeMethods.PostMessage(target.Hwnd, 0x0010 /* WM_CLOSE */, IntPtr.Zero, IntPtr.Zero);
            }

            _apps.RemoveAt(_selectedIndex);
            if (_apps.Count == 0)
            {
                Hide();
                return;
            }

            if (_selectedIndex >= _apps.Count) _selectedIndex = _apps.Count - 1;
            RebuildVisuals();
            HighlightSelection();
            UpdateLayout();
            Left = (SystemParameters.PrimaryScreenWidth - ActualWidth) / 2.0;
            Top = (SystemParameters.PrimaryScreenHeight - ActualHeight) / 2.0;
        }

        public void HideSelectedApp()
        {
            if (!IsVisible || _selectedIndex < 0 || _selectedIndex >= _apps.Count) return;
            OpenWindowEntry target = _apps[_selectedIndex];
            if (target.Hwnd != IntPtr.Zero && NativeMethods.IsWindow(target.Hwnd))
            {
                NativeMethods.ShowWindow(target.Hwnd, NativeMethods.SW_SHOWMINIMIZED);
            }
            Hide();
        }

        public void Cancel()
        {
            Hide();
            NativeMethods.keybd_event(0x12, 0, 2, UIntPtr.Zero);
            NativeMethods.keybd_event(0xA4, 0, 2, UIntPtr.Zero);
            NativeMethods.keybd_event(0xA5, 0, 2, UIntPtr.Zero);
        }
    }
}
