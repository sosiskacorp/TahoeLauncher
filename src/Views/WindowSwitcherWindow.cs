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
        private StackPanel _spacesStack;
        private WrapPanel _windowsWrap;
        private TextBlock _subTitle;
        private int _currentSpaceIndex = 0;

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
            Background = new SolidColorBrush(Color.FromArgb(208, 10, 12, 18));
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
                Margin = new Thickness(0, 36, 0, 0),
                Width = Math.Min(1360, Width - 60)
            };

            // =========================================================================
            // 1. TOP SPACES BAR (Полоса виртуальных рабочих столов — Spaces)
            // =========================================================================
            Border spacesContainer = new Border
            {
                Background = LiquidGlassTheme.CreateGlassBrush(140, 180),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(16, 10, 16, 12),
                Margin = new Thickness(0, 0, 0, 16)
            };

            StackPanel spacesPanel = new StackPanel();

            // Spaces Header
            Grid spacesHeaderGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            TextBlock spacesLabel = new TextBlock
            {
                Text = "ПРОСТРАНСТВА И РАБОЧИЕ СТОЛЫ (SPACES)",
                FontSize = 11,
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

            // =========================================================================
            // 2. MISSION CONTROL HEADER & ACTIONS
            // =========================================================================
            Grid header = new Grid { Margin = new Thickness(12, 0, 12, 14) };
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
                Text = "Все открытые окна на экране — нажмите на окно для переключения",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromArgb(175, 235, 235, 245)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            titles.Children.Add(_subTitle);
            Grid.SetColumn(titles, 0);
            header.Children.Add(titles);

            StackPanel actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // Split View Button
            Border splitViewBtn = CreateActionPill("◫ Разделение экрана (Split View)", delegate
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

            // =========================================================================
            // 3. WINDOWS GALLERY WRAP
            // =========================================================================
            ScrollViewer sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = Math.Max(260, Height - 310)
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
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = Cursors.Hand
            };
            b.Child = new TextBlock { Text = text, FontSize = 12, FontWeight = FontWeights.Medium, Foreground = Brushes.White };
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

                spaceCard.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    _currentSpaceIndex = index;
                    ShellController.Instance.SwitchToSpace(index);
                    RefreshSpacesBar();
                };

                _spacesStack.Children.Add(spaceCard);
            }

            // "+" Button to add new Space
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
                ToolTip = "Добавить рабочий стол"
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

        private void PopulateWindows()
        {
            _windowsWrap.Children.Clear();
            List<OpenWindowEntry> wins = ShellController.Instance.GetOpenApplicationWindows();
            _subTitle.Text = string.Format("Открыто окон: {0} — нажмите на карточку для переключения или перетащите для разделения экрана", wins.Count);

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
                    Margin = new Thickness(0, 40, 0, 0)
                };
                emptyCard.Child = new TextBlock
                {
                    Text = "Нет открытых окон приложений.\r\nЗапустите программы из Dock или меню «Приложения» (Win).",
                    FontSize = 14,
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center
                };
                _windowsWrap.Children.Add(emptyCard);
                return;
            }

            for (int i = 0; i < wins.Count; i++)
            {
                OpenWindowEntry win = wins[i];
                Border card = CreateWindowCard(win);
                _windowsWrap.Children.Add(card);
            }
        }

        private Border CreateWindowCard(OpenWindowEntry win)
        {
            Border card = new Border
            {
                Width = 270,
                Height = 175,
                Margin = new Thickness(12),
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

            // Card Header: Icon + Title + Close Button
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
                _windowsWrap.Children.Remove(card);
            };
            Grid.SetColumn(closeBtn, 2);
            cardHeader.Children.Add(closeBtn);

            Grid.SetRow(cardHeader, 0);
            grid.Children.Add(cardHeader);

            // Card Body (Window thumbnail placeholder representation)
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
                card.BorderBrush = LiquidGlassTheme.AccentBlueBrush;
                card.Background = LiquidGlassTheme.CreateGlassBrush(200, 230);
            };
            card.MouseLeave += delegate
            {
                card.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush();
                card.Background = LiquidGlassTheme.CreateGlassBrush(180, 210);
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
                    NativeMethods.SwitchToThisWindow(win.Hwnd, true);
                }
            };

            return card;
        }
    }
}
