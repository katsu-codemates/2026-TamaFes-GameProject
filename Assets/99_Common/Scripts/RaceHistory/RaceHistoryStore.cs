using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// レース結果の履歴をApplication.persistentDataPathにJSONで保存・読み込みするクラス。
/// アプリを再起動しても履歴が残るので、BaseSceneの成績表示やGameSceneの出走抽選に使う。
/// 動物はAnimalData.AnimalIdで識別する（シーンをまたぐとAnimalDataのインスタンスは作り直されるため）。
/// </summary>
public static class RaceHistoryStore
{
    private const string FileName = "RaceHistory.json";

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    // 一度読み込んだ履歴をキャッシュしておく。nullなら未読み込み
    private static RaceHistoryData cache;

    // ドメインリロード無効時でも再生開始ごとにファイルから読み直す
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        cache = null;
    }

    private static RaceHistoryData Data
    {
        get
        {
            if (cache == null) cache = Load();
            return cache;
        }
    }

    /// <summary>
    /// レース結果を1件追加して保存する。finishedOrderは着順に並んでいること。
    /// </summary>
    public static void RecordRace(IReadOnlyList<RaceParticipant> finishedOrder)
    {
        var record = new RaceRecord
        {
            finishedAt = DateTime.Now.ToString("o"),
            racerCount = finishedOrder.Count,
        };

        for (int i = 0; i < finishedOrder.Count; i++)
        {
            AnimalData animal = finishedOrder[i].animalData;
            record.results.Add(new RaceEntryResult
            {
                animalId = animal.AnimalId,
                animalName = animal.animalName,
                rank = i + 1,
            });
        }

        Data.races.Add(record);
        Save();
    }

    /// <summary>
    /// 指定した動物の出走回数を返す。
    /// </summary>
    public static int GetRaceCount(string animalId)
    {
        int count = 0;
        foreach (var race in Data.races)
        {
            if (FindResult(race, animalId) != null) count++;
        }
        return count;
    }

    /// <summary>
    /// 指定した動物の1着回数を返す。
    /// </summary>
    public static int GetWinCount(string animalId)
    {
        int count = 0;
        foreach (var race in Data.races)
        {
            var result = FindResult(race, animalId);
            if (result != null && result.rank == 1) count++;
        }
        return count;
    }

    /// <summary>
    /// 指定した動物の直近の成績を新しい順に最大maxCount件返す。
    /// </summary>
    public static List<(RaceRecord race, RaceEntryResult result)> GetRecentResults(string animalId, int maxCount)
    {
        var list = new List<(RaceRecord, RaceEntryResult)>();
        for (int i = Data.races.Count - 1; i >= 0 && list.Count < maxCount; i--)
        {
            var result = FindResult(Data.races[i], animalId);
            if (result != null) list.Add((Data.races[i], result));
        }
        return list;
    }

    /// <summary>
    /// 指定した動物が直前のレースで1着だったかどうか。BaseSceneで喜ぶ動きをさせる判定に使う。
    /// </summary>
    public static bool IsLastRaceWinner(string animalId)
    {
        if (string.IsNullOrEmpty(animalId) || Data.races.Count == 0) return false;

        var lastRace = Data.races[Data.races.Count - 1];
        return lastRace.results.Count > 0 && lastRace.results[0].animalId == animalId;
    }

    /// <summary>
    /// 履歴をすべて消去する（リハーサル後のリセット用）。
    /// </summary>
    public static void ClearAll()
    {
        cache = new RaceHistoryData();
        Save();
    }

    private static RaceEntryResult FindResult(RaceRecord race, string animalId)
    {
        if (string.IsNullOrEmpty(animalId)) return null;
        foreach (var result in race.results)
        {
            if (result.animalId == animalId) return result;
        }
        return null;
    }

    private static RaceHistoryData Load()
    {
        if (!File.Exists(FilePath)) return new RaceHistoryData();

        try
        {
            string json = File.ReadAllText(FilePath);
            var data = JsonUtility.FromJson<RaceHistoryData>(json) ?? new RaceHistoryData();
            if (data.races == null) data.races = new List<RaceRecord>();
            return data;
        }
        catch (Exception e)
        {
            // 壊れたファイルは次の保存で上書きされないよう退避しておく
            Debug.LogError($"レース履歴の読み込みに失敗しました: {e.Message}");
            try { File.Copy(FilePath, FilePath + ".broken", true); } catch (Exception) { }
            return new RaceHistoryData();
        }
    }

    private static void Save()
    {
        try
        {
            // 書き込み途中で落ちてもファイルが壊れないよう、一時ファイルに書いてから置き換える
            string tempPath = FilePath + ".tmp";
            File.WriteAllText(tempPath, JsonUtility.ToJson(Data, true));
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(tempPath, FilePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"レース履歴の保存に失敗しました: {e.Message}");
        }
    }
}
