using UnityEngine;

public class CameraFocus : MonoBehaviour
{
    public Camera mainCamera;
    public Vector3 screenPosition;   // 対象が映ってほしいスクリーン座標

    private CameraMover cameraMover;
    private Transform targetTransform;
    private Vector3 originalPosition;
    private bool isFocusing = false;

    private void Start()
    {
        cameraMover = mainCamera.GetComponent<CameraMover>();
        if (cameraMover == null)
        {
            Debug.LogError("CameraMoverコンポーネントが見つかりません。");
        }
    }
    private void Update()
    {
        if (isFocusing && targetTransform != null)
        {
            Focus(targetTransform);
        }
    }
    public void Focus(Transform targetTransform)
    {
        Vector3 targetPos = targetTransform.position;
        Vector3 p = mainCamera.ScreenToWorldPoint(screenPosition);
        Vector3 offset = targetPos - p;
        this.transform.position += offset;
    }

    public void StartFocus(Transform targetTransform)
    {
        this.targetTransform = targetTransform;
        originalPosition = this.transform.position;
        cameraMover.enabled = false;
        isFocusing = true;
    }
    public void Unfocus()
    {
        this.transform.position = originalPosition;
        cameraMover.enabled = true;
        isFocusing = false;
        targetTransform = null;
    }
}
