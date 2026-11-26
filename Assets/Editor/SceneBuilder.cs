using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// One-time setup tool (editor only, never part of a build).
// Builds the scenes, prefabs, materials and the BuildSpot layer for the current state of the game,
// so the project opens ready to play. It runs by itself the first time the project is opened
// (when Assets/Scenes/Game.unity does not exist yet) and can be run again from the menu:
// SE320 > Rebuild Scenes and Prefabs.
// After the first build everything is normal Unity assets, and this file can be deleted.
[InitializeOnLoad]
public static class SceneBuilder
{
    const string ScenesFolder = "Assets/Scenes";
    const string PrefabsFolder = "Assets/Prefabs";
    const string MaterialsFolder = "Assets/Materials";
    const string MenuScenePath = ScenesFolder + "/MainMenu.unity";
    const string GameScenePath = ScenesFolder + "/Game.unity";
    const string ClickSoundPath = "Assets/Audio/click.wav";
    const string HitSoundPath = "Assets/Audio/hit.wav";
    const string HeavyHitSoundPath = "Assets/Audio/heavy_hit.wav";
    const string PlaceSoundPath = "Assets/Audio/place.wav";
    const string HeavyPlaceSoundPath = "Assets/Audio/heavy_place.wav";
    const string BuildSpotLayerName = "BuildSpot";
    const int UILayer = 5;

    // Waypoints in order. The base sits right after the last one.
    static readonly Vector3[] PathPoints =
    {
        new Vector3(-15f, 0f, 6f),
        new Vector3(-8f, 0f, 6f),
        new Vector3(-8f, 0f, -5f),
        new Vector3(0f, 0f, -5f),
        new Vector3(0f, 0f, 5f),
        new Vector3(8f, 0f, 5f),
        new Vector3(8f, 0f, -5f),
        new Vector3(13f, 0f, -5f),
    };

    // (x, z) of every build spot. All of them are at least 2 units away from the path.
    static readonly Vector2[] BuildSpotPositions =
    {
        new Vector2(-12f, 9f),
        new Vector2(-11f, 3f),
        new Vector2(-11f, -2f),
        new Vector2(-4f, 1f),
        new Vector2(-4f, -8.5f),
        new Vector2(4f, 0f),
        new Vector2(4f, 8f),
        new Vector2(11f, 1f),
        new Vector2(11f, -8.5f),
    };

    const float PathHeight = 0.05f;
    const float PathWidth = 2f;

    class Palette
    {
        public Material ground, path, spot, spawn, playerBase;
        public Material enemyBasic, enemyFast, enemyTank, nose;
        public Material towerBase, basicHead, heavyHead, barrel;
        public Material projectile, heavyProjectile, trail;
    }

    class Prefabs
    {
        public Enemy basicEnemy, fastEnemy, tankEnemy;
        public Tower basicTower, heavyTower;
    }

    static double tmpWaitDeadline;

    // ------------------------------------------------------------------ entry points

    static SceneBuilder()
    {
        if (Application.isBatchMode) return;
        EditorApplication.delayCall += AutoBuildOnFirstOpen;
    }

    static void AutoBuildOnFirstOpen()
    {
        if (File.Exists(GameScenePath)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += AutoBuildOnFirstOpen;
            return;
        }

        Debug.Log("[SE320] First time opening the project: building the scenes and prefabs.");
        Build();
    }

    // Greyed out in play mode: scenes can't be saved or replaced while the game is running.
    [MenuItem("SE320/Rebuild Scenes and Prefabs", true)]
    static bool CanRebuild()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    [MenuItem("SE320/Rebuild Scenes and Prefabs")]
    static void RebuildFromMenu()
    {
        if (File.Exists(GameScenePath) && !EditorUtility.DisplayDialog(
                "Rebuild scenes and prefabs?",
                "This overwrites Scenes/MainMenu, Scenes/Game and the prefabs and materials made by this tool. " +
                "Any changes made to them in the editor will be lost.",
                "Rebuild", "Cancel"))
            return;

        Build();
    }

    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // TextMeshPro needs its essential resources (default font, settings) inside the project first.
        if (!TextMeshProReady())
        {
            ImportTextMeshProEssentials();
            return;
        }

