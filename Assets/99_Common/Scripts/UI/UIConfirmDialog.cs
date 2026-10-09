using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 「本当に〜しますか？」のような はい/いいえ の確認ダイアログ。
/// 開閉アニメーションは同じGameObjectに付けたUIPanelAnimatorが担当する。
/// はい/いいえボタンのonClickには、InspectorからOnYesClicked/OnNoClickedを設定する。
/// </summary>
[RequireComponent(typeof(UIPanelAnimator))]
public class UIConfirmDialog : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;

    private UIPanelAnimator panelAnimator;
    private Action onYes;
    private Action onNo;

    /// <summary>ダイアログを開いているかどうか。</summary>
    public bool IsOpen => PanelAnimator.IsShown;

    // 非アクティブなまま参照された場合にも取得できるようにする
    private UIPanelAnimator PanelAnimator
    {
        get
        {
            if (panelAnimator == null) panelAnimator = GetComponent<UIPanelAnimator>();
            return panelAnimator;
        }
    }

    /// <summary>
    /// メッセージを表示してダイアログを開く。
    /// はいならonYes、いいえならonNoが、ダイアログが閉じ終わってから呼ばれる。
    /// </summary>
    public void Open(string message, Action onYes, Action onNo = null)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
        this.onYes = onYes;
        this.onNo = onNo;
        PanelAnimator.Show();
    }

    /// <summary>はいボタン用(InspectorのonClickから呼ぶ)。</summary>
    public void OnYesClicked()
    {
        Close(onYes);
    }

    /// <summary>いいえボタン用(InspectorのonClickから呼ぶ)。</summary>
    public void OnNoClicked()
    {
        Close(onNo);
    }

    /// <summary>何も選ばずに閉じる(いいえ扱い)。Escキーなどで閉じる場合に使う。</summary>
    public void Cancel()
    {
        if (!IsOpen) return;
        OnNoClicked();
    }

    private void Close(Action callback)
    {
        if (!IsOpen) return;

        // 連打で2回呼ばれないよう、閉じる前にコールバックを外しておく
        onYes = null;
        onNo = null;
        PanelAnimator.Hide(callback);
    }
}
