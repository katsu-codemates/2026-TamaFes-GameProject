using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// レース狩猟時に呼び出され、最終順位をならべて表示させるクラス。
/// 1位は専用の大きなカードで表示し、2位以下は行として並べる。
/// </summary>
public class RaceResultScreen : MonoBehaviour
{
    [Header("この画面全体のルート")]
    [SerializeField] private GameObject screenRoot;

    [Header("結果行のプレハブ")]
    [SerializeField] private GameObject resultRowPrefab;

    [Header("結果行を並べる親オブジェクト")]
    [SerializeField] private Transform rowContainer;

    [Header("1位専用カードのプレハブ(未設定なら1位も行として並べる)")]
    [SerializeField] private GameObject winnerCardPrefab;

    [Header("1位カードを置く親オブジェクト")]
    [SerializeField] private Transform winnerContainer;

    [Header("登場演出")]
    [Tooltip("1位カードが出てから2位の行が出始めるまでの秒数")]
    [SerializeField] private float rowsStartDelay = 0.5f;
    [Tooltip("行と行の登場間隔(秒)")]
    [SerializeField] private float rowInterval = 0.12f;
    [Tooltip("行がスライドしてくる距離(px)")]
    [SerializeField] private float rowSlideDistance = 300f;
    [SerializeField] private float rowSlideDuration = 0.35f;

    [Header("2位以下の一覧の自動スクロール(未設定なら自動スクロールしない)")]
    [SerializeField] private RaceResultAutoScroller autoScroller;

    private void Awake()
    {
        if (screenRoot != null)
        {
            screenRoot.SetActive(false);
        }
    }

    /// <summary>
    /// 最終着順を受け取って表示する。
    /// </summary>
    public void Show(List<RaceParticipant> finishedOrder)
    {
        ClearRows();

        if (screenRoot != null)
        {
            screenRoot.SetActive(true);
        }

        bool useWinnerCard = winnerCardPrefab != null && winnerContainer != null && finishedOrder.Count > 0;
        float rowsDelay = 0f;

        if (useWinnerCard)
        {
            GameObject card = Instantiate(winnerCardPrefab, winnerContainer);
            card.GetComponent<RaceResultRowView>().Setup(finishedOrder[0]);

            RaceResultWinnerEffect effect = card.GetComponent<RaceResultWinnerEffect>();
            if (effect != null)
            {
                effect.Play(0f);
            }
            rowsDelay = rowsStartDelay;
        }

        List<RectTransform> rows = new List<RectTransform>();
        for (int i = useWinnerCard ? 1 : 0; i < finishedOrder.Count; i++)
        {
            GameObject row = Instantiate(resultRowPrefab, rowContainer);
            RaceResultRowView view = row.GetComponent<RaceResultRowView>();
            view.Setup(finishedOrder[i]);
            rows.Add((RectTransform)row.transform);
        }

        PlayRowsAppear(rows, rowsDelay);

        if (autoScroller != null)
        {
            // 全行が出そろってから自動スクロールを始める
            autoScroller.StartAutoScroll(rowsDelay + rows.Count * rowInterval + rowSlideDuration);
        }
    }

    /// <summary>
    /// 行を上から順に右からスライドインさせる。
    /// </summary>
    private void PlayRowsAppear(List<RectTransform> rows, float startDelay)
    {
        // GridLayoutGroupの並び位置を先に確定させ、そこを着地点にする
        if (rowContainer is RectTransform containerRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }

        for (int i = 0; i < rows.Count; i++)
        {
            RectTransform row = rows[i];
            CanvasGroup group = row.GetComponent<CanvasGroup>();
            if (group == null) group = row.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            // 位置はレイアウトに上書きされることがあるので、着地点からのずれだけを毎フレーム書き込む
            Vector2 landing = row.anchoredPosition;
            float delay = startDelay + i * rowInterval;
            DOTween.To(() => rowSlideDistance, x => row.anchoredPosition = landing + new Vector2(x, 0f), 0f, rowSlideDuration)
                .SetEase(Ease.OutCubic).SetDelay(delay).SetLink(row.gameObject);
            group.DOFade(1f, rowSlideDuration * 0.6f).SetDelay(delay).SetLink(row.gameObject);
        }
    }

    private void ClearRows()
    {
        for (int i = rowContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(rowContainer.GetChild(i).gameObject);
        }

        if (winnerContainer != null)
        {
            for (int i = winnerContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(winnerContainer.GetChild(i).gameObject);
            }
        }
    }

}
