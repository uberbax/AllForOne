using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Plague Tree Catalog")]
public sealed class PlagueTreeCatalog : ScriptableObject
{
    [Serializable] public struct Replacement { public Sprite living; public Sprite dead; }
    public List<Replacement> replacements = new List<Replacement>();
}
