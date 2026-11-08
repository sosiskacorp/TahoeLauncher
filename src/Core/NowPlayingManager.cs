using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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
        public string SourceApp = "Плеер";
        public ImageSource CoverImage = null;
        public int PositionSeconds = 0;
        public int DurationSeconds = 210; // default 3:30
        public bool IsPlaying = false;
        public bool HasActiveMedia = false;
    }

    public class NowPlayingManager
    {
        public static readonly NowPlayingManager Instance = new NowPlayingManager();

        public NowPlayingInfo CurrentInfo { get; private set; }
        public event Action MediaUpdated;

        private DispatcherTimer _pollTimer;
        private int _simulatedSeconds = 42;

        public NowPlayingManager()
        {
            CurrentInfo = new NowPlayingInfo();
            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            _pollTimer.Tick += OnPollTick;
            _pollTimer.Start();
            PollMediaState();
        }

        private void OnPollTick(object sender, EventArgs e)
        {
            PollMediaState();
        }

        public void PollMediaState()
        {
            bool found = false;
            try
            {
                // 1. Scan for Yandex Music desktop app
                Process[] yandexProcs = Process.GetProcessesByName("Яндекс Музыка");
                if (yandexProcs.Length == 0) yandexProcs = Process.GetProcessesByName("YandexMusic");

                for (int i = 0; i < yandexProcs.Length; i++)
                {
                    string winTitle = yandexProcs[i].MainWindowTitle;
                    if (!string.IsNullOrEmpty(winTitle) && !string.Equals(winTitle, "Яндекс Музыка", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseTrackAndArtist(winTitle, "Яндекс Музыка");
                        found = true;
                        break;
                    }
                    else if (yandexProcs.Length > 0 && !found)
                    {
                        CurrentInfo.SourceApp = "Яндекс Музыка";
                        CurrentInfo.Title = "Яндекс Музыка";
                        CurrentInfo.Artist = "Нажмите Play для воспроизведения";
                        CurrentInfo.HasActiveMedia = true;
                        CurrentInfo.IsPlaying = true;
                        found = true;
                    }
                }

                // 2. Scan for Spotify
                if (!found)
                {
                    Process[] spotifyProcs = Process.GetProcessesByName("Spotify");
                    for (int i = 0; i < spotifyProcs.Length; i++)
                    {
                        string winTitle = spotifyProcs[i].MainWindowTitle;
                        if (!string.IsNullOrEmpty(winTitle) &&
                            !string.Equals(winTitle, "Spotify", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(winTitle, "Spotify Free", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(winTitle, "Spotify Premium", StringComparison.OrdinalIgnoreCase))
                        {
                            ParseTrackAndArtist(winTitle, "Spotify");
                            found = true;
                            break;
                        }
                    }
                }

                // 3. Scan for YouTube in browsers (Chrome, Edge, Firefox, Brave)
                if (!found)
                {
                    string[] browsers = new string[] { "chrome", "msedge", "firefox", "brave", "opera" };
                    for (int b = 0; b < browsers.Length; b++)
                    {
                        Process[] bProcs = Process.GetProcessesByName(browsers[b]);
                        for (int i = 0; i < bProcs.Length; i++)
                        {
                            string t = bProcs[i].MainWindowTitle;
                            if (!string.IsNullOrEmpty(t) && t.Contains("YouTube"))
                            {
                                int ytIdx = t.IndexOf(" - YouTube");
                                if (ytIdx > 0) t = t.Substring(0, ytIdx);
                                ParseTrackAndArtist(t, "YouTube (" + browsers[b] + ")");
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                }

                // Clean up process objects
                for (int i = 0; i < yandexProcs.Length; i++) yandexProcs[i].Dispose();
            }
            catch { }

            if (!found)
            {
                CurrentInfo.HasActiveMedia = false;
                CurrentInfo.IsPlaying = false;
                CurrentInfo.Title = "Музыка не воспроизводится";
                CurrentInfo.Artist = "Запустите Яндекс Музыку или плеер";
                CurrentInfo.SourceApp = "Аудио";
                CurrentInfo.PositionSeconds = 0;
            }
            else
            {
                if (CurrentInfo.IsPlaying)
                {
                    _simulatedSeconds = (_simulatedSeconds + 1) % Math.Max(60, CurrentInfo.DurationSeconds);
                    CurrentInfo.PositionSeconds = _simulatedSeconds;
                }
            }

            if (MediaUpdated != null) MediaUpdated();
        }

        private void ParseTrackAndArtist(string raw, string source)
        {
            CurrentInfo.HasActiveMedia = true;
            CurrentInfo.IsPlaying = true;
            CurrentInfo.SourceApp = source;

            // Common separators: " — ", " - ", " : "
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

            CurrentInfo.Title = title;
            CurrentInfo.Artist = artist;
            if (CurrentInfo.DurationSeconds <= 0) CurrentInfo.DurationSeconds = 210;
        }

        public void PlayPause()
        {
            NativeMethods.keybd_event(0xB3, 0, 0, UIntPtr.Zero);
            NativeMethods.keybd_event(0xB3, 0, 2, UIntPtr.Zero);
            CurrentInfo.IsPlaying = !CurrentInfo.IsPlaying;
            if (MediaUpdated != null) MediaUpdated();
        }

        public void NextTrack()
        {
            NativeMethods.keybd_event(0xB0, 0, 0, UIntPtr.Zero);
            NativeMethods.keybd_event(0xB0, 0, 2, UIntPtr.Zero);
            _simulatedSeconds = 0;
            PollMediaState();
        }

        public void PreviousTrack()
        {
            NativeMethods.keybd_event(0xB1, 0, 0, UIntPtr.Zero);
            NativeMethods.keybd_event(0xB1, 0, 2, UIntPtr.Zero);
            _simulatedSeconds = 0;
            PollMediaState();
        }
    }
}
