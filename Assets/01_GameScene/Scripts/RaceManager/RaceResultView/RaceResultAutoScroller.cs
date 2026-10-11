using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 結果一覧のスクロールを自動で上下に往復させる。
/// ホイールやドラッグで触られたら一時停止し、しばらく操作が無ければ再開する。
/// 一覧の上をクリックしたとき(ドラッグでないとき)は、背面の「戻る」ボタンへクリックを渡す。
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class RaceResultAutoScroller : MonoBehaviour,
    IBeginDragHandler, IScrollHandler, IPointerDownHandler, IPointerClickHandler
{
    private enum Phase { WaitTop, Down, WaitBottom, Up }

    [SerializeField] private ScrollRect scrollRect;

    [Header("自動スクロール")]
    [Tooltip("スクロールの速さ(px/秒)")]
    [SerializeField] private float scrollSpeed = 80f;
    [Tooltip("一番上・一番下で止まる秒数")]
    [SerializeField] private float waitAtEnds = 2f;
    [Tooltip("手動で操作したあと、自動スクロールを再開するまでの秒数")]
    [SerializeField] private float resumeAfterIdle = 4f;

    [Header("一覧の上をクリックしたときに押したことにするボタン(未設定なら何もしない)")]
    [SerializeField] private Button clickThroughButton;

    private bool running;
    private Phase phase;
    private float waitTimer;
    private float idleTimer;
    private bool dragged;

    private void Reset()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    /// <summary>
    /// 一番上に戻してから、delay秒後に自動スクロールを始める。
    /// </summary>
    public void StartAutoScroll(float delay)
    {
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;
        phase = Phase.WaitTop;
        waitTimer = delay + waitAtEnds;
        idleTimer = resumeAfterIdle;
        running = true;
    }

    private void Update()
    {
        if (!running) return;

        // 手動操作の直後は自動スクロールを止めておく
        if (idleTimer < resumeAfterIdle)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= resumeAfterIdle)
            {
                scrollRect.StopMovement();
                phase = Phase.Down;
            }
            return;
        }

        float overflow = scrollRect.content.rect.height - scrollRect.viewport.rect.height;
        if (overflow <= 0f) return; // 全部見えているならスクロール不要

        float step = scrollSpeed / overflow * Time.deltaTime;
        float pos = scrollRect.verticalNormalizedPosition;

        switch (phase)
        {
            case Phase.WaitTop:
            case Phase.WaitBottom:
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    phase = phase == Phase.WaitTop ? Phase.Down : Phase.Up;
                }
                break;

            case Phase.Down:
                pos = Mathf.Max(0f, pos - step);
                if (pos <= 0f)
                {
                    phase = Phase.WaitBottom;
                    waitTimer = waitAtEnds;
                }
                scrollRect.verticalNormalizedPosition = pos;
                break;

            case Phase.Up:
                pos = Mathf.Min(1f, pos + step);
                if (pos >= 1f)
                {
                    phase = Phase.WaitTop;
                    waitTimer = waitAtEnds;
                }
                scrollRect.verticalNormalizedPosition = pos;
                break;
        }
    }

    private void OnUserInteracted()
    {
        idleTimer = 0f;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragged = true;
        OnUserInteracted();
    }

    public void OnScroll(PointerEventData eventData)
    {
        OnUserInteracted();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        dragged = false;
        OnUserInteracted();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // ドラッグでスクロールしただけのときは「戻る」を押さない
        if (dragged || clickThroughButton == null || !clickThroughButton.interactable) return;
        clickThroughButton.onClick.Invoke();
    }
}
