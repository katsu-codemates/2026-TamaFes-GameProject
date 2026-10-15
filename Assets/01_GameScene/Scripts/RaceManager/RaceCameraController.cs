using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;
using System.Collections;

/// <summary>
/// レースの走者を追従するカメラの制御クラス。
/// カメラの距離を変えて（寄り引き）なるべく全員を画面に収める。
/// 最大まで引いても入りきらないときは、1位から入るだけ映す前方ショットと、
/// 入りきらなかった走者を映す後続ショットを交互にカットで切り替える。
/// </summary>
public class RaceCameraController : MonoBehaviour
{
    [Header("レースマネージャー")]
    [SerializeField] private RaceManager raceManager;

    [Header("出走者追従用の仮想カメラ")]
    [SerializeField] private CinemachineCamera followVCam;

    [Header("運イベント等発生時の寄り用の仮想カメラ")]
    [SerializeField] private CinemachineCamera focusVCam;

    [Header("ゴールカメラ")]
    [SerializeField] private CinemachineCamera goalVCam;

    [Header("フォーカス対象のダミー")]
    [SerializeField] private Transform focusDummyTarget;

    [Header("フォーカスカメラ設定")]
    [SerializeField] private float focusSideDistance=8f;
    [SerializeField] private float focusHeight=1.5f;
    [SerializeField] private float focusSmoothTime=0.15f;

    // 実況側がこの値を呼んで、テロップの協調表示などの演出に使える。
    [Header("フォーカスの規定継続時間")]
    [SerializeField] private float defaultFocusDuration = 2.5f;
    public float DefaultFocusDuration => defaultFocusDuration;

    [Header("斜め上から見下ろす角度。固定")]
    [SerializeField] private Vector3 fixedAngles = new Vector3(35f, -135f, 0f);

    [Header("寄り引き：最も寄った時のカメラ距離（少頭数・密集時）")]
    [SerializeField] private float minDistance = 25f;

    [Header("寄り引き：最も引いた時のカメラ距離。これで入りきらない走者は後続ショットで映す")]
    [SerializeField] private float maxDistance = 120f;

    [Header("寄り引き：画面端の余白（画角に掛ける率。小さいほど余白が大きい）")]
    [Range(0.3f, 1f)]
    [SerializeField] private float screenMargin = 0.85f;

    [Header("寄り引き：動物の絵の大きさ分の余白（ワールド単位）")]
    [SerializeField] private float racerPadding = 2f;

    [Header("寄り引き：引く／寄る時の滑らかさ（寄りは遅めにして往復を防ぐ）")]
    [SerializeField] private float zoomOutSmoothTime = 0.4f;
    [SerializeField] private float zoomInSmoothTime = 1.0f;

    [Header("カメラの高さ")]
    [SerializeField] private float height = 5f;

    [Header("カメラの追従の滑らかさ")]
    [SerializeField] private float smoothTime = 0.3f;

    [Header("ショット切替：全員がこの率×最大距離に収まったら全員ショットに戻る（ちらつき防止のため1未満）")]
    [Range(0.5f, 1f)]
    [SerializeField] private float regroupDistanceRatio = 0.85f;

    [Header("ショット切替：入りきらない時に前方／後続の各ショットを映す秒数")]
    [SerializeField] private float leaderShotDuration = 4f;
    [SerializeField] private float chaserShotDuration = 3f;
    public float ChaserShotDuration => chaserShotDuration;

    [Header("カメラ位置の優先度")]
    [SerializeField] private int basePriority=10;
    [SerializeField] private int focusPriority=20;
    [SerializeField] private int goalPriority=30;

    [Header("ゴールカメラへ切り替える先頭のprogress閾値（ゴール直前）")]
    [SerializeField] private float goalCameraTriggerProgress = 0.95f;

    [Header("手振れ値")]
    [SerializeField] private float defaultShakeAmplitude = 0.3f;
    [SerializeField] private float defaultShakeFrequency = 1.5f;
    [SerializeField] private float eventShakeAmplitude = 1.2f;
    [SerializeField] private float eventShakeDuration = 0.6f;

