using UnityEngine;

/// <summary>
/// Adicione na Bruna. Aplica empurrões como um impulso que decai, SEM alterar o script de
/// movimentação existente (soma um Move extra por frame no CharacterController).
/// Se a personagem usar Rigidbody não-cinemático, aplica AddForce (Impulse).
/// </summary>
public class PlayerPushReceiver : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float decay = 8f;
    [Tooltip("Intervalo mínimo entre empurrões (evita sequência infinita de colisões).")]
    [SerializeField, Min(0f)] private float minInterval = 1f;

    private CharacterController cc;
    private Rigidbody rb;
    private Vector3 velocity;
    private float lastPushTime = -999f;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
    }

    public bool ApplyPush(Vector3 direction, float force)
    {
        if (Time.time - lastPushTime < minInterval) return false;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return false;
        direction.Normalize();
        lastPushTime = Time.time;

        if (cc != null && cc.enabled) velocity = direction * force;
        else if (rb != null && !rb.isKinematic) rb.AddForce(direction * force, ForceMode.VelocityChange);
        else transform.position += direction * (force * 0.15f);
        return true;
    }

    private void Update()
    {
        if (cc == null || !cc.enabled || velocity.sqrMagnitude < 0.01f) { velocity = Vector3.zero; return; }
        cc.Move(velocity * Time.deltaTime);
        velocity = Vector3.Lerp(velocity, Vector3.zero, decay * Time.deltaTime);
    }
}
