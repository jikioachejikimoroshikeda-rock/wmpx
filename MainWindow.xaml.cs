using LibVLCSharp.Shared;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using WinRT.Interop;

namespace Wmpx;

public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<TrackItem> _tracks = new();
    private readonly ObservableCollection<VideoItem> _videos = new();
    private readonly MediaPlayer _audioPlayer = new();
    private readonly LibVLC _libVlc;
    private readonly LibVLCSharp.Shared.MediaPlayer _videoPlayer;
    private readonly DispatcherQueueTimer _timer;
    private readonly ApplicationDataContainer _settings = ApplicationData.Current.LocalSettings;
    private LibVLCSharp.Shared.Media? _currentVideoMedia;
    private int _currentIndex = -1;
    private int _currentVideoIndex = -1;
    private bool _changingSlider;
    private bool _shuffle;
    private bool _repeat;
    private bool _videoMode;
    private bool _isFullscreen;

    public MainWindow()
    {
        InitializeComponent();
        TrackList.ItemsSource = _tracks;
        Core.Initialize();
        _libVlc = new LibVLC();
        _videoPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVlc);
        VideoSurface.MediaPlayer = _videoPlayer;
        _audioPlayer.MediaEnded += Player_MediaEnded;
        _videoPlayer.EndReached += VideoPlayer_EndReached;
        ApplySavedAppearance();
        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(500);
        _timer.Tick += (_, _) => UpdateTimeline();
        _timer.Start();
        Closed += (_, _) =>
        {
            _timer.Stop();
            _currentVideoMedia?.Dispose();
            _videoPlayer.Dispose();
            _libVlc.Dispose();
            _audioPlayer.Dispose();
        };
    }

    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        var themePicker = new ComboBox { Header = "Paleta de color", HorizontalAlignment = HorizontalAlignment.Stretch };
        foreach (var name in new[] { "Oscuro", "Medianoche", "Grafito", "Neblina", "Océano", "Aurora", "Rosa", "Ámbar" }) themePicker.Items.Add(name);
        themePicker.SelectedItem = _settings.Values["Theme"] as string ?? "Oscuro";
        var backgroundBox = new TextBox { Header = "Fondo personalizado (HEX)", Text = _settings.Values["BackgroundColor"] as string ?? "#101A2A" };
        var borderBox = new TextBox { Header = "Borde (HEX)", Text = _settings.Values["BorderColor"] as string ?? "#425571" };
        var accentBox = new TextBox { Header = "Color de acento (HEX)", Text = _settings.Values["AccentColor"] as string ?? "#3A9DFF" };
        var thickness = new Slider { Header = "Grosor del borde", Minimum = 0, Maximum = 8, StepFrequency = 1, Value = ReadSettingDouble("BorderThickness", 1) };
        var content = new StackPanel { Spacing = 10 };
        content.Children.Add(themePicker); content.Children.Add(backgroundBox); content.Children.Add(borderBox); content.Children.Add(accentBox); content.Children.Add(thickness);
        var dialog = new ContentDialog { Title = "Personalizar WMP X", Content = content, PrimaryButtonText = "Guardar", CloseButtonText = "Cancelar", DefaultButton = ContentDialogButton.Primary, XamlRoot = RootLayout.XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        if (!TryParseColor(backgroundBox.Text, out var background) || !TryParseColor(borderBox.Text, out var border) || !TryParseColor(accentBox.Text, out var accent))
        {
            StatusText.Text = "Algún color no es válido. Usa el formato HEX #RRGGBB."; return;
        }
        var theme = themePicker.SelectedItem as string ?? "Oscuro";
        _settings.Values["Theme"] = theme; _settings.Values["BackgroundColor"] = ToHex(background); _settings.Values["BorderColor"] = ToHex(border); _settings.Values["AccentColor"] = ToHex(accent); _settings.Values["BorderThickness"] = thickness.Value;
        ApplyAppearance(theme, background, border, accent, thickness.Value);
        StatusText.Text = $"Apariencia guardada · {theme}";
    }

    private void ApplySavedAppearance()
    {
        var theme = _settings.Values["Theme"] as string ?? "Oscuro"; var palette = GetThemeColors(theme);
        var background = TryParseColor(_settings.Values["BackgroundColor"] as string, out var bg) ? bg : palette.Background;
        var border = TryParseColor(_settings.Values["BorderColor"] as string, out var br) ? br : palette.Border;
        var accent = TryParseColor(_settings.Values["AccentColor"] as string, out var ac) ? ac : palette.Accent;
        ApplyAppearance(theme, background, border, accent, ReadSettingDouble("BorderThickness", 1));
    }

    private void ApplyAppearance(string theme, Color background, Color border, Color accent, double thickness)
    {
        var colors = GetThemeColors(theme); RootLayout.Background = new SolidColorBrush(background); SidebarPanel.Background = new SolidColorBrush(colors.Sidebar); QueuePanel.Background = new SolidColorBrush(colors.Panel); FooterPanel.Background = new SolidColorBrush(colors.Footer);
        WindowFrame.BorderBrush = new SolidColorBrush(border); WindowFrame.BorderThickness = new Thickness(Math.Clamp(thickness, 0, 8)); AlbumCover.BorderBrush = new SolidColorBrush(border); AlbumCover.BorderThickness = new Thickness(Math.Min(thickness, 4)); AlbumCover.Background = new SolidColorBrush(colors.Cover); Application.Current.Resources["AccentBrush"] = new SolidColorBrush(accent);
    }

    private static ThemePalette GetThemeColors(string theme) => theme switch
    {
        "Medianoche" => new("#080C1C", "#11162C", "#171D36", "#1C2340", "#33365F", "#494B78", "#A78BFA"), "Grafito" => new("#17191D", "#202329", "#252930", "#30343B", "#414750", "#525A66", "#38BDF8"), "Neblina" => new("#202A35", "#293744", "#344452", "#425564", "#60798A", "#8198A5", "#9DD6E8"), "Océano" => new("#071D2B", "#0C293A", "#10374A", "#14455A", "#226A7F", "#38879A", "#48D1CC"), "Aurora" => new("#15132B", "#211C3A", "#292347", "#352B59", "#55447D", "#77679F", "#B9A1FF"), "Rosa" => new("#281923", "#35212E", "#442B3B", "#55364B", "#86526B", "#A56F88", "#FF8FB8"), "Ámbar" => new("#241D13", "#332719", "#42321F", "#55412A", "#816037", "#A17C4F", "#FFC66D"), _ => new("#101A2A", "#172438", "#19283B", "#202F47", "#344E70", "#425571", "#3A9DFF")
    };

    private static bool TryParseColor(string? value, out Color color)
    {
        color = Colors.Transparent; if (string.IsNullOrWhiteSpace(value)) return false; var hex = value.Trim().TrimStart('#');
        if (hex.Length == 6 && uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var rgb)) { color = Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb); return true; } return false;
    }
    private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    private double ReadSettingDouble(string key, double fallback) { try { return Convert.ToDouble(_settings.Values[key] ?? fallback); } catch { return fallback; } }

    private async void OpenMedia_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        if (_videoMode)
        {
            foreach (var ext in new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mpeg", ".mpg", ".m4v", ".ts", ".m2ts", ".ogv", ".3gp", ".vob" }) picker.FileTypeFilter.Add(ext);
        }
        else
        {
            foreach (var ext in new[] { ".mp3", ".wav", ".m4a", ".wma", ".aac", ".flac", ".ogg", ".opus", ".aiff" }) picker.FileTypeFilter.Add(ext);
        }
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        IReadOnlyList<StorageFile> files;
        try { files = await picker.PickMultipleFilesAsync(); }
        catch (Exception ex) { StatusText.Text = $"No se pudieron abrir los archivos: {ex.Message}"; return; }
        if (_videoMode)
        {
            foreach (var file in files) if (!_videos.Any(item => item.File.Path.Equals(file.Path, StringComparison.OrdinalIgnoreCase))) _videos.Add(new VideoItem(file));
            TrackList.ItemsSource = _videos; QueueStatus.Text = $"{_videos.Count} vídeos";
            if (files.Count > 0) { StatusText.Text = "Vídeos añadidos a la lista"; if (_currentVideoIndex < 0 && _videos.Count > 0) PlayVideo(0); }
        }
        else
        {
            foreach (var file in files) if (!_tracks.Any(item => item.File.Path.Equals(file.Path, StringComparison.OrdinalIgnoreCase))) _tracks.Add(new TrackItem(file));
            TrackList.ItemsSource = _tracks; QueueStatus.Text = $"{_tracks.Count} canciones";
            if (files.Count > 0) { StatusText.Text = "Archivos añadidos a la lista"; if (_currentIndex < 0 && _tracks.Count > 0) PlayTrack(0); }
        }
        if (files.Count == 0) StatusText.Text = "No se seleccionaron archivos";
    }

    private void SetVideoMode(bool enabled)
    {
        _videoMode = enabled; AudioContent.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible; VideoContent.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        QueueTitle.Text = enabled ? "Vídeos" : "Lista de reproducción"; QueueIcon.Symbol = enabled ? Symbol.Video : Symbol.Bullets; OpenMediaLabel.Text = enabled ? "Abrir vídeos" : "Abrir archivos de audio"; OpenMediaIcon.Symbol = enabled ? Symbol.Video : Symbol.Add;
        TrackList.ItemsSource = enabled ? _videos : _tracks; QueueStatus.Text = enabled ? $"{_videos.Count} vídeos" : $"{_tracks.Count} canciones";
        if (enabled) { _audioPlayer.Pause(); SetPlaybackIcon(false); StatusText.Text = _videos.Count == 0 ? "Elige Abrir vídeos para cargar archivos" : "Selecciona un vídeo de la lista"; }
        else { _videoPlayer.Pause(); VideoEmptyState.Visibility = Visibility.Visible; StatusText.Text = "Modo música"; }
    }

    private void TrackList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (_videoMode && e.ClickedItem is VideoItem video) PlayVideo(_videos.IndexOf(video));
        else if (!_videoMode && e.ClickedItem is TrackItem track) PlayTrack(_tracks.IndexOf(track));
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_videoMode)
        {
            if (_currentVideoIndex < 0) { if (_videos.Count > 0) PlayVideo(0); else OpenMedia_Click(sender, e); return; }
            if (_videoPlayer.IsPlaying) _videoPlayer.Pause(); else _videoPlayer.Play();
            SetPlaybackIcon(_videoPlayer.IsPlaying); return;
        }
        if (_currentIndex < 0) { if (_tracks.Count > 0) PlayTrack(0); else OpenMedia_Click(sender, e); return; }
        if (_audioPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing) { _audioPlayer.Pause(); SetPlaybackIcon(false); } else { _audioPlayer.Play(); SetPlaybackIcon(true); }
    }

    private void SetPlaybackIcon(bool playing) => PlayPauseButton.Content = new SymbolIcon(playing ? Symbol.Pause : Symbol.Play);
    private void Previous_Click(object sender, RoutedEventArgs e)
    {
        if (_videoMode) { if (_videos.Count > 0) PlayVideo((_currentVideoIndex - 1 + _videos.Count) % _videos.Count); return; }
        if (_tracks.Count == 0) return; if (_audioPlayer.PlaybackSession.Position.TotalSeconds > 3) _audioPlayer.PlaybackSession.Position = TimeSpan.Zero; else PlayTrack((_currentIndex - 1 + _tracks.Count) % _tracks.Count);
    }
    private void Next_Click(object sender, RoutedEventArgs e) => PlayNext();
    private void PlayNext()
    {
        if (_videoMode) { if (_videos.Count > 0) PlayVideo(NextIndex(_currentVideoIndex, _videos.Count)); return; }
        if (_tracks.Count == 0) return; var next = NextIndex(_currentIndex, _tracks.Count); if (!_repeat && !_shuffle && _currentIndex == _tracks.Count - 1) { _audioPlayer.Pause(); SetPlaybackIcon(false); return; } PlayTrack(next);
    }
    private int NextIndex(int current, int count) => _shuffle && count > 1 ? Random.Shared.Next(count) : (current + 1) % count;

    private void PlayTrack(int index)
    {
        if (index < 0 || index >= _tracks.Count) return; _currentIndex = index; var track = _tracks[index]; TrackTitle.Text = track.Title; TrackArtist.Text = track.Artist; StatusText.Text = track.File.Name;
        _audioPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(track.File); _audioPlayer.Play(); SetPlaybackIcon(true); TrackList.SelectedIndex = index; UpdateTimeline();
    }

    private void PlayVideo(int index)
    {
        if (index < 0 || index >= _videos.Count) return; _currentVideoIndex = index; var video = _videos[index];
        try
        {
            var media = new LibVLCSharp.Shared.Media(_libVlc, video.File.Path, FromType.FromPath);
            if (!_videoPlayer.Play(media)) { media.Dispose(); StatusText.Text = "No se pudo iniciar este vídeo o no tiene un formato compatible."; return; }
            _currentVideoMedia?.Dispose(); _currentVideoMedia = media; VideoEmptyState.Visibility = Visibility.Collapsed; StatusText.Text = video.File.Name; SetPlaybackIcon(true); TrackList.SelectedIndex = index; UpdateTimeline();
        }
        catch (Exception ex) { StatusText.Text = $"Error al abrir el vídeo: {ex.Message}"; }
    }

    private void VideoPlayer_EndReached(object? sender, EventArgs e) => DispatcherQueue.TryEnqueue(PlayNext);
    private void Player_MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args) => DispatcherQueue.TryEnqueue(PlayNext);

    private void UpdateTimeline()
    {
        _changingSlider = true;
        if (_videoMode)
        {
            if (_currentVideoIndex >= 0)
            {
                var duration = _videoPlayer.Length; var position = _videoPlayer.Time;
                PositionSlider.Maximum = duration > 0 ? duration / 1000d : 1; PositionSlider.Value = Math.Clamp(position / 1000d, 0, PositionSlider.Maximum);
                ElapsedText.Text = FormatTime(TimeSpan.FromMilliseconds(Math.Max(0, position))); DurationText.Text = duration > 0 ? FormatTime(TimeSpan.FromMilliseconds(duration)) : "0:00";
            }
        }
        else if (_currentIndex >= 0)
        {
            var session = _audioPlayer.PlaybackSession; var duration = session.NaturalDuration; var position = session.Position;
            PositionSlider.Maximum = duration.TotalSeconds > 0 ? duration.TotalSeconds : 1; PositionSlider.Value = Math.Min(position.TotalSeconds, PositionSlider.Maximum); ElapsedText.Text = FormatTime(position); DurationText.Text = duration.TotalSeconds > 0 ? FormatTime(duration) : "0:00";
        }
        _changingSlider = false;
    }

    private void PositionSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_changingSlider) return;
        if (_videoMode && _currentVideoIndex >= 0 && _videoPlayer.Length > 0) _videoPlayer.Time = (long)(e.NewValue * 1000);
        else if (!_videoMode && _currentIndex >= 0 && PositionSlider.Maximum > 1) _audioPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(e.NewValue);
    }

    private void Shuffle_Click(object sender, RoutedEventArgs e) { _shuffle = !_shuffle; ShuffleButton.Foreground = new SolidColorBrush(_shuffle ? GetThemeColors(_settings.Values["Theme"] as string ?? "Oscuro").Accent : Colors.White); StatusText.Text = _shuffle ? "Reproducción aleatoria activada" : "Reproducción aleatoria desactivada"; }
    private void Repeat_Click(object sender, RoutedEventArgs e) { _repeat = !_repeat; _audioPlayer.IsLoopingEnabled = _repeat; _videoPlayer.EnableHardwareDecoding = true; RepeatButton.Foreground = new SolidColorBrush(_repeat ? GetThemeColors(_settings.Values["Theme"] as string ?? "Oscuro").Accent : Colors.White); StatusText.Text = _repeat ? "Repetición activada" : "Repetición desactivada"; }
    private static string FormatTime(TimeSpan time) => $"{(int)time.TotalMinutes}:{time.Seconds:00}";

    private void Home_Click(object sender, RoutedEventArgs e) { SetVideoMode(false); StatusText.Text = "Inicio · Reproduce tu música local"; }
    private void Music_Click(object sender, RoutedEventArgs e) { SetVideoMode(false); StatusText.Text = "Música · Añade canciones con ‘Abrir archivos de audio’"; }
    private void Videos_Click(object sender, RoutedEventArgs e) => SetVideoMode(true);
    private void Podcasts_Click(object sender, RoutedEventArgs e) { SetVideoMode(false); StatusText.Text = "Podcasts · Biblioteca de podcasts próximamente"; }
    private void Playlists_Click(object sender, RoutedEventArgs e) { SetVideoMode(false); StatusText.Text = "Listas de reproducción · Próximamente"; }
    private void Library_Click(object sender, RoutedEventArgs e) { SetVideoMode(false); StatusText.Text = "Biblioteca · Añade archivos locales para empezar"; }

    private sealed record ThemePalette(string BackgroundHex, string SidebarHex, string PanelHex, string FooterHex, string CoverHex, string BorderHex, string AccentHex)
    {
        public Color Background => Parse(BackgroundHex); public Color Sidebar => Parse(SidebarHex); public Color Panel => Parse(PanelHex); public Color Footer => Parse(FooterHex); public Color Cover => Parse(CoverHex); public Color Border => Parse(BorderHex); public Color Accent => Parse(AccentHex);
        private static Color Parse(string hex) => Color.FromArgb(255, Convert.ToByte(hex.Substring(1, 2), 16), Convert.ToByte(hex.Substring(3, 2), 16), Convert.ToByte(hex.Substring(5, 2), 16));
    }
}

public sealed class TrackItem(StorageFile file)
{
    public StorageFile File { get; } = file; public string Title { get; } = Path.GetFileNameWithoutExtension(file.Name); public string Artist { get; } = "Archivo local";
}

public sealed class VideoItem(StorageFile file)
{
    public StorageFile File { get; } = file; public string Title { get; } = Path.GetFileNameWithoutExtension(file.Name); public string Artist { get; } = "Vídeo local";
}
