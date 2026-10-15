using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


/// <summary>
/// 画像一覧の取得→ダウンロード→画面への配置をまとめて行う司令塔。
/// imageProviderを差し替えることでテスト仕様と本番仕様を切り替えられる。
/// </summary>
public class IllustrationManager : MonoBehaviour
{
    [SerializeField] private AnimalSelectionManager selectionManager;
    [SerializeField] private AnimalActionScheduler actionScheduler;

    [Header("表示に使うプレハブ")]
    [SerializeField] private GameObject illustrationPrefab; 

    [Header("表示に使う親オブジェクト")]
    [SerializeField] private Transform container;

    [Header("画像読み込み間隔（秒）")]
    [SerializeField] private float loadingInterval = 10f;

    [Header("開発中かどうか")]
    [SerializeField] private bool isDevelopment = true;

    [Header("【本番仕様】サーバーの画像一覧APIのURL")]
    [SerializeField] private string remoteEndpointUrl = "https://***/api/illustrations"; // 本番環境のAPI URLを指定

    [Header("【テスト仕様】ローカルの画像名一覧(pngファイル)")]
    [SerializeField] private string[] testImageNames; // StreamingAssets/TestImages/ に置いた画像

    [Header("表示座標の最大絶対値")]
    [SerializeField] private float displayPositionSqu = 5f; // 一片の長さがこの値の二倍である正方形の領域に表示する。

    [Header("動物の表示上限(オプションで変更でき、PlayerPrefsに保存される)")]
    [SerializeField] private int defaultMaxDisplayCount = 30;
    [SerializeField] private int minMaxDisplayCount = 1;
    [SerializeField] private int maxMaxDisplayCount = 100;

    [Header("表示上限を変えてから一覧を取得し直すまでの待ち時間（秒）")]
    [Tooltip("スライダーを動かしている間に何度も通信しないようにするため")]
    [SerializeField] private float limitChangeReloadDelay = 1f;

    // 表示上限を保存するPlayerPrefsのキー
    private const string MaxDisplayCountKey = "MaxDisplayAnimalCount";

    // imageNameと表示中のGameObjectの対応を保持する辞書。生成済みの画像を管理し、二重生成の防止や後で消すのに使う。
    private readonly Dictionary<string, GameObject> displayedIllustrations = new Dictionary<string, GameObject>();

    // 登録済みの動物を保持するリスト。必要に応じて、ここからランダムに出走メンバーを抽選するなどの処理に使える。
    private readonly List<AnimalData> registeredAnimals = new List<AnimalData>();

    private IImageProvider imageProvider;
    private Coroutine repeatCoroutine;
    private int maxDisplayCount;

    public int MaxDisplayCount => maxDisplayCount;
    public int MinMaxDisplayCount => minMaxDisplayCount;
    public int MaxMaxDisplayCount => maxMaxDisplayCount;

    private void Awake()
    {
        maxDisplayCount = ClampMaxDisplayCount(PlayerPrefs.GetInt(MaxDisplayCountKey, defaultMaxDisplayCount));
    }

    private void Start()
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

