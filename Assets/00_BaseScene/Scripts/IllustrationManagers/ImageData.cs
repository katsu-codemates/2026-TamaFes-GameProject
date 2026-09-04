using System;

/// <summary>
/// 一枚のイラストに対応するデータを保持するクラス
/// </summary>
[Serializable]
public class ImageData
{
    public string title; // イラストの名前
    public string createdAt; // 作成日時
    public string creatorName; // 作者名
    public string status; // "approved" / "pending" / "rejected"
    public string image; // データURI形式のbase64文字列
}
