using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MacOSTahoe.Core;
using MacOSTahoe.Native;
using MacOSTahoe.Theme;
using Microsoft.Win32;

namespace MacOSTahoe.Views
{
    public class AboutSystemWindow : Window
    {
        public AboutSystemWindow()
        {
            Title = "Об этом компьютере";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;

            Width = 500;
            Height = 310;
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2.0;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2.5;

            BuildUi();

            SourceInitialized += (s, e) =>
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                DwmBlurHelper.ApplyBlur(hwnd, true);
                DwmBlurHelper.MakeToolWindow(hwnd);
            };

            MouseLeftButtonDown += (s, e) => DragMove();
        }

        private void BuildUi()
        {
            var glass = new Border
            {
                Background = LiquidGlassTheme.CreateGlassBrush(200, 240),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(24)
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Close button
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

            // Top bar with close circle
            var topBar = new Grid();
            var closeBtn = new Border
            {
                Width = 12,
                Height = 12,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromRgb(255, 95, 87)),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            closeBtn.MouseLeftButtonUp += (s, e) => Close();
            topBar.Children.Add(closeBtn);
            Grid.SetRow(topBar, 0);
            mainGrid.Children.Add(topBar);

            // Body
            var bodyGrid = new Grid { Margin = new Thickness(0, 14, 0, 14) };
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Logo
            var logoBorder = new Border
            {
                Width = 100,
                Height = 100,
                CornerRadius = new CornerRadius(50),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var logoTxt = new TextBlock
            {
                Text = "",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 54,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            logoBorder.Child = logoTxt;
            Grid.SetColumn(logoBorder, 0);
            bodyGrid.Children.Add(logoBorder);

            // Specs
            var specsStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var osTitle = new TextBlock
            {
                Text = "Windows 11 Tahoe Edition",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            };
            specsStack.Children.Add(osTitle);

            var osVer = new TextBlock
            {
                Text = "Версия 24H2 (Сборка 26100.3194)",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                Margin = new Thickness(0, 0, 0, 12)
            };
            specsStack.Children.Add(osVer);

            string cpuName = GetProcessorName();
            string ramText = GetTotalRamText();
            string screenText = string.Format("{0} × {1}", (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);

            specsStack.Children.Add(CreateSpecRow("Процессор", cpuName));
            specsStack.Children.Add(CreateSpecRow("Память", ramText));
            specsStack.Children.Add(CreateSpecRow("Дисплей", screenText));

            Grid.SetColumn(specsStack, 1);
            bodyGrid.Children.Add(specsStack);

            Grid.SetRow(bodyGrid, 1);
            mainGrid.Children.Add(bodyGrid);

            // Footer button
            var btnReport = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 6, 14, 6),
                HorizontalAlignment = HorizontalAlignment.Right,
                Cursor = Cursors.Hand
            };
            var txtReport = new TextBlock
            {
                Text = "Сведения о системе...",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 11,
                Foreground = Brushes.White
            };
            btnReport.Child = txtReport;
            btnReport.MouseLeftButtonUp += (s, e) =>
            {
                try { Process.Start("msinfo32.exe"); } catch { }
            };

            Grid.SetRow(btnReport, 2);
            mainGrid.Children.Add(btnReport);

            glass.Child = mainGrid;
            Content = glass;
        }

        private Grid CreateSpecRow(string label, string val)
        {
            var g = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock
            {
                Text = label,
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255))
            };
            Grid.SetColumn(lbl, 0);
            g.Children.Add(lbl);

            var v = new TextBlock
            {
                Text = val,
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 11,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(v, 1);
            g.Children.Add(v);

            return g;
        }

        private string GetProcessorName()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                {
                    if (key != null)
                    {
                        var name = key.GetValue("ProcessorNameString") as string;
                        if (!string.IsNullOrEmpty(name)) return name.Trim();
                    }
                }
            }
            catch { }
            return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "AMD Ryzen / Intel Core";
        }

        private string GetTotalRamText()
        {
            try
            {
                var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)) };
                if (Win32Api.GlobalMemoryStatusEx(ref mem))
                {
                    double gb = mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                    return string.Format("{0:F0} ГБ", Math.Round(gb));
                }
            }
            catch { }
            return "16 ГБ";
        }
    }
}
