using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    [Header("Basic permanente")]
    [SerializeField] private string basicId = "Basic";
    [SerializeField] private AtaqueSO ataqueBasico;

    public event System.Action<string, int> OnChargesChanged;

    [Header("Ataques crafteables (por id)")]
    [SerializeField] private List<AtaqueSO> ataquesCrafteables = new();

    [Header("Estado")]
    [SerializeField] private string skillSeleccionadaId = "";

    private readonly Dictionary<string, int> cargas = new();
    private readonly Dictionary<string, AtaqueSO> ataquesPorId = new();

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

    public void SetSkillSeleccionada(string id) => skillSeleccionadaId = id;

    public void AddCharges(string id, int amount)
    {
        if (string.IsNullOrEmpty(id) || amount <= 0) return;

        if (!cargas.ContainsKey(id)) cargas[id] = 0;
        cargas[id] += amount;
        OnChargesChanged?.Invoke(id, cargas[id]);
    }

    public bool TryConsume(string id, int amount)
    {
        if (string.IsNullOrEmpty(id) || amount <= 0) return false;
        if (!cargas.TryGetValue(id, out int actual)) return false;
        if (actual < amount) return false;

        cargas[id] = actual - amount;
        OnChargesChanged?.Invoke(id, cargas[id]);
        return true;
    }

    public AtaqueSO GetAtaqueParaDisparar(AtaqueSO fallbackBasico)
    {
        if (string.IsNullOrEmpty(skillSeleccionadaId))
            return fallbackBasico;

        if (!ataquesPorId.TryGetValue(skillSeleccionadaId, out AtaqueSO ataque))
            return fallbackBasico;

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

    public AtaqueSO GetAtaqueSeleccionado()
    {
        if (string.IsNullOrEmpty(skillSeleccionadaId))
            return null;

        if (skillSeleccionadaId == basicId)
            return ataqueBasico;

        if (!ataquesPorId.TryGetValue(skillSeleccionadaId, out AtaqueSO ataque))
            return null;

        if (ataque.consumeCargas)
        {
            int costo = Mathf.Max(1, ataque.costoPorDisparo);
            if (!cargas.TryGetValue(skillSeleccionadaId, out int c) || c < costo)
                return null;
        }

        return ataque;
    }

    public string GetBasicId() => basicId;
}
