using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using MacOSTahoe.Core;
using MacOSTahoe.Models;
using MacOSTahoe.Native;
using MacOSTahoe.Services;
using MacOSTahoe.Theme;

namespace MacOSTahoe.Views
{
    public class DockWindow : Window
    {
        private StackPanel _dockStack;
        private Border _glassBorder;
        private List<DockAppItem> _items = new List<DockAppItem>();

        private DispatcherTimer _pollTimer;
        private DispatcherTimer _autoHideTimer;
        private bool _isMouseOverDock = false;
        private bool _isDockHidden = false;

        private double _baseSize = 50.0;
        private double _zoomRange = 26.0;
        private const double InfluenceRadius = 140.0;

        public DockWindow()
        {
            Title = "DockWindow";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;

            _baseSize = ShellController.Instance.Config.DockIconSize;
            _zoomRange = ShellController.Instance.Config.DockZoomRange;

            BuildUi();

            SourceInitialized += (s, e) =>
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                DwmBlurHelper.ApplyBlur(hwnd, ShellController.Instance.Config.IsDarkMode);
                DwmBlurHelper.MakeToolWindow(hwnd);
            };

            Loaded += (s, e) =>
            {
                LoadDockItems();
                PositionDock();
                StartTimers();
                CompositionTarget.Rendering += OnRenderTick;
            };

            MouseEnter += (s, e) =>
            {
                _isMouseOverDock = true;
                RevealDock();
            };

            MouseLeave += (s, e) =>
            {
                _isMouseOverDock = false;
                ResetWaveTargets();
            };

            MouseMove += OnDockMouseMove;
        }

