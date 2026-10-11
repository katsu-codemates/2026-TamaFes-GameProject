using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;


/// <summary>
/// レース開始前に、主層メンバーの画像 + 名前を並べて表示する画面。
/// </summary>
public class RaceEntryScreen : MonoBehaviour
{
    [SerializeField] private AnimalRoster animalRoster;
    [SerializeField] private RaceManager raceManager;

    [Header("参加者カード表示")]
    [SerializeField] private GameObject entryCardPrefab;
    [SerializeField] private Transform cardContainer;

    [Header("一画面に収めるレイアウト(cardContainerのGridLayoutGroupを自動調整)")]
    [Tooltip("カードの 横:縦 の比率。この比率を保ったまま、全員が収まる最大サイズに並べる")]
    [SerializeField] private float cardAspectRatio = 1f;
    [Tooltip("カード同士の間隔(セルの短い辺に対する割合)")]
    [SerializeField, Range(0f, 0.3f)] private float cardSpacingRatio = 0.06f;

    [Header("名前用フォント(全文字対応のためダイナミックフォントを実行時に生成)")]
    [Tooltip("名前に使う元フォント(.ttf/.otf)。未設定ならカードのフォントをそのまま使う")]
    [SerializeField] private Font nameSourceFont;
    [Tooltip("元フォントに無い文字を探すOSフォント(見つからないものは無視される)")]
    [SerializeField] private string[] osFallbackFontFamilies = { "Yu Gothic UI", "Meiryo", "Segoe UI Symbol", "Segoe UI", "Arial Unicode MS" };

    [Header("開始方法")]
    [Tooltip("ONならカウントダウンで自動開始、OFFならクリックで開始")]
    [SerializeField] private bool useCountdown = false;
    [SerializeField] private string clickToStartMessage = "クリックしてスタート";

