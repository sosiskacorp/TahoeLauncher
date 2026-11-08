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
using Microsoft.Win32;

namespace MacOSTahoe
{
    public class WindowSwitcherWindow : Window
    {
        private StackPanel _spacesStack;
        private StackPanel _windowsHost;
        private TextBlock _subTitle;
        private Border _groupByAppBtn;
        private int _currentSpaceIndex = 0;
        private bool _groupByApp = false;

        private OpenWindowEntry _draggedWindow = null;
        private Point _dragStartPoint;
        private bool _isDragging = false;
        private Popup _spaceMenuPopup;

        public WindowSwitcherWindow()
        {
            Title = "Mission Control & Spaces";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
            Background = new SolidColorBrush(Color.FromArgb(50, 14, 18, 28));
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();

            Visibility = Visibility.Collapsed;

            SourceInitialized += delegate
            {
                NativeMethods.EnableBlur(this, 0xCC0E0E16);
            };

            PreviewKeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape || e.Key == Key.F3)
                {
                    e.Handled = true;
                    Hide();
                }
                else if (e.Key == Key.Left)
                {
                    e.Handled = true;
                    if (_currentSpaceIndex > 0)
                    {
                        _currentSpaceIndex--;
                        ShellController.Instance.SwitchToSpace(_currentSpaceIndex);
                        RefreshSpacesBar();
                    }
                }
                else if (e.Key == Key.Right)
                {
                    e.Handled = true;
                    if (ShellController.Instance.Config.Spaces != null && _currentSpaceIndex < ShellController.Instance.Config.Spaces.Count - 1)
                    {
                        _currentSpaceIndex++;
                        ShellController.Instance.SwitchToSpace(_currentSpaceIndex);
                        RefreshSpacesBar();
                    }
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
                Margin = new Thickness(0, 32, 0, 0),
                Width = Math.Min(1380, Width - 50)
            };

            // 1. TOP SPACES BAR
            Border spacesContainer = new Border
            {
                Background = LiquidGlassTheme.CreateGlassBrush(145, 185),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(16, 10, 16, 12),
                Margin = new Thickness(0, 0, 0, 14)
            };

            StackPanel spacesPanel = new StackPanel();

            Grid spacesHeaderGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            TextBlock spacesLabel = new TextBlock
            {
                Text = "ПРОСТРАНСТВА И РАБОЧИЕ СТОЛЫ (SPACES — ДО 16 СТОЛОВ)",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(170, 235, 235, 245)),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            spacesHeaderGrid.Children.Add(spacesLabel);
            spacesPanel.Children.Add(spacesHeaderGrid);

            ScrollViewer spacesScrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            _spacesStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            spacesScrollViewer.Content = _spacesStack;
            spacesPanel.Children.Add(spacesScrollViewer);

            spacesContainer.Child = spacesPanel;
            container.Children.Add(spacesContainer);

