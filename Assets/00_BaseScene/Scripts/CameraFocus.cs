using UnityEngine;

public class CameraFocus : MonoBehaviour
{
    public Camera mainCamera;
    public Vector3 screenPosition;   // 対象が映ってほしいスクリーン座標

    public void Focus(Transform targetTransform)
    {
        Vector3 targetPos = targetTransform.position;
        Vector3 p = mainCamera.ScreenToWorldPoint(screenPosition);
        Vector3 offset = targetPos - p;
        this.transform.position += offset;
    }
}
