using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace MusicPlayer.Components;

public enum MusicType
{
    None,
    Menu,
    ActiveWave,
    BossRiot,
    BossQueen,
    BossReaper
}

public class AudioLoader : MonoBehaviour
{
    private readonly string _musicFolder =
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Music");

    private readonly Dictionary<string, AudioClip> _audioCache = new();
    private const float FadeSpeed = 1f;
    public static AudioLoader Instance { get; private set; }
    public AudioSource AudioSource;
    public MusicType CurrentMusicType = MusicType.None;
    private string _currentPlayingPath;
    private float _targetVolume = 1f;
    private float _maxVolume = 1f;

    private async void Awake()
    {
        try
        {
            Instance = this;
            if (AudioSource == null)
            {
                AudioSource = gameObject.AddComponent<AudioSource>();
            }

            AudioSource.loop = true;
            AudioSource.volume = 0f;

            if (Directory.Exists(_musicFolder))
            {
                var files = Directory.GetFiles(_musicFolder, "*.mp3");
                foreach (var file in files)
                {
                    var clip = await LoadAudioClip(file);
                    if (clip != null)
                    {
                        _audioCache[file] = clip;
                        MusicPlayer.Logger.LogInfo($"Cached audio: {file}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MusicPlayer.Logger.LogError($"Awake() error: {ex}");
        }
    }

    private void Update()
    {
        if (PersistenceController.instance?.soundsMenu?.saveAudio == null)
        {
            return;
        }

        var masterVolumeGame = PersistenceController.instance.soundsMenu.saveAudio.master / 100f;
        var musicVolumeCfg = MusicPlayer.MusicVolumeCfg.Value / 100f;
        _maxVolume = masterVolumeGame * musicVolumeCfg;

        if (AudioSource != null)
        {
            AudioSource.volume =
                Mathf.MoveTowards(AudioSource.volume, _targetVolume * _maxVolume, Time.deltaTime * FadeSpeed);

            if (_targetVolume <= 0f && AudioSource.volume <= 0f && AudioSource.isPlaying)
            {
                AudioSource.Stop();
            }
        }
    }

    private void LoadAndPlay(string filePath)
    {
        if (_currentPlayingPath == filePath &&
            (AudioSource.isPlaying || !_audioCache.ContainsKey(filePath))) return;

        if (_audioCache.TryGetValue(filePath, out var clip))
        {
            AudioSource.clip = clip;
            if (!AudioSource.isPlaying) AudioSource.Play();
            _currentPlayingPath = filePath;
        }
        else
        {
            _currentPlayingPath = filePath;
            MusicPlayer.Logger.LogWarning($"Audio clip not found in cache: {filePath}. Attempting on-demand load.");
            LoadAndPlayOnDemand(filePath);
        }
    }

    private async void LoadAndPlayOnDemand(string filePath)
    {
        try
        {
            var clip = await LoadAudioClip(filePath);
            if (clip != null)
            {
                _audioCache[filePath] = clip;

                if (_currentPlayingPath == filePath)
                {
                    AudioSource.clip = clip;
                    if (!AudioSource.isPlaying) AudioSource.Play();
                }
            }
        }
        catch (Exception ex)
        {
            MusicPlayer.Logger.LogError($"LoadAndPlayOnDemand() error: {ex}");
        }
    }

    private async Task<AudioClip> LoadAudioClip(string path)
    {
        var uri = "file://" + path;

        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG);
        await www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            return DownloadHandlerAudioClip.GetContent(www);
        }

        MusicPlayer.Logger.LogError($"Failed to load audio: {www.error}");
        return null;
    }

    private void ActuallyPlayMusic(string path, MusicType musicType)
    {
        if (CurrentMusicType == musicType && _currentPlayingPath == path)
        {
            _targetVolume = 1f;
            return;
        }

        CurrentMusicType = musicType;

        if (File.Exists(path))
        {
            _targetVolume = 1f;
            LoadAndPlay(path);
        }
        else
        {
            _currentPlayingPath = path;
            _targetVolume = 0f;
            MusicPlayer.Logger.LogError($"Music not found at: {path}");
        }
    }

    public void PlayMusic(MusicType musicType, int? waveTier = 1, int? phase = null)
    {
        switch (musicType)
        {
            case MusicType.Menu:
                var menuMusicPath = Path.Combine(_musicFolder, "Menu.mp3");
                ActuallyPlayMusic(menuMusicPath, MusicType.Menu);

                break;

            case MusicType.ActiveWave:
                waveTier = Math.Clamp(waveTier ?? 1, 1, 3);
                var waveMusicPath = Path.Combine(_musicFolder, $"Wave{waveTier}.mp3");
                ActuallyPlayMusic(waveMusicPath, MusicType.ActiveWave);

                break;

            case MusicType.BossRiot:
            case MusicType.BossQueen:
            case MusicType.BossReaper:
                var baseName = musicType.ToString();
                var bossMusicPath = phase > 0
                    ? Path.Combine(_musicFolder, $"{baseName}_{phase}.mp3")
                    : Path.Combine(_musicFolder, $"{baseName}.mp3");

                if (phase > 0 && !File.Exists(bossMusicPath))
                {
                    bossMusicPath = Path.Combine(_musicFolder, $"{baseName}.mp3");
                }

                ActuallyPlayMusic(bossMusicPath, musicType);

                break;

            default:
                MusicPlayer.Logger.LogError($"Got: {musicType}. Did you forget something?");
                break;
        }
    }

    public void StopMusic()
    {
        CurrentMusicType = MusicType.None;
        _targetVolume = 0f;
    }
}
