using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace GruaTorre
{
    public class TowerCraneUI : MonoBehaviour
    {
        [Header("Referencias del simulador")]
        public TowerCraneController crane;
        public CargoDropZone dropZone;
        public CargoManager cargoManager;
        public CraneCameraRig cameraRig;

        [Header("Sliders de control (4 ejes principales)")]
        public Slider slewSlider;
        public Slider trolleySlider;
        public Slider hoistSlider;
        public Slider headSlider;

        [Header("Etiquetas de valor de sliders")]
        public Text slewValueText;
        public Text trolleyValueText;
        public Text hoistValueText;
        public Text headValueText;

        [Header("Botones de pinza y sistema")]
        public Button openGripperButton;
        public Button closeGripperButton;
        public Button resetButton;
        public Toggle drivesToggle;
        public Text gripperStateText;

        [Header("Botones de cámara")]
        public Button camOverviewButton;
        public Button camJibButton;
        public Button camGripperButton;

        [Header("Gestión de cargas (agregar / cambiar forma)")]
        public Button prevShapeButton;
        public Button nextShapeButton;
        public Button spawnCrateButton;
        public Button applyShapeButton;
        public Button removeCrateButton;
        public Text selectedShapeText;
        public Text cargoCountText;

        [Header("Lectura de estado (Target vs Real) y tarea")]
        public Text telemetryText;
        public Text taskStatusText;

        private readonly StringBuilder _sb = new StringBuilder(512);
        private float _elapsedSeconds;
        private bool _taskCompleted;

        private void Start()
        {
            ConfigureSliders();
            BindEvents();
            SyncUIFromCrane();
        }

        private void ConfigureSliders()
        {
            if (crane == null) return;

            if (slewSlider != null)
            {
                slewSlider.minValue = crane.slewMinDeg;
                slewSlider.maxValue = crane.slewMaxDeg;
                slewSlider.SetValueWithoutNotify(crane.SlewTargetDeg);
            }

            if (trolleySlider != null)
            {
                trolleySlider.minValue = crane.trolleyMinM;
                trolleySlider.maxValue = crane.trolleyMaxM;
                trolleySlider.SetValueWithoutNotify(crane.TrolleyTargetM);
            }

            if (hoistSlider != null)
            {
                hoistSlider.minValue = crane.hoistMinM;
                hoistSlider.maxValue = crane.hoistMaxM;
                hoistSlider.SetValueWithoutNotify(crane.HoistTargetM);
            }

            if (headSlider != null)
            {
                headSlider.minValue = crane.headMinDeg;
                headSlider.maxValue = crane.headMaxDeg;
                headSlider.SetValueWithoutNotify(crane.HeadTargetDeg);
            }
        }

        private void BindEvents()
        {
            if (slewSlider != null)
                slewSlider.onValueChanged.AddListener(v => crane.SetSlewTarget(v));

            if (trolleySlider != null)
                trolleySlider.onValueChanged.AddListener(v => crane.SetTrolleyTarget(v));

            if (hoistSlider != null)
                hoistSlider.onValueChanged.AddListener(v => crane.SetHoistTarget(v));

            if (headSlider != null)
                headSlider.onValueChanged.AddListener(v => crane.SetHeadTarget(v));

            if (openGripperButton != null)
                openGripperButton.onClick.AddListener(OnOpenGripper);

            if (closeGripperButton != null)
                closeGripperButton.onClick.AddListener(OnCloseGripper);

            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetAll);

            if (drivesToggle != null)
                drivesToggle.onValueChanged.AddListener(enabled => crane.SetDrivesEnabled(enabled));

            if (camOverviewButton != null && cameraRig != null)
                camOverviewButton.onClick.AddListener(cameraRig.SetOverview);

            if (camJibButton != null && cameraRig != null)
                camJibButton.onClick.AddListener(cameraRig.SetJibView);

            if (camGripperButton != null && cameraRig != null)
                camGripperButton.onClick.AddListener(cameraRig.SetGripperView);

            if (prevShapeButton != null && cargoManager != null)
                prevShapeButton.onClick.AddListener(OnPrevShape);

            if (nextShapeButton != null && cargoManager != null)
                nextShapeButton.onClick.AddListener(OnNextShape);

            if (spawnCrateButton != null && cargoManager != null)
                spawnCrateButton.onClick.AddListener(OnSpawnCrate);

            if (applyShapeButton != null && cargoManager != null)
                applyShapeButton.onClick.AddListener(OnApplyShapeToAll);

            if (removeCrateButton != null && cargoManager != null)
                removeCrateButton.onClick.AddListener(OnRemoveLastCrate);
        }

        private void OnPrevShape()
        {
            cargoManager.PreviousShape();
            UpdateCargoManagerUI();
        }

        private void OnNextShape()
        {
            cargoManager.NextShape();
            UpdateCargoManagerUI();
        }

        private void OnSpawnCrate()
        {
            cargoManager.SpawnCrate();
            UpdateCargoManagerUI();
        }

        private void OnApplyShapeToAll()
        {
            cargoManager.ApplySelectedShapeToAll();
            UpdateCargoManagerUI();
        }

        private void OnRemoveLastCrate()
        {
            cargoManager.RemoveLastExtraCrate();
            UpdateCargoManagerUI();
        }

        private void OnOpenGripper()
        {
            if (crane == null) return;
            crane.OpenGripper();
            UpdateGripperVisualState();
        }

        private void OnCloseGripper()
        {
            if (crane == null) return;
            crane.CloseGripper();
            UpdateGripperVisualState();
        }

        private void OnResetAll()
        {
            if (crane != null) crane.ResetCrane();
            if (cargoManager != null)
                cargoManager.ResetToInitialCrates();
            else if (dropZone != null)
                dropZone.ResetAllCrates();

            _elapsedSeconds = 0f;
            _taskCompleted = false;

            SyncUIFromCrane();
        }

        private void SyncUIFromCrane()
        {
            if (crane == null) return;

            if (slewSlider != null) slewSlider.SetValueWithoutNotify(crane.SlewTargetDeg);
            if (trolleySlider != null) trolleySlider.SetValueWithoutNotify(crane.TrolleyTargetM);
            if (hoistSlider != null) hoistSlider.SetValueWithoutNotify(crane.HoistTargetM);
            if (headSlider != null) headSlider.SetValueWithoutNotify(crane.HeadTargetDeg);
            if (drivesToggle != null) drivesToggle.SetIsOnWithoutNotify(crane.DrivesEnabled);

            UpdateGripperVisualState();
            UpdateCargoManagerUI();
        }

        private void UpdateCargoManagerUI()
        {
            if (cargoManager == null) return;

            if (selectedShapeText != null)
            {
                selectedShapeText.text = CargoCrate.GetDisplayName(cargoManager.SelectedShape);
            }

            if (cargoCountText != null)
            {
                cargoCountText.text = $"Activas: {cargoManager.ActiveCount} / {cargoManager.maxTotalCrates}";
            }

            if (spawnCrateButton != null)
            {
                spawnCrateButton.interactable = cargoManager.CanSpawnMore;
            }

            if (removeCrateButton != null)
            {
                removeCrateButton.interactable = cargoManager.HasExtraCrates;
            }
        }

        private void UpdateGripperVisualState()
        {
            if (crane == null) return;

            if (gripperStateText != null)
            {
                gripperStateText.text = crane.GripperClosed ? "Estado pinza: CERRADA" : "Estado pinza: ABIERTA";
                gripperStateText.color = crane.GripperClosed
                    ? new Color(1f, 0.78f, 0.3f)
                    : new Color(0.55f, 0.92f, 0.65f);
            }
        }

        private void Update()
        {
            if (crane == null) return;

            UpdateSliderLabels();
            UpdateTelemetryPanel();
            UpdateTaskPanel();
        }

        private void UpdateSliderLabels()
        {
            if (slewValueText != null) slewValueText.text = $"{crane.SlewTargetDeg:F1}°";
            if (trolleyValueText != null) trolleyValueText.text = $"{crane.TrolleyTargetM:F2} m";
            if (hoistValueText != null) hoistValueText.text = $"{crane.HoistTargetM:F2} m";
            if (headValueText != null) headValueText.text = $"{crane.HeadTargetDeg:F1}°";
        }

        private void UpdateTelemetryPanel()
        {
            if (telemetryText == null) return;

            _sb.Clear();
            _sb.AppendLine("<b>LECTURA DE JOINTS (TARGET | REAL)</b>");
            _sb.AppendLine(crane.DrivesEnabled
                ? "<color=#8BE28B>Drives: ACTIVOS</color>"
                : "<color=#FF8866>Drives: DESACTIVADOS (caída libre)</color>");
            _sb.AppendLine("-------------------------------------------");
            _sb.AppendFormat("1. Pluma [Revolute]\n   Target: {0,6:F1}° | Real: {1,6:F1}°\n",
                crane.SlewTargetDeg, crane.ActualSlewDeg);
            _sb.AppendFormat("2. Carro [Prismatic]\n   Target: {0,5:F2} m | Real: {1,5:F2} m\n",
                crane.TrolleyTargetM, crane.ActualTrolleyM);
            _sb.AppendFormat("3. Izaje [Prismatic]\n   Target: {0,5:F2} m | Real: {1,5:F2} m\n",
                crane.HoistTargetM, crane.ActualHoistM);
            _sb.AppendFormat("4. Cabezal [Revolute]\n   Target: {0,6:F1}° | Real: {1,6:F1}°\n",
                crane.HeadTargetDeg, crane.ActualHeadDeg);
            _sb.AppendFormat("5. Mordaza Izq [Prismatic]\n   Target: {0,5:F2} m | Real: {1,5:F2} m\n",
                crane.LeftJawTargetM, crane.ActualLeftJawM);
            _sb.AppendFormat("6. Mordaza Der [Prismatic]\n   Target: {0,5:F2} m | Real: {1,5:F2} m\n",
                crane.RightJawTargetM, crane.ActualRightJawM);
            _sb.AppendLine("-------------------------------------------");
            _sb.AppendFormat("7. Cable [Spherical PASIVO]\n   Swing Y: {0,5:F1}° | Swing Z: {1,5:F1}°\n   Twist:   {2,5:F1}°",
                crane.ActualCableSwingYDeg, crane.ActualCableSwingZDeg, crane.ActualCableTwistDeg);

            telemetryText.text = _sb.ToString();
        }

        private void UpdateTaskPanel()
        {
            if (taskStatusText == null || dropZone == null) return;

            if (!dropZone.IsTaskComplete)
            {
                _elapsedSeconds += Time.deltaTime;
                _taskCompleted = false;
            }
            else if (!_taskCompleted)
            {
                _taskCompleted = true;
            }

            int mins = Mathf.FloorToInt(_elapsedSeconds / 60f);
            float secs = _elapsedSeconds - mins * 60f;

            if (_taskCompleted)
            {
                taskStatusText.text =
                    $"<b>MANIOBRA COMPLETADA</b>\n" +
                    $"Cargas en plataforma: {dropZone.DeliveredCount} / {dropZone.TotalCrates}\n" +
                    $"Tiempo: {mins:00}:{secs:00.0}  (Usá Reset para repetir)";
                taskStatusText.color = new Color(0.55f, 1f, 0.62f);
            }
            else
            {
                taskStatusText.text =
                    $"<b>OBJETIVO:</b> Trasladar cargas a la plataforma naranja (+90°)\n" +
                    $"Cargas depositadas: {dropZone.DeliveredCount} / {dropZone.TotalCrates}   |   Tiempo: {mins:00}:{secs:00.0}";
                taskStatusText.color = Color.white;
            }
        }
    }
}
