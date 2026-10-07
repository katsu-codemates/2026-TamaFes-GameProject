using UnityEngine;
using DG.Tweening;

/// <summary>
/// 動物の移動やアニメーションを制御するクラス。
/// 移動はUpdate()でtransformに直接反映させ、DOTweenは走行以外のアニメーションに使う。
/// そのため、走行するtransformは親に、アニメーションするtransormは子にしなければならない。
/// </summary>

public class AnimalRacerView : MonoBehaviour
{
    [Header("デバッグ用パラメータ閲覧")]
    [SerializeField] private RaceParticipant debugParticipant;

    [Header("見た目・演出用の子オブジェクト")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject spurtFlare;
    [SerializeField] private GameObject fatigueSweat;

    private RaceParticipant participant;
    private RaceTuningConfig raceTuning;
    private int totalParticipantCount;
    private bool notifiedFinish;
    private float postFinishSpeed; // ゴール後に走り続ける速度

    // 前フレームの状態を覚えておき、状態が変化したときだけ演出する
    private bool wasSpurting;
    private bool wasAccident;
    private bool wasMiracle;
    private bool notifiedStaminaDepleted;

    public void SetUp(RaceParticipant participant, RaceTuningConfig raceTuning, int totalParticipantCount)
    {
        this.participant = participant;
        this.raceTuning = raceTuning;
        this.totalParticipantCount = totalParticipantCount;
        debugParticipant = participant;
        spurtFlare.SetActive(false);
        fatigueSweat.SetActive(false);

        RaceSimulator.Initialize(participant, raceTuning);
        Debug.Log($"Initialized:{participant.animalData.animalName}");

        // 初期位置を設定
        transform.position = RaceTrack.GetWorldPosition(0f, participant.laneIndex, totalParticipantCount);

        // 見た目(既存の仕組みを流用)
        StartCoroutine(ImageLoader.LoadSpriteFromBase64(
            participant.animalData.createdAt, // キャッシュ用の一意なIDとしてcreatedAtを使用
            participant.animalData.imageBase64,
            onSuccess: (loadedSprite) =>
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = loadedSprite;
                    spriteRenderer.flipX = true;
                }
            },
            onError: (error) =>
            {
                Debug.LogError($"画像の取得に失敗しました: name={participant.animalData.animalName}, url={participant.animalData.imageBase64}, error={error}");
            }
        ));
    }

    private void Update()
    {
        if (participant == null) return;

        // ゴール後はシミュレーションを止め、見た目だけゴールテープの先へ走り続けさせる
        if (participant.isFinished)
        {
            transform.position += RaceTrack.ForwardDirection * postFinishSpeed * Time.deltaTime;
            return;
        }

        RaceSimulator.Tick(participant, Time.deltaTime, raceTuning);

        transform.position = RaceTrack.GetWorldPosition(participant.progress, participant.laneIndex, totalParticipantCount);

        HandleEffectTransitions();

        if (participant.isFinished && !notifiedFinish)
        {
            notifiedFinish = true;
            spurtFlare.SetActive(false);
            fatigueSweat.SetActive(false);
            postFinishSpeed = participant.currentSpeed * raceTuning.postFinishSpeedRatio;
            RaceManager.Instance.NotifyFinished(participant);
        }
    }

    /// <summary>
    /// 状態がfalse -> trueになったときだけ、visualRootに演出を発火
    /// </summary>
    private void HandleEffectTransitions()
    {
        // スパート演出
        if (participant.isSpurting && !wasSpurting)
        {
            visualRoot.DOKill();
            visualRoot.DOLocalRotate(new Vector3(0, 0, 0), 0f);
            visualRoot.DOPunchScale(Vector3.one * 1f, duration: 0.35f, vibrato: 6, elasticity: 0.5f);
            spurtFlare.SetActive(true);

            if (IsOnScreen())
            {
                RaceEventBus.RaiseSpurtStarted(participant);
            }
            Debug.Log($"{participant.animalData.animalName}がラストスパート！ progress={participant.progress}");
        }
        wasSpurting = participant.isSpurting;

        // アクシデント演出
        if (participant.isAccident && !wasAccident)
        {
            visualRoot.DOKill();
            visualRoot.DOLocalRotate(new Vector3(0, 0, -360f), duration: participant.accidentTimer, RotateMode.FastBeyond360)
                .SetEase(Ease.OutBack);

            if (IsOnScreen())
            {
                RaceEventBus.RaiseAccidentStarted(participant);
            }
            Debug.Log($"{participant.animalData.animalName}がアクシデント！");
        }
        wasAccident = participant.isAccident;

        // ミラクル演出
        if (participant.isMiracle && !wasMiracle)
        {
            visualRoot.DOKill();
            visualRoot.DOPunchScale(Vector3.one * 1f, participant.miracleTimer, vibrato: 8, elasticity: 0.6f);

            if (IsOnScreen())
            {
                RaceEventBus.RaiseMiracleStarted(participant);
            }
            Debug.Log($"{participant.animalData.animalName}がミラクル！");
        }
        wasMiracle = participant.isMiracle;

        // スタミナ切れ：バテによって実際に減速し始めたタイミングで一度だけ通知する。
        // （減速はRaceSimulatorの終盤処理でスタミナ比率がfatigueThresholdを切ったときに始まる。
        //   スパート中はボーナスで減速が打ち消されて見えないため、スパート終了時に通知される）
        if (!notifiedStaminaDepleted
            && participant.isFatigued
            && !participant.isSpurting)
        {
            notifiedStaminaDepleted = true;

            if (IsOnScreen())
            {
                RaceEventBus.RaiseStaminaDepleted(participant);
            }
            Debug.Log($"{participant.animalData.animalName}がスタミナ切れ！");
            fatigueSweat.SetActive(true);
        }
    }

    public bool IsOnScreen()
    {
        return spriteRenderer.isVisible;
    }

    public RaceParticipant GetParticipant()
    {
        return participant;
    }
}
