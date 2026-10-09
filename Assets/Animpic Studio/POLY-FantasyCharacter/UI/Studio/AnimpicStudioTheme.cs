using UnityEngine;

namespace Animpic.CharacterStudio
{
    public static class AnimpicStudioTheme
    {
        public static readonly Color Background = Hex(0x071018);
        public static readonly Color Panel = Hex(0x0F1B23);
        public static readonly Color Surface = Hex(0x14222B);
        public static readonly Color Border = Hex(0x263B47);
        public static readonly Color Text = Hex(0xF3F7F8);
        public static readonly Color Muted = Hex(0x80939C);
        public static readonly Color Accent = Hex(0x1EC3D3);
        public static readonly Color AccentBlue = Hex(0x087FFF);
        public static readonly Color AccentHover = Hex(0x42D7E3);
        public static readonly Color GradientMiddle = Hex(0x00B9FF);
        public static readonly Color GradientEnd = Hex(0x18D5D1);
        private static Font body, medium, heading;
        public static Font BodyFont { get { return body ? body : body = LoadFont("Inter-Regular"); } }
        public static Font MediumFont { get { return medium ? medium : medium = LoadFont("Inter-Medium"); } }
        public static Font HeadingFont { get { return heading ? heading : heading = LoadFont("Manrope-Bold"); } }
        public static Sprite Wordmark { get { return Resources.Load<Sprite>("AnimpicStudio/Brand/animpic-wordmark-light"); } }
        public static Sprite Mark { get { return Resources.Load<Sprite>("AnimpicStudio/Brand/animpic-mark"); } }
        private static Font LoadFont(string name)
        {
            var font = Resources.Load<Font>("AnimpicStudio/Fonts/" + name);
            return font ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        private static Color Hex(uint value) { return new Color32((byte)(value >> 16), (byte)(value >> 8), (byte)value, 255); }
    }
}
