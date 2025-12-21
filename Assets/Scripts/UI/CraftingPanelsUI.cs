using UnityEngine;

public class CraftingPanelsUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRecetas;
    [SerializeField] private GameObject panelAtomosReceta;
    [SerializeField] private CraftingBondBoardSingle board;

    public void OpenRecetas()
    {
        if (panelAtomosReceta != null) panelAtomosReceta.SetActive(false);
        if (panelRecetas != null) panelRecetas.SetActive(true);

        board?.ResetToTray(); // opcional: limpiar mesa al volver
    }
}
