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
    public class ControlCenterWindow : Window
    {
        private Border _darkModeTile;
        private TextBlock _darkModeSub;
        private Border _iconsTile;
        private TextBlock _iconsSub;
        private Border _widgetsTile;
        private TextBlock _widgetsSub;
        private Border _dockHideTile;
        private TextBlock _dockHideSub;
        private Border _stageManagerTile;
        private TextBlock _stageManagerSub;

        public ControlCenterWindow()
        {
            Title = "Пункт управления";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Width = 350;
            Height = 555;
            Background = Brushes.Transparent;
            FontFamily = LiquidGlassTheme.UiFont;

            BuildUI();

            Deactivated += delegate(object s, EventArgs e)
            {
                Hide();
            };
        }

        private void BuildUI()
        {
            Border card = new Border();
            card.Margin = new Thickness(12);
            card.CornerRadius = new CornerRadius(24);
            card.Background = LiquidGlassTheme.CreateGlassBrush(226, 240);
            card.BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush();
            card.BorderThickness = new Thickness(1);
            card.Padding = new Thickness(14);
            card.Effect = new DropShadowEffect
            {
                BlurRadius = 28,
                ShadowDepth = 8,
                Opacity = 0.6,
                Color = Colors.Black
            };

            StackPanel root = new StackPanel();

            Grid topGrid = new Grid();
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border connCard = CreateGlassTile();
            connCard.Padding = new Thickness(12, 10, 12, 10);
            StackPanel connStack = new StackPanel();
            connStack.Children.Add(CreateConnRow("Wi-Fi", "Подключено", true, delegate() { ShellController.RunProcess("explorer.exe", "ms-settings:network-wifi"); }));
            connStack.Children.Add(CreateConnRow("Bluetooth", "Вкл.", true, delegate() { ShellController.RunProcess("explorer.exe", "ms-settings:bluetooth"); }));
            connStack.Children.Add(CreateConnRow("Лаунчер", "Настройки", true, delegate() { Hide(); ShellController.Instance.ShowLauncherSettings(); }));
            connCard.Child = connStack;
            Grid.SetColumn(connCard, 0);
            topGrid.Children.Add(connCard);

            StackPanel rightCol = new StackPanel();
            _darkModeTile = CreateActionTile("Оформление", "Тёмная тема", true, out _darkModeSub, delegate()
            {
                ShellController.Instance.ToggleDarkMode();
                RefreshState();
            });
            _darkModeTile.Margin = new Thickness(0, 0, 0, 10);
            rightCol.Children.Add(_darkModeTile);

            _iconsTile = CreateActionTile("Значки стола", "Показаны", true, out _iconsSub, delegate()
            {
                NativeMethods.ToggleDesktopIcons();
                RefreshState();
            });
            rightCol.Children.Add(_iconsTile);

            Grid.SetColumn(rightCol, 2);
            topGrid.Children.Add(rightCol);
            root.Children.Add(topGrid);

            _widgetsTile = CreateActionTile("Виджеты рабочего стола", "Включены", true, out _widgetsSub, delegate()
            {
                ShellController.Instance.Config.ShowDesktopWidgets = !ShellController.Instance.Config.ShowDesktopWidgets;
                ShellController.Instance.SaveConfig();
                ShellController.Instance.ApplyModuleVisibility();
                RefreshState();
            });
            _widgetsTile.Margin = new Thickness(0, 10, 0, 0);
            root.Children.Add(_widgetsTile);

            _dockHideTile = CreateActionTile("Режим Dock", "Скрывать в полноэкранных окнах", true, out _dockHideSub, delegate()
            {
                ShellController.Instance.DockAutoHide = !ShellController.Instance.DockAutoHide;
                ShellController.Instance.SaveConfig();
                RefreshState();
            });
            _dockHideTile.Margin = new Thickness(0, 10, 0, 0);
            root.Children.Add(_dockHideTile);

            _stageManagerTile = CreateActionTile("Постановщик (Stage Manager)", "Выключен", true, out _stageManagerSub, delegate()
            {
                ShellController.Instance.ToggleStageManager();
                RefreshState();
            });
            _stageManagerTile.Margin = new Thickness(0, 10, 0, 0);
            root.Children.Add(_stageManagerTile);

            Border dispCard = CreateGlassTile();
            dispCard.Margin = new Thickness(0, 10, 0, 0);
            dispCard.Padding = new Thickness(12, 10, 12, 10);
            StackPanel dispStack = new StackPanel();
            dispStack.Children.Add(new TextBlock
            {
                Text = "Яркость экрана",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 6)
            });
            Slider brightSlider = new Slider
            {
                Minimum = 25,
                Maximum = 100,
                Value = 100,
                Cursor = Cursors.Hand
            };
            brightSlider.ValueChanged += delegate(object s, RoutedPropertyChangedEventArgs<double> e)
            {
                if (ShellController.Instance != null)
                    ShellController.Instance.SetBrightnessOverlay(e.NewValue);
            };
            dispStack.Children.Add(brightSlider);
            dispCard.Child = dispStack;
            root.Children.Add(dispCard);

            Border soundCard = CreateGlassTile();
            soundCard.Margin = new Thickness(0, 10, 0, 0);
            soundCard.Padding = new Thickness(12, 10, 12, 10);
            StackPanel soundStack = new StackPanel();
            soundStack.Children.Add(new TextBlock
            {
                Text = "Звук и мультимедиа",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 6)
            });

            StackPanel mediaBtns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            mediaBtns.Children.Add(CreateMediaChip("⏮", delegate() { NativeMethods.keybd_event(0xB1, 0, 0, UIntPtr.Zero); }));
            mediaBtns.Children.Add(CreateMediaChip("⏯ Пауза/Пуск", delegate() { NativeMethods.keybd_event(0xB3, 0, 0, UIntPtr.Zero); }));
            mediaBtns.Children.Add(CreateMediaChip("⏭", delegate() { NativeMethods.keybd_event(0xB0, 0, 0, UIntPtr.Zero); }));
            mediaBtns.Children.Add(CreateMediaChip("🔊-", delegate() { NativeMethods.keybd_event(0xAE, 0, 0, UIntPtr.Zero); }));
            mediaBtns.Children.Add(CreateMediaChip("🔊+", delegate() { NativeMethods.keybd_event(0xAF, 0, 0, UIntPtr.Zero); }));
            soundStack.Children.Add(mediaBtns);
            soundCard.Child = soundStack;
            root.Children.Add(soundCard);

            card.Child = root;
            Content = card;
        }

        private Border CreateGlassTile()
        {
            Border b = new Border();
            b.CornerRadius = new CornerRadius(16);
            b.Background = new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));
            b.BorderBrush = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255));
            b.BorderThickness = new Thickness(1);
            return b;
        }

        private FrameworkElement CreateConnRow(string title, string sub, bool active, Action onClick)
        {
            Border row = new Border();
            row.CornerRadius = new CornerRadius(10);
            row.Padding = new Thickness(4, 5, 4, 5);
            row.Background = Brushes.Transparent;
            row.Cursor = Cursors.Hand;

            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            Border dot = new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(13),
                Background = active ? LiquidGlassTheme.AccentBlueBrush : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                Margin = new Thickness(0, 0, 8, 0)
            };
            TextBlock iconTxt = new TextBlock
            {
                Text = title.Substring(0, 1),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            dot.Child = iconTxt;
            sp.Children.Add(dot);

            StackPanel labels = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            labels.Children.Add(new TextBlock { Text = title, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
            labels.Children.Add(new TextBlock { Text = sub, FontSize = 10.5, Foreground = new SolidColorBrush(Color.FromArgb(170, 235, 235, 245)) });
            sp.Children.Add(labels);

            row.Child = sp;
            row.MouseLeftButtonDown += delegate { if (onClick != null) onClick(); };
            return row;
        }

        private Border CreateActionTile(string title, string sub, bool active, out TextBlock subBlock, Action onClick)
        {
            Border tile = CreateGlassTile();
            tile.Padding = new Thickness(12, 9, 12, 9);
            tile.Cursor = Cursors.Hand;

            StackPanel sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = title, FontSize = 12.5, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
            subBlock = new TextBlock { Text = sub, FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(185, 235, 235, 245)), Margin = new Thickness(0, 2, 0, 0) };
            sp.Children.Add(subBlock);
            tile.Child = sp;

            tile.MouseLeftButtonDown += delegate { if (onClick != null) onClick(); };
            return tile;
        }

        private Border CreateMediaChip(string text, Action onClick)
        {
            Border chip = new Border();
            chip.CornerRadius = new CornerRadius(8);
            chip.Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255));
            chip.Padding = new Thickness(9, 5, 9, 5);
            chip.Margin = new Thickness(3, 0, 3, 0);
            chip.Cursor = Cursors.Hand;
            chip.Child = new TextBlock { Text = text, Foreground = Brushes.White, FontSize = 11.5, FontWeight = FontWeights.SemiBold };
            chip.MouseLeftButtonDown += delegate { if (onClick != null) onClick(); };
            return chip;
        }

        public void RefreshState()
        {
            bool dark = ShellController.Instance.IsDarkMode;
            _darkModeSub.Text = dark ? "Тёмная тема" : "Светлая тема";
            _darkModeTile.Background = dark
                ? new SolidColorBrush(Color.FromArgb(115, 10, 132, 255))
                : new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));

            bool iconsVis = NativeMethods.AreDesktopIconsVisible();
            _iconsSub.Text = iconsVis ? "Показаны" : "Скрыты";
            _iconsTile.Background = iconsVis
                ? new SolidColorBrush(Color.FromArgb(115, 10, 132, 255))
                : new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));

            bool widgetsVis = ShellController.Instance.Config.ShowDesktopWidgets;
            _widgetsSub.Text = widgetsVis ? "Показаны на рабочем столе" : "Скрыты";
            _widgetsTile.Background = widgetsVis
                ? new SolidColorBrush(Color.FromArgb(115, 10, 132, 255))
                : new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));

            bool autoHide = ShellController.Instance.DockAutoHide;
            _dockHideSub.Text = autoHide ? "Скрывать в полноэкранных окнах" : "Всегда показывать поверх окон";
            _dockHideTile.Background = autoHide
                ? new SolidColorBrush(Color.FromArgb(115, 10, 132, 255))
                : new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));

            bool stageActive = ShellController.Instance.Config.EnableStageManager;
            if (_stageManagerSub != null)
            {
                _stageManagerSub.Text = stageActive ? "Включён (боковая панель)" : "Выключен";
                _stageManagerTile.Background = stageActive
                    ? new SolidColorBrush(Color.FromArgb(115, 10, 132, 255))
                    : new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));
            }
        }
    }

    // =========================================================================
    // 5. OMNI-LAUNCHER & COMMAND PALETTE (Alt+Space / Быстрый поиск)
    // =========================================================================
}
