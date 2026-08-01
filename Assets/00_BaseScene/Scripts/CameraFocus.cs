using UnityEngine;

public class CameraFocus : MonoBehaviour
{
    private Transform target; // カメラが注視する対象のTransform
    public Vector3 offset;   // カメラと対象の位置の相対位置
    private Vector3 targetPosition; // カメラが向かう位置

    public void Focus(Transform targetTransform)
    {
        target = targetTransform;
        targetPosition = target.position + offset;
        transform.position = targetPosition;
        transform.LookAt(target.position);
    }
}
