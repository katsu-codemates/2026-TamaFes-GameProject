using UnityEngine;

/// <summary>
/// レースの進行度(0~1)とレーン番号からワールド座標を計算するクラス。
/// </summary>
public static class RaceTrack
{
    public static readonly Vector3 StartPosition = new Vector3(-5f, 0f, 0f);
    public static readonly Vector3 ForwardDirection = new Vector3(1f, 0f, 0f).normalized;
    public static readonly Vector3 LaneDirection = new Vector3(0f, 0f, 1f).normalized;

    public const float TrackLength = 10000f; // ゴールまでの距離
    public const float LaneWidth = 2f; // レーンの幅

    /// <summary>  
    /// progress(0~1)とlaneIndex(0~)から、レース上のワールド座標を計算する。
    /// </summary>
    public static Vector3 GetWorldPosition(float progress, int laneIndex, int totalLanes)
    {
        float disstanceAlongTrack = Mathf.Clamp01(progress) * TrackLength;
        float laneOffset = (laneIndex - (totalLanes - 1) / 2f) * LaneWidth; // レーンの中心を基準にオフセットを計算

        return StartPosition
            + ForwardDirection * disstanceAlongTrack
            + LaneDirection * laneOffset;
    }
}
