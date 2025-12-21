using UnityEngine;

public enum TipoEnemigo
{
    Normal,
    Fuego,
    Hiedra
}
public class VidaEnemigo : MonoBehaviour, IGolpeable
{
    [Header("Referencias")]
    [SerializeField] private Rigidbody2D rb2D;
    [SerializeField] private Animator animator;
    [SerializeField] private MovimientoEnemigo movimientoEnemigo;

    [Header("Tipo de Enemigo")]
    [SerializeField] private TipoEnemigo tipoEnemigo = TipoEnemigo.Normal;

    [Header("Vida")]
    [SerializeField] private int vidaMaxima;
    [SerializeField] private int vidaActual;

    [Header("Retroceso")]
    [SerializeField] private Vector2 fuerzaRetroceso;
    [SerializeField] private float tiempoMinimoRetroceso;

    private void Awake()
    {
        vidaActual = vidaMaxima;
    }

    public void TomarDaño(int cantidad, TipoDaño tipo, Transform sender)
    {
        float mult = GetMultiplicador(tipo);
        int dañoFinal = Mathf.RoundToInt(cantidad * mult);

        // Inmune / inútil: no retroceso, no animación de golpe
        if (dañoFinal <= 0) return;

        vidaActual = Mathf.Clamp(vidaActual - dañoFinal, 0, vidaMaxima);

        if (vidaActual == 0)
        {
            Destroy(gameObject);
            return;
        }

        Retroceso(sender);
    }

    private void Retroceso(Transform sender)
    {
        movimientoEnemigo.CambiarAEstadoOcupado(tiempoMinimoRetroceso, sender);

        Vector2 direccion = (transform.position - sender.position).normalized;

        Vector2 fuerza = new(Mathf.Sign(direccion.x) * fuerzaRetroceso.x, fuerzaRetroceso.y);

        rb2D.linearVelocity = Vector2.zero;

        rb2D.AddForce(fuerza, ForceMode2D.Impulse);

        animator.SetTrigger("Golpe");
    }

    private float GetMultiplicador(TipoDaño tipo)
    {
        switch (tipoEnemigo)
        {
            case TipoEnemigo.Normal:
                return 1f; // todos hacen daño normal

            case TipoEnemigo.Fuego:
                return (tipo == TipoDaño.Agua) ? 2f : 0f; // solo Agua daña

            case TipoEnemigo.Hiedra:
                return (tipo == TipoDaño.Metano) ? 1f : 0f; // solo Metano daña

            default:
                return 1f;
        }
    }
}
