using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public sealed class ForestdemoController : MonoBehaviour
{
    private sealed class SpikeStripData
    {
        public Rect Bounds;
    }

    private sealed class PushBoxData
    {
        public Transform Root;
        public Rigidbody2D Body;
        public BoxCollider2D Collider;
        public Vector2 RespawnPosition;
    }

    private sealed class PressureButtonData
    {
        public Transform Root;
        public MeshRenderer PlateRenderer;
        public Rect Trigger;
        public bool Pressed;
        public bool Latched;
    }

    private sealed class GateData
    {
        public Transform Root;
        public BoxCollider2D Collider;
        public MeshRenderer FillRenderer;
        public Vector2 ClosedPosition;
        public Vector2 OpenPosition;
        public float OpenAmount;
        public PressureButtonData Button;
    }

    private sealed class SwingPlatformData
    {
        public Transform Root;
        public Rigidbody2D Body;
        public BoxCollider2D Collider;
        public Transform LeftChain;
        public Transform RightChain;
        public Vector2 AnchorCenter;
        public float AnchorSpacing;
        public float ChainLength;
        public Vector2 Size;
        public float SwingAmplitude;
        public float SwingSpeed;
        public float Phase;
        public Vector2 Position;
        public Vector2 PreviousPosition;
    }

    [Header("Audio")]
    [SerializeField] private AudioClip stageBgm;
    [SerializeField] private AudioClip jumpSfx;
    [SerializeField] private AudioClip deathSfx;
    [SerializeField] private AudioClip gemPickupSfx;
    [SerializeField] private AudioClip buttonSfx;
    [SerializeField] private AudioClip stageClearSfx;
    [SerializeField] private AudioClip spikeHitSfx;
    [SerializeField] private AudioClip fallDeathSfx;
    [SerializeField] private AudioClip gateToggleSfx;
    [SerializeField] private AudioClip goalReadySfx;
    [SerializeField] private AudioClip goalEnterSfx;
    [SerializeField] private AudioClip boxResetSfx;

    [Header("Quit Dialog")]
    [SerializeField] private Rect quitDialogStayUv = new Rect(0.153f, 0.701f, 0.230f, 0.166f);
    [SerializeField] private Rect quitDialogExitUv = new Rect(0.603f, 0.701f, 0.281f, 0.166f);

    private const int RoomWidth = 52;
    private const int RoomHeight = 28;
    private const float CameraSize = 15.2f;
    private const float CameraY = 14.0f;
    private const float FloorThickness = 1.2f;
    private const float WallThickness = 1.2f;
    private const float BottomFloorTop = 3.0f;
    private const float SecondFloorTop = 9.0f;
    private const float ThirdFloorTop = 15.0f;
    private const float TopFloorTop = 21.0f;
    private const float PlayerRadius = 0.36f;
    private const float MoveSpeed = 7.1f;
    private const float JumpSpeed = 18.8f;
    private const float CoyoteTime = 0.11f;
    private const float JumpBufferTime = 0.12f;
    private const float JumpCutVelocityFactor = 0.42f;
    private const float RespawnInvulnerability = 1.1f;
    private const float GateOpenSpeed = 3.1f;
    private const float StageClearDelay = 1.35f;
    private const float GateLiftDistance = 4.0f;
    private const float BoxSize = 0.96f;
    private const float BoxHorizontalDamping = 7.2f;
    private const float PushBoxMass = 7.5f;
    private const float PushBoxDrag = 5.4f;
    private const float BoxResetMinY = -5.5f;
    private const float FallbackDeltaTime = 1f / 120f;
    private const string StartMenuSceneName = "PacJamStartMenu";
    private const string QuitDialogResourcePath = "PacJam/QuitDialog";
    private const string RestartButtonResourcePath = "PacJam/RestartButton";
    private const string SecretDiamondResourcePath = "Mariodemo/SecretDiamond";

    private static readonly Color BackgroundColor = new Color(0.08f, 0.11f, 0.08f, 1f);
    private static readonly Color BackdropTreeColor = new Color(0.12f, 0.20f, 0.12f, 1f);
    private static readonly Color BackdropLeafColor = new Color(0.14f, 0.28f, 0.16f, 1f);
    private static readonly Color WallColor = new Color(0.28f, 0.32f, 0.24f, 1f);
    private static readonly Color GroundColor = new Color(0.55f, 0.52f, 0.34f, 1f);
    private static readonly Color GroundTopColor = new Color(0.19f, 0.58f, 0.18f, 1f);
    private static readonly Color PlayerColor = new Color(0.98f, 0.88f, 0.18f, 1f);
    private static readonly Color GateColor = new Color(0.94f, 0.82f, 0.24f, 1f);
    private static readonly Color GateFrameColor = new Color(0.42f, 0.31f, 0.12f, 1f);
    private static readonly Color ButtonIdleColor = new Color(0.95f, 0.72f, 0.18f, 1f);
    private static readonly Color ButtonPressedColor = new Color(0.30f, 0.82f, 0.34f, 1f);
    private static readonly Color BoxOuterColor = new Color(0.32f, 0.34f, 0.36f, 1f);
    private static readonly Color BoxInnerColor = new Color(0.63f, 0.66f, 0.69f, 1f);
    private static readonly Color PlatformColor = new Color(0.51f, 0.34f, 0.16f, 1f);
    private static readonly Color PlatformAccentColor = new Color(0.73f, 0.50f, 0.28f, 1f);
    private static readonly Color ChainColor = new Color(0.78f, 0.78f, 0.82f, 1f);
    private static readonly Color DoorFrameColor = new Color(0.96f, 0.81f, 0.24f, 1f);
    private static readonly Color DoorFillColor = new Color(0.15f, 0.17f, 0.14f, 1f);
    private static readonly Color DoorReadyColor = new Color(0.33f, 0.85f, 0.38f, 1f);
    private static readonly Color SpikeColor = new Color(0.14f, 0.82f, 0.22f, 1f);
    private static readonly Color LifeActiveColor = new Color(0.18f, 0.86f, 0.33f, 1f);
    private static readonly Color LifeLostColor = new Color(0.89f, 0.25f, 0.22f, 1f);

    private readonly List<SpikeStripData> spikes = new List<SpikeStripData>();
    private readonly List<PushBoxData> pushBoxes = new List<PushBoxData>();
    private readonly List<PressureButtonData> buttons = new List<PressureButtonData>();
    private readonly List<GateData> gates = new List<GateData>();
    private readonly List<SwingPlatformData> swingPlatforms = new List<SwingPlatformData>();
    private readonly Dictionary<Color32, Material> materialCache = new Dictionary<Color32, Material>();
    private readonly HashSet<Collider2D> solidColliders = new HashSet<Collider2D>();
    private readonly Dictionary<Collider2D, PushBoxData> boxLookup = new Dictionary<Collider2D, PushBoxData>();

    private Mesh circleMesh;
    private Mesh triMesh;
    private Mesh rampMesh;
    private Mesh rectMesh;
    private Shader unlitShader;

    private Transform stageRoot;
    private Transform environmentRoot;
    private Transform actorRoot;
    private Transform hazardRoot;
    private Transform decoRoot;

    private Transform playerRoot;
    private MeshRenderer playerRenderer;
    private Rigidbody2D playerBody;
    private CircleCollider2D playerCollider;
    private Vector2 playerSpawn = new Vector2(4.2f, BottomFloorTop + PlayerRadius + 0.10f);

    private Rect goalRect;
    private MeshRenderer goalFillRenderer;

    private float desiredHorizontal;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool jumpReleaseRequested;
    private float invulnerabilityTimer;
    private bool runtimeInitialized;
    private bool gameOver;
    private bool stageClear;
    private bool showQuitConfirm;
    private bool respawnPending;
    private float respawnTimer;
    private float stageClearTimer;
    private bool goalWasReady;
    private int lives;

    private Camera mainCamera;
    private AudioSource sfxSource;
    private AudioSource bgmSource;
    private PhysicsMaterial2D frictionlessMaterial;
    private Texture2D loadedQuitDialogTexture;
    private bool quitDialogTextureChecked;
    private Texture2D loadedRestartButtonTexture;
    private bool restartButtonTextureChecked;
    private Texture2D loadedSecretDiamondTexture;
    private bool secretDiamondTextureChecked;
    private Texture2D quitStayIconTexture;
    private Texture2D quitExitIconTexture;
    private Texture2D uiCircleTexture;
    private GUIStyle uiQuitIconStyle;
    private static ForestdemoController activeInstance;

    private void Awake()
    {
        if (activeInstance != null && activeInstance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return;
        }

        activeInstance = this;
        unlitShader = ResolveRuntimeShader();
        Time.timeScale = 1f;
        Application.targetFrameRate = 120;
        Application.runInBackground = true;
        EnsureAudioSources();
        runtimeInitialized = false;
    }

    private void OnEnable()
    {
        runtimeInitialized = false;
        showQuitConfirm = false;
        respawnPending = false;
        respawnTimer = 0f;
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
        {
            activeInstance = null;
        }
    }

    private void Update()
    {
        if (!runtimeInitialized)
        {
            respawnPending = false;
            respawnTimer = 0f;
            lives = Mathf.Clamp(ForestdemoProgressState.Lives, 1, MariodemoProgressState.InitialLives);
            BuildStageRuntime();
            runtimeInitialized = true;
        }

        float dt = GetSafeUnscaledDeltaTime();

        if (showQuitConfirm)
        {
            StopManagedAudio();
            return;
        }

        if (respawnPending)
        {
            StopLoopSource(bgmSource);
            respawnTimer = Mathf.Max(0f, respawnTimer - dt);
            if (respawnTimer <= 0f)
            {
                BuildStageRuntime();
            }
            return;
        }

        if (gameOver)
        {
            StopLoopSource(bgmSource);
            return;
        }

        if (stageClear)
        {
            StopLoopSource(bgmSource);
            stageClearTimer = Mathf.Max(0f, stageClearTimer - dt);
            if (stageClearTimer <= 0f)
            {
                LoadEndingCutscene();
            }

            return;
        }

        ReadInput();
        TickTimers(dt);
        UpdateLoopingAudio();
        UpdateGoalPresentation();
    }

    private void FixedUpdate()
    {
        if (!runtimeInitialized || showQuitConfirm || respawnPending || gameOver || stageClear || playerBody == null)
        {
            return;
        }

        float dt = Time.fixedDeltaTime > 0.0001f ? Time.fixedDeltaTime : FallbackDeltaTime;
        bool grounded = IsPlayerGrounded();
        if (grounded)
        {
            coyoteTimer = CoyoteTime;
        }

        Vector2 velocity = playerBody.velocity;
        velocity.x = desiredHorizontal * MoveSpeed;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y = JumpSpeed;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            Play(jumpSfx);
        }

        if (jumpReleaseRequested)
        {
            if (velocity.y > 0f)
            {
                velocity.y *= JumpCutVelocityFactor;
            }

            jumpReleaseRequested = false;
        }

        playerBody.velocity = velocity;

        bool needsSync = false;
        needsSync |= TickPushBoxes(dt);
        needsSync |= TickSwingPlatforms();
        needsSync |= TickButtonsAndGates(dt);
        if (needsSync)
        {
            Physics2D.SyncTransforms();
        }

        CheckSpikePlayerCollisions();
        CheckGoalCollision();
        CheckFallDeath();
    }

    private void OnGUI()
    {
        DrawRuntimeUi();
    }

    private void BuildStageRuntime()
    {
        ResetRuntimeObjects();
        SetupCamera();
        EnsureAudioSources();

        respawnPending = false;
        respawnTimer = 0f;
        gameOver = false;
        stageClear = false;
        stageClearTimer = 0f;
        goalWasReady = false;
        desiredHorizontal = 0f;
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        jumpReleaseRequested = false;
        invulnerabilityTimer = RespawnInvulnerability;

        stageRoot = new GameObject("ForestdemoRoot").transform;
        stageRoot.SetParent(transform, false);
        environmentRoot = new GameObject("Environment").transform;
        environmentRoot.SetParent(stageRoot, false);
        actorRoot = new GameObject("Actors").transform;
        actorRoot.SetParent(stageRoot, false);
        hazardRoot = new GameObject("Hazards").transform;
        hazardRoot.SetParent(stageRoot, false);
        decoRoot = new GameObject("Decor").transform;
        decoRoot.SetParent(stageRoot, false);

        BuildWorld();
        CreatePlayer();
        ForestdemoProgressState.SetLives(lives);
        UpdateLoopingAudio();
        UpdateGoalPresentation();
        Physics2D.SyncTransforms();
    }

    private void ResetRuntimeObjects()
    {
        StopManagedAudio();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        spikes.Clear();
        pushBoxes.Clear();
        buttons.Clear();
        gates.Clear();
        swingPlatforms.Clear();
        materialCache.Clear();
        solidColliders.Clear();
        boxLookup.Clear();

        playerRoot = null;
        playerRenderer = null;
        playerBody = null;
        playerCollider = null;
        goalFillRenderer = null;
        goalRect = Rect.zero;
    }

    private void BuildWorld()
    {
        BuildBackdrop();
        BuildBounds();
        BuildBottomLayer();
        BuildSecondLayer();
        BuildThirdLayer();
        BuildTopLayer();
        BuildGoalDoor();
    }

    private void BuildBackdrop()
    {
        AddBackdropRect(new Vector2(7.0f, 10.5f), new Vector2(1.7f, 13.0f), BackdropTreeColor);
        AddBackdropCircle(new Vector2(7.0f, 18.6f), 6.4f, BackdropLeafColor);
        AddBackdropCircle(new Vector2(10.6f, 17.4f), 4.2f, BackdropLeafColor * new Color(1f, 1f, 1f, 0.86f));

        AddBackdropRect(new Vector2(24.8f, 11.8f), new Vector2(2.1f, 14.4f), BackdropTreeColor);
        AddBackdropCircle(new Vector2(24.4f, 20.0f), 7.4f, BackdropLeafColor);
        AddBackdropCircle(new Vector2(29.0f, 18.8f), 4.6f, BackdropLeafColor * new Color(1f, 1f, 1f, 0.82f));

        AddBackdropRect(new Vector2(42.6f, 11.1f), new Vector2(1.8f, 12.8f), BackdropTreeColor);
        AddBackdropCircle(new Vector2(42.2f, 19.1f), 6.9f, BackdropLeafColor);
        AddBackdropCircle(new Vector2(38.6f, 18.5f), 4.0f, BackdropLeafColor * new Color(1f, 1f, 1f, 0.80f));

        AddBackdropTri(new Vector2(15.5f, 25.0f), new Vector2(10.5f, 6.4f), BackdropLeafColor * new Color(1f, 1f, 1f, 0.70f), true);
        AddBackdropTri(new Vector2(33.6f, 24.2f), new Vector2(12.0f, 6.8f), BackdropLeafColor * new Color(1f, 1f, 1f, 0.65f), false);
    }

    private void BuildBounds()
    {
        AddSolidRect("LeftWall", new Vector2(WallThickness * 0.5f, RoomHeight * 0.5f), new Vector2(WallThickness, RoomHeight), WallColor, false);
        AddSolidRect("RightWallUpper", new Vector2(RoomWidth - WallThickness * 0.5f, 20.0f), new Vector2(WallThickness, 16.0f), WallColor, false);
        AddSolidRect("Ceiling", new Vector2(RoomWidth * 0.5f, RoomHeight - 0.6f), new Vector2(RoomWidth, 1.2f), WallColor, false);
    }

    private void BuildBottomLayer()
    {
        AddFloorStrip("BottomLeft", 7.2f, BottomFloorTop, 12.8f);
        AddFloorStrip("BottomMiddle", 21.0f, BottomFloorTop, 3.8f);
        AddFloorStrip("BottomRight", 35.8f, BottomFloorTop, 14.4f);
        AddSpikeStrip(new Vector2(16.4f, BottomFloorTop + 0.04f), 4.0f);
        AddSpikeStrip(new Vector2(25.6f, BottomFloorTop + 0.04f), 4.2f);
        AddRamp("LowerRamp", new Vector2(48.2f, 6.0f), new Vector2(6.8f, 6.0f), true);

    }

    private void BuildSecondLayer()
    {
        AddFloorStrip("SecondMain", 24.6f, SecondFloorTop, 34.8f);
        AddFloorStrip("SecondRightEntry", 44.0f, SecondFloorTop, 4.0f);
        AddRamp("MidRamp", new Vector2(5.6f, 12.0f), new Vector2(6.8f, 6.0f), false);
        AddSolidRect("SecondGateCeiling", new Vector2(39.9f, 13.6f), new Vector2(11.0f, 1.2f), GroundColor, false);
        CreateGateWithButton(
            "Lower",
            new Vector2(38.8f, SecondFloorTop + 1.80f),
            new Vector2(42.5f, SecondFloorTop + 0.08f),
            new Vector2(44.2f, SecondFloorTop + BoxSize * 0.5f));
    }

    private void BuildThirdLayer()
    {
        AddFloorStrip("ThirdLeft", 10.2f, ThirdFloorTop, 9.8f);
        AddFloorStrip("ThirdRight", 36.8f, ThirdFloorTop, 10.4f);
        AddSolidRect("ThirdSpikeShield", new Vector2(23.35f, 11.76f), new Vector2(16.6f, 0.72f), GroundColor, true);
        AddSpikeStrip(new Vector2(23.2f, 13.25f), 16.0f);
        AddSwingPlatform(new Vector2(18.2f, 19.0f), 3.8f, 1.8f, new Vector2(3.8f, 0.54f), 0.34f, 1.35f, 0f);
        AddSwingPlatform(new Vector2(27.6f, 19.1f), 4.0f, 1.8f, new Vector2(3.9f, 0.54f), 0.38f, 1.55f, 1.4f);
        AddRamp("UpperRamp", new Vector2(44.2f, 18.0f), new Vector2(6.8f, 6.0f), true);
        AddFloorStrip("UpperRampLanding", 49.0f, TopFloorTop, 2.2f);
    }

    private void BuildTopLayer()
    {
        AddFloorStrip("TopMain", 26.1f, TopFloorTop, 37.4f);
        AddSolidRect("UpperGateCeiling", new Vector2(24.2f, 25.2f), new Vector2(9.0f, 1.2f), GroundColor, false);
        CreateGateWithButton(
            "Upper",
            new Vector2(23.8f, TopFloorTop + 1.80f),
            new Vector2(31.2f, TopFloorTop + 0.08f),
            new Vector2(39.6f, TopFloorTop + BoxSize * 0.5f));
    }


    private void BuildGoalDoor()
    {
        AddFloorStrip("GoalDoorFloor", 5.0f, TopFloorTop, 4.4f);
        GameObject doorRoot = new GameObject("GoalDoor");
        doorRoot.transform.SetParent(environmentRoot, false);
        doorRoot.transform.position = new Vector3(5.0f, TopFloorTop + 0.92f, -0.08f);
        CreateShape("Frame", GetRectMesh(), new Vector3(1.45f, 1.95f, 1f), DoorFrameColor, doorRoot.transform, 0f);
        goalFillRenderer = CreateShape("Fill", GetRectMesh(), new Vector3(0.98f, 1.42f, 1f), DoorFillColor, doorRoot.transform, -0.01f).GetComponent<MeshRenderer>();
        CreateShape("CenterOrb", GetCircleMesh(), new Vector3(0.26f, 0.26f, 1f), PlayerColor, doorRoot.transform, -0.02f).transform.localPosition = new Vector3(0f, 0.04f, -0.02f);
        goalRect = new Rect(3.95f, TopFloorTop - 0.12f, 2.10f, 2.45f);
    }

    private void CreatePlayer()
    {
        GameObject root = new GameObject("Player");
        root.transform.SetParent(actorRoot, false);
        root.transform.position = new Vector3(playerSpawn.x, playerSpawn.y, -0.24f);
        playerRenderer = CreateShape("Body", GetCircleMesh(), new Vector3(0.76f, 0.76f, 1f), PlayerColor, root.transform, 0f).GetComponent<MeshRenderer>();
        playerBody = root.AddComponent<Rigidbody2D>();
        playerBody.gravityScale = 3.9f;
        playerBody.freezeRotation = true;
        playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        playerCollider = root.AddComponent<CircleCollider2D>();
        playerCollider.radius = PlayerRadius;
        playerCollider.sharedMaterial = GetFrictionlessMaterial();
        playerRoot = root.transform;
    }

    private void CreateGateWithButton(string namePrefix, Vector2 gateCenter, Vector2 buttonCenter, Vector2 boxCenter)
    {
        PressureButtonData button = AddPressureButton(namePrefix + "Button", buttonCenter, 1.26f);
        AddGate(namePrefix + "Gate", gateCenter, 0.82f, 3.60f, button);
        AddPushBox(namePrefix + "Box", boxCenter);
    }

    private void AddFloorStrip(string name, float centerX, float topY, float width)
    {
        AddSolidRect(name, new Vector2(centerX, topY - FloorThickness * 0.5f), new Vector2(width, FloorThickness), GroundColor, true);
    }

    private void AddBackdropRect(Vector2 center, Vector2 size, Color color)
    {
        GameObject root = new GameObject("BackdropRect");
        root.transform.SetParent(decoRoot, false);
        root.transform.position = new Vector3(center.x, center.y, 0.42f);
        CreateShape("Visual", GetRectMesh(), new Vector3(size.x, size.y, 1f), color, root.transform, 0f);
    }

    private void AddBackdropCircle(Vector2 center, float diameter, Color color)
    {
        GameObject root = new GameObject("BackdropCircle");
        root.transform.SetParent(decoRoot, false);
        root.transform.position = new Vector3(center.x, center.y, 0.45f);
        CreateShape("Visual", GetCircleMesh(), new Vector3(diameter, diameter, 1f), color, root.transform, 0f);
    }

    private void AddBackdropTri(Vector2 center, Vector2 size, Color color, bool riseRight)
    {
        GameObject root = new GameObject("BackdropTri");
        root.transform.SetParent(decoRoot, false);
        root.transform.position = new Vector3(center.x, center.y, 0.46f);
        CreateShape("Visual", GetTriMesh(), new Vector3(riseRight ? size.x : -size.x, size.y, 1f), color, root.transform, 0f);
    }

    private BoxCollider2D AddSolidRect(string name, Vector2 center, Vector2 size, Color color, bool addTopStrip)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(environmentRoot, false);
        root.transform.position = new Vector3(center.x, center.y, -0.08f);
        CreateShape("Base", GetRectMesh(), new Vector3(size.x, size.y, 1f), color, root.transform, 0f);
        if (addTopStrip)
        {
            CreateShape("TopStrip", GetRectMesh(), new Vector3(size.x * 0.94f, 0.16f, 1f), GroundTopColor, root.transform, -0.01f).transform.localPosition = new Vector3(0f, size.y * 0.5f - 0.14f, -0.01f);
        }

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.sharedMaterial = GetFrictionlessMaterial();
        solidColliders.Add(collider);
        return collider;
    }

    private void AddRamp(string name, Vector2 center, Vector2 size, bool risesRight)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(environmentRoot, false);
        root.transform.position = new Vector3(center.x, center.y, -0.07f);
        CreateShape("Base", GetRampMesh(), new Vector3(risesRight ? size.x : -size.x, size.y, 1f), GroundColor, root.transform, 0f);
        PolygonCollider2D collider = root.AddComponent<PolygonCollider2D>();
        collider.points = risesRight
            ? new[]
            {
                new Vector2(-0.5f * size.x, -0.5f * size.y),
                new Vector2(0.5f * size.x, -0.5f * size.y),
                new Vector2(0.5f * size.x, 0.5f * size.y),
            }
            : new[]
            {
                new Vector2(-0.5f * size.x, 0.5f * size.y),
                new Vector2(-0.5f * size.x, -0.5f * size.y),
                new Vector2(0.5f * size.x, -0.5f * size.y),
            };

        collider.sharedMaterial = GetFrictionlessMaterial();
        solidColliders.Add(collider);
    }

    private void AddSpikeStrip(Vector2 center, float width)
    {
        GameObject root = new GameObject("SpikeStrip_" + spikes.Count);
        root.transform.SetParent(hazardRoot, false);
        root.transform.position = new Vector3(center.x, center.y, -0.04f);
        int triCount = Mathf.Max(4, Mathf.RoundToInt(width / 0.46f));
        float spacing = width / triCount;
        float startX = -width * 0.5f + spacing * 0.5f;
        for (int i = 0; i < triCount; i++)
        {
            GameObject tri = CreateShape("Tri_" + i, GetTriMesh(), new Vector3(spacing * 1.12f, 0.62f, 1f), SpikeColor, root.transform, 0f);
            tri.transform.localPosition = new Vector3(startX + spacing * i, 0f, 0f);
        }

        spikes.Add(new SpikeStripData
        {
            Bounds = new Rect(center.x - width * 0.5f, center.y - 0.20f, width, 0.74f),
        });
    }

    private PressureButtonData AddPressureButton(string name, Vector2 center, float width)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(environmentRoot, false);
        root.transform.position = new Vector3(center.x, center.y, -0.06f);
        CreateShape("Base", GetRectMesh(), new Vector3(width, 0.20f, 1f), GateFrameColor, root.transform, 0f).transform.localPosition = new Vector3(0f, -0.08f, 0f);
        MeshRenderer plateRenderer = CreateShape("Plate", GetRectMesh(), new Vector3(width * 0.72f, 0.18f, 1f), ButtonIdleColor, root.transform, -0.01f).GetComponent<MeshRenderer>();
        PressureButtonData button = new PressureButtonData
        {
            Root = root.transform,
            PlateRenderer = plateRenderer,
            Trigger = new Rect(center.x - width * 0.5f, center.y - 0.02f, width, 0.95f),
            Pressed = false,
            Latched = false,
        };
        buttons.Add(button);
        return button;
    }

    private GateData AddGate(string name, Vector2 center, float width, float height, PressureButtonData button)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(environmentRoot, false);
        root.transform.position = new Vector3(center.x, center.y, -0.05f);
        CreateShape("Frame", GetRectMesh(), new Vector3(width + 0.18f, height + 0.10f, 1f), GateFrameColor, root.transform, 0f);
        MeshRenderer fillRenderer = CreateShape("Fill", GetRectMesh(), new Vector3(width, height, 1f), GateColor, root.transform, -0.01f).GetComponent<MeshRenderer>();
        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(width, height);
        collider.sharedMaterial = GetFrictionlessMaterial();
        solidColliders.Add(collider);
        GateData gate = new GateData
        {
            Root = root.transform,
            Collider = collider,
            FillRenderer = fillRenderer,
            ClosedPosition = center,
            OpenPosition = center + Vector2.up * GateLiftDistance,
            OpenAmount = 0f,
            Button = button,
        };
        gates.Add(gate);
        return gate;
    }

    private void AddPushBox(string name, Vector2 center)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(actorRoot, false);
        root.transform.position = new Vector3(center.x, center.y, -0.18f);
        CreateShape("Outer", GetRectMesh(), new Vector3(BoxSize, BoxSize, 1f), BoxOuterColor, root.transform, 0f);
        CreateShape("Inner", GetRectMesh(), new Vector3(BoxSize * 0.72f, BoxSize * 0.72f, 1f), BoxInnerColor, root.transform, -0.01f);
        CreateShape("Center", GetRectMesh(), new Vector3(BoxSize * 0.16f, BoxSize * 0.16f, 1f), BoxOuterColor, root.transform, -0.02f);

        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.gravityScale = 3.9f;
        body.mass = PushBoxMass;
        body.drag = PushBoxDrag;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(BoxSize * 0.92f, BoxSize * 0.92f);
        collider.sharedMaterial = GetFrictionlessMaterial();

        PushBoxData box = new PushBoxData
        {
            Root = root.transform,
            Body = body,
            Collider = collider,
            RespawnPosition = center,
        };
        pushBoxes.Add(box);
        boxLookup[collider] = box;
        solidColliders.Add(collider);
    }

    private void AddSwingPlatform(Vector2 anchorCenter, float chainLength, float anchorSpacing, Vector2 size, float amplitude, float speed, float phase)
    {
        GameObject root = new GameObject("SwingPlatform_" + swingPlatforms.Count);
        root.transform.SetParent(environmentRoot, false);
        root.transform.position = new Vector3(anchorCenter.x, anchorCenter.y - chainLength, -0.02f);

        CreateShape("TopLeft", GetCircleMesh(), new Vector3(0.28f, 0.28f, 1f), ChainColor, decoRoot, 0f).transform.position = new Vector3(anchorCenter.x - anchorSpacing * 0.5f, anchorCenter.y, 0.02f);
        CreateShape("TopRight", GetCircleMesh(), new Vector3(0.28f, 0.28f, 1f), ChainColor, decoRoot, 0f).transform.position = new Vector3(anchorCenter.x + anchorSpacing * 0.5f, anchorCenter.y, 0.02f);
        Transform leftChain = CreateShape("LeftChain", GetRectMesh(), new Vector3(0.10f, chainLength, 1f), ChainColor, decoRoot, 0.01f).transform;
        Transform rightChain = CreateShape("RightChain", GetRectMesh(), new Vector3(0.10f, chainLength, 1f), ChainColor, decoRoot, 0.01f).transform;
        CreateShape("Base", GetRectMesh(), new Vector3(size.x, size.y, 1f), PlatformColor, root.transform, 0f);
        CreateShape("Accent", GetRectMesh(), new Vector3(size.x * 0.90f, size.y * 0.22f, 1f), PlatformAccentColor, root.transform, -0.01f).transform.localPosition = new Vector3(0f, size.y * 0.12f, -0.01f);

        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.simulated = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.sharedMaterial = GetFrictionlessMaterial();
        solidColliders.Add(collider);

        Vector2 start = anchorCenter + Vector2.down * chainLength;
        swingPlatforms.Add(new SwingPlatformData
        {
            Root = root.transform,
            Body = body,
            Collider = collider,
            LeftChain = leftChain,
            RightChain = rightChain,
            AnchorCenter = anchorCenter,
            AnchorSpacing = anchorSpacing,
            ChainLength = chainLength,
            Size = size,
            SwingAmplitude = amplitude,
            SwingSpeed = speed,
            Phase = phase,
            Position = start,
            PreviousPosition = start,
        });
    }
    private void ReadInput()
    {
        bool left = false;
        bool right = false;
        bool jumpPressed = false;
        bool jumpReleased = false;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            left |= keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed;
            right |= keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed;
            jumpPressed |= keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame;
            jumpReleased |= keyboard.upArrowKey.wasReleasedThisFrame || keyboard.wKey.wasReleasedThisFrame || keyboard.spaceKey.wasReleasedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        left |= Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
        right |= Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
        jumpPressed |= Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space);
        jumpReleased |= Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.Space);
