using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 動物を「なまえ」または「さくしゃ」で検索し、ヒットした動物にフォーカスさせるクラス。
/// 1件ヒットならそのままフォーカスし、複数ヒットなら候補リストを表示して選ばせる。
/// ボタンのonClickやInputFieldのonSubmitには、Inspectorから各publicメソッドを設定する。
/// </summary>
public class AnimalSearchManager : MonoBehaviour
{
    public enum SearchMode
    {
        Name,    // 動物の名前で検索
        Creator, // 作者名で検索
    }

    [Header("各種管理クラス")]
    [SerializeField] private AnimalSelectionManager selectionManager;
    [SerializeField] private IllustrationManager illustrationManager;
    [SerializeField] private CameraMover cameraMover;
    [SerializeField] private OptionManager optionManager;

    [Header("UI")]
    [SerializeField] private UIPanelAnimator searchPanel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI modeLabel;   // 検索モード切替ボタンのラベル
    [SerializeField] private TextMeshProUGUI messageText; // 検索結果のメッセージ

    [Header("候補リスト")]
    [SerializeField] private Transform candidateContainer;
    [SerializeField] private AnimalSearchCandidateItem candidatePrefab;

    [Header("検索画面を開くキー")]
    [SerializeField] private KeyCode openKey = KeyCode.F;

    private SearchMode searchMode = SearchMode.Name;
    private readonly List<AnimalSearchCandidateItem> candidateItems = new List<AnimalSearchCandidateItem>();

    public bool IsOpen => searchPanel != null && searchPanel.IsShown;

    private void Start()
    {
        UpdateModeLabel();
        SetMessage(string.Empty);
    }

    private void Update()
    {
        if (IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseSearch();
            }
            return;
        }

        if (Input.GetKeyDown(openKey) && !IsTypingInOtherInputField())
        {
            OpenSearch();
        }
    }

    #region 開閉

    /// <summary>検索画面を開く(ボタン用)。</summary>
    public void OpenSearch()
    {
        if (IsOpen) return;
        // オプション画面と同時に開かないようにする
        if (optionManager != null && optionManager.IsOpen) return;

        // 動物を選択中だとカメラがフォーカスしたままになるため、先に解除する
        if (selectionManager != null)
        {
            selectionManager.Unselect();
        }
        // 文字入力中にWASDやSpace、Pキーでカメラが動かないようにする
        if (cameraMover != null)
        {
            cameraMover.enabled = false;
        }

        ClearCandidates();
        SetMessage(string.Empty);
        searchPanel.Show();

        if (inputField != null)
        {
            StartCoroutine(ActivateInputFieldNextFrame());
        }
    }

    // 開くキー(F)の入力がそのまま入力欄に入らないよう、1フレーム待ってから入力欄を空にして有効化する
    private IEnumerator ActivateInputFieldNextFrame()
    {
        yield return null;

        if (!IsOpen) yield break;

        inputField.text = string.Empty;
        inputField.ActivateInputField();
    }

    /// <summary>検索画面を閉じる(ボタン用)。</summary>
    public void CloseSearch()
    {
        if (!IsOpen) return;

        if (inputField != null)
        {
            inputField.DeactivateInputField();
        }
        ClearCandidates();
        searchPanel.Hide();

        // フォーカス中はCameraFocusがCameraMoverの有効無効を管理しているので触らない
        if (cameraMover != null && (selectionManager == null || !selectionManager.IsSelected))
        {
            cameraMover.enabled = true;
        }
    }

    /// <summary>検索画面の開閉を切り替える(ボタン用)。</summary>
    public void ToggleSearch()
    {
        if (IsOpen)
        {
            CloseSearch();
        }
        else
        {
            OpenSearch();
        }
    }

    #endregion

    #region 検索

    /// <summary>「なまえ」と「さくしゃ」の検索モードを切り替える(ボタン用)。</summary>
    public void ToggleSearchMode()
    {
        searchMode = searchMode == SearchMode.Name ? SearchMode.Creator : SearchMode.Name;
        UpdateModeLabel();

        // 入力済みなら新しいモードで検索し直す
        if (inputField != null && !string.IsNullOrWhiteSpace(inputField.text))
        {
            Search(inputField.text);
        }

        if (inputField != null)
        {
            inputField.ActivateInputField();
        }
    }

    /// <summary>InputFieldのonSubmit用(Dynamic stringで設定する)。Enterキーで検索する。</summary>
    public void OnSearchSubmitted(string keyword)
    {
        Search(keyword);
    }

    /// <summary>検索ボタン用。</summary>
    public void OnSearchButtonClicked()
    {
        if (inputField == null) return;

        Search(inputField.text);
    }

    private void Search(string keyword)
    {
        ClearCandidates();

        if (string.IsNullOrWhiteSpace(keyword))
        {
            SetMessage("キーワードを入力してね");
            return;
        }

        var hits = new List<AnimalDataHolder>();
        foreach (var animal in illustrationManager.GetDisplayedAnimals())
        {
            string target = searchMode == SearchMode.Name ? animal.Data.animalName : animal.Data.creatorName;
            if (SearchTextNormalizer.Contains(target, keyword))
            {
                hits.Add(animal);
            }
        }

        Debug.Log($"動物検索({searchMode}):「{keyword}」 {hits.Count}件ヒット");

        if (hits.Count == 0)
        {
            SetMessage("見つかりませんでした");
        }
        else if (hits.Count == 1)
        {
            FocusAnimal(hits[0]);
        }
        else
        {
            ShowCandidates(hits);
            SetMessage($"{hits.Count}ひき見つかりました。えらんでね");
        }
    }

    // 検索画面を閉じてから選択する。逆順だとCloseSearchでCameraMoverが再び有効になり、フォーカスと競合する
    private void FocusAnimal(AnimalDataHolder animal)
    {
        if (animal == null) return;

        CloseSearch();
        selectionManager.SelectAnimal(animal);
    }

    #endregion

    #region 候補リスト

    private void ShowCandidates(List<AnimalDataHolder> animals)
    {
        if (candidatePrefab == null || candidateContainer == null)
        {
            Debug.LogError("候補リストのプレハブまたは親オブジェクトが設定されていません。");
            return;
        }

        foreach (var animal in animals)
        {
            var item = Instantiate(candidatePrefab, candidateContainer);
            item.Setup(animal, FocusAnimal);
            candidateItems.Add(item);
        }
    }

    private void ClearCandidates()
    {
        foreach (var item in candidateItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        candidateItems.Clear();
    }

    #endregion

    private void UpdateModeLabel()
    {
        if (modeLabel == null) return;

        modeLabel.text = searchMode == SearchMode.Name ? "なまえ" : "さくしゃ";
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    // 他のInputFieldに文字を入力している最中は、開くキーに反応しないようにする
    private bool IsTypingInOtherInputField()
    {
        var current = EventSystem.current;
        if (current == null || current.currentSelectedGameObject == null) return false;

        var field = current.currentSelectedGameObject.GetComponent<TMP_InputField>();
        return field != null && field.isFocused;
    }
}
