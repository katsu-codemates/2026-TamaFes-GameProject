using TMPro;
using UnityEngine;

/// <summary>
/// 参加者一覧に並ぶ、一枚分のカード。
/// 画像と出場番号・名前のみ出す。パラメータは出さない。
/// </summary>
public class RaceEntryCardView : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("出場番号専用のテキスト(未設定なら名前の前に番号を付ける)")]
    [SerializeField] private TextMeshProUGUI entryNumberText;

    /// <summary>名前テキストに元々設定されているマテリアル(ダイナミックフォント生成時のシェーダー参照用)</summary>
    public Material NameTemplateMaterial => nameText != null ? nameText.fontSharedMaterial : null;

    /// <param name="nameFont">名前に使うフォント。nullならプレハブ設定のまま</param>
    public void Setup(RaceParticipant participant, TMP_FontAsset nameFont = null)
    {
        AnimalData data = participant.animalData;

        if (nameFont != null)
        {
            nameText.font = nameFont;
        }

        EntryNumberFormatter.Apply(participant, nameText, entryNumberText);

        // 既存のImageLoaderを使用する。
        // キャッシュ機能があるため二重ダウンロードにはならない…はず。
        StartCoroutine(ImageLoader.LoadSpriteFromBase64(
            data.createdAt, // キャッシュ用の一意なIDとしてcreatedAtを使用
            data.imageBase64,
            onSuccess: sprite => iconImage.sprite = sprite,
            onError: err => Debug.LogWarning($"参加者画像読み込み失敗(name = {data.animalName + ".png"}: {err})")
        ));
    }
}
