using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RaceResultRowView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image iconImage;

    public void Setup(RaceParticipant participant)
    {
        rankText.text = FormatRank(participant.finishRank);
        nameText.text = participant.animalData.animalName;

        StartCoroutine(ImageLoader.LoadSpriteFromBase64(
            participant.animalData.createdAt,
            participant.animalData.imageBase64,
            onSuccess: (sprite) => { iconImage.sprite = sprite; },
            onError: (error) => { Debug.LogError($"結果画面：画像読み込み失敗(name = {participant.animalData.animalName}) : {error}"); }
        ));
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
