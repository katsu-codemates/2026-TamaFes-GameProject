using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// BGM・SEの再生と音量を管理するクラス。
/// シーンごとに1つ配置し、AudioManager.Instance から呼び出す(シーン遷移で破棄される)。
/// 鳴らす音は SoundCatalog に登録したID(BgmId / SeId)で指定する。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("サウンド定義")]
    [SerializeField] private SoundCatalog catalog;

    [Header("AudioMixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup seGroup;

    [Tooltip("AudioMixerで公開(Expose)した音量パラメータ名")]
    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [SerializeField] private string bgmVolumeParam = "BgmVolume";
    [SerializeField] private string seVolumeParam = "SeVolume";

    [Header("初期音量 (0〜1)")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float seVolume = 1f;

    [Header("SE")]
    [Tooltip("SEの同時発音数")]
    [SerializeField, Min(1)] private int seSourceCount = 8;

    // 無音扱いにする下限の音量(dB)
    private const float MinVolumeDb = -80f;

    // 音量設定を保存するPlayerPrefsのキー(BaseScene/GameSceneのAudioManagerで共通)
    private const string MasterVolumeKey = "Volume.Master";
    private const string BgmVolumeKey = "Volume.Bgm";
    private const string SeVolumeKey = "Volume.Se";

    // Inspectorで設定した初期音量。保存済み設定のリセット時に使う
    private float defaultMasterVolume;
    private float defaultBgmVolume;
    private float defaultSeVolume;

    // クロスフェード用に2本持ち、交互に使う
    private AudioSource[] bgmSources;
    private int activeBgmIndex;
    private BgmId currentBgm = BgmId.None;

    private AudioSource[] seSources;
    private int nextSeIndex;

    // 歓声などの環境音を鳴らし続けるループ専用のSE
    private AudioSource loopSeSource;
    private SeId currentLoopSe = SeId.None;
    private float currentLoopSeBaseVolume = 1f;

    private bool hasWarnedNoMixer;

    public BgmId CurrentBgm => currentBgm;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        // Inspectorの初期音量を退避してから、保存済みの音量があれば読み込む
        defaultMasterVolume = masterVolume;
        defaultBgmVolume = bgmVolume;
        defaultSeVolume = seVolume;
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, masterVolume);
        bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, bgmVolume);
        seVolume = PlayerPrefs.GetFloat(SeVolumeKey, seVolume);

        bgmSources = new AudioSource[2];
        for (int i = 0; i < bgmSources.Length; i++)
        {
            bgmSources[i] = CreateSource($"BGM_{i}", bgmGroup, true);
        }

        seSources = new AudioSource[seSourceCount];
        for (int i = 0; i < seSources.Length; i++)
        {
            seSources[i] = CreateSource($"SE_{i}", seGroup, false);
        }

        loopSeSource = CreateSource("SE_Loop", seGroup, true);
    }

    private void Start()
    {
        // AudioMixer.SetFloatはAwake中だと反映されないことがあるため、Startで初期音量を適用する
        ApplyVolume(masterVolumeParam, masterVolume);
        ApplyVolume(bgmVolumeParam, bgmVolume);
        ApplyVolume(seVolumeParam, seVolume);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;

        // シーン遷移時に破棄済みのAudioSourceをTweenが触らないよう止めておく
        foreach (var source in bgmSources) source.DOKill();
        loopSeSource.DOKill();
    }

    private AudioSource CreateSource(string name, AudioMixerGroup group, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f; // 2Dサウンド
        source.outputAudioMixerGroup = group;
        return source;
    }

    #region BGM

    /// <summary>
    /// BGMを再生する。別のBGMが流れていればクロスフェードで切り替える。
    /// 同じBGMが既に流れている場合は何もしない。
    /// </summary>
    public void PlayBgm(BgmId id, float fadeDuration = 1f)
    {
        if (id == BgmId.None)
        {
            StopBgm(fadeDuration);
            return;
        }
        if (id == currentBgm && bgmSources[activeBgmIndex].isPlaying) return;
        if (catalog == null)
        {
            Debug.LogWarning("[AudioManager] SoundCatalogが設定されていません。");
            return;
        }
        if (!catalog.TryGetBgm(id, out var entry)) return;

        // 今鳴っている方をフェードアウト
        FadeOutAndStop(bgmSources[activeBgmIndex], fadeDuration);

        // もう一方のSourceで新しい曲をフェードイン
        activeBgmIndex = 1 - activeBgmIndex;
        var next = bgmSources[activeBgmIndex];
        next.DOKill();
        next.clip = entry.clip;
        next.Play();

        if (fadeDuration > 0f)
        {
            next.volume = 0f;
            next.DOFade(entry.volume, fadeDuration);
        }
        else
        {
            next.volume = entry.volume;
        }

        currentBgm = id;
        Debug.Log($"[AudioManager] BGM「{id}」を再生します。");
    }

    /// <summary>
    /// 再生中のBGMを止める。
    /// </summary>
    public void StopBgm(float fadeDuration = 1f)
    {
        foreach (var source in bgmSources)
        {
            FadeOutAndStop(source, fadeDuration);
        }
        currentBgm = BgmId.None;
    }

    public void PauseBgm()
    {
        foreach (var source in bgmSources)
        {
            source.Pause();
            DOTween.Pause(source);
        }
    }

    public void ResumeBgm()
    {
        foreach (var source in bgmSources)
        {
            if (source.clip == null) continue;
            source.UnPause();
            DOTween.Play(source);
        }
    }

    private void FadeOutAndStop(AudioSource source, float fadeDuration)
    {
        source.DOKill();
        if (!source.isPlaying) return;

        if (fadeDuration > 0f)
        {
            source.DOFade(0f, fadeDuration).OnComplete(source.Stop);
        }
        else
        {
            source.Stop();
        }
    }

    #endregion

    #region SE

    /// <summary>
    /// SEを1回鳴らす。
    /// </summary>
    public void PlaySe(SeId id)
    {
        PlaySe(id, 1f);
    }

    /// <summary>
    /// SEを1回鳴らす。volumeScaleはカタログに設定した音量への倍率。
    /// </summary>
    public void PlaySe(SeId id, float volumeScale)
    {
        if (id == SeId.None) return;
        if (catalog == null)
        {
            Debug.LogWarning("[AudioManager] SoundCatalogが設定されていません。");
            return;
        }
        if (!catalog.TryGetSe(id, out var entry)) return;

        var source = GetFreeSeSource();
        source.clip = entry.clip;
        source.volume = Mathf.Clamp01(entry.volume * volumeScale);
        source.pitch = entry.GetRandomPitch();
        source.Play();

        Debug.Log($"[AudioManager] SE「{id}」を再生します。");
    }

    /// <summary>
    /// 再生中のSEをすべて止める。
    /// </summary>
    public void StopAllSe()
    {
        foreach (var source in seSources)
        {
            source.Stop();
        }
        StopLoopSe(0f);
        Debug.Log($"[AudioManager] すべてのSEを停止します。");
    }

    /// <summary>
    /// ループSEを再生する。同じSEが既に流れている場合は頭出しせず音量だけ変える。
    /// volumeScaleはカタログに設定した音量への倍率。0を指定すると無音のまま再生を始める。
    /// </summary>
    public void PlayLoopSe(SeId id, float volumeScale = 1f, float fadeDuration = 0f)
    {
        if (id == SeId.None)
        {
            StopLoopSe(fadeDuration);
            return;
        }
        if (id == currentLoopSe && loopSeSource.isPlaying)
        {
            SetLoopSeVolume(volumeScale, fadeDuration);
            return;
        }
        if (catalog == null)
        {
            Debug.LogWarning("[AudioManager] SoundCatalogが設定されていません。");
            return;
        }
        if (!catalog.TryGetSe(id, out var entry)) return;

        loopSeSource.DOKill();
        loopSeSource.clip = entry.clip;
        loopSeSource.pitch = entry.GetRandomPitch();
        loopSeSource.volume = 0f;
        loopSeSource.Play();

        currentLoopSe = id;
        currentLoopSeBaseVolume = entry.volume;
        SetLoopSeVolume(volumeScale, fadeDuration);

        Debug.Log($"[AudioManager] ループSE「{id}」を再生します。");
    }

    /// <summary>
    /// 再生中のループSEの音量を、再生位置を保ったまま変える。volumeScaleはカタログに設定した音量への倍率。
    /// </summary>
    public void SetLoopSeVolume(float volumeScale, float fadeDuration = 0f)
    {
        if (currentLoopSe == SeId.None) return;

        float target = Mathf.Clamp01(currentLoopSeBaseVolume * volumeScale);
        loopSeSource.DOKill();
        if (fadeDuration > 0f)
        {
            // スローモーション演出(timeScale変更)中もフェード時間が変わらないよう、実時間で進める
            loopSeSource.DOFade(target, fadeDuration).SetUpdate(true);
        }
        else
        {
            loopSeSource.volume = target;
        }
    }

    /// <summary>
    /// 再生中のループSEを止める。
    /// </summary>
    public void StopLoopSe(float fadeDuration = 0.5f)
    {
        currentLoopSe = SeId.None;
        loopSeSource.DOKill();
        if (!loopSeSource.isPlaying) return;

        if (fadeDuration > 0f)
        {
            loopSeSource.DOFade(0f, fadeDuration).SetUpdate(true).OnComplete(loopSeSource.Stop);
        }
        else
        {
            loopSeSource.Stop();
        }
    }

    // 空いているSourceを返す。全部使用中なら順番に古いものから上書きする
    private AudioSource GetFreeSeSource()
    {
        for (int i = 0; i < seSources.Length; i++)
        {
            int index = (nextSeIndex + i) % seSources.Length;
            if (!seSources[index].isPlaying)
            {
                nextSeIndex = (index + 1) % seSources.Length;
                return seSources[index];
            }
        }

        var source = seSources[nextSeIndex];
        nextSeIndex = (nextSeIndex + 1) % seSources.Length;
        return source;
    }

    #endregion

    #region 音量

    public float GetMasterVolume() => masterVolume;
    public float GetBgmVolume() => bgmVolume;
    public float GetSeVolume() => seVolume;

    /// <summary>全体の音量を設定する(0〜1)。</summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolume(masterVolumeParam, masterVolume);
        SaveVolume(MasterVolumeKey, masterVolume);
    }

    /// <summary>BGMの音量を設定する(0〜1)。</summary>
    public void SetBgmVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        ApplyVolume(bgmVolumeParam, bgmVolume);
        SaveVolume(BgmVolumeKey, bgmVolume);
    }

    /// <summary>SEの音量を設定する(0〜1)。</summary>
    public void SetSeVolume(float volume)
    {
        seVolume = Mathf.Clamp01(volume);
        ApplyVolume(seVolumeParam, seVolume);
        SaveVolume(SeVolumeKey, seVolume);
    }

    /// <summary>
    /// 保存済みの音量設定を削除し、Inspectorで設定した初期音量に戻す。
    /// </summary>
    public void ResetVolumesToDefault()
    {
        PlayerPrefs.DeleteKey(MasterVolumeKey);
        PlayerPrefs.DeleteKey(BgmVolumeKey);
        PlayerPrefs.DeleteKey(SeVolumeKey);
        PlayerPrefs.Save();

        masterVolume = defaultMasterVolume;
        bgmVolume = defaultBgmVolume;
        seVolume = defaultSeVolume;
        ApplyVolume(masterVolumeParam, masterVolume);
        ApplyVolume(bgmVolumeParam, bgmVolume);
        ApplyVolume(seVolumeParam, seVolume);
    }

    private static void SaveVolume(string key, float volume)
    {
        PlayerPrefs.SetFloat(key, volume);
        PlayerPrefs.Save();
    }

    // 0〜1の音量をdBに変換してAudioMixerに反映する
    private void ApplyVolume(string paramName, float volume)
    {
        if (mixer == null)
        {
            if (!hasWarnedNoMixer)
            {
                Debug.LogWarning("[AudioManager] AudioMixerが設定されていないため、音量設定は反映されません。");
                hasWarnedNoMixer = true;
            }
            return;
        }

        float db = volume <= 0.0001f ? MinVolumeDb : 20f * Mathf.Log10(volume);
        if (!mixer.SetFloat(paramName, db))
        {
            Debug.LogWarning($"[AudioManager] AudioMixerに公開パラメータ「{paramName}」が見つかりません。");
        }
    }

    #endregion
}
