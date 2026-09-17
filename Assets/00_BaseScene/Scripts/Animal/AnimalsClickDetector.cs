using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 動物画像に対するマウス操作を担当する。
/// </summary>
[RequireComponent(typeof(AnimalDataHolder))]
[RequireComponent(typeof(SpriteRenderer))]
public class AnimalsClickDetector : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    // [Header("ホバー演出")]
    // [SerializeField] private Color animalOutlineColor = Color.yellow;
    // [SerializeField] private float outlineScale = 1.15f;

    private AnimalDataHolder animalDataHolder;
    private AnimalSelectionManager selectionManager;
    private SpriteRenderer mainRenderer;
    // private SpriteRenderer outlineRenderer;

    private void Awake()
    {
        animalDataHolder = GetComponent<AnimalDataHolder>();
        mainRenderer = GetComponent<SpriteRenderer>();
        // SetupOutline();
    }

    public void Initialize(AnimalSelectionManager selectionManager)
    {
        this.selectionManager = selectionManager;
    }

    // 本体のSpriteRendererを一回り大きく複製した子オブジェクトを本体の奥に重ね、輪郭線のように見せる
    // private void SetupOutline()
    // {
    //     var outlineObject = new GameObject("OutlineSprite");
    //     var outlineTransform = outlineObject.transform;
    //     outlineTransform.SetParent(transform, false);
    //     // ビルボード回転後のローカルZ-方向（カメラから遠ざかる向き）に少し下げて本体の奥に描画されるようにする
    //     outlineTransform.localPosition = new Vector3(0f, 0f, -0.05f);
    //     outlineTransform.localScale = Vector3.one * outlineScale;

    //     outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();
    //     outlineRenderer.color = animalOutlineColor;
    //     outlineRenderer.sortingOrder = mainRenderer.sortingOrder - 1;
    //     outlineObject.SetActive(false);
    // }

    // カーソルが重なったとき
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectionManager.IsSelected)
        {
            return;
        }

        // outlineRenderer.sprite = mainRenderer.sprite;
        // outlineRenderer.gameObject.SetActive(true);
        Debug.Log($"カーソルが重なった{animalDataHolder.Data.animalName}");
    }

    // カーソルが離れたとき
    public void OnPointerExit(PointerEventData eventData)
    {
        // outlineRenderer.gameObject.SetActive(false);
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

        // outlineRenderer.gameObject.SetActive(false);

        selectionManager.SelectAnimal(animalDataHolder);
        Debug.Log($"クリックされた{animalDataHolder.Data.animalName}");
    }
}