        // 画像一覧を取得して表示するのを繰り返す
        repeatCoroutine = StartCoroutine(RepeatDisplay());
    }

    private IEnumerator RepeatDisplay(float initialDelay = 0f)
    {
        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        var wait = new WaitForSeconds(loadingInterval);
        // シーン開始（または再読み込み）直後の1回目は、まとめて並べるだけで登場演出は出さない
        bool isInitialLoad = true;
        while(true)
        {
            yield return LoadAnyDisplayAll(isInitialLoad);
            isInitialLoad = false;
            Debug.Log("表示中の動物の数:" + displayedIllustrations.Count);
            
            yield return wait;
            Debug.Log("新規読み込み開始");
        }
    }

    private IEnumerator LoadAnyDisplayAll(bool isInitialLoad)
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

        // 作成日時が新しい順に表示上限の数だけ選ぶ。createdAtはISO形式なので文字列の比較で日時順になる
        var targets = new List<ImageData>();
        foreach (var imageData in imageList)
        {
            if (string.IsNullOrEmpty(imageData.createdAt))
            {
                Debug.LogWarning($"createdAtが空の画像はスキップします: {imageData.title}");
                continue;
            }
            targets.Add(imageData);
        }
        targets.Sort((a, b) => string.CompareOrdinal(b.createdAt, a.createdAt));
        if (targets.Count > maxDisplayCount)
        {
            targets.RemoveRange(maxDisplayCount, targets.Count - maxDisplayCount);
        }

        // 表示対象から外れた動物(新しい動物に押し出された古い動物など)を消す
        var targetIds = new HashSet<string>();
        foreach (var imageData in targets)
        {
            targetIds.Add(imageData.createdAt);
        }
        foreach (var imageId in new List<string>(displayedIllustrations.Keys))
        {
            if (!targetIds.Contains(imageId))
            {
                RemoveIllustration(imageId);
            }
        }

        foreach (var imageData in targets)
        {
            // 既に表示済みならスキップ
            if (displayedIllustrations.ContainsKey(imageData.createdAt))
            {
                continue;
            }

            // 新しい画像を表示する
            yield return DisplayImage(imageData, isInitialLoad);
        }
    }

    /// <summary>
    /// 表示中の動物を1体消す。レースの出走候補からも外す。
    /// </summary>
    private void RemoveIllustration(string imageId)
    {
        if (!displayedIllustrations.TryGetValue(imageId, out var go))
        {
            return;
        }

        if (go != null)
        {
            var holder = go.GetComponent<AnimalDataHolder>();

            // 選択中の動物が破棄されるとカメラのフォーカスが外れなくなるため、先に選択を解除する
            if (selectionManager != null && holder != null && selectionManager.SelectedAnimal == holder)
            {
                selectionManager.Unselect();
            }
            if (holder != null && holder.Data != null)
            {
                registeredAnimals.Remove(holder.Data);
            }

            // AnimalIdleAnimation.OnDestroyでTweenの停止・スケジューラからの登録解除・餌場や寝床の予約解除が行われる
            Destroy(go);
        }

        displayedIllustrations.Remove(imageId);
    }

    /// <summary>
    /// 表示上限を古い動物から削って守る。通信せずにすぐ反映できる。
    /// </summary>
    private void TrimToMaxDisplayCount()
    {
        if (displayedIllustrations.Count <= maxDisplayCount) return;

        var imageIds = new List<string>(displayedIllustrations.Keys);
        imageIds.Sort(string.CompareOrdinal); // 古い順
        int removeCount = imageIds.Count - maxDisplayCount;
        for (int i = 0; i < removeCount; i++)
        {
            RemoveIllustration(imageIds[i]);
        }
    }

    private int ClampMaxDisplayCount(int count)
    {
        return Mathf.Clamp(count, minMaxDisplayCount, maxMaxDisplayCount);
    }

    /// <summary>
    /// 表示上限を変更して保存する。減らした場合は古い動物からすぐに消し、
    /// 増やした場合は少し待ってから一覧を取得し直して足りない分を表示する。
    /// </summary>
    public void SetMaxDisplayCount(int count)
    {
        count = ClampMaxDisplayCount(count);
        if (count == maxDisplayCount) return;

        maxDisplayCount = count;
        PlayerPrefs.SetInt(MaxDisplayCountKey, maxDisplayCount);
        PlayerPrefs.Save();

        TrimToMaxDisplayCount();

        // ボタンの連打・長押し中に何度も通信しないよう、取得のやり直しは待ち時間を置いてから行う
        if (repeatCoroutine != null)
        {
            StopCoroutine(repeatCoroutine);
        }
        repeatCoroutine = StartCoroutine(RepeatDisplay(limitChangeReloadDelay));
    }

    /// <summary>
    /// 保存した表示上限を削除して初期値に戻す(キャッシュクリア用)。
    /// </summary>
    public void ResetMaxDisplayCountToDefault()
    {
        PlayerPrefs.DeleteKey(MaxDisplayCountKey);
        PlayerPrefs.Save();
        SetMaxDisplayCount(defaultMaxDisplayCount);
    }

    private IEnumerator DisplayImage(ImageData imageData, bool isInitialLoad)    {
        Sprite sprite = null;
        string errorMessage = null;

        // ImageLoaderが読み込み時にキャッシュを保存するので、読み込む前に判定する。
        // キャッシュが無い＝この端末で初めて読み込まれた動物（アプリを再起動しても初回扱いにはならない）
        bool isFirstLoad = !ImageCache.ExistsCacheFile(imageData.createdAt);

        // ImageLoaderを使ってImageDataからSpriteを取得する
        yield return ImageLoader.LoadSpriteFromBase64(
            imageData.createdAt, // キャッシュ用の一意なIDとしてcreatedAtを使用
            imageData.image, // データURI形式のbase64文字列
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
        var clickDetector = go.GetComponent<AnimalsClickDetector>();
        if (clickDetector != null)
        {
            clickDetector.Initialize(selectionManager);
        }
        var animation = go.GetComponent<AnimalIdleAnimation>();
        if (animation != null)
        {
            animation.Initialize(actionScheduler);

            // 新しく追加された動物だと分かるように登場演出を付ける
            if (isFirstLoad && !isInitialLoad)
            {
                animation.RequestEntrance();
            }
        }

        go.transform.localPosition = new Vector3(
            Random.Range(-displayPositionSqu, displayPositionSqu), // X座標をランダムに設定
            4f, 
            Random.Range(-displayPositionSqu, displayPositionSqu)  // Z座標をランダムに設定
        );

        // 表示中のイラストを辞書に追加する
        displayedIllustrations[imageData.createdAt] = go;
    }

    private AnimalData SetAnimalData(GameObject animalPrefab, ImageData data)
    {
        var holder = animalPrefab.GetComponent<AnimalDataHolder>();

        var animalData = holder.Data ?? new AnimalData();

        // 画像データから動物データを設定
        // int cutName = data.title.Length - 4;
        // animalData.animalName = data.title.Substring(0, cutName); // ".png"を引く
        animalData.animalName = data.title;
        animalData.creatorName = data.creatorName;
        animalData.imageBase64 = data.image;
        animalData.createdAt = data.createdAt;
        animalData.speed = data.speed;
        animalData.power = data.power;
        animalData.wisdom = data.intelligence;
        animalData.luck = data.fortune;
        animalData.stamina = data.stamina;
        // 他のデータも設定する...
            // // 仮実装ーーーーー
            //     float[] x = DebugSetParam();
            //     animalData.speed = x[0];
            //     animalData.power = x[1];
            //     animalData.wisdom = x[2];
            //     animalData.luck = x[3];
            //     animalData.stamina = x[4];

        holder.Data = animalData;

        if (!registeredAnimals.Contains(animalData))
        {
            registeredAnimals.Add(animalData);
        }

        return animalData;
    }

    public List<AnimalData> GetRegisteredAnimals()
    {
        return registeredAnimals;
    }

    /// <summary>
    /// 画面に表示中の動物を返す。検索などで動物のGameObjectが必要なときに使う。
    /// </summary>
    public List<AnimalDataHolder> GetDisplayedAnimals()
    {
        var animals = new List<AnimalDataHolder>();
        foreach (var go in displayedIllustrations.Values)
        {
            if (go == null) continue;

            var holder = go.GetComponent<AnimalDataHolder>();
            if (holder != null && holder.Data != null)
            {
                animals.Add(holder);
            }
        }
        return animals;
    }

    float[] DebugSetParam()
    {
        float[] values = new float[5];
        float sum = 0f;

        // Step1: ランダム値を作る
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = UnityEngine.Random.value; // 0〜1 のランダム
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

    public void MakeTransparentAllImages(string exceptionImageId)
    {
        foreach (var kvp in displayedIllustrations)
        {
            string imageId = kvp.Key;
            GameObject go = kvp.Value;

            if (imageId != exceptionImageId)
            {
                var renderer = go.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    Color color = renderer.color;
                    color.a = 0.5f; // 半透明にする
                    renderer.color = color;
                }
            }
        }
    }

    public void ResetTransparencyAllImages()
    {
        foreach (var kvp in displayedIllustrations)
        {
            GameObject go = kvp.Value;
            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = 1f; // 元の不透明度に戻す
                renderer.color = color;
            }
        }
    }

    /// <summary>
    /// 表示中の動物をすべて破棄し、画像一覧を取得し直して並べ直す。
    /// </summary>
    public void ReloadAllIllustrations()
    {
        // 選択中の動物が破棄されるとカメラのフォーカスが外れなくなるため、先に選択を解除する
        if (selectionManager != null)
        {
            selectionManager.Unselect();
        }

        if (repeatCoroutine != null)
        {
            StopCoroutine(repeatCoroutine);
            repeatCoroutine = null;
        }

        // AnimalIdleAnimation.OnDestroyでTweenの停止とスケジューラからの登録解除が行われる
        foreach (var go in displayedIllustrations.Values)
        {
            if (go != null)
            {
                Destroy(go);
            }
        }
        displayedIllustrations.Clear();
        registeredAnimals.Clear();

        // 食事中・睡眠中に破棄された動物の餌場・寝床の予約を解除する
        if (actionScheduler != null)
        {
            actionScheduler.ReleaseAllSpots();
        }

        Debug.Log("動物画像を読み込み直します");
        repeatCoroutine = StartCoroutine(RepeatDisplay());
    }

    private void OnDestroy()
    {
        if (repeatCoroutine != null)
        {
            StopCoroutine(repeatCoroutine);
        }
    }
}