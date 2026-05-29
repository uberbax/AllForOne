using UnityEditor;
using UnityEngine;

namespace Util.Editor
{
    public static class UnityMenuItems
    {
        [MenuItem("Tools/ClearPP")]
        public static void ClearPP()
        {
            PlayerPrefs.DeleteAll();
        }
    }
}