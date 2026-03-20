#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed partial class MariodemoController : MonoBehaviour
{
    private enum TileType { Ground, Brick, Question, Pipe, Goal }
    private enum MapPartId
    {
        StartLane = 1,
        BrickIntro = 2,
        FirstPipeTrap = 3,
        UpperTrapRun = 4,
        BulletPipeRun = 5,
        DoubleGapTrap = 6,
        GoalLane = 7,
    }

    private sealed class BlockData
    {
        public Vector2Int Cell;
        public TileType Type;
        public int PartId;
        public bool Used;
        public bool SpawnsEnemy;
        public bool SpawnsMushroom;
        public bool BreaksUnderHeavy;
        public int BreakClusterId;
        public GameObject Root;
        public MeshRenderer MainRenderer;
        public MeshRenderer[] Renderers;
        public Collider2D Collider;
        public bool HiddenUntilHit;
    }

    private sealed class EnemyData
    {
        public Transform Root;
        public MeshRenderer OutlineRenderer;
        public MeshRenderer FillRenderer;
        public Vector2 Position;
        public int Direction;
        public bool Alive;
        public int PartId;
        public float PatrolMinX;
        public float PatrolMaxX;
        public bool UsesPatrolRange;
        public bool LockPatrolOnWideGround;
        public bool UseGravity;
        public float VerticalVelocity;
        public bool PoweredUp;
    }
    private sealed class BulletData
    {
        public Transform Root;
        public MeshRenderer OutlineRenderer;
        public MeshRenderer FillRenderer;
        public Vector2 Position;
        public Vector2 Velocity;
        public bool Active;
        public int PartId;
    }
    private sealed class PipeSpawnerData
    {
        public Vector2 Origin;
        public Vector2 Direction;
        public float Cooldown;
        public float CooldownTimer;
        public int PartId;
        public bool Armed;
        public float SpeedMultiplier;
    }
    private sealed class FallingBrickTrapData
    {
        public BlockData Block;
        public Vector2 Position;
        public float VerticalVelocity;
        public bool Armed;
        public bool Falling;
    }
    private sealed class SpikeData
    {
        public Rect Bounds;
        public GameObject Root;
        public int PartId;
        public Collider2D Collider;
    }

    private sealed class RuntimeCoinData
    {
        public Transform Root;
        public Vector2 Origin;
        public float Elapsed;
    }

    private sealed class RuntimeMushroomData
    {
        public Transform Root;
        public Vector2 Position;
        public Vector2 SpawnOrigin;
        public float Emergence;
        public float VerticalVelocity;
        public int Direction;
        public bool Active;
        public int PartId;
    }

    private sealed class FallingBrokenBlockData
    {
        public Transform Root;
        public Vector2 Position;
        public Vector2 Velocity;
        public float AngularVelocity;
    }
    private sealed class FlagPoleData
    {
        public Transform Root;
        public MeshRenderer FlagRenderer;
        public Rect Trigger;
        public bool Activated;
    }

    private sealed class SecretWallData
    {
        public Transform Root;
        public BoxCollider2D Collider;
        public Rect Trigger;
        public int HitCount;
        public float ComboTimer;
        public bool ContactLatch;
        public bool Opened;
    }

    private sealed class SecretDiamondData
    {
        public Transform Root;
        public Vector2 Position;
    }

    [Header("Audio")]
    [SerializeField] private AudioClip stageBgm;
    [SerializeField] private AudioClip jumpSfx;
    [SerializeField] private AudioClip deathSfx;
    [SerializeField] private AudioClip gameOverSfx;
    [SerializeField] private AudioClip stompSfx;
    [SerializeField] private AudioClip blockHitSfx;
    [SerializeField] private AudioClip questionBlockSfx;
    [SerializeField] private AudioClip coinPopupSfx;
    [SerializeField] private AudioClip bulletFireSfx;
    [SerializeField] private AudioClip stageClearSfx;
    [SerializeField] private AudioClip mushroomPowerUpSfx;
    [SerializeField] private AudioClip secretWallOpenSfx;
    [SerializeField] private AudioClip secretDiamondPickupSfx;
    [SerializeField] private AudioClip heavyBreakSfx;
    [SerializeField] private AudioClip fallingBrickDropSfx;
    [SerializeField] private AudioClip flagPoleSfx;
    [SerializeField] private AudioClip goalEnterSfx;

    [Header("Quit Dialog")]
    [SerializeField] private Rect quitDialogStayUv = new Rect(0.153f, 0.701f, 0.230f, 0.166f);
    [SerializeField] private Rect quitDialogExitUv = new Rect(0.603f, 0.701f, 0.281f, 0.166f);

    private const float CellSize = 1f;
    private const int LevelWidth = 86;
    private const float FloorTop = 2f;
    private const int GoalColumn = 83;
    private const float CameraSize = 6.4f;
    private const float CameraY = 5.8f;
    private const float PlayerRadius = 0.34f;
    private const float EnemyRadius = 0.32f;
    private const float BulletRadius = 0.24f;
    private const float MoveSpeed = 6.4f;
    private const float JumpSpeed = 18.8f;
    private const float CoyoteTime = 0.11f;
    private const float JumpBufferTime = 0.12f;
    private const float JumpCutVelocityFactor = 0.42f;
    private const float RespawnInvulnerability = 1.2f;
    private const float HeadHitCooldown = 0.12f;
    private const float EnemySpeed = 2.15f;
    private const float EnemyFallSpeed = 6.2f;
    private const float EnemyGravity = 22f;
    private const float EnemyLaunchSpeed = 4.9f;
    private const float BulletSpeed = 7.2f;
    private const float MushroomRadius = 0.28f;
    private const float MushroomMoveSpeed = 2.45f;
    private const float MushroomGravity = 24f;
    private const float MushroomRiseHeight = 0.78f;
    private const float MushroomRiseDuration = 0.26f;
    private const float PlayerPoweredScale = 1.42f;
    private const float EnemyPoweredScale = 1.38f;
    private const float BrokenBlockGravity = 27f;
    private const float FallingBrickHalfSize = 0.47f;
    private const float FallingBrickGravity = 30f;
    private const float FallingBrickTriggerHalfWidth = 0.42f;
    private const float CoinRiseDuration = 0.52f;
    private const float CoinRiseHeight = 0.95f;
    private const float CoinRadius = 0.28f;
    private const float StageClearDelay = 1.35f;
    private const float GoalEntryDuration = 0.45f;
    private const float VictoryCueGap = 0.22f;
    private const float FlagPoleX = GoalColumn - 1.35f;
    private const float PipeTriggerApproachDistance = 1.45f;
    private const float PipeTriggerProjectionLead = 0.16f;
    private const float PipeTriggerRearmDistance = 2.35f;
    private const float PipeTriggerMinApproachSpeed = 1.1f;
    private const float WideGroundPatrolThreshold = 7.5f;
    private const float HiddenStartLeftExtent = 4.6f;
    private const float HiddenStartCameraTriggerX = -0.18f;
    private const float SecretWallComboWindow = 1.15f;
    private const float FallbackDeltaTime = 1f / 120f;
    private const string StartMenuSceneName = "PacJamStartMenu";
    private const string QuitDialogResourcePath = "PacJam/QuitDialog";
    private const string RestartButtonResourcePath = "PacJam/RestartButton";
    private const string SecretDiamondResourcePath = "Mariodemo/SecretDiamond";
    private const string StageBgmAssetPath = "Assets/Music/Mariodemo/MariodemoBgm.mp3";
    private const string JumpSfxAssetPath = "Assets/Music/Mariodemo/MariodemoJump.mp3";
    private const string DeathSfxAssetPath = "Assets/Music/Mariodemo/MariodemoDeath.mp3";
    private const string GameOverSfxAssetPath = "Assets/Music/Mariodemo/MariodemoGameOver.mp3";
    private const string CoinSfxAssetPath = "Assets/Music/Mariodemo/MariodemoCoin.mp3";
    private const string MushroomSfxAssetPath = "Assets/Music/Mariodemo/MariodemoMushroom.mp3";
    private const string FlagPoleSfxAssetPath = "Assets/Music/Mariodemo/MariodemoFlagPole.mp3";
    private const string StageClearSfxAssetPath = "Assets/Music/Mariodemo/MariodemoStageClear.mp3";

    private static readonly Color BackgroundColor = new Color(0.59f, 0.67f, 0.92f, 1f);
    private static readonly Color PlayerColor = new Color(0.98f, 0.88f, 0.20f, 1f);
    private static readonly Color GroundColor = new Color(0.69f, 0.46f, 0.23f, 1f);
    private static readonly Color GroundDetailColor = new Color(0.45f, 0.28f, 0.12f, 1f);
    private static readonly Color BrickColor = new Color(0.68f, 0.44f, 0.22f, 1f);
    private static readonly Color BrickDetailColor = new Color(0.33f, 0.20f, 0.09f, 1f);
    private static readonly Color QuestionColor = new Color(0.93f, 0.80f, 0.34f, 1f);
    private static readonly Color QuestionUsedColor = new Color(0.72f, 0.63f, 0.36f, 1f);
    private static readonly Color QuestionDetailColor = new Color(0.32f, 0.24f, 0.08f, 1f);
    private static readonly Color PipeColor = new Color(0.06f, 0.77f, 0.09f, 1f);
    private static readonly Color CoinColor = new Color(0.98f, 0.82f, 0.16f, 1f);
    private static readonly Color MushroomCapColor = new Color(0.93f, 0.22f, 0.19f, 1f);
    private static readonly Color MushroomSpotColor = new Color(0.98f, 0.96f, 0.94f, 1f);
    private static readonly Color MushroomStemColor = new Color(0.96f, 0.93f, 0.84f, 1f);
    private static readonly Color SpikeColor = new Color(0.18f, 0.72f, 0.20f, 1f);
    private static readonly Color EnemyOutlineColor = new Color(0.08f, 0.08f, 0.08f, 1f);
    private static readonly Color EnemyFillColor = new Color(0.98f, 0.98f, 0.98f, 1f);
    private static readonly Color GoalFrameColor = new Color(0.98f, 0.92f, 0.65f, 1f);
    private static readonly Color GoalFillColor = new Color(0.12f, 0.12f, 0.16f, 1f);
    private static readonly Color FlagPoleColor = new Color(0.92f, 0.92f, 0.88f, 1f);
    private static readonly Color FlagClothColor = new Color(0.98f, 0.90f, 0.36f, 1f);
    private static readonly Color FlagClothActiveColor = new Color(0.26f, 0.82f, 0.30f, 1f);
    private static readonly Color LifeActiveColor = new Color(0.18f, 0.86f, 0.33f, 1f);
    private static readonly Color LifeLostColor = new Color(0.89f, 0.25f, 0.22f, 1f);

    private readonly Dictionary<Vector2Int, BlockData> blocks = new Dictionary<Vector2Int, BlockData>();
    private readonly Dictionary<Collider2D, BlockData> blockLookup = new Dictionary<Collider2D, BlockData>();
    private readonly List<EnemyData> enemies = new List<EnemyData>();
    private readonly List<BulletData> bullets = new List<BulletData>();
    private readonly List<PipeSpawnerData> pipeSpawners = new List<PipeSpawnerData>();
    private readonly List<FallingBrickTrapData> fallingBrickTraps = new List<FallingBrickTrapData>();
    private readonly List<SpikeData> spikes = new List<SpikeData>();
    private readonly List<RuntimeCoinData> runtimeCoins = new List<RuntimeCoinData>();
    private readonly List<RuntimeMushroomData> runtimeMushrooms = new List<RuntimeMushroomData>();
    private readonly List<FallingBrokenBlockData> fallingBrokenBlocks = new List<FallingBrokenBlockData>();
    private readonly Dictionary<Color32, Material> materialCache = new Dictionary<Color32, Material>();

    private Mesh circleMesh;
    private Mesh triMesh;
    private Mesh rectMesh;
    private Shader unlitShader;

    private Transform stageRoot;
    private Transform environmentRoot;
    private Transform actorRoot;
    private Transform hazardRoot;

    private Transform playerRoot;
    private MeshRenderer playerRenderer;
    private Rigidbody2D playerBody;
    private CircleCollider2D playerCollider;
    private Vector2 playerSpawn = new Vector2(2.6f, FloorTop + PlayerRadius + 0.06f);
    private float desiredHorizontal;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool jumpReleaseRequested;
    private float invulnerabilityTimer;
    private float headHitCooldownTimer;
    private bool runtimeInitialized;
    private bool gameOver;
    private bool stageClear;
    private bool showQuitConfirm;
    private bool respawnPending;
    private float respawnTimer;
    private float stageClearTimer;
    private float stageClearEntryTimer;
    private bool stageClearCuePlayed;
    private float stageClearCueDelay;
    private int lives;
    private bool playerPoweredUp;
    private int nextBreakClusterId;
    private Vector3 stageClearStartPosition;
    private Vector2 goalDoorEntryTarget;
    private FlagPoleData goalFlagPole;
    private SecretWallData startSecretWall;
    private SecretDiamondData hiddenSecretDiamond;

    private Camera mainCamera;
    private AudioSource sfxSource;
    private AudioSource eventSfxSource;
    private AudioSource bgmSource;
    private Texture2D loadedQuitDialogTexture;
    private bool quitDialogTextureChecked;
    private Texture2D loadedRestartButtonTexture;
    private bool restartButtonTextureChecked;
    private Texture2D quitStayIconTexture;
    private Texture2D quitExitIconTexture;
    private Texture2D uiCircleTexture;
    private Texture2D loadedSecretDiamondTexture;
    private bool secretDiamondTextureChecked;
    private Sprite loadedSecretDiamondSprite;
    private GUIStyle uiQuitIconStyle;
    private PhysicsMaterial2D frictionlessMaterial;
    private static MariodemoController activeInstance;

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
            lives = Mathf.Clamp(MariodemoProgressState.Lives, 1, MariodemoProgressState.InitialLives);
            BuildStageRuntime();
            runtimeInitialized = true;
        }

        float dt = GetSafeUnscaledDeltaTime();

        if (showQuitConfirm)
        {
            StopLoopSource(bgmSource);
            return;
        }

        if (respawnPending)
        {
            StopLoopSource(sfxSource);
            StopLoopSource(bgmSource);
            respawnTimer = Mathf.Max(0f, respawnTimer - dt);
            if (respawnTimer <= 0f)
            {
                BuildStageRuntime();
            }
            return;
        }

        TickRuntimeCoins(dt);

        if (gameOver)
        {
            StopLoopSource(bgmSource);
            return;
        }

        if (stageClear)
        {
            StopLoopSource(bgmSource);
            TickStageClearSequence(dt);
            UpdatePlayerVisual();
            if (stageClearTimer <= 0f)
            {
                LoadForestdemoTransition();
            }
            return;
        }

        ReadInput();
        TickTimers(dt);
        UpdateLoopingAudio();
        UpdatePlayerVisual();
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
        TickPlayerHeadHit();
        TickRuntimeMushrooms(dt);
        TickBrokenBlocks(dt);
        TickFallingBrickTraps(dt);
        TickEnemies(dt);
        TickPipeSpawners(dt);
        TickBullets(dt);
        CheckMushroomPickups();
        TickStartSecretWall(dt);
        CheckSecretDiamondPickup();
        TickHeavyBreakableBlocks();
        CheckEnemyPlayerCollisions();
        CheckBulletPlayerCollisions();
        CheckSpikePlayerCollisions();
        CheckFlagPoleCollision();
        CheckGoalCollision();
        CheckFallDeath();
    }

    private void LateUpdate()
    {
        UpdateCameraFollow();
    }

    private void OnGUI()
    {
        DrawRuntimeUi();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignAudioClipIfMissing(ref stageBgm, StageBgmAssetPath);
        AutoAssignAudioClipIfMissing(ref jumpSfx, JumpSfxAssetPath);
        AutoAssignAudioClipIfMissing(ref deathSfx, DeathSfxAssetPath);
        AutoAssignAudioClipIfMissing(ref gameOverSfx, GameOverSfxAssetPath);
        AutoAssignAudioClipIfMissing(ref coinPopupSfx, CoinSfxAssetPath);
        AutoAssignAudioClipIfMissing(ref mushroomPowerUpSfx, MushroomSfxAssetPath);
        AutoAssignAudioClipIfMissing(ref flagPoleSfx, FlagPoleSfxAssetPath);
        AutoAssignAudioClipIfMissing(ref stageClearSfx, StageClearSfxAssetPath);
    }

    private static void AutoAssignAudioClipIfMissing(ref AudioClip clip, string assetPath)
    {
        if (clip != null) return;
        clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
    }
#endif
}





