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
    public class DockWindow : Window
    {
        private Grid _outerRoot;
        private Border _glassShelf;
        private StackPanel _itemsPanel;
        private Border _tooltipPill;
        private TextBlock _tooltipText;
        private TranslateTransform _dockSlideTransform;
        private Popup _dockContextMenuPopup;

        private Stopwatch _stopwatch = Stopwatch.StartNew();
        private double _lastFrameTime = 0.0;
        private double _currentSlideY = 0.0;
        private double _targetSlideY = 0.0;
        private bool _activeWindowIsMaximized = false;
        private bool _dockRevealedByMouse = false;
        private bool _isRenderingActive = false;
        private DispatcherTimer _windowScanTimer;

        public void EnsureRenderingActive()
        {
            if (!_isRenderingActive)
            {
                _isRenderingActive = true;
                _lastFrameTime = ElapsedSeconds;
                CompositionTarget.Rendering += OnRenderFrame;
            }
        }

        public void StopRendering()
        {
            if (_isRenderingActive)
            {
                _isRenderingActive = false;
                CompositionTarget.Rendering -= OnRenderFrame;
            }
        }

        private static readonly Dictionary<uint, CachedProcEntry> _procNameCache = new Dictionary<uint, CachedProcEntry>();
        private class CachedProcEntry
        {
            public string Name;
            public double Timestamp;
        }

        private string GetFastProcessName(uint pid, double now)
        {
            CachedProcEntry entry;
            if (_procNameCache.TryGetValue(pid, out entry))
            {
                if (now - entry.Timestamp < 15.0) return entry.Name;
            }

            string name = "";
            try
            {
                using (Process p = Process.GetProcessById((int)pid))
                {
                    name = p.ProcessName;
                }
            }
            catch { }

            _procNameCache[pid] = new CachedProcEntry { Name = name, Timestamp = now };
            return name;
        }

        private const double ItemMarginX = 3.5;

        public double ElapsedSeconds
        {
            get { return _stopwatch.Elapsed.TotalSeconds; }
        }

        private double BaseIconSize
        {
            get { return ShellController.Instance != null ? ShellController.Instance.Config.DockIconSize : 50.0; }
        }

        private double MaxIconZoom
        {
            get { return ShellController.Instance != null ? ShellController.Instance.Config.DockZoomRange : 26.0; }
        }

        public DockWindow()
        {
            Title = "Tahoe Dock";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Background = null;
            FontFamily = LiquidGlassTheme.UiFont;

            Width = Math.Min(1600, SystemParameters.PrimaryScreenWidth - 40);
            Height = 152;
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2.0;
            Top = SystemParameters.PrimaryScreenHeight - Height;

            BuildUI();

            SourceInitialized += delegate(object s, EventArgs e)
            {
                NativeMethods.MakeToolWindow(this, true);
            };

            MouseEnter += delegate { EnsureRenderingActive(); };
            MouseMove += delegate { EnsureRenderingActive(); };

            EnsureRenderingActive();

            _windowScanTimer = new DispatcherTimer();
            _windowScanTimer.Interval = TimeSpan.FromMilliseconds(1500); // 1.5s instead of 600ms
            _windowScanTimer.Tick += OnScanRunningWindows;
            _windowScanTimer.Start();
            OnScanRunningWindows(null, EventArgs.Empty);
        }

        private void BuildUI()
        {
            _outerRoot = new Grid();
            _outerRoot.Background = null;
            _outerRoot.ClipToBounds = false;
            _dockSlideTransform = new TranslateTransform(0, 0);
            _outerRoot.RenderTransform = _dockSlideTransform;

            _glassShelf = new Border();
            _glassShelf.Height = BaseIconSize + 14;
            _glassShelf.Width = 300.0;
            _glassShelf.HorizontalAlignment = HorizontalAlignment.Center;
            _glassShelf.VerticalAlignment = VerticalAlignment.Bottom;
            _glassShelf.Margin = new Thickness(0, 0, 0, 8);
            _glassShelf.CornerRadius = new CornerRadius(22);
            _glassShelf.Background = LiquidGlassTheme.CreateGlassBrush(168, 196);
            _glassShelf.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush();
            _glassShelf.BorderThickness = new Thickness(1);
            _glassShelf.Effect = new DropShadowEffect
            {
                BlurRadius = 26,
                ShadowDepth = 8,
                Opacity = 0.56,
                Color = Colors.Black
            };
            _outerRoot.Children.Add(_glassShelf);

            _itemsPanel = new StackPanel();
            _itemsPanel.Orientation = Orientation.Horizontal;
            _itemsPanel.HorizontalAlignment = HorizontalAlignment.Center;
            _itemsPanel.VerticalAlignment = VerticalAlignment.Bottom;
            _itemsPanel.Margin = new Thickness(0, 0, 0, 14);
            _itemsPanel.ClipToBounds = false;

            RebuildDockVisuals();
            _outerRoot.Children.Add(_itemsPanel);

            _tooltipPill = new Border();
            _tooltipPill.HorizontalAlignment = HorizontalAlignment.Left;
            _tooltipPill.VerticalAlignment = VerticalAlignment.Bottom;
            _tooltipPill.Background = LiquidGlassTheme.CreateGlassBrush(228, 242);
            _tooltipPill.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush();
            _tooltipPill.BorderThickness = new Thickness(1);
            _tooltipPill.CornerRadius = new CornerRadius(8);
            _tooltipPill.Padding = new Thickness(11, 4, 11, 4);
            _tooltipPill.Visibility = Visibility.Hidden;
            _tooltipPill.IsHitTestVisible = false;
            _tooltipPill.Effect = new DropShadowEffect
            {
                BlurRadius = 14,
                ShadowDepth = 4,
                Opacity = 0.45,
                Color = Colors.Black
            };

            _tooltipText = new TextBlock();
            _tooltipText.Foreground = Brushes.White;
            _tooltipText.FontSize = 12.5;
            _tooltipText.FontWeight = FontWeights.Medium;
            _tooltipPill.Child = _tooltipText;
            _outerRoot.Children.Add(_tooltipPill);

            Content = _outerRoot;
        }

        public void RebuildDockVisuals()
        {
            if (_itemsPanel == null) return;
            _itemsPanel.Children.Clear();
            if (_glassShelf != null)
            {
                _glassShelf.Height = BaseIconSize + 14;
                double initialRowW = 0.0;
                if (ShellController.Instance != null && ShellController.Instance.DockItems != null)
                {
                    foreach (DockAppItem item in ShellController.Instance.DockItems)
                    {
                        initialRowW += item.IsSeparator ? 15.0 : (BaseIconSize + ItemMarginX * 2.0);
                    }
                }
                _glassShelf.Width = Math.Max(200.0, initialRowW + 24.0);
            }
            foreach (DockAppItem item in ShellController.Instance.DockItems)
            {
                _itemsPanel.Children.Add(CreateDockItemElement(item));
            }
            OnScanRunningWindows(null, EventArgs.Empty);
        }

        private FrameworkElement CreateDockItemElement(DockAppItem item)
        {
            if (item.IsSeparator)
            {
                Border sep = new Border();
                sep.Width = 1;
                sep.Height = Math.Max(28, BaseIconSize - 12);
                sep.VerticalAlignment = VerticalAlignment.Bottom;
                sep.Margin = new Thickness(7, 0, 7, 6);
                sep.Background = new SolidColorBrush(Color.FromArgb(62, 255, 255, 255));
                item.RootContainer = sep;
                return sep;
            }

            Border container = new Border();
            container.Background = Brushes.Transparent;
            container.Margin = new Thickness(ItemMarginX, 0, ItemMarginX, 0);
            container.VerticalAlignment = VerticalAlignment.Bottom;
            container.Cursor = Cursors.Arrow;
            container.ClipToBounds = false;

            Grid wrapper = new Grid();
            wrapper.Width = BaseIconSize;
            wrapper.Height = BaseIconSize;
            wrapper.VerticalAlignment = VerticalAlignment.Bottom;
            wrapper.ClipToBounds = false;

            Image img = new Image();
            img.Source = (item.CustomImageSource != null)
                ? item.CustomImageSource
                : ShellController.Instance.GetIcon(item.IconFile);
            img.Stretch = Stretch.Uniform;
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
            img.Effect = new DropShadowEffect
            {
                BlurRadius = 10,
                ShadowDepth = 4,
                Opacity = 0.42,
                Color = Colors.Black
            };

            TransformGroup tg = new TransformGroup();
            ScaleTransform st = new ScaleTransform(1.0, 1.0);
            TranslateTransform tt = new TranslateTransform(0.0, 0.0);
            tg.Children.Add(st);
            tg.Children.Add(tt);
            img.RenderTransformOrigin = new Point(0.5, 1.0);
            img.RenderTransform = tg;
            item.IconScaleTransform = st;
            item.IconTranslateTransform = tt;

            wrapper.Children.Add(img);

            Border dot = new Border();
            dot.Width = 4.5;
            dot.Height = 4.5;
            dot.CornerRadius = new CornerRadius(2.5);
            dot.Background = Brushes.White;
            dot.HorizontalAlignment = HorizontalAlignment.Center;
            dot.VerticalAlignment = VerticalAlignment.Bottom;
            dot.Margin = new Thickness(0, 0, 0, -6);
            dot.Visibility = item.IsRunning ? Visibility.Visible : Visibility.Hidden;
            dot.Effect = new DropShadowEffect
            {
                BlurRadius = 6,
                ShadowDepth = 0,
                Opacity = 0.9,
                Color = Colors.White
            };
            wrapper.Children.Add(dot);

            container.Child = wrapper;

            item.RootContainer = container;
            item.IconWrapper = wrapper;
            item.IconImage = img;
            item.RunningDot = dot;
            item.CurrentSize = BaseIconSize;
            item.TargetSize = BaseIconSize;

            container.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                ShellController.Instance.LaunchOrFocusDockItem(item);
            };

            container.MouseRightButtonUp += delegate(object s, MouseButtonEventArgs e)
            {
                ShowDockContextMenu(item);
            };

            return container;
        }

        private void ShowDockContextMenu(DockAppItem item)
        {
            if (_dockContextMenuPopup != null && _dockContextMenuPopup.IsOpen)
            {
                _dockContextMenuPopup.IsOpen = false;
            }

            Popup popup = new Popup();
            popup.AllowsTransparency = true;
            popup.PlacementTarget = item.RootContainer;
            popup.Placement = PlacementMode.Top;
            popup.VerticalOffset = -10;
            popup.StaysOpen = false;
            popup.PopupAnimation = PopupAnimation.Fade;

            Border outer = new Border
            {
                Margin = new Thickness(10),
                Background = LiquidGlassTheme.CreateGlassBrush(234, 246),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(5),
                MinWidth = 220,
                Effect = new DropShadowEffect { BlurRadius = 22, ShadowDepth = 6, Opacity = 0.55, Color = Colors.Black }
            };

            StackPanel stack = new StackPanel();
            stack.Children.Add(CreateDockMenuRow("▶  Открыть «" + item.Name + "»", delegate
            {
                popup.IsOpen = false;
                ShellController.Instance.LaunchOrFocusDockItem(item);
            }));

            if (!item.IsAppsLauncher && !item.IsWindowSwitcher && !item.IsTrash)
            {
                stack.Children.Add(CreateDockMenuRow("➕  Новое окно", delegate
                {
                    popup.IsOpen = false;
                    ShellController.RunProcess(item.LaunchCommand, item.LaunchArgs);
                }));

                if (!item.IsDesktopFolder && !item.IsExplorer)
                {
                    stack.Children.Add(CreateDockMenuRow("🛡  Запустить от имени администратора", delegate
                    {
                        popup.IsOpen = false;
                        ShellController.RunProcessEx(item.LaunchCommand, item.LaunchArgs, true);
                    }));
                }

                if (!string.IsNullOrEmpty(item.LaunchCommand) && item.LaunchCommand.IndexOf('\\') >= 0 && File.Exists(item.LaunchCommand))
                {
                    stack.Children.Add(CreateDockMenuRow("📂  Открыть расположение файла", delegate
                    {
                        popup.IsOpen = false;
                        ShellController.OpenFileLocation(item.LaunchCommand);
                    }));
                }
            }

            if (item.IsTrash)
            {
                stack.Children.Add(CreateDockMenuRow("🗑  Очистить Корзину", delegate
                {
                    popup.IsOpen = false;
                    NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null, 0);
                }));
            }

            // Pin / Unpin / Move options
            if (item.IsDynamic)
            {
                stack.Children.Add(CreateDockSeparator());
                stack.Children.Add(CreateDockMenuRow("📌  Закрепить в Dock", delegate
                {
                    popup.IsOpen = false;
                    string exePath = item.LaunchCommand;
                    try
                    {
                        if (item.ActiveHwnd != IntPtr.Zero)
                        {
                            uint pid;
                            NativeMethods.GetWindowThreadProcessId(item.ActiveHwnd, out pid);
                            Process p = Process.GetProcessById((int)pid);
                            exePath = p.MainModule.FileName;
                        }
                    }
                    catch { }
                    ShellController.Instance.PinAppToDock(item.Name, exePath, "");
                }));
            }
            else if (item.IsPinned && !item.IsExplorer && !item.IsAppsLauncher && !item.IsTrash && !item.IsDesktopFolder)
            {
                stack.Children.Add(CreateDockSeparator());
                stack.Children.Add(CreateDockMenuRow("◀  Переместить влево", delegate
                {
                    popup.IsOpen = false;
                    ShellController.Instance.MovePinnedDockItem(item.Id, -1);
                }));
                stack.Children.Add(CreateDockMenuRow("▶  Переместить вправо", delegate
                {
                    popup.IsOpen = false;
                    ShellController.Instance.MovePinnedDockItem(item.Id, 1);
                }));
                stack.Children.Add(CreateDockMenuRow("❌  Открепить из Dock", delegate
                {
                    popup.IsOpen = false;
                    ShellController.Instance.UnpinAppFromDock(item.Id);
                }));
            }

            if (item.ActiveHwnd != IntPtr.Zero)
            {
                stack.Children.Add(CreateDockSeparator());
                stack.Children.Add(CreateDockMenuRow("🗕  Свернуть / Показать", delegate
                {
                    popup.IsOpen = false;
                    ShellController.Instance.LaunchOrFocusDockItem(item);
                }));
                stack.Children.Add(CreateDockMenuRow("✕  Закрыть окно", delegate
                {
                    popup.IsOpen = false;
                    NativeMethods.PostMessage(item.ActiveHwnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }));
            }

            if (!item.IsSeparator && !item.IsTrash && !item.IsAppsLauncher && !item.IsWindowSwitcher)
            {
                int curSpace = ShellController.Instance.Config.CurrentSpaceIndex;
                int assignedSpace = ShellController.Instance.GetAppSpaceAssignment(item.Name);
                stack.Children.Add(CreateDockSeparator());
                stack.Children.Add(CreateDockMenuRow("🖥  Назначить на: Рабочий стол " + (curSpace + 1).ToString(), delegate
                {
                    popup.IsOpen = false;
                    ShellController.Instance.AssignAppToSpace(item.Name, curSpace);
                }));
                stack.Children.Add(CreateDockMenuRow("🌐  Назначить на: Все рабочие столы", delegate
                {
                    popup.IsOpen = false;
                    ShellController.Instance.AssignAppToSpace(item.Name, -1);
                }));
                if (assignedSpace >= -1)
                {
                    stack.Children.Add(CreateDockMenuRow("⚪  Назначить на: Нет (по умолчанию)", delegate
                    {
                        popup.IsOpen = false;
                        ShellController.Instance.ClearAppSpaceAssignment(item.Name);
                    }));
                }
            }

            stack.Children.Add(CreateDockSeparator());
            stack.Children.Add(CreateDockMenuRow("⚙  Настройки Dock и лаунчера...", delegate
            {
                popup.IsOpen = false;
                ShellController.Instance.ShowLauncherSettings();
            }));

            outer.Child = stack;
            popup.Child = outer;
            _dockContextMenuPopup = popup;
            popup.IsOpen = true;
        }

        private FrameworkElement CreateDockSeparator()
        {
            return new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Margin = new Thickness(8, 4, 8, 4)
            };
        }

        private FrameworkElement CreateDockMenuRow(string text, Action onClick)
        {
            Border row = new Border
            {
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(10, 5, 10, 5),
                Background = Brushes.Transparent
            };
            row.Child = new TextBlock
            {
                Text = text,
                FontSize = 13,
                Foreground = Brushes.White
            };
            row.MouseEnter += delegate { row.Background = LiquidGlassTheme.AccentBlueBrush; };
            row.MouseLeave += delegate { row.Background = Brushes.Transparent; };
            row.MouseLeftButtonDown += delegate { if (onClick != null) onClick(); };
            return row;
        }

        private void OnRenderFrame(object sender, EventArgs e)
        {
            double now = ElapsedSeconds;
            double dt = now - _lastFrameTime;
            if (dt <= 0.0 || dt > 0.1) dt = 0.016;
            _lastFrameTime = now;

            NativeMethods.POINT pt;
            NativeMethods.GetCursorPos(out pt);

            PresentationSource src = PresentationSource.FromVisual(this);
            double dpiX = 1.0;
            double dpiY = 1.0;
            if (src != null && src.CompositionTarget != null)
            {
                dpiX = src.CompositionTarget.TransformToDevice.M11;
                dpiY = src.CompositionTarget.TransformToDevice.M22;
            }

            double cursorScreenX = pt.X / dpiX;
            double cursorScreenY = pt.Y / dpiY;

            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;

            double baseSize = BaseIconSize;
            double maxZoom = MaxIconZoom;

            double unmagnifiedRowWidth = 0.0;
            List<DockAppItem> items = ShellController.Instance.DockItems;
            for (int i = 0; i < items.Count; i++)
            {
                unmagnifiedRowWidth += items[i].IsSeparator ? 15.0 : (baseSize + ItemMarginX * 2.0);
            }
            double rowStartLeft = (screenW - unmagnifiedRowWidth) / 2.0;

            bool cursorWithinHorizontalBounds = (cursorScreenX >= rowStartLeft - 45.0) &&
                                                (cursorScreenX <= rowStartLeft + unmagnifiedRowWidth + 45.0);

            if (cursorScreenY >= screenH - 10.0 && cursorWithinHorizontalBounds)
            {
                _dockRevealedByMouse = true;
            }
            else if (cursorScreenY < screenH - 120.0 || !cursorWithinHorizontalBounds)
            {
                _dockRevealedByMouse = false;
            }

            bool popupOpen = (_dockContextMenuPopup != null && _dockContextMenuPopup.IsOpen);

            if (ShellController.Instance.DockAutoHide && _activeWindowIsMaximized && !_dockRevealedByMouse && !popupOpen)
            {
                _targetSlideY = baseSize + 38.0;
            }
            else
            {
                _targetSlideY = 0.0;
            }

            double slideLerp = 1.0 - Math.Exp(-16.0 * dt);
            _currentSlideY += (_targetSlideY - _currentSlideY) * slideLerp;
            _dockSlideTransform.Y = _currentSlideY;

            bool dockActiveHover = (_currentSlideY < 35.0) &&
                                   cursorWithinHorizontalBounds &&
                                   (cursorScreenY >= screenH - (baseSize + 48.0)) &&
                                   (cursorScreenY <= screenH);

            DockAppItem hoveredClosest = null;
            double closestDist = 9999.0;

            double totalShelfWidth = 22.0;
            double sizeLerp = 1.0 - Math.Exp(-25.0 * dt);
            double curBaseX = rowStartLeft;

            for (int i = 0; i < items.Count; i++)
            {
                DockAppItem item = items[i];
                if (item.IsSeparator)
                {
                    curBaseX += 15.0;
                    totalShelfWidth += 15.0;
                    continue;
                }

                double slotWidth = baseSize + ItemMarginX * 2.0;
                double baseCenterX = curBaseX + (slotWidth / 2.0);
                curBaseX += slotWidth;

                double dx = Math.Abs(cursorScreenX - baseCenterX);

                if (dockActiveHover && dx < 130.0)
                {
                    double norm = dx / 130.0;
                    double wave = (1.0 + Math.Cos(Math.PI * norm)) * 0.5;
                    wave = wave * wave;
                    item.TargetSize = baseSize + maxZoom * wave;
                    item.TargetLift = 9.0 * wave;

                    if (dx < closestDist && dx < 32.0)
                    {
                        closestDist = dx;
                        hoveredClosest = item;
                    }
                }
                else
                {
                    item.TargetSize = baseSize;
                    item.TargetLift = 0.0;
                }

                double bounceOffset = 0.0;
                double bounceElapsed = now - item.BounceStartTime;
                if (bounceElapsed >= 0.0 && bounceElapsed < 1.2)
                {
                    bounceOffset = Math.Abs(Math.Sin(bounceElapsed * Math.PI * 2.4)) * Math.Exp(-bounceElapsed * 1.8) * 24.0;
                }

                item.CurrentSize += (item.TargetSize - item.CurrentSize) * sizeLerp;
                item.CurrentLift += ((item.TargetLift + bounceOffset) - item.CurrentLift) * sizeLerp;

                double scale = (baseSize > 0) ? (item.CurrentSize / baseSize) : 1.0;
                if (item.IconScaleTransform != null)
                {
                    item.IconScaleTransform.ScaleX = scale;
                    item.IconScaleTransform.ScaleY = scale;
                }
                if (item.IconTranslateTransform != null)
                {
                    item.IconTranslateTransform.Y = -item.CurrentLift;
                }

                totalShelfWidth += item.CurrentSize + (ItemMarginX * 2.0);
            }

            double shelfW = Math.Max(200.0, totalShelfWidth);
            if (double.IsNaN(_glassShelf.Width) || Math.Abs(_glassShelf.Width - shelfW) > 0.5)
            {
                _glassShelf.Width = shelfW;
            }
            if (_glassShelf.Height != baseSize + 14)
            {
                _glassShelf.Height = baseSize + 14;
            }

            if (hoveredClosest != null && dockActiveHover && !popupOpen)
            {
                _tooltipText.Text = hoveredClosest.Name;
                if (_tooltipPill.Visibility != Visibility.Visible) _tooltipPill.Visibility = Visibility.Visible;
                Point centerPt = hoveredClosest.RootContainer.TranslatePoint(
                    new Point(hoveredClosest.RootContainer.ActualWidth / 2.0, 0), _outerRoot);
                double pillLeft = centerPt.X - (_tooltipPill.ActualWidth / 2.0);
                double pillBottom = 18.0 + hoveredClosest.CurrentSize + hoveredClosest.CurrentLift;
                _tooltipPill.Margin = new Thickness(pillLeft, 0, 0, pillBottom);
            }
            else
            {
                if (_tooltipPill.Visibility != Visibility.Hidden)
                    _tooltipPill.Visibility = Visibility.Hidden;
            }

            // Idle check: if mouse is outside dock and icons settled, STOP rendering loop to save 100% idle CPU
            bool anyActive = dockActiveHover || _dockRevealedByMouse || (Math.Abs(_currentSlideY - _targetSlideY) > 0.4);
            bool anyItemMoving = false;
            for (int i = 0; i < items.Count; i++)
            {
                DockAppItem it = items[i];
                if (Math.Abs(it.CurrentSize - baseSize) > 0.2 || Math.Abs(it.CurrentLift) > 0.2 || (now - it.BounceStartTime < 1.3))
                {
                    anyItemMoving = true;
                    break;
                }
            }

            if (!anyActive && !anyItemMoving && !popupOpen)
            {
                _currentSlideY = _targetSlideY;
                _dockSlideTransform.Y = _currentSlideY;
                double idleShelfW = Math.Max(200.0, unmagnifiedRowWidth + 24.0);
                if (double.IsNaN(_glassShelf.Width) || Math.Abs(_glassShelf.Width - idleShelfW) > 0.5)
                {
                    _glassShelf.Width = idleShelfW;
                }
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].CurrentSize = baseSize;
                    items[i].CurrentLift = 0.0;
                    if (items[i].IconScaleTransform != null)
                    {
                        items[i].IconScaleTransform.ScaleX = 1.0;
                        items[i].IconScaleTransform.ScaleY = 1.0;
                    }
                    if (items[i].IconTranslateTransform != null)
                    {
                        items[i].IconTranslateTransform.Y = 0.0;
                    }
                }
                if (_tooltipPill.Visibility != Visibility.Hidden) _tooltipPill.Visibility = Visibility.Hidden;
                StopRendering();
            }
        }

        private void OnScanRunningWindows(object sender, EventArgs e)
        {
            try
            {
                double now = ElapsedSeconds;
                Dictionary<string, IntPtr> procWindows = new Dictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, uint> procPids = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, string> procTitles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                IntPtr explorerFolderHwnd = IntPtr.Zero;
                uint selfPid = (uint)Process.GetCurrentProcess().Id;

                IntPtr fg = NativeMethods.GetForegroundWindow();
                bool fgMaximized = false;
                if (fg != IntPtr.Zero)
                {
                    StringBuilder fgCls = new StringBuilder(128);
                    NativeMethods.GetClassName(fg, fgCls, fgCls.Capacity);
                    string c = fgCls.ToString();
                    if (c != "Progman" && c != "WorkerW" && c != "Shell_TrayWnd")
                    {
                        fgMaximized = NativeMethods.IsZoomed(fg);
                    }
                }
                _activeWindowIsMaximized = fgMaximized;

                NativeMethods.EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
                {
                    if (!NativeMethods.IsWindowVisible(hWnd)) return true;

                    int cloaked = 0;
                    NativeMethods.DwmGetWindowAttribute(hWnd, 14, out cloaked, sizeof(int));
                    if (cloaked != 0) return true;

                    int exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
                    if ((exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0) return true;
                    if (NativeMethods.GetWindow(hWnd, 4) != IntPtr.Zero) return true;

                    StringBuilder sbTitle = new StringBuilder(256);
                    NativeMethods.GetWindowText(hWnd, sbTitle, sbTitle.Capacity);
                    string winTitle = sbTitle.ToString();

                    StringBuilder sbClass = new StringBuilder(128);
                    NativeMethods.GetClassName(hWnd, sbClass, sbClass.Capacity);
                    string cls = sbClass.ToString();
                    if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd" ||
                        cls == "Windows.UI.Core.CoreWindow" || cls == "ApplicationFrameWindow" || cls == "CEF-OSC-WIDGET")
                        return true;

                    if (cls == "CabinetWClass" || cls == "ExploreWClass")
                    {
                        if (explorerFolderHwnd == IntPtr.Zero)
                            explorerFolderHwnd = hWnd;
                        return true;
                    }

                    uint pid;
                    NativeMethods.GetWindowThreadProcessId(hWnd, out pid);
                    if (pid == selfPid) return true;

                    string pn = GetFastProcessName(pid, now);
                    if (string.IsNullOrEmpty(pn)) return true;
                    if (string.IsNullOrEmpty(winTitle)) winTitle = pn;

                    if (string.Equals(pn, "seelen-ui", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(pn, "TextInputHost", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(pn, "SearchHost", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(pn, "ShellExperienceHost", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(pn, "StartMenuExperienceHost", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    if (!procWindows.ContainsKey(pn))
                    {
                        procWindows[pn] = hWnd;
                        procPids[pn] = pid;
                        procTitles[pn] = winTitle;
                    }
                    return true;
                }, IntPtr.Zero);

                NativeMethods.SHQUERYRBINFO rbInfo = new NativeMethods.SHQUERYRBINFO();
                rbInfo.cbSize = Marshal.SizeOf(rbInfo);
                bool trashHasItems = false;
                if (NativeMethods.SHQueryRecycleBin(null, ref rbInfo) == 0 && rbInfo.i64NumItems > 0)
                {
                    trashHasItems = true;
                }

                HashSet<string> claimedProcs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                List<DockAppItem> items = ShellController.Instance.DockItems;

                for (int i = 0; i < items.Count; i++)
                {
                    DockAppItem item = items[i];
                    if (item.IsSeparator || item.IsDynamic) continue;
                    if (item.IsTrash)
                    {
                        string targetTrashIcon = trashHasItems ? "trash-full.png" : "trash.png";
                        if (item.IconFile != targetTrashIcon)
                        {
                            item.IconFile = targetTrashIcon;
                            if (item.IconImage != null)
                                item.IconImage.Source = ShellController.Instance.GetIcon(targetTrashIcon);
                        }
                        continue;
                    }
                    if (item.IsExplorer)
                    {
                        item.ActiveHwnd = explorerFolderHwnd;
                        item.IsRunning = (explorerFolderHwnd != IntPtr.Zero);
                        if (item.RunningDot != null)
                        {
                            item.RunningDot.Visibility = item.IsRunning ? Visibility.Visible : Visibility.Hidden;
                        }
                        claimedProcs.Add("explorer");
                        continue;
                    }

                    IntPtr matchedHwnd = IntPtr.Zero;
                    if (item.ProcessNames != null)
                    {
                        foreach (string pn in item.ProcessNames)
                        {
                            claimedProcs.Add(pn);
                            if (matchedHwnd == IntPtr.Zero && procWindows.TryGetValue(pn, out matchedHwnd))
                            {
                            }
                        }
                    }
                    item.ActiveHwnd = matchedHwnd;
                    item.IsRunning = (matchedHwnd != IntPtr.Zero);
                    if (item.RunningDot != null)
                    {
                        item.RunningDot.Visibility = item.IsRunning ? Visibility.Visible : Visibility.Hidden;
                    }
                }

                for (int i = items.Count - 1; i >= 0; i--)
                {
                    DockAppItem dyn = items[i];
                    if (!dyn.IsDynamic) continue;
                    string dynProc = dyn.ProcessNames[0];
                    IntPtr latestHwnd;
                    if (procWindows.TryGetValue(dynProc, out latestHwnd))
                    {
                        dyn.ActiveHwnd = latestHwnd;
                        claimedProcs.Add(dynProc);
                    }
                    else
                    {
                        _itemsPanel.Children.Remove(dyn.RootContainer);
                        items.RemoveAt(i);
                    }
                }

                int sepIndex = -1;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].IsSeparator)
                    {
                        sepIndex = i;
                        break;
                    }
                }
                if (sepIndex < 0) sepIndex = items.Count;

                foreach (KeyValuePair<string, IntPtr> kv in procWindows)
                {
                    string pn = kv.Key;
                    if (claimedProcs.Contains(pn)) continue;
                    if (items.Count >= 20) break;

                    uint pid = procPids[pn];
                    DockAppItem dynItem = new DockAppItem
                    {
                        Id = "dyn_" + pn,
                        Name = pn,
                        IconFile = ShellController.MapProcessToKnownIcon(pn) ?? "finder.png",
                        CustomImageSource = ShellController.Instance.ResolveDynamicIcon(pn, pid),
                        LaunchCommand = pn,
                        LaunchArgs = "",
                        ProcessNames = new string[] { pn },
                        IsPinned = false,
                        IsDynamic = true,
                        IsRunning = true,
                        ActiveHwnd = kv.Value
                    };

                    FrameworkElement elem = CreateDockItemElement(dynItem);
                    items.Insert(sepIndex, dynItem);
                    _itemsPanel.Children.Insert(sepIndex, elem);
                    sepIndex++;
                }
            }
            catch { }
        }
    }

    // =========================================================================
    // 3. DESKTOP WIDGETS LAYER (Настольные виджеты рабочего стола)
    // =========================================================================
}
