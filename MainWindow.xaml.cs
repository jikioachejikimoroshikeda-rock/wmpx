using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Collections.ObjectModel;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Wmpx;

public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<TrackItem> _tracks = new();
    private readonly MediaPlayer _player = new();
    private readonly DispatcherQueueTimer _timer;
    private int _currentIndex = -1;
    private bool _changingSlider;
    private bool _shuffle;
    private bool _repeat;

    public MainWindow()
    {
        InitializeComponent();
        TrackList.ItemsSource = _tracks;
        _player.MediaEnded += Player_MediaEnded;
        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(500);
        _timer.Tick += (_, _) => UpdateTimeline();
        _timer.Start();
        Closed += (_, _) => _player.Dispose();
    }

    private async void OpenFiles_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".mp3");
        picker.FileTypeFilter.Add(".wav");
        picker.FileTypeFilter.Add(".m4a");
        picker.FileTypeFilter.Add(".wma");
        picker.FileTypeFilter.Add(".aac");
        picker.FileTypeFilter.Add(".flac");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

        IReadOnlyList<StorageFile> files;
        try
        {
            files = await picker.PickMultipleFilesAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudieron abrir los archivos: {ex.Message}";
            return;
        }

        foreach (var file in files)
        {
            if (_tracks.Any(track => track.File.Path.Equals(file.Path, StringComparison.OrdinalIgnoreCase)))
                continue;
            _tracks.Add(new TrackItem(file));
        }
        QueueStatus.Text = $"{_tracks.Count} canciones";
        StatusText.Text = files.Count == 0 ? "No se seleccionaron archivos" : "Archivos añadidos a la lista";
        if (_currentIndex < 0 && _tracks.Count > 0)
            PlayTrack(0);
    }

    private void TrackList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is TrackItem track)
            PlayTrack(_tracks.IndexOf(track));
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_currentIndex < 0)
        {
            if (_tracks.Count > 0) PlayTrack(0);
            else OpenFiles_Click(sender, e);
            return;
        }

        if (_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
        {
            _player.Pause();
            PlayPauseButton.Content = "▶";
        }
        else
        {
            _player.Play();
            PlayPauseButton.Content = "Ⅱ";
        }
    }

    private void Previous_Click(object sender, RoutedEventArgs e)
    {
        if (_tracks.Count == 0) return;
        if (_player.PlaybackSession.Position.TotalSeconds > 3)
            _player.PlaybackSession.Position = TimeSpan.Zero;
        else
            PlayTrack((_currentIndex - 1 + _tracks.Count) % _tracks.Count);
    }

    private void Next_Click(object sender, RoutedEventArgs e) => PlayNext();

    private void PlayNext()
    {
        if (_tracks.Count == 0) return;
        int nextIndex = _shuffle && _tracks.Count > 1
            ? Random.Shared.Next(_tracks.Count)
            : (_currentIndex + 1) % _tracks.Count;
        if (!_repeat && !_shuffle && _currentIndex == _tracks.Count - 1)
        {
            _player.Pause();
            PlayPauseButton.Content = "▶";
            return;
        }
        PlayTrack(nextIndex);
    }

    private void PlayTrack(int index)
    {
        if (index < 0 || index >= _tracks.Count) return;
        _currentIndex = index;
        var track = _tracks[index];
        TrackTitle.Text = track.Title;
        TrackArtist.Text = track.Artist;
        StatusText.Text = track.File.Name;
        _player.Source = MediaSource.CreateFromStorageFile(track.File);
        _player.Play();
        PlayPauseButton.Content = "Ⅱ";
        TrackList.SelectedIndex = index;
        UpdateTimeline();
    }

    private void Player_MediaEnded(MediaPlayer sender, object args)
    {
        DispatcherQueue.TryEnqueue(PlayNext);
    }

    private void UpdateTimeline()
    {
        if (_currentIndex < 0) return;
        var session = _player.PlaybackSession;
        var duration = session.NaturalDuration;
        var position = session.Position;
        _changingSlider = true;
        PositionSlider.Maximum = duration.TotalSeconds > 0 ? duration.TotalSeconds : 1;
        PositionSlider.Value = Math.Min(position.TotalSeconds, PositionSlider.Maximum);
        _changingSlider = false;
        ElapsedText.Text = FormatTime(position);
        DurationText.Text = duration.TotalSeconds > 0 ? FormatTime(duration) : "0:00";
    }

    private void PositionSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_changingSlider && _currentIndex >= 0 && PositionSlider.Maximum > 1)
            _player.PlaybackSession.Position = TimeSpan.FromSeconds(e.NewValue);
    }

    private void Shuffle_Click(object sender, RoutedEventArgs e)
    {
        _shuffle = !_shuffle;
        StatusText.Text = _shuffle ? "Reproducción aleatoria activada" : "Reproducción aleatoria desactivada";
    }

    private void Repeat_Click(object sender, RoutedEventArgs e)
    {
        _repeat = !_repeat;
        _player.IsLoopingEnabled = _repeat;
        StatusText.Text = _repeat ? "Repetición activada" : "Repetición desactivada";
    }

    private static string FormatTime(TimeSpan time) => $"{(int)time.TotalMinutes}:{time.Seconds:00}";

    private void Home_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Inicio · Reproduce tu música local";
    private void Music_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Música · Añade canciones con ‘Abrir archivos de audio’";
    private void Videos_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Vídeos · Biblioteca de vídeos próximamente";
    private void Podcasts_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Podcasts · Biblioteca de podcasts próximamente";
    private void Playlists_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Listas de reproducción · Próximamente";
    private void Library_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Biblioteca · Añade archivos locales para empezar";
}

public sealed class TrackItem(StorageFile file)
{
    public StorageFile File { get; } = file;
    public string Title { get; } = Path.GetFileNameWithoutExtension(file.Name);
    public string Artist { get; } = "Archivo local";
}
