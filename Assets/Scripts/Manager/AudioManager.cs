using System.Collections;
using System.Collections.Generic;
using ByteLoop.Manager;
using ByteLoop.Tool;
using System;
using UnityEngine;

public class AudioManager :PersistentMonoSingleton<AudioManager>
{
    [SerializeField] public AudioClip[] audioClips;
    // public Dictionary<string,AudioClip> audioDic = new Dictionary<string, AudioClip>();
    [SerializeField] public AudioSource audioSource;

    // 音效独立通道，不与 BGM/语音共用
    private AudioSource _sfxSource;
    private AudioSource _loopSource;
    private readonly Dictionary<SfxType, AudioClip> _sfxClips = new Dictionary<SfxType, AudioClip>();

    private AudioListener audioListener;

    protected override void Awake()
    {
        base.Awake();
        InitSfx();
    }

    // —— 音效：全部来自 Resources/Sound Effect，之前没有接 ——
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
        LoadSfx(SfxType.Whoosh, "Sound Effect/Run");
    }

    private void LoadSfx(SfxType type, string path)
    {
        var clip = Resources.Load<AudioClip>(path);
        if (clip != null) _sfxClips[type] = clip;
        else Debug.LogWarning("音效缺失: " + path);
    }

    public void PlaySfx(SfxType type, float volume = 1f)
    {
        if (_sfxSource == null) return;
        if (_sfxClips.TryGetValue(type, out var clip))
        {
            _sfxSource.PlayOneShot(clip, volume);
        }
    }

    /// <summary>循环音效（锅里的滋滋声），重复调用只保留一路。</summary>
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

    public void Play(Music music){
        if(audioSource==null) return;
        // AudioClip audioClip = Resources.Load<AudioClip>(name);
        // audioSource.clip = audioClip;
        // audioSource.Play();
        audioSource.PlayOneShot(audioClips[(int)music]);
    }

// 
    public void PlayBGM(Music music,bool loop){
        // Debug.Log(bgmName);
        if(audioSource==null) return;
        AudioClip audioClip = audioClips[(int)music];
        Debug.Log(audioClip);
        audioSource.clip = audioClip;
        audioSource.loop = loop;
        audioSource.Play();
    }

    private void CheckAudioListener(){
        if(audioListener==null){
            this.gameObject.AddComponent<AudioListener>();
        }
    }
    public void Stop(){
        if(audioSource==null) return;
        audioSource.Stop();
    }


   



}

 public  enum Music{
        None=-1,
        MainMenuBGM = 0,
        MenuBGM =1,
        Fifth=2,
        First=3,
        Fourth=4,
        Second=5,
        Third=6,
    }

/// <summary>游戏内音效枚举，与 Resources/Sound Effect 下的素材一一对应</summary>
public enum SfxType
{
    PourBatter,  // 倒面糊（Fire）
    Sizzle,      // 煎制滋滋声（Fried，循环）
    EggBreak,    // 打鸡蛋
    Coin,        // 收钱（上餐成功）
    Give,        // 递餐/下一位顾客
    Clock,       // 计时滴答/换天
    UiClick,     // UI 点击
    MenuClick,   // 菜单点击
    Whoosh,      // 过场呼啸（Run）
}