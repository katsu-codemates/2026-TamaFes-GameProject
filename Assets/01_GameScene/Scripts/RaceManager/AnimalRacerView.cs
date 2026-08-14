using UnityEngine;
using DG.Tweening;

/// <summary>
/// 動物の移動やアニメーションを制御するクラス。
/// 速度は毎ティック再計算されるため、移動は短い区間のTweenをつなげていく形にし
/// ランダム性のある動きと滑らかさを同時に表現する。
/// </summary>

public class AnimalRacerView : MonoBehaviour
{
    [SerializeField] private float tickInterval = 0.3f; // 速度を再計算する間隔
    
    private RaceParticipant participant;
    private SpriteRenderer spriteRenderer;
    private int totalParticipantCount;
    private bool isRacing;

    public void SetUp(RaceParticipant participant, int totalParticipantCount)
    {
        this.participant = participant;
        this.totalParticipantCount = totalParticipantCount;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 初期位置を設定
        transform.position = RaceTrack.GetWorldPosition(participant.progress, participant.laneIndex, totalParticipantCount);

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

    public void StartRacing()
    {
        isRacing = true;
        RaceTick();
    }

    private void RaceTick()
    {
        if (!isRacing) return;

        // 速度を更新
        participant.UpdateSpeed();
        participant.progress += participant.currentSpeed * tickInterval / RaceTrack.TrackLength;
        participant.progress = Mathf.Clamp01(participant.progress);

        // 新しい位置を計算
        Vector3 targetPosition = RaceTrack.GetWorldPosition(
            participant.progress,
            participant.laneIndex, 
            totalParticipantCount
        );

        // Tweenで移動
        transform.DOMove(targetPosition, tickInterval)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                if(participant.progress >= 1f)
                {
                    // ゴールに到達した場合の処理
                    isRacing = false;
                    RaceManager.Instance.NotifyFinished(participant);
                }
                else
                {
                    RaceTick(); // 次のティックへ
                }
            });
    }
}
