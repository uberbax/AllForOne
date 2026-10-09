using System;
using System.Collections.Generic;
using UnityEngine;

namespace Animpic.CharacterStudio
{
    public enum ArmMode { Full = 0, Split = 1 }
    [Serializable] public sealed class SlotChoice { public string id; public int option; }
    [Serializable] public sealed class PartPaint { public string partId; public int palette = -1; public Color tint = Color.white; }
    [Serializable] public sealed class Appearance
    {
        public string catalogId;
        public int torso, pants, footwear, leftGlove, rightGlove, hair, brows, beard, basePalette;
        public ArmMode leftArmMode, rightArmMode;
        public int leftForearmStyle = -1, rightForearmStyle = -1;
        public SlotChoice[] extras = new SlotChoice[0];
        public PartPaint[] paints = new PartPaint[0];
        public Appearance Copy()
        {
            var copy = (Appearance)MemberwiseClone();
            copy.extras = extras == null ? null : Array.ConvertAll(extras, x => x == null ? null : new SlotChoice { id = x.id, option = x.option });
            copy.paints = paints == null ? null : Array.ConvertAll(paints, x => x == null ? null : new PartPaint { partId = x.partId, palette = x.palette, tint = x.tint });
            return copy;
        }
        public bool Validate(CharacterCatalog c, out string error)
        {
            if (c == null || catalogId != c.id) return CharacterCatalog.Fail("Appearance belongs to a different or missing catalog.", out error);
            if (!Index(torso, c.torsos.Length) || !Index(pants, c.pants.Length) || !Index(footwear, c.footwear.Length + 1) ||
                !Index(leftGlove, c.gloves.Length + 1) || !Index(rightGlove, c.gloves.Length + 1) || !Index(hair, c.hair.Length + 1) ||
                !Index(brows, c.brows.Length + 1) || !Index(beard, c.beards.Length + 1) || !Index(basePalette, 6) ||
                !Index(leftForearmStyle + 1, c.torsos.Length + 1) || !Index(rightForearmStyle + 1, c.torsos.Length + 1) ||
                !Index((int)leftArmMode, 2) || !Index((int)rightArmMode, 2))
                return CharacterCatalog.Fail("Appearance contains an out-of-range option or arm mode.", out error);
            if (extras == null || paints == null) return CharacterCatalog.Fail("Appearance extras and paints arrays must not be null.", out error);
            var slots = new Dictionary<string, ExtraSlot>(StringComparer.Ordinal);
            foreach (var slot in c.extras) slots.Add(slot.id, slot);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var choice in extras)
                if (choice == null || choice.id == null || !seen.Add(choice.id) || !slots.TryGetValue(choice.id, out var slot) || !Index(choice.option, slot.options.Length + 1))
                    return CharacterCatalog.Fail("Unknown, duplicate, or invalid extra slot choice.", out error);
            var partIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in c.parts) partIds.Add(p.id);
            seen.Clear();
            foreach (var paint in paints)
                if (paint == null || paint.partId == null || !partIds.Contains(paint.partId) || !seen.Add(paint.partId) ||
                    paint.palette < -1 || paint.palette > 5 || !Channel(paint.tint.r) || !Channel(paint.tint.g) || !Channel(paint.tint.b) || !Channel(paint.tint.a))
                    return CharacterCatalog.Fail("Unknown or duplicate painted part, invalid palette, or nonfinite/out-of-range tint.", out error);
            error = null; return true;
        }
        private static bool Index(int value, int count) { return value >= 0 && value < count; }
        private static bool Channel(float value) { return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 1; }
    }
    public sealed class VisiblePart { public string id, label; public SkinnedMeshRenderer renderer; }
}