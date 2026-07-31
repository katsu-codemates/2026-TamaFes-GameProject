using UnityEngine;

public class Billboard : MonoBehaviour
{
    void Update()
    {
        Vector3 p = Camera.main.transform.position;
		p.y = transform.position.y;
		transform.LookAt (p);
    }
}
