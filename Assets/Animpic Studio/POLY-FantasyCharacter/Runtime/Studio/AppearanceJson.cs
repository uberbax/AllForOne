using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Animpic.CharacterStudio
{
    // JsonUtility accepts null/absent nested objects as defaults in some Unity
    // versions. Parse the save's small schema explicitly before touching a scene.
    internal sealed class AppearanceJson
    {
        private string text; private int at;
        internal static Appearance Read(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 1048576) throw Bad();
            var reader = new AppearanceJson { text = json };
            var root = Obj(reader.Value(0)); reader.White();
            if (reader.at != json.Length || root.Count != 2 || Integer(root, "schemaVersion") != 1) throw Bad();
            var data = Obj(Get(root, "appearance"));
            if (data.Count != 16) throw Bad();
            var a = new Appearance {
                catalogId = String(data, "catalogId"), torso = Integer(data, "torso"), pants = Integer(data, "pants"),
                footwear = Integer(data, "footwear"), leftGlove = Integer(data, "leftGlove"), rightGlove = Integer(data, "rightGlove"),
                hair = Integer(data, "hair"), brows = Integer(data, "brows"), beard = Integer(data, "beard"), basePalette = Integer(data, "basePalette"),
                leftArmMode = (ArmMode)Integer(data, "leftArmMode"), rightArmMode = (ArmMode)Integer(data, "rightArmMode"),
                leftForearmStyle = Integer(data, "leftForearmStyle"), rightForearmStyle = Integer(data, "rightForearmStyle")
            };
            var extras = Arr(Get(data, "extras")); a.extras = new SlotChoice[extras.Count];
            for (int i = 0; i < extras.Count; i++) { var e = Obj(extras[i]); if (e.Count != 2) throw Bad(); a.extras[i] = new SlotChoice { id = String(e, "id"), option = Integer(e, "option") }; }
            var paints = Arr(Get(data, "paints")); a.paints = new PartPaint[paints.Count];
            for (int i = 0; i < paints.Count; i++)
            {
                var p = Obj(paints[i]); if (p.Count != 3) throw Bad(); var c = Obj(Get(p, "tint")); if (c.Count != 4) throw Bad();
                a.paints[i] = new PartPaint { partId = String(p, "partId"), palette = Integer(p, "palette"),
                    tint = new UnityEngine.Color(Channel(c, "r"), Channel(c, "g"), Channel(c, "b"), Channel(c, "a")) };
            }
            return a;
        }
        private static object Get(Dictionary<string, object> o, string key) { if (!o.TryGetValue(key, out var v)) throw Bad(); return v; }
        private static Dictionary<string, object> Obj(object v) { return v as Dictionary<string, object> ?? throw Bad(); }
        private static List<object> Arr(object v) { return v as List<object> ?? throw Bad(); }
        private static string String(Dictionary<string, object> o, string key) { return Get(o, key) as string ?? throw Bad(); }
        private static double Number(Dictionary<string, object> o, string key) { var v = Get(o, key); if (!(v is double)) throw Bad(); return (double)v; }
        private static float Channel(Dictionary<string, object> o, string key) { double d = Number(o, key); if (d < 0 || d > 1) throw Bad(); return (float)d; }
        private static int Integer(Dictionary<string, object> o, string key) { double d = Number(o, key); if (d < int.MinValue || d > int.MaxValue || d != Math.Truncate(d)) throw Bad(); return (int)d; }
        private static FormatException Bad() { return new FormatException("Invalid or incomplete appearance JSON; expected schemaVersion 1 and a complete appearance object."); }
        private void White() { while (at < text.Length && (text[at] == ' ' || text[at] == '\r' || text[at] == '\n' || text[at] == '\t')) at++; }
        private bool Eat(char c) { White(); if (at < text.Length && text[at] == c) { at++; return true; } return false; }
        private object Value(int depth)
        {
            White(); if (depth > 32 || at >= text.Length) throw Bad();
            if (text[at] == '"') return Quoted();
            if (Eat('{'))
            {
                var o = new Dictionary<string, object>(StringComparer.Ordinal);
                if (Eat('}')) return o;
                do { White(); string k = Quoted(); if (!Eat(':') || o.ContainsKey(k)) throw Bad(); o.Add(k, Value(depth + 1)); } while (Eat(','));
                if (!Eat('}')) throw Bad(); return o;
            }
            if (Eat('['))
            {
                var a = new List<object>(); if (Eat(']')) return a;
                do { a.Add(Value(depth + 1)); } while (Eat(','));
                if (!Eat(']')) throw Bad(); return a;
            }
            foreach (var literal in new[] { "null", "true", "false" })
                if (at + literal.Length <= text.Length && string.CompareOrdinal(text, at, literal, 0, literal.Length) == 0)
                { at += literal.Length; return literal == "null" ? null : (object)(literal == "true"); }
            int start = at;
            if (at < text.Length && text[at] == '-') at++;
            if (at >= text.Length || text[at] < '0' || text[at] > '9') throw Bad();
            if (text[at] == '0') at++; else while (at < text.Length && char.IsDigit(text[at])) at++;
            if (at < text.Length && text[at] == '.') { at++; int first = at; while (at < text.Length && char.IsDigit(text[at])) at++; if (first == at) throw Bad(); }
            if (at < text.Length && (text[at] == 'e' || text[at] == 'E'))
            { at++; if (at < text.Length && (text[at] == '+' || text[at] == '-')) at++; int first = at; while (at < text.Length && char.IsDigit(text[at])) at++; if (first == at) throw Bad(); }
            if (!double.TryParse(text.Substring(start, at - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) || double.IsNaN(n) || double.IsInfinity(n)) throw Bad();
            return n;
        }
        private string Quoted()
        {
            if (at >= text.Length || text[at++] != '"') throw Bad();
            var b = new StringBuilder();
            while (at < text.Length)
            {
                char c = text[at++]; if (c == '"') return b.ToString(); if (c < 32) throw Bad();
                if (c != '\\') { b.Append(c); continue; }
                if (at >= text.Length) throw Bad(); c = text[at++];
                switch (c)
                {
                    case '"': case '\\': case '/': b.Append(c); break;
                    case 'b': b.Append('\b'); break; case 'f': b.Append('\f'); break;
                    case 'n': b.Append('\n'); break; case 'r': b.Append('\r'); break; case 't': b.Append('\t'); break;
                    case 'u':
                        if (at + 4 > text.Length || !ushort.TryParse(text.Substring(at, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) throw Bad();
                        b.Append((char)value); at += 4; break;
                    default: throw Bad();
                }
            }
            throw Bad();
        }
    }
}