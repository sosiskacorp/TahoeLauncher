using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace MacOSTahoe
{
    public class ControlCenterWindow : Window
    {
        private Border _darkModeTile;
        private TextBlock _darkModeSub;
        private Border _iconsTile;
        private TextBlock _iconsSub;
        private Border _widgetsTile;
        private TextBlock _widgetsSub;
        private Border _dockHideTile;
        private TextBlock _dockHideSub;
        private Border _stageManagerTile;
        private TextBlock _stageManagerSub;
        private TranslateTransform _translateTransform;
        private Border _card;
        private bool _isClosing = false;

        // Connectivity Subtitles & Dots
        private TextBlock _connWifiSub;
        private Border _connWifiDot;
        private TextBlock _connBtSub;
        private Border _connBtDot;

        // Miniplayer Elements
        private TextBlock _ccMediaSource;
        private TextBlock _ccMediaStatus;
        private Image _ccCoverImage;
        private TextBlock _ccCoverIcon;
        private TextBlock _ccTrackTitle;
        private TextBlock _ccTrackArtist;
        private TextBlock _ccTimeElapsed;
        private TextBlock _ccTimeTotal;
        private ProgressBar _ccProgressBar;
        private Border _ccPlayPauseChip;
        private TextBlock _ccPlayPauseText;

        public ControlCenterWindow()
        {
            Title = "Пункт управления";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Width = 350;
            SizeToContent = SizeToContent.Height;
            MaxHeight = SystemParameters.WorkArea.Height - 35;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();

            Deactivated += delegate(object s, EventArgs e)
            {
                if (!_isClosing && IsVisible) HideAnimated();
            };

            NowPlayingManager.Instance.MediaUpdated += delegate
            {
                if (IsVisible) Dispatcher.BeginInvoke(new Action(UpdateMiniplayerUI));
            };

            NetworkHelper.WifiStateChanged += delegate
            {
                if (IsVisible) Dispatcher.BeginInvoke(new Action(RefreshState));
            };

            BluetoothHelper.StateChanged += delegate
            {
                if (IsVisible) Dispatcher.BeginInvoke(new Action(RefreshState));
            };
        }

        private void BuildUI()
        {
            Border card = new Border();
            _card = card;
            _translateTransform = new TranslateTransform(0, 0);
            _card.RenderTransform = _translateTransform;
            card.Margin = new Thickness(12);
            card.CornerRadius = new CornerRadius(22);
            card.Background = LiquidGlassTheme.CreateGlassBrush(226, 240);
            card.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush();
            card.BorderThickness = new Thickness(1);
            card.Padding = new Thickness(12);
            card.Effect = new DropShadowEffect
            {
                BlurRadius = 28,
                ShadowDepth = 8,
                Opacity = 0.6,
                Color = Colors.Black
            };

            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Focusable = false,
                Background = Brushes.Transparent,
                PanningMode = PanningMode.VerticalOnly
            };

            StackPanel root = new StackPanel();

            // 1. TOP ROW: Connectivity + Actions (Dark mode, Desktop icons)
            Grid topGrid = new Grid();
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Connectivity Card
            Border connCard = CreateGlassTile();
            connCard.Padding = new Thickness(10, 8, 10, 8);
            StackPanel connStack = new StackPanel();

            // Wi-Fi / Network Row
            Border wifiRow = CreateConnRow("Сеть", "Проверка...", true, "📶", out _connWifiSub, out _connWifiDot, delegate()
            {
                ShellController.Instance.ToggleNetworkMenu();
            });
            connStack.Children.Add(wifiRow);

            // Bluetooth Row
            Border btRow = CreateConnRow("Bluetooth", "Вкл.", true, "ᛒ", out _connBtSub, out _connBtDot, delegate()
            {
                BluetoothHelper.ToggleBluetooth();
                RefreshState();
            });
            btRow.MouseRightButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                ShellController.RunProcess("explorer.exe", "ms-settings:bluetooth");
            };
            connStack.Children.Add(btRow);

            // Launcher settings
            TextBlock dummySub;
            Border dummyDot;
            Border launcherRow = CreateConnRow("Лаунчер", "Параметры", true, "⚙", out dummySub, out dummyDot, delegate()
            {
                Hide();
                ShellController.Instance.ShowLauncherSettings();
            });
            connStack.Children.Add(launcherRow);

            connCard.Child = connStack;
            Grid.SetColumn(connCard, 0);
            topGrid.Children.Add(connCard);

            // Right Column: Dark mode & Desktop icons
            StackPanel rightCol = new StackPanel();
            _darkModeTile = CreateActionTile("Оформление", "Тёмная тема", true, out _darkModeSub, delegate()
            {
                ShellController.Instance.ToggleDarkMode();
                RefreshState();
            });
            _darkModeTile.Margin = new Thickness(0, 0, 0, 8);
            rightCol.Children.Add(_darkModeTile);

            _iconsTile = CreateActionTile("Значки стола", "Показаны", true, out _iconsSub, delegate()
            {
                NativeMethods.ToggleDesktopIcons();
                RefreshState();
            });
            rightCol.Children.Add(_iconsTile);

            Grid.SetColumn(rightCol, 2);
            topGrid.Children.Add(rightCol);
            root.Children.Add(topGrid);

            // 2. TOGGLE TILES
            _widgetsTile = CreateActionTile("Виджеты рабочего стола", "Включены", true, out _widgetsSub, delegate()
            {
                ShellController.Instance.Config.ShowDesktopWidgets = !ShellController.Instance.Config.ShowDesktopWidgets;
                ShellController.Instance.SaveConfig();
                ShellController.Instance.ApplyModuleVisibility();
                RefreshState();
            });
            _widgetsTile.Margin = new Thickness(0, 8, 0, 0);
            root.Children.Add(_widgetsTile);

            _dockHideTile = CreateActionTile("Режим Dock", "Скрывать в полноэкранных окнах", true, out _dockHideSub, delegate()
            {
                ShellController.Instance.DockAutoHide = !ShellController.Instance.DockAutoHide;
                ShellController.Instance.SaveConfig();
                RefreshState();
            });
            _dockHideTile.Margin = new Thickness(0, 8, 0, 0);
            root.Children.Add(_dockHideTile);

            _stageManagerTile = CreateActionTile("Постановщик (Stage Manager)", "Выключен", true, out _stageManagerSub, delegate()
            {
                ShellController.Instance.ToggleStageManager();
                RefreshState();
            });
            _stageManagerTile.Margin = new Thickness(0, 8, 0, 0);
            root.Children.Add(_stageManagerTile);

            TextBlock bgSub;
            Border bgTasksTile = CreateActionTile("Фоновые задачи", "Трей и процессы (Win+B)", true, out bgSub, delegate()
            {
                Hide();
                ShellController.Instance.ToggleBackgroundTasks();
            });
            bgTasksTile.Margin = new Thickness(0, 8, 0, 0);
            root.Children.Add(bgTasksTile);

            // 3. BRIGHTNESS SLIDER
            Border dispCard = CreateGlassTile();
            dispCard.Margin = new Thickness(0, 8, 0, 0);
            dispCard.Padding = new Thickness(12, 8, 12, 8);
            StackPanel dispStack = new StackPanel();
            dispStack.Children.Add(new TextBlock
            {
                Text = "Яркость экрана",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            });
            Slider brightSlider = new Slider
            {
                Minimum = 25,
                Maximum = 100,
                Value = 100,
                Cursor = Cursors.Hand
            };
            brightSlider.ValueChanged += delegate(object s, RoutedPropertyChangedEventArgs<double> e)
            {
                if (ShellController.Instance != null)
                    ShellController.Instance.SetBrightnessOverlay(e.NewValue);
            };
            dispStack.Children.Add(brightSlider);
            dispCard.Child = dispStack;
            root.Children.Add(dispCard);

            // 4. NOW PLAYING MINIPLAYER CARD
            Border mediaCard = CreateGlassTile();
            mediaCard.Margin = new Thickness(0, 8, 0, 0);
            mediaCard.Padding = new Thickness(12, 10, 12, 10);

            StackPanel mediaStack = new StackPanel();

            // Header: source & status
            Grid mediaHeader = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            mediaHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mediaHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _ccMediaSource = new TextBlock
            {
                Text = "🎵 Яндекс Музыка",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = LiquidGlassTheme.AccentBlueBrush
            };
            Grid.SetColumn(_ccMediaSource, 0);
            mediaHeader.Children.Add(_ccMediaSource);

            _ccMediaStatus = new TextBlock
            {
                Text = "Сейчас играет",
                FontSize = 9.5,
                Foreground = new SolidColorBrush(Color.FromArgb(160, 235, 235, 245))
            };
            Grid.SetColumn(_ccMediaStatus, 1);
            mediaHeader.Children.Add(_ccMediaStatus);
            mediaStack.Children.Add(mediaHeader);

            // Track & Artist with Cover art
            Grid trackRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            trackRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
            trackRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border coverBorder = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)),
                ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid coverGrid = new Grid();
            _ccCoverIcon = new TextBlock
            {
                Text = "🎵",
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _ccCoverImage = new Image
            {
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            coverGrid.Children.Add(_ccCoverIcon);
            coverGrid.Children.Add(_ccCoverImage);
            coverBorder.Child = coverGrid;
            Grid.SetColumn(coverBorder, 0);
            trackRow.Children.Add(coverBorder);

            StackPanel trackInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };
            _ccTrackTitle = new TextBlock
            {
                Text = "Музыка не играет",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            _ccTrackArtist = new TextBlock
            {
                Text = "Запустите Яндекс Музыку",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 235, 235, 245)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 1, 0, 0)
            };
            trackInfo.Children.Add(_ccTrackTitle);
            trackInfo.Children.Add(_ccTrackArtist);
            Grid.SetColumn(trackInfo, 1);
            trackRow.Children.Add(trackInfo);
            mediaStack.Children.Add(trackRow);

            // Timeline Scrubber
            Grid timelineGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            timelineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            timelineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            timelineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _ccTimeElapsed = new TextBlock { Text = "00:00", FontSize = 9.5, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(160, 235, 235, 245)) };
            Grid.SetColumn(_ccTimeElapsed, 0);
            timelineGrid.Children.Add(_ccTimeElapsed);

            _ccProgressBar = new ProgressBar
            {
                Height = 4,
                Minimum = 0,
                Maximum = 180,
                Value = 0,
                Margin = new Thickness(6, 0, 6, 0),
                Foreground = LiquidGlassTheme.AccentBlueBrush,
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_ccProgressBar, 1);
            timelineGrid.Children.Add(_ccProgressBar);

            _ccTimeTotal = new TextBlock { Text = "03:00", FontSize = 9.5, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(160, 235, 235, 245)) };
            Grid.SetColumn(_ccTimeTotal, 2);
            timelineGrid.Children.Add(_ccTimeTotal);
            mediaStack.Children.Add(timelineGrid);

            // Playback controls row
            StackPanel controls = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            controls.Children.Add(CreateMediaChip("⏮", delegate() { NowPlayingManager.Instance.PreviousTrack(); }));
            _ccPlayPauseChip = CreateMediaChip("▶ Пуск", delegate() { NowPlayingManager.Instance.PlayPause(); });
            _ccPlayPauseText = _ccPlayPauseChip.Child as TextBlock;
            controls.Children.Add(_ccPlayPauseChip);
            controls.Children.Add(CreateMediaChip("⏭", delegate() { NowPlayingManager.Instance.NextTrack(); }));
            mediaStack.Children.Add(controls);

            mediaCard.Child = mediaStack;
            root.Children.Add(mediaCard);

            scroll.Content = root;
            card.Child = scroll;
            Content = card;
        }

        private Border CreateGlassTile()
        {
            Border b = new Border();
            b.CornerRadius = new CornerRadius(14);
            b.Background = new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));
            b.BorderBrush = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255));
            b.BorderThickness = new Thickness(1);
            return b;
        }

        private Border CreateConnRow(string title, string sub, bool active, string iconChar, out TextBlock subBlock, out Border dotBlock, Action onClick)
        {
            Border row = new Border();
            row.CornerRadius = new CornerRadius(9);
            row.Padding = new Thickness(4, 5, 4, 5);
            row.Background = Brushes.Transparent;
            row.Cursor = Cursors.Hand;

            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            Border dot = new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(13),
                Background = active ? LiquidGlassTheme.AccentBlueBrush : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                Margin = new Thickness(0, 0, 8, 0)
            };
            dotBlock = dot;
            TextBlock iconTxt = new TextBlock
            {
                Text = iconChar,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            dot.Child = iconTxt;
            sp.Children.Add(dot);

            StackPanel labels = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            labels.Children.Add(new TextBlock { Text = title, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
            subBlock = new TextBlock { Text = sub, FontSize = 10, Foreground = new SolidColorBrush(Color.FromArgb(170, 235, 235, 245)), TextTrimming = TextTrimming.CharacterEllipsis };
            labels.Children.Add(subBlock);
            sp.Children.Add(labels);

            row.Child = sp;
            row.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };
            return row;
        }

        private Border CreateActionTile(string title, string sub, bool active, out TextBlock subBlock, Action onClick)
        {
            Border tile = CreateGlassTile();
            tile.Padding = new Thickness(12, 8, 12, 8);
            tile.Cursor = Cursors.Hand;

            StackPanel sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = title, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
            subBlock = new TextBlock { Text = sub, FontSize = 10.5, Foreground = new SolidColorBrush(Color.FromArgb(185, 235, 235, 245)), Margin = new Thickness(0, 1, 0, 0) };
            sp.Children.Add(subBlock);
            tile.Child = sp;

            tile.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };
            return tile;
        }

        private Border CreateMediaChip(string text, Action onClick)
        {
            Border chip = new Border();
            chip.CornerRadius = new CornerRadius(8);
            chip.Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255));
            chip.Padding = new Thickness(9, 5, 9, 5);
            chip.Margin = new Thickness(3, 0, 3, 0);
            chip.Cursor = Cursors.Hand;
            chip.Child = new TextBlock { Text = text, Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.SemiBold };
            chip.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };
            return chip;
        }

        public void ShowAnimated()
        {
            _isClosing = false;
            RefreshState();
            Left = SystemParameters.PrimaryScreenWidth - Width - 12;
            Top = (ShellController.Instance != null && ShellController.Instance.Config.ShowTopBar) ? 34 : 12;
            Show();
            Activate();

            AnimationHelper.SlideDownAndFadeIn(_card, _translateTransform, -16.0, 180);
        }

        public void HideAnimated(Action onCompleted = null)
        {
            if (_isClosing || !IsVisible) return;
            _isClosing = true;
            AnimationHelper.SlideUpAndFadeOut(_card, _translateTransform, -14.0, 130, delegate
            {
                Hide();
                _isClosing = false;
                if (onCompleted != null) onCompleted();
            });
        }

        private void UpdateMiniplayerUI()
        {
            NowPlayingInfo info = NowPlayingManager.Instance.CurrentInfo;
            if (info != null)
            {
                if (_ccMediaSource != null) _ccMediaSource.Text = "🎵 " + info.SourceApp;
                if (_ccTrackTitle != null) _ccTrackTitle.Text = info.Title;
                if (_ccTrackArtist != null) _ccTrackArtist.Text = info.Artist;
                if (_ccMediaStatus != null) _ccMediaStatus.Text = info.IsPlaying ? "Сейчас играет" : "Пауза";
                if (_ccPlayPauseText != null)
                {
                    _ccPlayPauseText.Text = info.IsPlaying ? "⏸ Пауза" : "▶ Пуск";
                }

                // Cover Image
                if (info.CoverImage != null)
                {
                    _ccCoverImage.Source = info.CoverImage;
                    _ccCoverImage.Visibility = Visibility.Visible;
                    _ccCoverIcon.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _ccCoverImage.Source = null;
                    _ccCoverImage.Visibility = Visibility.Collapsed;
                    _ccCoverIcon.Visibility = Visibility.Visible;
                }

                if (_ccProgressBar != null && _ccTimeElapsed != null && _ccTimeTotal != null)
                {
                    int pos = info.PositionSeconds;
                    int dur = Math.Max(1, info.DurationSeconds);
                    _ccProgressBar.Maximum = dur;
                    _ccProgressBar.Value = Math.Min(pos, dur);
                    _ccTimeElapsed.Text = string.Format("{0:D2}:{1:D2}", pos / 60, pos % 60);
                    _ccTimeTotal.Text = string.Format("{0:D2}:{1:D2}", dur / 60, dur % 60);
                }
            }
        }

        public void RefreshState()
        {
            bool dark = ShellController.Instance.IsDarkMode;
            _darkModeSub.Text = dark ? "Тёмная тема" : "Светлая тема";
            _darkModeTile.Background = dark
                ? new SolidColorBrush(Color.FromArgb(115, 10, 132, 255))
                : new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));

            bool iconsVis = NativeMethods.AreDesktopIconsVisible();
            _iconsSub.Text = iconsVis ? "Показаны" : "Скрыты";
            _iconsTile.Background = iconsVis
                ? new SolidColorBrush(Color.FromArgb(115, 10, 132, 255))
                : new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));

            bool widgetsVis = ShellController.Instance.Config.ShowDesktopWidgets;
            _widgetsSub.Text = widgetsVis ? "Показаны на рабочем столе" : "Скрыты";
            _widgetsTile.Background = widgetsVis
                ? new SolidColorBrush(Color.FromArgb(115, 10, 132, 255))
                : new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));

            bool autoHide = ShellController.Instance.DockAutoHide;
            _dockHideSub.Text = autoHide ? "Скрывать в полноэкранных окнах" : "Всегда показывать поверх окон";
            _dockHideTile.Background = autoHide
                ? new SolidColorBrush(Color.FromArgb(115, 10, 132, 255))
                : new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));

            bool stageActive = ShellController.Instance.Config.EnableStageManager;
            if (_stageManagerSub != null)
            {
                _stageManagerSub.Text = stageActive ? "Включён (боковая панель)" : "Выключен";
                _stageManagerTile.Background = stageActive
                    ? new SolidColorBrush(Color.FromArgb(115, 10, 132, 255))
                    : new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));
            }

            // Network Subtitle & Dot
            NetworkStatusInfo net = NetworkHelper.GetCurrentStatus();
            if (_connWifiSub != null)
            {
                if (net.IsConnected)
                {
                    if (net.Kind == NetworkKind.WiFi)
                    {
                        _connWifiSub.Text = !string.IsNullOrEmpty(net.Ssid) ? ("Wi-Fi: " + net.Ssid) : "Wi-Fi • Подключено";
                    }
                    else if (net.Kind == NetworkKind.Ethernet)
                    {
                        _connWifiSub.Text = "Ethernet (Провод)";
                    }
                    else
                    {
                        _connWifiSub.Text = net.InterfaceName + " • В сети";
                    }
                }
                else
                {
                    _connWifiSub.Text = NetworkHelper.IsWifiEnabled ? "Сеть отключена" : "Wi-Fi выключен";
                }
            }
            if (_connWifiDot != null)
            {
                _connWifiDot.Background = (net.IsConnected || NetworkHelper.IsWifiEnabled)
                    ? LiquidGlassTheme.AccentBlueBrush
                    : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
            }

            // Bluetooth Subtitle & Dot
            if (_connBtSub != null)
            {
                _connBtSub.Text = BluetoothHelper.IsBluetoothEnabled ? "Вкл." : "Выкл.";
            }
            if (_connBtDot != null)
            {
                _connBtDot.Background = BluetoothHelper.IsBluetoothEnabled
                    ? LiquidGlassTheme.AccentBlueBrush
                    : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
            }

            UpdateMiniplayerUI();
        }
    }
}
