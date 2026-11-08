using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MacOSTahoe
{
    public class NowPlayingInfo
    {
        public string Title = "Музыка не играет";
        public string Artist = "Запустите Яндекс Музыку или плеер";
        public string Album = "";
        public string SourceApp = "Аудио";
        public ImageSource CoverImage = null;
        public int PositionSeconds = 0;
        public int DurationSeconds = 180; // default 3 min
        public bool IsPlaying = false;
        public bool HasActiveMedia = false;
    }

    public class NowPlayingManager
    {
        public static readonly NowPlayingManager Instance = new NowPlayingManager();

        public NowPlayingInfo CurrentInfo { get; private set; }
        public event Action MediaUpdated;

        private DispatcherTimer _uiTimer;
        private bool _isQuerying = false;
        private DateTime _lastCoverModified = DateTime.MinValue;

        public NowPlayingManager()
        {
            CurrentInfo = new NowPlayingInfo();

            // Local 1-second UI timer for smooth progress ticks
            _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.0) };
            _uiTimer.Tick += OnUiTimerTick;
            _uiTimer.Start();

            // Initial query
            QueryMediaStateAsync();
        }

        private int _pollCounter = 0;
        private void OnUiTimerTick(object sender, EventArgs e)
        {
            if (CurrentInfo.IsPlaying && CurrentInfo.HasActiveMedia)
            {
                if (CurrentInfo.DurationSeconds > 0 && CurrentInfo.PositionSeconds < CurrentInfo.DurationSeconds)
                {
                    CurrentInfo.PositionSeconds++;
                }
                if (MediaUpdated != null) MediaUpdated();
            }

            _pollCounter++;
            if (_pollCounter >= 2) // Sync with GSMTC every 2 seconds
            {
                _pollCounter = 0;
                QueryMediaStateAsync();
            }
        }

        public void QueryMediaStateAsync()
        {
            if (_isQuerying) return;
            _isQuerying = true;

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    string scriptPath = GetScriptPath();
                    if (!File.Exists(scriptPath)) EnsureScriptExists(scriptPath);

                    ProcessStartInfo psi = new ProcessStartInfo("powershell.exe",
                        "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\" -Action Get")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        StandardOutputEncoding = Encoding.UTF8
                    };

                    using (Process p = Process.Start(psi))
                    {
                        string output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(3000);

                        if (!string.IsNullOrEmpty(output) && output.Contains("MEDIA_DATA|"))
                        {
                            ParseMediaDataLine(output);
                        }
                        else
                        {
                            // Fallback to window title scanning
                            FallbackScanProcessTitles();
                        }
                    }
                }
                catch
                {
                    FallbackScanProcessTitles();
                }
                finally
                {
                    _isQuerying = false;
                }
            });
        }

        private void ParseMediaDataLine(string output)
        {
            try
            {
                string[] lines = output.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.StartsWith("MEDIA_DATA|"))
                    {
                        string[] parts = line.Split('|');
                        if (parts.Length >= 9)
                        {
                            string title = parts[1].Trim();
                            string artist = parts[2].Trim();
                            string album = parts[3].Trim();
                            string app = parts[4].Trim();
                            string status = parts[5].Trim();
                            int posSec = 0;
                            int endSec = 0;
                            int.TryParse(parts[6].Trim(), out posSec);
                            int.TryParse(parts[7].Trim(), out endSec);
                            bool hasThumb = (parts[8].Trim() == "1");

                            // App formatting
                            string sourceApp = "Медиаплеер";
                            if (app.Contains("yandex.desktop.music") || app.Contains("YandexMusic") || app.Contains("музык"))
                                sourceApp = "Яндекс Музыка";
                            else if (app.Contains("Spotify"))
                                sourceApp = "Spotify";
                            else if (app.Contains("chrome") || app.Contains("edge") || app.Contains("browser"))
                                sourceApp = "Браузер";
                            else if (!string.IsNullOrEmpty(app))
                                sourceApp = app;

                            bool isPlaying = (status.Equals("Playing", StringComparison.OrdinalIgnoreCase));

                            // Load cover if present
                            ImageSource cover = null;
                            if (hasThumb)
                            {
                                cover = TryLoadCoverImage();
                            }

                            // Update on UI thread
                            if (Application.Current != null && Application.Current.Dispatcher != null)
                            {
                                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    CurrentInfo.HasActiveMedia = true;
                                    CurrentInfo.Title = string.IsNullOrEmpty(title) ? "Аудиотрек" : title;
                                    CurrentInfo.Artist = string.IsNullOrEmpty(artist) ? sourceApp : artist;
                                    CurrentInfo.Album = album;
                                    CurrentInfo.SourceApp = sourceApp;
                                    CurrentInfo.IsPlaying = isPlaying;
                                    if (posSec >= 0) CurrentInfo.PositionSeconds = posSec;
                                    if (endSec > 0) CurrentInfo.DurationSeconds = endSec;
                                    if (cover != null) CurrentInfo.CoverImage = cover;

                                    if (MediaUpdated != null) MediaUpdated();
                                }));
                            }
                            return;
                        }
                    }
                }
            }
            catch { }
        }

        private ImageSource TryLoadCoverImage()
        {
            try
            {
                string thumbPath = Path.Combine(Path.GetTempPath(), "tahoe_cover.jpg");
                if (File.Exists(thumbPath))
                {
                    FileInfo fi = new FileInfo(thumbPath);
                    if (fi.Length > 0 && fi.LastWriteTimeUtc != _lastCoverModified)
                    {
                        _lastCoverModified = fi.LastWriteTimeUtc;
                        byte[] bytes = File.ReadAllBytes(thumbPath);
                        using (MemoryStream ms = new MemoryStream(bytes))
                        {
                            BitmapImage bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.StreamSource = ms;
                            bmp.EndInit();
                            bmp.Freeze();
                            return bmp;
                        }
                    }
                    else if (CurrentInfo.CoverImage != null)
                    {
                        return CurrentInfo.CoverImage;
                    }
                }
            }
            catch { }
            return null;
        }

        private void FallbackScanProcessTitles()
        {
            bool found = false;
            try
            {
                Process[] yandexProcs = Process.GetProcessesByName("Яндекс Музыка");
                if (yandexProcs.Length == 0) yandexProcs = Process.GetProcessesByName("YandexMusic");

                for (int i = 0; i < yandexProcs.Length; i++)
                {
                    string winTitle = yandexProcs[i].MainWindowTitle;
                    if (!string.IsNullOrEmpty(winTitle) && !string.Equals(winTitle, "Яндекс Музыка", StringComparison.OrdinalIgnoreCase))
                    {
                        SetFallbackTrack(winTitle, "Яндекс Музыка");
                        found = true;
                        break;
                    }
                }
                for (int i = 0; i < yandexProcs.Length; i++) yandexProcs[i].Dispose();
            }
            catch { }

            if (!found)
            {
                if (Application.Current != null && Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        CurrentInfo.HasActiveMedia = false;
                        CurrentInfo.IsPlaying = false;
                        CurrentInfo.Title = "Музыка не играет";
                        CurrentInfo.Artist = "Запустите Яндекс Музыку или плеер";
                        CurrentInfo.SourceApp = "Аудио";
                        CurrentInfo.PositionSeconds = 0;
                        if (MediaUpdated != null) MediaUpdated();
                    }));
                }
            }
        }

        private void SetFallbackTrack(string raw, string source)
        {
            string title = raw.Trim();
            string artist = source;
            if (title.Contains(" — "))
            {
                string[] parts = title.Split(new string[] { " — " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    artist = parts[0].Trim();
                    title = parts[1].Trim();
                }
            }
            else if (title.Contains(" - "))
            {
                string[] parts = title.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    artist = parts[0].Trim();
                    title = parts[1].Trim();
                }
            }

            if (Application.Current != null && Application.Current.Dispatcher != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    CurrentInfo.HasActiveMedia = true;
                    CurrentInfo.IsPlaying = true;
                    CurrentInfo.SourceApp = source;
                    CurrentInfo.Title = title;
                    CurrentInfo.Artist = artist;
                    if (MediaUpdated != null) MediaUpdated();
                }));
            }
        }

        public void PlayPause()
        {
            // Immediate hardware media key
            NativeMethods.keybd_event(0xB3, 0, 0, UIntPtr.Zero);
            NativeMethods.keybd_event(0xB3, 0, 2, UIntPtr.Zero);

            CurrentInfo.IsPlaying = !CurrentInfo.IsPlaying;
            if (MediaUpdated != null) MediaUpdated();

            // Background GSMTC sync
            ExecuteActionAsync("PlayPause");
        }

        public void NextTrack()
        {
            // Immediate hardware media key
            NativeMethods.keybd_event(0xB0, 0, 0, UIntPtr.Zero);
            NativeMethods.keybd_event(0xB0, 0, 2, UIntPtr.Zero);

            CurrentInfo.PositionSeconds = 0;
            if (MediaUpdated != null) MediaUpdated();

            ExecuteActionAsync("Next");
        }

        public void PreviousTrack()
        {
            // Immediate hardware media key
            NativeMethods.keybd_event(0xB1, 0, 0, UIntPtr.Zero);
            NativeMethods.keybd_event(0xB1, 0, 2, UIntPtr.Zero);

            CurrentInfo.PositionSeconds = 0;
            if (MediaUpdated != null) MediaUpdated();

            ExecuteActionAsync("Prev");
        }

        private void ExecuteActionAsync(string act)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    string scriptPath = GetScriptPath();
                    if (File.Exists(scriptPath))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo("powershell.exe",
                            "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\" -Action " + act)
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            StandardOutputEncoding = Encoding.UTF8
                        };
                        using (Process p = Process.Start(psi))
                        {
                            string outStr = p.StandardOutput.ReadToEnd();
                            p.WaitForExit(3000);
                            if (outStr.Contains("MEDIA_DATA|"))
                            {
                                ParseMediaDataLine(outStr);
                            }
                        }
                    }
                }
                catch { }
            });
        }

        private static string GetScriptPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "scripts", "GetMediaInfo.ps1");
        }

        private static void EnsureScriptExists(string path)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string code = @"param(
    [string]$Action = ""Get""
)
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | ? { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
function Await($winRtTask, $resultType) {
    $asTask = $asTaskGeneric.MakeGenericMethod($resultType)
    $netTask = $asTask.Invoke($null, @($winRtTask))
    $netTask.Wait(-1) | Out-Null
    $netTask.Result
}
[Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager, Windows.Media, ContentType = WindowsRuntime] | Out-Null
$mgrType = [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager]
$op = $mgrType::RequestAsync()
$manager = Await $op $mgrType
$cur = $manager.GetCurrentSession()
if (-not $cur) {
    Write-Host ""NO_MEDIA""
    exit
}

if ($Action -eq ""PlayPause"") {
    $cur.TryTogglePlayPauseAsync() | Out-Null
} elseif ($Action -eq ""Next"") {
    $cur.TrySkipNextAsync() | Out-Null
} elseif ($Action -eq ""Prev"") {
    $cur.TrySkipPreviousAsync() | Out-Null
}

$mediaOp = $cur.TryGetMediaPropertiesAsync()
$props = Await $mediaOp ([Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties])
$playback = $cur.GetPlaybackInfo()
$timeline = $cur.GetTimelineProperties()

$hasThumb = ""0""
if ($props.Thumbnail) {
    try {
        $streamOp = $props.Thumbnail.OpenReadAsync()
        $stream = Await $streamOp ([Windows.Storage.Streams.IRandomAccessStreamWithContentType])
        $m = ([System.IO.WindowsRuntimeStreamExtensions].GetMethods() | ? { $_.Name -eq 'AsStream' -and $_.GetParameters().Count -eq 1 })[0]
        $netStream = $m.Invoke($null, @($stream))
        $thumbPath = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), ""tahoe_cover.jpg"")
        $fileStream = [System.IO.File]::Create($thumbPath)
        $netStream.CopyTo($fileStream)
        $fileStream.Dispose()
        $netStream.Dispose()
        $hasThumb = ""1""
    } catch { }
}

$posSec = [int]$timeline.Position.TotalSeconds
$endSec = [int]$timeline.EndTime.TotalSeconds
$status = $playback.PlaybackStatus.ToString()
$app = $cur.SourceAppUserModelId

Write-Host ""MEDIA_DATA|$($props.Title)|$($props.Artist)|$($props.AlbumTitle)|$app|$status|$posSec|$endSec|$hasThumb""
";
                File.WriteAllText(path, code, Encoding.UTF8);
            }
            catch { }
        }
    }
}