        try
        {
            EnsureFolder(ScenesFolder);
            EnsureFolder(PrefabsFolder);
            EnsureFolder(MaterialsFolder);
            int buildSpotLayer = EnsureLayer(BuildSpotLayerName);

            Palette palette = CreateMaterials();

            // Prefabs are put together in an empty scene and then saved as assets.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Prefabs prefabs = CreatePrefabs(palette);

            BuildMenuScene();
            BuildGameScene(palette, prefabs, buildSpotLayer);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };

            PlayerSettings.productName = "Last Line";

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(MenuScenePath);

            Debug.Log("[SE320] Done. The MainMenu scene is open, press Play to start.");
            EditorUtility.DisplayDialog("Last Line",
                "Scenes, prefabs and materials are ready.\n\nThe MainMenu scene is open. Press Play, then Start.",
                "OK");
        }
        catch (Exception e)
        {
            Debug.LogError("[SE320] Building the project failed: " + e);
        }
    }

    // ------------------------------------------------------------------ TextMeshPro essentials

    static bool TextMeshProReady()
    {
        return Resources.Load<TMP_Settings>("TMP Settings") != null;
    }

    static void ImportTextMeshProEssentials()
    {
        Debug.Log("[SE320] Importing TextMesh Pro Essential Resources first...");

        if (!StartTmpImport())
        {
            Debug.LogError("[SE320] Could not import TextMesh Pro Essential Resources automatically. " +
                           "Use Window > TextMeshPro > Import TMP Essential Resources, then run " +
                           "SE320 > Rebuild Scenes and Prefabs.");
            return;
        }

        tmpWaitDeadline = EditorApplication.timeSinceStartup + 120.0;
        EditorApplication.update -= WaitForTextMeshPro;
        EditorApplication.update += WaitForTextMeshPro;
    }

    static bool StartTmpImport()
    {
        // 1) TMP's own importer (same thing the "Import TMP Essentials" button does).
        try
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type importer = assembly.GetType("TMPro.TMP_PackageResourceImporter", false);
                if (importer == null) continue;

                MethodInfo method = importer.GetMethod("ImportResources",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null && method.GetParameters().Length == 3)
                {
                    method.Invoke(null, new object[] { true, false, false });
                    return true;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SE320] TMP importer call failed, trying the package file directly. " + e.Message);
        }

        // 2) Import the .unitypackage that ships inside the package.
        string[] packages = { "com.unity.ugui", "com.unity.textmeshpro" };
        foreach (string package in packages)
        {
            string file = Path.Combine(Path.GetFullPath("Packages/" + package),
                "Package Resources", "TMP Essential Resources.unitypackage");
            if (File.Exists(file))
            {
                AssetDatabase.ImportPackage(file, false);
                return true;
            }
        }
        return false;
    }

    static void WaitForTextMeshPro()
    {
        if (TextMeshProReady())
        {
            EditorApplication.update -= WaitForTextMeshPro;
            EditorApplication.delayCall += Build;
            return;
        }

        if (EditorApplication.timeSinceStartup > tmpWaitDeadline)
        {
            EditorApplication.update -= WaitForTextMeshPro;
            Debug.LogError("[SE320] TextMesh Pro Essential Resources did not finish importing. " +
                           "Use Window > TextMeshPro > Import TMP Essential Resources, then run " +
                           "SE320 > Rebuild Scenes and Prefabs.");
        }
    }

    // ------------------------------------------------------------------ project setup

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing != -1) return existing;

        SerializedObject tagManager =
            new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++) // 0-7 are Unity's built-in layers
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
        }
        throw new Exception("No free layer slot for '" + layerName + "'.");
    }

    // ------------------------------------------------------------------ materials

    static Palette CreateMaterials()
    {
        Material lit = DefaultLitMaterial();
        Palette p = new Palette
        {
            ground = SaveMaterial(Colored(lit, "Ground", new Color(0.36f, 0.58f, 0.32f))),
            path = SaveMaterial(Colored(lit, "Path", new Color(0.78f, 0.69f, 0.50f))),
            spot = SaveMaterial(Colored(lit, "BuildSpot", new Color(0.55f, 0.55f, 0.58f))),
            spawn = SaveMaterial(Colored(lit, "Spawn", new Color(0.45f, 0.12f, 0.12f))),
            playerBase = SaveMaterial(Colored(lit, "PlayerBase", new Color(0.20f, 0.40f, 0.90f))),
            enemyBasic = SaveMaterial(Colored(lit, "EnemyBasic", new Color(0.85f, 0.20f, 0.20f))),
            enemyFast = SaveMaterial(Colored(lit, "EnemyFast", new Color(0.95f, 0.80f, 0.10f))),
            enemyTank = SaveMaterial(Colored(lit, "EnemyTank", new Color(0.50f, 0.22f, 0.62f))),
            nose = SaveMaterial(Colored(lit, "EnemyNose", new Color(0.08f, 0.08f, 0.08f))),
            towerBase = SaveMaterial(Colored(lit, "TowerBase", new Color(0.40f, 0.40f, 0.42f))),
            basicHead = SaveMaterial(Colored(lit, "BasicTowerHead", new Color(0.55f, 0.75f, 0.95f))),
            heavyHead = SaveMaterial(Colored(lit, "HeavyTowerHead", new Color(0.70f, 0.25f, 0.15f))),
            barrel = SaveMaterial(Colored(lit, "Barrel", new Color(0.15f, 0.15f, 0.17f))),
            projectile = SaveMaterial(Colored(lit, "Projectile", new Color(1.00f, 0.90f, 0.30f))),
            heavyProjectile = SaveMaterial(Colored(lit, "HeavyProjectile", new Color(1.00f, 0.50f, 0.10f))),
        };

        Shader trailShader = Shader.Find("Sprites/Default");
        Material trail = trailShader != null ? new Material(trailShader) : new Material(lit);
        trail.name = "ProjectileTrail";
        p.trail = SaveMaterial(trail);
        return p;
    }

    // The material Unity puts on a new cube. Copying it means the colours work with the
    // built-in render pipeline and with URP.
    static Material DefaultLitMaterial()
    {
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material material = temp.GetComponent<MeshRenderer>().sharedMaterial;
        Object.DestroyImmediate(temp);
        return material;
    }

    static Material Colored(Material template, string name, Color color)
    {
        Material material = new Material(template) { name = name };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        return material;
    }

    static Material SaveMaterial(Material material)
    {
        string path = MaterialsFolder + "/" + material.name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(material, existing);
            return existing;
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // ------------------------------------------------------------------ prefabs

    static Prefabs CreatePrefabs(Palette p)
    {
        Projectile basicShot = SavePrefab(MakeProjectile("BasicProjectile", 0.25f, 18f, p.projectile, p.trail, HitSoundPath, 0.15f))
            .GetComponent<Projectile>();
        Projectile heavyShot = SavePrefab(MakeProjectile("HeavyProjectile", 0.45f, 12f, p.heavyProjectile, p.trail, HeavyHitSoundPath, 0.12f))
            .GetComponent<Projectile>();

        Prefabs prefabs = new Prefabs();
        prefabs.basicEnemy = SavePrefab(MakeEnemy<BasicEnemy>("BasicEnemy", 0.8f, p.enemyBasic, p.nose, 2.5f, 6, 10))
            .GetComponent<Enemy>();
        prefabs.fastEnemy = SavePrefab(MakeEnemy<FastEnemy>("FastEnemy", 0.6f, p.enemyFast, p.nose, 4.5f, 3, 8))
            .GetComponent<Enemy>();
        prefabs.tankEnemy = SavePrefab(MakeEnemy<TankEnemy>("TankEnemy", 1.2f, p.enemyTank, p.nose, 1.4f, 20, 25))
            .GetComponent<Enemy>();

        prefabs.basicTower = SavePrefab(MakeTower<BasicTower>("BasicTower", p, p.basicHead, 0.2f, 0.8f, basicShot,
            50, 5f, 2f, 1, PlaceSoundPath, 0.3f)).GetComponent<Tower>();
        prefabs.heavyTower = SavePrefab(MakeTower<HeavyTower>("HeavyTower", p, p.heavyHead, 0.35f, 0.9f, heavyShot,
            120, 6f, 0.5f, 5, HeavyPlaceSoundPath, 0.4f)).GetComponent<Tower>();

        return prefabs;
    }

    static Prefabs ReloadPrefabs()
    {
        return new Prefabs
        {
            basicEnemy = LoadPrefab<Enemy>("BasicEnemy"),
            fastEnemy = LoadPrefab<Enemy>("FastEnemy"),
            tankEnemy = LoadPrefab<Enemy>("TankEnemy"),
            basicTower = LoadPrefab<Tower>("BasicTower"),
            heavyTower = LoadPrefab<Tower>("HeavyTower"),
        };
    }

    static T LoadPrefab<T>(string name) where T : Component
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsFolder + "/" + name + ".prefab");
        T component = asset != null ? asset.GetComponent<T>() : null;
        if (component == null)
            throw new Exception("Prefab '" + name + "' (" + typeof(T).Name + ") was not found in " + PrefabsFolder);
        return component;
    }

    static GameObject SavePrefab(GameObject sceneObject)
    {
        string path = PrefabsFolder + "/" + sceneObject.name + ".prefab";
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(sceneObject, path);
        Object.DestroyImmediate(sceneObject);
        return asset;
    }

    static GameObject MakeProjectile(string name, float size, float speed, Material material, Material trailMaterial,
        string hitSoundPath, float hitVolume)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.localScale = Vector3.one * size;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        go.GetComponent<SphereCollider>().isTrigger = true;

        Rigidbody body = go.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        TrailRenderer trail = go.AddComponent<TrailRenderer>();
        trail.sharedMaterial = trailMaterial;
        trail.time = 0.15f;
        trail.startWidth = size * 0.8f;
        trail.endWidth = 0f;
        trail.startColor = new Color(1f, 0.9f, 0.4f, 1f);
        trail.endColor = new Color(1f, 0.6f, 0.2f, 0f);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Projectile projectile = go.AddComponent<Projectile>();
        SetFloat(projectile, "speed", speed);
        SetRef(projectile, "hitSound", AssetDatabase.LoadAssetAtPath<AudioClip>(hitSoundPath));
        SetFloat(projectile, "hitVolume", hitVolume);
        return go;
    }

    // Enemy root sits on the ground. The body cube and the "nose" (shows which way it faces) are children.
    static GameObject MakeEnemy<T>(string name, float size, Material bodyMaterial, Material noseMaterial,
        float speed, int health, int reward) where T : Enemy
    {
        GameObject root = new GameObject(name);

        Rigidbody body = root.AddComponent<Rigidbody>(); // needed so tower range triggers notice the enemy
        body.isKinematic = true;
        body.useGravity = false;

        Primitive(PrimitiveType.Cube, "Body", root.transform, new Vector3(0f, size / 2f, 0f),
            Vector3.one * size, bodyMaterial, true);
        Primitive(PrimitiveType.Cube, "Nose", root.transform, new Vector3(0f, size * 0.6f, size * 0.65f),
            Vector3.one * (size * 0.3f), noseMaterial, false);

        T enemy = root.AddComponent<T>();
        SetFloat(enemy, "speed", speed);
        SetInt(enemy, "maxHealth", health);
        SetInt(enemy, "reward", reward);
        SetInt(enemy, "baseDamage", 1);
        return root;
    }

    // Tower = base + rotating head (with the barrel on it). The root has the range trigger.
    static GameObject MakeTower<T>(string name, Palette p, Material headMaterial, float barrelWidth,
        float barrelLength, Projectile shot, int cost, float range, float fireRate, int damage, string placeSoundPath,
        float placeVolume) where T : Tower
    {
        GameObject root = new GameObject(name);
        SphereCollider rangeTrigger = root.AddComponent<SphereCollider>();
        rangeTrigger.isTrigger = true;
        rangeTrigger.radius = range;

        Primitive(PrimitiveType.Cylinder, "Base", root.transform, new Vector3(0f, 0.3f, 0f),
            new Vector3(1.2f, 0.3f, 1.2f), p.towerBase, false);

        GameObject head = new GameObject("Head");
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.9f, 0f);

        Primitive(PrimitiveType.Cube, "HeadMesh", head.transform, Vector3.zero,
            new Vector3(0.8f, 0.6f, 0.8f), headMaterial, false);
        Primitive(PrimitiveType.Cube, "Barrel", head.transform, new Vector3(0f, 0.05f, 0.4f + barrelLength / 2f),
            new Vector3(barrelWidth, barrelWidth, barrelLength), p.barrel, false);

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(head.transform, false);
        firePoint.transform.localPosition = new Vector3(0f, 0.05f, 0.5f + barrelLength);

        T tower = root.AddComponent<T>();
        SetInt(tower, "cost", cost);
        SetFloat(tower, "range", range);
        SetFloat(tower, "fireRate", fireRate);
        SetInt(tower, "damage", damage);
        SetRef(tower, "head", head.transform);
        SetRef(tower, "firePoint", firePoint.transform);
        SetRef(tower, "projectilePrefab", shot);
        SetRef(tower, "placeSound", AssetDatabase.LoadAssetAtPath<AudioClip>(placeSoundPath));
        SetFloat(tower, "placeVolume", placeVolume);
        return root;
    }

    // ------------------------------------------------------------------ main menu scene

    static void BuildMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Camera cam = MainCamera(scene);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.14f, 0.20f);

        Canvas canvas = CreateCanvas();
        CreateEventSystem();

        Vector2 center = new Vector2(0.5f, 0.5f);
        TextMeshProUGUI title = CreateText(canvas.transform, "Title", "LAST LINE", 140, center, center,
            new Vector2(0f, 230f), new Vector2(1400f, 170f), TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        CreateText(canvas.transform, "Subtitle", "A tower defense game  |  SE 320", 36, center, center,
            new Vector2(0f, 130f), new Vector2(1000f, 60f), TextAlignmentOptions.Center);

        Button start = CreateButton(canvas.transform, "StartButton", "Start", center, new Vector2(0f, -20f),
            new Vector2(420f, 100f));
        Button quit = CreateButton(canvas.transform, "QuitButton", "Quit", center, new Vector2(0f, -150f),
            new Vector2(420f, 100f));

        GameObject menuObject = new GameObject("MainMenu");
        AudioSource click = menuObject.AddComponent<AudioSource>();
        click.playOnAwake = false;
        click.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClickSoundPath);

        MainMenu menu = menuObject.AddComponent<MainMenu>();
        SetRef(menu, "startButton", start);
        SetRef(menu, "quitButton", quit);
        SetRef(menu, "clickSound", click);

        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    // ------------------------------------------------------------------ game scene

    static void BuildGameScene(Palette p, Prefabs prefabs, int buildSpotLayer)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Creating a new scene can unload the prefab objects that CreatePrefabs returned, and references
        // to them then end up empty in the Game scene (WaveManager and TowerPanel got "None" and threw
        // NullReferenceException on Play). Loading the prefabs from their asset files again, after the
        // new scene exists, gives references that stay valid.
        prefabs = ReloadPrefabs();

        Camera cam = MainCamera(scene);
        cam.transform.position = new Vector3(0f, 24f, -18f);
        cam.transform.LookAt(new Vector3(0f, 0f, -1.5f));
        cam.fieldOfView = 50f;

        // Ground and the path tiles (only visuals, the enemies follow the waypoints).
        GameObject map = new GameObject("Map");
        Primitive(PrimitiveType.Plane, "Ground", map.transform, Vector3.zero, new Vector3(3.2f, 1f, 2.2f),
            p.ground, true);

        GameObject tiles = new GameObject("PathTiles");
        tiles.transform.SetParent(map.transform, false);
        for (int i = 1; i < PathPoints.Length; i++)
        {
            Vector3 a = PathPoints[i - 1];
            Vector3 b = PathPoints[i];
            Vector3 center = (a + b) / 2f;
            Vector3 size = new Vector3(Mathf.Abs(b.x - a.x) + PathWidth, 0.1f, Mathf.Abs(b.z - a.z) + PathWidth);
            Primitive(PrimitiveType.Cube, "PathTile " + i, tiles.transform, center, size, p.path, false);
        }

        // Waypoints: empty objects, children of the path, in walking order.
        GameObject pathObject = new GameObject("EnemyPath");
        EnemyPath enemyPath = pathObject.AddComponent<EnemyPath>();
        for (int i = 0; i < PathPoints.Length; i++)
        {
            GameObject waypoint = new GameObject("Waypoint " + i);
            waypoint.transform.SetParent(pathObject.transform, false);
            waypoint.transform.position = PathPoints[i] + Vector3.up * PathHeight;
        }

        // Spawner at the start of the path.
        GameObject spawner = new GameObject("Spawner");
        spawner.transform.position = PathPoints[0] + Vector3.up * PathHeight;
        spawner.transform.rotation = Quaternion.LookRotation(PathPoints[1] - PathPoints[0]);
        Primitive(PrimitiveType.Cube, "SpawnMarker", spawner.transform, new Vector3(0f, 0.01f, 0f),
            new Vector3(2f, 0.04f, 2f), p.spawn, false);

        WaveManager waves = spawner.AddComponent<WaveManager>();
        SetRef(waves, "path", enemyPath);
        SetRefs(waves, "enemyTypes", new Object[] { prefabs.basicEnemy, prefabs.fastEnemy, prefabs.tankEnemy });


        // The player's base at the end of the path.
        GameObject baseObject = new GameObject("PlayerBase");
        baseObject.transform.position = new Vector3(14.5f, 0f, -5f);
        Primitive(PrimitiveType.Cube, "BaseMesh", baseObject.transform, new Vector3(0f, 1f, 0f),
            new Vector3(2f, 2f, 2.4f), p.playerBase, false);
        PlayerBase playerBase = baseObject.AddComponent<PlayerBase>();

        // Build spots, on their own layer so the click raycast only hits them.
        GameObject spots = new GameObject("BuildSpots");
        for (int i = 0; i < BuildSpotPositions.Length; i++)
        {
            Vector2 xz = BuildSpotPositions[i];
            GameObject spot = Primitive(PrimitiveType.Cube, "BuildSpot " + (i + 1), spots.transform,
                new Vector3(xz.x, 0.1f, xz.y), new Vector3(1.8f, 0.2f, 1.8f), p.spot, true);
            spot.layer = buildSpotLayer;
            spot.AddComponent<BuildSpot>();
        }

        // Money and tower building.
        GameObject systems = new GameObject("GameSystems");
        MoneyManager money = systems.AddComponent<MoneyManager>();
        TowerBuilder builder = systems.AddComponent<TowerBuilder>();
        SetRef(builder, "cam", cam);
        SetInt(builder, "buildSpotLayer", 1 << buildSpotLayer);
        SetRef(builder, "money", money);

        // UI
        Canvas canvas = CreateCanvas();
        CreateEventSystem();
        Vector2 middle = new Vector2(0.5f, 0.5f);
        Vector2 topLeft = new Vector2(0f, 1f);

        RectTransform hudRect = CreateRect("HUD", canvas.transform, middle, middle, Vector2.zero, Vector2.zero);
        Stretch(hudRect);
        RectTransform hudBackground = CreateRect("Background", hudRect, topLeft, topLeft, new Vector2(15f, -15f),
            new Vector2(470f, 145f));
        Image hudBackgroundImage = hudBackground.gameObject.AddComponent<Image>();
        hudBackgroundImage.color = new Color(0f, 0f, 0f, 0.45f);
        hudBackgroundImage.raycastTarget = false; // only for readability, must not block clicks
        TextMeshProUGUI healthText = CreateText(hudRect, "BaseHealthText", "Base Health: 20", 44, topLeft, topLeft,
            new Vector2(35f, -25f), new Vector2(440f, 60f), TextAlignmentOptions.Left);
        TextMeshProUGUI moneyText = CreateText(hudRect, "MoneyText", "Money: $150", 44, topLeft, topLeft,
            new Vector2(35f, -90f), new Vector2(440f, 60f), TextAlignmentOptions.Left);
        HUD hud = hudRect.gameObject.AddComponent<HUD>();
        SetRef(hud, "playerBase", playerBase);
        SetRef(hud, "money", money);
        SetRef(hud, "healthText", healthText);
        SetRef(hud, "moneyText", moneyText);

        Vector2 bottom = new Vector2(0.5f, 0f);
        RectTransform panelRect = CreateRect("TowerPanel", canvas.transform, bottom, bottom, new Vector2(0f, 15f),
            new Vector2(700f, 140f));
        panelRect.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
        Button basicButton = CreateButton(panelRect, "BasicTowerButton", "Basic Tower", middle,
            new Vector2(-170f, 0f), new Vector2(310f, 105f));
        Button heavyButton = CreateButton(panelRect, "HeavyTowerButton", "Heavy Tower", middle,
            new Vector2(170f, 0f), new Vector2(310f, 105f));

        TowerPanel towerPanel = panelRect.gameObject.AddComponent<TowerPanel>();
        SetRef(towerPanel, "builder", builder);
        SerializedObject panelSettings = new SerializedObject(towerPanel);
        SerializedProperty options = panelSettings.FindProperty("options");
        options.arraySize = 2;
        FillTowerOption(options.GetArrayElementAtIndex(0), "Basic Tower", prefabs.basicTower, basicButton);
        FillTowerOption(options.GetArrayElementAtIndex(1), "Heavy Tower", prefabs.heavyTower, heavyButton);
        panelSettings.ApplyModifiedPropertiesWithoutUndo();

        RectTransform gameOverRect = CreateRect("GameOverScreen", canvas.transform, middle, middle, Vector2.zero,
            Vector2.zero);
        Stretch(gameOverRect);
        RectTransform gameOverPanel = CreateRect("Panel", gameOverRect, middle, middle, Vector2.zero, Vector2.zero);
        Stretch(gameOverPanel);
        gameOverPanel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        TextMeshProUGUI gameOverText = CreateText(gameOverPanel, "GameOverText", "GAME OVER", 130, middle, middle,
            new Vector2(0f, 90f), new Vector2(1200f, 180f), TextAlignmentOptions.Center);
        gameOverText.fontStyle = FontStyles.Bold;
        Button restart = CreateButton(gameOverPanel, "RestartButton", "Restart", middle, new Vector2(0f, -90f),
            new Vector2(380f, 100f));

        GameOverScreen gameOver = gameOverRect.gameObject.AddComponent<GameOverScreen>();
        SetRef(gameOver, "playerBase", playerBase);
        SetRef(gameOver, "panel", gameOverPanel.gameObject);
        SetRef(gameOver, "restartButton", restart);
        gameOverPanel.gameObject.SetActive(false);

        EditorSceneManager.SaveScene(scene, GameScenePath);
    }

    static void FillTowerOption(SerializedProperty option, string displayName, Tower prefab, Button button)
    {
        option.FindPropertyRelative("displayName").stringValue = displayName;
        option.FindPropertyRelative("towerPrefab").objectReferenceValue = prefab;
        option.FindPropertyRelative("button").objectReferenceValue = button;
    }

    // ------------------------------------------------------------------ scene helpers

    static Camera MainCamera(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Camera found = root.GetComponentInChildren<Camera>(true);
            if (found != null) return found;
        }

        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.AddComponent<AudioListener>();
        return go.AddComponent<Camera>();
    }

    static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
        Vector3 localScale, Material material, bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (!keepCollider)
            Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    // ------------------------------------------------------------------ UI helpers

    static Canvas CreateCanvas()
    {
        GameObject go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        go.layer = UILayer;

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Scale With Screen Size keeps the layout the same at different resolutions.
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    static void CreateEventSystem()
    {
        GameObject go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_LEGACY_INPUT_MANAGER
        go.AddComponent<StandaloneInputModule>();
#else
        Type inputSystemModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModule != null)
            go.AddComponent(inputSystemModule);
        else
            go.AddComponent<StandaloneInputModule>();
#endif
    }

    static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 pivot,
        Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = UILayer;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, Vector2 anchor,
        Vector2 pivot, Vector2 anchoredPosition, Vector2 size, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent, anchor, pivot, anchoredPosition, size);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    static Button CreateButton(Transform parent, string name, string text, Vector2 anchor,
        Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform rect = CreateRect(name, parent, anchor, new Vector2(0.5f, 0.5f), anchoredPosition, size);

        Image image = rect.gameObject.AddComponent<Image>();
        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }
        image.color = new Color(0.18f, 0.22f, 0.30f);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI label = CreateText(rect, "Label", text, 38, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        return button;
    }

    // ------------------------------------------------------------------ serialized field helpers
    // The scripts keep their fields private with [SerializeField], so they are set the same way
    // the Inspector does it.

    static void SetProperty(Object target, string field, Action<SerializedProperty> assign)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null)
            throw new Exception("Field '" + field + "' not found on " + target.GetType().Name);
        assign(property);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetFloat(Object target, string field, float value)
    {
        SetProperty(target, field, p => p.floatValue = value);
    }

    static void SetInt(Object target, string field, int value)
    {
        SetProperty(target, field, p => p.intValue = value);
    }

    static void SetRef(Object target, string field, Object value)
    {
        SetProperty(target, field, p => p.objectReferenceValue = value);
    }

    static void SetRefs(Object target, string field, Object[] values)
    {
        SetProperty(target, field, p =>
        {
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        });
    }
}
