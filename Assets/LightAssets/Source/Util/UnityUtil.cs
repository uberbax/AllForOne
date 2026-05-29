using UnityEngine;

namespace Util
{
    public static class UnityUtil
    {
        public static GameObject QuickInstantiate(string what, Vector3 pos)
        {
            var r = Resources.Load(what);
            return GameObject.Instantiate(r, pos, Quaternion.identity) as GameObject;
        }
        
        public static T QuickInstantiate<T>(string what, Vector3 pos) where T : Component
        {
            return QuickInstantiate(what, pos).GetComponent<T>();
        }
        
        public static T QuickInstantiate<T>(string what) where T : Component
        {
            return QuickInstantiate(what, Vector3.zero).GetComponent<T>();
        }
        
    }
}