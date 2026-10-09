using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BaseSceneのオプション画面を管理するクラス。
/// 音量設定・キャッシュクリア・動物画像の再読み込み・ゲーム終了を行う。
/// ボタンのonClickやスライダーのonValueChangedには、Inspectorから各publicメソッドを設定する。
/// </summary>
public class OptionManager : MonoBehaviour
{
    [Header("各種管理クラス")]
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private IllustrationManager illustrationManager;
    [SerializeField] private AnimalSelectionManager selectionManager;
    [SerializeField] private CameraMover cameraMover;

    [Header("UI")]
    [SerializeField] private UIPanelAnimator optionPanel;
    [SerializeField] private UIConfirmDialog confirmDialog;
    [SerializeField] private UIToast toast;

    [Header("音量スライダー (Min 0 / Max 1)")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider seVolumeSlider;

    [Header("Escキーでオプションを閉じるか")]
    [SerializeField] private bool closeWithEscape = true;

    public bool IsOpen => optionPanel != null && optionPanel.IsShown;

    private void Update()
    {
        if (!closeWithEscape || !IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 確認ダイアログが出ていればダイアログだけを閉じる
            if (confirmDialog != null && confirmDialog.IsOpen)
            {
                confirmDialog.Cancel();
            }
            else
            {
                CloseOption();
            }
        }
    }

    #region 開閉

    /// <summary>オプション画面を開く(ボタン用)。</summary>
    public void OpenOption()
    {
        if (IsOpen) return;

        // 動物を選択中だとカメラがフォーカスしたままになるため、先に解除する
        if (selectionManager != null)
        {
            selectionManager.Unselect();
        }
        // オプション操作中にWASDやマウスドラッグでカメラが動かないようにする
        if (cameraMover != null)
        {
            cameraMover.enabled = false;
        }

        SyncVolumeSliders();
        optionPanel.Show();
    }

    /// <summary>オプション画面を閉じる(ボタン用)。</summary>
    public void CloseOption()
    {
        if (!IsOpen) return;

        if (confirmDialog != null && confirmDialog.IsOpen)
        {
            confirmDialog.Cancel();
        }

        optionPanel.Hide();
        if (cameraMover != null)
        {
            cameraMover.enabled = true;
        }
    }

    /// <summary>オプション画面の開閉を切り替える(ボタン用)。</summary>
    public void ToggleOption()
    {
        if (IsOpen)
        {
            CloseOption();
        }
        else
        {
            OpenOption();
        }
    }

    #endregion

    #region 音量

    /// <summary>マスター音量スライダー用(onValueChangedのDynamic floatで設定する)。</summary>
    public void OnMasterVolumeChanged(float value)
    {
        if (audioManager != null) audioManager.SetMasterVolume(value);
    }

    /// <summary>BGM音量スライダー用(onValueChangedのDynamic floatで設定する)。</summary>
    public void OnBgmVolumeChanged(float value)
    {
        if (audioManager != null) audioManager.SetBgmVolume(value);
    }

    /// <summary>SE音量スライダー用(onValueChangedのDynamic floatで設定する)。</summary>
    public void OnSeVolumeChanged(float value)
    {
        if (audioManager != null) audioManager.SetSeVolume(value);
    }

    // スライダーの位置を現在の音量に合わせる。onValueChangedは発火させない
    private void SyncVolumeSliders()
    {
        if (audioManager == null) return;

        if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(audioManager.GetMasterVolume());
        if (bgmVolumeSlider != null) bgmVolumeSlider.SetValueWithoutNotify(audioManager.GetBgmVolume());
        if (seVolumeSlider != null) seVolumeSlider.SetValueWithoutNotify(audioManager.GetSeVolume());
    }

    #endregion

    #region キャッシュクリア

    /// <summary>キャッシュクリアボタン用。確認ダイアログを出してから削除する。</summary>
    public void OnClearCacheButtonClicked()
    {
        Confirm("キャッシュ（画像・レース履歴・音量設定）を\n削除しますか？", ClearCache);
    }

    private void ClearCache()
    {
        ImageCache.ClearAll();
        RaceHistoryStore.ClearAll();
        if (audioManager != null)
        {
            audioManager.ResetVolumesToDefault();
        }
        SyncVolumeSliders();

        Debug.Log("キャッシュを削除しました");
        ShowToast("キャッシュを削除しました");
    }

    #endregion

    #region 動物画像の再読み込み

    /// <summary>動物画像の再読み込みボタン用。表示中の動物をすべて消して読み込み直す。</summary>
    public void OnReloadButtonClicked()
    {
        if (illustrationManager == null) return;

        illustrationManager.ReloadAllIllustrations();
        ShowToast("動物を読み込み直しています…");
    }

    #endregion

    #region ゲーム終了

    /// <summary>ゲーム終了ボタン用。確認ダイアログを出してから終了する。</summary>
    public void OnQuitButtonClicked()
    {
        Confirm("ゲームを終了しますか？", QuitGame);
    }

    private void QuitGame()
    {
        Debug.Log("ゲームを終了します");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    // 確認ダイアログが設定されていなければ、確認せずにそのまま実行する
    private void Confirm(string message, System.Action onYes)
    {
        if (confirmDialog != null)
        {
            confirmDialog.Open(message, onYes);
        }
        else
        {
            onYes?.Invoke();
        }
    }

    private void ShowToast(string message)
    {
        if (toast != null)
        {
            toast.Show(message);
        }
    }
}
