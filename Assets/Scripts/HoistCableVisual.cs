using UnityEngine;

namespace GruaTorre
{
    public class HoistCableVisual : MonoBehaviour
    {
        public Transform topAnchor;
        public Transform bottomAnchor;
        public float cableRadius = 0.04f;

        private void LateUpdate()
        {
            if (topAnchor == null || bottomAnchor == null) return;

            Vector3 a = topAnchor.position;
            Vector3 b = bottomAnchor.position;
            Vector3 delta = b - a;
            float dist = delta.magnitude;

            if (dist < 0.01f)
            {
                transform.localScale = Vector3.zero;
                return;
            }

            transform.position = (a + b) * 0.5f;
            transform.up = delta / dist;
            transform.localScale = new Vector3(cableRadius * 2f, dist * 0.5f, cableRadius * 2f);
        }
    }
}
