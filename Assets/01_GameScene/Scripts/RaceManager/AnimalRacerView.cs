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
    [SerializeField] private GameObject spurtFrare;
    
    private RaceParticipant participant;
    private RaceTuningConfig raceTuning;
    private int totalParticipantCount;
    private bool nortifiedFinish;

    // 前フレームの状態を覚えておき、状態が変化したときだけ演出する
    private bool wasSpurting;
    private bool wasAccident;
    private bool wasMiracle;

    public void SetUp(RaceParticipant participant, RaceTuningConfig raceTuning, int totalParticipantCount)
    {
        this.participant = participant;
        this.raceTuning = raceTuning;
        this.totalParticipantCount = totalParticipantCount;
        debugParticipant = participant;
        spurtFrare.SetActive(false);
        
        RaceSimulator.Initialize(participant, raceTuning);
        Debug.Log($"Initialized:{participant.animalData.animalName}");

        // 初期位置を設定
        transform.position = RaceTrack.GetWorldPosition(0f, participant.laneIndex, totalParticipantCount);

        // 見た目(既存の仕組みを流用)
        StartCoroutine(ImageLoader.LoadSprite(
            participant.animalData.animalName, 
            participant.animalData.imageUrl,
            onSuccess: (loadedSprite) => {
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = loadedSprite;
                    spriteRenderer.flipX = true;
                }
            },
            onError: (error) => {
                Debug.LogError($"画像の取得に失敗しました: name={participant.animalData.animalName}, url={participant.animalData.imageUrl}, error={error}");
            }
        ));
    }

    private void Update()
    {
        if (participant == null || participant.isFinished) return;

        RaceSimulator.Tick(participant, Time.deltaTime, raceTuning);

        transform.position = RaceTrack.GetWorldPosition(participant.progress, participant.laneIndex, totalParticipantCount);

        HandleEffectTransitions();

        if (participant.isFinished && !nortifiedFinish)
        {
            nortifiedFinish = true;
            spurtFrare.SetActive(false);
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
            spurtFrare.SetActive(true);
            Debug.Log($"{participant.animalData.animalName}がラストスパート！ progress={participant.progress}");
        }
        wasSpurting = participant.isSpurting;

        // アクシデント演出
        if (participant.isAccident && !wasAccident)
        {
            visualRoot.DOKill();
            visualRoot.DOLocalRotate(new Vector3(0, 0, -360f), duration: participant.accidentTimer, RotateMode.FastBeyond360)
                .SetEase(Ease.OutBack);
            Debug.Log($"{participant.animalData.animalName}がアクシデント！");
        }
        wasAccident = participant.isAccident;

        // ミラクル演出
        if (participant.isMiracle && !wasMiracle)
        {
            visualRoot.DOKill();
            visualRoot.DOPunchScale(Vector3.one * 1f, participant.miracleTimer, vibrato: 8, elasticity: 0.6f);
            Debug.Log($"{participant.animalData.animalName}がミラクル！");
        }
        wasMiracle = participant.isMiracle;
    }
}
