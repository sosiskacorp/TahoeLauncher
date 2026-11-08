using System;
using System.Windows;
using System.Windows.Media;

namespace MacOSTahoe.Theme
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
}
