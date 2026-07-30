using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// imageUrl（httpでもローカルパスでも）からTexture2Dを取得し、Spriteに変換するクラス
/// imageNameを渡すことで、キャッシュがあればそれを使い、なければ新たにDLしてキャッシュに保存する
/// </summary>
public static class ImageLoader
{
    public static IEnumerator LoadSprite(string imageName, string imageUrl, Action<Sprite> onSuccess, Action<string> onError)
    {
        // まずローカルキャッシュを確認する。あれば通信せずにディスクから読み込む。
        if (ImageCache.ExistsCacheFile(imageName))
        {
            Texture2D cachedTexture = ImageCache.LoadTexture(imageName);
            onSuccess?.Invoke(CreateSprite(cachedTexture));
            yield break;
        }

        // キャッシュがなければimageUrlからダウンロードする
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(imageUrl)) //usingを使うことで、通信終了後に自動で破棄してくれる
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(req.error);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(req);

            // ダウンロードしたTexture2Dをキャッシュに保存する
            byte[] pngBytes = texture.EncodeToPNG();
            ImageCache.SaveCacheFile(imageName, pngBytes);

            onSuccess?.Invoke(CreateSprite(texture));
        }
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
