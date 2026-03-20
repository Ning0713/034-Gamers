#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;

public partial class PacJamDemoController : MonoBehaviour
{
    [Header("Audio")]
    [InspectorName("SFX Source")] [SerializeField] private AudioSource sfxSource;
    [InspectorName("Pellet SFX")] [SerializeField] private AudioClip pelletSfx;
    [InspectorName("Power Pellet SFX")] [SerializeField] private AudioClip powerPelletSfx;
    [InspectorName("Ghost Eaten SFX")] [SerializeField] private AudioClip ghostEatenSfx;
    [InspectorName("Player Hit SFX")] [SerializeField] private AudioClip playerHitSfx;
    [InspectorName("Entrance SFX")] [SerializeField] private AudioClip entranceSfx;
    [InspectorName("Ghost Hunt Intro SFX")] [SerializeField] private AudioClip ghostHuntIntroSfx;
    [InspectorName("Ghost Hunt BGM")] [SerializeField] private AudioClip ghostHuntBgm;
    [InspectorName("Frightened Loop SFX")] [SerializeField] private AudioClip frightenedLoopSfx;
    [InspectorName("Chase Loop SFX")] [SerializeField] private AudioClip chaseLoopSfx;
    [InspectorName("Move Loop SFX")] [SerializeField] private AudioClip moveLoopSfx;

    [Header("VFX")]
    [InspectorName("Pellet FX Prefab")] [SerializeField] private ParticleSystem pelletFxPrefab;
    [InspectorName("Ghost Eat FX Prefab")] [SerializeField] private ParticleSystem ghostEatFxPrefab;
    [InspectorName("Player Hit FX Prefab")] [SerializeField] private ParticleSystem playerHitFxPrefab;

    [Header("Quit Dialog")]
    [InspectorName("Stay Button UV")] [SerializeField] private Rect quitDialogStayUv = new Rect(0.153f, 0.701f, 0.230f, 0.166f);
    [InspectorName("Exit Button UV")] [SerializeField] private Rect quitDialogExitUv = new Rect(0.603f, 0.701f, 0.281f, 0.166f);

    private const float CellSize = 1f;
    private const float PlayerSpeed = 5f;
    private const float GhostScatterSpeed = 3.9f;
    private const float GhostChaseSpeed = 4.3f;
    private const float GhostFrightenedSpeed = 2.8f;
    private const float GhostEatenSpeed = 6.5f;
    private const float GhostPreySpeed = 4.8f;
    private const float FrightenedDuration = 6f;
    private const float EnemyPauseDuration = 2f;
    private const float GhostHuntIntroDuration = 2f;
    private const float RespawnPauseDuration = 0.75f;
    private const float MariodemoTeleportDuration = 2f;
    private const float CollisionDistance = 0.7f;
    private const float IndicatorForwardOffset = 0.62f;
    private const float CenterEpsilonSqr = 0.00004f;
    private const float PlayerPreTurnWindow = 0.2f;
    private const float FallbackDeltaTime = 1f / 120f;
    private const float SimStep = 1f / 120f;
    private const int MaxSimStepsPerFrame = 8;
    private const int InitialLives = 3;
    private const string StartMenuSceneName = "PacJamStartMenu";
    private const string QuitDialogResourcePath = "PacJam/QuitDialog";
    private const string RestartButtonResourcePath = "PacJam/RestartButton";
    private static readonly Color PelletColor = new Color(0.99f, 0.88f, 0.26f);
    private static readonly Color PowerPelletColor = new Color(1.0f, 0.45f, 0.08f);
    private static readonly Color LifeActiveColor = new Color(0.18f, 0.86f, 0.33f, 1f);
    private static readonly Color LifeLostColor = new Color(0.89f, 0.25f, 0.22f, 1f);
    private static readonly Color GhostGuardedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
    private static readonly Color GhostPreyColor = new Color(0.58f, 0.58f, 0.58f, 1f);
    private static readonly Color GhostCapturedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
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
    private int lives = InitialLives;
    private bool gameOver;
    private bool victory;
    private float respawnTimer;
    private float frightenedTimer;
    private float enemyPauseTimer;
    private float ghostHuntIntroTimer;
    private float mariodemoTransitionTimer;
    private float modeTimer;
    private int modePhase;
    private StagePhase stagePhase;
    private bool runtimeInitialized;
    private bool mariodemoTransitionActive;
    private float simAccumulator;
    private float upPressTime;
    private float downPressTime;
    private float leftPressTime;
    private float rightPressTime;
    private bool showQuitConfirm;
    private bool quitDialogTextureChecked;
    private Texture2D loadedQuitDialogTexture;
    private bool restartButtonTextureChecked;
    private Texture2D loadedRestartButtonTexture;
    private Texture2D quitStayIconTexture;
    private Texture2D quitExitIconTexture;
    private Texture2D uiCircleTexture;
    private AudioSource cueSource;
    private AudioSource ghostHuntBgmSource;
    private AudioSource stateLoopSource;
    private AudioSource moveLoopSource;
    private bool chaseLoopUnlocked;
    private bool queuedGhostHuntBgm;
    private float ghostHuntBgmDelayTimer;

