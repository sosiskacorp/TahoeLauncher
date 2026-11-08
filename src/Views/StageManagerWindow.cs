using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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

namespace MacOSTahoe
{
    public class StageManagerWindow : Window
    {
        private StackPanel _shelfStack;
        private Border _glassCard;
        private Border _desktopBtn;
        private TextBlock _desktopBtnText;
        private DispatcherTimer _pollTimer;
        private bool _isHovered = false;
        private bool _isCardDragging = false;
        private Point _dragStartPoint;
        private StageGroup _draggedGroup;

        private const double NORMAL_WIDTH = 144.0;
        private const double COLLAPSED_WIDTH = 10.0;

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

            UpdateDimensions();
            BuildUI();

            Visibility = Visibility.Collapsed;

            SourceInitialized += delegate
            {
                NativeMethods.MakeToolWindow(this, true);
                NativeMethods.EnableBlur(this, 0xCC12121A);
            };

            MouseEnter += delegate
            {
                _isHovered = true;
                if (ShellController.Instance != null && ShellController.Instance.Config.StageManagerAutoHide)
                {
                    Width = NORMAL_WIDTH;
                    _glassCard.Opacity = 1.0;
                }
            };

            MouseLeave += delegate
            {
                _isHovered = false;
                if (ShellController.Instance != null && ShellController.Instance.Config.StageManagerAutoHide && !_isCardDragging)
                {
                    Width = COLLAPSED_WIDTH;
                    _glassCard.Opacity = 0.35;
                }
            };

            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
            _pollTimer.Tick += OnPollTick;
            _pollTimer.Start();
        }

        public void UpdateDimensions()
        {
            double topH = (ShellController.Instance != null && ShellController.Instance.Config.ShowTopBar) ? 34.0 : 8.0;
            double dockH = (ShellController.Instance != null && ShellController.Instance.Config.ShowDock && !ShellController.Instance.Config.DockAutoHide) ? 78.0 : 12.0;

            Left = 6;
            Top = topH;
            Height = Math.Max(260, SystemParameters.PrimaryScreenHeight - topH - dockH);
            Width = (ShellController.Instance != null && ShellController.Instance.Config.StageManagerAutoHide) ? COLLAPSED_WIDTH : NORMAL_WIDTH;
        }

        public void ApplyAutoHideSetting()
        {
            UpdateDimensions();
            if (ShellController.Instance != null && ShellController.Instance.Config.StageManagerAutoHide)
            {
                Width = _isHovered ? NORMAL_WIDTH : COLLAPSED_WIDTH;
                if (_glassCard != null) _glassCard.Opacity = _isHovered ? 1.0 : 0.35;
            }
            else
            {
                Width = NORMAL_WIDTH;
                if (_glassCard != null) _glassCard.Opacity = 1.0;
            }
        }

        private void BuildUI()
        {
            _glassCard = new Border
            {
                CornerRadius = new CornerRadius(18),
                Background = LiquidGlassTheme.CreateGlassBrush(175, 215),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 10, 6, 10),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _glassCard.Effect = new DropShadowEffect
            {
                BlurRadius = 26,
                ShadowDepth = 6,
                Opacity = 0.55,
                Color = Colors.Black
            };

            Grid rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 1. Header: Pill Label
            Border headerBadge = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Padding = new Thickness(6, 3, 6, 3),
                Margin = new Thickness(4, 0, 4, 8),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            TextBlock headerTxt = new TextBlock
            {
                Text = "ПОСТАНОВЩИК",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(190, 240, 240, 250)),
                            };
            headerBadge.Child = headerTxt;
            Grid.SetRow(headerBadge, 0);
            rootGrid.Children.Add(headerBadge);

