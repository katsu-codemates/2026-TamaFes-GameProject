using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// imageUrl（httpでもローカルパスでも）からTexture2Dを取得し、Spriteに変換するクラス
/// imageNameを渡すことで、キャッシュがあればそれを使い、なければ新たにDLしてキャッシュに保存する
/// </summary>
public static class ImageLoader
{
    public static IEnumerator LoadSpriteFromBase64(string id, string dataUriOrBase64, Action<Sprite> onSuccess, Action<string> onError)
    {
        // まずローカルキャッシュを確認する。あれば通信せずにディスクから読み込む。
        if (ImageCache.ExistsCacheFile(id))
        {
            Texture2D cachedTexture = ImageCache.LoadTexture(id);
            onSuccess?.Invoke(CreateSprite(cachedTexture));
            yield break;
        }

        string base64 = ExtractBase64(dataUriOrBase64);

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64);
        }
        catch (FormatException e)
        {
            onError?.Invoke($"base64のデコードに失敗:{e.Message}, base64={base64}");
            yield break;
        }

        Texture2D texture = new Texture2D(2, 2); // サイズはLoadImageが調整
        bool loaded = texture.LoadImage(bytes);

        if (!loaded)
        {
            onError?.Invoke("画像データの読み込みに失敗（不正なバイト列の可能性があります）");
            yield break;
        }

        // キャッシュ機能
        ImageCache.SaveCacheFile(id, bytes);

        onSuccess?.Invoke(CreateSprite(texture));
    }

    /// <summary>
    /// "data:image/png;base64,..."のような形式の文字列から、base64部分だけを抽出する
    /// </summary>
    private static string ExtractBase64(string dataUriOrBase64)
    {
        const string prefix = "base64";
        int index = dataUriOrBase64.IndexOf(prefix, StringComparison.Ordinal);
        if (index < 0)
        {
            // "data:...;base64"というプレフィクス自体がない場合、素のbase64文字列とみなす
            return dataUriOrBase64;
        }
        
        int startIndex = index + prefix.Length;

        // カンマがあれば読み飛ばす。なければそのまま
        if (startIndex < dataUriOrBase64.Length && dataUriOrBase64[startIndex] == ',')
        {
            startIndex++;
        }

        return dataUriOrBase64.Substring(startIndex);
    }

    private static Sprite CreateSprite(Texture2D texture)
    {
        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
    }
}
