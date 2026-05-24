#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BallRoomSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/BallRoomDemo.unity";
    private const string GeneratedFolder = "Assets/Generated";
    private const string MaterialFolder = "Assets/Generated/Materials";
    private const string PhysicsFolder = "Assets/Generated/PhysicsMaterials";

    [MenuItem("Tools/Ball Room Demo/Build Ball Room Scene")]
    public static void BuildBallRoomScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureProjectFolders();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Make small/medium impacts bounce instead of being absorbed by Unity's default threshold.
        Physics.bounceThreshold = 0.25f;

        Material floorMat = GetOrCreateMaterial("Mat_BallRoom_Floor", new Color(0.22f, 0.24f, 0.27f, 1f));
        Material wallMat = GetOrCreateMaterial("Mat_BallRoom_Wall", new Color(0.62f, 0.66f, 0.72f, 1f));
        Material ballMat = GetOrCreateMaterial("Mat_BallRoom_Ball", new Color(1f, 0.42f, 0.12f, 1f));
        Material stripeMat = GetOrCreateMaterial("Mat_BallRoom_BallStripe", new Color(0.08f, 0.08f, 0.1f, 1f));
        Material bumperMat = GetOrCreateMaterial("Mat_BallRoom_Bumper", new Color(0.1f, 0.52f, 0.92f, 1f));
        Material darkMat = GetOrCreateMaterial("Mat_BallRoom_Dark", new Color(0.07f, 0.07f, 0.08f, 1f));

        PhysicMaterial ballPhysics = GetOrCreatePhysicMaterial("Phys_BallRoom_BouncyBall", 0.025f, 0.025f, 0.88f, PhysicMaterialCombine.Minimum, PhysicMaterialCombine.Maximum);
        PhysicMaterial wallPhysics = GetOrCreatePhysicMaterial("Phys_BallRoom_BouncyWalls", 0.02f, 0.02f, 0.92f, PhysicMaterialCombine.Minimum, PhysicMaterialCombine.Maximum);
        PhysicMaterial floorPhysics = GetOrCreatePhysicMaterial("Phys_BallRoom_Floor", 0.08f, 0.08f, 0.68f, PhysicMaterialCombine.Minimum, PhysicMaterialCombine.Maximum);

        CreateLighting();
        CreateRoom(floorMat, wallMat, darkMat, floorPhysics, wallPhysics);
        CreateBumpers(bumperMat, wallPhysics);

        Rigidbody ball = CreateBall(ballMat, stripeMat, ballPhysics);
        Camera sceneCamera = CreateCamera(ball.transform);
        CreateGameController(sceneCamera, ball);

        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = ball.gameObject;
        EditorGUIUtility.PingObject(ball.gameObject);
        Debug.Log("Ball Room Demo scene created: " + ScenePath);
    }

    private static void EnsureProjectFolders()
    {
        if (!Directory.Exists(GeneratedFolder)) Directory.CreateDirectory(GeneratedFolder);
        if (!Directory.Exists(MaterialFolder)) Directory.CreateDirectory(MaterialFolder);
        if (!Directory.Exists(PhysicsFolder)) Directory.CreateDirectory(PhysicsFolder);
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
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.color = color;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static PhysicMaterial GetOrCreatePhysicMaterial(string name, float dynamicFriction, float staticFriction, float bounciness, PhysicMaterialCombine frictionCombine, PhysicMaterialCombine bounceCombine)
    {
        string path = PhysicsFolder + "/" + name + ".physicMaterial";
        PhysicMaterial mat = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(path);
        if (mat == null)
        {
            mat = new PhysicMaterial(name);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.dynamicFriction = dynamicFriction;
        mat.staticFriction = staticFriction;
        mat.bounciness = bounciness;
        mat.frictionCombine = frictionCombine;
        mat.bounceCombine = bounceCombine;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void CreateLighting()
    {
        GameObject keyLight = new GameObject("Ball Room Key Light");
        Light light = keyLight.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.shadows = LightShadows.Soft;
        keyLight.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

        GameObject fillLight = new GameObject("Ball Room Fill Light");
        Light fill = fillLight.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.intensity = 1.9f;
        fill.range = 16f;
        fill.transform.position = new Vector3(0f, 5.2f, -3.5f);

        RenderSettings.ambientLight = new Color(0.48f, 0.5f, 0.56f, 1f);
        RenderSettings.skybox = null;
    }

    private static void CreateRoom(Material floorMat, Material wallMat, Material darkMat, PhysicMaterial floorPhysics, PhysicMaterial wallPhysics)
    {
        CreateBox("Floor", new Vector3(0f, -0.05f, 0f), new Vector3(18f, 0.1f, 18f), floorMat, floorPhysics);
        CreateBox("Ceiling", new Vector3(0f, 7.05f, 0f), new Vector3(18f, 0.1f, 18f), darkMat, wallPhysics);
        CreateBox("Back Wall", new Vector3(0f, 3.5f, 9f), new Vector3(18f, 7f, 0.2f), wallMat, wallPhysics);
        CreateBox("Front Wall", new Vector3(0f, 3.5f, -9f), new Vector3(18f, 7f, 0.2f), wallMat, wallPhysics);
        CreateBox("Left Wall", new Vector3(-9f, 3.5f, 0f), new Vector3(0.2f, 7f, 18f), wallMat, wallPhysics);
        CreateBox("Right Wall", new Vector3(9f, 3.5f, 0f), new Vector3(0.2f, 7f, 18f), wallMat, wallPhysics);

        CreateBox("Room Center Mark", new Vector3(0f, 0.02f, 0f), new Vector3(0.12f, 0.02f, 17f), darkMat, null);
        CreateBox("Room Cross Mark", new Vector3(0f, 0.03f, 0f), new Vector3(17f, 0.02f, 0.12f), darkMat, null);
    }

    private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material mat, PhysicMaterial physicsMat)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = mat;

        Collider collider = box.GetComponent<Collider>();
        if (collider != null) collider.sharedMaterial = physicsMat;

        return box;
    }

    private static void CreateBumpers(Material bumperMat, PhysicMaterial wallPhysics)
    {
        GameObject parent = new GameObject("Bouncy Room Obstacles");

        GameObject bumperA = CreateBox("Angled Bumper A", new Vector3(-4.6f, 0.55f, 2.7f), new Vector3(3.8f, 0.8f, 0.45f), bumperMat, wallPhysics);
        bumperA.transform.rotation = Quaternion.Euler(0f, 34f, 0f);
        bumperA.transform.SetParent(parent.transform);

        GameObject bumperB = CreateBox("Angled Bumper B", new Vector3(4.6f, 0.55f, -2.7f), new Vector3(3.8f, 0.8f, 0.45f), bumperMat, wallPhysics);
        bumperB.transform.rotation = Quaternion.Euler(0f, -34f, 0f);
        bumperB.transform.SetParent(parent.transform);

        for (int i = 0; i < 4; i++)
        {
            float x = i < 2 ? -6.4f : 6.4f;
            float z = i % 2 == 0 ? -6.4f : 6.4f;
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Round Bumper " + (i + 1);
            post.transform.position = new Vector3(x, 0.6f, z);
            post.transform.localScale = new Vector3(0.55f, 0.6f, 0.55f);
            post.GetComponent<Renderer>().sharedMaterial = bumperMat;
            Collider col = post.GetComponent<Collider>();
            if (col != null) col.sharedMaterial = wallPhysics;
            post.transform.SetParent(parent.transform);
        }
    }

    private static Rigidbody CreateBall(Material ballMat, Material stripeMat, PhysicMaterial ballPhysics)
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Clickable Physics Ball";
        ball.transform.position = new Vector3(0f, 1f, 0f);
        ball.transform.localScale = Vector3.one * 1.35f;
        ball.GetComponent<Renderer>().sharedMaterial = ballMat;

        Collider collider = ball.GetComponent<Collider>();
        if (collider != null) collider.sharedMaterial = ballPhysics;

        Rigidbody rb = ball.AddComponent<Rigidbody>();
        rb.mass = 1.0f;
        rb.drag = 0.055f;
        rb.angularDrag = 0.075f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.maxAngularVelocity = 45f;

        BallBounceBooster bounceBooster = ball.AddComponent<BallBounceBooster>();
        bounceBooster.wallBounceBoost = 0.98f;
        bounceBooster.bumperBounceBoost = 1.03f;
        bounceBooster.floorBounceBoost = 0.82f;
                bounceBooster.maxSpeed = 28f;
        bounceBooster.velocityBlend = 0.62f;
        bounceBooster.linearDampingPerSecond = 0.22f;
        bounceBooster.angularDampingPerSecond = 0.32f;
        bounceBooster.sleepSpeed = 0.18f;
        bounceBooster.sleepDelay = 1.25f;

        GameObject stripeA = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stripeA.name = "Ball Direction Stripe Horizontal";
        stripeA.transform.SetParent(ball.transform);
        stripeA.transform.localPosition = Vector3.zero;
        stripeA.transform.localRotation = Quaternion.identity;
        stripeA.transform.localScale = new Vector3(0.76f, 0.018f, 0.76f);
        stripeA.GetComponent<Renderer>().sharedMaterial = stripeMat;
        Object.DestroyImmediate(stripeA.GetComponent<Collider>());

        GameObject stripeB = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stripeB.name = "Ball Direction Stripe Vertical";
        stripeB.transform.SetParent(ball.transform);
        stripeB.transform.localPosition = Vector3.zero;
        stripeB.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        stripeB.transform.localScale = new Vector3(0.76f, 0.018f, 0.76f);
        stripeB.GetComponent<Renderer>().sharedMaterial = stripeMat;
        Object.DestroyImmediate(stripeB.GetComponent<Collider>());

        return rb;
    }

    private static Camera CreateCamera(Transform target)
    {
        GameObject cameraGo = new GameObject("BallRoomCamera");
        cameraGo.tag = "MainCamera";
        cameraGo.transform.position = new Vector3(0f, 3.45f, -6.9f);
        cameraGo.transform.LookAt(target.position + Vector3.up * 0.35f);

        Camera cam = cameraGo.AddComponent<Camera>();
        cam.fieldOfView = 66f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 100f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
        cameraGo.AddComponent<AudioListener>();

        BallRoomCameraOrbit orbit = cameraGo.AddComponent<BallRoomCameraOrbit>();
        orbit.target = target;
        orbit.followHeight = 0.35f;
        orbit.distance = 7.2f;
        orbit.minDistance = 4.5f;
        orbit.maxDistance = 8.5f;
        orbit.pitch = 18f;
        orbit.minPitch = 10f;
        orbit.maxPitch = 48f;
        orbit.yaw = 0f;
        orbit.keepInsideRoom = true;
        orbit.roomHalfSize = 8.15f;
        orbit.minCameraHeight = 1.25f;
        orbit.maxCameraHeight = 6.35f;
        orbit.useSmoothing = true;
        orbit.positionSharpness = 18f;
        orbit.rotationSharpness = 22f;
        orbit.SnapToTarget();

        return cam;
    }

    private static void CreateGameController(Camera sceneCamera, Rigidbody ball)
    {
        GameObject resetPoint = new GameObject("Ball Reset Point");
        resetPoint.transform.position = new Vector3(0f, 1f, 0f);

        GameObject controller = new GameObject("Ball Room Game Controller");
        BallClickShooter shooter = controller.AddComponent<BallClickShooter>();
        shooter.sceneCamera = sceneCamera;
        shooter.ball = ball;
        shooter.resetPoint = resetPoint.transform;
        shooter.cameraOrbit = sceneCamera != null ? sceneCamera.GetComponent<BallRoomCameraOrbit>() : null;
        shooter.fullChargeTime = 1.35f;
        shooter.minImpulse = 3.5f;
        shooter.maxImpulse = 30f;
        shooter.upwardBias = 0.1f;
        shooter.cameraDirectionBlend = 0.32f;
        shooter.extraSpinMultiplier = 0.45f;
        shooter.clickAssistRadiusMultiplier = 1.85f;
        shooter.maxClickDistance = 80f;
        shooter.maxBallSpeed = 28f;

        BallRoomHUD hud = controller.AddComponent<BallRoomHUD>();
        hud.shooter = shooter;
    }
}
#endif
