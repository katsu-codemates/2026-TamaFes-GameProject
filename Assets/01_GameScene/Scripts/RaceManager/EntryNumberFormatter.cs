using TMPro;

/// <summary>
/// 出場番号の表記をまとめたクラス。出場者表示画面・実況・結果画面で表記を揃えるために使う。
/// </summary>
public static class EntryNumberFormatter
{
    // 例: 「3番」
    public static string Format(RaceParticipant participant)
        => $"{participant.EntryNumber}番";

    /// <summary>
    /// 出場番号と名前をテキストに反映する。
    /// 番号専用のテキストがあればそちらに番号を出し、なければ名前の前に「3番 ポチ」の形で付ける。
    /// </summary>
    public static void Apply(RaceParticipant participant, TextMeshProUGUI nameText, TextMeshProUGUI entryNumberText)
    {
        string number = Format(participant);
        string name = participant.animalData.animalName;

        if (entryNumberText != null)
        {
            entryNumberText.text = number;
            nameText.text = name;
        }
        else
        {
            nameText.text = $"{number} {name}";
        }
    }
}
