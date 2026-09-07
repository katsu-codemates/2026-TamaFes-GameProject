using UnityEngine;

public class CameraFocus : MonoBehaviour
{
    public Camera mainCamera;
    public Vector3 screenPosition;   // 対象が映ってほしいスクリーン座標

    private CameraMover cameraMover;
    private Transform targetTransform;
    private Vector3 originalPosition;
    private bool IsFocusing => targetTransform != null;

    private void Awake()
    {
        cameraMover = mainCamera.GetComponent<CameraMover>();
        if (cameraMover == null)
        {
            Debug.LogError("CameraMoverコンポーネントが見つかりません。");
        }
    }
    private void LateUpdate()
    {
        if (!IsFocusing)
        {
            return;
        }

        UpdateCameraPosition();
    }

    public void StartFocus(Transform target)
    {
        targetTransform = target;
        originalPosition = transform.position;
        cameraMover.enabled = false;
    }

    private void UpdateCameraPosition()
    {
        Vector3 targetPosition = targetTransform.position;
        Vector3 screenWorldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        Vector3 offset = targetPosition - screenWorldPosition;
        transform.position = originalPosition + offset;
    }

    public void Unfocus()
    {
        this.transform.position = originalPosition;
        cameraMover.enabled = true;
        targetTransform = null;
    }
}
