using System.Collections;
using UnityEngine;

public class MovimientoJugador : MonoBehaviour
{
    private const string STRING_VELOCIDAD_HORIZONTAL = "VelocidadHorizontal";
    private const string STRING_VELOCIDAD_VERTICAL = "VelocidadVertical";
    private const string STRING_EN_SUELO = "EnSuelo";
    private const string STRING_ATERRIZAR = "Aterrizar";

    [Header("Referencias")]
    [SerializeField] private Rigidbody2D rb2D;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D colisionadorJugador;

    [Header("Movimiento Horizontal")]
    [SerializeField] private float velocidadMovimiento = 6f;
    private float entradaHorizontal;
    private float entradaVertical;

    [Header("Salto")]
    [SerializeField] private float fuerzaSalto = 12f;
    [SerializeField] private Transform controladorSuelo;
    [SerializeField] private Vector2 dimensionesCaja = new Vector2(0.6f, 0.1f);
    [SerializeField] private LayerMask capasSalto;
    [SerializeField] private bool sePuedeMoverEnElAire = true;
    [SerializeField] private float tiempoCaerPlataforma = 0.15f;

    private bool enSuelo;
    private bool entradaSalto;
    private bool estaCayendoPorPlataforma;

    private void Update()
    {
        entradaHorizontal = Input.GetAxisRaw("Horizontal");
        entradaVertical = Input.GetAxisRaw("Vertical");

        if (Input.GetButtonDown("Jump"))
        {
            entradaSalto = true;
        }

        ControlarAnimaciones();
    }

    private void FixedUpdate()
    {
        ActualizarEnSuelo();
        ControlarMovimientoHorizontal();
        ControlarSalto();
        entradaSalto = false;
    }

    private void ActualizarEnSuelo()
    {
        bool estabaEnElSuelo = enSuelo;
        enSuelo = false;

        Collider2D suelo = Physics2D.OverlapBox(controladorSuelo.position, dimensionesCaja, 0f, capasSalto);

        if (suelo != null)
        {
            enSuelo = true;
            if (!estabaEnElSuelo && rb2D.linearVelocity.y <= 0)
            {
                animator.SetTrigger(STRING_ATERRIZAR);
            }
        }
    }

    private void ControlarSalto()
    {
        if (!entradaSalto) return;
        if (!enSuelo) return;

        if (entradaVertical < 0)
        {
            if (!estaCayendoPorPlataforma)
                StartCoroutine(CaerPorPlataformasCoroutine());
        }
        else
        {
            Saltar();
        }
    }

    private void Saltar()
    {
        entradaSalto = false;
        rb2D.AddForce(new Vector2(0, fuerzaSalto), ForceMode2D.Impulse);
    }

    private IEnumerator CaerPorPlataformasCoroutine()
    {
        estaCayendoPorPlataforma = true;

        Collider2D[] objetosTocados = Physics2D.OverlapBoxAll(controladorSuelo.position, dimensionesCaja, 0f, capasSalto);

        foreach (Collider2D objeto in objetosTocados)
        {
            if (objeto != null && objeto.GetComponent<PlatformEffector2D>() != null)
            {
                Physics2D.IgnoreCollision(colisionadorJugador, objeto, true);
            }
        }

        yield return new WaitForSeconds(tiempoCaerPlataforma);

        foreach (Collider2D objeto in objetosTocados)
        {
            if (objeto != null && objeto.GetComponent<PlatformEffector2D>() != null)
            {
                Physics2D.IgnoreCollision(colisionadorJugador, objeto, false);
            }
        }

        estaCayendoPorPlataforma = false;
    }

    private void ControlarMovimientoHorizontal()
    {
        if (!enSuelo && !sePuedeMoverEnElAire) return;

        rb2D.linearVelocity = new Vector2(entradaHorizontal * velocidadMovimiento, rb2D.linearVelocity.y);

        if ((entradaHorizontal > 0 && !MirandoALaDerecha()) || (entradaHorizontal < 0 && MirandoALaDerecha()))
        {
            Girar();
        }
    }

    private void Girar()
    {
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }

    public bool MirandoALaDerecha()
    {
        return transform.localScale.x > 0;
    }

    private void ControlarAnimaciones()
    {
        animator.SetFloat(STRING_VELOCIDAD_HORIZONTAL, Mathf.Abs(rb2D.linearVelocity.x));
        animator.SetFloat(STRING_VELOCIDAD_VERTICAL, Mathf.Sign(rb2D.linearVelocity.y));
        animator.SetBool(STRING_EN_SUELO, enSuelo);
    }

    private void OnDrawGizmos()
    {
        if (controladorSuelo == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(controladorSuelo.position, dimensionesCaja);
    }
}