        private void BuildUi()
        {
            _glassBorder = new Border
            {
                Background = LiquidGlassTheme.CreateGlassBrush(130, 180),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(10, 6, 10, 6),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            _dockStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            _glassBorder.Child = _dockStack;
            Content = _glassBorder;
        }

        public void PositionDock()
        {
            UpdateLayout();
            _glassBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double desiredW = Math.Max(_glassBorder.DesiredSize.Width + 24, 200);
            double desiredH = Math.Max(_baseSize + _zoomRange + 28, 86);

            Width = desiredW;
            Height = desiredH;
            Left = (SystemParameters.PrimaryScreenWidth - desiredW) / 2.0;

            if (!_isDockHidden)
            {
                Top = SystemParameters.PrimaryScreenHeight - desiredH - 8;
            }
            else
            {
                Top = SystemParameters.PrimaryScreenHeight - 4;
            }
        }

        public void LoadDockItems()
        {
            _dockStack.Children.Clear();
            _items.Clear();

            var pinnedConfigs = ShellController.Instance.Config.PinnedDockItems;
            if (pinnedConfigs != null)
            {
                foreach (var cfg in pinnedConfigs)
                {
                    var item = CreateItemFromConfig(cfg);
                    _items.Add(item);
                }
            }

            RebuildVisualTree();
            UpdateRunningStates();
        }

        private DockAppItem CreateItemFromConfig(DockItemConfig cfg)
        {
            var item = new DockAppItem
            {
                Id = cfg.Id,
                Name = cfg.Name,
                IconFile = cfg.IconFile,
                LaunchCommand = cfg.LaunchCommand,
                LaunchArgs = cfg.LaunchArgs,
                ProcessNames = cfg.ProcessNames,
                IsPinned = true,
                IsSeparator = cfg.IsSeparator,
                IsTrash = cfg.IsTrash,
                IsAppsLauncher = cfg.IsAppsLauncher,
                IsDesktopFolder = cfg.IsDesktopFolder,
                IsExplorer = cfg.IsExplorer,
                IsWindowSwitcher = cfg.IsWindowSwitcher,
                CurrentSize = _baseSize,
                TargetSize = _baseSize
            };
            return item;
        }

        private void RebuildVisualTree()
        {
            _dockStack.Children.Clear();

            foreach (var item in _items)
            {
                if (item.IsSeparator)
                {
                    var sepBorder = new Border
                    {
                        Width = 1,
                        Height = _baseSize * 0.7,
                        Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
                        Margin = new Thickness(6, 0, 6, 8),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    item.RootContainer = sepBorder;
                    _dockStack.Children.Add(sepBorder);
                    continue;
                }

                var container = new Border
                {
                    Background = Brushes.Transparent,
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(3, 0, 3, 0),
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                var stack = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var iconWrapper = new Grid
                {
                    Width = _baseSize,
                    Height = _baseSize,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                var iconImg = new Image
                {
                    Width = _baseSize,
                    Height = _baseSize,
                    Stretch = Stretch.Uniform
                };
                RenderOptions.SetBitmapScalingMode(iconImg, BitmapScalingMode.HighQuality);

                iconImg.Source = ResolveItemIcon(item);
                iconWrapper.Children.Add(iconImg);
                stack.Children.Add(iconWrapper);

                // Running indicator dot
                var dot = new Border
                {
                    Width = 4,
                    Height = 4,
                    CornerRadius = new CornerRadius(2),
                    Background = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 3, 0, 1),
                    Visibility = item.IsRunning ? Visibility.Visible : Visibility.Collapsed
                };
                stack.Children.Add(dot);

                container.Child = stack;

                item.RootContainer = container;
                item.IconWrapper = iconWrapper;
                item.IconImage = iconImg;
                item.RunningDot = dot;

                if (!string.IsNullOrEmpty(item.Name))
                {
                    container.ToolTip = item.Name;
                }

                // Interaction
                container.MouseLeftButtonUp += (s, e) => OnItemClicked(item);
                container.MouseRightButtonUp += (s, e) => ShowItemContextMenu(item);

                _dockStack.Children.Add(container);
            }

            PositionDock();
        }

        private ImageSource ResolveItemIcon(DockAppItem item)
        {
            if (item.CustomImageSource != null) return item.CustomImageSource;

            if (!string.IsNullOrEmpty(item.IconFile))
            {
                string fullPath = System.IO.Path.Combine(ShellController.IconsDir, item.IconFile);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        var bmp = new BitmapImage(new Uri(fullPath, UriKind.Absolute));
                        bmp.Freeze();
                        return bmp;
                    }
                    catch { }
                }
            }

            if (!string.IsNullOrEmpty(item.LaunchCommand) && File.Exists(item.LaunchCommand))
            {
                var icon = ShellComInterop.ExtractHighResIcon(item.LaunchCommand, 0);
                if (icon != null) return icon;
            }

            return null;
        }

        private void OnItemClicked(DockAppItem item)
        {
            if (item.IsAppsLauncher)
            {
                ShellController.Instance.ToggleLaunchpad();
                return;
            }

            if (item.IsWindowSwitcher)
            {
                ShellController.Instance.ToggleWindowSwitcher();
                return;
            }

            if (item.IsRunning && item.ActiveHwnd != IntPtr.Zero)
            {
                WindowTrackerService.ActivateWindow(item.ActiveHwnd);
                return;
            }

            ShellController.Instance.LaunchApp(item.LaunchCommand, item.LaunchArgs);
        }

        private void ShowItemContextMenu(DockAppItem item)
        {
            ContextMenu cm = new ContextMenu();

            if (item.IsTrash)
            {
                var miEmpty = new MenuItem { Header = "Очистить Корзину" };
                miEmpty.Click += (s, e) =>
                {
                    Win32Api.SHEmptyRecycleBin(IntPtr.Zero, null, 0);
                };
                cm.Items.Add(miEmpty);
                cm.IsOpen = true;
                return;
            }

            var miOpen = new MenuItem { Header = "Открыть" };
            miOpen.Click += (s, e) => OnItemClicked(item);
            cm.Items.Add(miOpen);

            if (!string.IsNullOrEmpty(item.LaunchCommand) && !item.LaunchCommand.StartsWith("@"))
            {
                var miAdmin = new MenuItem { Header = "Запуск от имени администратора" };
                miAdmin.Click += (s, e) =>
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = item.LaunchCommand,
                            Arguments = item.LaunchArgs ?? "",
                            Verb = "runas",
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                    }
                    catch { }
                };
                cm.Items.Add(miAdmin);
            }

            cm.Items.Add(new Separator());

            if (item.IsPinned)
            {
                var miUnpin = new MenuItem { Header = "Открепить от Dock" };
                miUnpin.Click += (s, e) => UnpinItem(item);
                cm.Items.Add(miUnpin);
            }
            else
            {
                var miPin = new MenuItem { Header = "Закрепить в Dock" };
                miPin.Click += (s, e) => PinItem(item);
                cm.Items.Add(miPin);
            }

