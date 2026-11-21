using System.Collections.Generic;
using UnityEngine;

namespace GruaTorre
{
    public class CargoManager : MonoBehaviour
    {
        [Header("Referencias")]
        public CargoDropZone dropZone;
        public Transform cratesParent;

        [Header("Materiales y física")]
        public Material[] paletteMaterials;
        public Material trimMaterial;
        public PhysicsMaterial gripPhysicsMaterial;

        [Header("Límites y zona de spawn")]
        public int maxTotalCrates = 10;
        public Vector3 pickupZoneCenter = new Vector3(0f, 0.08f, 6.0f);

        public CargoShape SelectedShape { get; private set; } = CargoShape.Cube;

        private readonly List<CargoCrate> _spawnedExtras = new List<CargoCrate>();
        private int _spawnCounter = 3;

        private static readonly Vector2[] BaseSlotOffsets =
        {
            new Vector2(0.0f, 0.0f),
            new Vector2(-1.45f, -0.15f),
            new Vector2(1.45f, -0.15f),
            new Vector2(0.0f, 1.05f),
            new Vector2(-1.45f, 1.05f),
            new Vector2(1.45f, 1.05f),
            new Vector2(0.0f, -1.05f),
            new Vector2(-1.45f, -1.05f),
            new Vector2(1.45f, -1.05f)
        };

        public int ActiveCount => dropZone != null ? dropZone.TotalCrates : 0;
        public bool CanSpawnMore => ActiveCount < maxTotalCrates;
        public bool HasExtraCrates
        {
            get
            {
                PruneDestroyedExtras();
                return _spawnedExtras.Count > 0;
            }
        }

        public void NextShape()
        {
            int next = ((int)SelectedShape + 1) % 4;
            SelectedShape = (CargoShape)next;
        }

        public void PreviousShape()
        {
            int prev = ((int)SelectedShape + 3) % 4;
            SelectedShape = (CargoShape)prev;
        }

        public void SetShape(CargoShape shape)
        {
            SelectedShape = shape;
        }

        public CargoCrate SpawnCrate()
        {
            if (dropZone == null || !CanSpawnMore) return null;

            _spawnCounter++;
            float halfH = CargoCrate.GetHalfHeight(SelectedShape);
            Vector3 spawnPos = FindFreeSpawnPosition(halfH);

            var go = new GameObject($"Carga_{_spawnCounter:00}_{SelectedShape}");
            if (cratesParent != null)
            {
                go.transform.SetParent(cratesParent, false);
            }
            go.transform.position = spawnPos;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            go.AddComponent<Rigidbody>();
            var crate = go.AddComponent<CargoCrate>();
            crate.cargoName = go.name;
            crate.isInitialCrate = false;
            crate.SetInitialPose(spawnPos, Quaternion.identity);

            Material mainMat = PickMaterial(_spawnCounter - 1);
            crate.ConfigureShape(SelectedShape, mainMat, trimMaterial, gripPhysicsMaterial, adjustHeightOnSwap: false);

            _spawnedExtras.Add(crate);
            dropZone.RegisterCrate(crate);
            return crate;
        }

        public void ApplySelectedShapeToAll()
        {
            if (dropZone == null) return;

            for (int i = 0; i < dropZone.trackedCrates.Count; i++)
            {
                CargoCrate crate = dropZone.trackedCrates[i];
                if (crate == null) continue;

                Material mainMat = crate.mainMaterial != null ? crate.mainMaterial : PickMaterial(i);
                crate.ConfigureShape(SelectedShape, mainMat, trimMaterial, gripPhysicsMaterial, adjustHeightOnSwap: true);
            }
        }

        public void RemoveLastExtraCrate()
        {
            PruneDestroyedExtras();
            if (_spawnedExtras.Count == 0) return;

            int lastIdx = _spawnedExtras.Count - 1;
            CargoCrate last = _spawnedExtras[lastIdx];
            _spawnedExtras.RemoveAt(lastIdx);

            if (dropZone != null)
            {
                dropZone.UnregisterCrate(last);
            }

            if (last != null)
            {
                Destroy(last.gameObject);
            }
        }
        public void ResetToInitialCrates()
        {
            PruneDestroyedExtras();

            for (int i = _spawnedExtras.Count - 1; i >= 0; i--)
            {
                CargoCrate extra = _spawnedExtras[i];
                if (dropZone != null) dropZone.UnregisterCrate(extra);
                if (extra != null) Destroy(extra.gameObject);
            }

            _spawnedExtras.Clear();
            _spawnCounter = 3;

            if (dropZone != null)
            {
                dropZone.ResetAllCrates();
            }
        }

        private Vector3 FindFreeSpawnPosition(float halfHeight)
        {
            for (int layer = 0; layer < 2; layer++)
            {
                float y = pickupZoneCenter.y + halfHeight + 0.03f + layer * 1.05f;
                for (int i = 0; i < BaseSlotOffsets.Length; i++)
                {
                    Vector3 candidate = new Vector3(
                        pickupZoneCenter.x + BaseSlotOffsets[i].x,
                        y,
                        pickupZoneCenter.z + BaseSlotOffsets[i].y);

                    if (!IsPositionOccupied(candidate, 0.42f))
                    {
                        return candidate;
                    }
                }
            }
            return new Vector3(pickupZoneCenter.x, pickupZoneCenter.y + halfHeight + 2.15f, pickupZoneCenter.z);
        }

        private static bool IsPositionOccupied(Vector3 center, float radius)
        {
            Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].attachedRigidbody != null || hits[i].attachedArticulationBody != null)
                {
                    return true;
                }
            }
            return false;
        }

        private Material PickMaterial(int index)
        {
            if (paletteMaterials == null || paletteMaterials.Length == 0) return null;
            return paletteMaterials[Mathf.Abs(index) % paletteMaterials.Length];
        }

        private void PruneDestroyedExtras()
        {
            for (int i = _spawnedExtras.Count - 1; i >= 0; i--)
            {
                if (_spawnedExtras[i] == null) _spawnedExtras.RemoveAt(i);
            }
        }
    }
}
