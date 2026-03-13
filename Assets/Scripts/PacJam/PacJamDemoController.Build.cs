using UnityEngine;
using UnityEngine.Rendering;

public partial class PacJamDemoController
{
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
