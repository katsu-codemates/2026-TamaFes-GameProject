using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class AnimalsClickDetector : MonoBehaviour, IPointerClickHandler
{
    private CameraFocus cameraFocus;
    private bool isFocusing = false;
    [SerializeField] TextMeshProUGUI animalName;
    private GameObject animalNameParent;
    private void Start()
    {
        var mainCamera = Camera.main;
        cameraFocus = mainCamera.GetComponent<CameraFocus>();
        if (cameraFocus == null)
        {
            Debug.LogError("CameraFocusコンポーネントが見つかりません。");
        }
    }

    void Update()
    {
        if (isFocusing)
        {
            if (Input.GetMouseButtonDown(1))
            {
                isFocusing = false;
                animalNameParent.SetActive(false); // 動物の名前を表示するUIを非表示
                cameraFocus.Unfocus();
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && !isFocusing)
        {
            // クリックされた動物のデータを取得
            var animalData = GetComponent<AnimalData>();
            if (animalData != null && cameraFocus != null)
            {
                cameraFocus.StartFocus(transform); // カメラを動物にフォーカス
                isFocusing = true;
                SetAnimalParameter(animalData); // 動物の名前をUIに表示
                animalNameParent = animalName.transform.parent.gameObject; // 親オブジェクトを取得
                animalNameParent.SetActive(true); // 動物の名前を表示するUIを有効化
            }
            Debug.Log($"クリックされた動物: {animalData.animalName}");
        }
    }

    private void SetAnimalParameter(AnimalData animalData)
    {
        animalName.text = animalData.animalName;
    }
}