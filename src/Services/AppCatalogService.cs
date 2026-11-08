using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MacOSTahoe.Models;
using MacOSTahoe.Native;

namespace MacOSTahoe.Services
{
    public class AppCatalogService
    {
        private static readonly string[] JunkKeywords = new[]
        {
            "uninstall", "удалить", "деинсталл", "deinstall",
            "help", "справка", "readme", "прочти", "manual",
            "website", "веб-сайт", "документация", "documentation",
            "release notes", "changelog", "license", "лицензия",
            "update", "обновление", "configuration", "настройка",
            "troubleshoot", "устранение неполадок", "install", "установка"
        };

        private readonly object _lock = new object();
        private List<AppCatalogEntry> _catalog = new List<AppCatalogEntry>();
        private FileSystemWatcher _watcherCommon;
        private FileSystemWatcher _watcherUser;
        private DispatcherTimer _debounceTimer;

        public event Action OnCatalogUpdated;

        public IReadOnlyList<AppCatalogEntry> GetCatalog()
        {
            lock (_lock)
            {
                return _catalog.ToList();
            }
        }

        public void Initialize(Dispatcher uiDispatcher)
        {
            _debounceTimer = new DispatcherTimer(DispatcherPriority.Background, uiDispatcher)
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _debounceTimer.Tick += (s, e) =>
            {
                _debounceTimer.Stop();
                RefreshCatalogAsync(uiDispatcher);
            };

            SetupWatchers();
            RefreshCatalogAsync(uiDispatcher);
        }

        private void SetupWatchers()
        {
            try
            {
                string commonPrograms = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    @"Microsoft\Windows\Start Menu\Programs");

                if (Directory.Exists(commonPrograms))
                {
                    _watcherCommon = new FileSystemWatcher(commonPrograms)
                    {
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                        Filter = "*.lnk",
                        EnableRaisingEvents = true
                    };
                    _watcherCommon.Created += OnFileSystemChanged;
                    _watcherCommon.Deleted += OnFileSystemChanged;
                    _watcherCommon.Renamed += OnFileSystemChanged;
                }

                string userPrograms = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Windows\Start Menu\Programs");

                if (Directory.Exists(userPrograms))
                {
                    _watcherUser = new FileSystemWatcher(userPrograms)
                    {
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                        Filter = "*.lnk",
                        EnableRaisingEvents = true
                    };
                    _watcherUser.Created += OnFileSystemChanged;
                    _watcherUser.Deleted += OnFileSystemChanged;
                    _watcherUser.Renamed += OnFileSystemChanged;
                }
            }
            catch { }
        }

