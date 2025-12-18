using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    [Header("Ataques crafteables (por id)")]
    [SerializeField] private List<Ataque> ataquesCrafteables = new();

    [Header("Estado")]
    [SerializeField] private string skillSeleccionadaId = ""; // "Agua", "Metano", etc.

    private readonly Dictionary<string, int> cargas = new();
    private readonly Dictionary<string, Ataque> ataquesPorId = new();

    private void Awake()
    {
        ataquesPorId.Clear();
        foreach (var a in ataquesCrafteables)
        {
            if (a == null || string.IsNullOrEmpty(a.id)) continue;
            ataquesPorId[a.id] = a;

            if (!cargas.ContainsKey(a.id))
                cargas[a.id] = 0;
        }
    }

    public void SetSkillSeleccionada(string id)
    {
        skillSeleccionadaId = id;
    }

    public void AddCharges(string id, int amount)
    {
        if (string.IsNullOrEmpty(id) || amount <= 0) return;

        if (!cargas.ContainsKey(id)) cargas[id] = 0;
        cargas[id] += amount;
    }

    public bool TryConsume(string id, int amount)
    {
        if (string.IsNullOrEmpty(id) || amount <= 0) return false;
        if (!cargas.TryGetValue(id, out int actual)) return false;
        if (actual < amount) return false;

        cargas[id] = actual - amount;
        return true;
    }

    public Ataque GetAtaqueParaDisparar(Ataque fallbackBasico)
    {
        if (string.IsNullOrEmpty(skillSeleccionadaId))
            return fallbackBasico;

        if (!ataquesPorId.TryGetValue(skillSeleccionadaId, out Ataque ataque))
            return fallbackBasico;

        // Si consume cargas, exige al menos 1 (o el costo que definas)
        if (ataque.consumeCargas)
        {
            int costo = Mathf.Max(1, ataque.costoPorDisparo);
            if (!cargas.TryGetValue(skillSeleccionadaId, out int c) || c < costo)
                return fallbackBasico;
        }

        return ataque;
    }

    public int GetCharges(string id) => cargas.TryGetValue(id, out int c) ? c : 0;
    public string GetSelectedId() => skillSeleccionadaId;
}
