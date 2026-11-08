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

        private TextBlock _statusTitle;
        private TextBlock _statusSubtitle;
        private TextBlock _ipText;
        private TextBlock _speedText;
        private TextBlock _adapterText;
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
            Height = 350;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();

            Deactivated += delegate
            {
                if (!_isClosing && IsVisible) HideAnimated();
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

            // 1. HEADER
            Grid headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _connTypeIcon = new TextBlock
            {
                Text = "🔌",
                FontSize = 20,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(_connTypeIcon, 0);
            headerGrid.Children.Add(_connTypeIcon);

            StackPanel titles = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _statusTitle = new TextBlock
            {
                Text = "Подключение к сети",
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            _statusSubtitle = new TextBlock
            {
                Text = "Проверка состояния...",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 235, 235, 245))
            };
            titles.Children.Add(_statusTitle);
            titles.Children.Add(_statusSubtitle);
            Grid.SetColumn(titles, 1);
            headerGrid.Children.Add(titles);

            root.Children.Add(headerGrid);

            // 2. ACTIVE CONNECTION DETAILS CARD
            Border detailsCard = new Border
            {
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10)
            };

            StackPanel detailsStack = new StackPanel();

            _adapterText = CreateDetailRow("Адаптер:", "Ethernet");
            _ipText = CreateDetailRow("IP-адрес:", "192.168.1.1");
            _speedText = CreateDetailRow("Скорость:", "1 Гбит/с");

            detailsStack.Children.Add(_adapterText);
            detailsStack.Children.Add(_ipText);
            detailsStack.Children.Add(_speedText);
            detailsCard.Child = detailsStack;
            root.Children.Add(detailsCard);

            // 3. QUICK ACTIONS
            Border wifiToggleRow = CreateClickableAction("Wi-Fi сети", "Поиск доступных точек...", delegate
            {
                ShellController.RunProcess("explorer.exe", "ms-settings:network-wifi");
                HideAnimated();
            });
            root.Children.Add(wifiToggleRow);

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
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromArgb(210, 235, 235, 245)),
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
                sp.Children.Add(new TextBlock { Text = sub, FontSize = 10.5, Foreground = new SolidColorBrush(Color.FromArgb(160, 235, 235, 245)), Margin = new Thickness(0, 1, 0, 0) });
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

        public void RefreshNetwork()
        {
            NetworkStatusInfo info = NetworkHelper.GetCurrentStatus();
            if (info.IsConnected)
            {
                _statusTitle.Text = info.ConnectionTitle;
                _statusSubtitle.Text = "Подключено • Доступ к Интернету";
                _adapterText.Text = "Адаптер: " + info.InterfaceName;
                _ipText.Text = "IP-адрес: " + (string.IsNullOrEmpty(info.IpAddress) ? "DHCP назначен" : info.IpAddress);
                _speedText.Text = "Скорость линка: " + (string.IsNullOrEmpty(info.Speed) ? "Автоопределение" : info.Speed);
                _connTypeIcon.Text = (info.Kind == NetworkKind.Ethernet) ? "🔌" : "📶";
            }
            else
            {
                _statusTitle.Text = "Нет подключения";
                _statusSubtitle.Text = "Кабель отключен или нет сети";
                _adapterText.Text = "Интерфейсы не активны";
                _ipText.Text = "IP-адрес: —";
                _speedText.Text = "Скорость: 0 Мбит/с";
                _connTypeIcon.Text = "⚠️";
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
