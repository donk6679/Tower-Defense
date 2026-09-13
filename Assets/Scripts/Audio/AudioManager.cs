using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局音频管理器：
/// - 音效从 Resources/Audio/SFX/&lt;名称&gt; 读取并缓存；
/// - BGM 从 Resources/Audio/BGM/&lt;名称&gt; 读取；
/// - 场景里没有手动放置时，会在第一次播放时自动创建并跨场景保留。
/// </summary>
public sealed class AudioManager : MonoBehaviour
{
    private const string SfxFolder = "Audio/SFX/";
    private const string BgmFolder = "Audio/BGM/";

    public static AudioManager Instance { get; private set; }

    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.6f;
    [SerializeField, Min(1)] private int sfxSourceCount = 6;
    [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.05f;
    [SerializeField, Min(0f)] private float minReplayInterval = 0.03f;

    private AudioSource[] sfxSources;
    private AudioSource musicSource;
    private int nextSfxSource;

    private static readonly Dictionary<string, AudioClip> ClipCache =
        new Dictionary<string, AudioClip>();

    private readonly Dictionary<string, float> lastPlayTimes =
        new Dictionary<string, float>();

    public static AudioManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        GameObject managerObject = new GameObject("AudioManager");
        Instance = managerObject.AddComponent<AudioManager>();
        DontDestroyOnLoad(managerObject);
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSources = new AudioSource[Mathf.Max(1, sfxSourceCount)];
        for (int i = 0; i < sfxSources.Length; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = sfxVolume;
            sfxSources[i] = source;
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = musicVolume;
    }

    public static void PlaySfx(string clipName, float volumeScale = 1f)
    {
        if (string.IsNullOrEmpty(clipName))
            return;

        EnsureInstance().PlaySfxInternal(clipName, volumeScale);
    }

    public static void PlayBgm(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
            return;

        EnsureInstance().PlayBgmInternal(clipName);
    }

    public static void StopBgm()
    {
        if (Instance != null && Instance.musicSource != null)
            Instance.musicSource.Stop();
    }

    private void PlaySfxInternal(string clipName, float volumeScale)
    {
        AudioClip clip = LoadClip(SfxFolder, clipName);
        if (clip == null || sfxSources == null || sfxSources.Length == 0)
            return;

        // 同一音效短时间内重复触发时跳过，避免敌人多时爆音
        if (minReplayInterval > 0f)
        {
            if (lastPlayTimes.TryGetValue(clipName, out float lastTime) &&
                Time.unscaledTime - lastTime < minReplayInterval)
            {
                return;
            }

            lastPlayTimes[clipName] = Time.unscaledTime;
        }

        AudioSource source = sfxSources[nextSfxSource];
        nextSfxSource = (nextSfxSource + 1) % sfxSources.Length;

        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    private void PlayBgmInternal(string clipName)
    {
        AudioClip clip = LoadClip(BgmFolder, clipName);
        if (clip == null || musicSource == null)
            return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    private static AudioClip LoadClip(string folder, string clipName)
    {
        string key = folder + clipName;
        if (ClipCache.TryGetValue(key, out AudioClip cached))
            return cached;

        AudioClip clip = Resources.Load<AudioClip>(key);
        ClipCache[key] = clip;
        return clip;
    }
}