    [Header("スローモーション設定")]
    [SerializeField] private float slowMotionScale = 0.35f;
    [SerializeField] private float slowMotionDuration = 0.5f;

    private Vector3 velocity;
    private Vector3 smoothedCenter;
    private float currentDistance = -1f;
    private float distanceVelocity;
    private Vector3 focusVelocity;
    private List<RaceParticipant> participants;
    private RaceParticipant focusedParticipant;
    private Coroutine focusRoutine;
    private Coroutine shakeRoutine;
    private Coroutine slowMotionRoutine;

    private CinemachineBasicMultiChannelPerlin followNoise;
    private  CinemachineBasicMultiChannelPerlin focusNoise;

    private CinemachineBrain brain;
    private bool hasTriggeredGoalCamera;

    private float defaultFixedDeltaTime;

    // 実況側がカメラの切り替わりに合わせてコメントを出すためのイベント
    public event System.Action<RaceParticipant> OnGoalCameraStarted;   // 引数：その時点の先頭
    public event System.Action<List<RaceParticipant>> OnChaserShotStarted; // 引数：後続ショットに映る走者（順位順）
    public bool IsGoalCameraActive => hasTriggeredGoalCamera;

    // 追従カメラのショット種別
    private enum ShotMode
    {
        All,    // 全員が収まる：全員を映す
        Front,  // 入りきらない：1位から入るだけ映す
        Chaser, // 入りきらない：前方ショットに入らなかった走者から入るだけ映す
    }
    private ShotMode currentShot = ShotMode.All;
    private float shotTimer;
    private readonly List<RaceParticipant> ranking = new List<RaceParticipant>();
    private static readonly System.Comparison<RaceParticipant> ByProgressDesc =
        (a, b) => b.progress.CompareTo(a.progress);

    private void Awake()
    {
        // transform.eulerAngles = fixedAngles;

        brain=GetComponent<CinemachineBrain>();
        brain.DefaultBlend.Time = focusSmoothTime;

        // 物理演算のFiixedUpdate間隔も一緒にスケールさせるため、元の値を覚えておく
        defaultFixedDeltaTime = Time.fixedDeltaTime;

        if (focusDummyTarget == null)
        {
            var dummy = new GameObject("RaceCam_FocusTarget");
            focusDummyTarget = dummy.transform;
        }

        SetPriority(followVCam,basePriority);
        SetPriority(focusVCam,0);
        SetPriority(goalVCam,0);

        followNoise=followVCam!=null?followVCam.GetComponent<CinemachineBasicMultiChannelPerlin>():null;
        focusNoise=focusVCam!=null?focusVCam.GetComponent<CinemachineBasicMultiChannelPerlin>():null;

        ApplyNoise(followNoise, defaultShakeAmplitude, defaultShakeFrequency);
        ApplyNoise(focusNoise, defaultShakeAmplitude, defaultShakeFrequency);

        // if (focusVCam != null)
        // {
        //     focusVCam.Follow = focusDummyTarget;
        //     focusVCam.LookAt = focusDummyTarget;
        // }

    }

    public void SetParticipants(List<RaceParticipant> list)
    {
        participants = list;
        hasTriggeredGoalCamera=false;
        currentShot = ShotMode.All;
        shotTimer = 0f;
        // 初回フレームは目標位置へ即座に合わせる
        currentDistance = -1f;
    }

