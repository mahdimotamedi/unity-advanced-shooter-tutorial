#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DemoSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/SwingShooterDemo.unity";
    private const string GeneratedFolder = "Assets/Generated";
    private const string MaterialFolder = "Assets/Generated/Materials";
    private const string PrefabFolder = "Assets/Generated/Prefabs";

    [MenuItem("Tools/Swing Shooter Demo/Build Demo Scene")]
    public static void BuildDemoScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureProjectFolders();
        EnsureTag("SwingAnchor");

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material groundMat = GetOrCreateMaterial("Mat_Ground", new Color(0.28f, 0.38f, 0.28f, 1f));
        Material playerMat = GetOrCreateMaterial("Mat_Player", new Color(0.18f, 0.42f, 0.9f, 1f));
        Material chainMat = GetOrCreateMaterial("Mat_Chain", new Color(0.78f, 0.78f, 0.72f, 1f));
        Material anchorMat = GetOrCreateMaterial("Mat_AnchorSphere", new Color(0.1f, 0.55f, 1f, 1f));
        Material gunTargetMat = GetOrCreateMaterial("Mat_Target_GunOnly", new Color(0.95f, 0.22f, 0.12f, 1f));
        Material spearTargetMat = GetOrCreateMaterial("Mat_Target_SpearOnly", new Color(0.2f, 0.62f, 1f, 1f));
        Material anyTargetMat = GetOrCreateMaterial("Mat_Target_Any", new Color(1f, 0.78f, 0.12f, 1f));
        Material darkMat = GetOrCreateMaterial("Mat_Dark", new Color(0.06f, 0.06f, 0.07f, 1f));
        Material woodMat = GetOrCreateMaterial("Mat_Wood", new Color(0.45f, 0.25f, 0.12f, 1f));
        Material steelMat = GetOrCreateMaterial("Mat_Steel", new Color(0.72f, 0.72f, 0.76f, 1f));
        Material bulletMat = GetOrCreateMaterial("Mat_Bullet", new Color(1f, 0.82f, 0.2f, 1f));

        GameObject bulletPrefab = CreateBulletPrefab(bulletMat);
        GameObject spearPrefab = CreateSpearPrefab(woodMat, steelMat);

        CreateLighting();
        CreateEnvironment(groundMat, darkMat);
        ScoreManager scoreManager = CreateScoreManager();

        GameObject player = CreatePlayer(playerMat, darkMat, woodMat, steelMat, chainMat, bulletPrefab, spearPrefab, scoreManager);
        CreateSwingAnchor(anchorMat, chainMat);
        CreateTargets(gunTargetMat, spearTargetMat, anyTargetMat);
        CreateHintObjects(chainMat, anyTargetMat);

        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = player;
        EditorGUIUtility.PingObject(player);
        Debug.Log("Swing Shooter Demo scene created: " + ScenePath);
    }

    private static void EnsureProjectFolders()
    {
        if (!Directory.Exists(GeneratedFolder)) Directory.CreateDirectory(GeneratedFolder);
        if (!Directory.Exists(MaterialFolder)) Directory.CreateDirectory(MaterialFolder);
        if (!Directory.Exists(PrefabFolder)) Directory.CreateDirectory(PrefabFolder);
        AssetDatabase.Refresh();
    }

    private static Material GetOrCreateMaterial(string name, Color color)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            mat.name = name;
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.color = color;
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    private static void CreateLighting()
    {
        GameObject lightGo = new GameObject("Sun Directional Light");
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        RenderSettings.ambientLight = new Color(0.48f, 0.5f, 0.56f, 1f);
        RenderSettings.skybox = null;
    }

    private static void CreateEnvironment(Material groundMat, Material darkMat)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(9f, 1f, 9f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        CreatePlatform("Start Platform", new Vector3(0f, 0.08f, -8f), new Vector3(7f, 0.16f, 7f), darkMat);
        CreatePlatform("Target Platform", new Vector3(0f, 0.1f, 24f), new Vector3(24f, 0.2f, 17f), darkMat);
        CreatePlatform("Swing Landing Platform", new Vector3(0f, 0.12f, 12f), new Vector3(10f, 0.24f, 4f), darkMat);
        CreatePlatform("Left Shooting Ledge", new Vector3(-15f, 1.1f, 27f), new Vector3(5f, 0.25f, 4f), darkMat);
        CreatePlatform("Right Shooting Ledge", new Vector3(15f, 1.1f, 27f), new Vector3(5f, 0.25f, 4f), darkMat);

        for (int i = 0; i < 10; i++)
        {
            float x = -20f + i * 4.4f;
            CreatePlatform("Low Cover " + i, new Vector3(x, 0.75f, 31f + (i % 2) * 2f), new Vector3(1.2f, 1.5f, 1.2f), darkMat);
        }
    }

    private static GameObject CreatePlatform(string name, Vector3 position, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private static ScoreManager CreateScoreManager()
    {
        GameObject scoreGo = new GameObject("ScoreManager");
        return scoreGo.AddComponent<ScoreManager>();
    }

    private static GameObject CreatePlayer(Material playerMat, Material darkMat, Material woodMat, Material steelMat, Material chainMat, GameObject bulletPrefab, GameObject spearPrefab, ScoreManager scoreManager)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 2.05f, -9f);
        player.GetComponent<Renderer>().sharedMaterial = playerMat;
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.42f;
        controller.center = Vector3.zero;
        controller.stepOffset = 0.45f;
        controller.slopeLimit = 50f;
        controller.skinWidth = 0.06f;
        controller.minMoveDistance = 0f;

        GameObject pivot = new GameObject("CameraPivot");
        pivot.transform.SetParent(player.transform);
        pivot.transform.localPosition = new Vector3(0f, 1.2f, 0f);

        GameObject camGo = new GameObject("PlayerCamera");
        camGo.transform.SetParent(pivot.transform);
        camGo.transform.localPosition = new Vector3(0.45f, 0.25f, -5.2f);
        camGo.transform.localRotation = Quaternion.identity;
        Camera cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 68f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 300f;
        camGo.AddComponent<AudioListener>();

        GameObject hand = new GameObject("SwingHandPoint");
        hand.transform.SetParent(player.transform);
        hand.transform.localPosition = new Vector3(0.35f, 0.85f, 0.25f);

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(camGo.transform);
        firePoint.transform.localPosition = new Vector3(0.33f, -0.24f, 0.82f);
        firePoint.transform.localRotation = Quaternion.identity;

        GameObject gunVisual = CreateGunVisual(camGo.transform, darkMat, steelMat);
        GameObject spearVisual = CreateHeldSpearVisual(camGo.transform, woodMat, steelMat);

        PlayerMotor motor = player.AddComponent<PlayerMotor>();
        motor.cameraPivot = pivot.transform;
        motor.playerCamera = cam;
        motor.walkSpeed = 6.2f;
        motor.mouseSensitivity = 2.1f;
        motor.swingAirControl = 8.5f;
        motor.groundedExternalDamping = 15f;
        motor.groundedExternalMaxSpeed = 4.8f;
        motor.groundSettleThreshold = 0.22f;

        ChainVisual chainVisual = player.AddComponent<ChainVisual>();
        chainVisual.chainMaterial = chainMat;
        chainVisual.linkCount = 38;
        chainVisual.linkLength = 0.36f;
        chainVisual.linkThickness = 0.065f;
        chainVisual.sideOffset = 0.065f;
        chainVisual.sagPerMeter = 0.018f;
        chainVisual.maxSag = 0.38f;

        SwingController swing = player.AddComponent<SwingController>();
        swing.playerCamera = cam;
        swing.handPoint = hand.transform;
        swing.dynamicChain = chainVisual;
        swing.maxGrabDistance = 45f;
        swing.closeGrabRadius = 6.25f;
        swing.swingKick = 2.75f;
        swing.releaseBoost = 0.38f;
        swing.maxReleaseSpeed = 8.25f;
        swing.maxSwingAngleFromVertical = 68f;
        swing.maxSideAngleFromGrabPlane = 38f;
        swing.swingEnergyDamping = 0.988f;
        swing.maxSwingSpeed = 11.5f;
        swing.ropeCorrectionStrength = 0.82f;

        ShooterController shooter = player.AddComponent<ShooterController>();
        shooter.playerCamera = cam;
        shooter.firePoint = firePoint.transform;
        shooter.bulletPrefab = bulletPrefab;
        shooter.spearPrefab = spearPrefab;
        shooter.gunVisual = gunVisual;
        shooter.spearVisual = spearVisual;

        DemoHUD hud = player.AddComponent<DemoHUD>();
        hud.shooter = shooter;
        hud.swing = swing;
        hud.scoreManager = scoreManager;

        return player;
    }

    private static GameObject CreateGunVisual(Transform parent, Material darkMat, Material steelMat)
    {
        GameObject root = new GameObject("GunVisual_FirstPerson");
        root.transform.SetParent(parent);
        root.transform.localPosition = new Vector3(0.42f, -0.36f, 0.95f);
        root.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "GunBody";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.22f, 0.18f, 0.48f);
        body.GetComponent<Renderer>().sharedMaterial = darkMat;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "GunBarrel";
        barrel.transform.SetParent(root.transform);
        barrel.transform.localPosition = new Vector3(0f, 0.04f, 0.42f);
        barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        barrel.transform.localScale = new Vector3(0.045f, 0.34f, 0.045f);
        barrel.GetComponent<Renderer>().sharedMaterial = steelMat;
        Object.DestroyImmediate(barrel.GetComponent<Collider>());

        return root;
    }

    private static GameObject CreateHeldSpearVisual(Transform parent, Material woodMat, Material steelMat)
    {
        GameObject root = new GameObject("SpearVisual_FirstPerson");
        root.transform.SetParent(parent);
        root.transform.localPosition = new Vector3(0.48f, -0.42f, 0.9f);
        root.transform.localRotation = Quaternion.Euler(8f, -8f, 0f);

        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "HeldSpearShaft";
        shaft.transform.SetParent(root.transform);
        shaft.transform.localPosition = new Vector3(0f, 0f, 0.35f);
        shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        shaft.transform.localScale = new Vector3(0.035f, 0.9f, 0.035f);
        shaft.GetComponent<Renderer>().sharedMaterial = woodMat;
        Object.DestroyImmediate(shaft.GetComponent<Collider>());

        GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        tip.name = "HeldSpearTip";
        tip.transform.SetParent(root.transform);
        tip.transform.localPosition = new Vector3(0f, 0f, 1.2f);
        tip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        tip.transform.localScale = new Vector3(0.08f, 0.18f, 0.08f);
        tip.GetComponent<Renderer>().sharedMaterial = steelMat;
        Object.DestroyImmediate(tip.GetComponent<Collider>());

        root.SetActive(false);
        return root;
    }

    private static void CreateSwingAnchor(Material anchorMat, Material chainMat)
    {
        GameObject root = new GameObject("SwingAnchor_SphereWithHangingChain");
        root.transform.position = new Vector3(0f, 11f, 13f);
        SwingAnchor anchor = root.AddComponent<SwingAnchor>();
        anchor.pivot = root.transform;
        anchor.grabRadius = 6.25f;
        root.tag = "SwingAnchor";

        GameObject staticChainRoot = new GameObject("StaticHangingChainVisual");
        staticChainRoot.transform.SetParent(root.transform);
        staticChainRoot.transform.localPosition = Vector3.zero;
        anchor.staticChainVisualRoot = staticChainRoot;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Blue Swing Sphere Pivot";
        sphere.transform.SetParent(root.transform);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale = Vector3.one * 2.4f;
        sphere.GetComponent<Renderer>().sharedMaterial = anchorMat;
        sphere.tag = "SwingAnchor";

        int chainLinks = 18;
        float spacing = 0.34f;
        Transform lastLink = null;

        for (int i = 0; i < chainLinks; i++)
        {
            GameObject link = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            link.name = "HangingChainLink_" + i.ToString("00");
            link.transform.SetParent(staticChainRoot.transform);
            float sway = (i % 2 == 0) ? 0.045f : -0.045f;
            link.transform.localPosition = new Vector3(sway, -1.45f - i * spacing, 0f);
            link.transform.localRotation = Quaternion.Euler(i % 2 == 0 ? 90f : 0f, 0f, i % 2 == 0 ? 0f : 90f);
            link.transform.localScale = new Vector3(0.11f, 0.22f, 0.11f);
            link.GetComponent<Renderer>().sharedMaterial = chainMat;
            link.tag = "SwingAnchor";
            lastLink = link.transform;
        }

        GameObject grabEnd = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        grabEnd.name = "Visible Grab End";
        grabEnd.transform.SetParent(staticChainRoot.transform);
        grabEnd.transform.localPosition = new Vector3(0f, -1.45f - chainLinks * spacing, 0f);
        grabEnd.transform.localScale = Vector3.one * 0.5f;
        grabEnd.GetComponent<Renderer>().sharedMaterial = chainMat;
        grabEnd.tag = "SwingAnchor";
        anchor.hangingChainEnd = grabEnd.transform;

        GameObject grabTrigger = new GameObject("Large Proximity Grab Trigger");
        grabTrigger.transform.SetParent(staticChainRoot.transform);
        grabTrigger.transform.position = grabEnd.transform.position;
        SphereCollider trigger = grabTrigger.AddComponent<SphereCollider>();
        trigger.radius = 3.2f;
        trigger.isTrigger = true;
        grabTrigger.tag = "SwingAnchor";

        if (lastLink != null)
        {
            GameObject label = new GameObject("ChainGrabZone_GizmoOnly");
            label.transform.SetParent(staticChainRoot.transform);
            label.transform.position = lastLink.position;
        }
    }

    private static void CreateTargets(Material gunTargetMat, Material spearTargetMat, Material anyTargetMat)
    {
        Vector3 spawnMin = new Vector3(-15f, 1.15f, 20f);
        Vector3 spawnMax = new Vector3(15f, 4.0f, 35f);

        for (int i = 0; i < 8; i++)
        {
            float x = -12.25f + i * 3.5f;
            Vector3 pos = new Vector3(x, 1.45f + (i % 2) * 0.55f, 23.5f + (i % 3) * 2.2f);
            PrimitiveType shape = i % 2 == 0 ? PrimitiveType.Cube : PrimitiveType.Cylinder;
            CreateTarget("Gun Target " + (i + 1), TargetWeaponRequirement.GunOnly, shape, pos, new Vector3(1.05f, 1.95f, 0.65f), gunTargetMat, spawnMin, spawnMax);
        }

        for (int i = 0; i < 8; i++)
        {
            float x = -12.25f + i * 3.5f;
            Vector3 pos = new Vector3(x, 2.15f + (i % 2) * 0.75f, 29.5f + (i % 3) * 1.7f);
            CreateTarget("Spear Target " + (i + 1), TargetWeaponRequirement.SpearOnly, PrimitiveType.Capsule, pos, new Vector3(0.82f, 1.25f, 0.82f), spearTargetMat, spawnMin, spawnMax);
        }

        for (int i = 0; i < 5; i++)
        {
            float x = -8f + i * 4f;
            Vector3 pos = new Vector3(x, 3.8f, 34.5f + (i % 2) * 1.4f);
            CreateTarget("Any Weapon Bonus " + (i + 1), TargetWeaponRequirement.Any, PrimitiveType.Sphere, pos, Vector3.one * 1.2f, anyTargetMat, spawnMin, spawnMax);
        }
    }

    private static GameObject CreateTarget(string name, TargetWeaponRequirement requirement, PrimitiveType shape, Vector3 position, Vector3 scale, Material mat, Vector3 spawnMin, Vector3 spawnMax)
    {
        GameObject target = GameObject.CreatePrimitive(shape);
        target.name = name;
        target.transform.position = position;
        target.transform.localScale = scale;
        target.GetComponent<Renderer>().sharedMaterial = mat;

        TargetDummy dummy = target.AddComponent<TargetDummy>();
        dummy.requiredWeapon = requirement;
        dummy.maxHealth = 3;
        dummy.pointsPerHit = requirement == TargetWeaponRequirement.Any ? 8 : 5;
        dummy.pointsOnDestroyed = requirement == TargetWeaponRequirement.Any ? 55 : 35;
        dummy.destroyDelay = 0.5f;
        dummy.spawnAreaMin = spawnMin;
        dummy.spawnAreaMax = spawnMax;

        return target;
    }

    private static void CreateHintObjects(Material chainMat, Material markerMat)
    {
        GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arrow.name = "Look Here Marker";
        arrow.transform.position = new Vector3(0f, 2.5f, 4f);
        arrow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        arrow.transform.localScale = new Vector3(0.08f, 2.6f, 0.08f);
        arrow.GetComponent<Renderer>().sharedMaterial = chainMat;
        Object.DestroyImmediate(arrow.GetComponent<Collider>());

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "Target Area Marker";
        marker.transform.position = new Vector3(0f, 3.2f, 24f);
        marker.transform.localScale = Vector3.one * 0.5f;
        marker.GetComponent<Renderer>().sharedMaterial = markerMat;
        Object.DestroyImmediate(marker.GetComponent<Collider>());
    }

    private static GameObject CreateBulletPrefab(Material bulletMat)
    {
        string path = PrefabFolder + "/BulletProjectile.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "BulletProjectile";
        bullet.transform.localScale = Vector3.one * 0.16f;
        bullet.GetComponent<Renderer>().sharedMaterial = bulletMat;
        Rigidbody rb = bullet.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 0.08f;
        bullet.AddComponent<Projectile>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bullet, path);
        Object.DestroyImmediate(bullet);
        return prefab;
    }

    private static GameObject CreateSpearPrefab(Material woodMat, Material steelMat)
    {
        string path = PrefabFolder + "/SpearProjectile.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject spear = new GameObject("SpearProjectile");
        Rigidbody rb = spear.AddComponent<Rigidbody>();
        rb.mass = 0.65f;
        rb.drag = 0.02f;

        CapsuleCollider col = spear.AddComponent<CapsuleCollider>();
        col.direction = 2;
        col.radius = 0.07f;
        col.height = 1.65f;
        col.center = new Vector3(0f, 0f, 0.05f);

        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Shaft";
        shaft.transform.SetParent(spear.transform);
        shaft.transform.localPosition = new Vector3(0f, 0f, -0.15f);
        shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        shaft.transform.localScale = new Vector3(0.045f, 0.72f, 0.045f);
        shaft.GetComponent<Renderer>().sharedMaterial = woodMat;
        Object.DestroyImmediate(shaft.GetComponent<Collider>());

        GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        tip.name = "Tip";
        tip.transform.SetParent(spear.transform);
        tip.transform.localPosition = new Vector3(0f, 0f, 0.66f);
        tip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        tip.transform.localScale = new Vector3(0.09f, 0.16f, 0.09f);
        tip.GetComponent<Renderer>().sharedMaterial = steelMat;
        Object.DestroyImmediate(tip.GetComponent<Collider>());

        spear.AddComponent<SpearProjectile>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(spear, path);
        Object.DestroyImmediate(spear);
        return prefab;
    }

    private static void EnsureTag(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty tag = tagsProp.GetArrayElementAtIndex(i);
            if (tag.stringValue == tagName) return;
        }

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
    }
}
#endif
