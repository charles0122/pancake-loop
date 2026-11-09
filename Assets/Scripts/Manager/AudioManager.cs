using System.Collections;
using System.Collections.Generic;
using ByteLoop.Tool;
using UnityEngine;

public class AudioManager : PersistentMonoSingleton<AudioManager>
{
    [Header("音乐与角色语音")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip[] npcVoiceClips;
    [SerializeField] public AudioSource audioSource;

    private AudioSource _sfxSource;
    private AudioSource _loopSource;
    private readonly Dictionary<SfxType, AudioClip> _sfxClips = new Dictionary<SfxType, AudioClip>();

    protected override void Awake()
    {
        base.Awake();
        InitSfx();
    }

    private void InitSfx()
    {
        if (_sfxSource != null) return;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;

        _loopSource = gameObject.AddComponent<AudioSource>();
        _loopSource.playOnAwake = false;
        _loopSource.loop = true;

        LoadSfx(SfxType.PourBatter, "Sound Effect/Fire");
        LoadSfx(SfxType.Sizzle, "Sound Effect/Fried");
        LoadSfx(SfxType.EggBreak, "Sound Effect/Egg Break");
        LoadSfx(SfxType.Coin, "Sound Effect/Coin");
        LoadSfx(SfxType.Give, "Sound Effect/Give");
        LoadSfx(SfxType.Clock, "Sound Effect/Clock");
        LoadSfx(SfxType.UiClick, "Sound Effect/Turn UI");
        LoadSfx(SfxType.MenuClick, "Sound Effect/Turn Menu");
    }

    private void LoadSfx(SfxType type, string path)
    {
        var clip = Resources.Load<AudioClip>(path);
        if (clip != null) _sfxClips[type] = clip;
        else Debug.LogWarning("音效缺失: " + path);
    }

    public void PlayNpcVoice(int npcIndex)
    {
        if (audioSource == null || npcVoiceClips == null) return;
        if (npcIndex < 0 || npcIndex >= npcVoiceClips.Length) return;

        var clip = npcVoiceClips[npcIndex];
        if (clip != null) audioSource.PlayOneShot(clip);
    }

    public void PlayBGM(bool loop = true)
    {
        if (audioSource == null || bgmClip == null) return;
        StartCoroutine(PlayBgmWhenReady(loop));
    }

    private IEnumerator PlayBgmWhenReady(bool loop)
    {
        // Streaming 音频由平台边播边解码，不调用 LoadAudioData，避免 WebGL 主线程阻塞。
        if (bgmClip.loadType == AudioClipLoadType.Streaming)
        {
            audioSource.clip = bgmClip;
            audioSource.loop = loop;
            audioSource.Play();
            yield break;
        }

        if (bgmClip.loadState == AudioDataLoadState.Unloaded)
        {
            bgmClip.LoadAudioData();
        }

        float timeout = 10f;
        while (bgmClip.loadState == AudioDataLoadState.Loading && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (bgmClip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogWarning("BGM 加载失败，游戏继续运行。", this);
            yield break;
        }

        audioSource.clip = bgmClip;
        audioSource.loop = loop;
        audioSource.Play();
    }

    public void PlaySfx(SfxType type, float volume = 1f)
    {
        if (_sfxSource == null) return;
        if (_sfxClips.TryGetValue(type, out var clip))
        {
            _sfxSource.PlayOneShot(clip, volume);
        }
    }

    public void PlaySfxLoop(SfxType type, float volume = 0.4f)
    {
        if (_loopSource == null) return;
        if (_sfxClips.TryGetValue(type, out var clip))
        {
            _loopSource.clip = clip;
            _loopSource.volume = volume;
            _loopSource.Play();
        }
    }

    public void StopSfxLoop()
    {
        if (_loopSource != null) _loopSource.Stop();
    }

    public void Stop()
    {
        if (audioSource != null) audioSource.Stop();
        StopSfxLoop();
    }
}

public enum SfxType
{
    PourBatter,
    Sizzle,
    EggBreak,
    Coin,
    Give,
    Clock,
    UiClick,
    MenuClick,
}
