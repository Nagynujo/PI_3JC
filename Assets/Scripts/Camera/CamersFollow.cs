using UnityEngine;

public class CamersFollow : MonoBehaviour
{
   
    public Transform target; // o personagem
    
    public Vector3 offset = new Vector3(0f, 6f, -6f);

    public float smoothSpeed = 0f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = smoothSpeed > 0f
            ? Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime)
            : desiredPosition;

        transform.LookAt(target);
    }
}