    private void OnEnable()
    {
        RaceEventBus.OnMiracleStarted += HandleLuckEvent;
    }
    private void OnDisable()
    {
        RaceEventBus.OnMiracleStarted -= HandleLuckEvent;

        // スロー効果(timeScale)が、このオブジェクトが無効かされたときにそのままになる事故を防ぐ
        if (slowMotionRoutine != null)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultFixedDeltaTime;
        }
    }
    private void HandleLuckEvent(RaceParticipant participant)
    {
        FocusOnParticipant(participant);

        // スロー演出
        SlowMotionOnce(slowMotionScale, slowMotionDuration);
    }

    private void LateUpdate()
    {
        if (participants == null || participants.Count == 0||followVCam ==null) return;

        // 順位順に並べる（先頭 = ranking[0]）
        UpdateRanking();
        RaceParticipant leader = ranking[0];

        // ゴールカメラ切り替え判定
        if (!hasTriggeredGoalCamera && leader.progress >= goalCameraTriggerProgress)
        {
            hasTriggeredGoalCamera = true;
            TriggerGoalCamera();
        }

        // 全員が収まるかどうかでショットを決める。切り替わった瞬間はカットで映像を切り替える
        bool isCut = UpdateShotMode();
        int startRank, endRank;
        GetShotRange(out startRank, out endRank);
        if (isCut && currentShot == ShotMode.Chaser && !hasTriggeredGoalCamera)
        {
            OnChaserShotStarted?.Invoke(GetMembers(startRank, endRank));
        }

        Vector3 shotCenter;
        float requiredDistance = GetRequiredDistance(startRank, endRank, out shotCenter);
        float targetDistance = Mathf.Clamp(requiredDistance, minDistance, maxDistance);

        if (isCut || currentDistance < 0f)
        {
            // 差が大きいとパンでは映像が流れて見づらいため、瞬時に切り替える
            smoothedCenter = shotCenter;
            currentDistance = targetDistance;
            velocity = Vector3.zero;
            distanceVelocity = 0f;
            followVCam.PreviousStateIsValid = false;
        }
        else
        {
            smoothedCenter = Vector3.SmoothDamp(smoothedCenter, shotCenter, ref velocity, smoothTime);
            // 引くときは見切れないよう速めに、寄るときはゆっくり
            float zoomSmooth = targetDistance > currentDistance ? zoomOutSmoothTime : zoomInSmoothTime;
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomSmooth);
        }

        Quaternion fixedRotation = Quaternion.Euler(fixedAngles);
        Transform camTransform = followVCam.transform;
        camTransform.position = smoothedCenter
            - (fixedRotation * Vector3.forward) * currentDistance
            + Vector3.up * height;
        camTransform.rotation = fixedRotation;

        // focus実行中
        if (focusedParticipant != null && focusVCam != null)
        {
            // 走者の現在位置
            Vector3 participantPosition = RaceTrack.GetWorldPosition(
                focusedParticipant.progress,focusedParticipant.laneIndex,participants.Count
            );

            // レースの進行方向
            Vector3 sideDirection = RaceTrack.LaneDirection.normalized;

            Transform cam = focusVCam.transform;

            // 走者とのカメラの相対的位置
            cam.position = participantPosition
                            - sideDirection * focusSideDistance
                            + Vector3.up * focusHeight;

            // 走者を見る
            cam.LookAt(participantPosition);
        }

        //transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }

    private void UpdateRanking()
    {
        ranking.Clear();
        ranking.AddRange(participants);
        ranking.Sort(ByProgressDesc);
    }

    // 順位a位とb位（0始まり）のトラック上の距離
    private float GetGap(int a, int b)
    {
        return (ranking[a].progress - ranking[b].progress) * RaceTrack.TrackLength;
    }

    /// <summary>
    /// 全員が画面に収まるかどうかに応じてショットを更新する。
    /// カット（瞬時の切り替え）が必要なフレームならtrueを返す。
    /// </summary>
    private bool UpdateShotMode()
    {
        if (ranking.Count < 2) return false;

        ShotMode previousShot = currentShot;
        Vector3 unused;
        float allDistance = GetRequiredDistance(0, ranking.Count, out unused);

        if (currentShot == ShotMode.All)
        {
            if (allDistance > maxDistance)
            {
                // 入りきらなくなった：まずは前方を映す（寄るだけなのでカットしない）
                currentShot = ShotMode.Front;
                shotTimer = 0f;
            }
        }
        else if (allDistance <= maxDistance * regroupDistanceRatio)
        {
            // 十分に縮まったので全員ショットに戻る
            currentShot = ShotMode.All;
        }
        else if (currentShot == ShotMode.Chaser && GetFittingEndRank(0) >= ranking.Count)
        {
            // 前方ショットだけで全員入るようになったので、後続ショットを打ち切る
            currentShot = ShotMode.Front;
            shotTimer = 0f;
        }
        else
        {
            shotTimer += Time.deltaTime;
            float duration = currentShot == ShotMode.Front ? leaderShotDuration : chaserShotDuration;
            if (shotTimer >= duration)
            {
                if (currentShot == ShotMode.Chaser)
                {
                    currentShot = ShotMode.Front;
                }
                else if (GetFittingEndRank(0) < ranking.Count)
                {
                    // 前方ショットに入りきらない走者がいる時だけ後続ショットへ
                    currentShot = ShotMode.Chaser;
                }
                shotTimer = 0f;
            }
        }

        // 後続ショットへの出入りは距離が大きいのでカット。全員⇔前方は近いのでSmoothDampで繋ぐ
        return currentShot != previousShot
            && (currentShot == ShotMode.Chaser || previousShot == ShotMode.Chaser);
    }

    /// <summary>
    /// 現在のショットに映す走者の順位範囲 [startRank, endRank) を返す。
    /// </summary>
    private void GetShotRange(out int startRank, out int endRank)
    {
        switch (currentShot)
        {
            case ShotMode.Front:
                startRank = 0;
                endRank = GetFittingEndRank(0);
                return;
            case ShotMode.Chaser:
                // 前方ショットに入らなかった最上位から
                startRank = Mathf.Min(GetFittingEndRank(0), ranking.Count - 1);
                endRank = GetFittingEndRank(startRank);
                return;
            default:
                startRank = 0;
                endRank = ranking.Count;
                return;
        }
    }

    /// <summary>
    /// 順位startRank（0始まり）から順に走者を足していき、最大距離に収まる直前の順位（排他）を返す。最低1人は含む。
    /// </summary>
    private int GetFittingEndRank(int startRank)
    {
        Vector3 unused;
        int end = startRank + 1;
        while (end < ranking.Count && GetRequiredDistance(startRank, end + 1, out unused) <= maxDistance)
        {
            end++;
        }
        return end;
    }

    /// <summary>
    /// 順位 [startRank, endRank) の走者全員を画面に収めるのに必要なカメラ距離を返す。
    /// centerには走者の範囲の中心（カメラが追う点）を返す。
    /// </summary>
    private float GetRequiredDistance(int startRank, int endRank, out Vector3 center)
    {
        int count = participants.Count;
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        for (int i = startRank; i < endRank; i++)
        {
            Vector3 p = RaceTrack.GetWorldPosition(ranking[i].progress, ranking[i].laneIndex, count);
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        center = (min + max) * 0.5f;

        // 画角（縦・横）の半分のtan。余白の分だけ狭く見積もる
        float tanV = Mathf.Tan(followVCam.Lens.FieldOfView * 0.5f * Mathf.Deg2Rad) * screenMargin;
        Camera outputCamera = brain != null ? brain.OutputCamera : null;
        float aspect = outputCamera != null ? outputCamera.aspect : 16f / 9f;
        float tanH = tanV * aspect;

        // カメラ位置 = center - forward*D + up*height なので、
        // カメラ座標での走者位置は a + (0,0,D)（a = R^-1 * (p - center - up*height)）。
        // 画面内に入る条件 |a.x| <= tanH*(a.z+D), |a.y| <= tanV*(a.z+D) からDの下限を求める
        Quaternion inverseRotation = Quaternion.Inverse(Quaternion.Euler(fixedAngles));
        Vector3 offset = center + Vector3.up * height;
        float required = 0f;
        for (int i = startRank; i < endRank; i++)
        {
            Vector3 p = RaceTrack.GetWorldPosition(ranking[i].progress, ranking[i].laneIndex, count);
            Vector3 a = inverseRotation * (p - offset);
            float byWidth = (Mathf.Abs(a.x) + racerPadding) / tanH - a.z;
            float byHeight = (Mathf.Abs(a.y) + racerPadding) / tanV - a.z;
            required = Mathf.Max(required, Mathf.Max(byWidth, byHeight));
        }
        return required;
    }

    /// <summary>
    /// 順位 [startRank, endRank) の走者の一覧を返す（順位順）。
    /// </summary>
    private List<RaceParticipant> GetMembers(int startRank, int endRank)
    {
        return ranking.GetRange(startRank, endRank - startRank);
    }

    private void SetPriority(CinemachineCamera vCam,int priority)
    {
        if(vCam==null)return;
        vCam.Priority=priority;
    }

    private void ApplyNoise(CinemachineBasicMultiChannelPerlin noise,float amplitude,float frequency)
    {
        if(noise==null)return;
        noise.AmplitudeGain=amplitude;
        noise.FrequencyGain=frequency;
    }

    /// <summary>
    /// 運イベント等で特定の参加者にカメラを寄せる。
    /// duration秒経過後は自動的に通常追従カメラへ戻る。
    /// フォーカス対象は呼び出し側から自由に切り替え可能。
    /// </summary>
    public void FocusOnParticipant(RaceParticipant target, float? duration = null)
    {
        if (focusVCam == null || target == null) return;
        if (hasTriggeredGoalCamera) return; // ゴール演出中はフォーカスしない

        float actualDuration = duration ?? defaultFocusDuration;
 
        focusedParticipant = target;
        focusVCam.Priority = focusPriority;
        
        // フォーカス中に別の対象へフォーカスさせられるとき
        if (focusRoutine != null)
        {
            StopCoroutine(focusRoutine);
            raceManager.ResetTransparency();
        } 
        focusRoutine = StartCoroutine(ReleaseFocusAfter(actualDuration));
 
        ShakeOnce(eventShakeAmplitude, eventShakeDuration, focusNoise);

        raceManager.MakeTransparentUnFocusedRacers(focusedParticipant);
    }
 
    private IEnumerator ReleaseFocusAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (focusVCam != null) focusVCam.Priority = 0;
        focusedParticipant = null;
        focusRoutine = null;
        raceManager.ResetTransparency();
    }
 
    /// <summary>
    /// ゴール時にゴールラインを垂直に映すカメラへ切り替える。
    /// goalVCamはあらかじめゴールラインに対して垂直な位置・向きに配置しておくこと。
    /// </summary>
    public void TriggerGoalCamera()
    {
        if (goalVCam == null) return;
 
        if (focusRoutine != null)
        {
            StopCoroutine(focusRoutine);
            focusRoutine = null;
            raceManager.ResetTransparency();
            focusedParticipant = null;
            if (focusVCam != null) focusVCam.Priority = 0;
        }
 
        goalVCam.Priority = goalPriority;

        if (ranking.Count > 0) OnGoalCameraStarted?.Invoke(ranking[0]);
    }
 
    /// <summary>
    /// 現在ブレンドでアクティブになっているカメラに対して、瞬間的に強めの手振れを加える。
    /// overrideNoiseを指定すればそのVCamのノイズに対して直接演出できる。
    /// </summary>
    public void ShakeOnce(float amplitude, float duration, CinemachineBasicMultiChannelPerlin overrideNoise = null)
    {
        CinemachineBasicMultiChannelPerlin noise = overrideNoise;
 
        if (noise == null && brain != null)
        {
            var activeVCam = brain.ActiveVirtualCamera as CinemachineCamera;
            noise = activeVCam != null ? activeVCam.GetComponent<CinemachineBasicMultiChannelPerlin>() : null;
        }
 
        if (noise == null) return;
 
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine(noise, amplitude, duration));
    }
 
    private IEnumerator ShakeRoutine(CinemachineBasicMultiChannelPerlin noise, float amplitude, float duration)
    {
        float originalAmplitude = noise.AmplitudeGain;
        noise.AmplitudeGain = amplitude;
        yield return new WaitForSeconds(duration);
        noise.AmplitudeGain = originalAmplitude;
        shakeRoutine = null;
    }

    /// <summary>
    /// 一瞬だけスローにする演出。
    /// durationはリアルタイム秒（ゲーム内時間のスケールに関係なく一定の現実時間）で指定
    /// </summary>
    public void SlowMotionOnce(float scale, float duration)
    {
        if (slowMotionRoutine != null)
        {
            StopCoroutine(slowMotionRoutine);
        }
        slowMotionRoutine = StartCoroutine(SlowMotionRoutine(scale, duration));
    }

    private IEnumerator SlowMotionRoutine(float scale, float duration)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * scale;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
        slowMotionRoutine = null;
    }

    public RaceParticipant GetFocusedParticipant()
    {
        return focusedParticipant;
    }
}
