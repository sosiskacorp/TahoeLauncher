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
    public class DesktopWidgetsWindow : Window
    {
        private TextBlock _timeText;
        private TextBlock _dateText;
        private Border _ramFillBar;
        private TextBlock _ramLabel;
        private Border _diskFillBar;
        private TextBlock _diskLabel;
        private Border _appsFillBar;
        private TextBlock _appsLabel;
        private WrapPanel _favWrap;
        private TextBox _noteBox;
        private DispatcherTimer _timer;

        public DesktopWidgetsWindow()
        {
            Title = "Tahoe Desktop Widgets";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = false;
            ShowInTaskbar = false;
            Width = 340;
            Height = 640;
            Left = SystemParameters.PrimaryScreenWidth - Width - 18;
            Top = 44;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();

            SourceInitialized += delegate
            {
                NativeMethods.MakeToolWindow(this, true);
                NativeMethods.SendWindowToBottom(this);
            };

            Activated += delegate
            {
                NativeMethods.SendWindowToBottom(this);
            };

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Tick += OnTick;
            _timer.Start();
            OnTick(null, EventArgs.Empty);
        }

        private void BuildUI()
        {
            StackPanel root = new StackPanel();

            // WIDGET 1: Clock & System Telemetry Card
            Border clockCard = CreateWidgetCard();
            clockCard.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    try { DragMove(); } catch { }
                }
            };

            StackPanel clockStack = new StackPanel();
            Grid headerRow = new Grid();
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _timeText = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm"),
                FontSize = 38,
                FontWeight = FontWeights.Light,
                Foreground = Brushes.White
            };
            Grid.SetColumn(_timeText, 0);
            headerRow.Children.Add(_timeText);

            Border settingsChip = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255)),
                Padding = new Thickness(9, 4, 9, 4),
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = Cursors.Hand,
                ToolTip = "Открыть Настройки лаунчера"
            };
            settingsChip.Child = new TextBlock { Text = "⚙ Лаунчер", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
            settingsChip.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                ShellController.Instance.ShowLauncherSettings();
            };
            Grid.SetColumn(settingsChip, 1);
            headerRow.Children.Add(settingsChip);
            clockStack.Children.Add(headerRow);

            CultureInfo ru = new CultureInfo("ru-RU");
            _dateText = new TextBlock
            {
                Text = DateTime.Now.ToString("dddd, d MMMM", ru),
                FontSize = 12.5,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromArgb(185, 235, 235, 245)),
                Margin = new Thickness(0, 0, 0, 12)
            };
            clockStack.Children.Add(_dateText);

            clockStack.Children.Add(CreateMetricBar("Память ОЗУ", out _ramLabel, out _ramFillBar, Color.FromRgb(10, 132, 255)));
            clockStack.Children.Add(CreateMetricBar("Диск C:", out _diskLabel, out _diskFillBar, Color.FromRgb(48, 209, 88)));
            clockStack.Children.Add(CreateMetricBar("Активные окна", out _appsLabel, out _appsFillBar, Color.FromRgb(191, 90, 242)));

            clockCard.Child = clockStack;
            root.Children.Add(clockCard);

            // WIDGET 2: Quick Launch / Favorites Card
            Border favCard = CreateWidgetCard();
            StackPanel favStack = new StackPanel();
            Grid favHeader = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            favHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            favHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            favHeader.Children.Add(new TextBlock
            {
                Text = "★ Быстрый запуск",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            });

            TextBlock allAppsLink = new TextBlock
            {
                Text = "Все →",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 210, 255)),
                Cursor = Cursors.Hand
            };
            allAppsLink.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                ShellController.Instance.ToggleLaunchpad();
            };
            Grid.SetColumn(allAppsLink, 1);
            favHeader.Children.Add(allAppsLink);
            favStack.Children.Add(favHeader);

            _favWrap = new WrapPanel();
            favStack.Children.Add(_favWrap);
            favCard.Child = favStack;
            root.Children.Add(favCard);

            // WIDGET 3: Quick Notes Card
            Border noteCard = CreateWidgetCard();
            StackPanel noteStack = new StackPanel();
            noteStack.Children.Add(new TextBlock
            {
                Text = "📝 Заметки рабочего стола",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 6)
            });

            _noteBox = new TextBox
            {
                Text = ShellController.Instance.Config.QuickNoteText ?? "",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 86,
                Background = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Foreground = new SolidColorBrush(Color.FromArgb(235, 255, 255, 255)),
                CaretBrush = Brushes.White,
                FontSize = 12,
                Padding = new Thickness(8, 6, 8, 6)
            };
            _noteBox.TextChanged += delegate
            {
                ShellController.Instance.Config.QuickNoteText = _noteBox.Text;
                ShellController.Instance.SaveConfig();
            };
            noteStack.Children.Add(_noteBox);
            noteCard.Child = noteStack;
            root.Children.Add(noteCard);

            Content = root;
            RefreshFavorites();
        }

        private Border CreateWidgetCard()
        {
            return new Border
            {
                Margin = new Thickness(8, 6, 8, 6),
                CornerRadius = new CornerRadius(20),
                Background = LiquidGlassTheme.CreateGlassBrush(185, 210),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 12, 14, 12),
                Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 6, Opacity = 0.45, Color = Colors.Black }
            };
        }

        private FrameworkElement CreateMetricBar(string title, out TextBlock valBlock, out Border fillBorder, Color accent)
        {
            StackPanel sp = new StackPanel { Margin = new Thickness(0, 4, 0, 4) };
            Grid row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            row.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(190, 235, 235, 245))
            });
            valBlock = new TextBlock
            {
                Text = "0%",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };
            Grid.SetColumn(valBlock, 1);
            row.Children.Add(valBlock);
            sp.Children.Add(row);

            Border track = new Border
            {
                Height = 6,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Margin = new Thickness(0, 3, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            fillBorder = new Border
            {
                Height = 6,
                Width = 80,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(accent),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            track.Child = fillBorder;
            sp.Children.Add(track);
            return sp;
        }

        public void RefreshFavorites()
        {
            if (_favWrap == null || ShellController.Instance == null) return;
            _favWrap.Children.Clear();

            List<string> favs = ShellController.Instance.Config.FavoriteApps;
            List<AppCatalogEntry> catalog = ShellController.Instance.InstalledApps;
            int shown = 0;

            for (int i = 0; i < favs.Count && shown < 6; i++)
            {
                string favTitle = favs[i];
                AppCatalogEntry matched = null;
                for (int j = 0; j < catalog.Count; j++)
                {
                    if (string.Equals(catalog[j].Title, favTitle, StringComparison.OrdinalIgnoreCase) ||
                        catalog[j].Title.IndexOf(favTitle, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matched = catalog[j];
                        break;
                    }
                }
                if (matched == null) continue;

                AppCatalogEntry cap = matched;
                Border chip = new Border
                {
                    Width = 92,
                    Height = 66,
                    Margin = new Thickness(3),
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                    ToolTip = "Открыть " + cap.Title
                };

                StackPanel sp = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Image img = new Image
                {
                    Source = cap.IconSource ?? ShellController.Instance.GetIcon("finder.png"),
                    Width = 30,
                    Height = 30
                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                sp.Children.Add(img);
                sp.Children.Add(new TextBlock
                {
                    Text = cap.Title,
                    FontSize = 10.5,
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 84,
                    Margin = new Thickness(0, 4, 0, 0)
                });
                chip.Child = sp;

                chip.MouseEnter += delegate { chip.Background = new SolidColorBrush(Color.FromArgb(65, 10, 132, 255)); };
                chip.MouseLeave += delegate { chip.Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)); };
                chip.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    ShellController.RunProcess(cap.Cmd, cap.Args);
                };

                _favWrap.Children.Add(chip);
                shown++;
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!IsVisible) return;
            NativeMethods.SendWindowToBottom(this);

            CultureInfo ru = new CultureInfo("ru-RU");
            _timeText.Text = DateTime.Now.ToString("HH:mm");
            _dateText.Text = DateTime.Now.ToString("dddd, d MMMM", ru);

            double trackWidth = 280.0;
            NativeMethods.MEMORYSTATUSEX mem = new NativeMethods.MEMORYSTATUSEX();
            mem.dwLength = (uint)Marshal.SizeOf(mem);
            if (NativeMethods.GlobalMemoryStatusEx(ref mem))
            {
                double usedGb = (mem.ullTotalPhys - mem.ullAvailPhys) / (1024.0 * 1024.0 * 1024.0);
                double totalGb = mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                _ramLabel.Text = string.Format("{0:0.0} / {1:0} ГБ ({2}%)", usedGb, totalGb, mem.dwMemoryLoad);
                _ramFillBar.Width = Math.Max(10, trackWidth * (mem.dwMemoryLoad / 100.0));
            }

            try
            {
                DriveInfo di = new DriveInfo("C");
                if (di.IsReady)
                {
                    double freeGb = di.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    double totalGb = di.TotalSize / (1024.0 * 1024.0 * 1024.0);
                    double usedPct = ((totalGb - freeGb) / totalGb) * 100.0;
                    _diskLabel.Text = string.Format("Свободно {0:0} ГБ", freeGb);
                    _diskFillBar.Width = Math.Max(10, trackWidth * (usedPct / 100.0));
                }
            }
            catch { }

            if (ShellController.Instance != null)
            {
                int winCount = ShellController.Instance.GetOpenApplicationWindows().Count;
                _appsLabel.Text = string.Format("{0} активных", winCount);
                _appsFillBar.Width = Math.Max(12, Math.Min(trackWidth, trackWidth * (winCount / 14.0)));
            }
        }
    }

    // =========================================================================
    // 4. CONTROL CENTER WINDOW (Пункт управления)
    // =========================================================================
}
