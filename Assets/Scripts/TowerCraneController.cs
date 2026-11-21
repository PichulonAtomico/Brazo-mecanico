using System.Collections.Generic;
using UnityEngine;

namespace GruaTorre
{
    public class TowerCraneController : MonoBehaviour
    {
        [Header("Raíz y cuerpos articulados (9 cuerpos)")]
        [Tooltip("Raíz fija de la articulación (immovable = true).")]
        public ArticulationBody torreBase;

        [Tooltip("Drive 1 - Revolute (giro horizontal de la pluma en grados).")]
        public ArticulationBody pluma;

        [Tooltip("Fixed - Contrapeso trasero de la pluma.")]
        public ArticulationBody contrapeso;

        [Tooltip("Drive 2 - Prismatic (avance radial del carro en metros).")]
        public ArticulationBody carro;

        [Tooltip("Drive 3 - Prismatic (subida y bajada del polipasto en metros).")]
        public ArticulationBody izaje;

        [Tooltip("Pasivo - Spherical sin stiffness (péndulo libre del cable).")]
        public ArticulationBody cablePendulo;

        [Tooltip("Drive 4 - Revolute (rotación del cabezal de la pinza en grados).")]
        public ArticulationBody cabezalPinza;

        [Tooltip("Drive 5 - Prismatic (mordaza izquierda de la pinza en metros).")]
        public ArticulationBody mordazaIzq;

        [Tooltip("Drive 6 - Prismatic (mordaza derecha de la pinza en metros).")]
        public ArticulationBody mordazaDer;

        [Header("Límites operativos")]
        public float slewMinDeg = -180f;
        public float slewMaxDeg = 180f;

        public float trolleyMinM = 0f;
        public float trolleyMaxM = 8f;

        public float hoistMinM = -6.55f;
        public float hoistMaxM = 0f;

        public float headMinDeg = -180f;
        public float headMaxDeg = 180f;

        [Tooltip("Desplazamiento de cada mordaza abierta (0 m).")]
        public float jawOpenM = 0f;

        [Tooltip("Desplazamiento de cada mordaza cerrada (m).")]
        public float jawClosedM = 0.48f;

        [Header("Estado inicial (Home)")]
        public float homeSlewDeg = 0f;
        public float homeTrolleyM = 3.5f;
        public float homeHoistM = -2.0f;
        public float homeHeadDeg = 0f;
        public bool homeGripperClosed = false;

        [Header("Parámetros de Solver (estabilidad WebGL)")]
        public int solverIterations = 20;
        public int solverVelocityIterations = 8;
        public float SlewTargetDeg { get; private set; }
        public float TrolleyTargetM { get; private set; }
        public float HoistTargetM { get; private set; }
        public float HeadTargetDeg { get; private set; }
        public bool GripperClosed { get; private set; }
        public float LeftJawTargetM => GripperClosed ? jawClosedM : jawOpenM;
        public float RightJawTargetM => GripperClosed ? -jawClosedM : -jawOpenM;
        public bool DrivesEnabled { get; private set; } = true;

        private SavedDriveParams _plumaDrive;
        private SavedDriveParams _carroDrive;
        private SavedDriveParams _izajeDrive;
        private SavedDriveParams _cabezalDrive;
        private SavedDriveParams _mordazaIzqDrive;
        private SavedDriveParams _mordazaDerDrive;
        private bool _drivesCached;

        private struct SavedDriveParams
        {
            public float stiffness;
            public float damping;
            public float forceLimit;
        }

        private void Awake()
        {
            ConfigureSolver();
            CacheDriveParameters();

            SlewTargetDeg = Mathf.Clamp(homeSlewDeg, slewMinDeg, slewMaxDeg);
            TrolleyTargetM = Mathf.Clamp(homeTrolleyM, trolleyMinM, trolleyMaxM);
            HoistTargetM = Mathf.Clamp(homeHoistM, hoistMinM, hoistMaxM);
            HeadTargetDeg = Mathf.Clamp(homeHeadDeg, headMinDeg, headMaxDeg);
            GripperClosed = homeGripperClosed;
        }

        private void Start()
        {
            SnapToTargets();
        }

