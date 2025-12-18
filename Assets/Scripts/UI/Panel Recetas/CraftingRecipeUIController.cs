using System.Collections.Generic;
using UnityEngine;

public class CraftingRecipeUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelRecetas;
    [SerializeField] private GameObject panelAtomosReceta;

    [Header("Contenedor donde se instancian los átomos")]
    [SerializeField] private Transform contentAtomosReceta;

    [Header("Board (Tabla)")]
    [SerializeField] private CraftingBondBoardSingle board;

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
        ShowRecetas();
    }

    // Conecta esto al OnClick() de cada botón (Agua/Metano/Etanol/Amoniaco)
    public void OnClickReceta(int recipeId)
    {
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
        Debug.Log($"Receta {id}: creando tokens...");
        RecipeDefinition receta = recetas.Find(r => r.id == id);
        if (receta == null) return;

        panelRecetas.SetActive(false);
        panelAtomosReceta.SetActive(true);

        Clear(contentAtomosReceta);

        foreach (var req in receta.requeridos)
        {
            for (int i = 0; i < req.cantidad; i++)
            {
                GameObject go = Instantiate(atomTokenPrefab, contentAtomosReceta, false);
                var token = go.GetComponent<UIAtomToken>();
                if (token == null)
                {
                    Debug.LogError("El prefab no tiene UIAtomToken.", go);
                    Destroy(go);
                    return;
                }
                token.Setup(req.elemento, GetSprite(req.elemento));
            }
            Debug.Log($"Total tokens en content: {contentAtomosReceta.childCount}");
        }
        // Después de instanciar todos los tokens:
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
