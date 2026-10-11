using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 結果画面の1位カードを派手に見せる演出。
/// 登場時にカードを弾ませ、その後は後光の回転・発光の明滅・光の帯・きらめきをループさせる。
/// </summary>
public class RaceResultWinnerEffect : MonoBehaviour
{
    [Header("フェードイン用(カード全体)")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("登場時に弾ませるカード本体")]
    [SerializeField] private RectTransform cardBody;

    [Header("回転させる後光")]
    [SerializeField] private RectTransform rays;
    [SerializeField] private float raysRotateSeconds = 12f;

    [Header("明滅させる発光")]
    [SerializeField] private Image glow;

    [Header("鼓動させる順位テキスト")]
    [SerializeField] private TextMeshProUGUI rankText;

    [Header("カードの上を横切る光の帯(親が切り抜き範囲になる)")]
    [SerializeField] private RectTransform shine;
    [SerializeField] private float shineInterval = 2.5f;

    [Header("きらめき")]
    [SerializeField] private RectTransform[] sparkles;

    private bool hasPlayed;

    private void Awake()
    {
        // Play()までは見えないようにしておく
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 登場演出を開始する。delay秒待ってから現れる。
    /// </summary>
    public void Play(float delay)
    {
        if (hasPlayed) return;
        hasPlayed = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, 0.3f).SetDelay(delay).SetLink(gameObject);
        }

        if (cardBody != null)
        {
            cardBody.localScale = Vector3.one * 0.3f;
            cardBody.DOScale(1f, 0.6f).SetEase(Ease.OutBack, 2f).SetDelay(delay).SetLink(gameObject);
        }

        // ここから下は登場後もずっと続くループ演出
        float loopStart = delay + 0.4f;

        if (rays != null)
        {
            rays.DORotate(new Vector3(0f, 0f, -360f), raysRotateSeconds, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart).SetLink(gameObject);
            rays.localScale = Vector3.zero;
            rays.DOScale(1f, 0.8f).SetEase(Ease.OutCubic).SetDelay(delay).SetLink(gameObject);
        }

        if (glow != null)
        {
            Color c = glow.color;
            glow.color = new Color(c.r, c.g, c.b, c.a * 0.5f);
            glow.DOFade(c.a, 0.9f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
                .SetDelay(loopStart).SetLink(gameObject);
            glow.rectTransform.DOScale(1.08f, 0.9f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
                .SetDelay(loopStart).SetLink(gameObject);
        }

        if (rankText != null)
        {
            rankText.rectTransform.DOScale(1.12f, 0.45f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
                .SetDelay(loopStart).SetLink(gameObject);
        }

        PlayShine(loopStart);
        PlaySparkles(delay);
    }

    private void PlayShine(float delay)
    {
        if (shine == null) return;

        RectTransform area = shine.parent as RectTransform;
        // 光の帯がカードからはみ出さないよう、親で切り抜く
        if (area != null && area.GetComponent<RectMask2D>() == null)
        {
            area.gameObject.AddComponent<RectMask2D>();
        }
        float halfWidth = (area != null ? area.rect.width : 1000f) * 0.5f + shine.rect.width;
        shine.anchoredPosition = new Vector2(-halfWidth, shine.anchoredPosition.y);

        DOTween.Sequence()
            .Append(shine.DOAnchorPosX(halfWidth, 0.9f).SetEase(Ease.InOutQuad))
            .AppendInterval(shineInterval)
            .SetDelay(delay)
            .SetLoops(-1, LoopType.Restart)
            .SetLink(gameObject);
    }

    private void PlaySparkles(float delay)
    {
        if (sparkles == null) return;

        foreach (RectTransform sparkle in sparkles)
        {
            if (sparkle == null) continue;

            sparkle.localScale = Vector3.zero;
            // きらめきごとに間隔をずらして、バラバラに光らせる
            float duration = Random.Range(0.5f, 0.8f);
            DOTween.Sequence()
                .Append(sparkle.DOScale(1f, duration).SetEase(Ease.OutQuad))
                .Join(sparkle.DOLocalRotate(new Vector3(0f, 0f, 90f), duration * 2f, RotateMode.LocalAxisAdd).SetEase(Ease.Linear))
                .Append(sparkle.DOScale(0f, duration).SetEase(Ease.InQuad))
                .AppendInterval(Random.Range(0.3f, 1.2f))
                .SetDelay(delay + Random.Range(0.2f, 1.2f))
                .SetLoops(-1, LoopType.Restart)
                .SetLink(gameObject);
        }
    }
}
