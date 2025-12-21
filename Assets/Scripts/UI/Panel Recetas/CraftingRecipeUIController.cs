using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[Serializable]
public class RecipeButtonRef
{
    public RecipeId id;
    public Button button;
    public CanvasGroup canvasGroup;
}
public class CraftingRecipeUIController : MonoBehaviour
{
    [Header("Inventario")]
    [SerializeField] private InventarioAtoms inventario;

    [Header("Botones recetas (PanelRecetas)")]
    [SerializeField] private List<RecipeButtonRef> botonesRecetas = new();
    [SerializeField, Range(0f, 1f)] private float alphaDeshabilitado = 0.35f;

    [Header("Panels")]
    [SerializeField] private GameObject panelRecetas;
    [SerializeField] private GameObject panelAtomosReceta;

    [Header("Contenedor donde se instancian los slots")]
    [SerializeField] private Transform contentAtomosReceta;

    [Header("Board (Tabla)")]
    [SerializeField] private CraftingBondBoardSingle board;

    [Header("Prefab del slot (con UISlot)")]
    [SerializeField] private GameObject slotPrefab;

    [Header("Prefab del token UI")]
    [SerializeField] private GameObject atomTokenPrefab;

    [Header("Sprites por elemento")]
    [SerializeField] private Sprite spriteH;
    [SerializeField] private Sprite spriteO;
    [SerializeField] private Sprite spriteC;
    [SerializeField] private Sprite spriteN;

    [Header("Recetas")]
    [SerializeField] private List<RecipeDefinition> recetas = new();

    private RecipeDefinition selectedRecipe;

    private void Awake()
    {
        if (inventario == null)
        {
            ShowRecetas();
        }
    }
    private void OnEnable()
    {
        if (inventario == null) inventario = FindFirstObjectByType<InventarioAtoms>();
        if (inventario != null) inventario.OnCantidadCambio += HandleInvChanged;

        RefreshRecipeButtons();
    }

    private void OnDisable()
    {
        if (inventario != null) inventario.OnCantidadCambio -= HandleInvChanged;
    }

    private void HandleInvChanged(Elemento e, int nuevaCantidad) => RefreshRecipeButtons();

    private bool CanCraft(RecipeDefinition r)
    {
        if (r == null || inventario == null) return false;
        foreach (var req in r.requeridos)
            if (inventario.Get(req.elemento) < req.cantidad) return false;
        return true;
    }

    private void RefreshRecipeButtons()
    {
        foreach (var br in botonesRecetas)
        {
            var receta = recetas.Find(r => r.id == br.id);
            bool ok = CanCraft(receta);

            if (br.button != null) br.button.interactable = ok;

            if (br.canvasGroup != null)
            {
                br.canvasGroup.alpha = ok ? 1f : alphaDeshabilitado;
                br.canvasGroup.blocksRaycasts = ok;
                br.canvasGroup.interactable = ok;
            }
        }
    }

    // Conecta esto al OnClick() de cada botón (Agua/Metano/Etanol/Amoniaco)
    public void OnClickReceta(int recipeId)
    {
        var r = recetas.Find(x => x.id == (RecipeId)recipeId);
    if (!CanCraft(r)) return; // evita abrir el panel si no hay materiales
    ShowRecipe((RecipeId)recipeId);
    }

    public RecipeDefinition GetSelectedRecipe() => selectedRecipe;

    public Sprite GetSprite(Elemento e)
    {
        return e switch
        {
            Elemento.H => spriteH,
            Elemento.O => spriteO,
            Elemento.C => spriteC,
            Elemento.N => spriteN,
            _ => null
        };
    }

    public void ShowRecetas()
    {
        panelRecetas.SetActive(true);
        panelAtomosReceta.SetActive(false);

        Clear(contentAtomosReceta);
        selectedRecipe = null;
    }

    private void ShowRecipe(RecipeId id)
    {
        selectedRecipe = recetas.Find(r => r.id == id);
        if (selectedRecipe == null) return;

        panelRecetas.SetActive(false);
        panelAtomosReceta.SetActive(true);

        Clear(contentAtomosReceta);

        if (slotPrefab == null)
        {
            Debug.LogError("slotPrefab no asignado en CraftingRecipeUIController.", this);
            return;
        }

        if (atomTokenPrefab == null)
        {
            Debug.LogError("atomTokenPrefab no asignado en CraftingRecipeUIController.", this);
            return;
        }

        // 1) Crear slots + tokens según receta
        foreach (var req in selectedRecipe.requeridos)
        {
            for (int i = 0; i < req.cantidad; i++)
            {
                // Slot
                GameObject slotGO = Instantiate(slotPrefab, contentAtomosReceta, false);
                var slotRT = slotGO.GetComponent<RectTransform>();
                if (slotRT == null)
                {
                    Debug.LogError("slotPrefab debe tener RectTransform.", slotGO);
                    Destroy(slotGO);
                    return;
                }

                // Token dentro del slot
                GameObject tokenGO = Instantiate(atomTokenPrefab, slotRT, false);
                var token = tokenGO.GetComponent<UIAtomToken>();
                if (token == null)
                {
                    Debug.LogError("El atomTokenPrefab no tiene UIAtomToken.", tokenGO);
                    Destroy(tokenGO);
                    return;
                }

                token.Setup(req.elemento, GetSprite(req.elemento));

                // Centrar dentro del slot
                var tokenRT = (RectTransform)token.transform;
                tokenRT.anchoredPosition = Vector2.zero;
            }
        }

        // 2) Registrar "home" para volver a slots
        // Nota: este método debe ser actualizado para setear Home = cada slot (ver abajo).
        if (board != null)
            board.RegisterTrayTokensAsHome();
        else
            Debug.LogError("Board no asignado en CraftingRecipeUIController", this);
    }

    private void Clear(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
