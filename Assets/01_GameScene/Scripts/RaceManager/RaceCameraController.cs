using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;
using System.Collections;

//  変更予定内容
//      ・水平移動のみでなく、カメラ設置ポイントからのフォーカスを交える
//      ・運イベント発生時、該当者にカメラが寄るようにする
//      ・ゴール時はゴールラインを垂直に映すようにする
//      ・手振れを追加する
//      ・フォーカス対象を場合によって動的に変更できるようにする   
//            
//  方針
//      CinemachineBrainによる仮想カメラを使用（各場所または走者に対し配置し、slerpによる滑らかなカメラ移動を行う）   
//      手振れはパーリンノイズを使用
//      フォーカス対象は運やスタミナ切れ等のイベントによって変更する
//

/// <summary>
/// レースの先頭を追従するカメラの制御クラス。
/// </summary>



public class RaceCameraController : MonoBehaviour
{

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



    [Header("斜め上から見下ろす角度。固定")]
    [SerializeField] private Vector3 fixedAngles = new Vector3(35f, -135f, 0f);

    [Header("先頭からどれだけ後方に位置するか")]
    [SerializeField] private float distanceBehind = 10f;

    [Header("カメラの高さ")]
    [SerializeField] private float height = 5f;



    [Header("カメラの追従の滑らかさ")]
    [SerializeField] private float smoothTime = 0.3f;

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

    private Vector3 velocity;
    private Vector3 focusVelocity;
    private List<RaceParticipant> participants;
    private RaceParticipant focusedParticipant;
    private Coroutine focusRoutine;
    private Coroutine shakeRoutine;

    private CinemachineBasicMultiChannelPerlin followNoise;
    private  CinemachineBasicMultiChannelPerlin focusNoise;

    private CinemachineBrain brain;
    private bool hasTriggeredGoalCamera;

    private void Awake()
    {
        // transform.eulerAngles = fixedAngles;

        brain=GetComponent<CinemachineBrain>();

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
    }

    private void OnEnable()
    {
        RaceEventBus.OnAccidentStarted+=HandleLuckEvent;
        RaceEventBus.OnMiracleStarted+=HandleLuckEvent;
    }
    private void OnDisable()
    {
        RaceEventBus.OnAccidentStarted -= HandleLuckEvent;
        RaceEventBus.OnMiracleStarted -= HandleLuckEvent;
    }
    private void HandleLuckEvent(RaceParticipant participant)
    {
        FocusOnParticipant(participant);
    }

    private void LateUpdate()
    {
        if (participants == null || participants.Count == 0||followVCam ==null) return;

        // 先頭の参加者を見つける
        RaceParticipant leader = GetLeader();
        Vector3 leaderPosition = RaceTrack.GetWorldPosition(leader.progress, leader.laneIndex, participants.Count);

        Quaternion fixedRotation=Quaternion.Euler(fixedAngles);
        Vector3 targetPosition=leaderPosition
            -(fixedRotation*Vector3.forward)
            *distanceBehind
            +Vector3.up*height;
        
        Transform camTransform = followVCam.transform;
        camTransform.position = Vector3.SmoothDamp(camTransform.position, targetPosition, ref velocity, smoothTime);
        camTransform.rotation = fixedRotation;

        if (focusedParticipant != null && focusVCam!=null)
        {
            // 走者の現在位置
            Vector3 participantPosition = RaceTrack.GetWorldPosition(
                focusedParticipant.progress,focusedParticipant.laneIndex,participants.Count
            );

            // レースの進行方向
            Vector3 sideDirection = RaceTrack.LaneDirection.normalized;

            Transform cam = focusVCam.transform;

            // 走者の横8m、高さ1.5m
            cam.position = participantPosition
                            - sideDirection * 8f
                            + Vector3.up * 1.5f;

            // 走者を見る
            cam.LookAt(participantPosition);
        }

        //transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }

    private RaceParticipant GetLeader()
    {
        RaceParticipant leader = participants[0];
        foreach (var participant in participants)
        {
            if (participant.progress > leader.progress)
            {
                leader = participant;
            }
        }
        return leader;
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
    public void FocusOnParticipant(RaceParticipant target, float duration = 2.5f)
    {
        if (focusVCam == null || target == null) return;
 
        focusedParticipant = target;
        focusVCam.Priority = focusPriority;
 
        if (focusRoutine != null) StopCoroutine(focusRoutine);
        focusRoutine = StartCoroutine(ReleaseFocusAfter(duration));
 
        ShakeOnce(eventShakeAmplitude, eventShakeDuration, focusNoise);
    }
 
    private IEnumerator ReleaseFocusAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (focusVCam != null) focusVCam.Priority = 0;
        focusedParticipant = null;
        focusRoutine = null;
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

}
