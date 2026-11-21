using UnityEngine;

namespace GruaTorre
{
    public class DropTargetMarker : MonoBehaviour
    {
        public Transform gripperHead;
        public float rayStartOffsetY = -1.25f;
        public float maxRayDistance = 15f;
        public float surfaceHoverY = 0.03f;
        public LayerMask raycastMask = ~0;

        private Renderer _renderer;

        private void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
        }

        private void LateUpdate()
        {
            if (gripperHead == null) return;

            Vector3 origin = gripperHead.TransformPoint(new Vector3(0f, rayStartOffsetY, 0f));
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxRayDistance, raycastMask, QueryTriggerInteraction.Ignore))
            {
                if (_renderer != null) _renderer.enabled = true;
                transform.position = hit.point + Vector3.up * surfaceHoverY;
                transform.rotation = Quaternion.Euler(0f, gripperHead.eulerAngles.y, 0f);
            }
            else if (_renderer != null)
            {
                _renderer.enabled = false;
            }
        }
    }
}
