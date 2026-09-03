using UnityEngine;
using System.IO;


/// <summary>
/// ダウンロード済みの画像をApplication.persistentDataPathに保存するクラス
/// </summary> 
public static class ImageCache
{
    // persistentDataPath配下にキャッシュ用の専用フォルダを作る
    private const string CacheFolderName = "ImageCache"; // const(定数)で定義しておくと、後で変更する場合に便利。

    private static string CacheDirectory =>
        Path.Combine(Application.persistentDataPath, CacheFolderName);

    /// <summary>
    /// 指定されたidのキャッシュファイルのパスを返す
    /// </summary>
    public static string GetCacheFilePath(string id)
    {
        return Path.Combine(CacheDirectory, id + ".png");
    }

    /// <summary>
    /// 指定されたidのキャッシュファイルが存在するかどうかを返す
    /// </summary>
    public static bool ExistsCacheFile(string id)
    {
        string cacheFilePath = GetCacheFilePath(id);
        return File.Exists(cacheFilePath);
    }

    /// <summary>
    /// ダウンロードしたバイト列をキャッシュとして保存する
    /// </summary>
    public static void SaveCacheFile(string id, byte[] pngBytes)
    {
        // キャッシュ用のディレクトリが存在しない場合は作成する
        if (!Directory.Exists(CacheDirectory))
        {
            Directory.CreateDirectory(CacheDirectory);
        }

        string cacheFilePath = GetCacheFilePath(id);
        File.WriteAllBytes(cacheFilePath, pngBytes);
    }

    /// <summary>
    /// 指定されたidのキャッシュファイルを読み込み、Texture2Dとして返す
    /// </summary>
    public static Texture2D LoadTexture(string id)
    {
        string cacheFilePath = GetCacheFilePath(id);
        if (!File.Exists(cacheFilePath))
        {
            Debug.LogError($"キャッシュファイルが存在しません: {cacheFilePath}");
            return null;
        }

        byte[] bytes = File.ReadAllBytes(cacheFilePath);
        Texture2D texture = new Texture2D(2, 2); // サイズは後で自動的に調整されるので、仮の値を指定
        texture.LoadImage(bytes);
        return texture;
    }
}
