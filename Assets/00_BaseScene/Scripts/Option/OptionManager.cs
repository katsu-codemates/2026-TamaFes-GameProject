using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BaseSceneのオプション画面を管理するクラス。
/// 音量設定・動物の表示上限・出走数・カメラ操作設定・キャッシュクリア・動物画像の再読み込み・ゲーム終了を行う。
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

    [Header("動物の表示上限 (最小・最大値はIllustrationManagerの設定に従う)")]
    [SerializeField] private Button maxDisplayCountMinusButton;
    [SerializeField] private Button maxDisplayCountPlusButton;
    [SerializeField] private TextMeshProUGUI maxDisplayCountLabel;

    [Header("出走数 (最小・最大値はRacerCountSettingの定数に従う)")]
    [SerializeField] private Button racerCountMinusButton;
    [SerializeField] private Button racerCountPlusButton;
    [SerializeField] private TextMeshProUGUI racerCountLabel;

    [Header("カメラ感度スライダー (最小・最大値はCameraMoverの設定で上書きされる)")]
    [SerializeField] private Slider cameraMouseSensitivitySlider;
    [SerializeField] private TextMeshProUGUI cameraMouseSensitivityLabel;
    [SerializeField] private Slider cameraKeySensitivitySlider;
    [SerializeField] private TextMeshProUGUI cameraKeySensitivityLabel;

    [Header("カメラ反転トグル")]
    [SerializeField] private Toggle cameraInvertHorizontalToggle;
    [SerializeField] private Toggle cameraInvertVerticalToggle;

    [Header("Escキーでオプションを閉じるか")]
    [SerializeField] private bool closeWithEscape = true;

    [Header("クリック防止")]
    [SerializeField] private GameObject[] clickBlockers;

    public bool IsOpen => optionPanel != null && optionPanel.IsShown;

    private bool isSyncingCameraSettingsUI = false;

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
        UpdateMaxDisplayCountUI();
        UpdateRacerCountUI();
        SyncCameraSettingsUI();
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

    #region 動物の表示上限

    /// <summary>表示上限のプラスボタン用。</summary>
    public void IncreaseMaxDisplayCount()
    {
        ChangeMaxDisplayCount(1);
    }

    /// <summary>表示上限のマイナスボタン用。</summary>
    public void DecreaseMaxDisplayCount()
    {
        ChangeMaxDisplayCount(-1);
    }

    private void ChangeMaxDisplayCount(int delta)
    {
        if (illustrationManager == null) return;

        illustrationManager.SetMaxDisplayCount(illustrationManager.MaxDisplayCount + delta);
        UpdateMaxDisplayCountUI();
    }

    // ラベルを現在の表示上限に合わせ、最小値ならマイナス・最大値ならプラスを押せなくする
    private void UpdateMaxDisplayCountUI()
    {
        if (illustrationManager == null) return;

        UpdateStepperUI(
            illustrationManager.MaxDisplayCount,
            illustrationManager.MinMaxDisplayCount,
            illustrationManager.MaxMaxDisplayCount,
            maxDisplayCountMinusButton,
            maxDisplayCountPlusButton,
            maxDisplayCountLabel);
    }

    #endregion

    #region 出走数

    /// <summary>出走数のプラスボタン用。</summary>
    public void IncreaseRacerCount()
    {
        ChangeRacerCount(1);
    }

    /// <summary>出走数のマイナスボタン用。</summary>
    public void DecreaseRacerCount()
    {
        ChangeRacerCount(-1);
    }

    private void ChangeRacerCount(int delta)
    {
        RacerCountSetting.Set(RacerCountSetting.Get() + delta);
        UpdateRacerCountUI();
    }

    // ラベルを現在の出走数に合わせ、最小値ならマイナス・最大値ならプラスを押せなくする
    private void UpdateRacerCountUI()
    {
        UpdateStepperUI(
            RacerCountSetting.Get(),
            RacerCountSetting.Min,
            RacerCountSetting.Max,
            racerCountMinusButton,
            racerCountPlusButton,
            racerCountLabel);
    }

    #endregion

    #region プラス・マイナスボタン共通

    private void UpdateStepperUI(int value, int min, int max, Button minusButton, Button plusButton, TextMeshProUGUI label)
    {
        if (minusButton != null) minusButton.interactable = value > min;
        if (plusButton != null) plusButton.interactable = value < max;
        if (label != null) label.text = $"{value}";
    }

    #endregion

    #region カメラ操作

    /// <summary>カメラ感度(マウス)スライダー用(onValueChangedのDynamic floatで設定する)。</summary>
    public void OnCameraMouseSensitivityChanged(float value)
    {
        if (cameraMover == null || isSyncingCameraSettingsUI) return;

        cameraMover.SetMouseSensitivityMultiplier(value);
        UpdateCameraSensitivityLabels();
    }

    /// <summary>カメラ感度(WASD)スライダー用(onValueChangedのDynamic floatで設定する)。</summary>
    public void OnCameraKeySensitivityChanged(float value)
    {
        if (cameraMover == null || isSyncingCameraSettingsUI) return;

        cameraMover.SetKeySensitivityMultiplier(value);
        UpdateCameraSensitivityLabels();
    }

    /// <summary>カメラ左右反転トグル用(onValueChangedのDynamic boolで設定する)。</summary>
    public void OnCameraInvertHorizontalChanged(bool isOn)
    {
        if (cameraMover != null && !isSyncingCameraSettingsUI) cameraMover.SetInvertHorizontal(isOn);
    }

    /// <summary>カメラ上下反転トグル用(onValueChangedのDynamic boolで設定する)。</summary>
    public void OnCameraInvertVerticalChanged(bool isOn)
    {
        if (cameraMover != null && !isSyncingCameraSettingsUI) cameraMover.SetInvertVertical(isOn);
    }

    /// <summary>カメラ位置リセットボタン用</summary>
    public void ResetCameraPosition()
    {
        if (cameraMover != null)
        {
            cameraMover.ForcedResetCameraToDefault();
        }
    }

    // スライダー・トグルを現在のカメラ設定に合わせる。onValueChangedは発火させない
    private void SyncCameraSettingsUI()
    {
        if (cameraMover == null) return;

        // minValue/maxValueの変更で値がクランプされるとonValueChangedが発火するため、その間は保存しない
        isSyncingCameraSettingsUI = true;
        SyncCameraSensitivitySlider(cameraMouseSensitivitySlider, cameraMover.MouseSensitivityMultiplier);
        SyncCameraSensitivitySlider(cameraKeySensitivitySlider, cameraMover.KeySensitivityMultiplier);
        if (cameraInvertHorizontalToggle != null) cameraInvertHorizontalToggle.SetIsOnWithoutNotify(cameraMover.InvertHorizontal);
        if (cameraInvertVerticalToggle != null) cameraInvertVerticalToggle.SetIsOnWithoutNotify(cameraMover.InvertVertical);
        isSyncingCameraSettingsUI = false;
        UpdateCameraSensitivityLabels();
    }

    private void SyncCameraSensitivitySlider(Slider slider, float value)
    {
        if (slider == null) return;

        slider.wholeNumbers = false;
        slider.minValue = cameraMover.MinSensitivityMultiplier;
        slider.maxValue = cameraMover.MaxSensitivityMultiplier;
        slider.SetValueWithoutNotify(value);
    }

    private void UpdateCameraSensitivityLabels()
    {
        if (cameraMover == null) return;

        if (cameraMouseSensitivityLabel != null) cameraMouseSensitivityLabel.text = $"x{cameraMover.MouseSensitivityMultiplier:0.0}";
        if (cameraKeySensitivityLabel != null) cameraKeySensitivityLabel.text = $"x{cameraMover.KeySensitivityMultiplier:0.0}";
    }

    #endregion

    #region キャッシュクリア

    /// <summary>キャッシュクリアボタン用。確認ダイアログを出してから削除する。</summary>
    public void OnClearCacheButtonClicked()
    {
        Confirm("キャッシュ（画像・レース履歴・音量設定・表示上限・出走数・カメラ設定）を\n削除しますか？", ClearCache);
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
        if (illustrationManager != null)
        {
            illustrationManager.ResetMaxDisplayCountToDefault();
        }
        UpdateMaxDisplayCountUI();
        RacerCountSetting.ResetToDefault();
        UpdateRacerCountUI();
        if (cameraMover != null)
        {
            cameraMover.ResetSettingsToDefault();
        }
        SyncCameraSettingsUI();

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

    public void SetClickBlockerActive(int index)
    {
        if (clickBlockers != null && index < clickBlockers.Length)
        {
            clickBlockers[index].SetActive(true);
        }
    }

    public void SetClickBlockerInactive(int index)
    {
        if (clickBlockers != null && index < clickBlockers.Length)
        {
            clickBlockers[index].SetActive(false);
        }
    }
}
