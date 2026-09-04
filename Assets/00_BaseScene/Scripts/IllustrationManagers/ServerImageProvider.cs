using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 本番サーバーの画像一覧APIを叩いて、イラストの一覧を取得するクラス
/// </summary>
public class ServerImageProvider : IImageProvider
{
    private readonly string endpointUrl;
    private readonly bool onlyApproved;

    public ServerImageProvider(string endpointUrl, bool onlyApproved = true)
    {
        this.endpointUrl = endpointUrl;
        this.onlyApproved = onlyApproved;
    }

    public IEnumerator FetchImageList(Action<ImageData[]> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(endpointUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"画像一覧の取得に失敗: {request.error}");
                yield break;
            }

            string jsonResponse = request.downloadHandler.text;

            ImageData[] imageList;
            try
            {
                // JSONをパースしてImageDataの配列に変換する
                imageList = JsonArrayUtility.FromJsonArray<ImageData>(jsonResponse);

                foreach (var img in imageList)
                {
                    if (!string.IsNullOrEmpty(img.createdAt))
                    {
                        img.createdAt = img.createdAt
                            .Replace(":", "-")
                            .Replace(".", "-");
                    }

                }
            }
            catch (Exception e)
            {
                onError?.Invoke($"JSONのパースに失敗: {e.Message}");
                yield break;
            }

            if (onlyApproved)
            {
                imageList = imageList.Where(image => image.status == "approved").ToArray();
            }

            onSuccess?.Invoke(imageList);
        }
    }
}
