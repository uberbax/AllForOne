using System;

namespace Animpic.FantasyCharacter
{
    public enum FemaleArmMode { Full = 0, Split = 1 }
    public enum FemaleHandCoverage { ReplacesHand = 0, BareHandRequired = 1 }

    /// <summary>Logical choices. Equipment may temporarily cover a saved arm choice.</summary>
    [Serializable]
    public sealed class FemaleCustomizationSelection
    {
        public int torso;
        public int pants;
        // 0 = barefoot; 1..6 = boots A..F.
        public int footwear;
        public FemaleArmMode leftArmMode;
        public FemaleArmMode rightArmMode;
        // -1 follows torso; otherwise 0..3 = A..D. Used only by split arms.
        public int leftForearmStyle = -1;
        public int rightForearmStyle = -1;
        // 0 = none; 1..4 = A..D. Gloves cover the forearm as well as the hand.
        public int leftGlove;
        public int rightGlove;
        // 0 = none; 1..3 = the catalogue's three variants.
        public int hair = 1;
        public int brows = 1;
        public int palette;

        public FemaleCustomizationSelection Copy()
        {
            return (FemaleCustomizationSelection)MemberwiseClone();
        }

        public bool Validate(out string error)
        {
            error = null;
            if (torso < 0 || torso > 3 || pants < 0 || pants > 3)
                error = "Torso and pants must be in the range 0..3.";
            else if (footwear < 0 || footwear > 6)
                error = "Footwear must be in the range 0..6.";
            else if (!ValidMode(leftArmMode) || !ValidMode(rightArmMode))
                error = "Arm mode must be Full or Split.";
            else if (leftForearmStyle < -1 || leftForearmStyle > 3 || rightForearmStyle < -1 || rightForearmStyle > 3)
                error = "Forearm style must be -1 (follow torso) or 0..3.";
            else if (leftGlove < 0 || leftGlove > 4 || rightGlove < 0 || rightGlove > 4)
                error = "Glove choices must be in the range 0..4.";
            else if (hair < 0 || hair > 3 || brows < 0 || brows > 3)
                error = "Hair and brows must be in the range 0..3.";
            else if (palette < 0 || palette > 5)
                error = "Palette must be in the range 0..5.";
            return error == null;
        }

        private static bool ValidMode(FemaleArmMode mode)
        {
            return mode == FemaleArmMode.Full || mode == FemaleArmMode.Split;
        }
    }
}
