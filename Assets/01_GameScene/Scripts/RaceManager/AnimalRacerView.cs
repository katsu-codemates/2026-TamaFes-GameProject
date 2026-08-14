using UnityEngine;
using DG.Tweening;

/// <summary>
/// 動物の移動やアニメーションを制御するクラス。
/// 移動はUpdate()でtransformに直接反映させ、DOTweenは走行以外のアニメーションに使う。
/// そのため、走行するtransformは親に、アニメーションするtransormは子にしなければならない。
/// </summary>

public class AnimalRacerView : MonoBehaviour
{
    [Header("見た目・演出用の子オブジェクト")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
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
                }
            },
            onError: (error) => {
                Debug.LogError($"画像の取得に失敗しました: name={participant.animalData.animalName}, url={participant.animalData.imageUrl}, error={error}");
            }
        ));
    }

    private void Update()
    {
        Debug.Log($"{participant.animalData.animalName}: progress={participant.progress}, speed={participant.currentSpeed}, finished={participant.isFinished}");
        if (participant == null || participant.isFinished) return;

        RaceSimulator.Tick(participant, Time.deltaTime, raceTuning);

        transform.position = RaceTrack.GetWorldPosition(participant.progress, participant.laneIndex, totalParticipantCount);

        HandleEffectTransitions();

        if (participant.isFinished && !nortifiedFinish)
        {
            nortifiedFinish = true;
            RaceManager.Instance.NotifyFinished(participant);
        }
    }

    /// <summary>
    /// 状態がfalse -> trueになったときだけ、visualRootに演出を発火
    /// </summary>
    private void HandleEffectTransitions()
    {
        if (participant.isSpurting && !wasSpurting)
        {
            visualRoot.DOKill();
            visualRoot.DOPunchScale(Vector3.one * 0.25f, duration: 0.35f, vibrato: 6, elasticity: 0.5f);
        }
        wasSpurting = participant.isSpurting;

        if (participant.isAccident && !wasAccident)
        {
            visualRoot.DOKill();
            visualRoot.DOShakePosition(raceTuning.accidentDuration, strength: 0.3f, vibrato: 20);
        }
        wasAccident = participant.isAccident;

        if (participant.isMiracle && !wasMiracle)
        {
            visualRoot.DOKill();
            visualRoot.DOPunchScale(Vector3.one * 0.4f, duration: 0.5f, vibrato: 8, elasticity: 0.6f);
        }
        wasMiracle = participant.isMiracle;
    }
}
