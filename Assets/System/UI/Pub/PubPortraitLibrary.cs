using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PubPortraitLibrary : ScriptableObject
{
    [Serializable] public struct Entry { public string id; public Sprite sprite; }
    public List<Entry> entries = new List<Entry>();
    private Dictionary<string, Sprite> lookup;
    public Sprite Get(string id)
    {
        if (lookup == null)
        {
            lookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            foreach (var entry in entries) if (!string.IsNullOrEmpty(entry.id)) lookup[entry.id] = entry.sprite;
        }
        return id != null && lookup.TryGetValue(id, out var sprite) ? sprite : null;
    }
    private void OnEnable() => lookup = null;
#if UNITY_EDITOR
    public void ClearLookup() => lookup = null;
#endif
}
