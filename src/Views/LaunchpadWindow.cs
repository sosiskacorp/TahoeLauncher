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
    public class LaunchpadWindow : Window
    {
        private const int ColumnsPerPage = 8;
        private const int RowsPerPage = 4;
        private const int AppsPerPage = ColumnsPerPage * RowsPerPage;

        private UniformGrid _gridPanel;
        private TranslateTransform _gridTranslate;
        private TextBox _filterBox;
        private TextBlock _placeholderText;
        private StackPanel _categoryPanel;
        private StackPanel _dotsPanel;
        private ScaleTransform _scaleTransform;
        private Popup _appContextMenuPopup;

        private string _activeCategory = "Все";
        private int _currentPage = 0;
        private int _totalPages = 1;
        private List<AppCatalogEntry> _filteredApps = new List<AppCatalogEntry>();

        private static readonly string[] Categories = new string[]
        {
            "Все", "★ Избранное", "Игры", "Разработка", "Интернет", "Медиа и Офис", "Система", "Программы"
        };

        public LaunchpadWindow()
        {
            Title = "Приложения";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
            Background = new SolidColorBrush(Color.FromArgb(198, 12, 12, 20));
            FontFamily = LiquidGlassTheme.UiFont;

            Visibility = Visibility.Collapsed;

            BuildUI();

            SourceInitialized += delegate
            {
                NativeMethods.EnableBlur(this, 0xBB0E0E16);
            };

            PreviewKeyDown += OnPreviewKeyDown;
            PreviewMouseWheel += OnPreviewMouseWheel;
        }

        private void BuildUI()
        {
            Grid root = new Grid();
            root.Background = Brushes.Transparent;
            _scaleTransform = new ScaleTransform(1.0, 1.0, Width / 2.0, Height / 2.0);
            root.RenderTransform = _scaleTransform;

            StackPanel container = new StackPanel();
            container.HorizontalAlignment = HorizontalAlignment.Center;
            container.VerticalAlignment = VerticalAlignment.Top;
            container.Margin = new Thickness(0, 44, 0, 0);

            root.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.OriginalSource == root || e.OriginalSource == container || e.OriginalSource == _gridPanel)
                {
                    HideWithAnimation();
                }
            };

            // Top Header Row: Search Input + Quick Actions
            StackPanel topHeaderRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 14)
            };

            Border searchBorder = new Border
            {
                Width = 360,
                Height = 38,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(48, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 4, 12, 4)
            };

            Grid searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock searchIcon = new TextBlock
            {
                Text = "🔍",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(searchIcon, 0);
            searchGrid.Children.Add(searchIcon);

            _placeholderText = new TextBlock
            {
                Text = "Поиск приложений...",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromArgb(140, 235, 235, 245)),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            Grid.SetColumn(_placeholderText, 1);
            searchGrid.Children.Add(_placeholderText);

            _filterBox = new TextBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                CaretBrush = Brushes.White,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            _filterBox.TextChanged += delegate
            {
                _placeholderText.Visibility = string.IsNullOrEmpty(_filterBox.Text) ? Visibility.Visible : Visibility.Hidden;
                _currentPage = 0;
                RenderAppGrid(_filterBox.Text.Trim(), 0);
            };
            Grid.SetColumn(_filterBox, 1);
            searchGrid.Children.Add(_filterBox);
            searchBorder.Child = searchGrid;
            topHeaderRow.Children.Add(searchBorder);

            Border settingsBtn = CreateHeaderActionButton("⚙ Настройки лаунчера", delegate
            {
                HideWithAnimation();
                ShellController.Instance.ShowLauncherSettings();
            });
            settingsBtn.Margin = new Thickness(12, 0, 0, 0);
            topHeaderRow.Children.Add(settingsBtn);

            Border winOverviewBtn = CreateHeaderActionButton("🗔 Активные окна", delegate
            {
                HideWithAnimation();
                ShellController.Instance.ToggleWindowSwitcher();
            });
            winOverviewBtn.Margin = new Thickness(8, 0, 0, 0);
            topHeaderRow.Children.Add(winOverviewBtn);

            container.Children.Add(topHeaderRow);

            // Category Filter Pills Bar
            _categoryPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 18)
            };
            container.Children.Add(_categoryPanel);
            RebuildCategoryPills();

            // 8x4 UniformGrid
            _gridPanel = new UniformGrid
            {
                Columns = ColumnsPerPage,
                Rows = RowsPerPage,
                Width = Math.Min(1280, Width - 120),
                Height = Math.Min(560, Height - 275),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _gridTranslate = new TranslateTransform(0, 0);
            _gridPanel.RenderTransform = _gridTranslate;
            container.Children.Add(_gridPanel);

            // Page Dots & Navigation Bar
            _dotsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 16, 0, 0)
            };
            container.Children.Add(_dotsPanel);

            root.Children.Add(container);
            Content = root;

            RenderAppGrid("", 0);
        }

        private Border CreateHeaderActionButton(string label, Action onClick)
        {
            Border b = new Border
            {
                Height = 38,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(65, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 0, 14, 0),
                Cursor = Cursors.Hand
            };
            b.Child = new TextBlock
            {
                Text = label,
                FontSize = 12.5,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            b.MouseEnter += delegate { b.Background = new SolidColorBrush(Color.FromArgb(75, 10, 132, 255)); };
            b.MouseLeave += delegate { b.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)); };
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };
            return b;
        }

        private void RebuildCategoryPills()
        {
            if (_categoryPanel == null) return;
            _categoryPanel.Children.Clear();
            for (int i = 0; i < Categories.Length; i++)
            {
                string cat = Categories[i];
                bool active = string.Equals(cat, _activeCategory, StringComparison.OrdinalIgnoreCase);
                Border pill = new Border
                {
                    CornerRadius = new CornerRadius(14),
                    Background = active
                        ? LiquidGlassTheme.AccentBlueBrush
                        : new SolidColorBrush(Color.FromArgb(34, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(active ? (byte)150 : (byte)45, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(14, 5, 14, 5),
                    Margin = new Thickness(4, 0, 4, 0),
                    Cursor = Cursors.Hand
                };
                pill.Child = new TextBlock
                {
                    Text = cat,
                    FontSize = 12.5,
                    FontWeight = active ? FontWeights.SemiBold : FontWeights.Medium,
                    Foreground = Brushes.White
                };
                string capturedCat = cat;
                pill.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    _activeCategory = capturedCat;
                    _currentPage = 0;
                    RebuildCategoryPills();
                    RenderAppGrid(_filterBox.Text.Trim(), 0);
                };
                _categoryPanel.Children.Add(pill);
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                HideWithAnimation();
            }
            else if (e.Key == Key.Enter)
            {
                if (_filteredApps.Count > 0)
                {
                    e.Handled = true;
                    AppCatalogEntry first = _filteredApps[0];
                    HideWithAnimation();
                    ShellController.RunProcess(first.Cmd, first.Args);
                }
            }
            else if (e.Key == Key.PageDown || (e.Key == Key.Right && string.IsNullOrEmpty(_filterBox.Text)))
            {
                if (_currentPage < _totalPages - 1)
                {
                    e.Handled = true;
                    SwitchToPage(_currentPage + 1, 1);
                }
            }
            else if (e.Key == Key.PageUp || (e.Key == Key.Left && string.IsNullOrEmpty(_filterBox.Text)))
            {
                if (_currentPage > 0)
                {
                    e.Handled = true;
                    SwitchToPage(_currentPage - 1, -1);
                }
            }
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0 && _currentPage < _totalPages - 1)
            {
                SwitchToPage(_currentPage + 1, 1);
            }
            else if (e.Delta > 0 && _currentPage > 0)
            {
                SwitchToPage(_currentPage - 1, -1);
            }
        }

        private void SwitchToPage(int newPage, int direction)
        {
            if (newPage < 0 || newPage >= _totalPages || newPage == _currentPage) return;
            _currentPage = newPage;
            RenderAppGrid(_filterBox.Text.Trim(), direction);
        }

        public void OnCatalogUpdated()
        {
            RenderAppGrid(_filterBox.Text.Trim(), 0);
        }

        private void RenderAppGrid(string filter, int slideDirection)
        {
            _gridPanel.Children.Clear();
            _filteredApps.Clear();

            List<AppCatalogEntry> allApps = ShellController.Instance != null
                ? ShellController.Instance.InstalledApps
                : new List<AppCatalogEntry>();

            _placeholderText.Text = string.Format("Поиск среди {0} приложений (ПКМ — меню)...", allApps.Count);

            for (int i = 0; i < allApps.Count; i++)
            {
                AppCatalogEntry entry = allApps[i];
                if (!string.IsNullOrEmpty(filter))
                {
                    if (entry.Title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                        (entry.Category ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }
                else if (_activeCategory != "Все")
                {
                    if (_activeCategory == "★ Избранное")
                    {
                        if (!ShellController.Instance.IsFavoriteApp(entry.Title)) continue;
                    }
                    else if (!string.Equals(entry.Category, _activeCategory, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }
                _filteredApps.Add(entry);
            }

            _totalPages = Math.Max(1, (int)Math.Ceiling(_filteredApps.Count / (double)AppsPerPage));
            if (_currentPage >= _totalPages) _currentPage = _totalPages - 1;
            if (_currentPage < 0) _currentPage = 0;

            int startIdx = _currentPage * AppsPerPage;
            int endIdx = Math.Min(startIdx + AppsPerPage, _filteredApps.Count);

            for (int i = startIdx; i < endIdx; i++)
            {
                AppCatalogEntry entry = _filteredApps[i];
                bool isFav = ShellController.Instance != null && ShellController.Instance.IsFavoriteApp(entry.Title);

                Border itemBox = new Border
                {
                    Margin = new Thickness(6, 4, 6, 4),
                    CornerRadius = new CornerRadius(16),
                    Background = Brushes.Transparent,
                    Cursor = Cursors.Hand
                };

                StackPanel sp = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid iconWrap = new Grid { Width = 78, Height = 78 };
                Image img = new Image
                {
                    Source = entry.IconSource ?? ShellController.Instance.GetIcon("finder.png"),
                    Width = 72,
                    Height = 72,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                img.Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 5, Opacity = 0.48, Color = Colors.Black };
                iconWrap.Children.Add(img);

                if (isFav)
                {
                    Border starBadge = new Border
                    {
                        Width = 18,
                        Height = 18,
                        CornerRadius = new CornerRadius(9),
                        Background = new SolidColorBrush(Color.FromArgb(210, 255, 195, 0)),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 2, 2, 0)
                    };
                    starBadge.Child = new TextBlock
                    {
                        Text = "★",
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Black,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    iconWrap.Children.Add(starBadge);
                }
                sp.Children.Add(iconWrap);

                TextBlock titleBlock = new TextBlock
                {
                    Text = entry.Title,
                    Foreground = Brushes.White,
                    FontSize = 12.5,
                    FontWeight = FontWeights.Medium,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 136,
                    Margin = new Thickness(0, 6, 0, 0)
                };
                sp.Children.Add(titleBlock);

                itemBox.Child = sp;

                AppCatalogEntry cap = entry;
                Border capBox = itemBox;
                itemBox.MouseEnter += delegate
                {
                    img.Width = 78;
                    img.Height = 78;
                    capBox.Background = new SolidColorBrush(Color.FromArgb(26, 255, 255, 255));
                };
                itemBox.MouseLeave += delegate
                {
                    img.Width = 72;
                    img.Height = 72;
                    capBox.Background = Brushes.Transparent;
                };
                itemBox.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    HideWithAnimation();
                    ShellController.RunProcess(cap.Cmd, cap.Args);
                };
                itemBox.MouseRightButtonUp += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    ShowAppContextMenu(capBox, cap);
                };

                _gridPanel.Children.Add(itemBox);
            }

            _dotsPanel.Children.Clear();
            if (_totalPages > 1)
            {
                Border prevBtn = CreatePageArrow("‹", _currentPage > 0, delegate { SwitchToPage(_currentPage - 1, -1); });
                _dotsPanel.Children.Add(prevBtn);

                for (int p = 0; p < _totalPages; p++)
                {
                    int pageIndex = p;
                    bool active = (p == _currentPage);
                    Border dot = new Border
                    {
                        Width = active ? 20 : 8,
                        Height = 8,
                        CornerRadius = new CornerRadius(4),
                        Background = active
                            ? Brushes.White
                            : new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
                        Margin = new Thickness(5, 0, 5, 0),
                        Cursor = Cursors.Hand
                    };
                    dot.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                    {
                        e.Handled = true;
                        SwitchToPage(pageIndex, pageIndex > _currentPage ? 1 : -1);
                    };
                    _dotsPanel.Children.Add(dot);
                }

                Border nextBtn = CreatePageArrow("›", _currentPage < _totalPages - 1, delegate { SwitchToPage(_currentPage + 1, 1); });
                _dotsPanel.Children.Add(nextBtn);
            }

            if (slideDirection != 0 && _gridTranslate != null)
            {
                DoubleAnimation slideAnim = new DoubleAnimation(slideDirection * 48.0, 0.0, TimeSpan.FromMilliseconds(160));
                _gridTranslate.BeginAnimation(TranslateTransform.XProperty, slideAnim);
            }
        }

        private void ShowAppContextMenu(FrameworkElement anchor, AppCatalogEntry app)
        {
            if (_appContextMenuPopup != null && _appContextMenuPopup.IsOpen)
            {
                _appContextMenuPopup.IsOpen = false;
            }

            Popup popup = new Popup
            {
                AllowsTransparency = true,
                PlacementTarget = anchor,
                Placement = PlacementMode.Bottom,
                VerticalOffset = -8,
                StaysOpen = false,
                PopupAnimation = PopupAnimation.Fade
            };

            Border outer = new Border
            {
                Margin = new Thickness(10),
                Background = LiquidGlassTheme.CreateGlassBrush(236, 248),
                BorderBrush = LiquidGlassTheme.CreateSpecularBorderBrush(),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(5),
                MinWidth = 225,
                Effect = new DropShadowEffect { BlurRadius = 22, ShadowDepth = 6, Opacity = 0.58, Color = Colors.Black }
            };

            StackPanel stack = new StackPanel();
            stack.Children.Add(CreateMenuRow("▶  Открыть «" + app.Title + "»", delegate
            {
                popup.IsOpen = false;
                HideWithAnimation();
                ShellController.RunProcess(app.Cmd, app.Args);
            }));

            stack.Children.Add(CreateMenuRow("🛡  Запустить от имени администратора", delegate
            {
                popup.IsOpen = false;
                HideWithAnimation();
                ShellController.RunProcessEx(app.Cmd, app.Args, true);
            }));

            bool pinned = ShellController.Instance.IsAppPinnedInDock(app.Cmd) || ShellController.Instance.IsAppPinnedInDock(app.Title);
            stack.Children.Add(CreateMenuRow(pinned ? "❌  Открепить из Dock" : "📌  Закрепить в Dock", delegate
            {
                popup.IsOpen = false;
                if (pinned)
                    ShellController.Instance.UnpinAppFromDock(app.Title);
                else
                    ShellController.Instance.PinAppToDock(app.Title, app.Cmd, app.Args);
            }));

            bool isFav = ShellController.Instance.IsFavoriteApp(app.Title);
            stack.Children.Add(CreateMenuRow(isFav ? "☆  Убрать из Избранного" : "★  Добавить в Избранное", delegate
            {
                popup.IsOpen = false;
                ShellController.Instance.ToggleFavoriteApp(app.Title);
                RenderAppGrid(_filterBox.Text.Trim(), 0);
            }));

            if (!string.IsNullOrEmpty(app.Cmd) && app.Cmd.IndexOf('\\') >= 0 && File.Exists(app.Cmd))
            {
                stack.Children.Add(CreateMenuRow("📂  Открыть расположение файла", delegate
                {
                    popup.IsOpen = false;
                    HideWithAnimation();
                    ShellController.OpenFileLocation(app.Cmd);
                }));
            }

            stack.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Margin = new Thickness(8, 4, 8, 4)
            });

            stack.Children.Add(CreateMenuRow("👁  Скрыть из лаунчера", delegate
            {
                popup.IsOpen = false;
                ShellController.Instance.HideAppFromCatalog(app.Title);
            }));

            outer.Child = stack;
            popup.Child = outer;
            _appContextMenuPopup = popup;
            popup.IsOpen = true;
        }

        private FrameworkElement CreateMenuRow(string text, Action onClick)
        {
            Border row = new Border
            {
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(10, 5, 10, 5),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand
            };
            row.Child = new TextBlock
            {
                Text = text,
                FontSize = 13,
                Foreground = Brushes.White
            };
            row.MouseEnter += delegate { row.Background = LiquidGlassTheme.AccentBlueBrush; };
            row.MouseLeave += delegate { row.Background = Brushes.Transparent; };
            row.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                if (onClick != null) onClick();
            };
            return row;
        }

        private Border CreatePageArrow(string text, bool enabled, Action onClick)
        {
            Border b = new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(13),
                Background = new SolidColorBrush(Color.FromArgb(enabled ? (byte)42 : (byte)15, 255, 255, 255)),
                Margin = new Thickness(10, 0, 10, 0),
                Cursor = enabled ? Cursors.Hand : Cursors.Arrow
            };
            b.Child = new TextBlock
            {
                Text = text,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(enabled ? (byte)240 : (byte)80, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -2, 0, 0)
            };
            if (enabled)
            {
                b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    if (onClick != null) onClick();
                };
            }
            return b;
        }

        public void ShowWithAnimation()
        {
            if (ShellController.Instance != null)
            {
                ShellController.Instance.RefreshInstalledApps();
            }
            _filterBox.Text = "";
            _placeholderText.Visibility = Visibility.Visible;
            _currentPage = 0;
            RebuildCategoryPills();
            RenderAppGrid("", 0);
            Show();
            Activate();
            _filterBox.Focus();
            DoubleAnimation anim = new DoubleAnimation(1.06, 1.0, TimeSpan.FromMilliseconds(170));
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        public void HideWithAnimation()
        {
            if (_appContextMenuPopup != null) _appContextMenuPopup.IsOpen = false;
            Hide();
        }
    }

    // =========================================================================
    // 7. WINDOW SWITCHER / MISSION CONTROL (Обзор активных окон)
    // =========================================================================
}
