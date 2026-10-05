using System.Text;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject animalInfoParent;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private TextMeshProUGUI widomText;
    [SerializeField] private TextMeshProUGUI luckText;
    [SerializeField] private TextMeshProUGUI staminaText;

    [Header("レース成績")]
    [SerializeField] private TextMeshProUGUI raceHistoryText;
    [SerializeField] private int recentResultCount = 5; // 直近何件まで表示するか

    void Awake()
    {
        HideAnimalInfo();
    }

    public void ShowAnimalInfo(AnimalData data)
    {
        animalInfoParent.SetActive(true);

        nameText.text = data.animalName;
        speedText.text = "はやさ:" + data.speed.ToString();
        powerText.text = "ちから:" + data.power.ToString();
        widomText.text = "かしこさ:" + data.wisdom.ToString();
        luckText.text = "うんのよさ:" + data.luck.ToString();
        staminaText.text = "スタミナ:" + data.stamina.ToString();

        if (raceHistoryText != null)
        {
            raceHistoryText.text = BuildRaceHistoryText(data);
        }
    }

    /// <summary>
    /// 例: 「出走:3回 1着:1回\n直近:1着(5頭) 3着(5頭) 2着(5頭)」
    /// </summary>
    private string BuildRaceHistoryText(AnimalData data)
    {
        string id = data.AnimalId;
        int raceCount = RaceHistoryStore.GetRaceCount(id);
        if (raceCount == 0)
        {
            return "まだレースに出ていません";
        }

        var sb = new StringBuilder();
        sb.Append($"出走:{raceCount}回 1着:{RaceHistoryStore.GetWinCount(id)}回");
        sb.Append("\n直近:");
        foreach (var (race, result) in RaceHistoryStore.GetRecentResults(id, recentResultCount))
        {
            sb.Append($" {result.rank}着({race.racerCount}頭)");
        }
        return sb.ToString();
    }

    public void HideAnimalInfo()
    {
        animalInfoParent.SetActive(false);
    }

}
