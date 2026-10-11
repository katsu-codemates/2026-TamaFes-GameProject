using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RaceResultRowView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image iconImage;

    [Header("出場番号専用のテキスト(未設定なら名前の前に番号を付ける)")]
    [SerializeField] private TextMeshProUGUI entryNumberText;

    [Header("行の背景プレート(未設定なら色分けしない)")]
    [SerializeField] private Image plateImage;

    [Header("1〜3位の順位文字・プレートの色(金・銀・銅)")]
    [SerializeField] private Color goldColor = new Color(1f, 0.82f, 0.25f);
    [SerializeField] private Color silverColor = new Color(0.85f, 0.88f, 0.95f);
    [SerializeField] private Color bronzeColor = new Color(0.86f, 0.55f, 0.3f);

    public void Setup(RaceParticipant participant)
    {
        rankText.text = FormatRank(participant.finishRank);
        ApplyRankColor(participant.finishRank);
        EntryNumberFormatter.Apply(participant, nameText, entryNumberText);

        StartCoroutine(ImageLoader.LoadSpriteFromBase64(
            participant.animalData.createdAt,
            participant.animalData.imageBase64,
            onSuccess: (sprite) => { iconImage.sprite = sprite; },
            onError: (error) => { Debug.LogError($"結果画面：画像読み込み失敗(name = {participant.animalData.animalName}) : {error}"); }
        ));
    }

    private void ApplyRankColor(int rank)
    {
        Color color;
        switch (rank)
        {
            case 1: color = goldColor; break;
            case 2: color = silverColor; break;
            case 3: color = bronzeColor; break;
            default: return; // 4位以下はプレハブの色のまま
        }

        rankText.color = color;
        if (plateImage != null)
        {
            // 文字が埋もれないよう暗めにした色をプレートに付ける(透明度は元のまま)
            plateImage.color = new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, plateImage.color.a);
        }
    }

    private string FormatRank(int rank)
    {
        switch (rank)
        {
            case 1:
                return "1st";
            case 2:
                return "2nd";
            case 3:
                return "3rd";
            default:
                return rank + "th";
        }
    }
}
