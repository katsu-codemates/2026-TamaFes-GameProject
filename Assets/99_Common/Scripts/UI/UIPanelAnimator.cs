using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// UIパネルの表示・非表示をDOTweenでアニメーションさせるクラス。
/// アニメーションさせたいパネルのルート(RectTransform)に付け、Show()/Hide()/Toggle()を呼ぶ。
/// Time.timeScaleの影響を受けないよう、実時間で再生する。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIPanelAnimator : MonoBehaviour
{
    public enum AnimationType
    {
        Fade,           // フェードのみ
        Scale,          // フェード＋拡大縮小
        SlideFromTop,   // フェード＋上から
        SlideFromBottom,// フェード＋下から
        SlideFromLeft,  // フェード＋左から
        SlideFromRight, // フェード＋右から
    }

    [Header("演出")]
    [SerializeField] private AnimationType animationType = AnimationType.Scale;

    [Header("表示")]
    [SerializeField, Min(0f)] private float showDuration = 0.3f;
    [SerializeField] private Ease showEase = Ease.OutBack;

    [Header("非表示")]
    [SerializeField, Min(0f)] private float hideDuration = 0.2f;
    [SerializeField] private Ease hideEase = Ease.InBack;

    [Header("Scale用: 非表示時の大きさ")]
    [SerializeField, Range(0f, 1f)] private float hiddenScale = 0.8f;

    [Header("Slide用: 移動距離(px)")]
    [SerializeField] private float slideDistance = 300f;

    [Header("初期状態")]
    [Tooltip("起動時に非表示にしておくか")]
    [SerializeField] private bool hideOnAwake = true;
    [Tooltip("非表示になったらGameObjectを非アクティブにするか")]
    [SerializeField] private bool deactivateOnHidden = true;

    [Header("イベント")]
    public UnityEvent onShowStarted;
    public UnityEvent onShown;
    public UnityEvent onHideStarted;
    public UnityEvent onHidden;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 shownPosition;
    private Vector3 shownScale;
    private Sequence sequence;
    private bool initialized;

    /// <summary>表示中(表示アニメーション中を含む)かどうか。</summary>
    public bool IsShown { get; private set; }

    /// <summary>アニメーション再生中かどうか。</summary>
    public bool IsAnimating => sequence != null && sequence.IsActive() && sequence.IsPlaying();

    private void Awake()
    {
        Initialize();

        // 非アクティブなパネルに対してShow()が呼ばれ、SetActive(true)でAwakeが走った場合は表示処理を優先する
        if (IsShown) return;

        if (hideOnAwake)
        {
            HideImmediate();
        }
        else
        {
            IsShown = true;
            SetInteractable(true);
        }
    }

    private void OnDestroy()
    {
        sequence?.Kill();
    }

    // 非アクティブな状態から最初にShow()が呼ばれた場合にも備え、Awake以外からも呼べるようにしておく
    private void Initialize()
    {
        if (initialized) return;
        initialized = true;

        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            shownPosition = rectTransform.anchoredPosition;
        }
        shownScale = transform.localScale;
    }

    /// <summary>パネルを表示する。</summary>
    public void Show()
    {
        Show(null);
    }

    /// <summary>パネルを表示する。表示し終わったらonCompleteを呼ぶ。</summary>
    public void Show(Action onComplete)
    {
        Initialize();
        if (IsShown)
        {
            onComplete?.Invoke();
            return;
        }
        IsShown = true;

        gameObject.SetActive(true);
        sequence?.Kill();

        // 非表示状態から始める
        ApplyHiddenState();
        // 表示アニメーション中から操作を受け付ける(閉じるボタンの連打などに対応するため)
        SetInteractable(true);

        onShowStarted?.Invoke();

        sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject);

        sequence.Join(canvasGroup.DOFade(1f, showDuration).SetEase(Ease.OutQuad));
        switch (animationType)
        {
            case AnimationType.Scale:
                sequence.Join(transform.DOScale(shownScale, showDuration).SetEase(showEase));
                break;
            case AnimationType.SlideFromTop:
            case AnimationType.SlideFromBottom:
            case AnimationType.SlideFromLeft:
            case AnimationType.SlideFromRight:
                if (rectTransform != null)
                {
                    sequence.Join(rectTransform.DOAnchorPos(shownPosition, showDuration).SetEase(showEase));
                }
                break;
        }

        sequence.OnComplete(() =>
        {
            onShown?.Invoke();
            onComplete?.Invoke();
        });
    }

    /// <summary>パネルを非表示にする。</summary>
    public void Hide()
    {
        Hide(null);
    }

    /// <summary>パネルを非表示にする。非表示になったらonCompleteを呼ぶ。</summary>
    public void Hide(Action onComplete)
    {
        Initialize();
        if (!IsShown)
        {
            onComplete?.Invoke();
            return;
        }
        IsShown = false;

        sequence?.Kill();
        // 閉じている最中は操作させない
        SetInteractable(false);

        onHideStarted?.Invoke();

        sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject);

        sequence.Join(canvasGroup.DOFade(0f, hideDuration).SetEase(Ease.InQuad));
        switch (animationType)
        {
            case AnimationType.Scale:
                sequence.Join(transform.DOScale(shownScale * hiddenScale, hideDuration).SetEase(hideEase));
                break;
            case AnimationType.SlideFromTop:
            case AnimationType.SlideFromBottom:
            case AnimationType.SlideFromLeft:
            case AnimationType.SlideFromRight:
                if (rectTransform != null)
                {
                    sequence.Join(rectTransform.DOAnchorPos(shownPosition + GetSlideOffset(), hideDuration).SetEase(hideEase));
                }
                break;
        }

        sequence.OnComplete(() =>
        {
            if (deactivateOnHidden)
            {
                gameObject.SetActive(false);
            }
            onHidden?.Invoke();
            onComplete?.Invoke();
        });
    }

    /// <summary>表示中なら非表示に、非表示なら表示にする。</summary>
    public void Toggle()
    {
        if (IsShown)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    /// <summary>アニメーションせずに即座に表示する。</summary>
    public void ShowImmediate()
    {
        Initialize();
        sequence?.Kill();
        IsShown = true;

        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        transform.localScale = shownScale;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = shownPosition;
        }
        SetInteractable(true);
    }

    /// <summary>アニメーションせずに即座に非表示にする。</summary>
    public void HideImmediate()
    {
        Initialize();
        sequence?.Kill();
        IsShown = false;

        ApplyHiddenState();
        SetInteractable(false);
        if (deactivateOnHidden)
        {
            gameObject.SetActive(false);
        }
    }

    // 演出の開始地点(非表示時の見た目)にする
    private void ApplyHiddenState()
    {
        canvasGroup.alpha = 0f;
        transform.localScale = animationType == AnimationType.Scale ? shownScale * hiddenScale : shownScale;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = shownPosition + GetSlideOffset();
        }
    }

    private Vector2 GetSlideOffset()
    {
        switch (animationType)
        {
            case AnimationType.SlideFromTop: return new Vector2(0f, slideDistance);
            case AnimationType.SlideFromBottom: return new Vector2(0f, -slideDistance);
            case AnimationType.SlideFromLeft: return new Vector2(-slideDistance, 0f);
            case AnimationType.SlideFromRight: return new Vector2(slideDistance, 0f);
            default: return Vector2.zero;
        }
    }

    private void SetInteractable(bool value)
    {
        canvasGroup.interactable = value;
        canvasGroup.blocksRaycasts = value;
    }
}
