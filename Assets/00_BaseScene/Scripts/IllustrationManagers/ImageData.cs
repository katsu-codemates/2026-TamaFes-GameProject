using System;

/// <summary>
/// 一枚のイラストに対応するデータを保持するクラス
/// </summary>
[Serializable]
public class ImageData
{
    public string imageName; // イラストの名前
    public string imageUrl;  // イラストのURL
}

/// <summary>
/// 画像一覧APIのレスポンス全体を受けるためのクラス。
/// JsonUtilityというシステムを使うが、JsonUtilityは配列を直接扱えないため、配列を包むクラスを作る必要がある。
/// </summary>
[Serializable]
public class ImageListResponse
{
    public ImageData[] images; // 画像データの配列
}
