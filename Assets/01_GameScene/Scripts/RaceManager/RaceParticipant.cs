using UnityEngine;

/// <summary>
/// レース中における一等の動的な状態。
/// AnimalDataとは別に、進行度や現在速度など
/// レース中に変化する値を保持するためのクラス。
/// </summary>

public class RaceParticipant
{
    public AnimalData animalData;
    public int laneIndex;   // 0から始まるレーン番号
    public float progress;  // 0.0fから1.0fまでの範囲で、ゴールまでの進行度を表す
    public float currentSpeed;
    public int finishRank = -1; // ゴールした順位。ゴールしていない場合は-1

    /// <summary>
    /// 速度を再計算するメソッド。呼ばれるたびに乱数で速度が変化する。
    /// </summary>
    public void UpdateSpeed()
    {
        // luckが高いほど速度の振れ幅が大きい
        float fluctuation = Random.Range(-1f, 1f) * animalData.luck;

        // powerが低いほど、終盤(progressが高いほど)失速しやすくする
        float staminaPenalty = progress > 0.7f ? (1f - Mathf.Clamp01(animalData.power)) * (progress - 0.7f) * 3f : 0f;

        currentSpeed = Mathf.Max(1f, animalData.speed + fluctuation - staminaPenalty);
    }
}
