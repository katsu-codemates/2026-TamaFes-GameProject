using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 検索で複数ヒットしたときに表示する候補リストの1行。
/// プレハブのButton.onClickにはInspectorからこのコンポーネントのOnClicked()を設定する。
/// </summary>
public class AnimalSearchCandidateItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI creatorText;
    [Header("サムネイル（任意）")]
    [SerializeField] private Image thumbnail;

    private AnimalDataHolder animal;
    private Action<AnimalDataHolder> onSelected;

    public void Setup(AnimalDataHolder animal, Action<AnimalDataHolder> onSelected)
    {
        this.animal = animal;
        this.onSelected = onSelected;

        var data = animal.Data;
        if (nameText != null)
        {
            nameText.text = data.animalName;
        }
        if (creatorText != null)
        {
            creatorText.text = string.IsNullOrEmpty(data.creatorName)
                ? "さくしゃ: ふめい"
                : "さくしゃ: " + data.creatorName;
        }
        if (thumbnail != null)
        {
            var renderer = animal.GetComponent<SpriteRenderer>();
            thumbnail.sprite = renderer != null ? renderer.sprite : null;
            thumbnail.preserveAspect = true;
        }
    }

    /// <summary>候補ボタン用。選ばれた動物を検索マネージャに通知する。</summary>
    public void OnClicked()
    {
        // 候補表示中に再読み込みなどで動物が破棄されていた場合は何もしない
        if (animal == null) return;

        onSelected?.Invoke(animal);
    }
}