        private void ConfigureSolver()
        {
            if (torreBase != null)
            {
                torreBase.solverIterations = solverIterations;
                torreBase.solverVelocityIterations = solverVelocityIterations;
            }
        }

        private void CacheDriveParameters()
        {
            if (_drivesCached) return;

            _plumaDrive = ReadDrive(pluma, ArticulationDriveAxis.X);
            _carroDrive = ReadDrive(carro, ArticulationDriveAxis.Z);
            _izajeDrive = ReadDrive(izaje, ArticulationDriveAxis.Y);
            _cabezalDrive = ReadDrive(cabezalPinza, ArticulationDriveAxis.X);
            _mordazaIzqDrive = ReadDrive(mordazaIzq, ArticulationDriveAxis.X);
            _mordazaDerDrive = ReadDrive(mordazaDer, ArticulationDriveAxis.X);

            _drivesCached = true;
        }

        private static SavedDriveParams ReadDrive(ArticulationBody body, ArticulationDriveAxis axis)
        {
            if (body == null) return default;
            ArticulationDrive d = axis switch
            {
                ArticulationDriveAxis.Y => body.yDrive,
                ArticulationDriveAxis.Z => body.zDrive,
                _ => body.xDrive
            };
            return new SavedDriveParams
            {
                stiffness = d.stiffness,
                damping = d.damping,
                forceLimit = d.forceLimit
            };
        }

        private void FixedUpdate()
        {
            if (!DrivesEnabled) return;

            ApplyDriveTarget(pluma, ArticulationDriveAxis.X, SlewTargetDeg);
            ApplyDriveTarget(carro, ArticulationDriveAxis.Z, TrolleyTargetM);
            ApplyDriveTarget(izaje, ArticulationDriveAxis.Y, HoistTargetM);
            ApplyDriveTarget(cabezalPinza, ArticulationDriveAxis.X, HeadTargetDeg);
            ApplyDriveTarget(mordazaIzq, ArticulationDriveAxis.X, LeftJawTargetM);
            ApplyDriveTarget(mordazaDer, ArticulationDriveAxis.X, RightJawTargetM);
        }

        private static void ApplyDriveTarget(ArticulationBody body, ArticulationDriveAxis axis, float target)
        {
            if (body == null) return;

            switch (axis)
            {
                case ArticulationDriveAxis.X:
                {
                    var d = body.xDrive;
                    d.target = target;
                    body.xDrive = d;
                    break;
                }
                case ArticulationDriveAxis.Y:
                {
                    var d = body.yDrive;
                    d.target = target;
                    body.yDrive = d;
                    break;
                }
                case ArticulationDriveAxis.Z:
                {
                    var d = body.zDrive;
                    d.target = target;
                    body.zDrive = d;
                    break;
                }
            }
        }
        public void SetSlewTarget(float degrees)
        {
            SlewTargetDeg = Mathf.Clamp(degrees, slewMinDeg, slewMaxDeg);
        }

        public void SetTrolleyTarget(float meters)
        {
            TrolleyTargetM = Mathf.Clamp(meters, trolleyMinM, trolleyMaxM);
        }

        public void SetHoistTarget(float meters)
        {
            HoistTargetM = Mathf.Clamp(meters, hoistMinM, hoistMaxM);
        }

        public void SetHeadTarget(float degrees)
        {
            HeadTargetDeg = Mathf.Clamp(degrees, headMinDeg, headMaxDeg);
        }

        public void OpenGripper()
        {
            GripperClosed = false;
        }

        public void CloseGripper()
        {
            GripperClosed = true;
        }
        public void SetDrivesEnabled(bool enabled)
        {
            CacheDriveParameters();
            DrivesEnabled = enabled;

            SetDrivePower(pluma, ArticulationDriveAxis.X, _plumaDrive, enabled);
            SetDrivePower(carro, ArticulationDriveAxis.Z, _carroDrive, enabled);
            SetDrivePower(izaje, ArticulationDriveAxis.Y, _izajeDrive, enabled);
            SetDrivePower(cabezalPinza, ArticulationDriveAxis.X, _cabezalDrive, enabled);
            SetDrivePower(mordazaIzq, ArticulationDriveAxis.X, _mordazaIzqDrive, enabled);
            SetDrivePower(mordazaDer, ArticulationDriveAxis.X, _mordazaDerDrive, enabled);
        }

