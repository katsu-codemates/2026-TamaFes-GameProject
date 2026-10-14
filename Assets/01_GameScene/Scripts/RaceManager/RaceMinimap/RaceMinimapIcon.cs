using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ミニマップ上に表示する、走者一体分のアイコン。
/// 動物のイラストと出場番号を出す。
/// </summary>
public class RaceMinimapIcon : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI entryNumberText;

    [Header("ゴール後に表示するオブジェクト(任意)")]
    [SerializeField] private GameObject finishedMark;

    private RaceParticipant participant;

    public RaceParticipant Participant => participant;
    public RectTransform RectTransform => (RectTransform)transform;

    public void Setup(RaceParticipant participant)
    {
        this.participant = participant;
        AnimalData data = participant.animalData;

        // 小さいアイコンなので「3番」ではなく数字だけを出す
        if (entryNumberText != null)
        {
            entryNumberText.text = participant.EntryNumber.ToString();
        }

        if (finishedMark != null)
        {
            finishedMark.SetActive(false);
        }

        // 既存のImageLoaderを使用する(キャッシュがあるので出走表と二重読み込みにはならない)
        StartCoroutine(ImageLoader.LoadSpriteFromBase64(
            data.createdAt, // キャッシュ用の一意なIDとしてcreatedAtを使用
            data.imageBase64,
            onSuccess: sprite =>
            {
                if (iconImage != null) iconImage.sprite = sprite;
            },
            onError: err => Debug.LogWarning($"ミニマップ用画像読み込み失敗(name = {data.animalName}: {err})")
        ));
    }

    public void SetFinished(bool finished)
    {
        if (finishedMark != null && finishedMark.activeSelf != finished)
        {
            finishedMark.SetActive(finished);
        }
    }
}