    private GUIStyle uiQuitIconStyle;

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
        EnsureAudioSources();
        runtimeInitialized = false;
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        AutoAssignAudioClipIfMissing(ref pelletSfx, "Assets/Music/PacJam/鍚冭眴闊虫晥.mp3");
        AutoAssignAudioClipIfMissing(ref powerPelletSfx, "Assets/Music/PacJam/鍚冭眴闊虫晥.mp3");
        AutoAssignAudioClipIfMissing(ref ghostEatenSfx, "Assets/Music/PacJam/鍚冩晫浜洪煶鏁?mp3");
        AutoAssignAudioClipIfMissing(ref playerHitSfx, "Assets/Music/PacJam/澶辫触闊虫晥.mp3");
        AutoAssignAudioClipIfMissing(ref entranceSfx, "Assets/Music/PacJam/鍏ュ満闊虫晥.mp3");
        AutoAssignAudioClipIfMissing(ref ghostHuntIntroSfx, "Assets/Music/PacJam/GhostHuntIntro.mp3");
        AutoAssignAudioClipIfMissing(ref ghostHuntBgm, "Assets/Music/PacJam/GhostHuntBgm.ogg");
        AutoAssignAudioClipIfMissing(ref frightenedLoopSfx, "Assets/Music/PacJam/鏁屼汉閫冭窇闊虫晥.mp3");
        AutoAssignAudioClipIfMissing(ref chaseLoopSfx, "Assets/Music/PacJam/杩藉嚮鏁屼汉闊虫晥.mp3");
        AutoAssignAudioClipIfMissing(ref moveLoopSfx, "Assets/Music/PacJam/绉诲姩闊虫晥.mp3");
#endif
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            showQuitConfirm = false;
            runtimeInitialized = false;
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (!(dt > 0.00001f) || float.IsNaN(dt) || float.IsInfinity(dt))
        {
            dt = FallbackDeltaTime;
        }
        else
        {
            dt = Mathf.Min(dt, 0.1f);
        }

        if (!runtimeInitialized)
        {
            ParseMap();
            BuildLevel();
            runtimeInitialized = true;
        }

        if (mariodemoTransitionActive)
        {
            StopManagedAudioLoops();
            TickMariodemoTransition(dt);
            return;
        }

        if (showQuitConfirm)
        {
            StopManagedAudioLoops();
            return;
        }

        if (gameOver || victory)
        {
            StopManagedAudioLoops();
            return;
        }

        if (Time.timeScale <= 0.0001f)
        {
            Time.timeScale = 1f;
        }

        simAccumulator += dt;
        int simSteps = 0;
        while (simAccumulator >= SimStep && simSteps < MaxSimStepsPerFrame)
        {
            SimulateStep(SimStep);
            simAccumulator -= SimStep;
            simSteps++;
            if (gameOver || victory || mariodemoTransitionActive)
            {
                break;
            }
        }

        if (simSteps == 0 && !gameOver && !victory && !mariodemoTransitionActive)
        {
            float tiny = Mathf.Max(simAccumulator, FallbackDeltaTime * 0.5f);
            SimulateStep(tiny);
            simAccumulator = 0f;
        }

