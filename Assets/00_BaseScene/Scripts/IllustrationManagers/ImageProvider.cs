using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 「画像一覧をどこからとってくるのか」を抽象化するインターフェイス。
/// テストのときとブラウザから画像を取得するときの扱いを同じにする。
/// </summary>
public interface IImageProvider
{
    /// <summary>
    /// 画像一覧を取得する。成功したらonSuccessに画像リストを渡す。失敗したらonErrorを呼び出す。
    /// </summary>
    /// <param name="onSuccess">取得成功時のコールバック</param>
    /// <param name="onError">取得失敗時のコールバック</param>
    IEnumerator FetchImageList(Action<ImageData[]> onSuccess, Action<string> onError);
}

/// <summary>
/// 【テスト実装】StreamingAssetsから画像一覧を取得するクラス
/// サーバーがない段階でも、表示の動作確認ができる。
/// 使い方：StreamingAssets/TestImages/にpngファイルを何枚か置いておく。
/// </summary>
public class TestImageProvider : IImageProvider
{
    private readonly string[] filenames;

    public TestImageProvider(string[] filenames)
    {
        this.filenames = filenames;
    }

    public IEnumerator FetchImageList(Action<ImageData[]> onSuccess, Action<string> onError)
    {
        /// テストなので通信は発生しない。
        /// StreamingAssetsに置かれた画像ファイルのパスを入れる。
        var imageDataList = new ImageData[filenames.Length];
        for (int i = 0; i < filenames.Length; i++)
        {
            imageDataList[i] = new ImageData
            {
                title = filenames[i],
                image = System.IO.Path.Combine(Application.streamingAssetsPath, "TestImages", filenames[i])
            };
        }

        yield return null; // コルーチンなので1フレーム待つ
        onSuccess?.Invoke(imageDataList);
    }
}

// /// <summary>
// /// 【本番実装】サーバーから画像一覧を取得するクラス
// /// バックエンドが完成したら、serverUrlを差し替える。
// /// </summary>
// public class ServerImageProvider : IImageProvider
// {
//     private readonly string serverUrl;

//     public ServerImageProvider(string serverUrl)
//     {
//         this.serverUrl = serverUrl;
//     }

//     public IEnumerator FetchImageList(Action<ImageData[]> onSuccess, Action<string> onError)
//     {
//         using (UnityWebRequest req = UnityWebRequest.Get(serverUrl))
//         {
//             yield return req.SendWebRequest(); //非同期処理でリクエストを送信し、完了するまで待機

//             if (req.result != UnityWebRequest.Result.Success) // エラーが発生した場合
//             {
//                 onError?.Invoke(req.error);
//                 yield break;
//             }

//             ImageListResponse response = null;
//             try
//             {
//                 response = JsonUtility.FromJson<ImageListResponse>(req.downloadHandler.text);
//             }
//             catch (Exception e)
//             {
//                 onError?.Invoke($"JSONのパースに失敗しました: {e.Message}");
//                 yield break;
//             }

//             onSuccess?.Invoke(response?.images ?? Array.Empty<ImageData>()); // 成功したら画像リストを返す。nullの場合は空配列を返す。
//         }
//     }
// }