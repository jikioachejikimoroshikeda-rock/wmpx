using LibVLCSharp.Shared;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Storage.Pickers;
using Windows.UI;
using WinRT.Interop;

namespace Wmpx;

public sealed partial class MainWindow
{
    private DispatcherQueueTimer? _videoUiTimer;
    private bool _changingVideoSlider;
    private bool _isMuted;
    private double _lastVolume = 80;

    private void VideoContent_Loaded(object sender, RoutedEventArgs e)
    {
        if (_videoUiTimer is not null) return;
        _videoUiTimer = DispatcherQueue.CreateTimer();
        _videoUiTimer.Interval = TimeSpan.FromMilliseconds(250);
        _videoUiTimer.Tick += (_, _) => UpdateVideoProgress();
        _videoUiTimer.Start();
    }

    private void UpdateVideoProgress()
    {
        if (!_videoMode || _currentVideoIndex < 0 || _videoPlayer.Length <= 0) return;
        var duration = _videoPlayer.Length;
        var position = Math.Max(0, _videoPlayer.Time);
        _changingVideoSlider = true;
        VideoProgressSlider.Maximum = duration / 1000d;
        VideoProgressSlider.Value = Math.Clamp(position / 1000d, 0, VideoProgressSlider.Maximum);
        _changingVideoSlider = false;
        VideoElapsedText.Text = FormatTime(TimeSpan.FromMilliseconds(position));
        VideoDurationText.Text = FormatTime(TimeSpan.FromMilliseconds(duration));
    }

    private void VideoProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_changingVideoSlider && _videoMode && _currentVideoIndex >= 0 && _videoPlayer.Length > 0)
            _videoPlayer.Time = (long)(e.NewValue * 1000);
    }

    private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_videoPlayer is not null)
        {
            var volume = (int)Math.Clamp(e.NewValue, 0, 100);
            _videoPlayer.Volume = volume;
            if (volume > 0) _lastVolume = volume;
            if (volume > 0 && _isMuted)
            {
                _isMuted = false;
                _videoPlayer.Mute = false;
                _audioPlayer.IsMuted = false;
                MuteIcon.Symbol = Symbol.Volume;
            }
        }
        if (_audioPlayer is not null)
        {
            _audioPlayer.Volume = Math.Clamp(e.NewValue / 100d, 0, 1);
            if (e.NewValue > 0 && _isMuted) _audioPlayer.IsMuted = false;
        }
    }

    private void Mute_Click(object sender, RoutedEventArgs e)
    {
        _isMuted = !_isMuted;
        _videoPlayer.Mute = _isMuted;
        _audioPlayer.IsMuted = _isMuted;
        if (_isMuted)
        {
            if (VolumeSlider.Value > 0) _lastVolume = VolumeSlider.Value;
            VolumeSlider.Value = 0;
            MuteIcon.Symbol = Symbol.Mute;
        }
        else
        {
            VolumeSlider.Value = _lastVolume > 0 ? _lastVolume : 80;
            MuteIcon.Symbol = Symbol.Volume;
        }
        StatusText.Text = _isMuted ? "Audio silenciado" : "Audio activado";
    }

    private async void OpenSubtitle_Click(object sender, RoutedEventArgs e)
    {
        if (_currentVideoIndex < 0)
        {
            StatusText.Text = "Primero abre un vídeo y después carga sus subtítulos.";
            return;
        }

        var picker = new FileOpenPicker();
        foreach (var extension in new[] { ".srt", ".ass", ".ssa", ".vtt", ".sub" })
            picker.FileTypeFilter.Add(extension);
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

        try
        {
            var file = await picker.PickSingleFileAsync();
            if (file is null) return;
            var subtitleUri = new Uri(file.Path).AbsoluteUri;
            if (!_videoPlayer.AddSlave(MediaSlaveType.Subtitle, subtitleUri, true))
            {
                StatusText.Text = "No se pudo cargar el subtítulo. Comprueba su formato y codificación.";
                return;
            }
            StatusText.Text = $"Subtítulos cargados: {file.Name}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error al cargar subtítulos: {ex.Message}";
        }
    }

    private void Fullscreen_Click(object sender, RoutedEventArgs e)
    {
        if (!_videoMode)
        {
            StatusText.Text = "La pantalla completa está disponible en el modo Vídeos.";
            return;
        }

        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            _isFullscreen = !_isFullscreen;
            appWindow.SetPresenter(_isFullscreen ? AppWindowPresenterKind.FullScreen : AppWindowPresenterKind.Overlapped);

            SidebarPanel.Visibility = _isFullscreen ? Visibility.Collapsed : Visibility.Visible;
            QueuePanel.Visibility = _isFullscreen ? Visibility.Collapsed : Visibility.Visible;
            FooterPanel.Visibility = _isFullscreen ? Visibility.Collapsed : Visibility.Visible;
            WindowFrame.Visibility = _isFullscreen ? Visibility.Collapsed : Visibility.Visible;
            SidebarColumn.Width = new GridLength(_isFullscreen ? 0 : 238);
            FullscreenIcon.Symbol = _isFullscreen ? Symbol.BackToWindow : Symbol.FullScreen;
            VideoOverlay.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            _isFullscreen = false;
            StatusText.Text = $"No se pudo cambiar a pantalla completa: {ex.Message}";
        }
    }
}
