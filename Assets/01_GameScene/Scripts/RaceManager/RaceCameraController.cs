using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;
using System.Collections;

/// <summary>
/// レースの先頭集団を追従するカメラの制御クラス。
/// 先頭が独走しているときは、先頭と後続集団のショットを交互にカットで切り替える。
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

    [Header("先頭からどれだけ後方に位置するか")]
    [SerializeField] private float distanceBehind = 10f;

    [Header("カメラの高さ")]
    [SerializeField] private float height = 5f;



    [Header("カメラの追従の滑らかさ")]
    [SerializeField] private float smoothTime = 0.3f;

    [Header("ショット切替：1位と2位の差がこの距離以下なら集団の中心を追う")]
    [SerializeField] private float groupFramingDistance = 25f;

    [Header("ショット切替：独走状態から集団追従に戻る距離（ちらつき防止のため上より小さく）")]
    [SerializeField] private float groupReenterDistance = 18f;

    [Header("ショット切替：独走時に各ショットを映す秒数")]
    [SerializeField] private float leaderShotDuration = 4f;
    [SerializeField] private float chaserShotDuration = 3f;
    public float ChaserShotDuration => chaserShotDuration;

    [Header("ショット切替：2位からこの距離以内の走者を後続集団として映す")]
    [SerializeField] private float chaserGroupRange = 25f;

    [Header("ショット切替：集団として映す最大人数")]
    [SerializeField] private int maxGroupSize = 3;

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
        Group,  // 先頭集団の中心を追う
        Leader, // 独走中：先頭を映す
        Chaser, // 独走中：後続集団を映す
    }
    private ShotMode currentShot = ShotMode.Group;
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
        currentShot = ShotMode.Group;
        shotTimer = 0f;
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
        SlowMortionOnce(slowMotionScale, slowMotionDuration);
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

        // 独走状態かどうかでショットを決める。切り替わった瞬間はカットで映像を切り替える
        bool isCut = UpdateShotMode();
        if (isCut && currentShot == ShotMode.Chaser && !hasTriggeredGoalCamera)
        {
            OnChaserShotStarted?.Invoke(GetGroupMembers(1, chaserGroupRange));
        }
        Vector3 shotCenter = GetShotCenter();

        Quaternion fixedRotation=Quaternion.Euler(fixedAngles);
        Vector3 targetPosition=shotCenter
            -(fixedRotation*Vector3.forward)
            *distanceBehind
            +Vector3.up*height;

        Transform camTransform = followVCam.transform;
        if (isCut)
        {
            // 差が大きいとパンでは映像が流れて見づらいため、瞬時に切り替える
            camTransform.position = targetPosition;
            velocity = Vector3.zero;
            followVCam.PreviousStateIsValid = false;
        }
        else
        {
            camTransform.position = Vector3.SmoothDamp(camTransform.position, targetPosition, ref velocity, smoothTime);
        }
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
    /// 1位と2位の差に応じてショットを更新する。
    /// カット（瞬時の切り替え）が必要なフレームならtrueを返す。
    /// </summary>
    private bool UpdateShotMode()
    {
        if (ranking.Count < 2) return false;

        ShotMode previousShot = currentShot;
        float leaderGap = GetGap(0, 1);

        if (currentShot == ShotMode.Group)
        {
            if (leaderGap > groupFramingDistance)
            {
                // 独走開始：まずは先頭を映す
                currentShot = ShotMode.Leader;
                shotTimer = 0f;
            }
        }
        else if (leaderGap < groupReenterDistance)
        {
            // 差が縮まったので集団追従に戻る
            currentShot = ShotMode.Group;
        }
        else
        {
            shotTimer += Time.deltaTime;
            float duration = currentShot == ShotMode.Leader ? leaderShotDuration : chaserShotDuration;
            if (shotTimer >= duration)
            {
                currentShot = currentShot == ShotMode.Leader ? ShotMode.Chaser : ShotMode.Leader;
                shotTimer = 0f;
            }
        }

        // 後続ショットへの出入りは距離が大きいのでカット。先頭⇔集団は近いのでSmoothDampで繋ぐ
        return currentShot != previousShot
            && (currentShot == ShotMode.Chaser || previousShot == ShotMode.Chaser);
    }

    // 現在のショットでカメラが追う中心点
    private Vector3 GetShotCenter()
    {
        switch (currentShot)
        {
            case ShotMode.Leader:
                return GetGroupCenter(0, 0f);
            case ShotMode.Chaser:
                return GetGroupCenter(1, chaserGroupRange);
            default:
                return GetGroupCenter(0, groupFramingDistance);
        }
    }

    /// <summary>
    /// 順位startRank（0始まり）の走者と、そこからrange以内にいる後続の走者（最大maxGroupSize人）の中心座標を返す。
    /// </summary>
    private Vector3 GetGroupCenter(int startRank, float range)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        for (int i = startRank; i < ranking.Count && count < Mathf.Max(1, maxGroupSize); i++)
        {
            if (i > startRank && GetGap(startRank, i) > range) break;

            RaceParticipant p = ranking[i];
            sum += RaceTrack.GetWorldPosition(p.progress, p.laneIndex, participants.Count);
            count++;
        }
        return sum / count;
    }
    
    /// <summary>
    /// GetGroupCenterと同じ条件で、ショットに映る走者の一覧を返す。
    /// </summary>
    private List<RaceParticipant> GetGroupMembers(int startRank, float range)
    {
        var members = new List<RaceParticipant>();
        for (int i = startRank; i < ranking.Count && members.Count < Mathf.Max(1, maxGroupSize); i++)
        {
            if (i > startRank && GetGap(startRank, i) > range) break;
            members.Add(ranking[i]);
        }
        return members;
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
    public void SlowMortionOnce(float scale, float duration)
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
