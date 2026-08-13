using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class AnimalsClickDetector : MonoBehaviour, IPointerClickHandler
{
    private CameraFocus cameraFocus;
    private bool isFocusing = false;
    
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
                UIManager.Instance.HideAnimalInfo();
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
                UIManager.Instance.ShowAnimalInfo(animalData);
            }
            Debug.Log($"クリックされた動物: {animalData.animalName}");
        }
    }
}