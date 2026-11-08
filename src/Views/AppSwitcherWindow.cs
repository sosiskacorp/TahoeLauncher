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
    public class AppSwitcherWindow : Window
    {
        private StackPanel _iconsStack;
        private TextBlock _appNameText;
        private List<OpenWindowEntry> _apps = new List<OpenWindowEntry>();
        private List<Border> _itemCards = new List<Border>();
        private int _selectedIndex = 0;

        public AppSwitcherWindow()
        {
            Title = "Переключатель программ (Command + Tab)";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            Width = 600;
            Height = 140;
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2.0;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2.0;

            BuildUI();

            Visibility = Visibility.Collapsed;

            SourceInitialized += delegate
            {
                NativeMethods.MakeToolWindow(this, true);
                NativeMethods.EnableBlur(this, 0xDD12121A);
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
            Border glassCard = new Border
            {
                CornerRadius = new CornerRadius(24),
                Background = LiquidGlassTheme.CreateGlassBrush(230, 246),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(18, 14, 18, 12),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            glassCard.Effect = new DropShadowEffect
            {
                BlurRadius = 40,
                ShadowDepth = 12,
                Opacity = 0.75,
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
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0),
                TextAlignment = TextAlignment.Center
            };
            root.Children.Add(_appNameText);

            glassCard.Child = root;
            Content = glassCard;
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

                // Center dynamically
                double desiredWidth = Math.Min(SystemParameters.PrimaryScreenWidth - 100, Math.Max(260, (_apps.Count * 76) + 60));
                Width = desiredWidth;
                Left = (SystemParameters.PrimaryScreenWidth - Width) / 2.0;
                Top = (SystemParameters.PrimaryScreenHeight - Height) / 2.0;

                Show();
                Activate();
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
                    Width = 66,
                    Height = 66,
                    CornerRadius = new CornerRadius(16),
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(4, 0, 4, 0),
                    Cursor = Cursors.Hand
                };

                Grid iconGrid = new Grid();
                Image iconImg = new Image
                {
                    Width = 48,
                    Height = 48,
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
                    _itemCards[i].Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
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

            if (_selectedIndex >= 0 && _selectedIndex < _apps.Count)
            {
                IntPtr hwnd = _apps[_selectedIndex].Hwnd;
                if (hwnd != IntPtr.Zero)
                {
                    if (NativeMethods.IsIconic(hwnd))
                    {
                        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
                    }
                    NativeMethods.SetForegroundWindow(hwnd);
                    NativeMethods.SwitchToThisWindow(hwnd, true);
                }
            }
        }

        public void QuitSelectedApp()
        {
            if (!IsVisible || _selectedIndex < 0 || _selectedIndex >= _apps.Count) return;
            OpenWindowEntry target = _apps[_selectedIndex];
            if (target.Hwnd != IntPtr.Zero)
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
        }

        public void HideSelectedApp()
        {
            if (!IsVisible || _selectedIndex < 0 || _selectedIndex >= _apps.Count) return;
            OpenWindowEntry target = _apps[_selectedIndex];
            if (target.Hwnd != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(target.Hwnd, NativeMethods.SW_SHOWMINIMIZED);
            }
            Hide();
        }

        public void Cancel()
        {
            Hide();
        }
    }
}
