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
using MacOSTahoe.Core;
using MacOSTahoe.Models;
using MacOSTahoe.Native;
using MacOSTahoe.Theme;

namespace MacOSTahoe.Views
{
    public class LaunchpadWindow : Window
    {
        private TextBox _searchBox;
        private WrapPanel _appsPanel;
        private ScrollViewer _scrollViewer;
        private StackPanel _categoryTabs;
        private string _selectedCategory = "Все";

        public LaunchpadWindow()
        {
            Title = "LaunchpadWindow";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;

            // Start strictly hidden and non-blocking
            Visibility = Visibility.Collapsed;
            IsHitTestVisible = false;

            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;

            BuildUi();

            SourceInitialized += (s, e) =>
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                DwmBlurHelper.ApplyBlur(hwnd, true);
                DwmBlurHelper.MakeToolWindow(hwnd);
            };

            ShellController.Instance.AppCatalog.OnCatalogUpdated += () =>
            {
                if (IsVisible)
                {
                    PopulateApps();
                }
            };

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    HideLaunchpad();
                }
            };
        }

        private void BuildUi()
        {
            var root = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(215, 12, 12, 16))
            };
            root.MouseLeftButtonUp += (s, e) =>
            {
                if (e.OriginalSource == root || e.OriginalSource == _scrollViewer)
                {
                    HideLaunchpad();
                }
            };

            var mainStack = new Grid();
            mainStack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(110) }); // Top search & categories
            mainStack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Apps grid

            // --- Top bar: Search + Categories ---
            var topPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 30, 0, 10)
            };

            // Search box container
            var searchBorder = new Border
            {
                Width = 360,
                Height = 36,
                CornerRadius = new CornerRadius(18),
                Background = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 0, 14, 0)
            };

            var searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconTxt = new TextBlock
            {
                Text = "🔍",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(iconTxt, 0);
            searchGrid.Children.Add(iconTxt);

            _searchBox = new TextBox
            {
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 14,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                CaretBrush = Brushes.White
            };
            _searchBox.TextChanged += (s, e) => PopulateApps();
            Grid.SetColumn(_searchBox, 1);
            searchGrid.Children.Add(_searchBox);

            searchBorder.Child = searchGrid;
            topPanel.Children.Add(searchBorder);

            // Category selector tabs
            _categoryTabs = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 0)
            };
            BuildCategoryTabs();
            topPanel.Children.Add(_categoryTabs);

            Grid.SetRow(topPanel, 0);
            mainStack.Children.Add(topPanel);

            // --- Apps ScrollViewer & WrapPanel ---
            _scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(60, 10, 60, 40)
            };

            _appsPanel = new WrapPanel
            {
                ItemWidth = 120,
                ItemHeight = 120,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top
            };

            _scrollViewer.Content = _appsPanel;
            Grid.SetRow(_scrollViewer, 1);
            mainStack.Children.Add(_scrollViewer);

            root.Children.Add(mainStack);
            Content = root;
        }

        private void BuildCategoryTabs()
        {
            _categoryTabs.Children.Clear();
            string[] categories = new[] { "Все", "Разработка", "Интернет", "Мультимедиа", "Офис", "Утилиты", "Система" };

            foreach (var cat in categories)
            {
                bool isSelected = (cat == _selectedCategory);
                var btn = new Border
                {
                    Background = isSelected ? LiquidGlassTheme.AccentBlueBrush : new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(12, 4, 12, 4),
                    Margin = new Thickness(4, 0, 4, 0),
                    Cursor = Cursors.Hand
                };

                var txt = new TextBlock
                {
                    Text = cat,
                    FontFamily = LiquidGlassTheme.UiFont,
                    FontSize = 12,
                    FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = Brushes.White
                };
                btn.Child = txt;

                btn.MouseLeftButtonUp += (s, e) =>
                {
                    _selectedCategory = cat;
                    BuildCategoryTabs();
                    PopulateApps();
                };

                _categoryTabs.Children.Add(btn);
            }
        }

        private void PopulateApps()
        {
            _appsPanel.Children.Clear();

            string query = _searchBox.Text.Trim().ToLowerInvariant();
            var allApps = ShellController.Instance.AppCatalog.GetCatalog();

            var filtered = allApps.Where(a =>
            {
                if (_selectedCategory != "Все" && !string.Equals(a.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if (!string.IsNullOrEmpty(query))
                {
                    return a.Title.ToLowerInvariant().Contains(query);
                }
                return true;
            }).ToList();

            foreach (var app in filtered)
            {
                var card = CreateAppCard(app);
                _appsPanel.Children.Add(card);
            }
        }

        private UIElement CreateAppCard(AppCatalogEntry app)
        {
            var border = new Border
            {
                Width = 110,
                Height = 110,
                CornerRadius = new CornerRadius(12),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Margin = new Thickness(5)
            };

            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var img = new Image
            {
                Width = 58,
                Height = 58,
                Stretch = Stretch.Uniform,
                Source = app.IconSource
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
            stack.Children.Add(img);

            var title = new TextBlock
            {
                Text = app.Title,
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 12,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(4, 6, 4, 0),
                MaxWidth = 100
            };
            stack.Children.Add(title);

            border.Child = stack;

            // Hover styling
            border.MouseEnter += (s, e) =>
            {
                border.Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255));
            };
            border.MouseLeave += (s, e) =>
            {
                border.Background = Brushes.Transparent;
            };

            // Click to launch
            border.MouseLeftButtonUp += (s, e) =>
            {
                HideLaunchpad();
                ShellController.Instance.LaunchApp(app.Cmd, app.Args);
            };

            // Context menu
            border.MouseRightButtonUp += (s, e) =>
            {
                ShowAppContextMenu(border, app);
            };

            return border;
        }

        private void ShowAppContextMenu(UIElement target, AppCatalogEntry app)
        {
            ContextMenu cm = new ContextMenu();

            var miOpen = new MenuItem { Header = "Открыть" };
            miOpen.Click += (s, e) =>
            {
                HideLaunchpad();
                ShellController.Instance.LaunchApp(app.Cmd, app.Args);
            };
            cm.Items.Add(miOpen);

            var miAdmin = new MenuItem { Header = "Запуск от имени администратора" };
            miAdmin.Click += (s, e) =>
            {
                HideLaunchpad();
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = app.Cmd,
                        Arguments = app.Args ?? "",
                        Verb = "runas",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                catch { }
            };
            cm.Items.Add(miAdmin);

            cm.Items.Add(new Separator());

            var miPin = new MenuItem { Header = "Закрепить в Dock" };
            miPin.Click += (s, e) =>
            {
                var newConfig = new DockItemConfig
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = app.Title,
                    LaunchCommand = app.Cmd,
                    LaunchArgs = app.Args,
                    ProcessNames = new[] { Path.GetFileNameWithoutExtension(app.Cmd) }
                };
                ShellController.Instance.Config.PinnedDockItems.Add(newConfig);
                ShellController.Instance.SaveConfig();
                if (ShellController.Instance.Dock != null)
                {
                    ShellController.Instance.Dock.LoadDockItems();
                }
            };
            cm.Items.Add(miPin);

            if (File.Exists(app.Cmd))
            {
                var miFolder = new MenuItem { Header = "Открыть расположение файла" };
                miFolder.Click += (s, e) =>
                {
                    HideLaunchpad();
                    Process.Start("explorer.exe", "/select,\"" + app.Cmd + "\"");
                };
                cm.Items.Add(miFolder);
            }

            cm.IsOpen = true;
        }

        public void ShowLaunchpad()
        {
            Visibility = Visibility.Visible;
            IsHitTestVisible = true;
            _searchBox.Text = "";
            PopulateApps();
            Activate();
            _searchBox.Focus();
        }

        public void HideLaunchpad()
        {
            Visibility = Visibility.Collapsed;
            IsHitTestVisible = false;
        }
    }
}
