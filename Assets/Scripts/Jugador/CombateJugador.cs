using System;
using System.Collections.Generic;
using UnityEngine;

public class CombateJugador : MonoBehaviour
{
    public static Action JugadorGolpeoUnObjetivo;

    [Header("Referencias")]
    [SerializeField] private Animator animator;
    [SerializeField] private MovimientoJugador movimientoJugador; // para saber dirección
    [SerializeField] private Transform puntoDisparo;
    [SerializeField] private SkillManager skillManager;

    [Header("Ataques disponibles (fallback/básicos)")]
    [SerializeField] private Ataque ataqueBasico; // dispara cuando no hay skill seleccionada/cargas

    [Header("Ataque")]
    [SerializeField] private float tiempoEntreAtaques = 0.2f;
    [SerializeField] private float tiempoUltimoAtaque;

    [Header("Buffer de entrada")]
    [SerializeField] private float tiempoBufferEntrada = 0.25f;
    private readonly Queue<float> bufferEntradas = new();

    [SerializeField] private Collider2D colliderJugador;
    [SerializeField] private LayerMask capasQueDestruyenProyectil; // Ground/Wall

    [SerializeField] private Camera cam;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (colliderJugador == null) colliderJugador = GetComponent<Collider2D>();
        if (movimientoJugador == null) movimientoJugador = GetComponent<MovimientoJugador>();
        if (skillManager == null) skillManager = GetComponent<SkillManager>();
    }
    private Vector2 GetMouseWorld()
    {
        Vector3 m = Input.mousePosition;
        m.z = 0f; // En 2D ortográfico no importa, pero no estorba [web:249]
        return cam.ScreenToWorldPoint(m);
    }

    private void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            //Debug.Log("Disparo OK");
            bufferEntradas.Enqueue(Time.time);
        }

        while (bufferEntradas.Count > 0 && Time.time > bufferEntradas.Peek() + tiempoBufferEntrada)
        {
            bufferEntradas.Dequeue();
        }

        if (bufferEntradas.Count > 0)
        {
            IntentarDisparar();
        }
    }

    private void IntentarDisparar()
    {
        if (Time.time < tiempoUltimoAtaque + tiempoEntreAtaques) return;

        bufferEntradas.Dequeue();
        Disparar();
    }

    private void Disparar()
    {
        tiempoUltimoAtaque = Time.time;

        // 1) Elegir ataque actual: skill si existe y tiene cargas; si no, básico
        Ataque ataqueActual = skillManager != null ? skillManager.GetAtaqueParaDisparar(ataqueBasico) : ataqueBasico;

        if (ataqueActual == null)
        {
            //Debug.LogError("ataqueActual es NULL");
            return;
        }
        if (ataqueActual.proyectilPrefab == null)
        {
            //Debug.LogError("proyectilPrefab es NULL (revisa ataqueBasico en el Inspector del Jugador en escena)");
            return;
        }
        if (puntoDisparo == null)
        {
            //Debug.LogError("puntoDisparo es NULL (revisa referencia en Inspector)");
            return;
        }

        //Debug.Log($"DISPARANDO: {ataqueActual.id} desde {puntoDisparo.position}");

        // 2) Consumir cargas si corresponde
        if (skillManager != null && ataqueActual.consumeCargas)
        {
            if (!skillManager.TryConsume(ataqueActual.id, ataqueActual.costoPorDisparo))
                ataqueActual = ataqueBasico; // fallback si se quedó sin cargas
        }

        // 3) Animación
        if (!string.IsNullOrEmpty(ataqueActual.stringAnimacion))
            animator.SetTrigger(ataqueActual.stringAnimacion);

        // 4) Dirección según orientación del jugador
        Vector2 mouseWorld = GetMouseWorld();
        Vector2 dir = (mouseWorld - (Vector2)puntoDisparo.position).normalized;

        // Evita NaN si el mouse está exactamente sobre el puntoDisparo
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.right;

        // 5) Instanciar proyectil
        Projectile p = Instantiate(ataqueActual.proyectilPrefab, puntoDisparo.position, Quaternion.identity);
        //Debug.Log($"Instanciado: {p.name}", p.gameObject);
        p.Init(new Projectile.Payload
        {
            damage = ataqueActual.cantidadDeDaño,
            speed = ataqueActual.velocidadProyectil,
            lifetime = ataqueActual.vidaProyectil,
            direction = dir,
            owner = transform,
            hitMask = capasQueDestruyenProyectil
        });

        var projectileCollider = p.GetComponent<Collider2D>();
        if (projectileCollider != null)
        {
            foreach (var c in GetComponentsInChildren<Collider2D>())
            {
                Physics2D.IgnoreCollision(projectileCollider, c, true); // Ignora jugador completo [web:232]
            }
        }
    }

    // Llamado por Projectile cuando golpea algo
    public static void NotifyHit()
    {
        JugadorGolpeoUnObjetivo?.Invoke();
    }
}