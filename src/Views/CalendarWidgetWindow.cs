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
    public class CalendarWidgetWindow : Window
    {
        private TextBlock _bigTime;
        private TextBlock _bigDate;

        public CalendarWidgetWindow()
        {
            Title = "Календарь";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Width = 340;
            Height = 390;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();
            Deactivated += delegate { Hide(); };
        }

        private void BuildUI()
        {
            Border card = new Border
            {
                Margin = new Thickness(12),
                CornerRadius = new CornerRadius(24),
                Background = LiquidGlassTheme.CreateGlassBrush(226, 242),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16),
                Effect = new DropShadowEffect { BlurRadius = 28, ShadowDepth = 8, Opacity = 0.6, Color = Colors.Black }
            };

            StackPanel root = new StackPanel();

            _bigTime = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm"),
                FontSize = 44,
                FontWeight = FontWeights.Light,
                Foreground = Brushes.White
            };
            root.Children.Add(_bigTime);

            CultureInfo ru = new CultureInfo("ru-RU");
            _bigDate = new TextBlock
            {
                Text = DateTime.Now.ToString("dddd, d MMMM yyyy г.", ru),
                FontSize = 13.5,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromArgb(195, 235, 235, 245)),
                Margin = new Thickness(0, 0, 0, 14)
            };
            root.Children.Add(_bigDate);

            Border calCard = new Border
            {
                CornerRadius = new CornerRadius(16),
                Background = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 10, 12, 10)
            };

            UniformGrid calGrid = new UniformGrid { Columns = 7 };
            string[] days = new string[] { "ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ", "ВС" };
            for (int i = 0; i < 7; i++)
            {
                calGrid.Children.Add(new TextBlock
                {
                    Text = days[i],
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = (i >= 5)
                        ? new SolidColorBrush(Color.FromArgb(180, 255, 105, 97))
                        : new SolidColorBrush(Color.FromArgb(150, 235, 235, 245)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 6)
                });
            }

            DateTime now = DateTime.Now;
            DateTime firstDay = new DateTime(now.Year, now.Month, 1);
            int offset = ((int)firstDay.DayOfWeek + 6) % 7;
            int daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);

            for (int b = 0; b < offset; b++)
            {
                calGrid.Children.Add(new Border { Width = 28, Height = 28 });
            }

            for (int d = 1; d <= daysInMonth; d++)
            {
                bool isToday = (d == now.Day);
                Border dayPill = new Border
                {
                    Width = 28,
                    Height = 28,
                    CornerRadius = new CornerRadius(14),
                    Background = isToday ? new SolidColorBrush(Color.FromRgb(255, 69, 58)) : Brushes.Transparent,
                    Margin = new Thickness(1)
                };
                dayPill.Child = new TextBlock
                {
                    Text = d.ToString(),
                    FontSize = 12,
                    FontWeight = isToday ? FontWeights.Bold : FontWeights.Medium,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                calGrid.Children.Add(dayPill);
            }

            calCard.Child = calGrid;
            root.Children.Add(calCard);

            card.Child = root;
            Content = card;
        }

        public void RefreshState()
        {
            CultureInfo ru = new CultureInfo("ru-RU");
            _bigTime.Text = DateTime.Now.ToString("HH:mm");
            _bigDate.Text = DateTime.Now.ToString("dddd, d MMMM yyyy г.", ru);
        }
    }

    // =========================================================================
    // 10. ABOUT SYSTEM WINDOW (О системе — Динамическое определение железа ПК)
    // =========================================================================
}
