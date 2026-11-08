using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace MacOSTahoe
{
    public class NetworkMenuWindow : Window
    {
        private Border _card;
        private TranslateTransform _translateTransform;
        private bool _isClosing = false;

        private Border _wifiToggleChip;
        private TextBlock _wifiToggleText;
        private TextBlock _statusTitle;
        private TextBlock _statusSubtitle;
        private TextBlock _ipText;
        private TextBlock _speedText;
        private TextBlock _adapterText;
        private TextBlock _signalText;
        private TextBlock _connTypeIcon;

        public NetworkMenuWindow()
        {
            Title = "Сеть";
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

            NetworkHelper.WifiStateChanged += delegate
            {
                if (IsVisible) Dispatcher.BeginInvoke(new Action(RefreshNetwork));
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

            // 1. WI-FI MASTER TOGGLE ROW
            Border wifiToggleRow = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 10)
            };

            Grid wifiGrid = new Grid();
            wifiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            wifiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            wifiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock wifiIcon = new TextBlock
            {
                Text = "📶",
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(wifiIcon, 0);
            wifiGrid.Children.Add(wifiIcon);

            TextBlock wifiLabel = new TextBlock
            {
                Text = "Wi-Fi модуль",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(wifiLabel, 1);
            wifiGrid.Children.Add(wifiLabel);

            _wifiToggleChip = new Border
            {
                Width = 52,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = LiquidGlassTheme.AccentBlueBrush,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            _wifiToggleText = new TextBlock
            {
                Text = "Вкл",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _wifiToggleChip.Child = _wifiToggleText;
            _wifiToggleChip.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                NetworkHelper.ToggleWifi();
                UpdateWifiSwitchUI(!NetworkHelper.IsWifiEnabled);
            };
            Grid.SetColumn(_wifiToggleChip, 2);
            wifiGrid.Children.Add(_wifiToggleChip);

            wifiToggleRow.Child = wifiGrid;
            root.Children.Add(wifiToggleRow);

            // 2. ACTIVE CONNECTION HEADER
            Grid headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _connTypeIcon = new TextBlock
            {
                Text = "🔌",
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(_connTypeIcon, 0);
            headerGrid.Children.Add(_connTypeIcon);

            StackPanel titles = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _statusTitle = new TextBlock
            {
                Text = "Подключение к сети",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            _statusSubtitle = new TextBlock
            {
                Text = "Проверка состояния...",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 235, 235, 245))
            };
            titles.Children.Add(_statusTitle);
            titles.Children.Add(_statusSubtitle);
            Grid.SetColumn(titles, 1);
            headerGrid.Children.Add(titles);

            root.Children.Add(headerGrid);

            // 3. CONNECTION DETAILS CARD
            Border detailsCard = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 10)
            };

            StackPanel detailsStack = new StackPanel();

            _adapterText = CreateDetailRow("Адаптер:", "Ethernet");
            _ipText = CreateDetailRow("IP-адрес:", "192.168.1.1");
            _speedText = CreateDetailRow("Скорость:", "1 Гбит/с");
            _signalText = CreateDetailRow("Сигнал Wi-Fi:", "Отличный");

            detailsStack.Children.Add(_adapterText);
            detailsStack.Children.Add(_ipText);
            detailsStack.Children.Add(_speedText);
            detailsStack.Children.Add(_signalText);
            detailsCard.Child = detailsStack;
            root.Children.Add(detailsCard);

            // 4. QUICK ACTIONS
            Border wifiNetworksRow = CreateClickableAction("Поиск и подключение к Wi-Fi...", "Открыть системный список сетей Windows", delegate
            {
                ShellController.RunProcess("explorer.exe", "ms-settings:network-wifi");
                HideAnimated();
            });
            root.Children.Add(wifiNetworksRow);

            Border settingsRow = CreateClickableAction("Параметры сети и Интернета...", "", delegate
            {
                ShellController.RunProcess("explorer.exe", "ms-settings:network");
                HideAnimated();
            });
            settingsRow.Margin = new Thickness(0, 4, 0, 0);
            root.Children.Add(settingsRow);

            card.Child = root;
            Content = card;
        }

        private TextBlock CreateDetailRow(string label, string val)
        {
            TextBlock tb = new TextBlock
            {
                Text = label + " " + val,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(215, 235, 235, 245)),
                Margin = new Thickness(0, 2, 0, 2)
            };
            return tb;
        }

        private Border CreateClickableAction(string title, string sub, Action onClick)
        {
            Border b = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)),
                Padding = new Thickness(10, 8, 10, 8),
                Cursor = Cursors.Hand
            };

            StackPanel sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = title, FontSize = 12, FontWeight = FontWeights.Medium, Foreground = Brushes.White });
            if (!string.IsNullOrEmpty(sub))
            {
                sp.Children.Add(new TextBlock { Text = sub, FontSize = 10, Foreground = new SolidColorBrush(Color.FromArgb(160, 235, 235, 245)), Margin = new Thickness(0, 1, 0, 0) });
            }
            b.Child = sp;
            b.MouseEnter += delegate { b.Background = LiquidGlassTheme.AccentBlueBrush; };
            b.MouseLeave += delegate { b.Background = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)); };
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };
            return b;
        }

        private void UpdateWifiSwitchUI(bool enabled)
        {
            if (enabled)
            {
                _wifiToggleChip.Background = LiquidGlassTheme.AccentBlueBrush;
                _wifiToggleText.Text = "Вкл";
            }
            else
            {
                _wifiToggleChip.Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                _wifiToggleText.Text = "Выкл";
            }
        }

        public void RefreshNetwork()
        {
            UpdateWifiSwitchUI(NetworkHelper.IsWifiEnabled);

            NetworkStatusInfo info = NetworkHelper.GetCurrentStatus();
            if (info.IsConnected)
            {
                _statusTitle.Text = info.ConnectionTitle;
                _statusSubtitle.Text = "Подключено • Доступ в Интернет";
                _adapterText.Text = "Адаптер: " + info.InterfaceName;
                _ipText.Text = "IP-адрес: " + (string.IsNullOrEmpty(info.IpAddress) ? "DHCP" : info.IpAddress);
                _speedText.Text = "Скорость линка: " + (string.IsNullOrEmpty(info.Speed) ? "Авто" : info.Speed);

                if (info.Kind == NetworkKind.WiFi)
                {
                    _connTypeIcon.Text = "📶";
                    _signalText.Visibility = Visibility.Visible;
                    _signalText.Text = "Уровень сигнала: " + (string.IsNullOrEmpty(info.SignalStrength) ? "Хороший" : info.SignalStrength);
                }
                else
                {
                    _connTypeIcon.Text = "🔌";
                    _signalText.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                _statusTitle.Text = NetworkHelper.IsWifiEnabled ? "Сеть не подключена" : "Wi-Fi выключен";
                _statusSubtitle.Text = NetworkHelper.IsWifiEnabled ? "Выберите сеть для подключения" : "Включите Wi-Fi для поиска сетей";
                _adapterText.Text = "Адаптеры не активны";
                _ipText.Text = "IP-адрес: —";
                _speedText.Text = "Скорость: 0 Мбит/с";
                _signalText.Visibility = Visibility.Collapsed;
                _connTypeIcon.Text = NetworkHelper.IsWifiEnabled ? "⚠️" : "📶";
            }
        }

        public void ShowAnimated()
        {
            _isClosing = false;
            RefreshNetwork();
            Left = Math.Max(10, SystemParameters.PrimaryScreenWidth - Width - 110);
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
