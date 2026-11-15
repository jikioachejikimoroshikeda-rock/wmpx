using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Wmpx;

public sealed partial class MainWindow
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff", ".webp"
    };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".m4a", ".wma", ".aac", ".flac", ".ogg", ".opus", ".aiff"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mpeg", ".mpg", ".m4v", ".ts", ".m2ts", ".ogv", ".3gp", ".vob"
    };

    private async void Images_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        foreach (var extension in ImageExtensions) picker.FileTypeFilter.Add(extension);
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

        IReadOnlyList<StorageFile> files;
        try { files = await picker.PickMultipleFilesAsync(); }
        catch (Exception ex) { StatusText.Text = $"No se pudieron abrir las imágenes: {ex.Message}"; return; }
        if (files.Count == 0) return;

        await ShowImageViewerAsync(files);
    }

    private async void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

        StorageFolder? folder;
        try { folder = await picker.PickSingleFolderAsync(); }
        catch (Exception ex) { StatusText.Text = $"No se pudo abrir la carpeta: {ex.Message}"; return; }
        if (folder is null) return;

        try
        {
            var files = await folder.GetFilesAsync();
            if (_videoMode)
            {
                foreach (var file in files.Where(file => VideoExtensions.Contains(Path.GetExtension(file.Name))))
                    if (!_videos.Any(item => string.Equals(item.File.Path, file.Path, StringComparison.OrdinalIgnoreCase))) _videos.Add(new VideoItem(file));
                TrackList.ItemsSource = _videos;
                QueueStatus.Text = $"{_videos.Count} vídeos";
                if (_currentVideoIndex < 0 && _videos.Count > 0) PlayVideo(0);
                StatusText.Text = $"Carpeta añadida: {folder.Name}";
            }
            else
            {
                foreach (var file in files.Where(file => AudioExtensions.Contains(Path.GetExtension(file.Name))))
                    if (!_tracks.Any(item => string.Equals(item.File.Path, file.Path, StringComparison.OrdinalIgnoreCase))) _tracks.Add(new TrackItem(file));
                TrackList.ItemsSource = _tracks;
                QueueStatus.Text = $"{_tracks.Count} canciones";
                if (_currentIndex < 0 && _tracks.Count > 0) PlayTrack(0);
                StatusText.Text = $"Carpeta añadida: {folder.Name}";
            }
        }
        catch (Exception ex) { StatusText.Text = $"No se pudo leer la carpeta: {ex.Message}"; }
    }

    private async System.Threading.Tasks.Task ShowImageViewerAsync(IReadOnlyList<StorageFile> images)
    {
        var title = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 13, Margin = new Thickness(0, 0, 0, 8) };
        var image = new Image { Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        var transform = new ScaleTransform { ScaleX = 1, ScaleY = 1 };
        image.RenderTransform = transform;
        image.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        var viewer = new Grid { MinWidth = 640, MinHeight = 400, MaxWidth = 1000, MaxHeight = 650, RowSpacing = 8 };
        viewer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        viewer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        viewer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        viewer.Children.Add(title);
        var scroll = new ScrollViewer { Content = image, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, ZoomMode = ZoomMode.Disabled, Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 12, 16, 24)) };
        Grid.SetRow(scroll, 1);
        viewer.Children.Add(scroll);

        var position = 0;
        var counter = new TextBlock { VerticalAlignment = VerticalAlignment.Center, MinWidth = 66, TextAlignment = TextAlignment.Center };
        var previous = new Button { Content = "‹", MinWidth = 42, ToolTipService = { ToolTip = "Imagen anterior" } };
        var next = new Button { Content = "›", MinWidth = 42, ToolTipService = { ToolTip = "Imagen siguiente" } };
        var zoomOut = new Button { Content = "−", MinWidth = 42, ToolTipService = { ToolTip = "Alejar" } };
        var zoomIn = new Button { Content = "+", MinWidth = 42, ToolTipService = { ToolTip = "Acercar" } };
        var resetZoom = new Button { Content = "Ajustar", MinWidth = 68 };
        var controls = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 8 };
        controls.Children.Add(previous); controls.Children.Add(counter); controls.Children.Add(next);
        controls.Children.Add(new Border { Width = 1, Height = 24, Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(80, 255, 255, 255)), Margin = new Thickness(4, 0, 4, 0) });
        controls.Children.Add(zoomOut); controls.Children.Add(zoomIn); controls.Children.Add(resetZoom);
        Grid.SetRow(controls, 2);
        viewer.Children.Add(controls);

        async System.Threading.Tasks.Task DisplayCurrentAsync()
        {
            var file = images[position];
            try
            {
                using var stream = await file.OpenAsync(FileAccessMode.Read);
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);
                image.Source = bitmap;
                title.Text = file.Name;
                counter.Text = $"{position + 1} / {images.Count}";
                transform.ScaleX = transform.ScaleY = 1;
            }
            catch (Exception ex) { title.Text = $"No se pudo mostrar {file.Name}: {ex.Message}"; image.Source = null; }
        }

        previous.Click += async (_, _) => { position = (position - 1 + images.Count) % images.Count; await DisplayCurrentAsync(); };
        next.Click += async (_, _) => { position = (position + 1) % images.Count; await DisplayCurrentAsync(); };
        zoomOut.Click += (_, _) => transform.ScaleX = transform.ScaleY = Math.Max(0.25, transform.ScaleX - 0.25);
        zoomIn.Click += (_, _) => transform.ScaleX = transform.ScaleY = Math.Min(4, transform.ScaleX + 0.25);
        resetZoom.Click += (_, _) => transform.ScaleX = transform.ScaleY = 1;

        await DisplayCurrentAsync();
        var dialog = new ContentDialog
        {
            Title = "Visor de imágenes",
            Content = viewer,
            CloseButtonText = "Cerrar",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = RootLayout.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
