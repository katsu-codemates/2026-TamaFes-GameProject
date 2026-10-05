using System;
using System.Collections.Generic;

/// <summary>
/// レース1回分の中での、動物1頭の結果。
/// </summary>
[Serializable]
public class RaceEntryResult
{
    public string animalId;   // AnimalData.AnimalId
    public string animalName; // 記録時点の名前（表示用）
    public int rank;          // 着順（1始まり）
}

/// <summary>
/// レース1回分の記録。
/// </summary>
[Serializable]
public class RaceRecord
{
    public string finishedAt; // レース終了日時（ISO 8601形式）
    public int racerCount;    // 出走頭数
    public List<RaceEntryResult> results = new List<RaceEntryResult>(); // 着順に並んだ結果
}

/// <summary>
/// 保存ファイルに書き出すレース履歴全体。古いレースから順に並ぶ。
/// </summary>
[Serializable]
public class RaceHistoryData
{
    public List<RaceRecord> races = new List<RaceRecord>();
}
