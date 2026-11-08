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
    public class TopBarWindow : Window
    {
        private TextBlock _activeAppText;
        private TextBlock _clockText;
        private TextBlock _langText;
        private TextBlock _cpuMemText;
        private Popup _activeMenuPopup;
        private bool _appBarRegistered = false;
        private uint _appBarCallbackMsg = 0;
        private DispatcherTimer _pollTimer;
        private static readonly CultureInfo RuCulture = new CultureInfo("ru-RU");
        private uint _selfPid = 0;
        private IntPtr _kbHookHandle = IntPtr.Zero;
        private NativeMethods.LowLevelKeyboardProc _kbHookProc;
        private bool _winKeyDown = false;
        private bool _otherKeyWhileWin = false;

        public TopBarWindow()
        {
            Title = "Tahoe Menu Bar";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = 28;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();

            SourceInitialized += OnSourceInitialized;
            Closed += OnClosed;

            try { _selfPid = (uint)Process.GetCurrentProcess().Id; } catch { }

            _pollTimer = new DispatcherTimer();
            _pollTimer.Interval = TimeSpan.FromMilliseconds(1000); // 1.0s instead of 200ms
            _pollTimer.Tick += OnPollTick;
            _pollTimer.Start();
            OnPollTick(null, EventArgs.Empty);
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            NativeMethods.HideNativeTaskbars();
            NativeMethods.MakeToolWindow(this, true);
            NativeMethods.EnableBlur(this, 0x9916161E);
            RegisterAppBar();
            EnforceWorkArea();

            WindowInteropHelper helper = new WindowInteropHelper(this);
            HwndSource src = HwndSource.FromHwnd(helper.Handle);
            if (src != null) src.AddHook(WndProc);
            // Register Win+Shift+A (9002) for Settings Hub (Alt+Space handled in low-level hook)
            NativeMethods.RegisterHotKey(helper.Handle, 9002, 0x0008 | 0x0004, 0x41);

            _kbHookProc = new NativeMethods.LowLevelKeyboardProc(KeyboardHookCallback);
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _kbHookHandle = NativeMethods.SetWindowsHookEx(13, _kbHookProc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && ShellController.Instance != null)
            {
                int msg = wParam.ToInt32();
                int vkCode = Marshal.ReadInt32(lParam);
                int flags = Marshal.ReadInt32(lParam, 8);
                bool injected = (flags & 0x10) != 0;

                if (!injected)
                {
                    bool isWinKey = (vkCode == 0x5B || vkCode == 0x5C);
                    bool isKeyDown = (msg == 0x0100 || msg == 0x0104);
                    bool isKeyUp = (msg == 0x0101 || msg == 0x0105);

                    if (isWinKey)
                    {
                        if (isKeyDown)
                        {
                            if (!_winKeyDown)
                            {
                                _winKeyDown = true;
                                _otherKeyWhileWin = false;
                            }
                        }
                        else if (isKeyUp)
                        {
                            bool soloTap = (_winKeyDown && !_otherKeyWhileWin);
                            _winKeyDown = false;
                            _otherKeyWhileWin = false;
                            if (soloTap && ShellController.Instance.Config.InterceptWinKey)
                            {
                                NativeMethods.keybd_event(0xFF, 0, 0, UIntPtr.Zero);
                                NativeMethods.keybd_event(0xFF, 0, 2, UIntPtr.Zero);
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    ShellController.Instance.ToggleLaunchpad();
                                }));
                            }
                        }
                    }
                    else if (isKeyUp)
                    {
                        if (_winKeyDown)
                        {
                            _otherKeyWhileWin = true;
                        }

                        if (vkCode == 0x09)
                        {
                            bool isAltDown = ((NativeMethods.GetAsyncKeyState(0x12) & 0x8000) != 0) || ((NativeMethods.GetAsyncKeyState(0xA4) & 0x8000) != 0) || ((NativeMethods.GetAsyncKeyState(0xA5) & 0x8000) != 0);
                            if (isAltDown || (ShellController.Instance.AppSwitcher != null && ShellController.Instance.AppSwitcher.IsVisible))
                            {
                                return (IntPtr)1;
                            }
                        }

                        if (vkCode == 0x12 || vkCode == 0xA4 || vkCode == 0xA5)
                        {
                            if (ShellController.Instance.AppSwitcher != null && ShellController.Instance.AppSwitcher.IsVisible)
                            {
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    ShellController.Instance.CommitAppSwitcher();
                                }));
                                // DO NOT return (IntPtr)1! Let WM_KEYUP reach Windows so Alt key is never stuck!
                            }
                        }
                    }
                    else if (isKeyDown)
                    {
                        if (_winKeyDown)
                        {
                            _otherKeyWhileWin = true;
                        }

                        // Hotkey: Win + Shift + A -> Launcher Settings Hub
                        if (vkCode == 0x41) // 'A'
                        {
                            bool isWinDown = _winKeyDown || ((NativeMethods.GetAsyncKeyState(0x5B) & 0x8000) != 0) || ((NativeMethods.GetAsyncKeyState(0x5C) & 0x8000) != 0);
                            bool isShiftDown = ((NativeMethods.GetAsyncKeyState(0x10) & 0x8000) != 0) || ((NativeMethods.GetAsyncKeyState(0xA0) & 0x8000) != 0) || ((NativeMethods.GetAsyncKeyState(0xA1) & 0x8000) != 0);

                            if (isWinDown && isShiftDown)
                            {
                                _otherKeyWhileWin = true;
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    ShellController.Instance.ShowLauncherSettings();
                                }));
                                return (IntPtr)1;
                            }
                        }

                        // Hotkey: Alt + Space -> Quick Spotlight Search
                        if (vkCode == 0x20) // Space
                        {
                            bool isWinDown = _winKeyDown || ((NativeMethods.GetAsyncKeyState(0x5B) & 0x8000) != 0) || ((NativeMethods.GetAsyncKeyState(0x5C) & 0x8000) != 0);
                            if (isWinDown)
                            {
                                // Win + Space is Windows language switching: NEVER intercept, let it pass to Windows!
                                _otherKeyWhileWin = true;
                            }
                            else
                            {
                                // Must actually have Alt pressed down (WM_SYSKEYDOWN 0x0104 or LLKHF_ALTDOWN 0x20)
                                bool altFlag = (msg == 0x0104) || ((flags & 0x20) != 0);
                                bool isAltDown = altFlag && (((NativeMethods.GetAsyncKeyState(0x12) & 0x8000) != 0) || ((NativeMethods.GetAsyncKeyState(0xA4) & 0x8000) != 0));
                                bool isCtrlDown = ((NativeMethods.GetAsyncKeyState(0x11) & 0x8000) != 0);

                                if (isAltDown && !isCtrlDown)
                                {
                                    Dispatcher.BeginInvoke(new Action(delegate
                                    {
                                        ShellController.Instance.ToggleSpotlight();
                                    }));
                                    return (IntPtr)1;
                                }
                            }
                        }

                        // Hotkey: F3 -> Mission Control
                        if (vkCode == 0x72)
                        {
                            Dispatcher.BeginInvoke(new Action(delegate
                            {
                                ShellController.Instance.ToggleWindowSwitcher();
                            }));
                            return (IntPtr)1;
                        }

                        // Hotkey: Ctrl + Up -> Mission Control (macOS Standard)
                        if (vkCode == 0x26)
                        {
                            bool isCtrlDown = ((NativeMethods.GetAsyncKeyState(0x11) & 0x8000) != 0);
                            bool isWinDown = _winKeyDown || ((NativeMethods.GetAsyncKeyState(0x5B) & 0x8000) != 0);
                            if (isCtrlDown && !isWinDown)
                            {
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    ShellController.Instance.ToggleWindowSwitcher();
                                }));
                                return (IntPtr)1;
                            }
                        }

                        // Hotkey: Ctrl + Down -> App Exposé (macOS Standard)
                        if (vkCode == 0x28)
                        {
                            bool isCtrlDown = ((NativeMethods.GetAsyncKeyState(0x11) & 0x8000) != 0);
                            if (isCtrlDown)
                            {
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    ShellController.Instance.ToggleAppExpose();
                                }));
                                return (IntPtr)1;
                            }
                        }

                        // Hotkey: Ctrl + Left / Right -> Switch Spaces
                        if (vkCode == 0x25 || vkCode == 0x27)
                        {
                            bool isCtrlDown = ((NativeMethods.GetAsyncKeyState(0x11) & 0x8000) != 0);
                            bool isWinDown = _winKeyDown || ((NativeMethods.GetAsyncKeyState(0x5B) & 0x8000) != 0);
                            if (isCtrlDown && !isWinDown)
                            {
                                int diff = (vkCode == 0x27) ? 1 : -1;
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    int target = ShellController.Instance.Config.CurrentSpaceIndex + diff;
                                    ShellController.Instance.SwitchToSpace(target);
                                }));
                                return (IntPtr)1;
                            }
                        }

                        // Hotkey: Alt + Tab -> macOS Command+Tab App Switcher HUD
                        if (vkCode == 0x09)
                        {
                            bool isAltDown = ((NativeMethods.GetAsyncKeyState(0x12) & 0x8000) != 0) || ((NativeMethods.GetAsyncKeyState(0xA4) & 0x8000) != 0) || ((NativeMethods.GetAsyncKeyState(0xA5) & 0x8000) != 0);
                            if (isAltDown)
                            {
                                bool isShiftDown = ((NativeMethods.GetAsyncKeyState(0x10) & 0x8000) != 0) || ((NativeMethods.GetAsyncKeyState(0xA0) & 0x8000) != 0) || ((NativeMethods.GetAsyncKeyState(0xA1) & 0x8000) != 0);
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    ShellController.Instance.ShowAppSwitcher(isShiftDown);
                                }));
                                return (IntPtr)1;
                            }
                        }

                        // Hotkey: Alt + ~ (Tilde/Ё) -> step backwards in App Switcher HUD
                        if (vkCode == 0xC0)
                        {
                            bool isAltDown = ((NativeMethods.GetAsyncKeyState(0x12) & 0x8000) != 0) || ((NativeMethods.GetAsyncKeyState(0xA4) & 0x8000) != 0) || ((NativeMethods.GetAsyncKeyState(0xA5) & 0x8000) != 0);
                            if (isAltDown)
                            {
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    ShellController.Instance.ShowAppSwitcher(true);
                                }));
                                return (IntPtr)1;
                            }
                        }

                        // When App Switcher HUD is open: Q quits, H hides, Esc cancels
                        if (ShellController.Instance.AppSwitcher != null && ShellController.Instance.AppSwitcher.IsVisible)
                        {
                            if (vkCode == 0x25) // Left arrow -> step back
                            {
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    ShellController.Instance.ShowAppSwitcher(true);
                                }));
                                return (IntPtr)1;
                            }
                            if (vkCode == 0x27) // Right arrow -> step forward
                            {
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    ShellController.Instance.ShowAppSwitcher(false);
                                }));
                                return (IntPtr)1;
                            }
                            if (vkCode == 0x51) // 'Q'
                            {
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    ShellController.Instance.AppSwitcherQuit();
                                }));
                                return (IntPtr)1;
                            }
                            if (vkCode == 0x48) // 'H'
                            {
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    ShellController.Instance.AppSwitcherHide();
                                }));
                                return (IntPtr)1;
                            }
                            if (vkCode == 0x1B) // Esc
                            {
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    ShellController.Instance.CancelAppSwitcher();
                                }));
                                return (IntPtr)1;
                            }
                        }
                    }
                }
            }
            return NativeMethods.CallNextHookEx(_kbHookHandle, nCode, wParam, lParam);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312)
            {
                int id = wParam.ToInt32();
                if (id == 9002)
                {
                    ShellController.Instance.ShowLauncherSettings();
                    handled = true;
                }
            }
            else if (_appBarCallbackMsg != 0 && (uint)msg == _appBarCallbackMsg)
            {
                if (wParam.ToInt32() == 1)
                {
                    EnforceWorkArea();
                }
            }
            return IntPtr.Zero;
        }

        private void RegisterAppBar()
        {
            try
            {
                WindowInteropHelper helper = new WindowInteropHelper(this);
                if (helper.Handle == IntPtr.Zero) return;
                if (_appBarCallbackMsg == 0)
                {
                    _appBarCallbackMsg = NativeMethods.RegisterWindowMessage("TahoeAppBarCallbackMsg");
                }
                NativeMethods.APPBARDATA abd = new NativeMethods.APPBARDATA();
                abd.cbSize = Marshal.SizeOf(abd);
                abd.hWnd = helper.Handle;
                abd.uCallbackMessage = _appBarCallbackMsg;
                NativeMethods.SHAppBarMessage(NativeMethods.ABM_NEW, ref abd);
                _appBarRegistered = true;
            }
            catch { }
        }

        public void EnforceWorkAreaPublic()
        {
            EnforceWorkArea();
        }

        private void EnforceWorkArea()
        {
            try
            {
                NativeMethods.HideNativeTaskbars();
                WindowInteropHelper helper = new WindowInteropHelper(this);
                if (helper.Handle == IntPtr.Zero) return;

                PresentationSource source = PresentationSource.FromVisual(this);
                double dpiScale = 1.0;
                if (source != null && source.CompositionTarget != null)
                {
                    dpiScale = source.CompositionTarget.TransformToDevice.M11;
                }
                int pixelWidth = (int)Math.Round(SystemParameters.PrimaryScreenWidth * dpiScale);
                int screenPixelHeight = (int)Math.Round(SystemParameters.PrimaryScreenHeight * dpiScale);
                bool showBar = ShellController.Instance == null || ShellController.Instance.Config.ShowTopBar;
                int barPixelHeight = showBar ? (int)Math.Round(28 * dpiScale) : 0;

                if (_appBarRegistered)
                {
                    NativeMethods.APPBARDATA abd = new NativeMethods.APPBARDATA();
                    abd.cbSize = Marshal.SizeOf(abd);
                    abd.hWnd = helper.Handle;
                    abd.uEdge = NativeMethods.ABE_TOP;
                    abd.rc.Left = 0;
                    abd.rc.Top = 0;
                    abd.rc.Right = pixelWidth;
                    abd.rc.Bottom = barPixelHeight;
                    NativeMethods.SHAppBarMessage(NativeMethods.ABM_QUERYPOS, ref abd);
                    abd.rc.Left = 0;
                    abd.rc.Top = 0;
                    abd.rc.Right = pixelWidth;
                    abd.rc.Bottom = barPixelHeight;
                    NativeMethods.SHAppBarMessage(NativeMethods.ABM_SETPOS, ref abd);
                }

                NativeMethods.RECT workRect = new NativeMethods.RECT();
                workRect.Left = 0;
                workRect.Top = barPixelHeight;
                workRect.Right = pixelWidth;
                workRect.Bottom = screenPixelHeight;
                NativeMethods.RECT curRect = new NativeMethods.RECT();
                if (NativeMethods.SystemParametersInfoRect(0x0030, 0, ref curRect, 0))
                {
                    if (curRect.Top != workRect.Top || curRect.Bottom != workRect.Bottom || curRect.Left != workRect.Left || curRect.Right != workRect.Right)
                    {
                        NativeMethods.SystemParametersInfoRect(0x002F, 0, ref workRect, 0);
                    }
                }
                else
                {
                    NativeMethods.SystemParametersInfoRect(0x002F, 0, ref workRect, 0);
                }
                NativeMethods.HideNativeTaskbars();
            }
            catch { }
        }

        public void UnregisterAndRestoreWorkArea()
        {
            try
            {
                if (_kbHookHandle != IntPtr.Zero)
                {
                    NativeMethods.UnhookWindowsHookEx(_kbHookHandle);
                    _kbHookHandle = IntPtr.Zero;
                }
                WindowInteropHelper helper = new WindowInteropHelper(this);
                NativeMethods.UnregisterHotKey(helper.Handle, 9001);
                NativeMethods.UnregisterHotKey(helper.Handle, 9002);
                if (_appBarRegistered)
                {
                    NativeMethods.APPBARDATA abd = new NativeMethods.APPBARDATA();
                    abd.cbSize = Marshal.SizeOf(abd);
                    abd.hWnd = helper.Handle;
                    NativeMethods.SHAppBarMessage(NativeMethods.ABM_REMOVE, ref abd);
                    _appBarRegistered = false;
                }
            }
            catch { }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            UnregisterAndRestoreWorkArea();
            NativeMethods.RestoreNativeTaskbars();
        }

        private void BuildUI()
        {
            Border barBorder = new Border();
            barBorder.Background = LiquidGlassTheme.CreateGlassBrush(180, 200);
            barBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));
            barBorder.BorderThickness = new Thickness(0, 0, 0, 1);
            barBorder.Padding = new Thickness(8, 0, 10, 0);

            Grid rootGrid = new Grid();
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel leftStack = new StackPanel();
            leftStack.Orientation = Orientation.Horizontal;
            leftStack.VerticalAlignment = VerticalAlignment.Center;

            Border sysMenuBtn = CreateBarButton(CreateAppleLogoPath(), delegate(object s, MouseButtonEventArgs e)
            {
                ShowMenuDropdown((FrameworkElement)s, BuildSystemMenuItems());
            });
            sysMenuBtn.Padding = new Thickness(9, 3, 9, 3);
            leftStack.Children.Add(sysMenuBtn);

            _activeAppText = new TextBlock();
            _activeAppText.Text = "Рабочий стол";
            _activeAppText.FontWeight = FontWeights.Bold;
            _activeAppText.FontSize = 13;
            _activeAppText.Foreground = Brushes.White;
            _activeAppText.VerticalAlignment = VerticalAlignment.Center;
            Border appBtn = CreateBarButton(_activeAppText, delegate(object s, MouseButtonEventArgs e)
            {
                ShowMenuDropdown((FrameworkElement)s, BuildActiveAppMenuItems());
            });
            leftStack.Children.Add(appBtn);

            leftStack.Children.Add(CreateTextMenuButton("Файл", BuildFileMenuItems));
            leftStack.Children.Add(CreateTextMenuButton("Правка", BuildEditMenuItems));
            leftStack.Children.Add(CreateTextMenuButton("Вид", BuildViewMenuItems));
            leftStack.Children.Add(CreateTextMenuButton("Переход", BuildGoMenuItems));
            leftStack.Children.Add(CreateTextMenuButton("Окно", BuildWindowMenuItems));
            leftStack.Children.Add(CreateTextMenuButton("Справка", BuildHelpMenuItems));

            Grid.SetColumn(leftStack, 0);
            rootGrid.Children.Add(leftStack);

            StackPanel rightStack = new StackPanel();
            rightStack.Orientation = Orientation.Horizontal;
            rightStack.VerticalAlignment = VerticalAlignment.Center;

            _cpuMemText = new TextBlock();
            _cpuMemText.Text = "ОЗУ 42%";
            _cpuMemText.FontSize = 11.5;
            _cpuMemText.FontWeight = FontWeights.Medium;
            _cpuMemText.Foreground = new SolidColorBrush(Color.FromArgb(220, 245, 245, 247));
            _cpuMemText.VerticalAlignment = VerticalAlignment.Center;
            Border sysBtn = CreateBarButton(_cpuMemText, delegate(object s, MouseButtonEventArgs e)
            {
                ShellController.RunProcess("taskmgr.exe", "");
            });
            sysBtn.ToolTip = "Открыть Диспетчер задач";
            rightStack.Children.Add(sysBtn);

            Border langBadge = new Border();
            langBadge.BorderBrush = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255));
            langBadge.BorderThickness = new Thickness(1);
            langBadge.CornerRadius = new CornerRadius(3.5);
            langBadge.Padding = new Thickness(4, 0.5, 4, 0.5);
            langBadge.VerticalAlignment = VerticalAlignment.Center;
            _langText = new TextBlock();
            _langText.Text = "RU";
            _langText.FontSize = 10.5;
            _langText.FontWeight = FontWeights.Bold;
            _langText.Foreground = Brushes.White;
            langBadge.Child = _langText;

            Border langBtn = CreateBarButton(langBadge, delegate(object s, MouseButtonEventArgs e)
            {
                SwitchKeyboardLayout();
            });
            langBtn.ToolTip = "Переключить раскладку клавиатуры";
            rightStack.Children.Add(langBtn);

            Border wifiBtn = CreateBarButton(CreateVectorIcon("M1,6 C5.5,2 12.5,2 17,6 M3.8,9.2 C7,6.4 11,6.4 14.2,9.2 M6.8,12.2 C8.2,11 9.8,11 11.2,12.2 M9,15 A1,1 0 1,1 9.01,15", 16, 15, true),
                delegate(object s, MouseButtonEventArgs e)
                {
                    ShellController.Instance.ToggleControlCenter();
                });
            wifiBtn.ToolTip = "Сеть и Интернет";
            rightStack.Children.Add(wifiBtn);

            Border soundBtn = CreateBarButton(CreateVectorIcon("M2,6 L5,6 L9,2.5 L9,13.5 L5,10 L2,10 Z M11.5,5 C12.8,6.5 12.8,9.5 11.5,11 M13.8,3 C16,5.8 16,10.2 13.8,13", 16, 15, true),
                delegate(object s, MouseButtonEventArgs e)
                {
                    ShellController.Instance.ToggleControlCenter();
                });
            soundBtn.ToolTip = "Звук и мультимедиа";
            rightStack.Children.Add(soundBtn);

            Border searchBtn = CreateBarButton(CreateVectorIcon("M10.5,6.5 A4.5,4.5 0 1,1 1.5,6.5 A4.5,4.5 0 1,1 10.5,6.5 M9.8,9.8 L14,14", 15, 15, true),
                delegate(object s, MouseButtonEventArgs e)
                {
                    ShellController.Instance.ToggleSpotlight();
                });
            searchBtn.ToolTip = "Быстрый поиск (Alt+Пробел)";
            rightStack.Children.Add(searchBtn);

            Border ccBtn = CreateBarButton(CreateControlCenterIcon(),
                delegate(object s, MouseButtonEventArgs e)
                {
                    ShellController.Instance.ToggleControlCenter();
                });
            ccBtn.ToolTip = "Пункт управления";
            rightStack.Children.Add(ccBtn);

            _clockText = new TextBlock();
            _clockText.Text = "Вс, 8 нояб.  15:00";
            _clockText.FontSize = 12.5;
            _clockText.FontWeight = FontWeights.Medium;
            _clockText.Foreground = Brushes.White;
            _clockText.VerticalAlignment = VerticalAlignment.Center;
            Border clockBtn = CreateBarButton(_clockText, delegate(object s, MouseButtonEventArgs e)
            {
                ShellController.Instance.ToggleCalendarPopup();
            });
            clockBtn.Padding = new Thickness(8, 3, 6, 3);
            clockBtn.ToolTip = "Календарь";
            rightStack.Children.Add(clockBtn);

            Grid.SetColumn(rightStack, 2);
            rootGrid.Children.Add(rightStack);

            barBorder.Child = rootGrid;
            Content = barBorder;
        }

        private UIElement CreateAppleLogoPath()
        {
            WpfPath p = new WpfPath();
            p.Data = Geometry.Parse("M 11.1,5.2 C 10.4,5.2 9.4,5.8 8.9,5.8 C 8.4,5.8 7.5,5.2 6.6,5.2 C 5.1,5.2 3.5,6.4 3.5,8.9 C 3.5,11.6 5.6,14.8 7.0,14.8 C 7.7,14.8 8.1,14.3 9.0,14.3 C 9.9,14.3 10.2,14.8 11.0,14.8 C 12.4,14.8 13.7,12.5 14.1,11.3 C 12.3,10.5 12.0,7.7 13.9,6.6 C 13.2,5.6 12.1,5.2 11.1,5.2 Z M 10.6,2.2 C 9.6,2.3 8.5,3.2 8.7,4.6 C 9.8,4.7 10.9,3.8 10.6,2.2 Z");
            p.Fill = Brushes.White;
            p.Stretch = Stretch.Uniform;
            p.Width = 13;
            p.Height = 15;
            p.VerticalAlignment = VerticalAlignment.Center;
            return p;
        }

        private UIElement CreateVectorIcon(string pathData, double w, double h, bool strokeOnly)
        {
            WpfPath p = new WpfPath();
            p.Data = Geometry.Parse(pathData);
            if (strokeOnly)
            {
                p.Stroke = Brushes.White;
                p.StrokeThickness = 1.5;
                p.StrokeStartLineCap = PenLineCap.Round;
                p.StrokeEndLineCap = PenLineCap.Round;
            }
            else
            {
                p.Fill = Brushes.White;
            }
            p.Width = w;
            p.Height = h;
            p.Stretch = Stretch.Uniform;
            p.VerticalAlignment = VerticalAlignment.Center;
            return p;
        }

        private UIElement CreateControlCenterIcon()
        {
            Canvas c = new Canvas();
            c.Width = 16;
            c.Height = 14;
            c.VerticalAlignment = VerticalAlignment.Center;

            Rectangle r1 = new Rectangle { Width = 15, Height = 5.5, RadiusX = 2.75, RadiusY = 2.75, Stroke = Brushes.White, StrokeThickness = 1.2 };
            Canvas.SetLeft(r1, 0.5);
            Canvas.SetTop(r1, 1);
            Ellipse e1 = new Ellipse { Width = 4.5, Height = 4.5, Fill = Brushes.White };
            Canvas.SetLeft(e1, 10.2);
            Canvas.SetTop(e1, 1.5);

            Rectangle r2 = new Rectangle { Width = 15, Height = 5.5, RadiusX = 2.75, RadiusY = 2.75, Stroke = Brushes.White, StrokeThickness = 1.2 };
            Canvas.SetLeft(r2, 0.5);
            Canvas.SetTop(r2, 7.8);
            Ellipse e2 = new Ellipse { Width = 4.5, Height = 4.5, Fill = Brushes.White };
            Canvas.SetLeft(e2, 1.3);
            Canvas.SetTop(e2, 8.3);

            c.Children.Add(r1);
            c.Children.Add(e1);
            c.Children.Add(r2);
            c.Children.Add(e2);
            return c;
        }

        private Border CreateTextMenuButton(string text, Func<List<FrameworkElement>> menuBuilder)
        {
            TextBlock tb = new TextBlock();
            tb.Text = text;
            tb.FontSize = 13;
            tb.FontWeight = FontWeights.Medium;
            tb.Foreground = new SolidColorBrush(Color.FromArgb(238, 255, 255, 255));
            tb.VerticalAlignment = VerticalAlignment.Center;
            return CreateBarButton(tb, delegate(object s, MouseButtonEventArgs e)
            {
                ShowMenuDropdown((FrameworkElement)s, menuBuilder());
            });
        }

        private Border CreateBarButton(UIElement child, MouseButtonEventHandler onClick)
        {
            Border b = new Border();
            b.CornerRadius = new CornerRadius(5);
            b.Padding = new Thickness(7, 3, 7, 3);
            b.Margin = new Thickness(1, 2, 1, 2);
            b.Background = Brushes.Transparent;
            b.Cursor = Cursors.Arrow;
            b.Child = child;

            b.MouseEnter += delegate(object s, MouseEventArgs e)
            {
                b.Background = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255));
            };
            b.MouseLeave += delegate(object s, MouseEventArgs e)
            {
                b.Background = Brushes.Transparent;
            };
            b.MouseLeftButtonDown += onClick;
            return b;
        }

        private void ShowMenuDropdown(FrameworkElement anchor, List<FrameworkElement> items)
        {
            if (_activeMenuPopup != null && _activeMenuPopup.IsOpen)
            {
                _activeMenuPopup.IsOpen = false;
                if (_activeMenuPopup.PlacementTarget == anchor)
                {
                    _activeMenuPopup = null;
                    return;
                }
            }

            Popup popup = new Popup();
            popup.AllowsTransparency = true;
            popup.PlacementTarget = anchor;
            popup.Placement = PlacementMode.Bottom;
            popup.VerticalOffset = 3;
            popup.StaysOpen = false;
            popup.PopupAnimation = PopupAnimation.Fade;

            Border outer = new Border();
            outer.Margin = new Thickness(10, 4, 10, 14);
            outer.Background = LiquidGlassTheme.CreateGlassBrush(234, 246);
            outer.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush();
            outer.BorderThickness = new Thickness(1);
            outer.CornerRadius = new CornerRadius(12);
            outer.Padding = new Thickness(5);
            outer.MinWidth = 245;
            outer.Effect = new DropShadowEffect
            {
                BlurRadius = 22,
                ShadowDepth = 6,
                Opacity = 0.55,
                Color = Colors.Black
            };

            StackPanel stack = new StackPanel();
            foreach (FrameworkElement el in items)
            {
                stack.Children.Add(el);
            }
            outer.Child = stack;
            popup.Child = outer;

            _activeMenuPopup = popup;
            popup.IsOpen = true;
        }

        private FrameworkElement CreateDropdownItem(string label, string shortcut, Action action)
        {
            Border itemBorder = new Border();
            itemBorder.CornerRadius = new CornerRadius(6);
            itemBorder.Padding = new Thickness(10, 5, 10, 5);
            itemBorder.Background = Brushes.Transparent;

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock lbl = new TextBlock
            {
                Text = label,
                FontSize = 13,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lbl, 0);
            g.Children.Add(lbl);

            if (!string.IsNullOrEmpty(shortcut))
            {
                TextBlock sc = new TextBlock
                {
                    Text = shortcut,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromArgb(145, 235, 235, 245)),
                    Margin = new Thickness(20, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(sc, 1);
                g.Children.Add(sc);
            }

            itemBorder.Child = g;

            itemBorder.MouseEnter += delegate(object s, MouseEventArgs e)
            {
                itemBorder.Background = LiquidGlassTheme.AccentBlueBrush;
            };
            itemBorder.MouseLeave += delegate(object s, MouseEventArgs e)
            {
                itemBorder.Background = Brushes.Transparent;
            };
            itemBorder.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (_activeMenuPopup != null) _activeMenuPopup.IsOpen = false;
                if (action != null) action();
            };
            return itemBorder;
        }

        private FrameworkElement CreateDropdownSeparator()
        {
            Border sep = new Border();
            sep.Height = 1;
            sep.Background = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255));
            sep.Margin = new Thickness(8, 4, 8, 4);
            return sep;
        }

        private List<FrameworkElement> BuildSystemMenuItems()
        {
            List<FrameworkElement> list = new List<FrameworkElement>();
            list.Add(CreateDropdownItem("О системе", "", delegate() { ShellController.Instance.ShowAboutSystem(); }));
            list.Add(CreateDropdownItem("Настройки лаунчера...", "Win+Shift+A", delegate() { ShellController.Instance.ShowLauncherSettings(); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Параметры Windows...", "Win+I", delegate() { ShellController.RunProcess("explorer.exe", "ms-settings:"); }));
            list.Add(CreateDropdownItem("Microsoft Store...", "", delegate() { ShellController.RunProcess("explorer.exe", "ms-windows-store:"); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Mission Control (все окна)", "F3", delegate() { ShellController.Instance.ToggleWindowSwitcher(); }));
            list.Add(CreateDropdownItem("Постановщик (Stage Manager)", "", delegate() { ShellController.Instance.ToggleStageManager(); }));
            list.Add(CreateDropdownItem("Диспетчер задач...", "Ctrl+Shift+Esc", delegate() { ShellController.RunProcess("taskmgr.exe", ""); }));
            list.Add(CreateDropdownItem("Закрыть активное окно", "Alt+F4", delegate()
            {
                IntPtr h = ShellController.Instance.LastExternalForegroundHwnd;
                if (h != IntPtr.Zero) NativeMethods.PostMessage(h, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Спящий режим", "", delegate() { ShellController.RunProcess("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0"); }));
            list.Add(CreateDropdownItem("Перезагрузка...", "", delegate() { ShellController.RunProcess("shutdown.exe", "/r /t 0"); }));
            list.Add(CreateDropdownItem("Выключение...", "", delegate() { ShellController.RunProcess("shutdown.exe", "/s /t 0"); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Заблокировать экран", "Win+L", delegate() { NativeMethods.LockWorkStation(); }));
            list.Add(CreateDropdownItem("Выход из лаунчера", "", delegate() { ShellController.Instance.ShutdownAndRestoreWindows(); }));
            return list;
        }

        private List<FrameworkElement> BuildActiveAppMenuItems()
        {
            string app = ShellController.Instance.ActiveAppName;
            List<FrameworkElement> list = new List<FrameworkElement>();
            list.Add(CreateDropdownItem("Настройки лаунчера...", "Win+Shift+A", delegate() { ShellController.Instance.ShowLauncherSettings(); }));
            list.Add(CreateDropdownItem("О системе", "", delegate() { ShellController.Instance.ShowAboutSystem(); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Свернуть «" + app + "»", "", delegate()
            {
                IntPtr h = ShellController.Instance.LastExternalForegroundHwnd;
                if (h != IntPtr.Zero) NativeMethods.ShowWindow(h, NativeMethods.SW_SHOWMINIMIZED);
            }));
            list.Add(CreateDropdownItem("Все приложения", "Win", delegate() { ShellController.Instance.ToggleLaunchpad(); }));
            list.Add(CreateDropdownItem("Обзор окон", "", delegate() { ShellController.Instance.ToggleWindowSwitcher(); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Закрыть «" + app + "»", "Alt+F4", delegate()
            {
                IntPtr h = ShellController.Instance.LastExternalForegroundHwnd;
                if (h != IntPtr.Zero) NativeMethods.PostMessage(h, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }));
            return list;
        }

        private List<FrameworkElement> BuildFileMenuItems()
        {
            List<FrameworkElement> list = new List<FrameworkElement>();
            list.Add(CreateDropdownItem("Новое окно Проводника", "Win+E", delegate() { ShellController.RunProcess("explorer.exe", ""); }));
            list.Add(CreateDropdownItem("Открыть Терминал", "", delegate() { ShellController.RunProcess("wt.exe", ""); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Папка «Рабочий стол»", "", delegate()
            {
                ShellController.RunProcess("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            }));
            list.Add(CreateDropdownItem("Скриншот области...", "Win+Shift+S", delegate()
            {
                ShellController.RunProcess("explorer.exe", "ms-screenclip:");
            }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Очистить Корзину...", "", delegate()
            {
                NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null, 0);
            }));
            list.Add(CreateDropdownItem("Закрыть окно", "Alt+F4", delegate()
            {
                IntPtr h = ShellController.Instance.LastExternalForegroundHwnd;
                if (h != IntPtr.Zero) NativeMethods.PostMessage(h, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }));
            return list;
        }

        private void SendShortcutToLastWindow(byte vkWithCtrl)
        {
            IntPtr h = ShellController.Instance.LastExternalForegroundHwnd;
            if (h != IntPtr.Zero) NativeMethods.SetForegroundWindow(h);
            Thread.Sleep(40);
            NativeMethods.keybd_event(0x11, 0, 0, UIntPtr.Zero);
            NativeMethods.keybd_event(vkWithCtrl, 0, 0, UIntPtr.Zero);
            NativeMethods.keybd_event(vkWithCtrl, 0, 2, UIntPtr.Zero);
            NativeMethods.keybd_event(0x11, 0, 2, UIntPtr.Zero);
        }

        private List<FrameworkElement> BuildEditMenuItems()
        {
            List<FrameworkElement> list = new List<FrameworkElement>();
            list.Add(CreateDropdownItem("Отменить", "Ctrl+Z", delegate() { SendShortcutToLastWindow(0x5A); }));
            list.Add(CreateDropdownItem("Повторить", "Ctrl+Y", delegate() { SendShortcutToLastWindow(0x59); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Вырезать", "Ctrl+X", delegate() { SendShortcutToLastWindow(0x58); }));
            list.Add(CreateDropdownItem("Копировать", "Ctrl+C", delegate() { SendShortcutToLastWindow(0x43); }));
            list.Add(CreateDropdownItem("Вставить", "Ctrl+V", delegate() { SendShortcutToLastWindow(0x56); }));
            list.Add(CreateDropdownItem("Выделить всё", "Ctrl+A", delegate() { SendShortcutToLastWindow(0x41); }));
            return list;
        }

        private List<FrameworkElement> BuildViewMenuItems()
        {
            List<FrameworkElement> list = new List<FrameworkElement>();
            list.Add(CreateDropdownItem("Все приложения", "Win", delegate() { ShellController.Instance.ToggleLaunchpad(); }));
            list.Add(CreateDropdownItem("Быстрый поиск", "Alt+Пробел", delegate() { ShellController.Instance.ToggleSpotlight(); }));
            list.Add(CreateDropdownItem("Обзор активных окон", "", delegate() { ShellController.Instance.ToggleWindowSwitcher(); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem(
                ShellController.Instance.IsDarkMode ? "Переключить на Светлую тему" : "Переключить на Тёмную тему",
                "",
                delegate() { ShellController.Instance.ToggleDarkMode(); }));
            list.Add(CreateDropdownItem(
                ShellController.Instance.Config.ShowDesktopWidgets ? "Скрыть виджеты рабочего стола" : "Показать виджеты рабочего стола",
                "",
                delegate()
                {
                    ShellController.Instance.Config.ShowDesktopWidgets = !ShellController.Instance.Config.ShowDesktopWidgets;
                    ShellController.Instance.SaveConfig();
                    ShellController.Instance.ApplyModuleVisibility();
                }));
            list.Add(CreateDropdownItem(
                NativeMethods.AreDesktopIconsVisible() ? "Скрыть значки рабочего стола" : "Показать значки рабочего стола",
                "",
                delegate() { NativeMethods.ToggleDesktopIcons(); }));
            list.Add(CreateDropdownItem(
                ShellController.Instance.DockAutoHide ? "Закрепить Dock (всегда виден)" : "Скрывать Dock в полноэкранных окнах",
                "",
                delegate()
                {
                    ShellController.Instance.DockAutoHide = !ShellController.Instance.DockAutoHide;
                    ShellController.Instance.SaveConfig();
                }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Настройки лаунчера...", "Win+Shift+A", delegate() { ShellController.Instance.ShowLauncherSettings(); }));
            return list;
        }

        private List<FrameworkElement> BuildGoMenuItems()
        {
            List<FrameworkElement> list = new List<FrameworkElement>();
            list.Add(CreateDropdownItem("Рабочий стол", "", delegate()
            {
                ShellController.RunProcess("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            }));
            list.Add(CreateDropdownItem("Загрузки", "", delegate()
            {
                string dl = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                ShellController.RunProcess("explorer.exe", dl);
            }));
            list.Add(CreateDropdownItem("Документы", "", delegate()
            {
                ShellController.RunProcess("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            }));
            list.Add(CreateDropdownItem("Папка пользователя", "", delegate()
            {
                ShellController.RunProcess("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            }));
            list.Add(CreateDropdownItem("Этот компьютер", "", delegate()
            {
                ShellController.RunProcess("explorer.exe", "shell:MyComputerFolder");
            }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Корзина", "", delegate()
            {
                ShellController.RunProcess("explorer.exe", "shell:RecycleBinFolder");
            }));
            return list;
        }

        private List<FrameworkElement> BuildWindowMenuItems()
        {
            List<FrameworkElement> list = new List<FrameworkElement>();
            list.Add(CreateDropdownItem("Mission Control (все окна)", "F3", delegate() { ShellController.Instance.ToggleWindowSwitcher(); }));
            list.Add(CreateDropdownItem("Постановщик (Stage Manager)", "", delegate() { ShellController.Instance.ToggleStageManager(); }));
            list.Add(CreateDropdownItem("Переключатель программ...", "Alt+Tab", delegate() { ShellController.Instance.ShowAppSwitcher(false); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Разделение экрана (Split View)...", "", delegate()
            {
                List<OpenWindowEntry> wins = ShellController.Instance.GetOpenApplicationWindows();
                if (wins.Count >= 2)
                    ShellController.Instance.EnterSplitView(wins[0].Hwnd, wins[1].Hwnd, 0.5);
            }));
            list.Add(CreateDropdownItem("Поместить окно слева (Split View)", "◧", delegate() { ShellController.Instance.StartSplitViewWithPicker(IntPtr.Zero, true); }));
            list.Add(CreateDropdownItem("Поместить окно справа (Split View)", "◨", delegate() { ShellController.Instance.StartSplitViewWithPicker(IntPtr.Zero, false); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Свернуть окно", "", delegate()
            {
                IntPtr h = ShellController.Instance.LastExternalForegroundHwnd;
                if (h != IntPtr.Zero) NativeMethods.ShowWindow(h, NativeMethods.SW_SHOWMINIMIZED);
            }));
            list.Add(CreateDropdownItem("Развернуть / Восстановить", "", delegate()
            {
                IntPtr h = ShellController.Instance.LastExternalForegroundHwnd;
                if (h != IntPtr.Zero)
                {
                    if (NativeMethods.IsZoomed(h))
                        NativeMethods.ShowWindow(h, NativeMethods.SW_RESTORE);
                    else
                        NativeMethods.ShowWindow(h, NativeMethods.SW_MAXIMIZE);
                }
            }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("Пункт управления", "", delegate() { ShellController.Instance.ToggleControlCenter(); }));
            list.Add(CreateDropdownItem("Календарь", "", delegate() { ShellController.Instance.ToggleCalendarPopup(); }));
            return list;
        }

        private List<FrameworkElement> BuildHelpMenuItems()
        {
            List<FrameworkElement> list = new List<FrameworkElement>();
            list.Add(CreateDropdownItem("Командная палитра и поиск...", "Alt+Пробел", delegate() { ShellController.Instance.ToggleSpotlight(); }));
            list.Add(CreateDropdownItem("Настройки лаунчера...", "Win+Shift+A", delegate() { ShellController.Instance.ShowLauncherSettings(); }));
            list.Add(CreateDropdownSeparator());
            list.Add(CreateDropdownItem("О системе", "", delegate() { ShellController.Instance.ShowAboutSystem(); }));
            return list;
        }

        private void SwitchKeyboardLayout()
        {
            IntPtr h = ShellController.Instance.LastExternalForegroundHwnd;
            if (h == IntPtr.Zero) h = NativeMethods.GetForegroundWindow();
            if (h != IntPtr.Zero)
            {
                NativeMethods.PostMessage(h, NativeMethods.WM_INPUTLANGCHANGEREQUEST, (IntPtr)2, IntPtr.Zero);
            }
        }

        private void OnPollTick(object sender, EventArgs e)
        {
            NativeMethods.POINT curPt;
            if (NativeMethods.GetCursorPos(out curPt))
            {
                ShellController.Instance.CheckHotCorners(curPt.X, curPt.Y);
            }
            if (_appBarRegistered && ShellController.Instance.Config.ShowTopBar && SystemParameters.WorkArea.Top < 26)
            {
                EnforceWorkArea();
            }

            DateTime now = DateTime.Now;
            string dayName = now.ToString("ddd", RuCulture);
            if (dayName.Length > 0) dayName = char.ToUpper(dayName[0]) + dayName.Substring(1);
            string monthName = now.ToString("MMM", RuCulture).TrimEnd('.');
            _clockText.Text = string.Format("{0}, {1} {2}.  {3:HH:mm}", dayName, now.Day, monthName, now);

            NativeMethods.MEMORYSTATUSEX mem = new NativeMethods.MEMORYSTATUSEX();
            mem.dwLength = (uint)Marshal.SizeOf(mem);
            if (NativeMethods.GlobalMemoryStatusEx(ref mem))
            {
                _cpuMemText.Text = string.Format("ОЗУ {0}%", mem.dwMemoryLoad);
            }

            IntPtr fg = NativeMethods.GetForegroundWindow();
            if (fg != IntPtr.Zero)
            {
                uint pid;
                uint tid = NativeMethods.GetWindowThreadProcessId(fg, out pid);
                IntPtr hkl = NativeMethods.GetKeyboardLayout(tid);
                int langId = hkl.ToInt32() & 0xFFFF;
                _langText.Text = (langId == 0x0419) ? "RU" : "EN";

                if (pid != _selfPid && _selfPid != 0)
                {
                    StringBuilder fgCls = new StringBuilder(128);
                    NativeMethods.GetClassName(fg, fgCls, fgCls.Capacity);
                    string clsName = fgCls.ToString();
                    if (clsName != "Shell_TrayWnd" && clsName != "Shell_SecondaryTrayWnd" && clsName != "CEF-OSC-WIDGET")
                    {
                        ShellController.Instance.LastExternalForegroundHwnd = fg;
                        string appName = ResolveFriendlyAppName(fg, pid);
                        ShellController.Instance.ActiveAppName = appName;
                        _activeAppText.Text = appName;
                    }
                }
            }
        }

        private static readonly Dictionary<uint, CachedAppNameEntry> _appNameCache = new Dictionary<uint, CachedAppNameEntry>();
        private class CachedAppNameEntry
        {
            public string Name;
            public DateTime Time;
        }

        private string ResolveFriendlyAppName(IntPtr hwnd, uint pid)
        {
            try
            {
                StringBuilder cls = new StringBuilder(128);
                NativeMethods.GetClassName(hwnd, cls, cls.Capacity);
                string clsName = cls.ToString();
                if (clsName == "Progman" || clsName == "WorkerW" || clsName == "Shell_TrayWnd")
                {
                    return "Рабочий стол";
                }
                if (clsName == "CabinetWClass" || clsName == "ExploreWClass")
                {
                    return "Проводник";
                }

                CachedAppNameEntry cached;
                if (_appNameCache.TryGetValue(pid, out cached))
                {
                    if ((DateTime.Now - cached.Time).TotalSeconds < 15.0) return cached.Name;
                }

                string pn = "";
                using (Process p = Process.GetProcessById((int)pid))
                {
                    pn = p.ProcessName;
                }

                string friendly = pn;
                if (string.Equals(pn, "explorer", StringComparison.OrdinalIgnoreCase)) friendly = "Проводник";
                else if (string.Equals(pn, "chrome", StringComparison.OrdinalIgnoreCase)) friendly = "Chrome";
                else if (string.Equals(pn, "Antigravity", StringComparison.OrdinalIgnoreCase)) friendly = "Antigravity";
                else if (string.Equals(pn, "OpenCode", StringComparison.OrdinalIgnoreCase)) friendly = "OpenCode";
                else if (string.Equals(pn, "iMe", StringComparison.OrdinalIgnoreCase) || string.Equals(pn, "Telegram", StringComparison.OrdinalIgnoreCase)) friendly = "iMe";
                else if (pn.IndexOf("Яндекс", StringComparison.OrdinalIgnoreCase) >= 0) friendly = "Яндекс Музыка";
                else if (pn.IndexOf("steam", StringComparison.OrdinalIgnoreCase) >= 0) friendly = "Steam";
                else if (string.Equals(pn, "WindowsTerminal", StringComparison.OrdinalIgnoreCase) || string.Equals(pn, "powershell", StringComparison.OrdinalIgnoreCase)) friendly = "Терминал";
                else if (string.Equals(pn, "SystemSettings", StringComparison.OrdinalIgnoreCase)) friendly = "Параметры";
                else if (string.Equals(pn, "Taskmgr", StringComparison.OrdinalIgnoreCase)) friendly = "Диспетчер задач";
                else if (string.Equals(pn, "Code", StringComparison.OrdinalIgnoreCase)) friendly = "VS Code";

                _appNameCache[pid] = new CachedAppNameEntry { Name = friendly, Time = DateTime.Now };
                return friendly;
            }
            catch
            {
                return "Рабочий стол";
            }
        }
    }
    // =========================================================================
    // 2. DOCK WINDOW (120+ FPS Deterministic Wave Physics, Pin/Unpin/Reorder)
    // =========================================================================
}