    [Header("カウントダウン")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float countdownSeconds = 30f;
    [Tooltip("カウントダウン時だけ表示するオブジェクト(「レース開始まで…」など)")]
    [SerializeField] private GameObject countdownOnlyObject;

    [Header("カウントダウン終了時に非表示にするオブジェクト")]
    [SerializeField] private GameObject screenRoot;

    // シーンをまたいでも作り直さないようにキャッシュする
    private static TMP_FontAsset cachedNameFont;

    private void Start()
    {
        AudioManager.Instance.PlayBgm(BgmId.Entry);
        raceManager.SelectParticipants(new List<AnimalData>(animalRoster.Animals));
        DisplayEntries(raceManager.Participants);

        if (countdownOnlyObject != null)
        {
            countdownOnlyObject.SetActive(useCountdown);
        }

        if (useCountdown)
        {
            StartCoroutine(CountdownRoutine());
        }
        else
        {
            StartCoroutine(ClickToStartRoutine());
        }
    }

    // 出場番号順(=レーン順)に並べる
    private void DisplayEntries(IReadOnlyList<RaceParticipant> participants)
    {
        TMP_FontAsset nameFont = null;

        foreach (var participant in participants)
        {
            GameObject card = Instantiate(entryCardPrefab, cardContainer);
            RaceEntryCardView view = card.GetComponent<RaceEntryCardView>();

            if (nameFont == null)
            {
                nameFont = GetOrCreateNameFont(view.NameTemplateMaterial);
            }

            view.Setup(participant, nameFont);
        }

        FitGridToContainer(participants.Count);
    }

    /// <summary>
    /// 参加人数に合わせて列数とカードサイズを決め、全員が一画面に収まるようにする。
    /// </summary>
    private void FitGridToContainer(int count)
    {
        GridLayoutGroup grid = cardContainer.GetComponent<GridLayoutGroup>();
        RectTransform containerRect = cardContainer as RectTransform;
        if (grid == null || containerRect == null || count <= 0) return;

        Canvas.ForceUpdateCanvases();

        float availableWidth = containerRect.rect.width - grid.padding.horizontal;
        float availableHeight = containerRect.rect.height - grid.padding.vertical;
        if (availableWidth <= 0f || availableHeight <= 0f) return;

        float aspect = Mathf.Max(0.1f, cardAspectRatio);
        float bestCellHeight = 0f;
        int bestColumns = 1;

        // 列数を 1〜count まで試し、カードが一番大きくなる列数を採用する
        for (int columns = 1; columns <= count; columns++)
        {
            int rows = Mathf.CeilToInt(count / (float)columns);

            // 間隔はセルサイズに比例させるので、 (列数 + 間隔数×比率) で割る
            float cellWidthByWidth = availableWidth / (columns + (columns - 1) * cardSpacingRatio);
            float cellHeightByHeight = availableHeight / (rows + (rows - 1) * cardSpacingRatio);

            float cellHeight = Mathf.Min(cellWidthByWidth / aspect, cellHeightByHeight);

            if (cellHeight > bestCellHeight)
            {
                bestCellHeight = cellHeight;
                bestColumns = columns;
            }
        }

        float cellH = Mathf.Floor(bestCellHeight);
        float cellW = Mathf.Floor(bestCellHeight * aspect);
        float spacing = Mathf.Floor(Mathf.Min(cellW, cellH) * cardSpacingRatio);

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = bestColumns;
        grid.cellSize = new Vector2(cellW, cellH);
        grid.spacing = new Vector2(spacing, spacing);
        grid.childAlignment = TextAnchor.MiddleCenter;
    }

    /// <summary>
    /// 名前表示用のダイナミックフォントを作る。
    /// 事前生成(Static)のフォントアセットだと収録していない漢字や記号が□になるため、
    /// 元フォントから必要な文字をその場で追加できるダイナミックフォントを使う。
    /// </summary>
    private TMP_FontAsset GetOrCreateNameFont(Material templateMaterial)
    {
        if (cachedNameFont != null) return cachedNameFont;
        if (nameSourceFont == null) return null;

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            nameSourceFont, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048,
            AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);

        if (fontAsset == null)
        {
            Debug.LogWarning($"名前用ダイナミックフォントの生成に失敗しました(font = {nameSourceFont.name})");
            return null;
        }

        // ビルドに含まれているシェーダーを使う(実行時生成のマテリアルはビルドに含まれないシェーダーを参照することがある)
        if (templateMaterial != null && fontAsset.material != null)
        {
            fontAsset.material.shader = templateMaterial.shader;
        }

        // 元フォントにも無い文字はOSのフォントから探す
        fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
        foreach (string family in osFallbackFontFamilies)
        {
            if (string.IsNullOrEmpty(family)) continue;

            TMP_FontAsset fallback = TMP_FontAsset.CreateFontAsset(family, "Regular");
            if (fallback == null) continue;

            if (templateMaterial != null && fallback.material != null)
            {
                fallback.material.shader = templateMaterial.shader;
            }
            fontAsset.fallbackFontAssetTable.Add(fallback);
        }

        cachedNameFont = fontAsset;
        return cachedNameFont;
    }

    private IEnumerator ClickToStartRoutine()
    {
        countdownText.text = clickToStartMessage;

        // 画面遷移直後のクリックを拾わないよう1フレーム待つ
        yield return null;

        while (!IsStartInputDown())
        {
            yield return null;
        }

        StartRace();
    }

    private static bool IsStartInputDown()
    {
        if (Input.GetMouseButtonDown(0)) return true;

        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began) return true;
        }

        return false;
    }

    private IEnumerator CountdownRoutine()
    {
        float remaining = countdownSeconds;

        while (remaining > 0f)
        {
            countdownText.text = Mathf.CeilToInt(remaining).ToString();
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }

        countdownText.text = "0";

        StartRace();
    }

    private void StartRace()
    {
        if (screenRoot != null)
        {
            screenRoot.SetActive(false);
        }

        raceManager.BeginRace();
    }
}
