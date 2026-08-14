using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 画像一覧の取得→ダウンロード→画面への配置をまとめて行う司令塔。
/// imageProviderを差し替えることでテスト仕様と本番仕様を切り替えられる。
/// </summary>
public class IllustrationManager : MonoBehaviour
{
    [Header("表示に使うプレハブ")]
    [SerializeField] private GameObject illustrationPrefab; 

    [Header("表示に使う親オブジェクト")]
    [SerializeField] private Transform container;

    [Header("開発中かどうか")]
    [SerializeField] private bool isDevelopment = true;

    [Header("【本番仕様】サーバーの画像一覧APIのURL")]
    [SerializeField] private string remoteEndpointUrl = "https://***/api/illustrations"; // 本番環境のAPI URLを指定

    [Header("【テスト仕様】ローカルの画像名一覧(pngファイル)")]
    [SerializeField] private string[] testImageNames; // StreamingAssets/TestImages/ に置いた画像

    // imageNameと表示中のGameObjectの対応を保持する辞書。生成済みの画像を管理し、二重生成の防止や後で消すのに使う。
    private readonly Dictionary<string, GameObject> displayedIllustrations = new Dictionary<string, GameObject>();

    // 登録済みの動物を保持するリスト。必要に応じて、ここからランダムに出走メンバーを抽選するなどの処理に使える。
    private readonly List<AnimalData> registeredAnimals = new List<AnimalData>();

    private IImageProvider imageProvider;

    private IEnumerator Start()
    {
        // 開発中かどうかに応じて、使用するImageProviderを切り替える
        if (isDevelopment)
        {
            imageProvider = new TestImageProvider(testImageNames);
        }
        else
        {
            imageProvider = new ServerImageProvider(remoteEndpointUrl);
        }

        // 画像一覧を取得して表示する
        yield return LoadAnyDisplayAll();
        Debug.Log("表示中の動物の数:" + displayedIllustrations.Count);
    }

    private IEnumerator LoadAnyDisplayAll()
    {
        ImageData[] imageList = null;
        string errorMessage = null;

        yield return imageProvider.FetchImageList(
            onSuccess: (images) => imageList = images,
            onError: (error) => errorMessage = error
        );

        if (errorMessage != null)
        {
            Debug.LogError($"画像一覧の取得に失敗しました: {errorMessage}");
            yield break;
        }

        foreach (var imageData in imageList)
        {
            // 既に表示済みならスキップ
            if (displayedIllustrations.ContainsKey(imageData.imageName))
            {
                continue;
            }

            // 新しい画像を表示する
            yield return DisplayImage(imageData);
        }
    }

    private IEnumerator DisplayImage(ImageData imageData)
    {
        Sprite sprite = null;
        string errorMessage = null;

        // ImageLoaderを使ってImageDataからSpriteを取得する
        yield return ImageLoader.LoadSprite(
            imageData.imageName,
            imageData.imageUrl,
            onSuccess: (loadedSprite) => sprite = loadedSprite,
            onError: (error) => errorMessage = error
        );

        if (errorMessage != null)
        {
            Debug.LogError($"画像の取得に失敗しました: {errorMessage}");
            yield break;
        }

        // 取得したSpriteを使って動物のプレハブを生成し、表示する
        GameObject go = Instantiate(illustrationPrefab, container);
        var animalData = SetAnimalData(go, imageData);
        var renderer = go.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = sprite;
        }
        else
        {
            Debug.LogError("SpriteRendererが見つかりませんでした。プレハブにSpriteRendererコンポーネントがアタッチされているか確認してください。");
        }

        go.transform.localPosition = new Vector3(
            Random.Range(-5f, 5f), // X座標をランダムに設定
            4f, 
            Random.Range(-5f, 5f)  // Z座標をランダムに設定
        );

        // 表示中のイラストを辞書に追加する
        displayedIllustrations[imageData.imageName] = go;
    }

    private AnimalData SetAnimalData(GameObject animalPrefab, ImageData animalImageData)
    {
        var animalData = animalPrefab.GetComponent<AnimalData>();
        if (animalData == null)
        {
            animalData = animalPrefab.AddComponent<AnimalData>();
        }

        // 画像データから動物データを設定
        animalData.animalName = animalImageData.imageName;
        animalData.imageUrl = animalImageData.imageUrl;
        // 他のデータも設定する...
            // 仮実装ーーーーー
                float[] x = DebugSetParam();
                animalData.speed = x[0];
                animalData.power = x[1];
                animalData.wisdom = x[2];
                animalData.luck = x[3];
                animalData.stamina = x[4];

        if (!registeredAnimals.Contains(animalData))
        {
            registeredAnimals.Add(animalData);
        }

        return animalData;
    }

    public List<AnimalData> GetResisterdAnimals()
    {
        return registeredAnimals;
    }

    float[] DebugSetParam()
    {
        float[] values = new float[5];
        float sum = 0f;

        // Step1: ランダム値を作る
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = Random.value; // 0〜1 のランダム
            sum += values[i];
        }

        // Step2: 合計が 250 になるように正規化
        float targetTotal = 250f;
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = values[i] / sum * targetTotal;
        }

        return values;
    }
}