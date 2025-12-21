using System.Collections.Generic;
using UnityEngine;

public class BarraDeSkillsUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SkillManager skillManager;

    [Header("Slots (tamaño 10)")]
    [SerializeField] private HabilidadUI[] slots = new HabilidadUI[10];

    [Header("Slot0 básico")]
    [SerializeField] private Sprite iconoBasico;
    [SerializeField] private string basicId = "Basic";

    // Mapeos
    private readonly Dictionary<string, int> slotPorSkillId = new();
    private readonly string[] skillIdPorSlot = new string[10];

    private int selectedIndex = 0;

    private void Awake()
    {
        if (skillManager == null) skillManager = FindFirstObjectByType<SkillManager>();

        // 1) Limpia data y UI
        slotPorSkillId.Clear();
        for (int i = 0; i < skillIdPorSlot.Length; i++)
        {
            skillIdPorSlot[i] = null;
            slots[i]?.SetEmpty();
        }

        // 2) Fija slot0 como Basic (permanente)
        skillIdPorSlot[0] = basicId;
        slotPorSkillId[basicId] = 0;
        slots[0]?.SetSkill(basicId, iconoBasico, -1);

        // 3) Selecciona slot0 por defecto
        SelectIndex(0);
    }

    private void OnEnable()
    {
        if (skillManager == null) skillManager = FindFirstObjectByType<SkillManager>();
        if (skillManager != null) skillManager.OnChargesChanged += HandleChargesChanged;
    }

    private void OnDisable()
    {
        if (skillManager != null) skillManager.OnChargesChanged -= HandleChargesChanged;
    }

    private void Update()
    {
        if (TryGetHotbarIndex(out int idx))
            SelectIndex(idx);
    }

    private void HandleChargesChanged(string id, int newValue)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (!slotPorSkillId.TryGetValue(id, out int idx)) return;
        slots[idx]?.SetCantidad(newValue);
    }

    private bool TryGetHotbarIndex(out int index)
    {
        // 1..9 => 0..8, 0 => 9 (hotbar típica). [web:390][web:391]
        if (Input.GetKeyDown(KeyCode.Alpha1)) { index = 0; return true; }  
        if (Input.GetKeyDown(KeyCode.Alpha2)) { index = 1; return true; }   
        if (Input.GetKeyDown(KeyCode.Alpha3)) { index = 2; return true; }  
        if (Input.GetKeyDown(KeyCode.Alpha4)) { index = 3; return true; }  
        if (Input.GetKeyDown(KeyCode.Alpha5)) { index = 4; return true; }
        if (Input.GetKeyDown(KeyCode.Alpha6)) { index = 5; return true; }        
        if (Input.GetKeyDown(KeyCode.Alpha7)) { index = 6; return true; }
        if (Input.GetKeyDown(KeyCode.Alpha8)) { index = 7; return true; }
        if (Input.GetKeyDown(KeyCode.Alpha9)) { index = 8; return true; }
        if (Input.GetKeyDown(KeyCode.Alpha0)) { index = 9; return true; }

        index = -1;
        return false;
    }

    public void SelectIndex(int index)
    {
        if (index < 0 || index >= slots.Length) return;

        selectedIndex = index;

        // Apaga todos los highlights y enciende solo el seleccionado
        for (int i = 0; i < slots.Length; i++)
            slots[i]?.SetSelected(i == selectedIndex);

        // Selección lógica:
        // - slot vacío => "" (no dispara)
        // - slot0 => "Basic"
        // - skill con id => ese id
        string id = skillIdPorSlot[selectedIndex];
        if (skillManager != null)
            skillManager.SetSkillSeleccionada(string.IsNullOrEmpty(id) ? "" : id);
    }

    public void AddOrIncrement(string skillId, Sprite icon, int amount)
    {
        if (string.IsNullOrEmpty(skillId) || amount <= 0) return;

        if (slotPorSkillId.TryGetValue(skillId, out int existingIndex))
        {
            int charges = skillManager != null ? skillManager.GetCharges(skillId) : 0;
            slots[existingIndex]?.SetSkill(skillId, icon, charges);
            return;
        }

        int free = FindFirstEmptySlot();
        if (free < 0) return;

        skillIdPorSlot[free] = skillId;
        slotPorSkillId[skillId] = free;

        int newCharges = skillManager != null ? skillManager.GetCharges(skillId) : amount;
        slots[free]?.SetSkill(skillId, icon, newCharges);
    }

    private int FindFirstEmptySlot()
    {
        for (int i = 1; i < skillIdPorSlot.Length; i++) // reserva slot0
            if (string.IsNullOrEmpty(skillIdPorSlot[i]))
                return i;
        return -1;
    }
}
