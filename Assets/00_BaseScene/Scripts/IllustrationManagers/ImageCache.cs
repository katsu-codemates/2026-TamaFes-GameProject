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
    /// 指定されたimageNameのキャッシュファイルのパスを返す
    /// </summary>
    public static string GetCacheFilePath(string imageName)
    {
        return Path.Combine(CacheDirectory, imageName + ".png");
    }

    /// <summary>
    /// 指定されたimageNameのキャッシュファイルが存在するかどうかを返す
    /// </summary>
    public static bool ExistsCacheFile(string imageName)
    {
        string cacheFilePath = GetCacheFilePath(imageName);
        return File.Exists(cacheFilePath);
    }

    /// <summary>
    /// ダウンロードしたバイト列をキャッシュとして保存する
    /// </summary>
    public static void SaveCacheFile(string imageName, byte[] pngBytes)
    {
        // キャッシュ用のディレクトリが存在しない場合は作成する
        if (!Directory.Exists(CacheDirectory))
        {
            Directory.CreateDirectory(CacheDirectory);
        }

        string cacheFilePath = GetCacheFilePath(imageName);
        File.WriteAllBytes(cacheFilePath, pngBytes);
    }

    /// <summary>
    /// 指定されたimageNameのキャッシュファイルを読み込み、Texture2Dとして返す
    /// </summary>
    public static Texture2D LoadTexture(string imageName)
    {
        string cacheFilePath = GetCacheFilePath(imageName);
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
