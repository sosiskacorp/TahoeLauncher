using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace MacOSTahoe
{
    public class SoundMenuWindow : Window
    {
        private Border _card;
        private TranslateTransform _translateTransform;
        private bool _isClosing = false;

        private Slider _volSlider;
        private TextBlock _volLabel;
        private StackPanel _devicesStack;

        // Rich Miniplayer Elements
        private Image _coverImage;
        private TextBlock _coverFallback;
        private TextBlock _mediaTitle;
        private TextBlock _mediaArtist;
        private TextBlock _mediaSourceBadge;
        private TextBlock _playBtnIcon;
        private ProgressBar _timelineProgress;
        private TextBlock _timeElapsed;
        private TextBlock _timeTotal;

        public SoundMenuWindow()
        {
            Title = "Звук";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Width = 330;
            SizeToContent = SizeToContent.Height;
            MaxHeight = SystemParameters.WorkArea.Height - 40;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();

            Deactivated += delegate
            {
                if (!_isClosing && IsVisible) HideAnimated();
            };

            NowPlayingManager.Instance.MediaUpdated += delegate
            {
                if (IsVisible) Dispatcher.BeginInvoke(new Action(UpdateNowPlayingUI));
            };
        }

        private void BuildUI()
        {
            Border card = new Border
            {
                Margin = new Thickness(12),
                CornerRadius = new CornerRadius(22),
                Background = LiquidGlassTheme.CreateGlassBrush(228, 244),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14),
                Effect = new DropShadowEffect { BlurRadius = 28, ShadowDepth = 8, Opacity = 0.6, Color = Colors.Black }
            };
            _card = card;
            _translateTransform = new TranslateTransform(0, 0);
            _card.RenderTransform = _translateTransform;

            StackPanel root = new StackPanel();

            // 1. VOLUME HEADER & SLIDER
            Grid volHeader = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            volHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            volHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock volTitle = new TextBlock
            {
                Text = "Громкость",
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            Grid.SetColumn(volTitle, 0);
            volHeader.Children.Add(volTitle);

            _volLabel = new TextBlock
            {
                Text = "75%",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 235, 235, 245))
            };
            Grid.SetColumn(_volLabel, 1);
            volHeader.Children.Add(_volLabel);
            root.Children.Add(volHeader);

            Grid sliderRow = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            sliderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            sliderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock muteBtn = new TextBlock
            {
                Text = "🔊",
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            muteBtn.MouseLeftButtonDown += delegate
            {
                AudioHelper.ToggleMute();
                RefreshSound();
            };
            Grid.SetColumn(muteBtn, 0);
            sliderRow.Children.Add(muteBtn);

            _volSlider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                Value = 75,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };
            _volSlider.ValueChanged += delegate
            {
                int val = (int)_volSlider.Value;
                _volLabel.Text = val + "%";
                AudioHelper.SetMasterVolume(val);
            };
            Grid.SetColumn(_volSlider, 1);
            sliderRow.Children.Add(_volSlider);
            root.Children.Add(sliderRow);

            // 2. OUTPUT DEVICES SECTION
            TextBlock outHeader = new TextBlock
            {
                Text = "ВЫХОД ЗВУКА",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(160, 235, 235, 245)),
                Margin = new Thickness(0, 0, 0, 6)
            };
            root.Children.Add(outHeader);

            _devicesStack = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            root.Children.Add(_devicesStack);

            // 3. NOW PLAYING RICH CARD
            Border mediaCard = new Border
            {
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 10, 10, 10),
                Margin = new Thickness(0, 0, 0, 10)
            };

            StackPanel mediaRoot = new StackPanel();

            // Track details row (Cover + Info)
            Grid trackRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            trackRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            trackRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border coverBorder = new Border
            {
                Width = 44,
                Height = 44,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)),
                ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid coverGrid = new Grid();
            _coverFallback = new TextBlock
            {
                Text = "🎵",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _coverImage = new Image
            {
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            coverGrid.Children.Add(_coverFallback);
            coverGrid.Children.Add(_coverImage);
            coverBorder.Child = coverGrid;
            Grid.SetColumn(coverBorder, 0);
            trackRow.Children.Add(coverBorder);

            StackPanel trackCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };
            _mediaSourceBadge = new TextBlock
            {
                Text = "🎵 Яндекс Музыка",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = LiquidGlassTheme.AccentBlueBrush
            };
            _mediaTitle = new TextBlock
            {
                Text = "Музыка не играет",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 1, 0, 0)
            };
            _mediaArtist = new TextBlock
            {
                Text = "Запустите плеер",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(170, 235, 235, 245)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 1, 0, 0)
            };
            trackCol.Children.Add(_mediaSourceBadge);
            trackCol.Children.Add(_mediaTitle);
            trackCol.Children.Add(_mediaArtist);
            Grid.SetColumn(trackCol, 1);
            trackRow.Children.Add(trackCol);

            mediaRoot.Children.Add(trackRow);

            // Timeline Track Bar
            Grid timelineGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            timelineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            timelineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            timelineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _timeElapsed = new TextBlock
            {
                Text = "00:00",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromArgb(160, 235, 235, 245))
            };
            Grid.SetColumn(_timeElapsed, 0);
            timelineGrid.Children.Add(_timeElapsed);

            _timelineProgress = new ProgressBar
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
            Grid.SetColumn(_timelineProgress, 1);
            timelineGrid.Children.Add(_timelineProgress);

            _timeTotal = new TextBlock
            {
                Text = "03:00",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromArgb(160, 235, 235, 245))
            };
            Grid.SetColumn(_timeTotal, 2);
            timelineGrid.Children.Add(_timeTotal);

            mediaRoot.Children.Add(timelineGrid);

            // Playback Controls (Prev, Play/Pause, Next)
            StackPanel controls = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };

            // Previous Button
            Border prevBtn = CreateMediaChip("⏮", delegate { NowPlayingManager.Instance.PreviousTrack(); });
            controls.Children.Add(prevBtn);

            // Play/Pause Button
            Border playBtn = new Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(16),
                Background = LiquidGlassTheme.AccentBlueBrush,
                Cursor = Cursors.Hand,
                Margin = new Thickness(14, 0, 14, 0)
            };
            _playBtnIcon = new TextBlock
            {
                Text = "▶",
                FontSize = 13,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            playBtn.Child = _playBtnIcon;
            playBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                NowPlayingManager.Instance.PlayPause();
            };
            controls.Children.Add(playBtn);

            // Next Button
            Border nextBtn = CreateMediaChip("⏭", delegate { NowPlayingManager.Instance.NextTrack(); });
            controls.Children.Add(nextBtn);

            mediaRoot.Children.Add(controls);
            mediaCard.Child = mediaRoot;
            root.Children.Add(mediaCard);

            // 4. SETTINGS LINK
            Border soundSettingsBtn = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)),
                Padding = new Thickness(8, 6, 8, 6),
                Cursor = Cursors.Hand
            };
            soundSettingsBtn.Child = new TextBlock
            {
                Text = "Параметры звука Windows...",
                FontSize = 11.5,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            soundSettingsBtn.MouseEnter += delegate { soundSettingsBtn.Background = LiquidGlassTheme.AccentBlueBrush; };
            soundSettingsBtn.MouseLeave += delegate { soundSettingsBtn.Background = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)); };
            soundSettingsBtn.MouseLeftButtonDown += delegate
            {
                ShellController.RunProcess("explorer.exe", "ms-settings:sound");
                HideAnimated();
            };
            root.Children.Add(soundSettingsBtn);

            card.Child = root;
            Content = card;
        }

        private Border CreateMediaChip(string icon, Action onClick)
        {
            Border chip = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)),
                Cursor = Cursors.Hand
            };
            TextBlock tb = new TextBlock
            {
                Text = icon,
                FontSize = 12,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            chip.Child = tb;
            chip.MouseEnter += delegate { chip.Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)); };
            chip.MouseLeave += delegate { chip.Background = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)); };
            chip.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };
            return chip;
        }

        public void RefreshSound()
        {
            bool isMuted;
            int vol = AudioHelper.GetMasterVolume(out isMuted);
            _volSlider.Value = vol;
            _volLabel.Text = isMuted ? "Без звука" : (vol + "%");

            _devicesStack.Children.Clear();
            List<AudioDeviceItem> devices = AudioHelper.GetPlaybackDevices();

            for (int i = 0; i < devices.Count; i++)
            {
                AudioDeviceItem dev = devices[i];
                Border row = CreateDeviceRow(dev);
                _devicesStack.Children.Add(row);
            }

            UpdateNowPlayingUI();
        }

        private Border CreateDeviceRow(AudioDeviceItem dev)
        {
            Border row = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = dev.IsDefault ? LiquidGlassTheme.AccentBlueBrush : new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 2, 0, 2),
                Cursor = Cursors.Hand
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock icon = new TextBlock
            {
                Text = dev.TypeIcon,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(icon, 0);
            g.Children.Add(icon);

            TextBlock name = new TextBlock
            {
                Text = dev.Name,
                FontSize = 11.5,
                FontWeight = dev.IsDefault ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(name, 1);
            g.Children.Add(name);

            if (dev.IsDefault)
            {
                TextBlock check = new TextBlock
                {
                    Text = "✓",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(check, 2);
                g.Children.Add(check);
            }

            row.Child = g;

            row.MouseEnter += delegate
            {
                if (!dev.IsDefault) row.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
            };
            row.MouseLeave += delegate
            {
                if (!dev.IsDefault) row.Background = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255));
            };

            row.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                AudioHelper.SetDefaultAudioDevice(dev.Id);
                RefreshSound();
            };

            return row;
        }

        private void UpdateNowPlayingUI()
        {
            NowPlayingInfo info = NowPlayingManager.Instance.CurrentInfo;

            _mediaSourceBadge.Text = "🎵 " + info.SourceApp;
            _mediaTitle.Text = info.Title;
            _mediaArtist.Text = info.Artist;

            // Play/Pause icon
            _playBtnIcon.Text = info.IsPlaying ? "⏸" : "▶";

            // Cover Image
            if (info.CoverImage != null)
            {
                _coverImage.Source = info.CoverImage;
                _coverImage.Visibility = Visibility.Visible;
                _coverFallback.Visibility = Visibility.Collapsed;
            }
            else
            {
                _coverImage.Source = null;
                _coverImage.Visibility = Visibility.Collapsed;
                _coverFallback.Visibility = Visibility.Visible;
            }

            // Timeline
            int cur = info.PositionSeconds;
            int total = Math.Max(1, info.DurationSeconds);
            _timeElapsed.Text = FormatTime(cur);
            _timeTotal.Text = FormatTime(total);

            _timelineProgress.Maximum = total;
            _timelineProgress.Value = Math.Min(cur, total);
        }

        private string FormatTime(int seconds)
        {
            if (seconds < 0) seconds = 0;
            int m = seconds / 60;
            int s = seconds % 60;
            return string.Format("{0:D2}:{1:D2}", m, s);
        }

        public void ShowAnimated()
        {
            _isClosing = false;
            RefreshSound();
            Left = Math.Max(10, SystemParameters.PrimaryScreenWidth - Width - 70);
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
    }
}
