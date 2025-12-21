using UnityEngine;
using UnityEngine.EventSystems;

public enum BondDir { Right, Left, Up, Down };

public class BondPoint : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] public BondDir dir;
    public bool IsOccupied { get; private set; }
    public AtomBond OccupantBond { get; private set; }

    public void SetBond(AtomBond bond)
    {
        IsOccupied = true;
        OccupantBond = bond;
    }

    public void ClearBond()
    {
        IsOccupied = false;
        OccupantBond = null;
    }

    private CraftingBondBoardSingle board;

    private void Awake()
    {
        board = GetComponentInParent<CraftingBondBoardSingle>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount != 2 || OccupantBond == null) return;

        // Resolver el board ahora (ya estás en MoleculeRoot)
        board ??= GetComponentInParent<CraftingBondBoardSingle>();

        board?.BreakBond(OccupantBond);
    }
}
