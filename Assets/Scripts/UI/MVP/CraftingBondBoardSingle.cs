using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CraftingBondBoardSingle : MonoBehaviour
{
    [Header("Límites/Contenedores")]
    [SerializeField] private RectTransform boardArea;      // marco
    [SerializeField] private RectTransform moleculeRoot;   // átomos dentro de la tabla

    [Header("Bandeja")]
    [SerializeField] private RectTransform contentAtomosReceta;

    [Header("UI")]
    [SerializeField] private Button botonCrear;

    [Header("Receta actual")]
    [SerializeField] private CraftingRecipeUIController recipesUI;

    [Header("Inventario")]
    [SerializeField] private InventarioAtoms inventario;

    [Header("Bonding")]
    [SerializeField] private float snapDistance = 45f;
    [SerializeField] private float bondOverlapPx = 8f;

    private readonly List<AtomBond> bonds = new();
    public RectTransform MoleculeRoot => moleculeRoot;

    private void Awake()
    {
        if (inventario == null) inventario = FindFirstObjectByType<InventarioAtoms>();
        SetCrear(false);
    }

    public void RegisterTrayTokensAsHome()
    {
        if (contentAtomosReceta == null) return;

        for (int i = 0; i < contentAtomosReceta.childCount; i++)
        {
            var token = contentAtomosReceta.GetChild(i).GetComponent<UIAtomToken>();
            if (token == null) continue;

            token.SetBoard(this);
            token.SetHome(contentAtomosReceta, ((RectTransform)token.transform).anchoredPosition);
        }

        Revalidate();
    }

    public void TryPlaceOrReturn(UIAtomToken token, PointerEventData eventData)
    {
        if (token == null) return;

        bool inside = RectTransformUtility.RectangleContainsScreenPoint(boardArea, eventData.position, null);
        if (!inside)
        {
            token.ReturnHome();
            Revalidate();
            return;
        }

        token.transform.SetParent(moleculeRoot, false);

        TrySnap(token);

        if (!AllTokensInsideBoard())
            ResetToTray();

        Revalidate();
    }

    public void PlaceGroupOnBoard(IEnumerable<UIAtomToken> group)
    {
        foreach (var t in group)
            t.transform.SetParent(moleculeRoot, false);
    }


    private bool AllTokensInsideBoard()
    {
        var all = moleculeRoot.GetComponentsInChildren<UIAtomToken>(true);
        foreach (var t in all)
        {
            var rt = (RectTransform)t.transform;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, rt.position);
            if (!RectTransformUtility.RectangleContainsScreenPoint(boardArea, screenPoint, null))
                return false;
        }
        return true;
    }

    public void ResetToTray()
    {
        BreakAllBonds();

        var all = moleculeRoot.GetComponentsInChildren<UIAtomToken>(true);
        foreach (var t in all)
            t.ReturnHome();

        Revalidate();
    }

    // ===== Bonds =====

    private bool AreOpposite(BondDir a, BondDir b) =>
        (a == BondDir.Right && b == BondDir.Left) ||
        (a == BondDir.Left && b == BondDir.Right) ||
        (a == BondDir.Up && b == BondDir.Down) ||
        (a == BondDir.Down && b == BondDir.Up);

    private Vector3 DirToVector(BondDir d) => d switch
    {
        BondDir.Right => Vector3.right,
        BondDir.Left => Vector3.left,
        BondDir.Up => Vector3.up,
        BondDir.Down => Vector3.down,
        _ => Vector3.zero
    };

    public bool TrySnap(UIAtomToken token)
    {
        if (token == null) return false;
        if (!token.HasFreeBondPoint()) return false;

        var all = moleculeRoot.GetComponentsInChildren<UIAtomToken>(true);

        BondPoint bestA = null;
        BondPoint bestB = null;
        UIAtomToken bestOther = null;
        float bestDist = float.MaxValue;

        foreach (var other in all)
        {
            if (other == token) continue;
            if (!other.HasFreeBondPoint()) continue;

            foreach (var a in token.FreePoints())
                foreach (var b in other.FreePoints())
                {
                    if (!AreOpposite(a.dir, b.dir))
                        continue;

                    float d = Vector2.Distance(((RectTransform)a.transform).position,
                                               ((RectTransform)b.transform).position);

                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestA = a;
                        bestB = b;
                        bestOther = other;
                    }
                }
        }

        if (bestOther == null || bestDist > snapDistance) return false;

        Vector3 delta = ((RectTransform)bestB.transform).position - ((RectTransform)bestA.transform).position;
        Vector3 overlap = DirToVector(bestA.dir) * bondOverlapPx;

        token.transform.position += delta + overlap;

        var bond = new AtomBond { a = token, b = bestOther, aPoint = bestA, bPoint = bestB };
        bonds.Add(bond);

        bestA.SetBond(bond);
        bestB.SetBond(bond);
        token.AddBond(bond);
        bestOther.AddBond(bond);

        return true;
    }

    public void BreakBond(AtomBond bond)
    {
        if (bond == null) return;

        bond.aPoint?.ClearBond();
        bond.bPoint?.ClearBond();
        bond.a?.RemoveBond(bond);
        bond.b?.RemoveBond(bond);

        bonds.Remove(bond);
        Revalidate();
    }

    public void BreakAllBondsOf(UIAtomToken token)
    {
        if (token == null) return;

        var copy = new List<AtomBond>(bonds);
        foreach (var b in copy)
            if (b.a == token || b.b == token)
                BreakBond(b);
    }

    public void BreakAllBonds()
    {
        var copy = new List<AtomBond>(bonds);
        foreach (var b in copy) BreakBond(b);
    }

    // ===== Validación + Crear =====

    public void Revalidate()
    {
        var receta = recipesUI != null ? recipesUI.GetSelectedRecipe() : null;
        bool ok = receta != null && RecipeCountsMatch(receta) && BondsWithinValence() && IsConnected();
        SetCrear(ok);
    }

    private bool BondsWithinValence()
    {
        var all = moleculeRoot.GetComponentsInChildren<UIAtomToken>(true);
        return all.All(t => t.BondCunt <= t.MaxBonds);
    }

    private bool RecipeCountsMatch(RecipeDefinition receta)
    {
        var all = moleculeRoot.GetComponentsInChildren<UIAtomToken>(true);
        var counts = all.GroupBy(t => t.Elemento).ToDictionary(g => g.Key, g => g.Count());

        foreach (var req in receta.requeridos)
            if (!counts.TryGetValue(req.elemento, out int v) || v != req.cantidad) return false;

        foreach (var kv in counts)
        {
            int required = receta.requeridos.Where(r => r.elemento == kv.Key).Select(r => r.cantidad).FirstOrDefault();
            if (kv.Value != required) return false;
        }

        return true;
    }

    private bool IsConnected()
    {
        var nodes = moleculeRoot.GetComponentsInChildren<UIAtomToken>(true);
        if (nodes.Length == 0) return false;

        var adj = nodes.ToDictionary(n => n, _ => new List<UIAtomToken>());
        foreach (var b in bonds)
        {
            if (b.a != null && b.b != null)
            {
                adj[b.a].Add(b.b);
                adj[b.b].Add(b.a);
            }
        }

        var visited = new HashSet<UIAtomToken>();
        var stack = new Stack<UIAtomToken>();
        stack.Push(nodes[0]);
        visited.Add(nodes[0]);

        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            foreach (var nb in adj[cur])
                if (visited.Add(nb)) stack.Push(nb);
        }

        return visited.Count == nodes.Length;
    }

    private void SetCrear(bool v)
    {
        if (botonCrear != null) botonCrear.interactable = v;
    }

    public void OnClickCrear()
    {
        var receta = recipesUI != null ? recipesUI.GetSelectedRecipe() : null;
        if (receta == null) return;

        foreach (var req in receta.requeridos)
            if (inventario.Get(req.elemento) < req.cantidad) return;

        foreach (var req in receta.requeridos)
            if (!inventario.Consume(req.elemento, req.cantidad)) return;

        ResetToTray();
    }

    public List<UIAtomToken> GetConnectedGroup(UIAtomToken start)
    {
        var nodes = moleculeRoot.GetComponentsInChildren<UIAtomToken>(true);
        var adj = nodes.ToDictionary(n => n, _ => new List<UIAtomToken>());

        foreach (var b in bonds)
        {
            if (b.a != null && b.b != null)
            {
                adj[b.a].Add(b.b);
                adj[b.b].Add(b.a);
            }
        }

        var group = new List<UIAtomToken>();
        var stack = new Stack<UIAtomToken>();
        var visited = new HashSet<UIAtomToken>();

        stack.Push(start);
        visited.Add(start);

        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            group.Add(cur);
            foreach (var nb in adj[cur])
                if (visited.Add(nb)) stack.Push(nb);
        }
        return group;
    }
}
