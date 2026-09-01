using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using BallisticSimulator.Core;
using BallisticSimulator.Camera;
using BallisticSimulator.Physics;
using BallisticSimulator.Targets;
using BallisticSimulator.UI;
using BallisticSimulator.Data;

/// <summary>
/// Herramienta de setup automático de escena para el Simulador Balístico 3D.
/// Acceder desde: Tools ▸ Ballistic Simulator
///
/// "Setup Completo" corre todos los pasos en orden con una barra de progreso.
/// También se puede ejecutar cada paso individualmente si algo falla.
/// </summary>
public static class BallisticSceneSetup
{
    // ── Rutas de assets (relativas a Assets/) ────────────────────────────────
    private const string k_Root       = "Assets/_Project";
    private const string k_Materials  = "Assets/_Project/Materials";
    private const string k_Presets    = "Assets/_Project/ScriptableObjects/Presets";
    private const string k_Prefabs    = "Assets/_Project/Prefabs";
    private const string k_UI         = "Assets/_Project/UI";
    private const string k_UIStyles   = "Assets/_Project/UI/Styles";

    // ════════════════════════════════════════════════════════════════════════
    // MENÚ PRINCIPAL
    // ════════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/Ballistic Simulator/🚀  Setup Completo", false, 0)]
    public static void SetupAll()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Setup Completo — Simulador Balístico",
            "Esto creará todos los assets y construirá la jerarquía de escena.\n\n" +
            "Los objetos ya existentes NO serán duplicados ni modificados.",
            "Continuar", "Cancelar");

        if (!confirm) return;

        try
        {
            Step("Creando tags...",              0.00f, CreateTags);
            Step("Creando carpetas...",          0.05f, EnsureFolders);
            Step("Creando materials y assets...", 0.15f, CreateMaterialsAndAssets);
            Step("Creando prefab TargetBox...",  0.40f, CreateTargetBoxPrefab);
            Step("Construyendo jerarquía...",    0.55f, BuildSceneHierarchy);
            Step("Asignando referencias...",     0.80f, AssignReferences);
            Step("Guardando todo...",            0.95f, SaveAll);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.DisplayDialog(
            "✅  Setup completado",
            "Escena lista.\n\n" +
            "Controlá la Console para ver el resumen.\n" +
            "Presioná ▶ Play para iniciar la simulación.",
            "OK");
    }

    // ════════════════════════════════════════════════════════════════════════
    // PASOS INDIVIDUALES (también accesibles desde el menú)
    // ════════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/Ballistic Simulator/Paso 1 ― Crear Tags", false, 20)]
    public static void CreateTags()
    {
        AddTag("Bullet");
        AddTag("Target");
        AddTag("Ground");
        Debug.Log("[Setup] ✓ Tags: Bullet, Target, Ground");
    }

    [MenuItem("Tools/Ballistic Simulator/Paso 2 ― Crear Materials y Assets", false, 21)]
    public static void CreateMaterialsAndAssets()
    {
        EnsureFolders();
        CreateMaterials();
        CreateBulletPresets();
        CreatePanelSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("[Setup] ✓ Materials, presets de munición y PanelSettings creados.");
    }

    [MenuItem("Tools/Ballistic Simulator/Paso 3 ― Crear Prefab TargetBox", false, 22)]
    public static void CreateTargetBoxPrefab()
    {
        EnsureFolders();
        string prefabPath = $"{k_Prefabs}/TargetBox.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            Debug.Log("[Setup] ✓ TargetBox.prefab ya existe — saltando.");
            return;
        }

        // Crear cubo temporal en la escena
        var go  = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "TargetBox_Temp";
        go.tag  = "Target";

        // Script de comportamiento (RequireComponent agrega Rigidbody y BoxCollider automáticamente)
        go.AddComponent<TargetBox>();

        // Configurar Rigidbody
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass          = 20f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // Configurar BoxCollider
        var col = go.GetComponent<BoxCollider>();
        if (col != null)
        {
            col.size = Vector3.one;
        }

        // Material
        var mat = AssetDatabase.LoadAssetAtPath<Material>($"{k_Materials}/Mat_TargetBox.mat");
        if (mat != null)
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // Guardar como prefab
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        UnityEngine.Object.DestroyImmediate(go);

        AssetDatabase.SaveAssets();
        Debug.Log($"[Setup] ✓ Prefab creado → {prefabPath}");
    }

    [MenuItem("Tools/Ballistic Simulator/Paso 4 ― Construir Jerarquía de Escena", false, 23)]
    public static void BuildSceneHierarchy()
    {
        // ── Grupos organizadores ──────────────────────────────────────────────
        var grpManagers    = GetOrCreate("[MANAGERS]");
        var grpCameras     = GetOrCreate("[CAMERAS]");
        var grpEnvironment = GetOrCreate("[ENVIRONMENT]");
        var grpGun         = GetOrCreate("[GUN]");
        var grpBullet      = GetOrCreate("[BULLET]");
        var grpTrajectory  = GetOrCreate("[TRAJECTORY]");
        var grpTargets     = GetOrCreate("[TARGETS]");
        var grpUI          = GetOrCreate("[UI]");

        // ── MANAGERS ─────────────────────────────────────────────────────────
        var gsmGO = GetOrCreate("GameStateManager",  grpManagers.transform);
        GetOrAddComponent<GameStateManager>(gsmGO);

        var simGO = GetOrCreate("SimulationManager", grpManagers.transform);
        GetOrAddComponent<SimulationManager>(simGO);

        var dbGO = GetOrCreate("DatabaseManager", grpManagers.transform);
        GetOrAddComponent<DatabaseManager>(dbGO);

        // ── CAMERAS ───────────────────────────────────────────────────────────
        // ── CAMERAS ───────────────────────────────────────────────────────────
        // Cámara de setup (fija) - VISTA LATERAL ORBITABLE
        var setupCamGO = GetOrCreate("SetupCamera", grpCameras.transform);
        var setupCam   = GetOrAddComponent<UnityEngine.Camera>(setupCamGO);
        GetOrAddComponent<SetupCameraController>(setupCamGO);
        setupCamGO.tag = "MainCamera";
        setupCamGO.transform.SetPositionAndRotation(
            new Vector3(12f, 6f, -30f),
            Quaternion.Euler(8f, 15f, 0f));

        // Cámara libre (empieza desactivada)
        var freeCamGO = GetOrCreate("FreeCamera", grpCameras.transform);
        GetOrAddComponent<UnityEngine.Camera>(freeCamGO);
        GetOrAddComponent<FreeCameraController>(freeCamGO);
        freeCamGO.transform.SetPositionAndRotation(
            new Vector3(12f, 6f, -30f),
            Quaternion.Euler(8f, 15f, 0f));
        freeCamGO.SetActive(false);

        // Gestor de cámaras
        var camMgrGO = GetOrCreate("CameraManagerGO", grpCameras.transform);
        GetOrAddComponent<CameraManager>(camMgrGO);

        // Desactivar o destruir la Main Camera por defecto si existe (evita conflictos)
        var defaultMainCam = GameObject.Find("Main Camera");
        if (defaultMainCam != null && defaultMainCam != setupCamGO)
        {
            defaultMainCam.SetActive(false);
            Debug.Log("[Setup] Main Camera por defecto desactivada.");
        }

        // ── ENVIRONMENT ───────────────────────────────────────────────────────
        // Suelo
        var ground = GetOrCreate("Ground", grpEnvironment.transform);
        if (ground.GetComponent<MeshFilter>() == null)
        {
            // Reemplazar GO vacío por un Plane primitivo
            UnityEngine.Object.DestroyImmediate(ground);
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(grpEnvironment.transform, false);
        }
        ground.tag = "Ground";
        ground.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        ground.transform.localScale = new Vector3(50f, 1f, 50f);

        var groundMat = AssetDatabase.LoadAssetAtPath<Material>($"{k_Materials}/Mat_Ground.mat");
        if (groundMat != null)
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

        // Luz direccional (reusar la existente)
        var existingLight = UnityEngine.Object.FindFirstObjectByType<Light>();
        if (existingLight == null)
        {
            var lightGO = new GameObject("DirectionalLight");
            lightGO.transform.SetParent(grpEnvironment.transform, false);
            var lt       = lightGO.AddComponent<Light>();
            lt.type      = LightType.Directional;
            lt.intensity = 1f;
            lt.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Undo.RegisterCreatedObjectUndo(lightGO, "Create Light");
        }
        else
        {
            existingLight.transform.SetParent(grpEnvironment.transform, true);
        }

        // ── GUN ──────────────────────────────────────────────────────────────
        var gunBase = GetOrCreate("GunBase", grpGun.transform);
        gunBase.transform.localPosition = new Vector3(0f, 1.5f, 0f);

        // Limpiar cualquier objeto viejo dentro de GunBase para evitar cañones duplicados
        for (int i = gunBase.transform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(gunBase.transform.GetChild(i).gameObject);
        }

        var gunMat = GetOrCreateMaterial(
            "Mat_GunMetal", "Universal Render Pipeline/Lit",
            new Color(0.2f, 0.22f, 0.25f), $"{k_Materials}/Mat_GunMetal.mat");

        // Base/Pedestal estático (RECTÁNGULO: no rota)
        var stand     = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stand.name    = "Stand";
        stand.transform.SetParent(gunBase.transform, false);
        stand.transform.localPosition = new Vector3(0.5f, -0.6f, 0f);
        stand.transform.localScale    = new Vector3(3.5f, 1.0f, 1.8f);
        stand.GetComponent<MeshRenderer>().sharedMaterial = gunMat;
        UnityEngine.Object.DestroyImmediate(stand.GetComponent<BoxCollider>());
        Undo.RegisterCreatedObjectUndo(stand, "Create Stand");

        // Pivot giratorio del cañón (único objeto que rota con el ángulo)
        var cannonPivot = new GameObject("CannonPivot");
        cannonPivot.transform.SetParent(gunBase.transform, false);
        cannonPivot.transform.localPosition = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(cannonPivot, "Create CannonPivot");

        // Muzzle: Nace EXACTAMENTE en la base/pivot que rota (altura de referencia)
        var muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(cannonPivot.transform, false);
        muzzle.transform.localPosition = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(muzzle, "Create Muzzle");

        // Modelo visual ÚNICO del cañón (Cilindro gran calibre: 1.2m diam × 5m largo)
        var barrel     = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name    = "Barrel";
        barrel.transform.SetParent(cannonPivot.transform, false);
        barrel.transform.localPosition = new Vector3(2.5f, 0f, 0f);
        barrel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        barrel.transform.localScale    = new Vector3(1.2f, 2.5f, 1.2f);
        barrel.GetComponent<MeshRenderer>().sharedMaterial = gunMat;
        UnityEngine.Object.DestroyImmediate(barrel.GetComponent<CapsuleCollider>());
        Undo.RegisterCreatedObjectUndo(barrel, "Create Barrel");

        // ── BULLET ────────────────────────────────────────────────────────────
        var bulletGO = GetOrCreate("BulletGO", grpBullet.transform);
        if (bulletGO.GetComponent<MeshFilter>() == null)
        {
            UnityEngine.Object.DestroyImmediate(bulletGO);
            bulletGO       = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulletGO.name  = "BulletGO";
            bulletGO.transform.SetParent(grpBullet.transform, false);
            Undo.RegisterCreatedObjectUndo(bulletGO, "Create Bullet");
        }
        bulletGO.tag = "Bullet";
        bulletGO.transform.localScale = Vector3.one * 0.5f; // 50cm de diámetro inicial
        GetOrAddComponent<BulletController>(bulletGO);

        var sphereCol = bulletGO.GetComponent<SphereCollider>();
        if (sphereCol != null) sphereCol.isTrigger = true;

        // Material dorado/amarillo brillante para el proyectil
        var bulletMat = GetOrCreateMaterial(
            "Mat_Bullet", "Universal Render Pipeline/Lit",
            new Color(1.0f, 0.75f, 0.0f), $"{k_Materials}/Mat_Bullet.mat");
        bulletGO.GetComponent<MeshRenderer>().sharedMaterial = bulletMat;
        bulletGO.SetActive(false); // empieza desactivada

        // ── TRAJECTORY ────────────────────────────────────────────────────────
        var trajGO = GetOrCreate("TrajectoryLine", grpTrajectory.transform);
        var lr     = GetOrAddComponent<LineRenderer>(trajGO);
        GetOrAddComponent<TrajectoryPreview>(trajGO);

        lr.useWorldSpace     = true;
        lr.startWidth        = 0.08f;
        lr.endWidth          = 0.02f;
        lr.numCapVertices    = 4;
        lr.numCornerVertices = 4;
        lr.positionCount     = 0;
        lr.textureMode       = LineTextureMode.Tile;

        var trajMat = AssetDatabase.LoadAssetAtPath<Material>($"{k_Materials}/Mat_Trajectory.mat");
        if (trajMat != null) lr.sharedMaterial = trajMat;

        // ── TARGETS ───────────────────────────────────────────────────────────
        var spawnerGO = GetOrCreate("TargetSpawnerGO", grpTargets.transform);
        GetOrAddComponent<TargetSpawner>(spawnerGO);

        // ── UI ────────────────────────────────────────────────────────────────
        var uiGO  = GetOrCreate("UIDocumentGO", grpUI.transform);
        var uidoc = GetOrAddComponent<UIDocument>(uiGO);
        GetOrAddComponent<SidePanelUI>(uiGO);

        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{k_UI}/SidePanel.uxml");
        if (uxml != null) uidoc.visualTreeAsset = uxml;
        else Debug.LogWarning("[Setup] SidePanel.uxml no encontrado en " + k_UI);

        var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>($"{k_UI}/BallisticPanelSettings.asset");
        if (ps != null) uidoc.panelSettings = ps;
        else Debug.LogWarning("[Setup] BallisticPanelSettings.asset no encontrado en " + k_UI);

        MarkSceneDirty();
        Debug.Log("[Setup] ✓ Jerarquía de escena construida.");
    }

    [MenuItem("Tools/Ballistic Simulator/Paso 5 ― Asignar Referencias", false, 24)]
    public static void AssignReferences()
    {
        // Localizar objetos clave
        var simGO      = FindRequired("SimulationManager");
        var camMgrGO   = FindRequired("CameraManagerGO");
        var spawnerGO  = FindRequired("TargetSpawnerGO");
        var uiGO       = FindRequired("UIDocumentGO");
        var trajGO     = FindRequired("TrajectoryLine");
        var bulletGO   = FindRequired("BulletGO");
        var setupCamGO = FindRequired("SetupCamera");
        var freeCamGO  = FindRequired("FreeCamera");
        var muzzleGO   = FindRequired("Muzzle");

        if (simGO == null || camMgrGO == null || spawnerGO == null ||
            uiGO  == null || trajGO   == null || bulletGO  == null ||
            setupCamGO == null || freeCamGO == null || muzzleGO == null)
        {
            EditorUtility.DisplayDialog("Error — Setup Balístico",
                "Faltan objetos en la escena.\n" +
                "Ejecutá primero el Paso 4 — Construir Jerarquía de Escena.", "OK");
            return;
        }

        // ── SimulationManager ─────────────────────────────────────────────────
        var sim           = simGO.GetComponent<SimulationManager>();
        var cannonPivotGO = FindRequired("CannonPivot");
        SetField(sim, "_bulletController",  bulletGO.GetComponent<BulletController>());
        SetField(sim, "_trajectoryPreview", trajGO.GetComponent<TrajectoryPreview>());
        SetField(sim, "_targetSpawner",     spawnerGO.GetComponent<TargetSpawner>());
        SetField(sim, "_muzzleTransform",   muzzleGO.transform);
        SetField(sim, "_gunBaseTransform",  cannonPivotGO != null ? cannonPivotGO.transform : null);

        // ── CameraManager ─────────────────────────────────────────────────────
        var camMgr = camMgrGO.GetComponent<CameraManager>();
        SetField(camMgr, "_setupCamera", setupCamGO.GetComponent<UnityEngine.Camera>());
        SetField(camMgr, "_freeCamera",  freeCamGO.GetComponent<UnityEngine.Camera>());

        // ── TargetSpawner ─────────────────────────────────────────────────────
        var spawner = spawnerGO.GetComponent<TargetSpawner>();
        var boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{k_Prefabs}/TargetBox.prefab");
        SetField(spawner, "_boxPrefab",       boxPrefab);
        SetField(spawner, "_muzzleTransform", muzzleGO.transform);

        // ── TrajectoryPreview ─────────────────────────────────────────────────
        var trajPreview = trajGO.GetComponent<TrajectoryPreview>();
        var trajMat     = AssetDatabase.LoadAssetAtPath<Material>($"{k_Materials}/Mat_Trajectory.mat");
        SetField(trajPreview, "_dottedMaterial", trajMat);

        // ── SidePanelUI — array de presets ────────────────────────────────────
        var sidePanelUI = uiGO.GetComponent<SidePanelUI>();
        var uiSo        = new SerializedObject(sidePanelUI);
        var presetsArr  = uiSo.FindProperty("_presets");

        string[] presetPaths =
        {
            $"{k_Presets}/Preset_22LR.asset",
            $"{k_Presets}/Preset_9mm.asset",
            $"{k_Presets}/Preset_762x39.asset",
            $"{k_Presets}/Preset_308Win.asset",
            $"{k_Presets}/Preset_50BMG.asset",
        };

        presetsArr.arraySize = presetPaths.Length;
        for (int i = 0; i < presetPaths.Length; i++)
        {
            var preset = AssetDatabase.LoadAssetAtPath<BulletPreset>(presetPaths[i]);
            if (preset == null)
                Debug.LogWarning($"[Setup] Preset no encontrado: {presetPaths[i]}");
            presetsArr.GetArrayElementAtIndex(i).objectReferenceValue = preset;
        }
        SetField(sidePanelUI, "_targetSpawner", spawner);
        uiSo.ApplyModifiedPropertiesWithoutUndo();

        MarkSceneDirty();
        Debug.Log("[Setup] ✓ Todas las referencias asignadas.");
    }

    [MenuItem("Tools/Ballistic Simulator/Paso 6 ― Guardar Todo", false, 25)]
    public static void SaveAll()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Setup] ✓ Escena y assets guardados.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS — Folders y Assets
    // ════════════════════════════════════════════════════════════════════════

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/_Project", "Materials");
        EnsureFolder("Assets/_Project", "Prefabs");
        EnsureFolder("Assets/_Project", "ScriptableObjects");
        EnsureFolder("Assets/_Project/ScriptableObjects", "Presets");
        EnsureFolder("Assets/_Project", "UI");
        EnsureFolder("Assets/_Project/UI", "Styles");
    }

    private static void CreateMaterials()
    {
        GetOrCreateMaterial("Mat_Ground",     "Universal Render Pipeline/Lit",
            new Color(0.18f, 0.24f, 0.18f), $"{k_Materials}/Mat_Ground.mat");

        GetOrCreateMaterial("Mat_TargetBox",  "Universal Render Pipeline/Lit",
            new Color(0.36f, 0.23f, 0.12f), $"{k_Materials}/Mat_TargetBox.mat");

        GetOrCreateMaterial("Mat_Trajectory", "Universal Render Pipeline/Unlit",
            new Color(0.22f, 0.83f, 0.33f), $"{k_Materials}/Mat_Trajectory.mat");
    }

    private static Material GetOrCreateMaterial(
        string matName, string shaderName, Color color, string assetPath)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (existing != null) return existing;

        var shader = Shader.Find(shaderName) ?? Shader.Find("Standard");
        if (Shader.Find(shaderName) == null)
            Debug.LogWarning($"[Setup] Shader '{shaderName}' no encontrado — usando Standard.");

        var mat   = new Material(shader) { name = matName };
        mat.color = color;
        AssetDatabase.CreateAsset(mat, assetPath);
        return mat;
    }

    private static void CreateBulletPresets()
    {
        // nombre asset, PresetName, masa(g), radio(mm), v0(m/s para campo de 50m)
        CreatePresetAsset("Preset_22LR",   ".22 LR",            2.6f,  2.80f,  35f);
        CreatePresetAsset("Preset_9mm",    "9mm Parabellum",    8.0f,  4.50f,  45f);
        CreatePresetAsset("Preset_762x39", "7.62x39 (AK-47)",  8.0f,  3.95f,  65f);
        CreatePresetAsset("Preset_308Win", ".308 Winchester",  10.0f,  3.90f,  80f);
        CreatePresetAsset("Preset_50BMG",  ".50 BMG",          42.0f,  6.35f,  100f);
    }

    private static void CreatePresetAsset(
        string fileName, string presetName, float massG, float radiusMm, float muzzleVel)
    {
        string path = $"{k_Presets}/{fileName}.asset";
        var p = AssetDatabase.LoadAssetAtPath<BulletPreset>(path);
        if (p == null)
        {
            p = ScriptableObject.CreateInstance<BulletPreset>();
            AssetDatabase.CreateAsset(p, path);
        }

        p.PresetName     = presetName;
        p.MassGrams      = massG;
        p.RadiusMm       = radiusMm;
        p.MuzzleVelocity = muzzleVel;
        EditorUtility.SetDirty(p);
    }

    private static void CreatePanelSettings()
    {
        string path = $"{k_UI}/BallisticPanelSettings.asset";
        if (AssetDatabase.LoadAssetAtPath<PanelSettings>(path) != null) return;

        var ps                 = ScriptableObject.CreateInstance<PanelSettings>();
        ps.scaleMode           = PanelScaleMode.ScaleWithScreenSize;
        ps.referenceResolution = new Vector2Int(1920, 1080);
        ps.screenMatchMode     = PanelScreenMatchMode.MatchWidthOrHeight;
        ps.match               = 0f;
        AssetDatabase.CreateAsset(ps, path);
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS — Scene Objects
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Devuelve el GameObject con ese nombre hijo de <paramref name="parent"/>,
    /// o lo crea si no existe. Registra la creación para deshacer con Ctrl+Z.
    /// </summary>
    private static GameObject GetOrCreate(string name, Transform parent = null)
    {
        Transform found = parent != null
            ? parent.Find(name)
            : FindInScene(name)?.transform;

        if (found != null) return found.gameObject;

        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go;
    }

    private static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS — Serialized fields
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Asigna un campo [SerializeField] private usando SerializedObject.
    /// Funciona para cualquier tipo de UnityEngine.Object.
    /// </summary>
    private static void SetField(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
    {
        if (target == null) return;

        var so   = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);

        if (prop == null)
        {
            Debug.LogWarning(
                $"[Setup] Campo '{fieldName}' no encontrado en {target.GetType().Name}.\n" +
                "Verificar que el nombre coincide exactamente con el script.");
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS — Tags y Folders
    // ════════════════════════════════════════════════════════════════════════

    private static void AddTag(string tag)
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        var tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string path = $"{parent}/{folderName}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, folderName);
    }

    private static GameObject FindRequired(string name)
    {
        var go = FindInScene(name);
        if (go == null)
            Debug.LogError($"[Setup] Objeto requerido no encontrado en escena: '{name}'");
        return go;
    }

    private static GameObject FindInScene(string name)
    {
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = FindRecursive(root.transform, name);
            if (found != null) return found;
        }
        return null;
    }

    private static GameObject FindRecursive(Transform current, string name)
    {
        if (current.name == name) return current.gameObject;
        for (int i = 0; i < current.childCount; i++)
        {
            var result = FindRecursive(current.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    private static void MarkSceneDirty() =>
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

    // ════════════════════════════════════════════════════════════════════════
    // HELPER — Ejecutar paso con barra de progreso
    // ════════════════════════════════════════════════════════════════════════
    private static void Step(string label, float progress, Action action)
    {
        EditorUtility.DisplayProgressBar("Setup Balístico", label, progress);
        action();
    }
}
