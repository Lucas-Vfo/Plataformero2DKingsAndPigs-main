using System;
using UnityEngine;

[Serializable]
public class Ataque
{
    [Header("Identidad")]
    public string id;                 // "Basic", "Agua", "Metano", etc.
    public string nombreAtaque;

    [Header("Combate")]
    public int cantidadDeDaño = 1;
    public Projectile proyectilPrefab;
    public float velocidadProyectil = 12f;
    public float vidaProyectil = 2f;

    [Header("Animación")]
    public string stringAnimacion;    // Trigger en Animator

    [Header("Costos")]
    public bool consumeCargas = false;
    public int costoPorDisparo = 1;
}