        private static void SetDrivePower(ArticulationBody body, ArticulationDriveAxis axis, SavedDriveParams saved, bool enabled)
        {
            if (body == null) return;

            float s = enabled ? saved.stiffness : 0f;
            float dmp = enabled ? saved.damping : 5f;
            float f = enabled ? saved.forceLimit : 0f;

            switch (axis)
            {
                case ArticulationDriveAxis.X:
                {
                    var d = body.xDrive;
                    d.stiffness = s;
                    d.damping = dmp;
                    d.forceLimit = f;
                    body.xDrive = d;
                    break;
                }
                case ArticulationDriveAxis.Y:
                {
                    var d = body.yDrive;
                    d.stiffness = s;
                    d.damping = dmp;
                    d.forceLimit = f;
                    body.yDrive = d;
                    break;
                }
                case ArticulationDriveAxis.Z:
                {
                    var d = body.zDrive;
                    d.stiffness = s;
                    d.damping = dmp;
                    d.forceLimit = f;
                    body.zDrive = d;
                    break;
                }
            }
        }
        public void ResetCrane()
        {
            SetDrivesEnabled(true);

            SlewTargetDeg = homeSlewDeg;
            TrolleyTargetM = homeTrolleyM;
            HoistTargetM = homeHoistM;
            HeadTargetDeg = homeHeadDeg;
            GripperClosed = homeGripperClosed;

            SnapToTargets();
        }

        private void SnapToTargets()
        {
            if (torreBase == null) return;

            var positions = new List<float>();
            var velocities = new List<float>();
            torreBase.GetJointPositions(positions);
            torreBase.GetJointVelocities(velocities);

            for (int i = 0; i < velocities.Count; i++)
            {
                velocities[i] = 0f;
            }
            SetSingleJointPosition(pluma, SlewTargetDeg * Mathf.Deg2Rad);
            SetSingleJointPosition(carro, TrolleyTargetM);
            SetSingleJointPosition(izaje, HoistTargetM);
            SetSingleJointPosition(cabezalPinza, HeadTargetDeg * Mathf.Deg2Rad);
            SetSingleJointPosition(mordazaIzq, LeftJawTargetM);
            SetSingleJointPosition(mordazaDer, RightJawTargetM);

            if (cablePendulo != null && cablePendulo.dofCount == 3)
            {
                cablePendulo.jointPosition = new ArticulationReducedSpace(0f, 0f, 0f);
                cablePendulo.jointVelocity = new ArticulationReducedSpace(0f, 0f, 0f);
            }

            torreBase.SetJointVelocities(velocities);

            FixedUpdate();
        }

        private static void SetSingleJointPosition(ArticulationBody body, float value)
        {
            if (body == null || body.dofCount < 1) return;
            body.jointPosition = new ArticulationReducedSpace(value);
            body.jointVelocity = new ArticulationReducedSpace(0f);
        }

        public float ActualSlewDeg => ReadAngleDeg(pluma, 0);
        public float ActualTrolleyM => ReadLinearM(carro, 0);
        public float ActualHoistM => ReadLinearM(izaje, 0);
        public float ActualHeadDeg => ReadAngleDeg(cabezalPinza, 0);
        public float ActualLeftJawM => ReadLinearM(mordazaIzq, 0);
        public float ActualRightJawM => ReadLinearM(mordazaDer, 0);

        public float ActualCableTwistDeg => ReadAngleDeg(cablePendulo, 0);
        public float ActualCableSwingYDeg => ReadAngleDeg(cablePendulo, 1);
        public float ActualCableSwingZDeg => ReadAngleDeg(cablePendulo, 2);

        private static float ReadAngleDeg(ArticulationBody body, int index)
        {
            if (body == null || body.dofCount <= index) return 0f;
            return body.jointPosition[index] * Mathf.Rad2Deg;
        }

        private static float ReadLinearM(ArticulationBody body, int index)
        {
            if (body == null || body.dofCount <= index) return 0f;
            return body.jointPosition[index];
        }
    }
}
