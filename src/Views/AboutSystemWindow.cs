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
    public class AboutSystemWindow : Window
    {
        public AboutSystemWindow()
        {
            Title = "О системе";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Width = 360;
            Height = 430;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();
        }

        private static string DetectCpuName()
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"HARDWARE\Description\System\CentralProcessor\0");
                if (rk != null)
                {
                    object val = rk.GetValue("ProcessorNameString");
                    rk.Close();
                    if (val != null) return val.ToString().Trim();
                }
            }
            catch { }
            return string.Format("Процессор ({0} потоков)", Environment.ProcessorCount);
        }

        private static string DetectGpuName()
        {
            try
            {
                RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
                if (baseKey != null)
                {
                    string[] subs = baseKey.GetSubKeyNames();
                    for (int i = 0; i < subs.Length; i++)
                    {
                        if (!subs[i].StartsWith("0")) continue;
                        RegistryKey sub = baseKey.OpenSubKey(subs[i]);
                        if (sub != null)
                        {
                            object desc = sub.GetValue("DriverDesc");
                            sub.Close();
                            if (desc != null && !string.IsNullOrEmpty(desc.ToString()))
                                return desc.ToString().Trim();
                        }
                    }
                    baseKey.Close();
                }
            }
            catch { }
            return "DirectX 12 GPU";
        }

        private static string DetectRamSize()
        {
            try
            {
                NativeMethods.MEMORYSTATUSEX mem = new NativeMethods.MEMORYSTATUSEX();
                mem.dwLength = (uint)Marshal.SizeOf(mem);
                if (NativeMethods.GlobalMemoryStatusEx(ref mem))
                {
                    double gb = Math.Round(mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0));
                    return string.Format("{0:0} ГБ ОЗУ", gb);
                }
            }
            catch { }
            return "16 ГБ ОЗУ";
        }

        private void BuildUI()
        {
            Border card = new Border
            {
                Margin = new Thickness(14),
                CornerRadius = new CornerRadius(22),
                Background = LiquidGlassTheme.CreateGlassBrush(235, 248),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(20, 14, 20, 20),
                Effect = new DropShadowEffect { BlurRadius = 30, ShadowDepth = 10, Opacity = 0.65, Color = Colors.Black }
            };

            card.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ButtonState == MouseButtonState.Pressed) DragMove();
            };

            StackPanel root = new StackPanel();

            StackPanel traffic = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
            Ellipse closeBtn = new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Color.FromRgb(255, 95, 87)), Margin = new Thickness(0, 0, 8, 0), Cursor = Cursors.Hand };
            closeBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; Close(); };
            Ellipse minBtn = new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Color.FromRgb(254, 188, 46)), Margin = new Thickness(0, 0, 8, 0) };
            Ellipse maxBtn = new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Color.FromRgb(40, 200, 64)) };
            traffic.Children.Add(closeBtn);
            traffic.Children.Add(minBtn);
            traffic.Children.Add(maxBtn);
            root.Children.Add(traffic);

            Image img = new Image
            {
                Source = ShellController.Instance.GetIcon("wallpaper.png"),
                Width = 82,
                Height = 82,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
            root.Children.Add(img);

            root.Children.Add(new TextBlock
            {
                Text = Environment.MachineName,
                FontSize = 21,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            root.Children.Add(new TextBlock
            {
                Text = "Windows 11 • Tahoe Liquid Glass v26.2",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromArgb(165, 235, 235, 245)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 16)
            });

            root.Children.Add(CreateSpecRow("Процессор", DetectCpuName()));
            root.Children.Add(CreateSpecRow("Графика", DetectGpuName()));
            root.Children.Add(CreateSpecRow("Память", DetectRamSize()));
            root.Children.Add(CreateSpecRow("Каталог", string.Format("{0} приложений", ShellController.Instance.InstalledApps.Count)));

            StackPanel btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };

            Border settingsBtn = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = LiquidGlassTheme.AccentBlueBrush,
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            settingsBtn.Child = new TextBlock { Text = "Настройки лаунчера", Foreground = Brushes.White, FontSize = 12, FontWeight = FontWeights.SemiBold };
            settingsBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                Close();
                ShellController.Instance.ShowLauncherSettings();
            };
            btnRow.Children.Add(settingsBtn);

            Border moreBtn = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(55, 255, 255, 255)),
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = Cursors.Hand
            };
            moreBtn.Child = new TextBlock { Text = "Параметры ОС...", Foreground = Brushes.White, FontSize = 12, FontWeight = FontWeights.Medium };
            moreBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                ShellController.RunProcess("explorer.exe", "ms-settings:about");
                Close();
            };
            btnRow.Children.Add(moreBtn);

            root.Children.Add(btnRow);

            card.Child = root;
            Content = card;
        }

        private FrameworkElement CreateSpecRow(string key, string val)
        {
            Grid g = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock k = new TextBlock
            {
                Text = key,
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromArgb(165, 235, 235, 245)),
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 0, 12, 0)
            };
            Grid.SetColumn(k, 0);
            g.Children.Add(k);

            TextBlock v = new TextBlock
            {
                Text = val,
                FontSize = 11.5,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(v, 1);
            g.Children.Add(v);
            return g;
        }
    }

}