            // 2. MISSION CONTROL HEADER & ACTIONS
            Grid header = new Grid { Margin = new Thickness(12, 0, 12, 12) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titles = new StackPanel();
            titles.Children.Add(new TextBlock
            {
                Text = "Mission Control",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            _subTitle = new TextBlock
            {
                Text = "Все открытые окна на экране — нажмите на окно для переключения или перетащите для разделения экрана",
                FontSize = 12.5,
                Foreground = new SolidColorBrush(Color.FromArgb(175, 235, 235, 245)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            titles.Children.Add(_subTitle);
            Grid.SetColumn(titles, 0);
            header.Children.Add(titles);

            StackPanel actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // Group by App Toggle
            _groupByAppBtn = CreateActionPill("◫ По приложениям: Выкл", delegate
            {
                _groupByApp = !_groupByApp;
                if (ShellController.Instance != null) ShellController.Instance.Config.MissionControlGroupByApp = _groupByApp;
                UpdateGroupBtnText();
                PopulateWindows();
            });
            actions.Children.Add(_groupByAppBtn);

            // Split View Button
            Border splitViewBtn = CreateActionPill("◫ Split View", delegate
            {
                List<OpenWindowEntry> wins = ShellController.Instance.GetOpenApplicationWindows();
                if (wins.Count >= 2)
                {
                    Hide();
                    ShellController.Instance.EnterSplitView(wins[0].Hwnd, wins[1].Hwnd, 0.5);
                }
                else
                {
                    MessageBox.Show("Для режима Split View необходимо как минимум два открытых окна.", "Split View", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            });
            splitViewBtn.Margin = new Thickness(8, 0, 0, 0);
            actions.Children.Add(splitViewBtn);

            Border minAllBtn = CreateActionPill("🗕 Свернуть все (F11)", delegate
            {
                List<OpenWindowEntry> list = ShellController.Instance.GetOpenApplicationWindows();
                for (int i = 0; i < list.Count; i++)
                {
                    NativeMethods.ShowWindow(list[i].Hwnd, NativeMethods.SW_SHOWMINIMIZED);
                }
                Hide();
            });
            minAllBtn.Margin = new Thickness(8, 0, 0, 0);
            actions.Children.Add(minAllBtn);

            Border closeBtn = CreateActionPill("✕ Закрыть (F3 / Esc)", delegate { Hide(); });
            closeBtn.Margin = new Thickness(8, 0, 0, 0);
            actions.Children.Add(closeBtn);

            Grid.SetColumn(actions, 1);
            header.Children.Add(actions);
            container.Children.Add(header);

            // 3. WINDOWS GALLERY
            ScrollViewer sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = Math.Max(260, Height - 310)
            };
            _windowsHost = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            sv.Content = _windowsHost;
            container.Children.Add(sv);

            root.Children.Add(container);
            Content = root;
        }

        private void UpdateGroupBtnText()
        {
            if (_groupByAppBtn != null && _groupByAppBtn.Child is TextBlock)
            {
                ((TextBlock)_groupByAppBtn.Child).Text = _groupByApp ? "◫ По приложениям: Вкл" : "◫ По приложениям: Выкл";
                _groupByAppBtn.Background = _groupByApp ? LiquidGlassTheme.AccentBlueBrush : new SolidColorBrush(Color.FromArgb(45, 255, 255, 255));
            }
        }

        private Border CreateActionPill(string text, Action onClick)
        {
            Border b = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(65, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = Cursors.Hand
            };
            b.Child = new TextBlock { Text = text, FontSize = 12, FontWeight = FontWeights.Medium, Foreground = Brushes.White };
            b.MouseEnter += delegate { b.Background = LiquidGlassTheme.AccentBlueBrush; };
            b.MouseLeave += delegate
            {
                if (b == _groupByAppBtn && _groupByApp) b.Background = LiquidGlassTheme.AccentBlueBrush;
                else b.Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255));
            };
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };
            return b;
        }

        public void ShowAndRefresh()
        {
            if (ShellController.Instance != null)
            {
                _currentSpaceIndex = ShellController.Instance.Config.CurrentSpaceIndex;
                _groupByApp = ShellController.Instance.Config.MissionControlGroupByApp;
                UpdateGroupBtnText();
            }
            RefreshSpacesBar();
            PopulateWindows();
            Show();
            Activate();
        }

        private void RefreshSpacesBar()
        {
            _spacesStack.Children.Clear();

            List<string> spaces = ShellController.Instance.Config.Spaces;
            if (spaces == null || spaces.Count == 0)
            {
                spaces = new List<string> { "Рабочий стол 1", "Рабочий стол 2" };
                ShellController.Instance.Config.Spaces = spaces;
            }

            for (int i = 0; i < spaces.Count; i++)
            {
                int index = i;
                string spaceName = spaces[i];
                bool isActive = (index == _currentSpaceIndex);

                Border spaceCard = new Border
                {
                    Width = 146,
                    Height = 84,
                    CornerRadius = new CornerRadius(10),
                    Background = isActive ? LiquidGlassTheme.AccentBlueBrush : new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                    BorderBrush = isActive ? new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
                    BorderThickness = new Thickness(isActive ? 2 : 1),
                    Margin = new Thickness(6, 0, 6, 0),
                    Padding = new Thickness(8),
                    Cursor = Cursors.Hand
                };

                Grid cardGrid = new Grid();
                cardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Mini preview representation
                TextBlock iconDesktop = new TextBlock
                {
                    Text = "🖥",
                    FontSize = 26,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(iconDesktop, 0);
                cardGrid.Children.Add(iconDesktop);

                // Close button for inactive spaces
                if (!isActive && spaces.Count > 1)
                {
                    Border closeSpaceBtn = new Border
                    {
                        Width = 18,
                        Height = 18,
                        CornerRadius = new CornerRadius(9),
                        Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Cursor = Cursors.Hand
                    };
                    closeSpaceBtn.Child = new TextBlock
                    {
                        Text = "✕",
                        FontSize = 10,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    closeSpaceBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                    {
                        e.Handled = true;
                        ShellController.Instance.CloseSpace(index);
                        RefreshSpacesBar();
                    };
                    cardGrid.Children.Add(closeSpaceBtn);
                }

                TextBlock lbl = new TextBlock
                {
                    Text = spaceName,
                    FontSize = 11,
                    FontWeight = isActive ? FontWeights.Bold : FontWeights.Medium,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetRow(lbl, 1);
                cardGrid.Children.Add(lbl);

                spaceCard.Child = cardGrid;

                // Left click switches space
                spaceCard.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    _currentSpaceIndex = index;
                    ShellController.Instance.SwitchToSpace(index);
                    RefreshSpacesBar();
                };

                // Right click opens space wallpaper / options menu
                spaceCard.MouseRightButtonUp += delegate(object s, MouseButtonEventArgs e)
                {
                    ShowSpaceContextMenu(spaceCard, index);
                };

                // Drop target for moving windows between Spaces
                spaceCard.MouseEnter += delegate
                {
                    if (_isDragging && _draggedWindow != null)
                    {
                        spaceCard.BorderBrush = new SolidColorBrush(Color.FromRgb(40, 200, 64));
                    }
                };
                spaceCard.MouseLeave += delegate
                {
                    spaceCard.BorderBrush = isActive ? new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
                };
                spaceCard.MouseLeftButtonUp += delegate
                {
                    if (_isDragging && _draggedWindow != null)
                    {
                        _isDragging = false;
                        ShellController.Instance.MoveWindowToSpace(_draggedWindow.Hwnd, index);
                        _draggedWindow = null;
                        PopulateWindows();
                    }
                };

                _spacesStack.Children.Add(spaceCard);
            }

            // "+" Button to add new Space (limit up to 16)
            if (spaces.Count < 16)
            {
                Border addBtn = new Border
                {
                    Width = 44,
                    Height = 84,
                    CornerRadius = new CornerRadius(10),
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(6, 0, 6, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Добавить рабочий стол (до 16)"
                };
                TextBlock plus = new TextBlock
                {
                    Text = "+",
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                addBtn.Child = plus;
                addBtn.MouseEnter += delegate { addBtn.Background = LiquidGlassTheme.AccentBlueBrush; };
                addBtn.MouseLeave += delegate { addBtn.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)); };
                addBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    ShellController.Instance.CreateNewSpace();
                    _currentSpaceIndex = ShellController.Instance.Config.Spaces.Count - 1;
                    RefreshSpacesBar();
                };
                _spacesStack.Children.Add(addBtn);
            }
        }

        private void ShowSpaceContextMenu(FrameworkElement anchor, int spaceIdx)
        {
            if (_spaceMenuPopup != null && _spaceMenuPopup.IsOpen) _spaceMenuPopup.IsOpen = false;

            Popup popup = new Popup
            {
                AllowsTransparency = true,
                PlacementTarget = anchor,
                Placement = PlacementMode.Bottom,
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
                MinWidth = 210
            };
            card.Effect = new DropShadowEffect { BlurRadius = 18, ShadowDepth = 6, Opacity = 0.55, Color = Colors.Black };

            StackPanel sp = new StackPanel();
            sp.Children.Add(CreateMenuRow("🖼  Сменить обои для этого стола...", delegate
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Title = "Выберите обои для Рабочего стола " + (spaceIdx + 1).ToString(),
                    Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
                };
                if (ofd.ShowDialog() == true)
                {
                    ShellController.Instance.SetSpaceWallpaper(spaceIdx, ofd.FileName);
                }
            }));

            if (ShellController.Instance.Config.Spaces.Count > 1)
            {
                sp.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), Margin = new Thickness(4, 3, 4, 3) });
                sp.Children.Add(CreateMenuRow("✕  Удалить этот рабочий стол", delegate
                {
                    ShellController.Instance.CloseSpace(spaceIdx);
                    RefreshSpacesBar();
                }));
            }

