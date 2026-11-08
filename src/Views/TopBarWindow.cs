using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using MacOSTahoe.Core;
using MacOSTahoe.Native;
using MacOSTahoe.Services;
using MacOSTahoe.Theme;

namespace MacOSTahoe.Views
{
    public class TopBarWindow : Window
    {
        private TextBlock _txtActiveApp;
        private TextBlock _txtClock;
        private TextBlock _txtLang;
        private TextBlock _txtMem;
        private DispatcherTimer _clockTimer;

        public TopBarWindow()
        {
            Title = "TopBarWindow";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;

            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = 32;

            BuildUi();

            SourceInitialized += (s, e) =>
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                DwmBlurHelper.ApplyBlur(hwnd, ShellController.Instance.Config.IsDarkMode);
                DwmBlurHelper.MakeToolWindow(hwnd);
            };

            Loaded += (s, e) =>
            {
                StartClock();
            };
        }

        private void BuildUi()
        {
            var root = new Border
            {
                Background = LiquidGlassTheme.CreateGlassBrush(140, 190),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Height = 32
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Left menu
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Spacer
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Right tray

            // --- Left Section ---
            var leftStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0)
            };

            // Tahoe / Apple button
            var logoBtn = CreateTopBarButton("", 16, true);
            logoBtn.Click += (s, e) => ShowSystemMenu(logoBtn);
            leftStack.Children.Add(logoBtn);

