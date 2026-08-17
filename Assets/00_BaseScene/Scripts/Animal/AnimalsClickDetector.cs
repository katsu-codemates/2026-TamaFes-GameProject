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
            var holder = GetComponent<AnimalDataHolder>();
            if (holder != null && holder.Data != null && cameraFocus != null)
            {
                cameraFocus.StartFocus(transform); // カメラを動物にフォーカス
                isFocusing = true;
                UIManager.Instance.ShowAnimalInfo(holder.Data);
            }
            Debug.Log($"クリックされた動物: {holder?.Data?.animalName}");
        }
    }
}