using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using MacOSTahoe.Core;
using MacOSTahoe.Native;
using MacOSTahoe.Theme;

namespace MacOSTahoe.Views
{
    public class CalendarWidgetWindow : Window
    {
        private DateTime _displayedMonth;
        private TextBlock _txtMonthYear;
        private Grid _daysGrid;

        public CalendarWidgetWindow()
        {
            Title = "CalendarWidgetWindow";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;

            Visibility = Visibility.Collapsed;
            IsHitTestVisible = false;

            Width = 280;
            Height = 310;
            Left = SystemParameters.PrimaryScreenWidth - Width - 14;
            Top = 40;

            _displayedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            BuildUi();

            SourceInitialized += (s, e) =>
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                DwmBlurHelper.ApplyBlur(hwnd, true);
                DwmBlurHelper.MakeToolWindow(hwnd);
            };

            Deactivated += (s, e) =>
            {
                HideCalendar();
            };
        }

        private void BuildUi()
        {
            var glass = new Border
            {
                Background = LiquidGlassTheme.CreateGlassBrush(190, 230),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(14)
            };

            var stack = new StackPanel { Orientation = Orientation.Vertical };

            // Month navigation header
            var navGrid = new Grid();
            navGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            navGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _txtMonthYear = new TextBlock
            {
                Text = "",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_txtMonthYear, 0);
            navGrid.Children.Add(_txtMonthYear);

            var btnsStack = new StackPanel { Orientation = Orientation.Horizontal };
            var btnPrev = CreateNavButton("‹", () =>
            {
                _displayedMonth = _displayedMonth.AddMonths(-1);
                RenderCalendar();
            });
            var btnNext = CreateNavButton("›", () =>
            {
                _displayedMonth = _displayedMonth.AddMonths(1);
                RenderCalendar();
            });
            btnsStack.Children.Add(btnPrev);
            btnsStack.Children.Add(btnNext);
            Grid.SetColumn(btnsStack, 1);
            navGrid.Children.Add(btnsStack);

            stack.Children.Add(navGrid);

            // Weekday names
            var weekGrid = new Grid { Margin = new Thickness(0, 10, 0, 6) };
            for (int i = 0; i < 7; i++)
            {
                weekGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            string[] dayNames = new[] { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
            for (int i = 0; i < 7; i++)
            {
                var lbl = new TextBlock
                {
                    Text = dayNames[i],
                    FontFamily = LiquidGlassTheme.UiFont,
                    FontSize = 11,
                    FontWeight = FontWeights.Medium,
                    Foreground = (i >= 5) ? new SolidColorBrush(Color.FromRgb(255, 105, 105)) : new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(lbl, i);
                weekGrid.Children.Add(lbl);
            }
            stack.Children.Add(weekGrid);

            // Days Grid (6 rows x 7 columns)
            _daysGrid = new Grid();
            for (int i = 0; i < 7; i++)
            {
                _daysGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            for (int r = 0; r < 6; r++)
            {
                _daysGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
            }
            stack.Children.Add(_daysGrid);

            glass.Child = stack;
            Content = glass;
        }

        private Border CreateNavButton(string arrow, Action onClick)
        {
            var btn = new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(13),
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                Margin = new Thickness(3, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            var txt = new TextBlock
            {
                Text = arrow,
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 16,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            btn.Child = txt;
            btn.MouseLeftButtonUp += (s, e) =>
            {
                if (onClick != null) onClick();
            };
            return btn;
        }

        private void RenderCalendar()
        {
            _daysGrid.Children.Clear();
            var ruCulture = new CultureInfo("ru-RU");
            _txtMonthYear.Text = _displayedMonth.ToString("MMMM yyyy", ruCulture);
            _txtMonthYear.Text = char.ToUpper(_txtMonthYear.Text[0]) + _txtMonthYear.Text.Substring(1);

            DateTime firstDayOfMonth = new DateTime(_displayedMonth.Year, _displayedMonth.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(_displayedMonth.Year, _displayedMonth.Month);

            // Monday = 0, Sunday = 6
            int startOffset = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;

            DateTime today = DateTime.Today;

            int row = 0;
            int col = startOffset;

            for (int day = 1; day <= daysInMonth; day++)
            {
                bool isToday = (_displayedMonth.Year == today.Year && _displayedMonth.Month == today.Month && day == today.Day);

                var border = new Border
                {
                    Width = 28,
                    Height = 28,
                    CornerRadius = new CornerRadius(14),
                    Background = isToday ? LiquidGlassTheme.AccentBlueBrush : Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var txt = new TextBlock
                {
                    Text = day.ToString(),
                    FontFamily = LiquidGlassTheme.UiFont,
                    FontSize = 12,
                    FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                border.Child = txt;

                Grid.SetRow(border, row);
                Grid.SetColumn(border, col);
                _daysGrid.Children.Add(border);

                col++;
                if (col == 7)
                {
                    col = 0;
                    row++;
                }
            }
        }

        public void ShowCalendar()
        {
            Visibility = Visibility.Visible;
            IsHitTestVisible = true;
            _displayedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            RenderCalendar();
            Activate();
        }

        public void HideCalendar()
        {
            Visibility = Visibility.Collapsed;
            IsHitTestVisible = false;
        }
    }
}
