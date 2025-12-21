using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIAtomToken : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Elemento Elemento { get; private set; }

    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI label;

    [Header("Drag")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform dragRoot;

    [Header("Bond points")]
    [SerializeField] private BondPoint[] bondPoints;

    private RectTransform rt;
    private CanvasGroup cg;

    // Home (recipiente)
    private RectTransform homeParent;
    private Vector2 homePos;

    // Drag state
    private Transform startParent;
    private Vector2 startPos;
    private Vector2 pointerOffset; // para drag normal

    // Drag Group
    private List<UIAtomToken> dragGroup;
    private Vector2 dragLeaderStartAnchoredPos;
    private Dictionary<UIAtomToken, Vector2> groupStartAnchoredPos;
    private Dictionary<UIAtomToken, Vector2> groupOffset;
    private bool draggingGroup;
    private Vector2 dragStartPointerScreen;
    private Vector2 leaderStartAnchored;

    // Board
    private CraftingBondBoardSingle board;

    // Bonds
    private readonly List<AtomBond> bonds = new();
    public int BondCunt => bonds.Count;

    public int MaxBonds => Elemento switch
    {
        Elemento.H => 1,
        Elemento.O => 2,
        Elemento.N => 3,
        Elemento.C => 4,
        _ => 0
    };

    private void Awake()
    {
        rt = (RectTransform)transform;
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        if (icon == null) icon = GetComponent<Image>(); // root
        if (icon == null) icon = GetComponentInChildren<Image>(true);

        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        // Buscar DragLayer en escena (hijo del Canvas)
        if (dragRoot == null && canvas != null)
        {
            var t = canvas.transform.Find("DragLayer");
            if (t != null) dragRoot = t as RectTransform;
            else dragRoot = canvas.transform as RectTransform; // fallback
        }
    }

    public void SetBoard(CraftingBondBoardSingle b) => board = b;

    public void SetHome(RectTransform parent, Vector2 anchoredPos)
    {
        homeParent = parent;
        homePos = anchoredPos;
    }

    public void ReturnHome()
    {
        board?.BreakAllBondsOf(this);

        transform.SetParent(homeParent, false);
        rt.anchoredPosition = homePos;

        rt.localScale = Vector3.one;
    }
    public void ReturnHomeToSlot(RectTransform slot)
    {
        if (slot == null) return;
        board?.BreakAllBondsOf(this);

        transform.SetParent(slot, false);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
    }
    public void Setup(Elemento elemento, Sprite sprite)
    {
        Elemento = elemento;

        if (sprite == null)
            Debug.LogError($"Sprite null para elemento {elemento}", gameObject);

        icon.sprite = sprite; // asignación UI.Image.sprite [web:389]
        icon.preserveAspect = true;
        // icon.SetNativeSize();
        //((RectTransform)transform).sizeDelta = new Vector2(64, 64);
        if (label != null) label.text = elemento.ToString();
    }
    public bool HasFreeBondPoint() => BondCunt < MaxBonds;

    public IEnumerable<BondPoint> FreePoints()
    {
        foreach (var p in bondPoints)
            if (p != null && !p.IsOccupied) yield return p;
    }
    private Vector2 PointerLocal(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
        dragRoot, e.position, null, out Vector2 localPoint); // Overlay => cam null [web:521]
        return localPoint;
    }

    public void AddBond(AtomBond bond) => bonds.Add(bond);

    public void RemoveBond(AtomBond bond) => bonds.Remove(bond);

    public void OnBeginDrag(PointerEventData eventData)
    {
        startParent = transform.parent;
        startPos = rt.anchoredPosition;

        cg.blocksRaycasts = false;

        bool wasOnBoard = (board != null && startParent == board.MoleculeRoot);

        // 1) Si está en la mesa, calcula grupo ANTES de reparentar
        if (wasOnBoard && board != null)
        {
            var group = board.GetConnectedGroup(this);
            if (group != null && group.Count > 1)
            {
                draggingGroup = true;
                dragGroup = group;

                // ahora sí reparenta todo al dragRoot (idealmente conservando world pos)
                foreach (var t in dragGroup)
                    t.transform.SetParent(dragRoot, true);

                Vector2 pointer = PointerLocal(eventData);
                groupOffset = new Dictionary<UIAtomToken, Vector2>();
                foreach (var t in dragGroup)
                {
                    var trt = (RectTransform)t.transform;
                    groupOffset[t] = trt.anchoredPosition - pointer;
                }
                return;
            }
        }

        // 2) Drag normal
        if (dragRoot != null)
            transform.SetParent(dragRoot, true);

        pointerOffset = rt.anchoredPosition - PointerLocal(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pointer = PointerLocal(eventData);

        if (draggingGroup && dragGroup != null && groupOffset != null)
        {
            foreach (var t in dragGroup)
            {
                var trt = (RectTransform)t.transform;
                trt.anchoredPosition = pointer + groupOffset[t];
            }
            return;
        }

        rt.anchoredPosition = pointer + pointerOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        cg.blocksRaycasts = true;

        // Si fue grupal, primero devuelve todo a la mesa (moleculeRoot) para que no quede nada colgando
        if (draggingGroup && dragGroup != null)
        {
            foreach (var t in dragGroup)
                t.transform.SetParent(board.MoleculeRoot, true);

            draggingGroup = false;
            dragGroup = null;
            groupOffset = null;

            board.TryPlaceOrReturn(this, eventData);
            return;
        }

        // Drag normal
        if (board != null)
        {
            board.TryPlaceOrReturn(this, eventData);
            return;
        }

        transform.SetParent(startParent, false);
        rt.anchoredPosition = startPos;
    }

    public bool HasHomeSlot() => homeParent != null;

    public RectTransform GetHomeSlot() => homeParent;

    public bool IsHomeSlot(RectTransform slot)
    {
        if (slot == null || homeParent == null) return false;
        return ReferenceEquals(homeParent, slot);
    }

}