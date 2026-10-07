using UnityEngine;
namespace Paperbound.Traveller
{
    [DisallowMultipleComponent]
    public sealed class TravellerBillboard : MonoBehaviour
    {
        public Camera targetCamera;
        void LateUpdate()
        {
            if (!targetCamera) targetCamera = Camera.main;
            if (targetCamera) transform.rotation = targetCamera.transform.rotation;
        }
    }
}
