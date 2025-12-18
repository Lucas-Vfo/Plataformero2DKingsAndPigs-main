using System;
using UnityEngine;

[Serializable]
public class RecipeDefinition
{
    public RecipeId id;
    public string nombreVisible;

    [Header("Átomos requeridos")]
    public AtomRequirement[] requeridos;
}