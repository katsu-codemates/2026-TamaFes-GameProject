using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 動物画像に対するマウス操作を担当する。
/// </summary>
[RequireComponent(typeof(AnimalDataHolder))]
public class AnimalsClickDetector : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("ホバー演出")]
    // [SerializeField] private Color animalOutlineColor = Color.white;
    // [SerializeField] private Vector2 outlineDistance = new Vector2(3f, 3f);

    private AnimalDataHolder animalDataHolder;
    private AnimalSelectionManager selectionManager;
    // // private Outline outline;

    private void Awake()
    {
        animalDataHolder = GetComponent<AnimalDataHolder>();
        // SetupOutline();
    }

    public void Initialize(AnimalSelectionManager selectionManager)
    {
        this.selectionManager = selectionManager;
    }

    // private void SetupOutline()
    // {
    //     outline = GetComponent<Outline>();
    //     if (outline == null) outline = gameObject.AddComponent<Outline>();

    //     outline.effectColor = animalOutlineColor;
    //     outline.effectDistance = outlineDistance;
    //     outline.enabled = false;
    // }

    // カーソルが重なったとき
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectionManager.IsSelected)
        {
            return;
        }

        // outline.enabled = true;
        Debug.Log($"カーソルが重なった{animalDataHolder.Data.animalName}");
    }

    // カーソルが離れたとき
    public void OnPointerExit(PointerEventData eventData)
    {
        // outline.enabled = false;
        Debug.Log($"カーソルが離れた{animalDataHolder.Data.animalName}");
    }

    // クリックされたとき
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (animalDataHolder == null || animalDataHolder.Data == null)
        {
            Debug.LogWarning($"動物データが設定されていません：{gameObject.name}");
            return;
        }

        // outline.enabled = false;

        selectionManager.SelectAnimal(animalDataHolder);
        Debug.Log($"クリックされた{animalDataHolder.Data.animalName}");
    }
}