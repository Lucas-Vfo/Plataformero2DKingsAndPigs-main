using TMPro;
using UnityEngine;

public class UIInventarioCrafting : MonoBehaviour
{
    [SerializeField] private InventarioAtoms inventario;

    [Header("Textos contador")]
    [Header("UI Hidrógeno")]
    [SerializeField] private TMP_Text numeroH;

    [Header("UI Oxígeno")]
    [SerializeField] private TMP_Text numeroO;

    [Header("UI Carbono")]
    [SerializeField] private TMP_Text numeroC;

    [Header("UI Nitrógeno")]
    [SerializeField] private TMP_Text numeroN;

    private void Awake()
    {
        // Asignar inventario desde Inspector (Jugador).
        if (inventario == null)
            inventario = FindFirstObjectByType<InventarioAtoms>();
    }

    private void OnEnable()
    {
        if (inventario != null)
            inventario.OnCantidadCambio += HandleCambio;

        RefreshAll();
    }

    private void OnDisable()
    {
        if (inventario != null)
            inventario.OnCantidadCambio -= HandleCambio;
    }

    public void RefreshAll()
    {
        if (inventario == null) return;

        SetText(numeroH, inventario.Get(Elemento.H));
        SetText(numeroO, inventario.Get(Elemento.O));
        SetText(numeroC, inventario.Get(Elemento.C));
        SetText(numeroN, inventario.Get(Elemento.N));
    }

    private void HandleCambio(Elemento e, int nuevaCantidad)
    {
        switch (e)
        {
            case Elemento.H: SetText(numeroH, nuevaCantidad); break;
            case Elemento.O: SetText(numeroO, nuevaCantidad); break;
            case Elemento.C: SetText(numeroC, nuevaCantidad); break;
            case Elemento.N: SetText(numeroN, nuevaCantidad); break;
        }
    }

    private void SetText(TMP_Text t, int value)
    {
        if (t != null) t.text = value.ToString();
    }
}