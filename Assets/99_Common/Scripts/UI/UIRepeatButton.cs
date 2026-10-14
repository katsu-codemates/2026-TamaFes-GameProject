using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Buttonを長押ししている間、ButtonのonClickを繰り返し呼び出すクラス。
/// Buttonと同じGameObjectに付ける。タップ時は通常どおりonClickが1回だけ呼ばれ、
/// 長押しで連続入力が始まった場合は、指を離したときのクリックを無効にして1回多く進まないようにする。
/// ボタンが押せなくなった(interactableがfalseになった)時点で連続入力を止める。
/// </summary>
[RequireComponent(typeof(Button))]
public class UIRepeatButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("長押しと判定するまでの時間（秒）")]
    [SerializeField] private float initialDelay = 0.4f;

    [Header("連続入力の間隔（秒）。最初の間隔から押し続けるほど最短の間隔まで短くなる")]
    [SerializeField] private float startInterval = 0.12f;
    [SerializeField] private float minInterval = 0.03f;
    [SerializeField] private float accelerationTime = 1.5f;

    private Button button;
    private bool isPressed = false;
    private bool hasRepeated = false;
    private float pressedTime;
    private float nextRepeatTime;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnDisable()
    {
        isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !button.IsInteractable()) return;

        isPressed = true;
        hasRepeated = false;
        // オプション画面でtimeScaleが変わっていても動くようにunscaledTimeを使う
        pressedTime = Time.unscaledTime;
        nextRepeatTime = pressedTime + initialDelay;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 連続入力した後は、離したときのクリックで1回多く進まないようにする
        if (hasRepeated)
        {
            eventData.eligibleForClick = false;
        }
        isPressed = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPressed = false;
    }

    private void Update()
    {
        if (!isPressed) return;

        if (!button.IsInteractable())
        {
            isPressed = false;
            return;
        }

        if (Time.unscaledTime < nextRepeatTime) return;

        hasRepeated = true;
        button.onClick.Invoke();

        float t = accelerationTime > 0f ? (Time.unscaledTime - pressedTime - initialDelay) / accelerationTime : 1f;
        nextRepeatTime = Time.unscaledTime + Mathf.Lerp(startInterval, minInterval, Mathf.Clamp01(t));
    }
}