            card.Child = sp;
            popup.Child = card;
            _spaceMenuPopup = popup;
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
            b.MouseLeftButtonDown += delegate
            {
                if (_spaceMenuPopup != null) _spaceMenuPopup.IsOpen = false;
                if (onClick != null) onClick();
            };
            return b;
        }

        private void PopulateWindows()
        {
            _windowsHost.Children.Clear();
            List<OpenWindowEntry> wins = ShellController.Instance.GetOpenApplicationWindows();
            _subTitle.Text = string.Format("Открыто окон: {0} — нажмите для переключения или перетащите окно на другое для Split View", wins.Count);

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
                    Margin = new Thickness(0, 40, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                emptyCard.Child = new TextBlock
                {
                    Text = "Нет открытых окон приложений.\r\nЗапустите программы из Dock или меню «Приложения» (Win).",
                    FontSize = 14,
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center
                };
                _windowsHost.Children.Add(emptyCard);
                return;
            }

            if (_groupByApp)
            {
                // Group by Process / App Title
                Dictionary<string, List<OpenWindowEntry>> groups = new Dictionary<string, List<OpenWindowEntry>>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < wins.Count; i++)
                {
                    OpenWindowEntry w = wins[i];
                    string key = !string.IsNullOrEmpty(w.AppTitle) ? w.AppTitle : w.ProcessName;
                    List<OpenWindowEntry> list;
                    if (!groups.TryGetValue(key, out list))
                    {
                        list = new List<OpenWindowEntry>();
                        groups[key] = list;
                    }
                    list.Add(w);
                }

                foreach (KeyValuePair<string, List<OpenWindowEntry>> pair in groups)
                {
                    StackPanel grpStack = new StackPanel { Margin = new Thickness(0, 8, 0, 14) };

                    // Group Header banner
                    StackPanel grpHeader = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(16, 0, 0, 8) };
                    Image grpIcon = new Image
                    {
                        Width = 20,
                        Height = 20,
                        Source = pair.Value[0].Icon != null ? pair.Value[0].Icon : ShellController.Instance.DefaultAppIcon,
                        Margin = new Thickness(0, 0, 8, 0)
                    };
                    RenderOptions.SetBitmapScalingMode(grpIcon, BitmapScalingMode.HighQuality);
                    grpHeader.Children.Add(grpIcon);
                    grpHeader.Children.Add(new TextBlock
                    {
                        Text = string.Format("{0} ({1})", pair.Key, pair.Value.Count),
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    });
                    grpStack.Children.Add(grpHeader);

                    WrapPanel grpWrap = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };
                    for (int j = 0; j < pair.Value.Count; j++)
                    {
                        grpWrap.Children.Add(CreateWindowCard(pair.Value[j]));
                    }
                    grpStack.Children.Add(grpWrap);
                    _windowsHost.Children.Add(grpStack);
                }
            }
            else
            {
                WrapPanel wrap = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };
                for (int i = 0; i < wins.Count; i++)
                {
                    wrap.Children.Add(CreateWindowCard(wins[i]));
                }
                _windowsHost.Children.Add(wrap);
            }
        }

        private Border CreateWindowCard(OpenWindowEntry win)
        {
            Border card = new Border
            {
                Width = 260,
                Height = 168,
                Margin = new Thickness(10),
                CornerRadius = new CornerRadius(16),
                Background = LiquidGlassTheme.CreateGlassBrush(180, 210),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Padding = new Thickness(12)
            };
            card.Effect = new DropShadowEffect
            {
                BlurRadius = 24,
                ShadowDepth = 6,
                Opacity = 0.5,
                Color = Colors.Black
            };

            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Card Header
            Grid cardHeader = new Grid();
            cardHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            cardHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cardHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });

            Image iconImg = new Image
            {
                Width = 22,
                Height = 22,
                Source = win.Icon != null ? win.Icon : ShellController.Instance.DefaultAppIcon,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            RenderOptions.SetBitmapScalingMode(iconImg, BitmapScalingMode.HighQuality);
            Grid.SetColumn(iconImg, 0);
            cardHeader.Children.Add(iconImg);

            TextBlock title = new TextBlock
            {
                Text = win.AppTitle,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 6, 0)
            };
            Grid.SetColumn(title, 1);
            cardHeader.Children.Add(title);

            Border closeBtn = new Border
            {
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            closeBtn.Child = new TextBlock
            {
                Text = "✕",
                FontSize = 10,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            closeBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                NativeMethods.PostMessage(win.Hwnd, 0x0010 /* WM_CLOSE */, IntPtr.Zero, IntPtr.Zero);
                PopulateWindows();
            };
            Grid.SetColumn(closeBtn, 2);
            cardHeader.Children.Add(closeBtn);

            Grid.SetRow(cardHeader, 0);
            grid.Children.Add(cardHeader);

            // Card Body (Window representation)
            Border body = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                Margin = new Thickness(0, 8, 0, 0),
                Padding = new Thickness(10)
            };
            StackPanel bodyContent = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            bodyContent.Children.Add(new TextBlock
            {
                Text = win.WindowTitle,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextAlignment = TextAlignment.Center
            });
            bodyContent.Children.Add(new TextBlock
            {
                Text = string.Format("Процесс: {0}", win.ProcessName),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            });
            body.Child = bodyContent;

            Grid.SetRow(body, 1);
            grid.Children.Add(body);

            card.Child = grid;

            // Hover styling
            card.MouseEnter += delegate
            {
                if (_isDragging && _draggedWindow != null && _draggedWindow != win)
                {
                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(40, 200, 64)); // Green drop indicator for Split View
                }
                else
                {
                    card.BorderBrush = LiquidGlassTheme.AccentBlueBrush;
                    card.Background = LiquidGlassTheme.CreateGlassBrush(200, 230);
                }
            };
            card.MouseLeave += delegate
            {
                card.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush();
                card.Background = LiquidGlassTheme.CreateGlassBrush(180, 210);
            };

            // Drag detection: drag onto another card for Split View, or onto a Space card
            card.PreviewMouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                _dragStartPoint = e.GetPosition(this);
                _draggedWindow = win;
                _isDragging = false;
            };

            card.PreviewMouseMove += delegate(object s, MouseEventArgs e)
            {
                if (e.LeftButton == MouseButtonState.Pressed && _draggedWindow == win)
                {
                    Point cur = e.GetPosition(this);
                    if (Math.Abs(cur.X - _dragStartPoint.X) > 10 || Math.Abs(cur.Y - _dragStartPoint.Y) > 10)
                    {
                        _isDragging = true;
                    }
                }
            };

            card.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                if (_isDragging && _draggedWindow != null && _draggedWindow != win)
                {
                    // Dropped onto another window card -> Launch Split View!
                    e.Handled = true;
                    IntPtr left = _draggedWindow.Hwnd;
                    IntPtr right = win.Hwnd;
                    _isDragging = false;
                    _draggedWindow = null;
                    Hide();
                    ShellController.Instance.EnterSplitView(left, right, 0.5);
                    return;
                }

                _isDragging = false;
                _draggedWindow = null;
            };

            // Click to activate window
            card.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                Hide();
                if (win.Hwnd != IntPtr.Zero)
                {
                    if (NativeMethods.IsIconic(win.Hwnd))
                    {
                        NativeMethods.ShowWindow(win.Hwnd, NativeMethods.SW_RESTORE);
                    }
                    NativeMethods.SetForegroundWindow(win.Hwnd);
                    NativeMethods.SwitchToThisWindow(win.Hwnd, false);
                }
            };

            return card;
        }
    }
}