#endif

        desiredHorizontal = 0f;
        if (left && !right) desiredHorizontal = -1f;
        if (right && !left) desiredHorizontal = 1f;

        if (jumpPressed)
        {
            jumpBufferTimer = JumpBufferTime;
        }

        if (jumpReleased)
        {
            jumpReleaseRequested = true;
        }
    }

    private void TickTimers(float dt)
    {
        if (coyoteTimer > 0f) coyoteTimer -= dt;
        if (jumpBufferTimer > 0f) jumpBufferTimer -= dt;
        if (invulnerabilityTimer > 0f) invulnerabilityTimer -= dt;
    }

    private bool IsPlayerGrounded()
    {
        if (playerRoot == null)
        {
            return false;
        }

        Vector2 feet = (Vector2)playerRoot.position + Vector2.down * (PlayerRadius + 0.10f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(feet, 0.13f);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit == playerCollider)
            {
                continue;
            }

            if (solidColliders.Contains(hit))
            {
                return true;
            }
        }

        return false;
    }

    private bool TickPushBoxes(float dt)
    {
        bool resetAny = false;
        for (int i = 0; i < pushBoxes.Count; i++)
        {
            PushBoxData box = pushBoxes[i];
            if (box == null || box.Body == null)
            {
                continue;
            }

            Vector2 velocity = box.Body.velocity;
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, BoxHorizontalDamping * dt);
            box.Body.velocity = velocity;

            if (box.Body.position.y < BoxResetMinY)
            {
                box.Body.position = box.RespawnPosition;
                box.Body.velocity = Vector2.zero;
                Play(boxResetSfx);
                resetAny = true;
            }
        }

        return resetAny;
    }

    private bool TickSwingPlatforms()
    {
        bool movedAny = false;
        for (int i = 0; i < swingPlatforms.Count; i++)
        {
            SwingPlatformData platform = swingPlatforms[i];
            if (platform == null || platform.Root == null)
            {
                continue;
            }

            platform.PreviousPosition = platform.Position;
            float swing = Mathf.Sin(Time.time * platform.SwingSpeed + platform.Phase) * platform.SwingAmplitude;
            Vector2 offset = new Vector2(Mathf.Sin(swing) * platform.ChainLength, -Mathf.Cos(swing) * platform.ChainLength);
            platform.Position = platform.AnchorCenter + offset;
            Vector2 delta = platform.Position - platform.PreviousPosition;

            platform.Root.position = new Vector3(platform.Position.x, platform.Position.y, platform.Root.position.z);
            if (platform.Body != null)
            {
                platform.Body.position = platform.Position;
            }

            UpdateChainVisual(
                platform.LeftChain,
                platform.AnchorCenter + Vector2.left * platform.AnchorSpacing * 0.5f,
                platform.Position + Vector2.left * platform.AnchorSpacing * 0.42f + Vector2.up * platform.Size.y * 0.20f);
            UpdateChainVisual(
                platform.RightChain,
                platform.AnchorCenter + Vector2.right * platform.AnchorSpacing * 0.5f,
                platform.Position + Vector2.right * platform.AnchorSpacing * 0.42f + Vector2.up * platform.Size.y * 0.20f);

            if (playerBody != null && delta.sqrMagnitude > 0.000001f && IsPlayerStandingOnPlatform(platform))
            {
                playerBody.position += delta;
            }

            movedAny = true;
        }

        return movedAny;
    }

    private void UpdateChainVisual(Transform chain, Vector2 start, Vector2 end)
    {
        if (chain == null)
        {
            return;
        }

        Vector2 delta = end - start;
        float length = delta.magnitude;
        chain.position = new Vector3((start.x + end.x) * 0.5f, (start.y + end.y) * 0.5f, chain.position.z);
        chain.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);
        chain.localScale = new Vector3(0.10f, Mathf.Max(0.01f, length), 1f);
    }

    private bool IsPlayerStandingOnPlatform(SwingPlatformData platform)
    {
        if (platform == null || playerBody == null)
        {
            return false;
        }

        float topY = platform.Position.y + platform.Size.y * 0.5f;
        if (playerBody.velocity.y > 1.2f)
        {
            return false;
        }

        if (playerBody.position.y < topY + PlayerRadius - 0.18f || playerBody.position.y > topY + PlayerRadius + 0.28f)
        {
            return false;
        }

        Rect topRect = new Rect(platform.Position.x - platform.Size.x * 0.5f, topY - 0.16f, platform.Size.x, 0.28f);
        return CircleIntersectsRect(playerBody.position, PlayerRadius, topRect);
    }

    private bool TickButtonsAndGates(float dt)
    {
        bool changed = false;

        for (int i = 0; i < buttons.Count; i++)
        {
            PressureButtonData button = buttons[i];
            bool pressed = button.Pressed || IsButtonPressed(button);
            if (button.Pressed == pressed)
            {
                continue;
            }
            button.Pressed = pressed;
            button.Latched |= pressed;
            if (button.PlateRenderer != null)
            {
                button.PlateRenderer.sharedMaterial = GetMaterial(pressed ? ButtonPressedColor : ButtonIdleColor);
            }

            Play(buttonSfx);
            Play(gateToggleSfx);
            changed = true;
        }

        for (int i = 0; i < gates.Count; i++)
        {
            GateData gate = gates[i];
            float target = gate.Button != null && gate.Button.Pressed ? 1f : 0f;
            float previous = gate.OpenAmount;
            gate.OpenAmount = Mathf.MoveTowards(gate.OpenAmount, target, dt * GateOpenSpeed);
            if (Mathf.Abs(gate.OpenAmount - previous) < 0.0001f)
            {
                continue;
            }

            Vector2 position = Vector2.Lerp(gate.ClosedPosition, gate.OpenPosition, gate.OpenAmount);
            gate.Root.position = new Vector3(position.x, position.y, gate.Root.position.z);
            gate.Collider.enabled = gate.OpenAmount < 0.96f;
            if (gate.FillRenderer != null)
            {
                gate.FillRenderer.sharedMaterial = GetMaterial(Color.Lerp(GateColor, ButtonPressedColor, gate.OpenAmount * 0.32f));
            }

            changed = true;
        }

        return changed;
    }

    private bool IsButtonPressed(PressureButtonData button)
    {
        if (button == null)
        {
            return false;
        }

        Collider2D[] hits = Physics2D.OverlapBoxAll(button.Trigger.center, button.Trigger.size, 0f);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            if (hit == playerCollider || boxLookup.ContainsKey(hit))
            {
                return true;
            }
        }

        return false;
    }

    private void CheckSpikePlayerCollisions()
    {
        if (playerBody == null || invulnerabilityTimer > 0f)
        {
            return;
        }

        for (int i = 0; i < spikes.Count; i++)
        {
            if (CircleIntersectsRect(playerBody.position, PlayerRadius, spikes[i].Bounds))
            {
                KillPlayer(spikeHitSfx != null ? spikeHitSfx : deathSfx);
                return;
            }
        }
    }

    private void CheckGoalCollision()
    {
        if (playerBody == null)
        {
            return;
        }

        if (!AllPuzzleButtonsPressed())
        {
            return;
        }

        if (CircleIntersectsRect(playerBody.position, PlayerRadius, goalRect))
        {
            Play(goalEnterSfx);
            CompleteStage();
        }
    }

    private void CheckFallDeath()
    {
        if (playerBody != null && playerBody.position.y < -2.5f)
        {
            KillPlayer(fallDeathSfx != null ? fallDeathSfx : deathSfx);
        }
    }

    private void KillPlayer(AudioClip deathClip = null)
    {
        if (gameOver || stageClear || respawnPending)
        {
            return;
        }

        lives = Mathf.Max(0, lives - 1);
        ForestdemoProgressState.SetLives(lives);
        desiredHorizontal = 0f;
        StopLoopSource(sfxSource);
        StopLoopSource(bgmSource);
        if (playerBody != null)
        {
            playerBody.velocity = Vector2.zero;
            playerBody.simulated = false;
        }

        AudioClip resolvedDeathClip = deathClip != null ? deathClip : deathSfx;
        Play(resolvedDeathClip);

        if (lives <= 0)
        {
            gameOver = true;
            return;
        }

        respawnPending = true;
        respawnTimer = Mathf.Max(0.15f, resolvedDeathClip != null ? resolvedDeathClip.length : 0.75f);
    }

    private void CompleteStage()
    {
        if (stageClear)
        {
            return;
        }

        stageClear = true;
        stageClearTimer = Mathf.Max(StageClearDelay, stageClearSfx != null ? stageClearSfx.length + 0.10f : StageClearDelay);
        if (playerBody != null)
        {
            playerBody.velocity = Vector2.zero;
            playerBody.simulated = false;
        }

        Play(stageClearSfx);
        StopLoopSource(bgmSource);
    }

    private void RestartStage()
    {
        lives = MariodemoProgressState.InitialLives;
        ForestdemoProgressState.ResetProgress();
        BuildStageRuntime();
    }

    private bool AllPuzzleButtonsPressed()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            if (!buttons[i].Latched)
            {
                return false;
            }
        }

        return buttons.Count > 0;
    }

    private void UpdateGoalPresentation()
    {
        if (goalFillRenderer == null)
        {
            return;
        }

        bool ready = AllPuzzleButtonsPressed();
        if (ready && !goalWasReady)
        {
            Play(goalReadySfx);
        }

        goalWasReady = ready;
        goalFillRenderer.sharedMaterial = GetMaterial(ready ? DoorReadyColor : DoorFillColor);
    }

    private void SetupCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            mainCamera = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
        }

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = CameraSize;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = BackgroundColor;
        mainCamera.transform.rotation = Quaternion.identity;
        mainCamera.transform.position = new Vector3(RoomWidth * 0.5f, CameraY, -20f);
    }

    private void EnsureAudioSources()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(sfxSource, false, 0.95f);
        }

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(bgmSource, true, 0.56f);
        }
    }

    private static void ConfigureAudioSource(AudioSource source, bool loop, float volume)
    {
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = volume;
    }

    private void UpdateLoopingAudio()
    {
        SyncLoopSource(bgmSource, stageBgm);
    }

    private void Play(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        EnsureAudioSources();
        sfxSource.PlayOneShot(clip);
    }

    private static void SyncLoopSource(AudioSource source, AudioClip clip)
    {
        if (source == null)
        {
            return;
        }

        if (clip == null)
        {
            if (source.isPlaying)
            {
                source.Stop();
            }

            source.clip = null;
            return;
        }

        if (source.clip != clip)
        {
            source.Stop();
            source.clip = clip;
        }

        if (!source.isPlaying)
        {
            source.Play();
        }
    }

    private void StopManagedAudio()
    {
        StopLoopSource(sfxSource);
        StopLoopSource(bgmSource);
    }

    private static void StopLoopSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        if (source.isPlaying)
        {
            source.Stop();
        }

        source.clip = null;
    }
    private void DrawRuntimeUi()
    {
        EnsureUiStyles();
        DrawLivesIndicator();

        if (!showQuitConfirm && !gameOver && !stageClear)
        {
            DrawQuitButton();
        }

        if (gameOver)
        {
            DrawRestartOverlay();
        }

        if (showQuitConfirm)
        {
            DrawQuitDialog();
        }
    }

    private void EnsureUiStyles()
    {
        if (uiQuitIconStyle == null)
        {
            uiQuitIconStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
        }

        int quitSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.030f), 18, 24);
        uiQuitIconStyle.fontSize = quitSize;
        uiQuitIconStyle.normal.textColor = Color.white;

        if (uiCircleTexture == null)
        {
            uiCircleTexture = BuildCircleIconTexture(128);
        }
    }

    private void DrawLivesIndicator()
    {
        if (uiCircleTexture == null)
        {
            return;
        }

        float radius = Mathf.Clamp(Screen.height * 0.022f, 10f, 16f);
        float diameter = radius * 2f;
        float gap = Mathf.Clamp(radius * 0.48f, 6f, 10f);
        float margin = Mathf.Clamp(Screen.height * 0.022f, 12f, 20f);
        int remainingLives = Mathf.Clamp(lives, 0, MariodemoProgressState.InitialLives);

        for (int i = 0; i < MariodemoProgressState.InitialLives; i++)
        {
            bool isActive = i < remainingLives;
            float x = margin + i * (diameter + gap);
            Rect rect = new Rect(x, margin, diameter, diameter);
            DrawTintedTexture(rect, uiCircleTexture, isActive ? LifeActiveColor : LifeLostColor);
        }

        if (MariodemoProgressState.HasSecretDiamond)
        {
            Texture2D diamondTexture = GetSecretDiamondTexture();
            if (diamondTexture != null)
            {
                float x = margin + MariodemoProgressState.InitialLives * (diameter + gap);
                Rect rect = new Rect(x, margin, diameter, diameter);
                GUI.DrawTexture(rect, diamondTexture, ScaleMode.ScaleToFit, true);
            }
        }
    }

    private void DrawQuitButton()
    {
        float size = Mathf.Clamp(Screen.height * 0.064f, 42f, 56f);
        float margin = Mathf.Clamp(Screen.height * 0.020f, 12f, 20f);
        Rect rect = new Rect(Screen.width - margin - size, margin, size, size);

        DrawPanelShadow(rect, 4f);
        DrawFilledRect(rect, new Color(0.84f, 0.27f, 0.24f, 1f));

        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
        {
            showQuitConfirm = true;
        }

        GUI.Label(rect, "X", uiQuitIconStyle);
    }

    private void DrawQuitDialog()
    {
        Event evt = Event.current;
        if (evt.type == EventType.KeyDown)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                showQuitConfirm = false;
                evt.Use();
            }
            else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                ReturnToTitleMenu();
                evt.Use();
            }
        }

        Texture2D dialogTexture = GetQuitDialogTexture();
        if (dialogTexture == null)
        {
            showQuitConfirm = false;
            return;
        }

        EnsureQuitButtonIcons();
        DrawFilledRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.17f, 0.15f, 0.13f, 0.34f));
        Rect dialogRect = FitRectToScreen(dialogTexture.width, dialogTexture.height, Screen.width * 0.30f, Screen.height * 0.26f);
        DrawPanelShadow(dialogRect, 8f);
        GUI.DrawTexture(dialogRect, dialogTexture, ScaleMode.StretchToFill, true);

        Rect stayRect = UvToScreenRect(quitDialogStayUv, dialogRect);
        Rect exitRect = UvToScreenRect(quitDialogExitUv, dialogRect);

        if (DrawArtworkButton(stayRect, quitStayIconTexture, new Color(0.86f, 0.25f, 0.22f, 1f), new Color(0f, 0f, 0f, 0.05f)))
        {
            showQuitConfirm = false;
        }

        if (DrawArtworkButton(exitRect, quitExitIconTexture, new Color(0.31f, 0.62f, 0.25f, 1f), new Color(0.12f, 0.40f, 0.10f, 0.08f)))
        {
            ReturnToTitleMenu();
        }
    }

    private void DrawRestartOverlay()
    {
        Texture2D restartTexture = GetRestartButtonTexture();
        DrawFilledRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.40f));

        float width = Mathf.Min(360f, Screen.width - 88f);
        float height = Mathf.Min(250f, Screen.height - 104f);
        Rect rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        DrawPanel(rect);

        float buttonSize = Mathf.Min(rect.width, rect.height) * 0.46f;
        Rect buttonRect = CenterRect(rect.center, buttonSize, buttonSize);
        bool hovered = buttonRect.Contains(Event.current.mousePosition);
        if (hovered)
        {
            DrawFilledRect(ShrinkRect(buttonRect, buttonRect.width * 0.10f, buttonRect.height * 0.10f), new Color(0.10f, 0.46f, 0.12f, 0.08f));
        }

        if (GUI.Button(buttonRect, GUIContent.none, GUIStyle.none))
        {
            RestartStage();
            return;
        }

        if (restartTexture != null)
        {
            float scale = hovered ? 1.05f : 1f;
            Rect iconRect = CenterRect(buttonRect.center, buttonRect.width * scale, buttonRect.height * scale);
            GUI.DrawTexture(iconRect, restartTexture, ScaleMode.ScaleToFit, true);
        }
    }

    private Texture2D GetQuitDialogTexture()
    {
        if (!quitDialogTextureChecked)
        {
            quitDialogTextureChecked = true;
            loadedQuitDialogTexture = Resources.Load<Texture2D>(QuitDialogResourcePath);
        }

        return loadedQuitDialogTexture;
    }

    private Texture2D GetRestartButtonTexture()
    {
        if (!restartButtonTextureChecked)
        {
            restartButtonTextureChecked = true;
            loadedRestartButtonTexture = Resources.Load<Texture2D>(RestartButtonResourcePath);
        }

        return loadedRestartButtonTexture;
    }

    private Texture2D GetSecretDiamondTexture()
    {
        if (!secretDiamondTextureChecked)
        {
            secretDiamondTextureChecked = true;
            loadedSecretDiamondTexture = Resources.Load<Texture2D>(SecretDiamondResourcePath);
        }

        return loadedSecretDiamondTexture;
    }

    private void EnsureQuitButtonIcons()
    {
        if (quitStayIconTexture != null && quitExitIconTexture != null)
        {
            return;
        }

        quitStayIconTexture = BuildTriangleIconTexture(120, true);
        quitExitIconTexture = BuildSquareIconTexture(120);
    }

    private bool DrawArtworkButton(Rect rect, Texture2D iconTexture, Color iconColor, Color hoverTint)
    {
        if (rect.Contains(Event.current.mousePosition))
        {
            Rect tintRect = ShrinkRect(rect, Mathf.Max(2f, rect.height * 0.10f), Mathf.Max(2f, rect.height * 0.14f));
            DrawFilledRect(tintRect, hoverTint);
        }

        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
        {
            return true;
        }

        if (iconTexture != null)
        {
            float iconSize = Mathf.Min(rect.width, rect.height) * 0.60f;
            Rect iconRect = CenterRect(rect.center, iconSize, iconSize);
            DrawTintedTexture(iconRect, iconTexture, iconColor);
        }

        return false;
    }

    private void DrawPanel(Rect rect)
    {
        DrawPanelShadow(rect, 6f);
        DrawFilledRect(rect, new Color(0.98f, 0.97f, 0.95f, 0.98f));
    }

    private static void DrawPanelShadow(Rect rect, float offset)
    {
        DrawFilledRect(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), new Color(0f, 0f, 0f, 0.10f));
    }

    private static Rect FitRectToScreen(float sourceWidth, float sourceHeight, float maxWidth, float maxHeight)
    {
        float aspect = sourceWidth / Mathf.Max(1f, sourceHeight);
        float width = Mathf.Min(maxWidth, maxHeight * aspect);
        float height = width / Mathf.Max(0.0001f, aspect);
        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * aspect;
        }

        return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
    }

    private static Rect UvToScreenRect(Rect uvRect, Rect parentRect)
    {
        return new Rect(
            parentRect.x + parentRect.width * uvRect.x,
            parentRect.y + parentRect.height * uvRect.y,
            parentRect.width * uvRect.width,
            parentRect.height * uvRect.height);
    }

    private static Rect ShrinkRect(Rect rect, float insetX, float insetY)
    {
        return new Rect(rect.x + insetX, rect.y + insetY, Mathf.Max(0f, rect.width - insetX * 2f), Mathf.Max(0f, rect.height - insetY * 2f));
    }

    private static Rect CenterRect(Vector2 center, float width, float height)
    {
        return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
    }

    private static void DrawTintedTexture(Rect rect, Texture texture, Color color)
    {
        Color oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
        GUI.color = oldColor;
    }

    private static void DrawFilledRect(Rect rect, Color color)
    {
        Color oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = oldColor;
    }
    private static Texture2D BuildTriangleIconTexture(int size, bool inverted)
    {
        Texture2D texture = CreateUiTexture(size);
        Color[] pixels = new Color[size * size];
        Vector2 a = inverted ? new Vector2(0.12f, 0.76f) : new Vector2(0.12f, 0.24f);
        Vector2 b = inverted ? new Vector2(0.88f, 0.76f) : new Vector2(0.88f, 0.24f);
        Vector2 c = inverted ? new Vector2(0.50f, 0.14f) : new Vector2(0.50f, 0.86f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                pixels[y * size + x] = PointInTriangle(p, a, b, c) ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static Texture2D BuildSquareIconTexture(int size)
    {
        Texture2D texture = CreateUiTexture(size);
        Color[] pixels = new Color[size * size];
        const float min = 0.18f;
        const float max = 0.82f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = (x + 0.5f) / size;
                float py = (y + 0.5f) / size;
                pixels[y * size + x] = px >= min && px <= max && py >= min && py <= max ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static Texture2D BuildCircleIconTexture(int size)
    {
        Texture2D texture = CreateUiTexture(size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(0.5f, 0.5f);
        const float radius = 0.44f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                float dist = Vector2.Distance(p, center);
                float alpha = Mathf.Clamp01((radius - dist) * size * 0.60f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static Texture2D CreateUiTexture(int size)
    {
        return new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float s1 = c.y - a.y;
        float s2 = c.x - a.x;
        float s3 = b.y - a.y;
        float s4 = p.y - a.y;
        float w1 = (a.x * s1 + s4 * s2 - p.x * s1) / (s3 * s2 - (b.x - a.x) * s1);
        float w2 = (s4 - w1 * s3) / s1;
        return w1 >= 0f && w2 >= 0f && w1 + w2 <= 1f;
    }

    private bool CircleIntersectsRect(Vector2 center, float radius, Rect rect)
    {
        float closestX = Mathf.Clamp(center.x, rect.xMin, rect.xMax);
        float closestY = Mathf.Clamp(center.y, rect.yMin, rect.yMax);
        Vector2 closest = new Vector2(closestX, closestY);
        return (center - closest).sqrMagnitude <= radius * radius;
    }

    private GameObject CreateShape(string name, Mesh mesh, Vector3 scale, Color color, Transform parent, float localZ)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0f, localZ);
        go.transform.localScale = scale;

        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = GetMaterial(color);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return go;
    }

    private Material GetMaterial(Color color)
    {
        Color32 key = (Color32)color;
        if (materialCache.TryGetValue(key, out Material material))
        {
            return material;
        }

        if (unlitShader == null)
        {
            unlitShader = ResolveRuntimeShader();
        }

        material = new Material(unlitShader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        materialCache[key] = material;
        return material;
    }

    private static Shader ResolveRuntimeShader()
    {
        bool usingSrp = GraphicsSettings.currentRenderPipeline != null;
        Shader shader = null;
        if (usingSrp)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null) return shader;
        }

        shader = Shader.Find("Sprites/Default");
        if (shader != null) return shader;
        shader = Shader.Find("Unlit/Color");
        if (shader != null) return shader;
        shader = Shader.Find("UI/Default");
        if (shader != null) return shader;
        shader = Shader.Find("Hidden/Internal-Colored");
        if (shader != null) return shader;
        return Shader.Find("Hidden/InternalErrorShader");
    }

    private Mesh GetCircleMesh()
    {
        if (circleMesh != null) return circleMesh;
        int segments = 28;
        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 6];
        vertices[0] = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, Mathf.Sin(angle) * 0.5f, 0f);
        }

        int index = 0;
        for (int i = 0; i < segments; i++)
        {
            int current = i + 1;
            int next = i == segments - 1 ? 1 : i + 2;
            triangles[index++] = 0;
            triangles[index++] = current;
            triangles[index++] = next;
            triangles[index++] = 0;
            triangles[index++] = next;
            triangles[index++] = current;
        }

        circleMesh = new Mesh { vertices = vertices, triangles = triangles, name = "ForestdemoCircle" };
        circleMesh.RecalculateNormals();
        circleMesh.RecalculateBounds();
        return circleMesh;
    }

    private Mesh GetTriMesh()
    {
        if (triMesh != null) return triMesh;
        triMesh = new Mesh
        {
            name = "ForestdemoTriangle",
            vertices = new[]
            {
                new Vector3(0f, 0.52f, 0f),
                new Vector3(-0.46f, -0.42f, 0f),
                new Vector3(0.46f, -0.42f, 0f),
            },
            triangles = new[] { 0, 1, 2, 0, 2, 1 },
        };
        triMesh.RecalculateNormals();
        triMesh.RecalculateBounds();
        return triMesh;
    }

    private Mesh GetRampMesh()
    {
        if (rampMesh != null) return rampMesh;
        rampMesh = new Mesh
        {
            name = "ForestdemoRamp",
            vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
            },
            triangles = new[] { 0, 1, 2, 0, 2, 1 },
        };
        rampMesh.RecalculateNormals();
        rampMesh.RecalculateBounds();
        return rampMesh;
    }

    private Mesh GetRectMesh()
    {
        if (rectMesh != null) return rectMesh;
        rectMesh = new Mesh
        {
            name = "ForestdemoRect",
            vertices = new[]
            {
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, -0.5f, 0f),
            },
            triangles = new[] { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 },
        };
        rectMesh.RecalculateNormals();
        rectMesh.RecalculateBounds();
        return rectMesh;
    }

    private PhysicsMaterial2D GetFrictionlessMaterial()
    {
        if (frictionlessMaterial == null)
        {
            frictionlessMaterial = new PhysicsMaterial2D("ForestdemoNoFriction")
            {
                friction = 0f,
                bounciness = 0f,
            };
        }

        return frictionlessMaterial;
    }

    private static float GetSafeUnscaledDeltaTime()
    {
        float dt = Time.unscaledDeltaTime;
        if (!(dt > 0.00001f) || float.IsNaN(dt) || float.IsInfinity(dt))
        {
            return FallbackDeltaTime;
        }

        return Mathf.Min(dt, 0.1f);
    }

    private void LoadEndingCutscene()
    {
        SceneManager.LoadScene(ForestdemoProgressState.EndingSceneName);
    }

    private void ReturnToTitleMenu()
    {
        SceneManager.LoadScene(StartMenuSceneName);
    }
}





