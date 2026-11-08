using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using MacOSTahoe.Core;
using MacOSTahoe.Native;
using MacOSTahoe.Theme;

namespace MacOSTahoe.Views
{
    public class DesktopWidgetsWindow : Window
    {
        private TextBlock _txtDisk;
        private TextBlock _txtRam;
        private TextBox _txtNote;
        private DispatcherTimer _telemetryTimer;

        public DesktopWidgetsWindow()
        {
            Title = "DesktopWidgetsWindow";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;

            Left = SystemParameters.PrimaryScreenWidth - 280;
            Top = 50;
            Width = 260;
            Height = 360;

            BuildUi();

            SourceInitialized += (s, e) =>
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                DwmBlurHelper.ApplyBlur(hwnd, true);
                DwmBlurHelper.MakeToolWindow(hwnd);
                SendToBottom(hwnd);
            };

            Loaded += (s, e) =>
            {
                UpdateStats();
                _telemetryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                _telemetryTimer.Tick += (st, ev) => UpdateStats();
                _telemetryTimer.Start();
            };
        }

        private void SendToBottom(IntPtr hwnd)
        {
            Win32Api.SetWindowPos(hwnd, NativeConstants.HWND_BOTTOM, 0, 0, 0, 0,
                NativeConstants.SWP_NOMOVE | NativeConstants.SWP_NOSIZE | NativeConstants.SWP_NOACTIVATE);
        }

        private void BuildUi()
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical };

            // Telemetry Card
            var statsCard = new Border
            {
                Background = LiquidGlassTheme.CreateGlassBrush(110, 160),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 14)
            };

            var statsStack = new StackPanel();
            var titleStats = new TextBlock
            {
                Text = "Статус системы",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8)
            };
            statsStack.Children.Add(titleStats);

            _txtRam = new TextBlock
            {
                Text = "Память: RAM --%",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            statsStack.Children.Add(_txtRam);

            _txtDisk = new TextBlock
            {
                Text = "Диск C: --",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255))
            };
            statsStack.Children.Add(_txtDisk);

            statsCard.Child = statsStack;
            stack.Children.Add(statsCard);

            // Quick Note Card
            var noteCard = new Border
            {
                Background = LiquidGlassTheme.CreateGlassBrush(110, 160),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(12)
            };

            var noteStack = new StackPanel();
            var titleNote = new TextBlock
            {
                Text = "Быстрые заметки",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8)
            };
            noteStack.Children.Add(titleNote);

            _txtNote = new TextBox
            {
                Text = ShellController.Instance.Config.QuickNoteText ?? "",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 11,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                Height = 120,
                CaretBrush = Brushes.White
            };
            _txtNote.LostFocus += (s, e) =>
            {
                ShellController.Instance.Config.QuickNoteText = _txtNote.Text;
                ShellController.Instance.SaveConfig();
            };
            noteStack.Children.Add(_txtNote);

            noteCard.Child = noteStack;
            stack.Children.Add(noteCard);

            Content = stack;
        }

        private void UpdateStats()
        {
            try
            {
                var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)) };
                if (Win32Api.GlobalMemoryStatusEx(ref mem))
                {
                    _txtRam.Text = string.Format("Память: {0}% занято ({1:F1} ГБ своб.)", mem.dwMemoryLoad, mem.ullAvailPhys / (1024.0 * 1024.0 * 1024.0));
                }

                DriveInfo d = new DriveInfo("C");
                if (d.IsReady)
                {
                    double freeGb = d.TotalFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    double totalGb = d.TotalSize / (1024.0 * 1024.0 * 1024.0);
                    _txtDisk.Text = string.Format("Диск C: {0:F0} ГБ из {1:F0} ГБ свободно", freeGb, totalGb);
                }
            }
            catch { }
        }
    }
}
