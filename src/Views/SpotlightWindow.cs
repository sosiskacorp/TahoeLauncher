using System;
using System.Collections.Generic;
using System.Data;
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
    public class SpotlightWindow : Window
    {
        private TextBox _txtSearch;
        private StackPanel _resultsStack;
        private ScrollViewer _scrollViewer;
        private List<SpotlightResultItem> _currentItems = new List<SpotlightResultItem>();
        private int _selectedIndex = -1;

        public SpotlightWindow()
        {
            Title = "SpotlightWindow";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;

            Visibility = Visibility.Collapsed;
            IsHitTestVisible = false;

            Width = 640;
            Height = 440;
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2.0;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 3.0;

            BuildUi();

            SourceInitialized += (s, e) =>
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                DwmBlurHelper.ApplyBlur(hwnd, true);
                DwmBlurHelper.MakeToolWindow(hwnd);
            };

            Deactivated += (s, e) =>
            {
                HideSpotlight();
            };

            KeyDown += OnWindowKeyDown;
        }

        private void BuildUi()
        {
            var glass = new Border
            {
                Background = LiquidGlassTheme.CreateGlassBrush(190, 230),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(14)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Search input
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Results

            // Search Header
            var searchBorder = new Border
            {
                Height = 48,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Padding = new Thickness(12, 0, 12, 0),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconTxt = new TextBlock
            {
                Text = "🔍",
                FontSize = 18,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            Grid.SetColumn(iconTxt, 0);
            headerGrid.Children.Add(iconTxt);

            _txtSearch = new TextBox
            {
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 18,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                CaretBrush = Brushes.White
            };
            _txtSearch.TextChanged += (s, e) => UpdateResults();
            Grid.SetColumn(_txtSearch, 1);
            headerGrid.Children.Add(_txtSearch);

            searchBorder.Child = headerGrid;
            Grid.SetRow(searchBorder, 0);
            grid.Children.Add(searchBorder);

            // Results List
            _scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            _resultsStack = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            _scrollViewer.Content = _resultsStack;
            Grid.SetRow(_scrollViewer, 1);
            grid.Children.Add(_scrollViewer);

            glass.Child = grid;
            Content = glass;
        }

        private void UpdateResults()
        {
            _resultsStack.Children.Clear();
            _currentItems.Clear();
            _selectedIndex = -1;

            string query = _txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            // 1. Check Terminal command prefix '>'
            if (query.StartsWith(">"))
            {
                string cmd = query.Substring(1).Trim();
                var item = new SpotlightResultItem
                {
                    Title = "Выполнить в терминале: " + cmd,
                    Subtitle = "Запуск через cmd /k",
                    Badge = "Команда",
                    OnExecute = () => Process.Start("cmd.exe", "/k " + cmd)
                };
                AddResult(item);
            }

            // 2. Check Math Expression
            string mathResult = TryEvaluateMath(query);
            if (!string.IsNullOrEmpty(mathResult))
            {
                var item = new SpotlightResultItem
                {
                    Title = "= " + mathResult,
                    Subtitle = "Калькулятор (Enter — скопировать в буфер)",
                    Badge = "Вычисление",
                    OnExecute = () => Clipboard.SetText(mathResult)
                };
                AddResult(item);
            }

            // 3. Search Installed Apps
            string qLower = query.ToLowerInvariant();
            var apps = ShellController.Instance.AppCatalog.GetCatalog()
                .Where(a => a.Title.ToLowerInvariant().Contains(qLower) || a.Category.ToLowerInvariant().Contains(qLower))
                .Take(7)
                .ToList();

            foreach (var app in apps)
            {
                var item = new SpotlightResultItem
                {
                    Title = app.Title,
                    Subtitle = app.Category + (!string.IsNullOrEmpty(app.Cmd) ? " • " + app.Cmd : ""),
                    Badge = "Приложение",
                    Icon = app.IconSource,
                    OnExecute = () => ShellController.Instance.LaunchApp(app.Cmd, app.Args),
                    OnAdminExecute = () =>
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo { FileName = app.Cmd, Arguments = app.Args ?? "", Verb = "runas", UseShellExecute = true });
                        }
                        catch { }
                    }
                };
                AddResult(item);
            }

            if (_currentItems.Count > 0)
            {
                _selectedIndex = 0;
                HighlightSelection();
            }
        }

        private void AddResult(SpotlightResultItem item)
        {
            _currentItems.Add(item);
            int idx = _currentItems.Count - 1;

            var card = new Border
            {
                Height = 44,
                CornerRadius = new CornerRadius(8),
                Background = Brushes.Transparent,
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 2, 0, 2),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Icon
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Title / Subtitle
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Badge

            if (item.Icon != null)
            {
                var img = new Image
                {
                    Width = 28,
                    Height = 28,
                    Source = item.Icon,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                Grid.SetColumn(img, 0);
                grid.Children.Add(img);
            }

            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var title = new TextBlock
            {
                Text = item.Title,
                FontFamily = LiquidGlassTheme.UiFont,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            textStack.Children.Add(title);

            if (!string.IsNullOrEmpty(item.Subtitle))
            {
                var sub = new TextBlock
                {
                    Text = item.Subtitle,
                    FontFamily = LiquidGlassTheme.UiFont,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                textStack.Children.Add(sub);
            }
            Grid.SetColumn(textStack, 1);
            grid.Children.Add(textStack);

            if (!string.IsNullOrEmpty(item.Badge))
            {
                var badge = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(6, 2, 6, 2),
                    VerticalAlignment = VerticalAlignment.Center
                };
                badge.Child = new TextBlock
                {
                    Text = item.Badge,
                    FontFamily = LiquidGlassTheme.UiFont,
                    FontSize = 10,
                    Foreground = Brushes.White
                };
                Grid.SetColumn(badge, 2);
                grid.Children.Add(badge);
            }

            card.Child = grid;

            card.MouseEnter += (s, e) =>
            {
                _selectedIndex = idx;
                HighlightSelection();
            };

            card.MouseLeftButtonUp += (s, e) =>
            {
                HideSpotlight();
                if (item.OnExecute != null) item.OnExecute();
            };

            _resultsStack.Children.Add(card);
        }

        private void HighlightSelection()
        {
            for (int i = 0; i < _resultsStack.Children.Count; i++)
            {
                var card = _resultsStack.Children[i] as Border;
                if (card != null)
                {
                    if (i == _selectedIndex)
                    {
                        card.Background = LiquidGlassTheme.AccentBlueBrush;
                    }
                    else
                    {
                        card.Background = Brushes.Transparent;
                    }
                }
            }
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                HideSpotlight();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                if (_currentItems.Count > 0)
                {
                    _selectedIndex = (_selectedIndex + 1) % _currentItems.Count;
                    HighlightSelection();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Up)
            {
                if (_currentItems.Count > 0)
                {
                    _selectedIndex = (_selectedIndex - 1 + _currentItems.Count) % _currentItems.Count;
                    HighlightSelection();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Enter)
            {
                if (_selectedIndex >= 0 && _selectedIndex < _currentItems.Count)
                {
                    var item = _currentItems[_selectedIndex];
                    HideSpotlight();
                    if (item.OnExecute != null) item.OnExecute();
                    e.Handled = true;
                }
            }
        }

        private string TryEvaluateMath(string expr)
        {
            try
            {
                // Only evaluate if string contains numbers and math operators
                if (!expr.Any(char.IsDigit) || !expr.Any(c => "+-*/%^".Contains(c))) return null;

                var dt = new DataTable();
                var val = dt.Compute(expr.Replace(',', '.'), "");
                if (val != null) return val.ToString();
            }
            catch { }
            return null;
        }

        public void ShowSpotlight()
        {
            Visibility = Visibility.Visible;
            IsHitTestVisible = true;
            _txtSearch.Text = "";
            _resultsStack.Children.Clear();
            _currentItems.Clear();
            _selectedIndex = -1;

            Activate();
            _txtSearch.Focus();
        }

        public void HideSpotlight()
        {
            Visibility = Visibility.Collapsed;
            IsHitTestVisible = false;
        }
    }
}