            cm.IsOpen = true;
        }

        private void PinItem(DockAppItem item)
        {
            item.IsPinned = true;
            item.IsDynamic = false;

            var newConfig = new DockItemConfig
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = item.Name,
                LaunchCommand = item.LaunchCommand,
                LaunchArgs = item.LaunchArgs,
                ProcessNames = item.ProcessNames
            };

            ShellController.Instance.Config.PinnedDockItems.Add(newConfig);
            ShellController.Instance.SaveConfig();
            LoadDockItems();
        }

        private void UnpinItem(DockAppItem item)
        {
            var cfg = ShellController.Instance.Config.PinnedDockItems
                .FirstOrDefault(x => x.Id == item.Id || (x.Name == item.Name && x.LaunchCommand == item.LaunchCommand));

            if (cfg != null)
            {
                ShellController.Instance.Config.PinnedDockItems.Remove(cfg);
                ShellController.Instance.SaveConfig();
                LoadDockItems();
            }
        }

        private void OnDockMouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(_glassBorder);
            double mouseX = mousePos.X;

            foreach (var item in _items)
            {
                if (item.RootContainer == null || item.IsSeparator) continue;

                Point itemPos = item.RootContainer.TranslatePoint(new Point(item.RootContainer.ActualWidth / 2.0, 0), _glassBorder);
                double d = Math.Abs(mouseX - itemPos.X);

                if (d < InfluenceRadius)
                {
                    double ratio = Math.Cos((d / InfluenceRadius) * (Math.PI / 2.0));
                    item.TargetSize = _baseSize + (_zoomRange * ratio);
                    item.TargetLift = -_zoomRange * 0.45 * ratio;
                }
                else
                {
                    item.TargetSize = _baseSize;
                    item.TargetLift = 0;
                }
            }
        }

        private void ResetWaveTargets()
        {
            foreach (var item in _items)
            {
                item.TargetSize = _baseSize;
                item.TargetLift = 0;
            }
        }

        private void OnRenderTick(object sender, EventArgs e)
        {
            bool needsResize = false;

            foreach (var item in _items)
            {
                if (item.IconWrapper == null || item.IsSeparator) continue;

                double diffSize = item.TargetSize - item.CurrentSize;
                double diffLift = item.TargetLift - item.CurrentLift;

                if (Math.Abs(diffSize) > 0.05 || Math.Abs(diffLift) > 0.05)
                {
                    item.CurrentSize += diffSize * 0.32;
                    item.CurrentLift += diffLift * 0.32;

                    item.IconWrapper.Width = item.CurrentSize;
                    item.IconWrapper.Height = item.CurrentSize;
                    item.IconImage.Width = item.CurrentSize;
                    item.IconImage.Height = item.CurrentSize;
                    item.IconWrapper.Margin = new Thickness(0, item.CurrentLift, 0, -item.CurrentLift);

                    needsResize = true;
                }
            }

            if (needsResize)
            {
                PositionDock();
            }
        }

        private void StartTimers()
        {
            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _pollTimer.Tick += (s, e) => UpdateRunningStates();
            _pollTimer.Start();

            if (ShellController.Instance.Config.DockAutoHide)
            {
                _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                _autoHideTimer.Tick += OnAutoHideCheck;
                _autoHideTimer.Start();
            }
        }

        private void OnAutoHideCheck(object sender, EventArgs e)
        {
            if (!ShellController.Instance.Config.DockAutoHide) return;

            POINT pt;
            if (Win32Api.GetCursorPos(out pt))
            {
                int screenH = (int)SystemParameters.PrimaryScreenHeight;
                if (pt.Y >= screenH - 6)
                {
                    RevealDock();
                }
                else if (pt.Y < screenH - Height - 50 && !_isMouseOverDock)
                {
                    HideDock();
                }
            }
        }

        private void RevealDock()
        {
            if (_isDockHidden)
            {
                _isDockHidden = false;
                PositionDock();
            }
        }

        private void HideDock()
        {
            if (!_isDockHidden)
            {
                _isDockHidden = true;
                PositionDock();
            }
        }

        private void UpdateRunningStates()
        {
            var openWindows = ShellController.Instance.WindowTracker.GetOpenWindows();

            foreach (var item in _items)
            {
                item.IsRunning = false;
                item.ActiveHwnd = IntPtr.Zero;

                if (item.ProcessNames != null && item.ProcessNames.Length > 0)
                {
                    var win = openWindows.FirstOrDefault(w => item.ProcessNames.Any(p => p.Equals(w.ProcessName, StringComparison.OrdinalIgnoreCase)));
                    if (win != null)
                    {
                        item.IsRunning = true;
                        item.ActiveHwnd = win.Hwnd;
                    }
                }
                else if (item.IsExplorer)
                {
                    var win = openWindows.FirstOrDefault(w => w.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase));
                    if (win != null)
                    {
                        item.IsRunning = true;
                        item.ActiveHwnd = win.Hwnd;
                    }
                }

                if (item.RunningDot != null)
                {
                    item.RunningDot.Visibility = item.IsRunning ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }
    }
}
