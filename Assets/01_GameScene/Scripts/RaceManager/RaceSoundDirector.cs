using UnityEngine;

/// <summary>
/// 画面に映っている内容に合わせてSEを鳴らす演出クラス。
/// ・アクシデント／ミラクル：RaceEventBusから受け取ってSEを鳴らす(イベント自体が画面内の走者でのみ発火する)
/// ・観客：カメラに観客が映っている間、歓声をループでフェードイン／アウトさせる(レース中のみ)
/// </summary>
public class RaceSoundDirector : MonoBehaviour
{
    [Header("判定に使うカメラ（未設定ならMainCamera）")]
    [SerializeField] private Camera targetCamera;

    [Header("イベントSE：同じSEを連続で鳴らさない間隔（秒）")]
    [SerializeField] private float eventSeCooldown = 0.3f;

    [Header("歓声：音量（カタログの音量への倍率）")]
    [SerializeField, Range(0f, 1f)] private float cheerVolume = 1f;

    [Header("歓声：フェード時間（秒）")]
    [SerializeField] private float cheerFadeIn = 0.8f;
    [SerializeField] private float cheerFadeOut = 1.2f;

    [Header("歓声：観客が映らなくなってからフェードアウトを始めるまでの猶予（秒）。カット切替時のちらつき防止")]
    [SerializeField] private float cheerReleaseDelay = 0.5f;

    [Header("歓声：観客が映っているかを調べる間隔（秒）")]
    [SerializeField] private float checkInterval = 0.1f;

    private readonly Plane[] frustumPlanes = new Plane[6];

    private bool isCheerActive;
    private bool isCheerAudible;
    private float checkTimer;
    private float lastAudienceVisibleTime;

    private float lastAccidentSeTime = float.NegativeInfinity;
    private float lastMiracleSeTime = float.NegativeInfinity;

    private void OnEnable()
    {
        RaceEventBus.OnAccidentStarted += HandleAccident;
        RaceEventBus.OnMiracleStarted += HandleMiracle;
    }

    private void OnDisable()
    {
        RaceEventBus.OnAccidentStarted -= HandleAccident;
        RaceEventBus.OnMiracleStarted -= HandleMiracle;
    }

    #region イベントSE

    private void HandleAccident(RaceParticipant p)
    {
        PlayEventSe(SeId.Accident, ref lastAccidentSeTime);
    }

    private void HandleMiracle(RaceParticipant p)
    {
        PlayEventSe(SeId.Miracle, ref lastMiracleSeTime);
    }

    private void PlayEventSe(SeId id, ref float lastPlayedTime)
    {
        if (AudioManager.Instance == null) return;

        // ミラクルのスロー演出中でも間隔が変わらないよう実時間で測る
        float now = Time.unscaledTime;
        if (now - lastPlayedTime < eventSeCooldown) return;
        lastPlayedTime = now;

        AudioManager.Instance.PlaySe(id);
    }

    #endregion

    #region 歓声

    /// <summary>
    /// 歓声の演出を開始する。観客が映るまでは無音のままループを流しておく。
    /// </summary>
    public void BeginCheer()
    {
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.PlayLoopSe(SeId.Cheer, 0f);
        isCheerActive = true;
        isCheerAudible = false;
        checkTimer = checkInterval; // 開始直後のフレームで判定する
        lastAudienceVisibleTime = float.NegativeInfinity;
    }

    /// <summary>
    /// 歓声の演出を終了する。
    /// </summary>
    public void StopCheer()
    {
        if (!isCheerActive) return;
        isCheerActive = false;
        isCheerAudible = false;

        if (AudioManager.Instance != null) AudioManager.Instance.StopLoopSe(cheerFadeOut);
    }

    private void Update()
    {
        if (!isCheerActive) return;

        checkTimer += Time.unscaledDeltaTime;
        if (checkTimer < checkInterval) return;
        checkTimer = 0f;

        float now = Time.unscaledTime;
        if (IsAnyAudienceVisible()) lastAudienceVisibleTime = now;

        bool shouldBeAudible = now - lastAudienceVisibleTime <= cheerReleaseDelay;
        if (shouldBeAudible == isCheerAudible) return;
        isCheerAudible = shouldBeAudible;

        if (AudioManager.Instance == null) return;
        if (isCheerAudible)
        {
            AudioManager.Instance.SetLoopSeVolume(cheerVolume, cheerFadeIn);
        }
        else
        {
            AudioManager.Instance.SetLoopSeVolume(0f, cheerFadeOut);
        }
    }

    /// <summary>
    /// ゲームカメラの視錐台に観客が1人でも入っているか。
    /// Renderer.isVisibleはSceneビューのカメラや影の描画でもtrueになるため、カメラから直接判定する。
    /// </summary>
    private bool IsAnyAudienceVisible()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return false;

        GeometryUtility.CalculateFrustumPlanes(cam, frustumPlanes);
        foreach (var viewer in AudienceViewer.All)
        {
            if (viewer.Renderer == null || !viewer.Renderer.enabled) continue;
            if (GeometryUtility.TestPlanesAABB(frustumPlanes, viewer.Renderer.bounds)) return true;
        }
        return false;
    }

    #endregion
}
