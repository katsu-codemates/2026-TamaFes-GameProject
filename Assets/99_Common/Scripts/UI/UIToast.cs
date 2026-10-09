using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 「キャッシュを削除しました」のような一時メッセージを表示して、自動で消すクラス。
/// 少し浮き上がりながらフェードインし、一定時間後にフェードアウトする。
/// 表示中に次のメッセージが来たら、すぐに差し替える。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIToast : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("時間(秒)")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.25f;
    [SerializeField, Min(0f)] private float displayDuration = 2f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.4f;

    [Header("表示時に浮き上がる距離(px)")]
    [SerializeField] private float riseDistance = 30f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 basePosition;
    private Sequence sequence;
    private bool initialized;

    private void Awake()
    {
        Initialize();
        // 初回のShow()でSetActive(true)された場合はここで隠さない
        if (sequence == null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void OnDestroy()
    {
        sequence?.Kill();
    }

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;

        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            basePosition = rectTransform.anchoredPosition;
        }

        // トーストの下にあるボタンなどの操作を妨げない
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>メッセージを表示し、一定時間後に自動で消す。</summary>
    public void Show(string message)
    {
        Initialize();
        sequence?.Kill();

        if (messageText != null)
        {
            messageText.text = message;
        }

        sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject);

        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = basePosition - new Vector2(0f, riseDistance);
            sequence.Join(rectTransform.DOAnchorPos(basePosition, fadeInDuration).SetEase(Ease.OutCubic));
        }
        sequence.Join(canvasGroup.DOFade(1f, fadeInDuration));
        sequence.AppendInterval(displayDuration);
        sequence.Append(canvasGroup.DOFade(0f, fadeOutDuration));
    }

    /// <summary>表示中のメッセージを即座に消す。</summary>
    public void HideImmediate()
    {
        Initialize();
        sequence?.Kill();
        canvasGroup.alpha = 0f;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = basePosition;
        }
    }
}