            // 2. Middle: Scrollable Stack of Stage Groups
            ScrollViewer sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalAlignment = VerticalAlignment.Center
            };
            _shelfStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            sv.Content = _shelfStack;
            Grid.SetRow(sv, 1);
            rootGrid.Children.Add(sv);

            // 3. Footer: Desktop Access Button
            _desktopBtn = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(4, 8, 4, 0),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _desktopBtnText = new TextBlock
            {
                Text = "🖥 Рабочий стол",
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _desktopBtn.Child = _desktopBtnText;
            _desktopBtn.MouseEnter += delegate { _desktopBtn.Background = LiquidGlassTheme.AccentBlueBrush; };
            _desktopBtn.MouseLeave += delegate { UpdateDesktopBtnState(); };
            _desktopBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                ShellController.Instance.HideActiveStageToDesktop();
                UpdateDesktopBtnState();
            };
            Grid.SetRow(_desktopBtn, 2);
            rootGrid.Children.Add(_desktopBtn);

            _glassCard.Child = rootGrid;
            Content = _glassCard;
        }

        private void UpdateDesktopBtnState()
        {
            if (_desktopBtn == null) return;
            bool revealed = ShellController.Instance != null && ShellController.Instance.IsStageDesktopRevealed;
            if (revealed)
            {
                _desktopBtn.Background = LiquidGlassTheme.AccentBlueBrush;
                _desktopBtnText.Text = "↩ Вернуть сцену";
            }
            else
            {
                _desktopBtn.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
                _desktopBtnText.Text = "🖥 Рабочий стол";
            }
        }

        private void OnPollTick(object sender, EventArgs e)
        {
            if (ShellController.Instance == null || !ShellController.Instance.Config.EnableStageManager)
            {
                if (IsVisible) Hide();
                return;
            }

            // Auto-hide when active foreground window is Maximized / Fullscreen
            IntPtr fg = NativeMethods.GetForegroundWindow();
            if (fg != IntPtr.Zero && NativeMethods.IsWindow(fg))
            {
                bool isMaximized = NativeMethods.IsZoomed(fg);
                if (isMaximized)
                {
                    if (Visibility != Visibility.Collapsed) Visibility = Visibility.Collapsed;
                    return;
                }
                else
                {
                    if (Visibility != Visibility.Visible) Visibility = Visibility.Visible;
                }
            }

            // Mouse proximity trigger for AutoHide
            if (ShellController.Instance.Config.StageManagerAutoHide)
            {
                NativeMethods.POINT pt;
                if (NativeMethods.GetCursorPos(out pt))
                {
                    if (pt.X <= 14 && !_isHovered)
                    {
                        Width = NORMAL_WIDTH;
                        _glassCard.Opacity = 1.0;
                    }
                    else if (pt.X > 160 && !_isHovered && Width > COLLAPSED_WIDTH)
                    {
                        Width = COLLAPSED_WIDTH;
                        _glassCard.Opacity = 0.35;
                    }
                }
            }

            RefreshShelf();
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
                UpdateDimensions();
                Show();
            }

            List<StageGroup> allGroups = ShellController.Instance.RefreshStageGroups();
            _shelfStack.Children.Clear();
            UpdateDesktopBtnState();

            StageGroup activeGroup = ShellController.Instance.ActiveStageGroup;

            int renderedCount = 0;
            for (int i = 0; i < allGroups.Count; i++)
            {
                StageGroup g = allGroups[i];
                // In macOS Stage Manager, the active group is in the center, so only background groups are shown on shelf
                if (g == activeGroup && !ShellController.Instance.IsStageDesktopRevealed) continue;

                Border card = CreateStageCard(g);
                _shelfStack.Children.Add(card);
                renderedCount++;
                if (renderedCount >= 6) break; // Maximum 6 stacks on shelf
            }

            if (renderedCount == 0)
            {
                Border emptyCard = new Border
                {
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                    Padding = new Thickness(8, 16, 8, 16),
                    Margin = new Thickness(4),
                    Width = 126
                };
                TextBlock empTxt = new TextBlock
                {
                    Text = "Нет фоновых окон",
                    FontSize = 10.5,
                    Foreground = new SolidColorBrush(Color.FromArgb(160, 235, 235, 245)),
                    TextAlignment = TextAlignment.Center
                };
                emptyCard.Child = empTxt;
                _shelfStack.Children.Add(emptyCard);
            }
        }

        private Border CreateStageCard(StageGroup group)
        {
            Grid containerGrid = new Grid
            {
                Width = 128,
                Height = 84,
                Margin = new Thickness(0, 6, 0, 6),
                Cursor = Cursors.Hand
            };

            int windowCount = group.Windows.Count;

            // 1. If 3D Stack (multiple windows): Draw background offset deck cards
            if (windowCount > 2)
            {
                Border backCard2 = new Border
                {
                    Width = 118,
                    Height = 74,
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush(Color.FromArgb(60, 20, 22, 34)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 8),
                    Opacity = 0.5
                };
                containerGrid.Children.Add(backCard2);
            }
            if (windowCount > 1)
            {
                Border backCard1 = new Border
                {
                    Width = 122,
                    Height = 78,
                    CornerRadius = new CornerRadius(13),
                    Background = new SolidColorBrush(Color.FromArgb(90, 25, 28, 42)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(55, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 0, 4),
                    Opacity = 0.75
                };
                containerGrid.Children.Add(backCard1);
            }

            // 2. Front Main Card
            Border frontCard = new Border
            {
                Width = 126,
                Height = 82,
                CornerRadius = new CornerRadius(14),
                Background = LiquidGlassTheme.CreateGlassBrush(185, 225),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(7),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            frontCard.Effect = new DropShadowEffect
            {
                BlurRadius = 14,
                ShadowDepth = 4,
                Opacity = 0.5,
                Color = Colors.Black
            };

            Grid cardLayout = new Grid();
            cardLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });
            cardLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            cardLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Window Header (Traffic Lights + Stack Badge)
            Grid topRow = new Grid();
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Traffic lights
            StackPanel trafficLights = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            trafficLights.Children.Add(new Ellipse { Width = 5, Height = 5, Fill = new SolidColorBrush(Color.FromRgb(255, 95, 87)), Margin = new Thickness(0, 0, 3, 0) });
            trafficLights.Children.Add(new Ellipse { Width = 5, Height = 5, Fill = new SolidColorBrush(Color.FromRgb(254, 188, 46)), Margin = new Thickness(0, 0, 3, 0) });
            trafficLights.Children.Add(new Ellipse { Width = 5, Height = 5, Fill = new SolidColorBrush(Color.FromRgb(40, 200, 64)) });
            Grid.SetColumn(trafficLights, 0);
            topRow.Children.Add(trafficLights);

            // Stack Count Badge
            if (windowCount > 1)
            {
                Border badge = new Border
                {
                    CornerRadius = new CornerRadius(5),
                    Background = new SolidColorBrush(Color.FromArgb(140, 10, 132, 255)),
                    Padding = new Thickness(4, 0.5, 4, 0.5),
                    VerticalAlignment = VerticalAlignment.Center
                };
                badge.Child = new TextBlock
                {
                    Text = windowCount.ToString(),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                };
                Grid.SetColumn(badge, 2);
                topRow.Children.Add(badge);
            }
            Grid.SetRow(topRow, 0);
            cardLayout.Children.Add(topRow);

            // Center: App Icon
            Image iconImg = new Image
            {
                Width = 32,
                Height = 32,
                Source = group.Icon != null ? group.Icon : ShellController.Instance.DefaultAppIcon,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(iconImg, BitmapScalingMode.HighQuality);
            Grid.SetRow(iconImg, 1);
            cardLayout.Children.Add(iconImg);

            // Bottom: Title
            TextBlock titleTxt = new TextBlock
            {
                Text = group.Title,
                FontSize = 10.5,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };
            Grid.SetRow(titleTxt, 2);
            cardLayout.Children.Add(titleTxt);

            // Hover Action Overlay
            Border hoverOverlay = new Border
            {
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromArgb(210, 14, 16, 24)),
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            StackPanel overlayStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // "+ В сцену" Button
            Border addBtn = CreateCardActionPill("＋ В сцену", delegate
            {
                ShellController.Instance.AddGroupToActiveStage(group);
            });
            overlayStack.Children.Add(addBtn);

            // Split View Button
            Border splitBtn = CreateCardActionPill("◫ Split View", delegate
            {
                if (group.Windows.Count > 0 && ShellController.Instance.ActiveStageGroup != null && ShellController.Instance.ActiveStageGroup.Windows.Count > 0)
                {
                    ShellController.Instance.EnterSplitView(ShellController.Instance.ActiveStageGroup.Windows[0].Hwnd, group.Windows[0].Hwnd, 0.5);
                }
            });
            splitBtn.Margin = new Thickness(0, 3, 0, 3);
            overlayStack.Children.Add(splitBtn);

            // Close Button
            Border closeBtn = CreateCardActionPill("✕ Закрыть", delegate
            {
                for (int i = 0; i < group.Windows.Count; i++)
                {
                    NativeMethods.PostMessage(group.Windows[i].Hwnd, 0x0010 /* WM_CLOSE */, IntPtr.Zero, IntPtr.Zero);
                }
                ShellController.Instance.CurrentStageGroups.Remove(group);
                RefreshShelf();
            });
            overlayStack.Children.Add(closeBtn);

            hoverOverlay.Child = overlayStack;

            Grid frontGrid = new Grid();
            frontGrid.Children.Add(cardLayout);
            frontGrid.Children.Add(hoverOverlay);
            frontCard.Child = frontGrid;

            containerGrid.Children.Add(frontCard);

            // Outer wrapper border
            Border wrapper = new Border
            {
                Child = containerGrid,
                Margin = new Thickness(0, 3, 0, 3)
            };

            // Mouse hover handling
            wrapper.MouseEnter += delegate
            {
                frontCard.BorderBrush = LiquidGlassTheme.AccentBlueBrush;
                hoverOverlay.Visibility = Visibility.Visible;
            };
            wrapper.MouseLeave += delegate
            {
                frontCard.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush();
                hoverOverlay.Visibility = Visibility.Collapsed;
            };

            // Drag and Drop: dragging card to center adds it to current stage
            wrapper.PreviewMouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                _dragStartPoint = e.GetPosition(this);
                _draggedGroup = group;
            };

            wrapper.PreviewMouseMove += delegate(object s, MouseEventArgs e)
            {
                if (e.LeftButton == MouseButtonState.Pressed && _draggedGroup == group)
                {
                    Point cur = e.GetPosition(this);
                    if (Math.Abs(cur.X - _dragStartPoint.X) > 12 || Math.Abs(cur.Y - _dragStartPoint.Y) > 12)
                    {
                        _isCardDragging = true;
                        NativeMethods.POINT screenPt;
                        if (NativeMethods.GetCursorPos(out screenPt))
                        {
                            if (screenPt.X > 165)
                            {
                                // Dragged into desktop area! Pull into active stage!
                                _isCardDragging = false;
                                _draggedGroup = null;
                                ShellController.Instance.AddGroupToActiveStage(group);
                            }
                        }
                    }
                }
            };

            wrapper.PreviewMouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                if (_isCardDragging && _draggedGroup == group)
                {
                    NativeMethods.POINT screenPt;
                    if (NativeMethods.GetCursorPos(out screenPt))
                    {
                        if (screenPt.X > 150)
                        {
                            ShellController.Instance.AddGroupToActiveStage(group);
                        }
                    }
                }
                _isCardDragging = false;
                _draggedGroup = null;
            };

            // Click activates stage
            wrapper.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                ShellController.Instance.SwitchToStageGroup(group);
            };

            return wrapper;
        }

        private Border CreateCardActionPill(string text, Action onClick)
        {
            Border b = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromArgb(120, 20, 24, 38)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 2, 6, 2),
                Cursor = Cursors.Hand
            };
            TextBlock tb = new TextBlock
            {
                Text = text,
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            b.Child = tb;
            b.MouseEnter += delegate { b.Background = LiquidGlassTheme.AccentBlueBrush; };
            b.MouseLeave += delegate { b.Background = new SolidColorBrush(Color.FromArgb(120, 20, 24, 38)); };
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };
            return b;
        }
    }
}