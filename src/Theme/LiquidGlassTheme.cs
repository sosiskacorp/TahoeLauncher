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
    public static class LiquidGlassTheme
    {
        public static readonly FontFamily UiFont = new FontFamily("-apple-system, SF Pro Display, SF Pro Text, Segoe UI Variable Display, Segoe UI");

        public static LinearGradientBrush CreateGlassBrush(byte alphaTop, byte alphaBottom)
        {
            LinearGradientBrush b = new LinearGradientBrush();
            b.StartPoint = new Point(0, 0);
            b.EndPoint = new Point(1, 1);
            b.GradientStops.Add(new GradientStop(Color.FromArgb(alphaTop, 40, 40, 52), 0.0));
            b.GradientStops.Add(new GradientStop(Color.FromArgb(alphaBottom, 20, 20, 28), 1.0));
            b.Freeze();
            return b;
        }

        public static LinearGradientBrush CreateSpecularBorderBrush()
        {
            LinearGradientBrush b = new LinearGradientBrush();
            b.StartPoint = new Point(0, 0);
            b.EndPoint = new Point(0, 1);
            b.GradientStops.Add(new GradientStop(Color.FromArgb(118, 255, 255, 255), 0.0));
            b.GradientStops.Add(new GradientStop(Color.FromArgb(40, 255, 255, 255), 1.0));
            b.Freeze();
            return b;
        }

        public static Brush AccentBlueBrush
        {
            get
            {
                SolidColorBrush b = new SolidColorBrush(Color.FromRgb(10, 132, 255));
                b.Freeze();
                return b;
            }
        }
    }

    // =========================================================================
    // 1. TOP MENU BAR WINDOW (Tahoe Liquid Glass)
    // =========================================================================
}
