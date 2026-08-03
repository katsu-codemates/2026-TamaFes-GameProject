using UnityEngine;
using UnityEngine.EventSystems;

public class AnimalsClickDetector : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // クリックされた動物のデータを取得
            var animalData = GetComponent<AnimalData>();
            if (animalData != null)
            {
                GameObject mainCamera = GameObject.Find("Main Camera");
                CameraFocus cameraFocus = mainCamera.GetComponent<CameraFocus>();
                if (cameraFocus != null) cameraFocus.Focus(transform);
                // クリックされた動物の名前をUIに表示する処理を追加
            }
            Debug.Log($"クリックされた動物: {animalData.animalName}");
        }
    }
}