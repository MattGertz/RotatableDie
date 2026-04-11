using Plugin.Maui.Audio;

namespace YachtDiceMaui.Services;

/// <summary>
/// Centralized sound effect playback. Preloads audio files for low-latency playback.
/// </summary>
public class SoundService
{
    private const int BouncePoolSize = 6;
    private readonly IAudioManager _audioManager;
    private readonly IAudioPlayer?[] _bouncePlayers = new IAudioPlayer?[BouncePoolSize];
    private int _bounceIndex;
    private IAudioPlayer? _togglePlayer;
    private IAudioPlayer? _scorePlayer;
    private IAudioPlayer? _applausePlayer;
    private bool _loaded;

    public bool Enabled { get; set; } = true;

    public SoundService(IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    public async Task PreloadAsync()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            for (int i = 0; i < BouncePoolSize; i++)
                _bouncePlayers[i] = _audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("bounce.wav"));
            _togglePlayer = _audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("toggle-to-from-tray.wav"));
            _scorePlayer = _audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("score.wav"));
            _applausePlayer = _audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("wildapplause.wav"));
        }
        catch
        {
            // Non-critical — sounds just won't play
        }
    }

    public void PlayBounce()
    {
        if (!Enabled) return;
        var player = _bouncePlayers[_bounceIndex];
        if (player == null) return;
        _bounceIndex = (_bounceIndex + 1) % BouncePoolSize;
        player.Seek(0);
        player.Play();
    }

    public void PlayToggle()
    {
        if (!Enabled || _togglePlayer == null) return;
        _togglePlayer.Seek(0);
        _togglePlayer.Play();
    }

    public void PlayScore()
    {
        if (!Enabled || _scorePlayer == null) return;
        _scorePlayer.Seek(0);
        _scorePlayer.Play();
    }

    public void PlayApplause()
    {
        if (!Enabled || _applausePlayer == null) return;
        _applausePlayer.Seek(0);
        _applausePlayer.Play();
    }
}
