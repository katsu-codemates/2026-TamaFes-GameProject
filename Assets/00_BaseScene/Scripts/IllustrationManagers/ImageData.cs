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
    public float speed; // 動物のスピード
    public float power; // 動物のパワー
    public float intelligence; // 動物の知恵
    public float fortune; // 動物の運
    public float stamina; // 動物のスタミナ
}
