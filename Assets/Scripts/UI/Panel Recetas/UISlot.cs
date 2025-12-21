using UnityEngine;
using UnityEngine.EventSystems;

public class UISlot : MonoBehaviour, IDropHandler
{
    [Header("Opcional: referencia al board para revalidar")]
    [SerializeField] private CraftingBondBoardSingle board;

    [Header("Reglas del slot")]
    [SerializeField] private bool acceptTokens = true;

    private RectTransform rt;

    private void Awake()
    {
        rt = (RectTransform)transform;
        if (board == null) board = GetComponentInParent<CraftingBondBoardSingle>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!acceptTokens) return;
        if (eventData == null) return;

        // El objeto arrastrado viene aquí. [web:139]
        var draggedGO = eventData.pointerDrag;
        if (draggedGO == null) return;

        var token = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<UIAtomToken>() : null;
        if (token == null) return;

        // Si este slot no es el "home" del token, rechaza
        if (!token.IsHomeSlot((RectTransform)transform))
        {
            token.ReturnHome();
            return;
        }

        token.ReturnHomeToSlot((RectTransform)transform);

        board?.Revalidate();
    }
}
