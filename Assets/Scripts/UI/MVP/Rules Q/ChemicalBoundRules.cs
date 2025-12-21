using System.Collections.Generic;
using System.Linq;

public sealed class ChemicalBondRules
{
    public bool CanBond(RecipeDefinition recipe, UIAtomToken a, UIAtomToken b)
    {
        if (a == null || b == null) return false;
        if (a == b) return false;

        // Regla general: no exceder capacidad (valencia simple).
        if (a.BondCunt >= a.MaxBonds) return false;
        if (b.BondCunt >= b.MaxBonds) return false;

        // Bloqueo opcional general (si no quieres H-H en ninguna receta)
        //if (a.Elemento == Elemento.H && b.Elemento == Elemento.H) return false;

        // Sin receta -> modo libre (solo valencia)
        if (recipe == null) return true;

        // Restricción por receta (MVP)
        return recipe.id switch
        {
            RecipeId.Agua => IsPair(a, b, Elemento.H, Elemento.O),
            RecipeId.Metano => IsPair(a, b, Elemento.C, Elemento.H),
            RecipeId.Amoniaco => IsPair(a, b, Elemento.N, Elemento.H),

            // Etanol: permite esqueleto y saturación con H
            RecipeId.Etanol => IsPair(a, b, Elemento.C, Elemento.C)
                            || IsPair(a, b, Elemento.C, Elemento.O)
                            || IsPair(a, b, Elemento.C, Elemento.H)
                            || IsPair(a, b, Elemento.O, Elemento.H),

            _ => true
        };
    }

    public bool BondsWithinValence(IEnumerable<UIAtomToken> tokens)
    {
        if (tokens == null) return false;
        var list = tokens as IList<UIAtomToken> ?? tokens.ToList();
        return list.Count > 0 && list.All(t => t != null && t.BondCunt <= t.MaxBonds);
    }

    public bool BondsSaturated(IEnumerable<UIAtomToken> tokens)
    {
        if (tokens == null) return false;
        var list = tokens as IList<UIAtomToken> ?? tokens.ToList();
        return list.Count > 0 && list.All(t => t != null && t.BondCunt == t.MaxBonds);
    }

    private static bool IsPair(UIAtomToken a, UIAtomToken b, Elemento x, Elemento y)
        => (a.Elemento == x && b.Elemento == y) || (a.Elemento == y && b.Elemento == x);
}