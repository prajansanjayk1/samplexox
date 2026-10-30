using System.IO;
using System.Media;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Core.Services;
using QuietoraFocus.Infrastructure.Logging;

namespace QuietoraFocus.Services;

public sealed class SoundService : IDisposable
{
    private SoundPlayer? _player;
    private MemoryStream? _currentStream;
    private AmbientSoundType _currentType = AmbientSoundType.None;

    public AmbientSoundType CurrentType => _currentType;
    public bool IsPlaying => _currentType != AmbientSoundType.None;

    public void PlaySound(AmbientSoundType type)
    {
        Stop();

        if (type == AmbientSoundType.None) return;

        try
        {
            var bytes = AmbientSoundGenerator.GenerateWavBytes(type, durationSeconds: 6.0);
            _currentStream = new MemoryStream(bytes);
            _player = new SoundPlayer(_currentStream);
            _player.Load();
            _player.PlayLooping();
            _currentType = type;
            AppLogger.LogInfo($"Ambient focus sound started: {type}");
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"Error playing ambient sound {type}", ex);
            Stop();
        }
    }

    public void Stop()
    {
        try
        {
            _player?.Stop();
            _player?.Dispose();
            _player = null;

            _currentStream?.Dispose();
            _currentStream = null;

            _currentType = AmbientSoundType.None;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Error stopping ambient sound", ex);
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
