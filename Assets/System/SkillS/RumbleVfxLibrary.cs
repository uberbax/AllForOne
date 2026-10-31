using System;
using UnityEngine;

[CreateAssetMenu(menuName="AllForOne/Rumble VFX Library")]
public sealed class RumbleVfxLibrary : ScriptableObject
{
    public const string ResourcePath="RumbleSkills/VfxLibrary";
    [Serializable]public sealed class Style
    {
        public string id;
        public GameObject projectile,impact,area,cast,beam,heal,shield;
    }
    public Style[] styles=Array.Empty<Style>();
    public enum Stage { Projectile,Impact,Area,Cast,Beam,Heal,Shield }
    public GameObject Get(string id,Stage stage)
    {
        foreach(var style in styles)
        {
            if(style.id!=id)continue;
            switch(stage)
            {
                case Stage.Projectile:return style.projectile;
                case Stage.Impact:return style.impact;
                case Stage.Area:return style.area;
                case Stage.Cast:return style.cast;
                case Stage.Beam:return style.beam;
                case Stage.Heal:return style.heal;
                case Stage.Shield:return style.shield;
            }
        }
        return null;
    }
}