            // Active App Title
            _txtActiveApp = new TextBlock
            {
                Text = "Tahoe",
                FontFamily = LiquidGlassTheme.UiFont,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 0, 10, 0)
            };
            leftStack.Children.Add(_txtActiveApp);

            // Menu Items
            string[] menus = new[] { "Файл", "Правка", "Вид", "Окно", "Справка" };
            foreach (var m in menus)
            {
                var menuBtn = CreateTopBarButton(m, 13, false);
                menuBtn.Click += (s, e) =>
                {
                    if (m == "Файл") ShellController.Instance.LaunchApp("explorer.exe", "");
                    else if (m == "Окно") ShellController.Instance.ToggleWindowSwitcher();
                    else if (m == "Справка") ShellController.Instance.OpenAbout();
                };
                leftStack.Children.Add(menuBtn);
            }

            Grid.SetColumn(leftStack, 0);
            grid.Children.Add(leftStack);

            // --- Right Section ---
            var rightStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 14, 0)
            };

            // RAM indicator
            _txtMem = new TextBlock
            {
                Text = "RAM --%",
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            _txtMem.MouseLeftButtonUp += (s, e) => ShellController.Instance.LaunchApp("taskmgr.exe", "");
            rightStack.Children.Add(_txtMem);

            // Keyboard Layout
            _txtLang = new TextBlock
            {
                Text = "RU",
                FontFamily = LiquidGlassTheme.UiFont,
                FontWeight = FontWeights.Medium,
                FontSize = 12,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            _txtLang.MouseLeftButtonUp += (s, e) => ToggleKeyboardLayout();
            rightStack.Children.Add(_txtLang);

            // Spotlight search icon button
            var spotBtn = CreateTopBarButton("🔍", 12, false);
            spotBtn.ToolTip = "Spotlight (Alt+Space)";
            spotBtn.Click += (s, e) => ShellController.Instance.ToggleSpotlight();
            rightStack.Children.Add(spotBtn);

            // Control Center button
            var ccBtn = CreateTopBarButton("⊞", 14, false);
            ccBtn.ToolTip = "Пункт управления";
            ccBtn.Click += (s, e) => ShellController.Instance.ToggleControlCenter();
            rightStack.Children.Add(ccBtn);

            // Clock / Date
            _txtClock = new TextBlock
            {
                Text = DateTime.Now.ToString("ddd d MMM  HH:mm"),
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 13,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            _txtClock.MouseLeftButtonUp += (s, e) => ShellController.Instance.ToggleCalendar();
            rightStack.Children.Add(_txtClock);

            Grid.SetColumn(rightStack, 2);
            grid.Children.Add(rightStack);

            root.Child = grid;
            Content = root;
        }

        private Button CreateTopBarButton(string text, double fontSize, bool bold)
        {
            var btn = new Button
            {
                Content = text,
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = fontSize,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6, 2, 6, 2),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };

            var template = new ControlTemplate(typeof(Button));
            var elemFactory = new FrameworkElementFactory(typeof(Border));
            elemFactory.Name = "border";
            elemFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            elemFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            elemFactory.AppendChild(contentPresenter);

            var trigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), "border"));

            template.VisualTree = elemFactory;
            template.Triggers.Add(trigger);
            btn.Template = template;

            return btn;
        }

        private void StartClock()
        {
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) =>
            {
                _txtClock.Text = DateTime.Now.ToString("ddd d MMM  HH:mm");
                UpdateMemoryUsage();
                UpdateActiveAppAndLayout();
            };
            _clockTimer.Start();
        }

        private void UpdateMemoryUsage()
        {
            try
            {
                var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)) };
                if (Win32Api.GlobalMemoryStatusEx(ref mem))
                {
                    _txtMem.Text = string.Format("RAM {0}%", mem.dwMemoryLoad);
                }
            }
            catch { }
        }

        private void UpdateActiveAppAndLayout()
        {
            try
            {
                IntPtr fg = Win32Api.GetForegroundWindow();
                if (fg != IntPtr.Zero)
                {
                    uint pid = 0;
                    uint tid = Win32Api.GetWindowThreadProcessId(fg, out pid);

                    // Language layout
                    if (tid != 0)
                    {
                        IntPtr layout = Win32Api.GetKeyboardLayout(tid);
                        int langId = (int)((long)layout & 0xFFFF);
                        if (langId == 0x0419) _txtLang.Text = "RU";
                        else if (langId == 0x0409) _txtLang.Text = "EN";
                        else _txtLang.Text = (langId != 0) ? "EN" : "RU";
                    }

                    // Active title
                    string pName;
                    WindowTrackerService.GetProcessPath(pid, out pName);
                    if (!string.IsNullOrEmpty(pName))
                    {
                        if (pName.Equals("explorer", StringComparison.OrdinalIgnoreCase)) _txtActiveApp.Text = "Проводник";
                        else if (pName.Equals("chrome", StringComparison.OrdinalIgnoreCase)) _txtActiveApp.Text = "Chrome";
                        else _txtActiveApp.Text = pName;
                    }
                }
            }
            catch { }
        }

        private void ToggleKeyboardLayout()
        {
            try
            {
                IntPtr fg = Win32Api.GetForegroundWindow();
                if (fg != IntPtr.Zero)
                {
                    // Send WM_INPUTLANGCHANGEREQUEST
                    Win32Api.PostMessage(fg, NativeConstants.WM_INPUTLANGCHANGEREQUEST, (IntPtr)0x0002, IntPtr.Zero);
                }
            }
            catch { }
        }

        private void ShowSystemMenu(UIElement target)
        {
            ContextMenu cm = new ContextMenu();
            cm.PlacementTarget = target;
            cm.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;

            var miAbout = new MenuItem { Header = "Об этом компьютере" };
            miAbout.Click += (s, e) => ShellController.Instance.OpenAbout();
            cm.Items.Add(miAbout);

            cm.Items.Add(new Separator());

            var miSettings = new MenuItem { Header = "Системные параметры..." };
            miSettings.Click += (s, e) => ShellController.Instance.LaunchApp("ms-settings:", "");
            cm.Items.Add(miSettings);

            var miTaskMgr = new MenuItem { Header = "Диспетчер задач" };
            miTaskMgr.Click += (s, e) => ShellController.Instance.LaunchApp("taskmgr.exe", "");
            cm.Items.Add(miTaskMgr);

            cm.Items.Add(new Separator());

            var miTahoeSettings = new MenuItem { Header = "Настройки Tahoe Launcher..." };
            miTahoeSettings.Click += (s, e) => ShellController.Instance.OpenSettings();
            cm.Items.Add(miTahoeSettings);

            cm.Items.Add(new Separator());

            var miLock = new MenuItem { Header = "Заблокировать экран" };
            miLock.Click += (s, e) => Win32Api.LockWorkStation();
            cm.Items.Add(miLock);

            var miRestart = new MenuItem { Header = "Перезагрузить..." };
            miRestart.Click += (s, e) => Process.Start("shutdown.exe", "/r /t 0");
            cm.Items.Add(miRestart);

            var miShutdown = new MenuItem { Header = "Выключить..." };
            miShutdown.Click += (s, e) => Process.Start("shutdown.exe", "/s /t 0");
            cm.Items.Add(miShutdown);

            cm.Items.Add(new Separator());

            var miExit = new MenuItem { Header = "Выход из Tahoe Launcher" };
            miExit.Click += (s, e) => ShellController.Instance.ShutdownLauncher();
            cm.Items.Add(miExit);

            cm.IsOpen = true;
        }
    }
}
