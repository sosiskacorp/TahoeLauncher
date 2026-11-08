using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MacOSTahoe
{
    public static class AnimationHelper
    {
        public static readonly IEasingFunction DefaultEaseOut = new CubicEase { EasingMode = EasingMode.EaseOut };
        public static readonly IEasingFunction DefaultEaseIn = new CubicEase { EasingMode = EasingMode.EaseIn };
        public static readonly IEasingFunction DefaultEaseInOut = new CubicEase { EasingMode = EasingMode.EaseInOut };

        public static void FadeIn(UIElement elem, double durationMs = 180, Action onCompleted = null)
        {
            if (elem == null) return;
            DoubleAnimation anim = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = DefaultEaseOut
            };
            if (onCompleted != null)
            {
                anim.Completed += delegate { onCompleted(); };
            }
            elem.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        public static void FadeOut(UIElement elem, double durationMs = 140, Action onCompleted = null)
        {
            if (elem == null) return;
            DoubleAnimation anim = new DoubleAnimation
            {
                From = elem.Opacity,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = DefaultEaseIn
            };
            if (onCompleted != null)
            {
                anim.Completed += delegate { onCompleted(); };
            }
            elem.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        public static void ScaleAndFadeIn(UIElement elem, ScaleTransform st, double fromScale = 0.94, double durationMs = 200, Action onCompleted = null)
        {
            if (elem == null || st == null) return;
            FadeIn(elem, durationMs, onCompleted);

            DoubleAnimation scaleAnim = new DoubleAnimation
            {
                From = fromScale,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = DefaultEaseOut
            };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
        }

        public static void ScaleAndFadeOut(UIElement elem, ScaleTransform st, double toScale = 0.95, double durationMs = 140, Action onCompleted = null)
        {
            if (elem == null || st == null) return;
            FadeOut(elem, durationMs, onCompleted);

            DoubleAnimation scaleAnim = new DoubleAnimation
            {
                From = st.ScaleX,
                To = toScale,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = DefaultEaseIn
            };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
        }

        public static void SlideDownAndFadeIn(UIElement elem, TranslateTransform tt, double fromY = -18.0, double durationMs = 180, Action onCompleted = null)
        {
            if (elem == null || tt == null) return;
            FadeIn(elem, durationMs, onCompleted);

            DoubleAnimation slideAnim = new DoubleAnimation
            {
                From = fromY,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = DefaultEaseOut
            };
            tt.BeginAnimation(TranslateTransform.YProperty, slideAnim);
        }

        public static void SlideUpAndFadeOut(UIElement elem, TranslateTransform tt, double toY = -14.0, double durationMs = 130, Action onCompleted = null)
        {
            if (elem == null || tt == null) return;
            FadeOut(elem, durationMs, onCompleted);

            DoubleAnimation slideAnim = new DoubleAnimation
            {
                From = tt.Y,
                To = toY,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = DefaultEaseIn
            };
            tt.BeginAnimation(TranslateTransform.YProperty, slideAnim);
        }

        public static void SlideRightAndFadeIn(UIElement elem, TranslateTransform tt, double fromX = -80.0, double durationMs = 240, Action onCompleted = null)
        {
            if (elem == null || tt == null) return;
            FadeIn(elem, durationMs, onCompleted);

            DoubleAnimation slideAnim = new DoubleAnimation
            {
                From = fromX,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = DefaultEaseOut
            };
            tt.BeginAnimation(TranslateTransform.XProperty, slideAnim);
        }

        public static void SlideLeftAndFadeOut(UIElement elem, TranslateTransform tt, double toX = -80.0, double durationMs = 180, Action onCompleted = null)
        {
            if (elem == null || tt == null) return;
            FadeOut(elem, durationMs, onCompleted);

            DoubleAnimation slideAnim = new DoubleAnimation
            {
                From = tt.X,
                To = toX,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = DefaultEaseIn
            };
            tt.BeginAnimation(TranslateTransform.XProperty, slideAnim);
        }
    }
}
