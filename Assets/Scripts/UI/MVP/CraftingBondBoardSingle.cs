using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CraftingBondBoardSingle : MonoBehaviour
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private BarraDeSkillsUI barraSkills;
    [SerializeField] private Sprite iconoAgua;
    [SerializeField] private Sprite iconoMetano;
    [SerializeField] private Sprite iconoAmoniaco;
    [SerializeField] private Sprite iconoEtanol;

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

    private ChemicalBondRules rules = new ChemicalBondRules();

    private Canvas _canvas;

    private void Awake()
    {
        if (inventario == null) inventario = FindFirstObjectByType<InventarioAtoms>();
        _canvas = GetComponentInParent<Canvas>();
        SetCrear(false);
    }

    // Cámara UI correcta según renderMode:
    // - ScreenSpaceOverlay => cam = null
    // - ScreenSpaceCamera / WorldSpace => cam = canvas.worldCamera
    private Camera UICamera
    {
        get
        {
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null) return null;

            return _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        }
    }

    private bool IsTokenInsideBoard(UIAtomToken token)
    {
        if (token == null || boardArea == null) return false;

        var rt = (RectTransform)token.transform;

        // Validar por el centro del token en pantalla, no por el mouse/touch
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(UICamera, rt.position);
        return RectTransformUtility.RectangleContainsScreenPoint(boardArea, screenPoint, UICamera);
    }

    public void RegisterTrayTokensAsHome()
    {
        if (contentAtomosReceta == null) return;

        // Cada hijo del content ahora es un SLOT, no un token.
        for (int i = 0; i < contentAtomosReceta.childCount; i++)
        {
            var slot = contentAtomosReceta.GetChild(i) as RectTransform;
            if (slot == null) continue;

            // Toma el primer token dentro del slot (si existe), incluyendo inactivos. [web:173]
            var token = slot.GetComponentInChildren<UIAtomToken>(true);
            if (token == null) continue;

            token.SetBoard(this);

            // Home = slot, posición home = centro del slot.
            // Con slots, no necesitas guardar una posición "home" distinta de (0,0).
            token.SetHome(slot, Vector2.zero);
        }

        Revalidate();
    }
    private void ReparentKeepScreenPosition(RectTransform child, RectTransform newParent)
    {
        // Canvas Overlay => camera null es correcto. [web:168]
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, child.position);
        child.SetParent(newParent, false); // mantiene escala local del UI (no “mundo”) [web:168]
        RectTransformUtility.ScreenPointToLocalPointInRectangle(newParent, screen, null, out var local);
        child.anchoredPosition = local;
        child.localScale = Vector3.one; // evita acumulación de escala entre padres [web:216]
    }

    public void TryPlaceOrReturn(UIAtomToken token, PointerEventData eventData)
    {
        if (token == null) return;

        // 1) Decide por la posición real del token (no por eventData.position)
        bool inside = IsTokenInsideBoard(token);
        if (!inside)
        {
            // Si estaba en la mesa y está unido a otros => volver TODO (tu regla de bloque)
            bool tokenEstabaEnMesa = (token.transform.parent == moleculeRoot);
            if (tokenEstabaEnMesa)
            {
                var group = GetConnectedGroup(token);
                if (group != null && group.Count > 1)
                {
                    ResetToTray();      // vuelve todo el bloque a Home
                    return;
                }
            }

            // Si NO es bloque => vuelve solo este token a su slot
            token.ReturnHome();
            Revalidate();
            return;
        }

        //Restuara tamaño
        ReparentKeepScreenPosition((RectTransform)token.transform, moleculeRoot);

        // Intenta snap a un bond compatible cercano
        TrySnap(token);

        // CLAMP AQUÍ: aplica al token ya en su posición final tras el snap
        ClampInsideBoard((RectTransform)token.transform);

        // Si luego del snap algo queda fuera del board => reset total
        if (!AllTokensInsideBoard())
            ResetToTray();

        Revalidate();
    }

    public void PlaceGroupOnBoard(IEnumerable<UIAtomToken> group)
    {
        if (group == null) return;
        foreach (var t in group)
            t.transform.SetParent(moleculeRoot, false);
    }
    private bool AllTokensInsideBoard()
    {
        if (moleculeRoot == null || boardArea == null) return false;

        var all = moleculeRoot.GetComponentsInChildren<UIAtomToken>(true);
        foreach (var t in all)
        {
            var rt = (RectTransform)t.transform;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(UICamera, rt.position);

            if (!RectTransformUtility.RectangleContainsScreenPoint(boardArea, screenPoint, UICamera))
                return false;
        }
        return true;
    }

    public void ResetToTray()
    {
        BreakAllBonds();

        // 1) Devuelve tokens que estén en la mesa a su slot "home" (si tienen).
        //    Esto no depende de posiciones; siempre vuelve al centro del slot.
        var all = moleculeRoot.GetComponentsInChildren<UIAtomToken>(true);
        foreach (var t in all)
            t.ReturnHome();

        // 2) Opcional: re-registra homes por si cambió la receta (slots/tokens recreados)
        //    Esto hace el sistema más tolerante a reconstrucciones del UI.
        RegisterTrayTokensAsHome();

        Revalidate();
    }

    // ===== Bonds =====

    private bool AreOpposite(BondDir a, BondDir b) =>
        (a == BondDir.Right && b == BondDir.Left) ||
        (a == BondDir.Left && b == BondDir.Right) ||
        (a == BondDir.Up && b == BondDir.Down) ||
        (a == BondDir.Down && b == BondDir.Up);

    public bool TrySnap(UIAtomToken token)
    {
        if (token == null) return false;
        if (!token.HasFreeBondPoint()) return false;
        if (moleculeRoot == null) return false;

        // Asegura que está en la mesa antes de calcular
        if (token.transform.parent != moleculeRoot)
            token.transform.SetParent(moleculeRoot, false);

        var all = moleculeRoot.GetComponentsInChildren<UIAtomToken>(true);

        BondPoint bestA = null;
        BondPoint bestB = null;
        UIAtomToken bestOther = null;
        float bestDist = float.MaxValue;

        var recipe = recipesUI != null ? recipesUI.GetSelectedRecipe() : null;

        foreach (var other in all)
        {
            if (other == token) continue;
            if (!other.HasFreeBondPoint()) continue;

            if (!rules.CanBond(recipe, token, other))
                continue;

            foreach (var a in token.FreePoints())
                foreach (var b in other.FreePoints())
                {
                    if (!AreOpposite(a.dir, b.dir))
                        continue;

                    // Distancia en el espacio local del board (moleculeRoot)
                    Vector2 aLocal = (Vector2)moleculeRoot.InverseTransformPoint(a.transform.position);
                    Vector2 bLocal = (Vector2)moleculeRoot.InverseTransformPoint(b.transform.position);
                    float d = Vector2.Distance(aLocal, bLocal);

                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestA = a;
                        bestB = b;
                        bestOther = other;
                    }
                }
        }

        if (bestOther == null || bestDist > snapDistance)
            return false;

        // Ajuste en anchoredPosition (espacio UI local del padre)
        var tokenRT = (RectTransform)token.transform;

        Vector2 bestALocal = (Vector2)moleculeRoot.InverseTransformPoint(bestA.transform.position);
        Vector2 bestBLocal = (Vector2)moleculeRoot.InverseTransformPoint(bestB.transform.position);

        Vector2 deltaLocal = bestBLocal - bestALocal;

        Vector2 overlapLocal = bestA.dir switch
        {
            BondDir.Right => Vector2.right * bondOverlapPx,
            BondDir.Left => Vector2.left * bondOverlapPx,
            BondDir.Up => Vector2.up * bondOverlapPx,
            BondDir.Down => Vector2.down * bondOverlapPx,
            _ => Vector2.zero
        };

        tokenRT.anchoredPosition += deltaLocal + overlapLocal;

        var bond = new AtomBond { a = token, b = bestOther, aPoint = bestA, bPoint = bestB };
        bonds.Add(bond);

        bestA.SetBond(bond);
        bestB.SetBond(bond);
        token.AddBond(bond);
        bestOther.AddBond(bond);

        return true;
    }

    private void ClampInsideBoard(RectTransform tokenRT)
    {
        Vector3[] tCorners = new Vector3[4];
        Vector3[] bCorners = new Vector3[4];
        tokenRT.GetWorldCorners(tCorners);
        boardArea.GetWorldCorners(bCorners);

        float left = bCorners[0].x;
        float right = bCorners[2].x;
        float bottom = bCorners[0].y;
        float top = bCorners[2].y;

        Vector3 delta = Vector3.zero;
        if (tCorners[0].x < left) delta.x += left - tCorners[0].x;
        if (tCorners[2].x > right) delta.x -= tCorners[2].x - right;
        if (tCorners[0].y < bottom) delta.y += bottom - tCorners[0].y;
        if (tCorners[2].y > top) delta.y -= tCorners[2].y - top;

        tokenRT.position += delta;
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
        var recipe = recipesUI != null ? recipesUI.GetSelectedRecipe() : null;
        var tokens = moleculeRoot.GetComponentsInChildren<UIAtomToken>(true);

        bool ok = recipe != null
               && RecipeCountsMatch(recipe)
               && rules.BondsWithinValence(tokens)
               && rules.BondsSaturated(tokens)   // opcional pero recomendado para “molécula completa”
               && IsConnected();

        SetCrear(ok);
    }

    private bool BondsWithinValence()
    {
        if (moleculeRoot == null) return false;
        var all = moleculeRoot.GetComponentsInChildren<UIAtomToken>(true);
        return all.All(t => t.BondCunt <= t.MaxBonds);
    }

    private bool RecipeCountsMatch(RecipeDefinition receta)
    {
        if (receta == null || moleculeRoot == null) return false;

        var all = moleculeRoot.GetComponentsInChildren<UIAtomToken>(true);
        var counts = all.GroupBy(t => t.Elemento).ToDictionary(g => g.Key, g => g.Count());

        foreach (var req in receta.requeridos)
            if (!counts.TryGetValue(req.elemento, out int v) || v != req.cantidad)
                return false;

        foreach (var kv in counts)
        {
            int required = receta.requeridos
                .Where(r => r.elemento == kv.Key)
                .Select(r => r.cantidad)
                .FirstOrDefault();

            if (kv.Value != required) return false;
        }

        return true;
    }

    private bool IsConnected()
    {
        if (moleculeRoot == null) return false;

        var nodes = moleculeRoot.GetComponentsInChildren<UIAtomToken>(true);
        if (nodes.Length == 0) return false;

        var adj = nodes.ToDictionary(n => n, _ => new List<UIAtomToken>());
        foreach (var b in bonds)
        {
            if (b.a == null || b.b == null) continue;

            if (adj.TryGetValue(b.a, out var listA) &&
                adj.TryGetValue(b.b, out var listB))
            {
                listA.Add(b.b);
                listB.Add(b.a);
            }
            // else: ignorar este bond porque apunta a nodos que no están en moleculeRoot
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
    private Sprite GetSkillIcon(RecipeId id) => id switch
    {
        RecipeId.Agua => iconoAgua,
        RecipeId.Metano => iconoMetano,
        RecipeId.Amoniaco => iconoAmoniaco,
        RecipeId.Etanol => iconoEtanol,
        _ => null
    };

    public void OnClickCrear()
    {
        var receta = recipesUI != null ? recipesUI.GetSelectedRecipe() : null;
        if (receta == null) return;

        foreach (var req in receta.requeridos)
            if (inventario.Get(req.elemento) < req.cantidad) return;

        foreach (var req in receta.requeridos)
            if (!inventario.Consume(req.elemento, req.cantidad)) return;

        string skillId = receta.id.ToString(); // "Agua", "Metano", "Etanol", "Amoniaco"

        int add = Mathf.Max(1, receta.cargasPorCrafteo);

        if (skillManager != null)
            skillManager.AddCharges(skillId, add);

        if (barraSkills != null)
        {
            // Ideal: que el icono venga desde Ataque (o una tabla)
            Sprite icon = GetSkillIcon(receta.id);

            // Refresca mostrando el TOTAL actual, no "add"
            int total = skillManager != null ? skillManager.GetCharges(skillId) : add;
            barraSkills.AddOrIncrement(skillId, icon, add); // tu método puede seguir así si internamente consulta total
                                                            // Si prefieres, crea barraSkills.Refresh(skillId,total) y úsalo aquí.
        }

        ResetToTray();
    }

    public List<UIAtomToken> GetConnectedGroup(UIAtomToken start)
    {
        if (start == null || moleculeRoot == null) return new List<UIAtomToken>();

        var nodes = moleculeRoot.GetComponentsInChildren<UIAtomToken>(true);
        var adj = nodes.ToDictionary(n => n, _ => new List<UIAtomToken>());

        foreach (var b in bonds)
        {
            if (b.a == null || b.b == null) continue;

            if (adj.TryGetValue(b.a, out var listA) &&
                adj.TryGetValue(b.b, out var listB))
            {
                listA.Add(b.b);
                listB.Add(b.a);
            }
            // else: ignorar este bond porque apunta a nodos que no están en moleculeRoot
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
