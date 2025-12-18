using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    public struct Payload
    {
        public int damage;
        public float speed;
        public float lifetime;
        public Vector2 direction;
        public Transform owner;
        public LayerMask hitMask; // NUEVO: qué capas destruyen el proyectil
    }

    private Payload payload;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

    public void Init(Payload p)
    {
        payload = p;
        payload.direction = payload.direction.normalized;
        Destroy(gameObject, payload.lifetime);
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = payload.direction * payload.speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1) Ignorar al dueño y sus hijos (muy importante si tu collider está en un hijo)
        if (payload.owner != null && other.transform.root == payload.owner.root) return;

        // 2) Si golpea un IGolpeable, hace daño y se destruye
        if (other.TryGetComponent(out IGolpeable golpeable))
        {
            golpeable.TomarDaño(payload.damage, payload.owner);
            CombateJugador.NotifyHit();
            Destroy(gameObject);
            return;
        }

        // 3) Si NO es golpeable, solo destruye si está en capas de impacto (suelo/pared)
        if (((1 << other.gameObject.layer) & payload.hitMask.value) != 0)
        {
            Destroy(gameObject);
        }
    }
}