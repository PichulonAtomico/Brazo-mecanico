using System;
using System.Collections.Generic;
using System.IO;
using GruaTorre;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GruaTorre.Editor
{
    /// <summary>
    /// Construye la escena completa del Simulador de Grúa Torre (Unity 6.3 LTS + URP + WebGL)
    /// con 9 Articulation Bodies, 4 tipos de joint, tarea de cargas Rigidbody y UI completa.
    /// </summary>
    public static class TowerCraneSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/TowerCraneSimulator.unity";
        private const string WebGLOutputDir = "Builds/WebGL";
        private const string MaterialsFolder = "Assets/Materials";
        private const string PhysicsFolder = "Assets/Physics";
        private const string GripPhysicsMatPath = "Assets/Physics/GripHighFriction.physicMaterial";

        [MenuItem("Tools/Grua Torre/Construir Escena Completa")]
        public static void BuildSceneMenu()
        {
            BuildSceneInternal();
            Debug.Log("[GruaTorre] Escena construida y guardada en: " + ScenePath);
        }

        [MenuItem("Tools/Grua Torre/Compilar Build WebGL")]
        public static void BuildWebGLMenu()
        {
            BuildWebGLInternal();
        }

        /// <summary>
        /// Punto de entrada para compilar la build WebGL desde CLI (-executeMethod).
        /// </summary>
        public static void BuildWebGLBatch()
        {
            try
            {
                BuildWebGLInternal();
                Debug.Log("[GruaTorre] BuildWebGLBatch finalizado sin errores.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[GruaTorre] Error en BuildWebGLBatch: " + ex);
                EditorApplication.Exit(1);
            }
        }

        private static void BuildWebGLInternal()
        {
            if (!File.Exists(ScenePath))
            {
                BuildSceneInternal();
            }

            // Configuración WebGL compatible con GitHub Pages, itch.io y servidores estáticos locales
            PlayerSettings.productName = "Simulador Grua Torre";
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.dataCaching = true;

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            }

            if (Directory.Exists(WebGLOutputDir))
            {
                Directory.Delete(WebGLOutputDir, true);
            }
            Directory.CreateDirectory(WebGLOutputDir);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = WebGLOutputDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Falló la build WebGL: {summary.result} ({summary.totalErrors} errores).");
            }

            double mb = summary.totalSize / (1024.0 * 1024.0);
            Debug.Log($"[GruaTorre] Build WebGL exitosa en '{WebGLOutputDir}' ({mb:F2} MB, {summary.totalTime.TotalSeconds:F1} s).");
        }

        /// <summary>
        /// Punto de entrada para construcción desatendida desde CLI (-executeMethod).
        /// </summary>
        public static void BuildSceneBatch()
        {
            try
            {
                BuildSceneInternal();
                ValidateSceneInternal();
                Debug.Log("[GruaTorre] BuildSceneBatch finalizado sin errores.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[GruaTorre] Error en BuildSceneBatch: " + ex);
                EditorApplication.Exit(1);
            }
        }

        private static void BuildSceneInternal()
        {
            EnsureFolder(MaterialsFolder);
            EnsureFolder(PhysicsFolder);
            EnsureFolder("Assets/Scenes");

            // Crear materiales URP y material físico de alta fricción
            Material matGround = CreateOrUpdateMaterial("Mat_Ground", new Color(0.27f, 0.29f, 0.32f), 0.0f, 0.15f);
            Material matConcrete = CreateOrUpdateMaterial("Mat_Concrete", new Color(0.58f, 0.59f, 0.60f), 0.0f, 0.12f);
            Material matCraneYellow = CreateOrUpdateMaterial("Mat_CraneYellow", new Color(0.95f, 0.72f, 0.08f), 0.25f, 0.45f);
            Material matDarkSteel = CreateOrUpdateMaterial("Mat_DarkSteel", new Color(0.18f, 0.20f, 0.23f), 0.65f, 0.55f);
            Material matCableSteel = CreateOrUpdateMaterial("Mat_CableSteel", new Color(0.32f, 0.35f, 0.39f), 0.80f, 0.60f);
            Material matCabinGlass = CreateOrUpdateMaterial("Mat_CabinGlass", new Color(0.18f, 0.45f, 0.68f), 0.15f, 0.85f);
            Material matRubberGrip = CreateOrUpdateMaterial("Mat_RubberGrip", new Color(0.10f, 0.10f, 0.11f), 0.0f, 0.10f);
            Material matPickupPad = CreateOrUpdateMaterial("Mat_PickupPad", new Color(0.24f, 0.36f, 0.48f), 0.0f, 0.20f);
            Material matDropZone = CreateOrUpdateMaterial("Mat_DropZone", new Color(0.86f, 0.42f, 0.12f), 0.10f, 0.30f);
            Material matTruckCab = CreateOrUpdateMaterial("Mat_TruckCab", new Color(0.76f, 0.22f, 0.18f), 0.30f, 0.55f);
            Material matCrateOrange = CreateOrUpdateMaterial("Mat_CrateOrange", new Color(0.92f, 0.44f, 0.10f), 0.05f, 0.25f);
            Material matCrateBlue = CreateOrUpdateMaterial("Mat_CrateBlue", new Color(0.15f, 0.50f, 0.82f), 0.05f, 0.25f);
            Material matCrateGreen = CreateOrUpdateMaterial("Mat_CrateGreen", new Color(0.18f, 0.65f, 0.36f), 0.05f, 0.25f);
            Material matCratePurple = CreateOrUpdateMaterial("Mat_CratePurple", new Color(0.56f, 0.30f, 0.72f), 0.05f, 0.25f);
            Material matTargetMarker = CreateOrUpdateMaterial("Mat_TargetMarker", new Color(0.15f, 0.95f, 0.85f), 0.0f, 0.8f);

            PhysicsMaterial gripPhysMat = CreateOrUpdateGripPhysicsMaterial();

            // Nueva escena limpia
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Iluminación
            BuildLighting();

            // Entorno de obra (suelo, zona de acopio, camión/plataforma de descarga, 3 cargas Rigidbody + CargoManager)
            BuildEnvironment(
                matGround, matConcrete, matPickupPad, matDropZone, matTruckCab, matDarkSteel,
                matCrateOrange, matCrateBlue, matCrateGreen, matCratePurple, gripPhysMat,
                out CargoDropZone dropZone, out CargoManager cargoManager);

            // Grúa torre (9 Articulation Bodies)
            TowerCraneController crane = BuildTowerCrane(
                matConcrete, matCraneYellow, matDarkSteel, matCableSteel, matCabinGlass, matRubberGrip,
                matTargetMarker, gripPhysMat);

            // Cámara principal con 3 vistas preestablecidas
            CraneCameraRig cameraRig = BuildCamera(crane);

            // UI completa (4 sliders + 2 botones de pinza + Reset + Toggle Drives + Cámaras + HUD Target/Real + Panel Cargas)
            BuildUI(crane, dropZone, cargoManager, cameraRig);

            // Guardar escena y registrarla en Build Settings (para WebGL)
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildLighting()
        {
            var sunGo = new GameObject("Directional Light");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.97f, 0.90f);
            sun.intensity = 1.6f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            var fillGo = new GameObject("Fill Light");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.75f, 0.85f, 1.0f);
            fill.intensity = 0.45f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(25f, 140f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.65f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.38f, 0.40f, 0.43f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.22f, 0.24f);
        }

        private static void BuildEnvironment(
            Material matGround,
            Material matConcrete,
            Material matPickupPad,
            Material matDropZone,
            Material matTruckCab,
            Material matDarkSteel,
            Material matCrateOrange,
            Material matCrateBlue,
            Material matCrateGreen,
            Material matCratePurple,
            PhysicsMaterial gripPhysMat,
            out CargoDropZone dropZone,
            out CargoManager cargoManager)
        {
            var envRoot = new GameObject("EntornoObra");

            // Suelo principal (superficie superior en Y = 0)
            var ground = CreateBox("Suelo", envRoot.transform, new Vector3(0f, -0.25f, 0f), new Vector3(40f, 0.5f, 40f), matGround, true);
            ground.GetComponent<Collider>().sharedMaterial = gripPhysMat;

            // Zona de Acopio (ángulo 0°, radio 6.0 m -> (0, 0, 6))
            var pickupRoot = new GameObject("ZonaAcopio");
            pickupRoot.transform.SetParent(envRoot.transform, false);
            pickupRoot.transform.position = new Vector3(0f, 0f, 6.0f);

            var pickupPad = CreateBox("PlataformaAcopio", pickupRoot.transform, new Vector3(0f, 0.04f, 0f), new Vector3(4.8f, 0.08f, 3.2f), matPickupPad, true);
            pickupPad.GetComponent<Collider>().sharedMaterial = gripPhysMat;

            // Bordes visuales de la zona de acopio
            CreateBox("BordeAcopio_N", pickupRoot.transform, new Vector3(0f, 0.09f, 1.55f), new Vector3(4.8f, 0.04f, 0.12f), matConcrete, false);
            CreateBox("BordeAcopio_S", pickupRoot.transform, new Vector3(0f, 0.09f, -1.55f), new Vector3(4.8f, 0.04f, 0.12f), matConcrete, false);

            // Zona de Descarga / Camión Plataforma (ángulo +90°, radio 7.0 m -> (7, 0, 0))
            var truckRoot = new GameObject("CamionDescarga");
            truckRoot.transform.SetParent(envRoot.transform, false);
            truckRoot.transform.position = new Vector3(7.0f, 0f, 0f);

            // Plataforma trasera del camión (superficie superior en Y = 0.60 m)
            var bed = CreateBox("PlataformaCamion", truckRoot.transform, new Vector3(0f, 0.42f, 0f), new Vector3(3.4f, 0.36f, 4.8f), matDropZone, true);
            bed.GetComponent<Collider>().sharedMaterial = gripPhysMat;

            // Chasis y ruedas visuales del camión
            CreateBox("ChasisCamion", truckRoot.transform, new Vector3(0f, 0.18f, -0.4f), new Vector3(2.4f, 0.24f, 6.2f), matDarkSteel, false);
            CreateBox("CabinaCamion", truckRoot.transform, new Vector3(0f, 1.10f, -3.35f), new Vector3(2.5f, 1.65f, 1.8f), matTruckCab, true);
            CreateCylinder("Rueda_FL", truckRoot.transform, new Vector3(-1.25f, 0.35f, -3.2f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.70f, 0.18f, 0.70f), matDarkSteel, false);
            CreateCylinder("Rueda_FR", truckRoot.transform, new Vector3(1.25f, 0.35f, -3.2f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.70f, 0.18f, 0.70f), matDarkSteel, false);
            CreateCylinder("Rueda_RL", truckRoot.transform, new Vector3(-1.25f, 0.35f, 1.4f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.70f, 0.18f, 0.70f), matDarkSteel, false);
            CreateCylinder("Rueda_RR", truckRoot.transform, new Vector3(1.25f, 0.35f, 1.4f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.70f, 0.18f, 0.70f), matDarkSteel, false);

            // Topes laterales bajos en la plataforma para evitar que una carga ruede fuera
            CreateBox("Baranda_Izq", truckRoot.transform, new Vector3(-1.66f, 0.68f, 0f), new Vector3(0.08f, 0.16f, 4.8f), matDarkSteel, true);
            CreateBox("Baranda_Der", truckRoot.transform, new Vector3(1.66f, 0.68f, 0f), new Vector3(0.08f, 0.16f, 4.8f), matDarkSteel, true);
            CreateBox("Baranda_Tras", truckRoot.transform, new Vector3(0f, 0.68f, 2.36f), new Vector3(3.4f, 0.16f, 0.08f), matDarkSteel, true);

            // Trigger CargoDropZone sobre la plataforma
            var zoneGo = new GameObject("ZonaObjetivoTrigger");
            zoneGo.transform.SetParent(truckRoot.transform, false);
            zoneGo.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            var zoneBox = zoneGo.AddComponent<BoxCollider>();
            zoneBox.isTrigger = true;
            zoneBox.size = new Vector3(3.2f, 3.5f, 4.6f);
            dropZone = zoneGo.AddComponent<CargoDropZone>();

            // 3 cargas Rigidbody comunes (0.90 m x 0.90 m x 0.90 m, 25 kg cada una)
            var cratesRoot = new GameObject("CargasRigidbody");
            cratesRoot.transform.SetParent(envRoot.transform, false);

            var crate1 = CreateCargoCrate("Carga_01_Naranja", cratesRoot.transform, new Vector3(0.0f, 0.54f, 6.0f), matCrateOrange, matDarkSteel, gripPhysMat);
            var crate2 = CreateCargoCrate("Carga_02_Azul", cratesRoot.transform, new Vector3(-1.45f, 0.54f, 5.85f), matCrateBlue, matDarkSteel, gripPhysMat);
            var crate3 = CreateCargoCrate("Carga_03_Verde", cratesRoot.transform, new Vector3(1.45f, 0.54f, 5.85f), matCrateGreen, matDarkSteel, gripPhysMat);

            dropZone.trackedCrates = new List<CargoCrate> { crate1, crate2, crate3 };

            cargoManager = cratesRoot.AddComponent<CargoManager>();
            cargoManager.dropZone = dropZone;
            cargoManager.cratesParent = cratesRoot.transform;
            cargoManager.paletteMaterials = new[] { matCrateOrange, matCrateBlue, matCrateGreen, matCratePurple };
            cargoManager.trimMaterial = matDarkSteel;
            cargoManager.gripPhysicsMaterial = gripPhysMat;
            cargoManager.pickupZoneCenter = new Vector3(0f, 0.08f, 6.0f);
            cargoManager.maxTotalCrates = 10;
        }

        private static CargoCrate CreateCargoCrate(
            string name,
            Transform parent,
            Vector3 worldPos,
            Material mainMat,
            Material trimMat,
            PhysicsMaterial physMat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.localScale = Vector3.one;

            go.AddComponent<Rigidbody>();

            var crate = go.AddComponent<CargoCrate>();
            crate.cargoName = name;
            crate.isInitialCrate = true;
            crate.ConfigureShape(CargoShape.Cube, mainMat, trimMat, physMat, adjustHeightOnSwap: false);
            return crate;
        }

        private static TowerCraneController BuildTowerCrane(
            Material matConcrete,
            Material matCraneYellow,
            Material matDarkSteel,
            Material matCableSteel,
            Material matCabinGlass,
            Material matRubberGrip,
            Material matTargetMarker,
            PhysicsMaterial gripPhysMat)
        {
            Quaternion rotY = Quaternion.FromToRotation(Vector3.right, Vector3.up);
            Quaternion rotDown = Quaternion.FromToRotation(Vector3.right, Vector3.down);

            // =========================================================================
            // 1. TorreBase (Root, immovable = true) — 2000 kg
            // =========================================================================
            var torreBaseGo = new GameObject("TorreBase");
            torreBaseGo.transform.position = Vector3.zero;
            torreBaseGo.transform.rotation = Quaternion.identity;
            torreBaseGo.transform.localScale = Vector3.one;

            var torreBaseAb = torreBaseGo.AddComponent<ArticulationBody>();
            torreBaseAb.immovable = true;
            torreBaseAb.mass = 2000f;
            torreBaseAb.solverIterations = 20;
            torreBaseAb.solverVelocityIterations = 8;

            // Zapata de hormigón (collider + visual)
            CreateBox("ZapataHormigon", torreBaseGo.transform, new Vector3(0f, 0.40f, 0f), new Vector3(3.0f, 0.80f, 3.0f), matConcrete, true);
            // Mástil vertical con collider limpio hasta Y = 10.90 (10 cm de holgura con Pluma)
            var mastColGo = new GameObject("MastilCollider");
            mastColGo.transform.SetParent(torreBaseGo.transform, false);
            var mastCol = mastColGo.AddComponent<BoxCollider>();
            mastCol.center = new Vector3(0f, 5.85f, 0f);
            mastCol.size = new Vector3(0.90f, 10.10f, 0.90f);

            // Visual celosía metálica del mástil (4 columnas + anillos)
            float colOff = 0.38f;
            CreateBox("Col_NE", torreBaseGo.transform, new Vector3(colOff, 5.85f, colOff), new Vector3(0.14f, 10.1f, 0.14f), matCraneYellow, false);
            CreateBox("Col_NW", torreBaseGo.transform, new Vector3(-colOff, 5.85f, colOff), new Vector3(0.14f, 10.1f, 0.14f), matCraneYellow, false);
            CreateBox("Col_SE", torreBaseGo.transform, new Vector3(colOff, 5.85f, -colOff), new Vector3(0.14f, 10.1f, 0.14f), matCraneYellow, false);
            CreateBox("Col_SW", torreBaseGo.transform, new Vector3(-colOff, 5.85f, -colOff), new Vector3(0.14f, 10.1f, 0.14f), matCraneYellow, false);
            for (int level = 1; level <= 9; level++)
            {
                float y = 0.8f + level * 1.1f;
                CreateBox($"Travesano_{level}", torreBaseGo.transform, new Vector3(0f, y, 0f), new Vector3(0.92f, 0.10f, 0.92f), matDarkSteel, false);
            }
            // Corona de giro
            CreateCylinder("CoronaGiro", torreBaseGo.transform, new Vector3(0f, 10.92f, 0f), Quaternion.identity, new Vector3(1.15f, 0.08f, 1.15f), matDarkSteel, false);

            // =========================================================================
            // 2. Pluma (RevoluteJoint - Drive 1: Giro horizontal ±180°) — 600 kg
            // =========================================================================
            var plumaGo = new GameObject("Pluma");
            plumaGo.transform.SetParent(torreBaseGo.transform, false);
            plumaGo.transform.localPosition = new Vector3(0f, 11.00f, 0f);
            plumaGo.transform.localRotation = Quaternion.identity;
            plumaGo.transform.localScale = Vector3.one;

            var plumaAb = plumaGo.AddComponent<ArticulationBody>();
            plumaAb.jointType = ArticulationJointType.RevoluteJoint;
            plumaAb.mass = 600f;
            plumaAb.linearDamping = 0.05f;
            plumaAb.angularDamping = 0.2f;
            plumaAb.matchAnchors = false;
            plumaAb.anchorPosition = Vector3.zero;
            plumaAb.parentAnchorPosition = new Vector3(0f, 11.00f, 0f);
            plumaAb.anchorRotation = rotY;
            plumaAb.parentAnchorRotation = rotY;
            plumaAb.twistLock = ArticulationDofLock.LimitedMotion;
            plumaAb.xDrive = new ArticulationDrive
            {
                lowerLimit = -180f,
                upperLimit = 180f,
                stiffness = 60000f,
                damping = 38000f,
                forceLimit = 50000f,
                target = 0f,
                driveType = ArticulationDriveType.Force
            };

            // Colliders y visuales de Pluma (desde localY = +0.06 hacia arriba para dejar holgura con Carro y TorreBase)
            CreateBox("VigaPlumaPrincipal", plumaGo.transform, new Vector3(0f, 0.42f, 5.75f), new Vector3(0.75f, 0.70f, 11.5f), matCraneYellow, true);
            CreateBox("VigaContrapluma", plumaGo.transform, new Vector3(0f, 0.35f, -1.65f), new Vector3(0.75f, 0.55f, 3.30f), matCraneYellow, true);
            // Cúspide y tirantes (solo visuales)
            CreateBox("Cuspide", plumaGo.transform, new Vector3(0f, 1.55f, 0f), new Vector3(0.55f, 1.80f, 0.55f), matCraneYellow, false);
            CreateBeamBetween("TiranteDelantero", plumaGo.transform, new Vector3(0f, 2.35f, 0f), new Vector3(0f, 0.75f, 8.8f), 0.06f, matCableSteel);
            CreateBeamBetween("TiranteTrasero", plumaGo.transform, new Vector3(0f, 2.35f, 0f), new Vector3(0f, 0.65f, -4.0f), 0.06f, matCableSteel);
            // Cabina de operador (visual)
            CreateBox("CabinaEstructura", plumaGo.transform, new Vector3(0.85f, 0.35f, 0.45f), new Vector3(0.95f, 1.25f, 1.35f), matCraneYellow, false);
            CreateBox("CabinaVidrio", plumaGo.transform, new Vector3(0.88f, 0.42f, 0.90f), new Vector3(0.85f, 0.85f, 0.55f), matCabinGlass, false);

            // =========================================================================
            // 3. Contrapeso (FixedJoint - 4º tipo de joint) — 500 kg
            // =========================================================================
            var contrapesoGo = new GameObject("Contrapeso");
            contrapesoGo.transform.SetParent(plumaGo.transform, false);
            contrapesoGo.transform.localPosition = new Vector3(0f, 0.30f, -4.00f);
            contrapesoGo.transform.localRotation = Quaternion.identity;
            contrapesoGo.transform.localScale = Vector3.one;

            var contrapesoAb = contrapesoGo.AddComponent<ArticulationBody>();
            contrapesoAb.jointType = ArticulationJointType.FixedJoint;
            contrapesoAb.mass = 500f;
            contrapesoAb.matchAnchors = false;
            contrapesoAb.anchorPosition = Vector3.zero;
            contrapesoAb.parentAnchorPosition = new Vector3(0f, 0.30f, -4.00f);
            contrapesoAb.anchorRotation = Quaternion.identity;
            contrapesoAb.parentAnchorRotation = Quaternion.identity;

            CreateBox("BloqueContrapeso", contrapesoGo.transform, Vector3.zero, new Vector3(1.50f, 1.15f, 1.20f), matConcrete, true);

            // =========================================================================
            // 4. Carro (PrismaticJoint - Drive 2: Avance radial 0 a 8 m) — 200 kg
            // =========================================================================
            var carroGo = new GameObject("Carro");
            carroGo.transform.SetParent(plumaGo.transform, false);
            carroGo.transform.localPosition = new Vector3(0f, -0.30f, 2.50f);
            carroGo.transform.localRotation = Quaternion.identity;
            carroGo.transform.localScale = Vector3.one;

            var carroAb = carroGo.AddComponent<ArticulationBody>();
            carroAb.jointType = ArticulationJointType.PrismaticJoint;
            carroAb.mass = 200f;
            carroAb.linearDamping = 0.05f;
            carroAb.angularDamping = 0.1f;
            carroAb.matchAnchors = false;
            carroAb.anchorPosition = Vector3.zero;
            carroAb.parentAnchorPosition = new Vector3(0f, -0.30f, 2.50f);
            carroAb.anchorRotation = Quaternion.identity;
            carroAb.parentAnchorRotation = Quaternion.identity;
            carroAb.linearLockX = ArticulationDofLock.LockedMotion;
            carroAb.linearLockY = ArticulationDofLock.LockedMotion;
            carroAb.linearLockZ = ArticulationDofLock.LimitedMotion;
            carroAb.zDrive = new ArticulationDrive
            {
                lowerLimit = 0f,
                upperLimit = 8.0f,
                stiffness = 8000f,
                damping = 4000f,
                forceLimit = 6000f,
                target = 3.5f,
                driveType = ArticulationDriveType.Force
            };

            CreateBox("ChasisCarro", carroGo.transform, Vector3.zero, new Vector3(1.05f, 0.44f, 1.25f), matDarkSteel, true);
            var winchAnchor = new GameObject("AnclaCableSuperior");
            winchAnchor.transform.SetParent(carroGo.transform, false);
            winchAnchor.transform.localPosition = new Vector3(0f, -0.22f, 0f);

            // =========================================================================
            // 5. Izaje (PrismaticJoint - Drive 3: Subida/bajada -6.55 a 0 m) — 120 kg
            // =========================================================================
            var izajeGo = new GameObject("Izaje");
            izajeGo.transform.SetParent(carroGo.transform, false);
            izajeGo.transform.localPosition = new Vector3(0f, -0.60f, 0f);
            izajeGo.transform.localRotation = Quaternion.identity;
            izajeGo.transform.localScale = Vector3.one;

            var izajeAb = izajeGo.AddComponent<ArticulationBody>();
            izajeAb.jointType = ArticulationJointType.PrismaticJoint;
            izajeAb.mass = 120f;
            izajeAb.linearDamping = 0.05f;
            izajeAb.angularDamping = 0.1f;
            izajeAb.matchAnchors = false;
            izajeAb.anchorPosition = Vector3.zero;
            izajeAb.parentAnchorPosition = new Vector3(0f, -0.60f, 0f);
            izajeAb.anchorRotation = Quaternion.identity;
            izajeAb.parentAnchorRotation = Quaternion.identity;
            izajeAb.linearLockX = ArticulationDofLock.LockedMotion;
            izajeAb.linearLockY = ArticulationDofLock.LimitedMotion;
            izajeAb.linearLockZ = ArticulationDofLock.LockedMotion;
            izajeAb.yDrive = new ArticulationDrive
            {
                lowerLimit = -6.55f,
                upperLimit = 0f,
                stiffness = 60000f,
                damping = 12000f,
                forceLimit = 25000f,
                target = -2.0f,
                driveType = ArticulationDriveType.Force
            };

            CreateBox("BloquePolipasto", izajeGo.transform, Vector3.zero, new Vector3(0.45f, 0.34f, 0.45f), matCraneYellow, true);
            var hoistTopAnchor = new GameObject("AnclaCableInferior");
            hoistTopAnchor.transform.SetParent(izajeGo.transform, false);
            hoistTopAnchor.transform.localPosition = new Vector3(0f, 0.17f, 0f);

            // Cable visual dinámico entre Carro e Izaje (fuera de los cuerpos para que no afecte física)
            var hoistCableVisGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hoistCableVisGo.name = "CableIzajeVisual";
            UnityEngine.Object.DestroyImmediate(hoistCableVisGo.GetComponent<Collider>());
            hoistCableVisGo.GetComponent<Renderer>().sharedMaterial = matCableSteel;
            var hoistCableVis = hoistCableVisGo.AddComponent<HoistCableVisual>();
            hoistCableVis.topAnchor = winchAnchor.transform;
            hoistCableVis.bottomAnchor = hoistTopAnchor.transform;
            hoistCableVis.cableRadius = 0.045f;

            // =========================================================================
            // 6. CablePendulo (SphericalJoint - PASIVO sin stiffness) — 60 kg
            // =========================================================================
            var cableGo = new GameObject("CablePendulo");
            cableGo.transform.SetParent(izajeGo.transform, false);
            cableGo.transform.localPosition = Vector3.zero;
            cableGo.transform.localRotation = Quaternion.identity;
            cableGo.transform.localScale = Vector3.one;

            var cableAb = cableGo.AddComponent<ArticulationBody>();
            cableAb.jointType = ArticulationJointType.SphericalJoint;
            cableAb.mass = 60f;
            cableAb.linearDamping = 0.02f;
            cableAb.angularDamping = 0.08f;
            cableAb.matchAnchors = false;
            cableAb.anchorPosition = Vector3.zero;
            cableAb.parentAnchorPosition = Vector3.zero;
            cableAb.anchorRotation = rotDown;
            cableAb.parentAnchorRotation = rotDown;
            cableAb.twistLock = ArticulationDofLock.LimitedMotion;
            cableAb.swingYLock = ArticulationDofLock.LimitedMotion;
            cableAb.swingZLock = ArticulationDofLock.LimitedMotion;
            // PASIVO: stiffness = 0 en los 3 ejes (se mueve solo por gravedad, inercia y contacto)
            cableAb.xDrive = new ArticulationDrive
            {
                lowerLimit = -20f,
                upperLimit = 20f,
                stiffness = 0f,
                damping = 15f,
                forceLimit = 10000f,
                driveType = ArticulationDriveType.Force
            };
            cableAb.yDrive = new ArticulationDrive
            {
                lowerLimit = -35f,
                upperLimit = 35f,
                stiffness = 0f,
                damping = 25f,
                forceLimit = 10000f,
                driveType = ArticulationDriveType.Force
            };
            cableAb.zDrive = new ArticulationDrive
            {
                lowerLimit = -35f,
                upperLimit = 35f,
                stiffness = 0f,
                damping = 25f,
                forceLimit = 10000f,
                driveType = ArticulationDriveType.Force
            };

            // Barra/cable articulado de 2.20 m (collider de -0.25 a -2.10 para dejar holgura arriba y abajo)
            var cableColGo = new GameObject("CableCollider");
            cableColGo.transform.SetParent(cableGo.transform, false);
            var cableCol = cableColGo.AddComponent<CapsuleCollider>();
            cableCol.center = new Vector3(0f, -1.18f, 0f);
            cableCol.radius = 0.06f;
            cableCol.height = 1.84f;

            CreateCylinder("CableBarraVisual", cableGo.transform, new Vector3(0f, -1.10f, 0f), Quaternion.identity, new Vector3(0.11f, 1.10f, 0.11f), matCableSteel, false);
            CreateSphere("RotulaVisual", cableGo.transform, new Vector3(0f, -0.18f, 0f), 0.16f, matDarkSteel);

            // =========================================================================
            // 7. CabezalPinza (RevoluteJoint - Drive 4: Rotación pinza ±180°) — 50 kg
            // =========================================================================
            var cabezalGo = new GameObject("CabezalPinza");
            cabezalGo.transform.SetParent(cableGo.transform, false);
            cabezalGo.transform.localPosition = new Vector3(0f, -2.20f, 0f);
            cabezalGo.transform.localRotation = Quaternion.identity;
            cabezalGo.transform.localScale = Vector3.one;

            var cabezalAb = cabezalGo.AddComponent<ArticulationBody>();
            cabezalAb.jointType = ArticulationJointType.RevoluteJoint;
            cabezalAb.mass = 50f;
            cabezalAb.linearDamping = 0.05f;
            cabezalAb.angularDamping = 0.15f;
            cabezalAb.matchAnchors = false;
            cabezalAb.anchorPosition = Vector3.zero;
            cabezalAb.parentAnchorPosition = new Vector3(0f, -2.20f, 0f);
            cabezalAb.anchorRotation = rotY;
            cabezalAb.parentAnchorRotation = rotY;
            cabezalAb.twistLock = ArticulationDofLock.LimitedMotion;
            cabezalAb.xDrive = new ArticulationDrive
            {
                lowerLimit = -180f,
                upperLimit = 180f,
                stiffness = 1200f,
                damping = 300f,
                forceLimit = 1500f,
                target = 0f,
                driveType = ArticulationDriveType.Force
            };

            // Viga del cabezal (de -0.02 a -0.32 -> holgura con CablePendulo y Mordazas)
            CreateBox("VigaCabezal", cabezalGo.transform, new Vector3(0f, -0.17f, 0f), new Vector3(1.75f, 0.30f, 0.55f), matCraneYellow, true);
            CreateCylinder("RotorCabezal", cabezalGo.transform, new Vector3(0f, -0.02f, 0f), Quaternion.identity, new Vector3(0.40f, 0.08f, 0.40f), matDarkSteel, false);

            // =========================================================================
            // 8. MordazaIzq (PrismaticJoint - Drive 5: Pinza Izquierda) — 20 kg
            // =========================================================================
            var mordazaIzqGo = new GameObject("MordazaIzq");
            mordazaIzqGo.transform.SetParent(cabezalGo.transform, false);
            mordazaIzqGo.transform.localPosition = new Vector3(-0.75f, -0.35f, 0f);
            mordazaIzqGo.transform.localRotation = Quaternion.identity;
            mordazaIzqGo.transform.localScale = Vector3.one;

            var mordazaIzqAb = mordazaIzqGo.AddComponent<ArticulationBody>();
            mordazaIzqAb.jointType = ArticulationJointType.PrismaticJoint;
            mordazaIzqAb.mass = 20f;
            mordazaIzqAb.linearDamping = 0.05f;
            mordazaIzqAb.angularDamping = 0.1f;
            mordazaIzqAb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            mordazaIzqAb.matchAnchors = false;
            mordazaIzqAb.anchorPosition = Vector3.zero;
            mordazaIzqAb.parentAnchorPosition = new Vector3(-0.75f, -0.35f, 0f);
            mordazaIzqAb.anchorRotation = Quaternion.identity;
            mordazaIzqAb.parentAnchorRotation = Quaternion.identity;
            mordazaIzqAb.linearLockX = ArticulationDofLock.LimitedMotion;
            mordazaIzqAb.linearLockY = ArticulationDofLock.LockedMotion;
            mordazaIzqAb.linearLockZ = ArticulationDofLock.LockedMotion;
            mordazaIzqAb.xDrive = new ArticulationDrive
            {
                lowerLimit = 0f,
                upperLimit = 0.52f,
                stiffness = 12000f,
                damping = 850f,
                forceLimit = 750f,
                target = 0f,
                driveType = ArticulationDriveType.Force
            };

            BuildJawGeometry(mordazaIzqGo.transform, +1f, matDarkSteel, matRubberGrip, gripPhysMat);

            // =========================================================================
            // 9. MordazaDer (PrismaticJoint - Drive 6: Pinza Derecha) — 20 kg
            // =========================================================================
            var mordazaDerGo = new GameObject("MordazaDer");
            mordazaDerGo.transform.SetParent(cabezalGo.transform, false);
            mordazaDerGo.transform.localPosition = new Vector3(0.75f, -0.35f, 0f);
            mordazaDerGo.transform.localRotation = Quaternion.identity;
            mordazaDerGo.transform.localScale = Vector3.one;

            var mordazaDerAb = mordazaDerGo.AddComponent<ArticulationBody>();
            mordazaDerAb.jointType = ArticulationJointType.PrismaticJoint;
            mordazaDerAb.mass = 20f;
            mordazaDerAb.linearDamping = 0.05f;
            mordazaDerAb.angularDamping = 0.1f;
            mordazaDerAb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            mordazaDerAb.matchAnchors = false;
            mordazaDerAb.anchorPosition = Vector3.zero;
            mordazaDerAb.parentAnchorPosition = new Vector3(0.75f, -0.35f, 0f);
            mordazaDerAb.anchorRotation = Quaternion.identity;
            mordazaDerAb.parentAnchorRotation = Quaternion.identity;
            mordazaDerAb.linearLockX = ArticulationDofLock.LimitedMotion;
            mordazaDerAb.linearLockY = ArticulationDofLock.LockedMotion;
            mordazaDerAb.linearLockZ = ArticulationDofLock.LockedMotion;
            mordazaDerAb.xDrive = new ArticulationDrive
            {
                lowerLimit = -0.52f,
                upperLimit = 0f,
                stiffness = 12000f,
                damping = 850f,
                forceLimit = 750f,
                target = 0f,
                driveType = ArticulationDriveType.Force
            };

            BuildJawGeometry(mordazaDerGo.transform, -1f, matDarkSteel, matRubberGrip, gripPhysMat);

            // Marcador láser/guía de alineación debajo de la pinza (solo visual, sin collider)
            var markerGo = new GameObject("MarcadorAlineacionPinza");
            CreateBox("Reticle_A", markerGo.transform, Vector3.zero, new Vector3(0.95f, 0.02f, 0.08f), matTargetMarker, false);
            CreateBox("Reticle_B", markerGo.transform, Vector3.zero, new Vector3(0.08f, 0.02f, 0.95f), matTargetMarker, false);
            var marker = markerGo.AddComponent<DropTargetMarker>();
            marker.gripperHead = cabezalGo.transform;

            // Controlador principal en la raíz
            var controller = torreBaseGo.AddComponent<TowerCraneController>();
            controller.torreBase = torreBaseAb;
            controller.pluma = plumaAb;
            controller.contrapeso = contrapesoAb;
            controller.carro = carroAb;
            controller.izaje = izajeAb;
            controller.cablePendulo = cableAb;
            controller.cabezalPinza = cabezalAb;
            controller.mordazaIzq = mordazaIzqAb;
            controller.mordazaDer = mordazaDerAb;

            return controller;
        }

        private static void BuildJawGeometry(
            Transform jawRoot,
            float inwardSign,
            Material matSteel,
            Material matRubber,
            PhysicsMaterial gripPhysMat)
        {
            // Brazo metálico exterior + almohadilla de goma interior (de localY = -0.05 a -0.82)
            // Collider único limpio en la almohadilla de agarre para contacto estable sin enganchen
            var padColGo = new GameObject("AlmohadillaCollider");
            padColGo.transform.SetParent(jawRoot, false);
            var col = padColGo.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.44f, 0f);
            col.size = new Vector3(0.15f, 0.76f, 0.72f);
            col.sharedMaterial = gripPhysMat;

            CreateBox("BrazoMordaza", jawRoot, new Vector3(-inwardSign * 0.03f, -0.43f, 0f), new Vector3(0.09f, 0.78f, 0.52f), matSteel, false);
            CreateBox("GomaAgarre", jawRoot, new Vector3(inwardSign * 0.045f, -0.45f, 0f), new Vector3(0.06f, 0.70f, 0.72f), matRubber, false);
        }

        private static CraneCameraRig BuildCamera(TowerCraneController crane)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.56f, 0.73f, 0.89f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 150f;
            camGo.AddComponent<AudioListener>();

            var rig = camGo.AddComponent<CraneCameraRig>();
            rig.jibTransform = crane.pluma.transform;
            rig.trolleyTransform = crane.carro.transform;
            rig.gripperHeadTransform = crane.cabezalPinza.transform;

            camGo.transform.position = rig.overviewPosition;
            camGo.transform.LookAt(rig.overviewLookTarget);

            return rig;
        }

        private static void BuildUI(TowerCraneController crane, CargoDropZone dropZone, CargoManager cargoManager, CraneCameraRig cameraRig)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // EventSystem compatible con Unity 6 Input System
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            var inputModule = esGo.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();

            // Canvas responsive (1280x720)
            var canvasGo = new GameObject("SimuladorCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var ui = canvasGo.AddComponent<TowerCraneUI>();
            ui.crane = crane;
            ui.dropZone = dropZone;
            ui.cargoManager = cargoManager;
            ui.cameraRig = cameraRig;

            Color panelBg = new Color(0.08f, 0.10f, 0.14f, 0.82f);

            // === Panel Izquierdo: Controles (4 Sliders + 2 Botones Pinza + Reset + Toggle Drives) ===
            RectTransform leftPanel = CreatePanel("PanelControles", canvasGo.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(14f, -14f), new Vector2(325f, 455f), panelBg);

            CreateLabel(leftPanel, "TituloControles", "CONTROLES GRÚA TORRE", font, 16, FontStyle.Bold,
                new Color(0.98f, 0.80f, 0.25f), TextAnchor.MiddleLeft, new Vector2(14f, -12f), new Vector2(295f, 26f));

            ui.slewSlider = CreateLabeledSlider(leftPanel, "SliderGiro", "1. Giro Pluma (°)", font, -48f, out ui.slewValueText);
            ui.trolleySlider = CreateLabeledSlider(leftPanel, "SliderCarro", "2. Carro Radial (m)", font, -108f, out ui.trolleyValueText);
            ui.hoistSlider = CreateLabeledSlider(leftPanel, "SliderIzaje", "3. Izaje / Polipasto (m)", font, -168f, out ui.hoistValueText);
            ui.headSlider = CreateLabeledSlider(leftPanel, "SliderCabezal", "4. Rotación Pinza (°)", font, -228f, out ui.headValueText);

            // Botones de Pinza (Abrir / Cerrar)
            ui.openGripperButton = CreateButton(leftPanel, "BtnAbrirPinza", "Abrir Pinza", font,
                new Vector2(14f, -296f), new Vector2(142f, 38f), new Color(0.18f, 0.56f, 0.34f));
            ui.closeGripperButton = CreateButton(leftPanel, "BtnCerrarPinza", "Cerrar Pinza", font,
                new Vector2(168f, -296f), new Vector2(142f, 38f), new Color(0.76f, 0.42f, 0.12f));

            ui.gripperStateText = CreateLabel(leftPanel, "EstadoPinza", "Estado pinza: ABIERTA", font, 14, FontStyle.Bold,
                new Color(0.55f, 0.92f, 0.65f), TextAnchor.MiddleCenter, new Vector2(14f, -340f), new Vector2(296f, 24f));

            // Toggle Drives (Paso 5: prueba de caída por gravedad)
            ui.drivesToggle = CreateToggle(leftPanel, "ToggleDrives", "Drives Activos (destildar = gravedad)", font,
                new Vector2(14f, -372f), new Vector2(296f, 26f));

            // Botón Reset
            ui.resetButton = CreateButton(leftPanel, "BtnReset", "Reiniciar Grúa y Cargas", font,
                new Vector2(14f, -406f), new Vector2(296f, 36f), new Color(0.28f, 0.34f, 0.44f));

            // === Panel Superior Central: Objetivo + Selector de Cámaras ===
            RectTransform topPanel = CreatePanel("PanelSuperior", canvasGo.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -14f), new Vector2(540f, 102f), panelBg);

            ui.taskStatusText = CreateLabel(topPanel, "EstadoTarea", "Cargando tarea...", font, 14, FontStyle.Normal,
                Color.white, TextAnchor.UpperCenter, new Vector2(12f, -8f), new Vector2(516f, 48f));
            ui.taskStatusText.supportRichText = true;

            ui.camOverviewButton = CreateButton(topPanel, "BtnCamGeneral", "Vista General", font,
                new Vector2(16f, -60f), new Vector2(162f, 32f), new Color(0.20f, 0.40f, 0.62f));
            ui.camJibButton = CreateButton(topPanel, "BtnCamPluma", "Vista Pluma", font,
                new Vector2(189f, -60f), new Vector2(162f, 32f), new Color(0.20f, 0.40f, 0.62f));
            ui.camGripperButton = CreateButton(topPanel, "BtnCamPinza", "Vista Pinza", font,
                new Vector2(362f, -60f), new Vector2(162f, 32f), new Color(0.20f, 0.40f, 0.62f));

            // === Panel Derecho: Lectura de Estado (Target vs Real por Joint) ===
            RectTransform rightPanel = CreatePanel("PanelTelemetria", canvasGo.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-14f, -14f), new Vector2(325f, 370f), panelBg);

            ui.telemetryText = CreateLabel(rightPanel, "TextoTelemetria", "Telemetría...", font, 13, FontStyle.Normal,
                new Color(0.92f, 0.95f, 0.98f), TextAnchor.UpperLeft, new Vector2(14f, -12f), new Vector2(297f, 346f));
            ui.telemetryText.supportRichText = true;
            ui.telemetryText.lineSpacing = 1.15f;

            // === Panel Derecho Inferior: Gestión de Cargas (Agregar objetos y cambiar forma) ===
            RectTransform cargoPanel = CreatePanel("PanelCargas", canvasGo.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-14f, -396f), new Vector2(325f, 174f), panelBg);

            CreateLabel(cargoPanel, "TituloCargas", "CARGAS (RIGIDBODY)", font, 15, FontStyle.Bold,
                new Color(0.98f, 0.80f, 0.25f), TextAnchor.MiddleLeft, new Vector2(14f, -10f), new Vector2(296f, 24f));

            ui.prevShapeButton = CreateButton(cargoPanel, "BtnFormaAnterior", "<", font,
                new Vector2(14f, -40f), new Vector2(38f, 32f), new Color(0.26f, 0.32f, 0.42f));

            ui.selectedShapeText = CreateLabel(cargoPanel, "TextoFormaActual", "Cubo (25 kg)", font, 14, FontStyle.Bold,
                Color.white, TextAnchor.MiddleCenter, new Vector2(56f, -40f), new Vector2(212f, 32f));

            ui.nextShapeButton = CreateButton(cargoPanel, "BtnFormaSiguiente", ">", font,
                new Vector2(272f, -40f), new Vector2(38f, 32f), new Color(0.26f, 0.32f, 0.42f));

            ui.spawnCrateButton = CreateButton(cargoPanel, "BtnAgregarCarga", "+ Agregar Carga", font,
                new Vector2(14f, -82f), new Vector2(142f, 36f), new Color(0.16f, 0.56f, 0.46f));

            ui.applyShapeButton = CreateButton(cargoPanel, "BtnCambiarForma", "Cambiar Forma", font,
                new Vector2(168f, -82f), new Vector2(142f, 36f), new Color(0.22f, 0.46f, 0.72f));

            ui.removeCrateButton = CreateButton(cargoPanel, "BtnQuitarCarga", "Quitar Última", font,
                new Vector2(14f, -128f), new Vector2(142f, 32f), new Color(0.55f, 0.26f, 0.26f));

            ui.cargoCountText = CreateLabel(cargoPanel, "TextoContadorCargas", "Activas: 3 / 10", font, 13, FontStyle.Bold,
                new Color(0.75f, 0.90f, 1f), TextAnchor.MiddleRight, new Vector2(168f, -128f), new Vector2(142f, 32f));
        }

        // === Helpers de UI ===

        private static RectTransform CreatePanel(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 size,
            Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            return rt;
        }

        private static Text CreateLabel(
            Transform parent,
            string name,
            string text,
            Font font,
            int fontSize,
            FontStyle style,
            Color color,
            TextAnchor align,
            Vector2 posFromTopLeft,
            Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = posFromTopLeft;
            rt.sizeDelta = size;

            var txt = go.AddComponent<Text>();
            txt.font = font;
            txt.text = text;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = align;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            return txt;
        }

        private static Slider CreateLabeledSlider(
            Transform parent,
            string name,
            string label,
            Font font,
            float topY,
            out Text valueText)
        {
            CreateLabel(parent, name + "_Label", label, font, 14, FontStyle.Normal,
                Color.white, TextAnchor.MiddleLeft, new Vector2(14f, topY), new Vector2(210f, 22f));

            valueText = CreateLabel(parent, name + "_Value", "0.0", font, 14, FontStyle.Bold,
                new Color(0.55f, 0.88f, 1f), TextAnchor.MiddleRight, new Vector2(224f, topY), new Vector2(86f, 22f));

            var sliderGo = new GameObject(name);
            sliderGo.transform.SetParent(parent, false);
            var rt = sliderGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(14f, topY - 26f);
            rt.sizeDelta = new Vector2(296f, 24f);

            var slider = sliderGo.AddComponent<Slider>();

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.30f);
            bgRt.anchorMax = new Vector2(1f, 0.70f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.24f, 0.28f, 0.34f, 1f);

            // Fill Area
            var fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRt = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0.30f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.70f);
            fillAreaRt.offsetMin = new Vector2(6f, 0f);
            fillAreaRt.offsetMax = new Vector2(-6f, 0f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.95f, 0.72f, 0.12f, 1f);

            // Handle Slide Area
            var handleAreaGo = new GameObject("Handle Slide Area");
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            var handleAreaRt = handleAreaGo.AddComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(10f, 0f);
            handleAreaRt.offsetMax = new Vector2(-10f, 0f);

            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleRt = handleGo.AddComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(20f, 24f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = Color.white;

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;

            return slider;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Font font,
            Vector2 posFromTopLeft,
            Vector2 size,
            Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = posFromTopLeft;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var txt = txtGo.AddComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            return btn;
        }

        private static Toggle CreateToggle(
            Transform parent,
            string name,
            string label,
            Font font,
            Vector2 posFromTopLeft,
            Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = posFromTopLeft;
            rt.sizeDelta = size;

            var toggle = go.AddComponent<Toggle>();
            toggle.isOn = true;

            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.5f);
            bgRt.anchorMax = new Vector2(0f, 0.5f);
            bgRt.pivot = new Vector2(0f, 0.5f);
            bgRt.anchoredPosition = Vector2.zero;
            bgRt.sizeDelta = new Vector2(20f, 20f);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.25f, 0.28f, 0.34f, 1f);

            var checkGo = new GameObject("Checkmark");
            checkGo.transform.SetParent(bgGo.transform, false);
            var checkRt = checkGo.AddComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.18f, 0.18f);
            checkRt.anchorMax = new Vector2(0.82f, 0.82f);
            checkRt.offsetMin = Vector2.zero;
            checkRt.offsetMax = Vector2.zero;
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = new Color(0.40f, 0.90f, 0.45f, 1f);

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lblRt = lblGo.AddComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = new Vector2(28f, 0f);
            lblRt.offsetMax = Vector2.zero;
            var lbl = lblGo.AddComponent<Text>();
            lbl.font = font;
            lbl.text = label;
            lbl.fontSize = 13;
            lbl.color = Color.white;
            lbl.alignment = TextAnchor.MiddleLeft;

            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;
            return toggle;
        }

        // === Helpers de Geometría y Assets ===

        private static GameObject CreateBox(
            string name,
            Transform parent,
            Vector3 localPos,
            Vector3 size,
            Material mat,
            bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;

            if (!keepCollider)
            {
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            }

            return go;
        }

        private static GameObject CreateCylinder(
            string name,
            Transform parent,
            Vector3 localPos,
            Quaternion localRot,
            Vector3 scale,
            Material mat,
            bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;

            if (!keepCollider)
            {
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            }

            return go;
        }

        private static GameObject CreateSphere(
            string name,
            Transform parent,
            Vector3 localPos,
            float diameter,
            Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * diameter;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void CreateBeamBetween(
            string name,
            Transform parent,
            Vector3 localA,
            Vector3 localB,
            float diameter,
            Material mat)
        {
            Vector3 delta = localB - localA;
            float dist = delta.magnitude;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = (localA + localB) * 0.5f;
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            go.transform.localScale = new Vector3(diameter, dist * 0.5f, diameter);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        private static Material CreateOrUpdateMaterial(string assetName, Color color, float metallic, float smoothness)
        {
            string path = $"{MaterialsFolder}/{assetName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static PhysicsMaterial CreateOrUpdateGripPhysicsMaterial()
        {
            const string legacyWrongPath = "Assets/Physics/GripHighFriction.physicsMaterial";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(legacyWrongPath) != null)
            {
                AssetDatabase.DeleteAsset(legacyWrongPath);
            }

            var pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(GripPhysicsMatPath);
            if (pm == null)
            {
                pm = new PhysicsMaterial("GripHighFriction");
                AssetDatabase.CreateAsset(pm, GripPhysicsMatPath);
            }

            pm.dynamicFriction = 1.0f;
            pm.staticFriction = 1.0f;
            pm.bounciness = 0.0f;
            pm.frictionCombine = PhysicsMaterialCombine.Maximum;
            pm.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(pm);
            return pm;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            string leaf = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var list = new List<EditorBuildSettingsScene>();
            list.Add(new EditorBuildSettingsScene(scenePath, true));

            foreach (var existing in EditorBuildSettings.scenes)
            {
                if (existing.path != scenePath && File.Exists(existing.path))
                {
                    list.Add(existing);
                }
            }

            EditorBuildSettings.scenes = list.ToArray();
        }

        private static void ValidateSceneInternal()
        {
            var bodies = UnityEngine.Object.FindObjectsByType<ArticulationBody>(FindObjectsSortMode.None);
            if (bodies.Length < 6 || bodies.Length > 10)
            {
                throw new Exception($"Cantidad de ArticulationBodies fuera de rango (6-10): {bodies.Length}");
            }

            var jointTypes = new HashSet<ArticulationJointType>();
            int roots = 0;

            foreach (var ab in bodies)
            {
                if (ab.transform.localScale != Vector3.one)
                {
                    throw new Exception($"ArticulationBody '{ab.name}' no tiene escala (1,1,1): {ab.transform.localScale}");
                }

                if (ab.isRoot)
                {
                    roots++;
                }
                else
                {
                    jointTypes.Add(ab.jointType);
                }
            }

            if (roots != 1)
            {
                throw new Exception($"Se esperaba 1 raíz immovable, encontradas: {roots}");
            }

            if (jointTypes.Count < 3)
            {
                throw new Exception($"Se esperaban al menos 3 tipos de joint distintos, encontrados: {jointTypes.Count}");
            }

            Debug.Log($"[GruaTorre] Validación OK: {bodies.Length} ArticulationBodies, {roots} raíz, {jointTypes.Count} tipos de joint ({string.Join(", ", jointTypes)}).");
        }
    }
}
