using UnityEngine;

namespace GruaTorre
{
    public enum CargoShape
    {
        Cube = 0,
        Cylinder = 1,
        Beam = 2,
        Sphere = 3
    }

    [RequireComponent(typeof(Rigidbody))]
    public class CargoCrate : MonoBehaviour
    {
        public string cargoName = "Carga";
        public CargoShape currentShape = CargoShape.Cube;
        public bool isInitialCrate = true;

        public Material mainMaterial;
        public Material trimMaterial;
        public PhysicsMaterial physicsMaterial;

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Rigidbody _rb;

        public Rigidbody Body => _rb != null ? _rb : (_rb = GetComponent<Rigidbody>());

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
        }

        public void SetInitialPose(Vector3 position, Quaternion rotation)
        {
            _initialPosition = position;
            _initialRotation = rotation;
        }

        public void ResetCrate()
        {
            var rb = Body;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(_initialPosition, _initialRotation);
            rb.Sleep();
            rb.WakeUp();
        }

        public void ConfigureShape(CargoShape newShape, Material mainMat, Material trimMat, PhysicsMaterial physMat, bool adjustHeightOnSwap = false)
        {
            float oldHalfHeight = GetHalfHeight(currentShape);
            float newHalfHeight = GetHalfHeight(newShape);

            currentShape = newShape;
            if (mainMat != null) mainMaterial = mainMat;
            if (trimMat != null) trimMaterial = trimMat;
            if (physMat != null) physicsMaterial = physMat;

            var oldColliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < oldColliders.Length; i++)
            {
                oldColliders[i].enabled = false;
                if (oldColliders[i].gameObject == gameObject)
                {
                    SafeDestroy(oldColliders[i]);
                }
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                SafeDestroy(transform.GetChild(i).gameObject);
            }

            var rb = Body;
            rb.mass = GetMass(newShape);
            rb.linearDamping = 0.05f;
            rb.angularDamping = newShape == CargoShape.Sphere ? 0.65f : 0.15f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            switch (newShape)
            {
                case CargoShape.Cube:
                {
                    var box = gameObject.AddComponent<BoxCollider>();
                    box.size = new Vector3(0.90f, 0.90f, 0.90f);
                    box.sharedMaterial = physicsMaterial;

                    CreateVisualPrimitive(PrimitiveType.Cube, "Cuerpo", Vector3.zero, new Vector3(0.90f, 0.90f, 0.90f), mainMaterial);
                    CreateVisualPrimitive(PrimitiveType.Cube, "BandaSuperior", new Vector3(0f, 0.42f, 0f), new Vector3(0.92f, 0.06f, 0.92f), trimMaterial);
                    CreateVisualPrimitive(PrimitiveType.Cube, "BandaInferior", new Vector3(0f, -0.42f, 0f), new Vector3(0.92f, 0.06f, 0.92f), trimMaterial);
                    break;
                }

                case CargoShape.Cylinder:
                {
                    var visual = CreateVisualPrimitive(PrimitiveType.Cylinder, "CuerpoBarril", Vector3.zero, new Vector3(0.78f, 0.475f, 0.78f), mainMaterial);
                    CreateVisualPrimitive(PrimitiveType.Cylinder, "AroSuperior", new Vector3(0f, 0.36f, 0f), new Vector3(0.81f, 0.04f, 0.81f), trimMaterial);
                    CreateVisualPrimitive(PrimitiveType.Cylinder, "AroInferior", new Vector3(0f, -0.36f, 0f), new Vector3(0.81f, 0.04f, 0.81f), trimMaterial);

                    var meshCol = visual.AddComponent<MeshCollider>();
                    meshCol.convex = true;
                    meshCol.sharedMaterial = physicsMaterial;
                    break;
                }

                case CargoShape.Beam:
                {
                    var box = gameObject.AddComponent<BoxCollider>();
                    box.size = new Vector3(0.55f, 0.65f, 1.55f);
                    box.sharedMaterial = physicsMaterial;

                    CreateVisualPrimitive(PrimitiveType.Cube, "CuerpoViga", Vector3.zero, new Vector3(0.55f, 0.65f, 1.55f), mainMaterial);
                    CreateVisualPrimitive(PrimitiveType.Cube, "TopeFrontal", new Vector3(0f, 0f, 0.74f), new Vector3(0.58f, 0.68f, 0.08f), trimMaterial);
                    CreateVisualPrimitive(PrimitiveType.Cube, "TopeTrasero", new Vector3(0f, 0f, -0.74f), new Vector3(0.58f, 0.68f, 0.08f), trimMaterial);
                    break;
                }

                case CargoShape.Sphere:
                {
                    var sphere = gameObject.AddComponent<SphereCollider>();
                    sphere.radius = 0.425f;
                    sphere.sharedMaterial = physicsMaterial;

                    CreateVisualPrimitive(PrimitiveType.Sphere, "CuerpoEsfera", Vector3.zero, Vector3.one * 0.85f, mainMaterial);
                    CreateVisualPrimitive(PrimitiveType.Cylinder, "AroEcuatorial", Vector3.zero, new Vector3(0.87f, 0.04f, 0.87f), trimMaterial);
                    break;
                }
            }

            if (adjustHeightOnSwap)
            {
                float deltaY = Mathf.Max(0f, newHalfHeight - oldHalfHeight) + 0.04f;
                transform.position += Vector3.up * deltaY;
                transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.WakeUp();
            }
        }

        private GameObject CreateVisualPrimitive(PrimitiveType primitive, string childName, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = childName;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;

            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
                SafeDestroy(col);
            }

            if (mat != null)
            {
                var r = go.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = mat;
            }

            return go;
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        public static float GetHalfHeight(CargoShape shape)
        {
            return shape switch
            {
                CargoShape.Cylinder => 0.475f,
                CargoShape.Beam => 0.325f,
                CargoShape.Sphere => 0.425f,
                _ => 0.45f
            };
        }

        public static float GetMass(CargoShape shape)
        {
            return shape switch
            {
                CargoShape.Cylinder => 22f,
                CargoShape.Beam => 30f,
                CargoShape.Sphere => 18f,
                _ => 25f
            };
        }

        public static string GetDisplayName(CargoShape shape)
        {
            return shape switch
            {
                CargoShape.Cylinder => "Cilindro (22 kg)",
                CargoShape.Beam => "Viga larga (30 kg)",
                CargoShape.Sphere => "Esfera (18 kg)",
                _ => "Cubo (25 kg)"
            };
        }
    }
}
