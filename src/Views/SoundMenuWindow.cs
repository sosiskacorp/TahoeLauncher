using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

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
        private TextBlock _mediaTitle;
        private TextBlock _mediaArtist;
        private TextBlock _mediaSourceBadge;
        private TextBlock _playBtnIcon;

        public SoundMenuWindow()
        {
            Title = "Звук";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Width = 330;
            Height = 440;
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

            // 3. NOW PLAYING PREVIEW CARD
            Border mediaCard = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 8)
            };

            Grid mediaGrid = new Grid();
            mediaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mediaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel trackCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _mediaSourceBadge = new TextBlock
            {
                Text = "🎵 Яндекс Музыка",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = LiquidGlassTheme.AccentBlueBrush
            };
            _mediaTitle = new TextBlock
            {
                Text = "Название трека",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 1, 0, 0)
            };
            _mediaArtist = new TextBlock
            {
                Text = "Исполнитель",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromArgb(170, 235, 235, 245)),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            trackCol.Children.Add(_mediaSourceBadge);
            trackCol.Children.Add(_mediaTitle);
            trackCol.Children.Add(_mediaArtist);
            Grid.SetColumn(trackCol, 0);
            mediaGrid.Children.Add(trackCol);

            Border playBtn = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                Background = LiquidGlassTheme.AccentBlueBrush,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            _playBtnIcon = new TextBlock
            {
                Text = "⏯",
                FontSize = 12,
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
            Grid.SetColumn(playBtn, 1);
            mediaGrid.Children.Add(playBtn);

            mediaCard.Child = mediaGrid;
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
                Border row = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Background = dev.IsDefault ? new SolidColorBrush(Color.FromArgb(60, 0, 122, 255)) : new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
                    Padding = new Thickness(8, 6, 8, 6),
                    Margin = new Thickness(0, 0, 0, 4),
                    Cursor = Cursors.Hand
                };

                Grid g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                TextBlock ico = new TextBlock
                {
                    Text = dev.TypeIcon,
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(ico, 0);
                g.Children.Add(ico);

                TextBlock name = new TextBlock
                {
                    Text = dev.Name,
                    FontSize = 11.5,
                    FontWeight = dev.IsDefault ? FontWeights.Bold : FontWeights.Normal,
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
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(check, 2);
                    g.Children.Add(check);
                }

                row.Child = g;

                string capturedId = dev.Id;
                row.MouseEnter += delegate
                {
                    if (!dev.IsDefault) row.Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255));
                };
                row.MouseLeave += delegate
                {
                    row.Background = dev.IsDefault ? new SolidColorBrush(Color.FromArgb(60, 0, 122, 255)) : new SolidColorBrush(Color.FromArgb(25, 255, 255, 255));
                };
                row.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    AudioHelper.SetDefaultAudioDevice(capturedId);
                    RefreshSound();
                };

                _devicesStack.Children.Add(row);
            }

            UpdateNowPlayingUI();
        }

        private void UpdateNowPlayingUI()
        {
            NowPlayingInfo info = NowPlayingManager.Instance.CurrentInfo;
            if (info != null)
            {
                _mediaSourceBadge.Text = "🎵 " + info.SourceApp;
                _mediaTitle.Text = info.Title;
                _mediaArtist.Text = info.Artist;
                _playBtnIcon.Text = info.IsPlaying ? "⏸" : "▶";
            }
        }

        public void ShowAnimated()
        {
            _isClosing = false;
            RefreshSound();
            Left = Math.Max(10, SystemParameters.PrimaryScreenWidth - Width - 80);
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