        UpdateLoopingAudio();
    }

    private void OnGUI()
    {
        DrawRuntimeUi();
    }

    private void DrawRuntimeUi()
    {
        EnsureUiStyles();
        DrawLivesIndicator();

        if (!showQuitConfirm && !gameOver && !victory && !mariodemoTransitionActive)
        {
            DrawQuitButton();
        }

        if (victory || gameOver)
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
        if (uiCircleTexture == null) return;

        float radius = Mathf.Clamp(Screen.height * 0.022f, 10f, 16f);
        float diameter = radius * 2f;
        float gap = Mathf.Clamp(radius * 0.48f, 6f, 10f);
        float margin = Mathf.Clamp(Screen.height * 0.022f, 12f, 20f);
        int remainingLives = Mathf.Clamp(lives, 0, InitialLives);

        for (int i = 0; i < InitialLives; i++)
        {
            bool isActive = i < remainingLives;
            float x = margin + i * (diameter + gap);
            Rect rect = new Rect(x, margin, diameter, diameter);
            DrawPanelShadow(rect, 2f);
            DrawTintedTexture(rect, uiCircleTexture, isActive ? LifeActiveColor : LifeLostColor);
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
        if (dialogTexture != null)
        {
            DrawImageQuitDialog(dialogTexture);
        }
        else
        {
            showQuitConfirm = false;
        }
    }

    private void DrawImageQuitDialog(Texture2D dialogTexture)
    {
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
            BuildLevel();
            return;
        }

        if (restartTexture != null)
        {
            float scale = hovered ? 1.05f : 1f;
            Rect iconRect = CenterRect(buttonRect.center, buttonRect.width * scale, buttonRect.height * scale);
            GUI.DrawTexture(iconRect, restartTexture, ScaleMode.ScaleToFit, true);
        }
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

    private void EnsureQuitButtonIcons()
    {
        if (quitStayIconTexture != null && quitExitIconTexture != null)
        {
            return;
        }

        quitStayIconTexture = BuildTriangleIconTexture(120, true);
        quitExitIconTexture = BuildSquareIconTexture(120);
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

    private static Texture2D CreateUiTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };
        return texture;
    }

    private static Texture2D BuildCircleIconTexture(int size)
    {
        Texture2D texture = CreateUiTexture(size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(0.5f, 0.5f);
        const float radius = 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                float dist = Vector2.Distance(p, center);
                float alpha = Mathf.Clamp01((radius - dist) * size * 0.45f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float s1 = Sign2D(p, a, b);
        float s2 = Sign2D(p, b, c);
        float s3 = Sign2D(p, c, a);
        bool hasNeg = s1 < 0f || s2 < 0f || s3 < 0f;
        bool hasPos = s1 > 0f || s2 > 0f || s3 > 0f;
        return !(hasNeg && hasPos);
    }

    private static float Sign2D(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    private Texture2D GetQuitDialogTexture()
    {
        if (!quitDialogTextureChecked)
        {
            quitDialogTextureChecked = true;
            loadedQuitDialogTexture = Resources.Load<Texture2D>(QuitDialogResourcePath);
            if (loadedQuitDialogTexture == null)
            {
                Debug.LogError("PacJamDemoController: missing quit dialog texture at Resources/" + QuitDialogResourcePath + ".png");
            }
        }

        return loadedQuitDialogTexture;
    }

    private Texture2D GetRestartButtonTexture()
    {
        if (!restartButtonTextureChecked)
        {
            restartButtonTextureChecked = true;
            loadedRestartButtonTexture = Resources.Load<Texture2D>(RestartButtonResourcePath);
            if (loadedRestartButtonTexture == null)
            {
                Debug.LogError("PacJamDemoController: missing restart button texture at Resources/" + RestartButtonResourcePath + ".png");
            }
        }

        return loadedRestartButtonTexture;
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
        return new Rect(
            rect.x + insetX,
            rect.y + insetY,
            Mathf.Max(0f, rect.width - insetX * 2f),
            Mathf.Max(0f, rect.height - insetY * 2f));
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

#if UNITY_EDITOR
    private static void AutoAssignAudioClipIfMissing(ref AudioClip clip, string assetPath)
    {
        if (clip != null) return;
        clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
    }
#endif
}




