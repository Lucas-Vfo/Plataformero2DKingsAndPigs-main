using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Ataque")]
public class AtaqueSO : ScriptableObject
{
    [Header("Identidad")]
    public string id;
    public string nombreAtaque;

    [Header("Tipo / Elemento")]
    public TipoDaño tipoDaño = TipoDaño.Basic;

    [Header("Combate")]
    public int cantidadDeDaño = 1;
    public Projectile proyectilPrefab;
    public float velocidadProyectil = 12f;
    public float vidaProyectil = 2f;

    [Header("Animación")]
    public string stringAnimacion;

    [Header("Costos")]
    public bool consumeCargas = false;
    public int costoPorDisparo = 1;

    [Header("Disparo")]
    public ModoAtaque modo = ModoAtaque.Proyectil;

    [Header("Curación")]
    public int curarCorazones = 1;
}