        private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
        {
            if (_debounceTimer != null)
            {
                _debounceTimer.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _debounceTimer.Stop();
                    _debounceTimer.Start();
                }));
            }
        }

        public void RefreshCatalogAsync(Dispatcher uiDispatcher)
        {
            Task.Run(() =>
            {
                var discovered = ScanStartMenuApps();

                uiDispatcher.BeginInvoke(new Action(() =>
                {
                    // Load icons on UI dispatcher
                    foreach (var item in discovered)
                    {
                        if (item.IconSource == null)
                        {
                            item.IconSource = ResolveIcon(item);
                        }
                    }

                    lock (_lock)
                    {
                        _catalog = discovered;
                    }

                    Action handler = OnCatalogUpdated;
                    if (handler != null) handler();
                }));
            });
        }

        private List<AppCatalogEntry> ScanStartMenuApps()
        {
            var results = new List<AppCatalogEntry>();
            var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Add essential system tools first
            AddSystemApps(results, seenTitles);

            var folders = new List<string>
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs")
            };

            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder)) continue;

                try
                {
                    var lnkFiles = Directory.GetFiles(folder, "*.lnk", SearchOption.AllDirectories);
                    foreach (var lnk in lnkFiles)
                    {
                        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(lnk);
                        if (IsJunk(fileNameWithoutExt)) continue;

                        string target = "";
                        string args = "";
                        string iconPath = "";
                        int iconIndex = 0;

                        ShellComInterop.ResolveShortcutFile(lnk, out target, out args, out iconPath, out iconIndex);

                        if (string.IsNullOrEmpty(target) && string.IsNullOrEmpty(iconPath))
                        {
                            continue;
                        }

                        // Filter non-executable targets if target is a file
                        if (!string.IsNullOrEmpty(target))
                        {
                            string ext = Path.GetExtension(target).ToLowerInvariant();
                            if (ext != ".exe" && ext != ".bat" && ext != ".cmd" && !target.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                        }

                        string cleanTitle = CleanDisplayName(fileNameWithoutExt);
                        if (seenTitles.Contains(cleanTitle)) continue;

                        seenTitles.Add(cleanTitle);

                        var entry = new AppCatalogEntry
                        {
                            Title = cleanTitle,
                            Cmd = !string.IsNullOrEmpty(target) ? target : lnk,
                            Args = args,
                            VerifyPath = !string.IsNullOrEmpty(iconPath) ? iconPath : (!string.IsNullOrEmpty(target) ? target : lnk),
                            Category = CategorizeApp(cleanTitle, target)
                        };

                        results.Add(entry);
                    }
                }
                catch { }
            }

            return results.OrderBy(x => x.Category).ThenBy(x => x.Title).ToList();
        }

        private static void AddSystemApps(List<AppCatalogEntry> list, HashSet<string> seen)
        {
            var sysApps = new[]
            {
                new { Title = "Проводник", Cmd = "explorer.exe", Args = "", Verify = @"C:\Windows\explorer.exe", Cat = "Система" },
                new { Title = "Параметры", Cmd = "ms-settings:", Args = "", Verify = @"C:\Windows\ImmersiveControlPanel\SystemSettings.exe", Cat = "Система" },
                new { Title = "Диспетчер задач", Cmd = "taskmgr.exe", Args = "", Verify = @"C:\Windows\System32\Taskmgr.exe", Cat = "Утилиты" },
                new { Title = "Терминал", Cmd = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WindowsApps\wt.exe")) ? "wt.exe" : "cmd.exe", Args = "", Verify = @"C:\Windows\System32\cmd.exe", Cat = "Разработка" },
                new { Title = "Калькулятор", Cmd = "calc.exe", Args = "", Verify = @"C:\Windows\System32\calc.exe", Cat = "Утилиты" },
                new { Title = "Блокнот", Cmd = "notepad.exe", Args = "", Verify = @"C:\Windows\System32\notepad.exe", Cat = "Утилиты" }
            };

            foreach (var a in sysApps)
            {
                if (!seen.Contains(a.Title))
                {
                    seen.Add(a.Title);
                    list.Add(new AppCatalogEntry
                    {
                        Title = a.Title,
                        Cmd = a.Cmd,
                        Args = a.Args,
                        VerifyPath = a.Verify,
                        Category = a.Cat
                    });
                }
            }
        }

        private static ImageSource ResolveIcon(AppCatalogEntry entry)
        {
            try
            {
                if (!string.IsNullOrEmpty(entry.VerifyPath) && File.Exists(entry.VerifyPath))
                {
                    var img = ShellComInterop.ExtractHighResIcon(entry.VerifyPath, 0);
                    if (img != null) return img;
                }

                if (!string.IsNullOrEmpty(entry.Cmd) && File.Exists(entry.Cmd))
                {
                    var img = ShellComInterop.ExtractHighResIcon(entry.Cmd, 0);
                    if (img != null) return img;
                }
            }
            catch { }
            return null;
        }

        private static bool IsJunk(string title)
        {
            string lower = title.ToLowerInvariant();
            return JunkKeywords.Any(k => lower.Contains(k));
        }

        private static string CleanDisplayName(string raw)
        {
            return raw.Replace("- Shortcut", "")
                      .Replace("- Ярлык", "")
                      .Trim();
        }

        private static string CategorizeApp(string title, string target)
        {
            string str = (title + " " + target).ToLowerInvariant();

            if (str.Contains("code") || str.Contains("visual studio") || str.Contains("git") ||
                str.Contains("terminal") || str.Contains("powershell") || str.Contains("cmd") ||
                str.Contains("python") || str.Contains("jetbrains") || str.Contains("pycharm") ||
                str.Contains("rider") || str.Contains("webstorm") || str.Contains("docker"))
            {
                return "Разработка";
            }

            if (str.Contains("chrome") || str.Contains("firefox") || str.Contains("edge") ||
                str.Contains("opera") || str.Contains("brave") || str.Contains("browser") ||
                str.Contains("telegram") || str.Contains("discord") || str.Contains("slack") ||
                str.Contains("whatsapp") || str.Contains("yandex") || str.Contains("вайбер"))
            {
                return "Интернет";
            }

            if (str.Contains("player") || str.Contains("music") || str.Contains("spotify") ||
                str.Contains("vlc") || str.Contains("photoshop") || str.Contains("premiere") ||
                str.Contains("illustrator") || str.Contains("figma") || str.Contains("obs") ||
                str.Contains("blender") || str.Contains("aimp") || str.Contains("sound"))
            {
                return "Мультимедиа";
            }

            if (str.Contains("word") || str.Contains("excel") || str.Contains("powerpoint") ||
                str.Contains("office") || str.Contains("notion") || str.Contains("obsidian") ||
                str.Contains("acrobat") || str.Contains("pdf") || str.Contains("onenote"))
            {
                return "Офис";
            }

            return "Утилиты";
        }
    }
}
