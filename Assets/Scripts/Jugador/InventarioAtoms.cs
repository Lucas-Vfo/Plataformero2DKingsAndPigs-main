using System;
using System.Collections.Generic;
using UnityEngine;

public enum Elemento { H, O, C, N }

public class InventarioAtoms : MonoBehaviour
{
    public event Action<Elemento, int> OnCantidadCambio;

    private readonly Dictionary<Elemento, int> cantidades = new();

    private void Awake()
    {
        foreach (Elemento e in Enum.GetValues(typeof(Elemento)))
            cantidades[e] = 0;
    }

    public int Get(Elemento e) => cantidades.TryGetValue(e, out int v) ? v : 0;

    public void Add(Elemento e, int amount)
    {
        if (amount <= 0) return;
        cantidades[e] += amount;
        OnCantidadCambio?.Invoke(e, cantidades[e]);
    }
    public bool Consume(Elemento e, int amount)
    {
        if (amount <= 0) return true;
        if (Get(e) < amount) return false;

        // Ajusta esta línea según cómo almacenas tus cantidades:
        // Si usas Dictionary<Elemento,int> cantidades:
        cantidades[e] -= amount;

        OnCantidadCambio?.Invoke(e, cantidades[e]);
        return true;
    }
}