using System.Collections.Generic;
using UnityEngine;

public partial class PacJamDemoController : MonoBehaviour
{
    [Header("Optional Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip pelletSfx;
    [SerializeField] private AudioClip powerPelletSfx;
    [SerializeField] private AudioClip ghostEatenSfx;
    [SerializeField] private AudioClip playerHitSfx;

    [Header("Optional VFX")]
    [SerializeField] private ParticleSystem pelletFxPrefab;
    [SerializeField] private ParticleSystem ghostEatFxPrefab;
    [SerializeField] private ParticleSystem playerHitFxPrefab;

    private const float CellSize = 1f;
    private const float PlayerSpeed = 5f;
    private const float GhostScatterSpeed = 3.9f;
    private const float GhostChaseSpeed = 4.3f;
    private const float GhostFrightenedSpeed = 2.8f;
    private const float GhostEatenSpeed = 6.5f;
    private const float FrightenedDuration = 6f;
    private const float RespawnPauseDuration = 0.75f;
    private const float CollisionDistance = 0.7f;
    private const float IndicatorForwardOffset = 0.62f;
    private const float CenterEpsilonSqr = 0.00004f;
    private const float PlayerPreTurnWindow = 0.2f;
    private const float FallbackDeltaTime = 1f / 120f;
    private const float SimStep = 1f / 120f;
    private const int MaxSimStepsPerFrame = 8;
    private const string BuildTag = "B2026.03.11.3";

    private static readonly Color PelletColor = new Color(0.99f, 0.88f, 0.26f);
    private static readonly Color PowerPelletColor = new Color(1.0f, 0.45f, 0.08f);
    private static readonly float[] ModeDurations = { 7f, 20f, 7f, 20f, 5f, 20f, 5f, 999f };
    private static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
    };

    private static readonly string[] MapRows =
    {
        "#####################",
        "#o........#........o#",
        "#.###.###.#.###.###.#",
        "#...................#",
        "#.###.#.#####.#.###.#",
        "#.....#...#...#.....#",
        "#####.###.#.###.#####",
        "#.........#.........#",
        "#.###.#.#####.#.###.#",
        "#...#.#...#...#.#...#",
        "###.#.###.#.###.#.###",
        "#.....#...P...#.....#",
        "#.###.#.#####.#.###.#",
        "#o..#...........#..o#",
        "###.#.#.#####.#.#.###",
        "#...................#",
        "#####################",
    };

    private readonly Dictionary<Vector2Int, Pellet> pellets = new Dictionary<Vector2Int, Pellet>();
    private readonly List<Ghost> ghosts = new List<Ghost>();
    private readonly Dictionary<Color32, Material> materialCache = new Dictionary<Color32, Material>();

    private Mesh circleMesh;
    private Mesh triMesh;
    private Mesh rectMesh;
    private Shader unlitShader;

    private char[,] map;
    private int width;
    private int height;
    private Vector2Int playerSpawn = new Vector2Int(10, 11);

    private Transform mapRoot;
    private Transform wallRoot;
    private Transform pelletRoot;
    private Transform actorRoot;

    private Actor player;
    private int score;
    private int lives = 3;
    private bool gameOver;
    private bool victory;
    private float respawnTimer;
    private float frightenedTimer;
    private float modeTimer;
    private int modePhase;
    private bool runtimeInitialized;
    private float simAccumulator;
    private float upPressTime;
    private float downPressTime;
    private float leftPressTime;
    private float rightPressTime;

    private void Awake()
    {
        unlitShader = ResolveRuntimeShader();
        if (unlitShader == null)
        {
            Debug.LogError("PacJamDemoController: no runtime shader found. Add URP/Unlit or Sprites/Default to Always Included Shaders.");
        }

        Time.timeScale = 1f;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;
        Application.runInBackground = true;
        runtimeInitialized = false;
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            runtimeInitialized = false;
        }
    }

    private void Update()
    {
        if (!runtimeInitialized)
        {
            ParseMap();
            BuildLevel();
            runtimeInitialized = true;
        }

        if ((gameOver || victory) && IsRestartPressed())
        {
            BuildLevel();
            return;
        }

        if (gameOver || victory)
        {
            return;
        }

        if (Time.timeScale <= 0.0001f)
        {
            Time.timeScale = 1f;
        }

        float dt = Time.unscaledDeltaTime;
        if (!(dt > 0.00001f) || float.IsNaN(dt) || float.IsInfinity(dt))
        {
            dt = FallbackDeltaTime;
        }
        else
        {
            dt = Mathf.Min(dt, 0.1f);
        }

        simAccumulator += dt;
        int simSteps = 0;
        while (simAccumulator >= SimStep && simSteps < MaxSimStepsPerFrame)
        {
            SimulateStep(SimStep);
            simAccumulator -= SimStep;
            simSteps++;
            if (gameOver || victory)
            {
                break;
            }
        }

        if (simSteps == 0 && !gameOver && !victory)
        {
            float tiny = Mathf.Max(simAccumulator, FallbackDeltaTime * 0.5f);
            SimulateStep(tiny);
            simAccumulator = 0f;
        }
    }

    private void OnGUI()
    {
        GUI.color = Color.white;
        GUI.Label(
            new Rect(12f, 12f, 540f, 28f),
            "WASD / Arrow Move | Score " + score + " | Lives " + lives + " | Mode " + ModeName() + " | Pellets " + pellets.Count + " | " + BuildTag);

        if (victory)
        {
            GUI.Label(new Rect(12f, 38f, 380f, 28f), "Level clear. Press R to rebuild demo.");
        }

        if (gameOver)
        {
            GUI.Label(new Rect(12f, 38f, 380f, 28f), "Game over. Press R to retry.");
        }

        Rect quitRect = new Rect(Screen.width - 118f, Screen.height - 42f, 106f, 30f);
        if (GUI.Button(quitRect, "Quit"))
        {
            QuitGame();
        }
    }
}
