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
                if (mainCamera != null)
                {
                    CameraFocus cameraFocus = mainCamera.GetComponent<CameraFocus>();
                    if (cameraFocus != null)
                    {
                        cameraFocus.Focus(transform);
                    }
                }
                Debug.Log($"クリックされた動物: {animalData.animalName}");
            }
        }

    }
}
