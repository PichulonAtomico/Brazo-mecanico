using System.Collections.Generic;
using UnityEngine;

namespace GruaTorre
{
    [RequireComponent(typeof(BoxCollider))]
    public class CargoDropZone : MonoBehaviour
    {
        [Tooltip("Velocidad máxima (m/s) para considerar que una carga quedó apoyada y estable.")]
        public float maxSettledSpeed = 0.25f;

        [Tooltip("Altura máxima del centro de la carga respecto a la plataforma para contarla como depositada (permite apilar).")]
        public float maxStackedHeight = 3.5f;

        [Tooltip("Lista de cargas activas de la tarea.")]
        public List<CargoCrate> trackedCrates = new List<CargoCrate>();

        private BoxCollider _zoneCollider;

        public int TotalCrates
        {
            get
            {
                int count = 0;
                for (int i = 0; i < trackedCrates.Count; i++)
                {
                    if (trackedCrates[i] != null) count++;
                }
                return count;
            }
        }

        public int DeliveredCount { get; private set; }
        public bool IsTaskComplete => TotalCrates > 0 && DeliveredCount >= TotalCrates;

        private void Awake()
        {
            _zoneCollider = GetComponent<BoxCollider>();
            _zoneCollider.isTrigger = true;
        }

        private void FixedUpdate()
        {
            if (_zoneCollider == null) return;

            Bounds bounds = _zoneCollider.bounds;
            int count = 0;

            for (int i = trackedCrates.Count - 1; i >= 0; i--)
            {
                CargoCrate crate = trackedCrates[i];
                if (crate == null)
                {
                    trackedCrates.RemoveAt(i);
                    continue;
                }

                Vector3 pos = crate.transform.position;
                bool inside = bounds.Contains(pos);
                bool withinHeight = (pos.y - bounds.min.y) <= maxStackedHeight;
                bool settled = crate.Body.linearVelocity.magnitude <= maxSettledSpeed;

                if (inside && withinHeight && settled)
                {
                    count++;
                }
            }

            DeliveredCount = count;
        }

        public void RegisterCrate(CargoCrate crate)
        {
            if (crate != null && !trackedCrates.Contains(crate))
            {
                trackedCrates.Add(crate);
            }
        }

        public void UnregisterCrate(CargoCrate crate)
        {
            if (crate != null)
            {
                trackedCrates.Remove(crate);
            }
        }

        public void ResetAllCrates()
        {
            for (int i = 0; i < trackedCrates.Count; i++)
            {
                if (trackedCrates[i] != null)
                {
                    trackedCrates[i].ResetCrate();
                }
            }

            DeliveredCount = 0;
        }
    }
}
