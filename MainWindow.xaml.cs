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
    private readonly MediaPlayer _player = new();
    private readonly DispatcherQueueTimer _timer;
    private readonly ApplicationDataContainer _settings = ApplicationData.Current.LocalSettings;
    private int _currentIndex = -1;
    private bool _changingSlider;
    private bool _shuffle;
    private bool _repeat;

    public MainWindow()
    {
        InitializeComponent();
        TrackList.ItemsSource = _tracks;
        ApplySavedAppearance();
        _player.MediaEnded += Player_MediaEnded;
        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(500);
        _timer.Tick += (_, _) => UpdateTimeline();
        _timer.Start();
        Closed += (_, _) => _player.Dispose();
    }

    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        var themePicker = new ComboBox { Header = "Tema", HorizontalAlignment = HorizontalAlignment.Stretch };
        themePicker.Items.Add("Oscuro");
        themePicker.Items.Add("Medianoche");
        themePicker.Items.Add("Grafito");
        themePicker.SelectedItem = _settings.Values["Theme"] as string ?? "Oscuro";

        var backgroundBox = new TextBox
        {
            Header = "Color de fondo (HEX)",
            PlaceholderText = "#101A2A",
            Text = _settings.Values["BackgroundColor"] as string ?? "#101A2A"
        };
        var borderBox = new TextBox
        {
            Header = "Color del borde (HEX)",
            PlaceholderText = "#425571",
            Text = _settings.Values["BorderColor"] as string ?? "#425571"
        };
        var borderThickness = new Slider
        {
            Header = "Grosor del borde",
            Minimum = 0,
            Maximum = 8,
            StepFrequency = 1,
            Value = ReadSettingDouble("BorderThickness", 1)
        };
        var hint = new TextBlock
        {
            Text = "Usa colores HEX como #101A2A. Los cambios se guardan en este equipo.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.8,
            Margin = new Thickness(0, 4, 0, 0)
        };
        var content = new StackPanel { Spacing = 12 };
        content.Children.Add(themePicker);
        content.Children.Add(backgroundBox);
        content.Children.Add(borderBox);
        content.Children.Add(borderThickness);
        content.Children.Add(hint);

        var dialog = new ContentDialog
        {
            Title = "Personalizar WMP X",
            Content = content,
            PrimaryButtonText = "Guardar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = RootLayout.XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        if (!TryParseColor(backgroundBox.Text, out var backgroundColor))
        {
            StatusText.Text = "Color de fondo no válido. Usa el formato #RRGGBB.";
            return;
        }
        if (!TryParseColor(borderBox.Text, out var borderColor))
        {
            StatusText.Text = "Color del borde no válido. Usa el formato #RRGGBB.";
            return;
        }

        var theme = themePicker.SelectedItem as string ?? "Oscuro";
        _settings.Values["Theme"] = theme;
        _settings.Values["BackgroundColor"] = ToHex(backgroundColor);
        _settings.Values["BorderColor"] = ToHex(borderColor);
        _settings.Values["BorderThickness"] = borderThickness.Value;
        ApplyAppearance(theme, backgroundColor, borderColor, borderThickness.Value);
        StatusText.Text = "Personalización guardada";
    }

    private void ApplySavedAppearance()
    {
        var theme = _settings.Values["Theme"] as string ?? "Oscuro";
        var defaults = GetThemeColors(theme);
        var background = TryParseColor(_settings.Values["BackgroundColor"] as string, out var savedBackground)
            ? savedBackground : defaults.Background;
        var border = TryParseColor(_settings.Values["BorderColor"] as string, out var savedBorder)
            ? savedBorder : defaults.Border;
        ApplyAppearance(theme, background, border, ReadSettingDouble("BorderThickness", 1));
    }

    private void ApplyAppearance(string theme, Color background, Color border, double thickness)
    {
        var colors = GetThemeColors(theme);
        RootLayout.Background = new SolidColorBrush(background);
        SidebarPanel.Background = new SolidColorBrush(colors.Sidebar);
        QueuePanel.Background = new SolidColorBrush(colors.Panel);
        FooterPanel.Background = new SolidColorBrush(colors.Footer);
        WindowFrame.BorderBrush = new SolidColorBrush(border);
        WindowFrame.BorderThickness = new Thickness(Math.Clamp(thickness, 0, 8));
        AlbumCover.BorderBrush = new SolidColorBrush(border);
        AlbumCover.BorderThickness = new Thickness(Math.Min(thickness, 4));
        AlbumCover.Background = new SolidColorBrush(colors.Cover);
        Application.Current.Resources["AccentBrush"] = new SolidColorBrush(colors.Accent);
    }

    private static (Color Background, Color Sidebar, Color Panel, Color Footer, Color Cover, Color Border, Color Accent) GetThemeColors(string theme) => theme switch
    {
        "Medianoche" => (FromHex("#080C1C"), FromHex("#11162C"), FromHex("#171D36"), FromHex("#1C2340"), FromHex("#33365F"), FromHex("#494B78"), FromHex("#A78BFA")),
        "Grafito" => (FromHex("#17191D"), FromHex("#202329"), FromHex("#252930"), FromHex("#30343B"), FromHex("#414750"), FromHex("#525A66"), FromHex("#38BDF8")),
        _ => (FromHex("#101A2A"), FromHex("#172438"), FromHex("#19283B"), FromHex("#202F47"), FromHex("#344E70"), FromHex("#425571"), FromHex("#3A9DFF"))
    };

    private static bool TryParseColor(string? value, out Color color)
    {
        color = Colors.Transparent;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var hex = value.Trim().TrimStart('#');
        if (hex.Length == 6 && uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
        {
            color = Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
            return true;
        }
        if (hex.Length == 8 && uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var argb))
        {
            color = Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
            return true;
        }
        return false;
    }

    private static Color FromHex(string value)
    {
        TryParseColor(value, out var color);
        return color;
    }

    private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private double ReadSettingDouble(string key, double fallback)
    {
        try { return Convert.ToDouble(_settings.Values[key] ?? fallback); }
        catch { return fallback; }
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
