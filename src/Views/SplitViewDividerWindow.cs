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
    public class SplitViewDividerWindow : Window
    {
        public IntPtr LeftHwnd { get; set; }
        public IntPtr RightHwnd { get; set; }
        public double SplitRatio { get; set; }

        private bool _isDragging = false;
        private Point _dragStartPoint;
        private double _startRatio;

        public SplitViewDividerWindow()
        {
            Title = "SplitViewDivider";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Background = Brushes.Transparent;
            Cursor = Cursors.SizeWE;
            SplitRatio = 0.5;

            Width = 10;
            BuildUI();

            Visibility = Visibility.Collapsed;

            SourceInitialized += delegate
            {
                NativeMethods.MakeToolWindow(this, true);
            };

            MouseLeftButtonDown += OnMouseDown;
            MouseLeftButtonUp += OnMouseUp;
            MouseMove += OnMouseMove;
            MouseDoubleClick += OnMouseDoubleClick;
        }

        private void BuildUI()
        {
            Border root = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 20, 20, 28)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1, 0, 1, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            Border pill = new Border
            {
                Width = 4,
                Height = 44,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            root.Child = pill;
            Content = root;
        }

        public void ApplyLayout()
        {
            int screenW = (int)SystemParameters.PrimaryScreenWidth;
            int screenH = (int)SystemParameters.PrimaryScreenHeight;
            int topBarH = (ShellController.Instance.TopBar != null && ShellController.Instance.Config.ShowTopBar) ? 32 : 0;
            int dockH = (ShellController.Instance.Dock != null && ShellController.Instance.Config.ShowDock && !ShellController.Instance.Config.DockAutoHide) ? 76 : 0;
            int usableH = screenH - topBarH - dockH;

            int splitX = (int)(screenW * SplitRatio);

            // Update divider window position
            Left = splitX - 5;
            Top = topBarH;
            Height = usableH;
            Width = 10;

            // Update left window
            if (LeftHwnd != IntPtr.Zero && NativeMethods.IsWindow(LeftHwnd))
            {
                if (NativeMethods.IsIconic(LeftHwnd)) NativeMethods.ShowWindow(LeftHwnd, NativeMethods.SW_RESTORE);
                NativeMethods.MoveWindow(LeftHwnd, 0, topBarH, splitX - 2, usableH, true);
            }

            // Update right window
            if (RightHwnd != IntPtr.Zero && NativeMethods.IsWindow(RightHwnd))
            {
                if (NativeMethods.IsIconic(RightHwnd)) NativeMethods.ShowWindow(RightHwnd, NativeMethods.SW_RESTORE);
                NativeMethods.MoveWindow(RightHwnd, splitX + 2, topBarH, screenW - splitX - 2, usableH, true);
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this);
            _startRatio = SplitRatio;
            CaptureMouse();
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ReleaseMouseCapture();

                // If dragged near edges, exit Split View
                if (SplitRatio < 0.15 || SplitRatio > 0.85)
                {
                    ShellController.Instance.ExitSplitView();
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                NativeMethods.POINT pt;
                if (NativeMethods.GetCursorPos(out pt))
                {
                    double screenW = SystemParameters.PrimaryScreenWidth;
                    double newRatio = pt.X / screenW;
                    if (newRatio >= 0.20 && newRatio <= 0.80)
                    {
                        SplitRatio = newRatio;
                        ApplyLayout();
                    }
                }
            }
        }

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to 50/50 on double click
            SplitRatio = 0.5;
            ApplyLayout();
        }
    }
}
