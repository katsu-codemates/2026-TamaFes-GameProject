using UnityEngine;

/// <summary>
/// レースの出走数をPlayerPrefsに保存・読み込みするクラス。
/// BaseSceneのオプション画面で変更し、GameSceneのRaceManagerが出走抽選のときに読む。
/// 表示中の動物が出走数より少ない場合は、表示中の動物が全員出走する。
/// </summary>
public static class RacerCountSetting
{
    public const int Min = 2;
    public const int Max = 50;
    public const int Default = 20;

    // 出走数を保存するPlayerPrefsのキー
    private const string RacerCountKey = "RacerCount";

    /// <summary>保存されている出走数(未保存なら初期値)。</summary>
    public static int Get()
    {
        return Clamp(PlayerPrefs.GetInt(RacerCountKey, Default));
    }

    /// <summary>出走数を範囲内に収めて保存する。</summary>
    public static void Set(int count)
    {
        PlayerPrefs.SetInt(RacerCountKey, Clamp(count));
        PlayerPrefs.Save();
    }

    /// <summary>保存した出走数を削除して初期値に戻す(キャッシュクリア用)。</summary>
    public static void ResetToDefault()
    {
        PlayerPrefs.DeleteKey(RacerCountKey);
        PlayerPrefs.Save();
    }

    private static int Clamp(int count)
    {
        return Mathf.Clamp(count, Min, Max);
    }
}
