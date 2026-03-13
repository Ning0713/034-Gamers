using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PacJamDemoController : MonoBehaviour
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
        new Vector2Int(0, -1), // Up
        new Vector2Int(-1, 0), // Left
        new Vector2Int(0, 1),  // Down
        new Vector2Int(1, 0),  // Right
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

    private enum GhostType { Red, Pink, Blue, Orange }
    private enum GhostState { Scatter, Chase, Frightened, Eaten }

    private class Actor
    {
        public Transform Root;
        public MeshRenderer Body;
        public Transform Indicator;
        public MeshRenderer IndicatorRenderer;
        public bool BodyIsDirectional;
        public bool ShowIndicator;
        public Vector2Int Cell;
        public Vector2Int Spawn;
        public Vector2Int Dir;
        public Vector2Int WantDir;
        public Vector2Int LastDir = Vector2Int.left;
    }

    private sealed class Ghost : Actor
    {
        public GhostType Type;
        public Color NormalColor;
        public Vector2Int ScatterTarget;
        public bool Eaten;
    }

    private sealed class Pellet
    {
        public bool Power;
        public GameObject Go;
    }

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

        if (gameOver || victory) return;

        if (Time.timeScale <= 0.0001f) Time.timeScale = 1f;
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
            if (gameOver || victory) break;
        }

        if (simSteps == 0 && !gameOver && !victory)
        {
            float tiny = Mathf.Max(simAccumulator, FallbackDeltaTime * 0.5f);
            SimulateStep(tiny);
            simAccumulator = 0f;
        }
    }

    private static bool IsRestartPressed()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.rKey.wasPressedThisFrame;
    }

    private void OnGUI()
    {
        GUI.color = Color.white;
        GUI.Label(new Rect(12f, 12f, 540f, 28f),
            "WASD / Arrow Move | Score " + score + " | Lives " + lives + " | Mode " + ModeName() + " | Pellets " + pellets.Count + " | " + BuildTag);
        if (victory) GUI.Label(new Rect(12f, 38f, 380f, 28f), "Level clear. Press R to rebuild demo.");
        if (gameOver) GUI.Label(new Rect(12f, 38f, 380f, 28f), "Game over. Press R to retry.");

        Rect quitRect = new Rect(Screen.width - 118f, Screen.height - 42f, 106f, 30f);
        if (GUI.Button(quitRect, "Quit")) QuitGame();
    }

    private void ParseMap()
    {
        height = MapRows.Length;
        width = MapRows[0].Length;
        map = new char[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                char t = MapRows[y][x];
                if (t == 'P')
                {
                    playerSpawn = new Vector2Int(x, y);
                    t = ' ';
                }
                map[x, y] = t;
            }
        }
    }

    private void BuildLevel()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);

        pellets.Clear();
        ghosts.Clear();
        materialCache.Clear();

        score = 0;
        lives = 3;
        gameOver = false;
        victory = false;
        respawnTimer = 0f;
        frightenedTimer = 0f;
        modeTimer = 0f;
        modePhase = 0;
        simAccumulator = 0f;
        upPressTime = -1f;
        downPressTime = -1f;
        leftPressTime = -1f;
        rightPressTime = -1f;

        mapRoot = new GameObject("PacJamRoot").transform;
        mapRoot.SetParent(transform, false);
        wallRoot = new GameObject("Walls").transform;
        wallRoot.SetParent(mapRoot, false);
        pelletRoot = new GameObject("Pellets").transform;
        pelletRoot.SetParent(mapRoot, false);
        actorRoot = new GameObject("Actors").transform;
        actorRoot.SetParent(mapRoot, false);

        SetupCamera();
        BuildMaze();
        BuildActors();
    }

    private void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject co = new GameObject("Main Camera");
            co.tag = "MainCamera";
            cam = co.AddComponent<Camera>();
            co.AddComponent<AudioListener>();
        }

        cam.orthographic = true;
        cam.transform.position = new Vector3((width - 1) * 0.5f, -(height - 1) * 0.5f, -20f);
        cam.transform.rotation = Quaternion.identity;
        cam.orthographicSize = height * 0.55f;
        cam.backgroundColor = new Color(0.02f, 0.03f, 0.10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    private void BuildMaze()
    {
        Color wallColor = new Color(0.08f, 0.22f, 0.75f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int c = new Vector2Int(x, y);
                char t = map[x, y];

                if (t == '#')
                {
                    GameObject w = CreateShape("W_" + x + "_" + y, GetRectMesh(), new Vector3(0.95f, 0.95f, 1f), wallColor, wallRoot, 0f);
                    w.transform.position = CellToWorld(c, 0f);
                }
                else if (t == '.' || t == 'o')
                {
                    bool power = t == 'o';
                    float r = power ? 0.18f : 0.10f;
                    Color pelletVisual = power ? PowerPelletColor : PelletColor;
                    GameObject p = CreateShape("P_" + x + "_" + y, GetCircleMesh(), new Vector3(r * 2f, r * 2f, 1f), pelletVisual, pelletRoot, 0f);
                    p.transform.position = CellToWorld(c, -0.1f);
                    pellets[c] = new Pellet { Power = power, Go = p };
                }
            }
        }
    }

    private void BuildActors()
    {
        player = CreateActor("Player", playerSpawn, new Color(1f, 0.95f, 0.12f), false, true);
        player.Dir = Vector2Int.left;
        player.WantDir = Vector2Int.left;
        UpdateIndicator(player);

        AddGhost("Ghost_Red", GhostType.Red, new Vector2Int(9, 7), new Color(1f, 0.32f, 0.28f), new Vector2Int(width - 2, 1));
        AddGhost("Ghost_Pink", GhostType.Pink, new Vector2Int(8, 7), new Color(1f, 0.62f, 0.82f), new Vector2Int(1, 1));
        AddGhost("Ghost_Blue", GhostType.Blue, new Vector2Int(11, 7), new Color(0.38f, 0.90f, 1f), new Vector2Int(width - 2, height - 2));
        AddGhost("Ghost_Orange", GhostType.Orange, new Vector2Int(12, 7), new Color(1f, 0.71f, 0.34f), new Vector2Int(1, height - 2));
    }

    private Actor CreateActor(string name, Vector2Int spawn, Color bodyColor, bool triangleBody, bool showIndicator)
    {
        GameObject rootGo = new GameObject(name);
        rootGo.transform.SetParent(actorRoot, false);
        rootGo.transform.position = CellToWorld(spawn, -0.2f);

        Mesh bodyMesh = triangleBody ? GetTriMesh() : GetCircleMesh();
        Vector3 bodyScale = triangleBody ? new Vector3(0.86f, 0.86f, 1f) : new Vector3(0.76f, 0.76f, 1f);
        GameObject bodyGo = CreateShape("Body", bodyMesh, bodyScale, bodyColor, rootGo.transform, 0f);

        GameObject triGo = null;
        if (showIndicator)
        {
            triGo = CreateShape("Facing", GetTriMesh(), new Vector3(0.34f, 0.34f, 1f), Faint(bodyColor), rootGo.transform, -0.01f);
        }

        return new Actor
        {
            Root = rootGo.transform,
            Body = bodyGo.GetComponent<MeshRenderer>(),
            Indicator = triGo != null ? triGo.transform : null,
            IndicatorRenderer = triGo != null ? triGo.GetComponent<MeshRenderer>() : null,
            BodyIsDirectional = triangleBody,
            ShowIndicator = showIndicator,
            Spawn = spawn,
            Cell = spawn,
            Dir = Vector2Int.zero,
            WantDir = Vector2Int.zero,
            LastDir = Vector2Int.left,
        };
    }

    private void AddGhost(string name, GhostType type, Vector2Int spawn, Color color, Vector2Int scatter)
    {
        Actor a = CreateActor(name, spawn, color, true, false);
        Ghost g = new Ghost
        {
            Root = a.Root,
            Body = a.Body,
            Indicator = a.Indicator,
            IndicatorRenderer = a.IndicatorRenderer,
            BodyIsDirectional = a.BodyIsDirectional,
            ShowIndicator = a.ShowIndicator,
            Spawn = a.Spawn,
            Cell = a.Cell,
            Dir = spawn.x < width / 2 ? Vector2Int.left : Vector2Int.right,
            WantDir = Vector2Int.zero,
            LastDir = a.LastDir,
            Type = type,
            NormalColor = color,
            ScatterTarget = scatter,
            Eaten = false,
        };
        ghosts.Add(g);
        UpdateIndicator(g);
    }

    private void TickModeTimer(float dt)
    {
        if (frightenedTimer > 0f)
        {
            frightenedTimer -= dt;
            if (frightenedTimer <= 0f)
            {
                frightenedTimer = 0f;
                ReleaseEatenGhostsAfterFrightened();
            }

            return;
        }

        modeTimer += dt;
        if (modeTimer >= ModeDurations[Mathf.Min(modePhase, ModeDurations.Length - 1)])
        {
            modeTimer = 0f;
            if (modePhase < ModeDurations.Length - 1) modePhase++;
        }
    }

    private void ReleaseEatenGhostsAfterFrightened()
    {
        for (int i = 0; i < ghosts.Count; i++)
        {
            Ghost g = ghosts[i];
            if (!g.Eaten) continue;

            g.Eaten = false;
            g.Cell = g.Spawn;
            g.Root.position = CellToWorld(g.Spawn, g.Root.position.z);
            g.Dir = ChooseGhostDir(g, GetGhostState(g));
            if (g.Dir == Vector2Int.zero)
            {
                for (int d = 0; d < Dirs.Length; d++)
                {
                    if (CanMove(g.Cell, Dirs[d]))
                    {
                        g.Dir = Dirs[d];
                        break;
                    }
                }
            }
        }
    }

    private void TickPlayer(float dt)
    {
        Vector2Int inputDir = ReadInputDir();
        if (inputDir != Vector2Int.zero)
        {
            player.WantDir = inputDir;

            // Pac-Man style immediate reverse for responsive control.
            if (player.Dir != Vector2Int.zero && inputDir == -player.Dir)
            {
                player.Dir = inputDir;
            }
        }

        TryApplyPlayerPreTurn();
        AdvanceActor(player, PlayerSpeed, dt, OnPlayerAtCellCenter);
        EatPellet(player.Cell);
        UpdateIndicator(player);
    }

    private void TickGhosts(float dt)
    {
        for (int i = 0; i < ghosts.Count; i++)
        {
            Ghost g = ghosts[i];
            GhostState state = GetGhostState(g);

            // Safety release: if an eaten flag lingers after frightened ended, recover immediately.
            if (state == GhostState.Eaten && frightenedTimer <= 0f)
            {
                g.Eaten = false;
                state = GetGhostState(g);
            }

            if (state == GhostState.Eaten)
            {
                g.Cell = g.Spawn;
                g.Root.position = CellToWorld(g.Spawn, g.Root.position.z);
                g.Dir = Vector2Int.zero;
                PaintGhost(g, state);
                UpdateIndicator(g);
                continue;
            }

            // Build/runtime safety: if direction is invalid or missing, re-bootstrap movement.
            if (g.Dir == Vector2Int.zero || (AtCenter(g) && !CanMove(g.Cell, g.Dir)))
            {
                Vector2Int bootstrap = ChooseGhostDir(g, state);
                if (bootstrap == Vector2Int.zero) bootstrap = FirstWalkableDir(g.Cell);
                g.Dir = bootstrap;
            }

            AdvanceActor(g, GhostSpeed(state), dt, OnGhostAtCellCenter);
            if (g.Dir == Vector2Int.zero)
            {
                Vector2Int fallback = ChooseGhostDir(g, state);
                if (fallback == Vector2Int.zero) fallback = FirstWalkableDir(g.Cell);
                if (fallback != Vector2Int.zero) g.Dir = fallback;
            }
            PaintGhost(g, GetGhostState(g));
            UpdateIndicator(g);
        }
    }

    private void SimulateStep(float dt)
    {
        if (respawnTimer > 0f)
        {
            respawnTimer -= dt;
            if (respawnTimer < 0f) respawnTimer = 0f;
        }

        TickModeTimer(dt);
        TickPlayer(dt);
        TickGhosts(dt);
        CheckCollisions();

        if (pellets.Count == 0) victory = true;
    }

    private void CheckCollisions()
    {
        if (respawnTimer > 0f) return;

        float hitSqr = CollisionDistance * CollisionDistance;
        for (int i = 0; i < ghosts.Count; i++)
        {
            Ghost g = ghosts[i];
            if ((g.Root.position - player.Root.position).sqrMagnitude > hitSqr) continue;

            GhostState s = GetGhostState(g);
            if (s == GhostState.Frightened)
            {
                g.Eaten = true;
                g.Cell = g.Spawn;
                g.Root.position = CellToWorld(g.Spawn, g.Root.position.z);
                g.Dir = Vector2Int.zero;
                score += 200;
                Play(ghostEatenSfx);
                SpawnFx(ghostEatFxPrefab, g.Root.position);
                PaintGhost(g, GetGhostState(g));
                UpdateIndicator(g);
            }
            else if (s != GhostState.Eaten)
            {
                lives--;
                Play(playerHitSfx);
                SpawnFx(playerHitFxPrefab, player.Root.position);
                if (lives <= 0) { gameOver = true; return; }

                frightenedTimer = 0f;
                respawnTimer = RespawnPauseDuration;
                ResetActorsToSpawn();
                return;
            }
        }
    }

    private void ResetActorsToSpawn()
    {
        player.Cell = player.Spawn;
        player.Root.position = CellToWorld(player.Spawn, player.Root.position.z);
        player.Dir = Vector2Int.left;
        player.WantDir = Vector2Int.left;
        UpdateIndicator(player);

        for (int i = 0; i < ghosts.Count; i++)
        {
            Ghost g = ghosts[i];
            g.Cell = g.Spawn;
            g.Root.position = CellToWorld(g.Spawn, g.Root.position.z);
            g.Dir = g.Spawn.x < width / 2 ? Vector2Int.left : Vector2Int.right;
            g.Eaten = false;
            PaintGhost(g, GetGhostState(g));
            UpdateIndicator(g);
        }
    }

    private GhostState GetGhostState(Ghost g)
    {
        if (g.Eaten) return GhostState.Eaten;
        if (frightenedTimer > 0f) return GhostState.Frightened;
        return modePhase % 2 == 0 ? GhostState.Scatter : GhostState.Chase;
    }

    private float GhostSpeed(GhostState s)
    {
        if (s == GhostState.Scatter) return GhostScatterSpeed;
        if (s == GhostState.Chase) return GhostChaseSpeed;
        if (s == GhostState.Frightened) return GhostFrightenedSpeed;
        return GhostEatenSpeed;
    }

    private Vector2Int ChooseGhostDir(Ghost g, GhostState state)
    {
        List<Vector2Int> options = new List<Vector2Int>(4);
        for (int i = 0; i < Dirs.Length; i++) if (CanMove(g.Cell, Dirs[i])) options.Add(Dirs[i]);
        if (options.Count == 0) return Vector2Int.zero;

        Vector2Int rev = -g.Dir;
        if (g.Dir != Vector2Int.zero && options.Count > 1) options.Remove(rev);
        if (options.Count == 0) options.Add(rev);

        if (state == GhostState.Frightened)
        {
            Vector2Int awayFrom = player.Cell;
            Vector2Int bestAway = options[0];
            float farthestDist = float.MinValue;
            for (int i = 0; i < Dirs.Length; i++)
            {
                Vector2Int d = Dirs[i];
                if (!options.Contains(d)) continue;
                float dist = ((Vector2)(g.Cell + d - awayFrom)).sqrMagnitude;
                if (dist > farthestDist)
                {
                    farthestDist = dist;
                    bestAway = d;
                }
            }

            return bestAway;
        }

        Vector2Int target = GhostTarget(g, state);
        Vector2Int best = options[0];
        float bestDist = float.MaxValue;
        for (int i = 0; i < Dirs.Length; i++)
        {
            Vector2Int d = Dirs[i];
            if (!options.Contains(d)) continue;
            float dist = ((Vector2)(g.Cell + d - target)).sqrMagnitude;
            if (dist < bestDist) { bestDist = dist; best = d; }
        }
        return best;
    }

    private Vector2Int GhostTarget(Ghost g, GhostState state)
    {
        if (state == GhostState.Eaten) return g.Spawn;
        if (state == GhostState.Scatter) return g.ScatterTarget;

        Vector2Int pDir = player.Dir == Vector2Int.zero ? player.LastDir : player.Dir;
        Vector2Int pCell = player.Cell;
        if (g.Type == GhostType.Red) return pCell;
        if (g.Type == GhostType.Pink) return pCell + pDir * 4;
        if (g.Type == GhostType.Blue)
        {
            Vector2Int redCell = ghosts[0].Cell;
            Vector2Int a = pCell + pDir * 2;
            return a + (a - redCell);
        }

        if (((Vector2)(pCell - g.Cell)).magnitude > 8f) return pCell;
        return g.ScatterTarget;
    }

    private void PaintGhost(Ghost g, GhostState s)
    {
        Color c = g.NormalColor;
        if (s == GhostState.Frightened) c = new Color(0.55f, 0.55f, 0.55f);
        if (s == GhostState.Eaten) c = new Color(0.88f, 0.88f, 0.88f);
        g.Body.sharedMaterial = GetMaterial(c);
        if (g.IndicatorRenderer != null) g.IndicatorRenderer.sharedMaterial = GetMaterial(Faint(c));
    }

    private void OnPlayerAtCellCenter(Actor a)
    {
        SnapToCell(a);
        EatPellet(a.Cell);

        if (CanMove(a.Cell, a.WantDir))
        {
            a.Dir = a.WantDir;
        }
        else if (!CanMove(a.Cell, a.Dir))
        {
            a.Dir = Vector2Int.zero;
        }
    }

    private void TryApplyPlayerPreTurn()
    {
        if (player == null || player.WantDir == Vector2Int.zero) return;
        if (player.Dir == Vector2Int.zero)
        {
            if (CanMove(player.Cell, player.WantDir))
            {
                SnapToCell(player);
                player.Dir = player.WantDir;
            }

            return;
        }

        if (player.WantDir == player.Dir) return;
        if (player.WantDir == -player.Dir) return;

        Vector2Int turnCell = player.Cell + player.Dir;
        if (!IsWalkable(turnCell) || !CanMove(turnCell, player.WantDir)) return;

        Vector3 turnWorld = CellToWorld(turnCell, player.Root.position.z);
        if ((player.Root.position - turnWorld).sqrMagnitude > PlayerPreTurnWindow * PlayerPreTurnWindow) return;

        player.Cell = turnCell;
        player.Root.position = turnWorld;
        player.Dir = player.WantDir;
    }

    private void OnGhostAtCellCenter(Actor a)
    {
        Ghost g = (Ghost)a;
        SnapToCell(g);
        if (g.Eaten)
        {
            g.Dir = Vector2Int.zero;
            return;
        }

        bool canForward = CanMove(g.Cell, g.Dir);
        int exits = CountWalkable(g.Cell);
        bool mustChoose = exits >= 3 || !canForward;
        if (!mustChoose && canForward)
        {
            // Keep moving straight in corridors to avoid per-tile jitter.
            return;
        }

        Vector2Int next = ChooseGhostDir(g, GetGhostState(g));
        if (next == Vector2Int.zero) next = FirstWalkableDir(g.Cell);
        g.Dir = next;
    }

    private void AdvanceActor(Actor a, float speed, float dt, Action<Actor> onCellCenter)
    {
        float remain = speed * CellSize * dt;
        int guard = 0;

        if (AtCenter(a))
        {
            SnapToCell(a);
            onCellCenter(a);
        }

        while (remain > 0.0001f && guard++ < 16)
        {
            if (a.Dir == Vector2Int.zero)
            {
                // Recover from tiny off-center drift: re-center first, then allow turning again.
                Vector3 center = CellToWorld(a.Cell, a.Root.position.z);
                Vector3 toCenter = center - a.Root.position;
                float centerDist = toCenter.magnitude;
                if (centerDist <= 0.0001f)
                {
                    a.Root.position = center;
                    onCellCenter(a);
                }
                else
                {
                    float step = Mathf.Min(remain, centerDist);
                    a.Root.position += toCenter.normalized * step;
                    remain -= step;
                    if ((a.Root.position - center).sqrMagnitude <= CenterEpsilonSqr)
                    {
                        a.Root.position = center;
                        onCellCenter(a);
                    }
                }

                if (a.Dir == Vector2Int.zero) break;
            }

            Vector2Int to = a.Cell + a.Dir;
            if (!IsWalkable(to))
            {
                if (AtCenter(a))
                {
                    onCellCenter(a);
                    to = a.Cell + a.Dir;
                }

                if (!IsWalkable(to))
                {
                    a.Dir = Vector2Int.zero;
                    break;
                }
            }

            Vector3 target = CellToWorld(to, a.Root.position.z);
            Vector3 delta = target - a.Root.position;
            float dist = delta.magnitude;

            if (dist <= 0.0001f)
            {
                a.Root.position = target;
                a.Cell = to;
                continue;
            }

            if (remain >= dist)
            {
                a.Root.position = target;
                a.Cell = to;
                remain -= dist;
                onCellCenter(a);
            }
            else
            {
                a.Root.position += delta.normalized * remain;
                remain = 0f;
            }
        }
    }

    private void EatPellet(Vector2Int c)
    {
        Pellet p;
        if (!pellets.TryGetValue(c, out p)) return;

        pellets.Remove(c);
        Destroy(p.Go);
        score += p.Power ? 50 : 10;
        Play(pelletSfx);
        SpawnFx(pelletFxPrefab, CellToWorld(c, -0.1f));

        if (p.Power)
        {
            frightenedTimer = FrightenedDuration;
            Play(powerPelletSfx);
        }
    }

    private void UpdateIndicator(Actor a)
    {
        Vector2Int d = a.Dir == Vector2Int.zero ? a.LastDir : a.Dir;
        if (d == Vector2Int.zero) d = Vector2Int.left;
        a.LastDir = d;

        Vector3 fw = GridDirToWorld(d).normalized;
        float angle = Mathf.Atan2(fw.y, fw.x) * Mathf.Rad2Deg;

        if (a.BodyIsDirectional && a.Body != null)
        {
            a.Body.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (!a.ShowIndicator || a.Indicator == null) return;

        a.Indicator.localPosition = fw * IndicatorForwardOffset + new Vector3(0f, 0f, -0.01f);
        a.Indicator.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private Vector2Int ReadInputDir()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return Vector2Int.zero;

        float now = Time.unscaledTime;
        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) upPressTime = now;
        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) downPressTime = now;
        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) leftPressTime = now;
        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) rightPressTime = now;

        bool upHeld = keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed;
        bool downHeld = keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed;
        bool leftHeld = keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed;
        bool rightHeld = keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed;

        float bestTime = float.NegativeInfinity;
        Vector2Int bestDir = Vector2Int.zero;
        if (upHeld && upPressTime > bestTime) { bestTime = upPressTime; bestDir = new Vector2Int(0, -1); }
        if (downHeld && downPressTime > bestTime) { bestTime = downPressTime; bestDir = new Vector2Int(0, 1); }
        if (leftHeld && leftPressTime > bestTime) { bestTime = leftPressTime; bestDir = new Vector2Int(-1, 0); }
        if (rightHeld && rightPressTime > bestTime) { bestTime = rightPressTime; bestDir = new Vector2Int(1, 0); }
        return bestDir;
    }

    private bool AtCenter(Actor a)
    {
        Vector3 c = CellToWorld(a.Cell, a.Root.position.z);
        return (a.Root.position - c).sqrMagnitude <= CenterEpsilonSqr;
    }

    private int CountWalkable(Vector2Int cell)
    {
        int count = 0;
        for (int i = 0; i < Dirs.Length; i++)
        {
            if (CanMove(cell, Dirs[i])) count++;
        }

        return count;
    }

    private Vector2Int FirstWalkableDir(Vector2Int cell)
    {
        for (int i = 0; i < Dirs.Length; i++)
        {
            if (CanMove(cell, Dirs[i])) return Dirs[i];
        }

        return Vector2Int.zero;
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SnapToCell(Actor a) { a.Root.position = CellToWorld(a.Cell, a.Root.position.z); }
    private bool CanMove(Vector2Int from, Vector2Int dir) { return dir != Vector2Int.zero && IsWalkable(from + dir); }
    private bool IsWalkable(Vector2Int c) { return c.x >= 0 && c.x < width && c.y >= 0 && c.y < height && map[c.x, c.y] != '#'; }
    private string ModeName() { return frightenedTimer > 0f ? "Frightened" : (modePhase % 2 == 0 ? "Scatter" : "Chase"); }

    private static Vector3 GridDirToWorld(Vector2Int d) { return new Vector3(d.x, -d.y, 0f); }
    private Vector3 CellToWorld(Vector2Int c, float z) { return new Vector3(c.x * CellSize, -c.y * CellSize, z); }
    private static Color Faint(Color c) { return Color.Lerp(c, Color.white, 0.65f); }
    private void Play(AudioClip clip) { if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip); }

    private static void SpawnFx(ParticleSystem prefab, Vector3 pos)
    {
        if (prefab == null) return;
        ParticleSystem fx = Instantiate(prefab, pos, Quaternion.identity);
        Destroy(fx.gameObject, 1.5f);
    }

    private GameObject CreateShape(string name, Mesh mesh, Vector3 scale, Color color, Transform parent, float localZ)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0f, localZ);
        go.transform.localScale = scale;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = GetMaterial(color);
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return go;
    }

    private Material GetMaterial(Color color)
    {
        Color32 key = (Color32)color;
        Material mat;
        if (materialCache.TryGetValue(key, out mat)) return mat;

        if (unlitShader == null)
        {
            unlitShader = ResolveRuntimeShader();
        }

        if (unlitShader == null)
        {
            // Last-resort internal shader to avoid runtime exceptions in player build.
            unlitShader = Shader.Find("Hidden/InternalErrorShader");
        }

        mat = new Material(unlitShader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        materialCache[key] = mat;
        return mat;
    }

    private static Shader ResolveRuntimeShader()
    {
        bool usingSrp = GraphicsSettings.currentRenderPipeline != null;
        Shader s = null;

        if (usingSrp)
        {
            s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s != null) return s;
        }

        s = Shader.Find("Sprites/Default");
        if (s != null) return s;

        s = Shader.Find("Unlit/Color");
        if (s != null) return s;

        s = Shader.Find("UI/Default");
        if (s != null) return s;

        s = Shader.Find("Hidden/Internal-Colored");
        if (s != null) return s;

        return Shader.Find("Hidden/InternalErrorShader");
    }

    private Mesh GetCircleMesh()
    {
        if (circleMesh != null) return circleMesh;
        int seg = 28;
        Vector3[] v = new Vector3[seg + 1];
        int[] t = new int[seg * 6];
        v[0] = Vector3.zero;
        for (int i = 0; i < seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            v[i + 1] = new Vector3(Mathf.Cos(a) * 0.5f, Mathf.Sin(a) * 0.5f, 0f);
        }
        int n = 0;
        for (int i = 0; i < seg; i++)
        {
            int c = i + 1;
            int nx = i == seg - 1 ? 1 : i + 2;
            t[n++] = 0; t[n++] = c; t[n++] = nx;
            t[n++] = 0; t[n++] = nx; t[n++] = c;
        }
        circleMesh = new Mesh { vertices = v, triangles = t, name = "PacJamCircle" };
        circleMesh.RecalculateNormals();
        circleMesh.RecalculateBounds();
        return circleMesh;
    }

    private Mesh GetTriMesh()
    {
        if (triMesh != null) return triMesh;
        triMesh = new Mesh
        {
            name = "PacJamTriangle",
            vertices = new[]
            {
                new Vector3(0.55f, 0f, 0f),
                new Vector3(-0.45f, 0.28f, 0f),
                new Vector3(-0.45f, -0.28f, 0f),
            },
            triangles = new[] { 0, 1, 2, 0, 2, 1 },
        };
        triMesh.RecalculateNormals();
        triMesh.RecalculateBounds();
        return triMesh;
    }

    private Mesh GetRectMesh()
    {
        if (rectMesh != null) return rectMesh;
        rectMesh = new Mesh
        {
            name = "PacJamRect",
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
}
