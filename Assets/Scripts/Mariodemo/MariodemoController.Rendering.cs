using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class MariodemoController
{
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
    }

    private GameObject CreateTileRoot(string name, Vector2Int cell, Transform parent)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = CellToWorld(cell, 0f);
        return root;
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

    private Vector3 CellToWorld(Vector2Int cell, float z)
    {
        return new Vector3((cell.x + 0.5f) * CellSize, (cell.y + 0.5f) * CellSize, z);
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

        circleMesh = new Mesh { vertices = vertices, triangles = triangles, name = "MariodemoCircle" };
        circleMesh.RecalculateNormals();
        circleMesh.RecalculateBounds();
        return circleMesh;
    }

    private Mesh GetTriMesh()
    {
        if (triMesh != null) return triMesh;
        triMesh = new Mesh
        {
            name = "MariodemoTriangle",
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

    private Mesh GetRectMesh()
    {
        if (rectMesh != null) return rectMesh;
        rectMesh = new Mesh
        {
            name = "MariodemoRect",
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

    private static float GetSafeUnscaledDeltaTime()
    {
        float dt = Time.unscaledDeltaTime;
        if (!(dt > 0.00001f) || float.IsNaN(dt) || float.IsInfinity(dt))
        {
            return FallbackDeltaTime;
        }

        return Mathf.Min(dt, 0.1f);
    }
}
