using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PacJamIntroCutsceneController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip doorOpenSfx;
    [SerializeField] private AudioClip holeFallSfx;

    private const float FallbackDeltaTime = 1f / 120f;
    private const float CutsceneDuration = 4.2f;
    private const float DoorOpenDuration = 0.7f;
    private const float WalkDuration = 1.35f;
    private const float FallStartTime = 2.1f;
    private const string GameplaySceneName = "PacJamDemo";

    private Texture2D circleTexture;
    private AudioSource sfxSource;
    private float cutsceneTime;
    private bool doorOpenPlayed;
    private bool holeFallPlayed;

    private void Awake()
    {
        Time.timeScale = 1f;
        Application.targetFrameRate = 120;
        Application.runInBackground = true;
        EnsureCamera();
        EnsureTextures();
        EnsureAudioSource();
    }

    private void Update()
    {
        EnsureCamera();
        cutsceneTime += GetSafeUnscaledDeltaTime();

        if (!doorOpenPlayed && cutsceneTime >= 0.06f)
        {
            doorOpenPlayed = true;
            Play(doorOpenSfx);
        }

        if (!holeFallPlayed && cutsceneTime >= FallStartTime)
        {
            holeFallPlayed = true;
            Play(holeFallSfx);
        }

        if (cutsceneTime >= CutsceneDuration)
        {
            SceneManager.LoadScene(GameplaySceneName);
        }
    }

    private void OnGUI()
    {
        EnsureTextures();

        DrawFilledRect(new Rect(0f, 0f, Screen.width, Screen.height), Color.black);

        float floorY = Screen.height * 0.72f;
        float floorHeight = Screen.height * 0.18f;
        DrawFilledRect(new Rect(0f, floorY, Screen.width, floorHeight), new Color(0.05f, 0.05f, 0.06f, 1f));
        DrawFilledRect(new Rect(Screen.width * 0.06f, floorY + floorHeight * 0.14f, Screen.width * 0.88f, floorHeight * 0.10f), new Color(0.18f, 0.18f, 0.22f, 0.46f));

        Rect doorRect = new Rect(Screen.width * 0.12f, Screen.height * 0.31f, Screen.width * 0.10f, Screen.height * 0.38f);
        DrawFilledRect(new Rect(doorRect.x - Screen.width * 0.010f, doorRect.y - Screen.height * 0.012f, doorRect.width + Screen.width * 0.020f, doorRect.height + Screen.height * 0.024f), new Color(0.92f, 0.92f, 0.96f, 1f));
        DrawFilledRect(doorRect, new Color(0.08f, 0.08f, 0.10f, 1f));

        float doorProgress = Mathf.Clamp01(cutsceneTime / DoorOpenDuration);
        float openAmount = Mathf.SmoothStep(0f, 1f, doorProgress);
        float leftWidth = doorRect.width * 0.5f;
        float doorPanelWidth = leftWidth * (1f - openAmount * 0.82f);
        DrawFilledRect(new Rect(doorRect.x, doorRect.y, doorPanelWidth, doorRect.height), new Color(0.70f, 0.70f, 0.76f, 1f));
        DrawFilledRect(new Rect(doorRect.xMax - doorPanelWidth, doorRect.y, doorPanelWidth, doorRect.height), new Color(0.70f, 0.70f, 0.76f, 1f));

        float holeWidth = Mathf.Clamp(Screen.width * 0.12f, 110f, 180f);
        float holeHeight = Mathf.Clamp(Screen.height * 0.07f, 34f, 56f);
        Vector2 holeCenter = new Vector2(Screen.width * 0.58f, floorY + holeHeight * 0.18f);
        DrawEllipse(new Vector2(holeCenter.x, holeCenter.y + holeHeight * 0.24f), holeWidth * 1.28f, holeHeight * 1.22f, new Color(0f, 0f, 0f, 0.22f));
        DrawEllipse(new Vector2(holeCenter.x, holeCenter.y - holeHeight * 0.05f), holeWidth * 1.18f, holeHeight * 0.92f, new Color(0.34f, 0.34f, 0.38f, 0.78f));
        DrawEllipse(holeCenter, holeWidth, holeHeight, new Color(0.01f, 0.01f, 0.01f, 0.98f));
        DrawEllipse(new Vector2(holeCenter.x, holeCenter.y - holeHeight * 0.10f), holeWidth * 0.74f, holeHeight * 0.34f, new Color(0.14f, 0.14f, 0.16f, 0.34f));

        float walkT = Mathf.Clamp01((cutsceneTime - 0.25f) / WalkDuration);
        float walkEased = Mathf.SmoothStep(0f, 1f, walkT);
        float fallT = Mathf.Clamp01((cutsceneTime - FallStartTime) / (CutsceneDuration - FallStartTime));
        float vanishT = Mathf.Clamp01((cutsceneTime - (FallStartTime + 0.35f)) / 1.1f);

        float startX = doorRect.center.x;
        float endX = holeCenter.x;
        float ballX = Mathf.Lerp(startX, endX, walkEased);
        float groundY = floorY - Screen.height * 0.012f;
        float fallDepth = Mathf.Lerp(0f, Screen.height * 0.24f, Mathf.SmoothStep(0f, 1f, fallT));
        float bob = Mathf.Sin(cutsceneTime * 10f) * Screen.height * 0.004f * (1f - Mathf.Clamp01((cutsceneTime - FallStartTime) * 4f));
        float ballRadius = Mathf.Clamp(Screen.height * 0.043f, 22f, 36f) * Mathf.Lerp(1f, 0.22f, vanishT);
        Vector2 ballCenter = new Vector2(ballX, groundY - fallDepth + bob - ballRadius * 0.1f);
        float ballAlpha = 1f - vanishT;

        for (int i = 0; i < 4; i++)
        {
            float trailT = Mathf.Clamp01((cutsceneTime - 0.25f - i * 0.12f) / WalkDuration);
            if (trailT <= 0f)
            {
                continue;
            }

            float trailX = Mathf.Lerp(startX, endX, Mathf.SmoothStep(0f, 1f, trailT));
            DrawCircle(new Vector2(trailX, groundY + ballRadius * 0.95f), ballRadius * 0.42f, new Color(0f, 0f, 0f, 0.07f * (1f - vanishT)));
        }

        DrawCircle(new Vector2(ballCenter.x + ballRadius * 0.18f, groundY + ballRadius * 0.92f), ballRadius * 0.96f, new Color(0f, 0f, 0f, 0.16f * ballAlpha));
        DrawCircle(ballCenter, ballRadius, new Color(0.98f, 0.88f, 0.20f, ballAlpha));

        if (fallT > 0.01f)
        {
            DrawEllipse(new Vector2(holeCenter.x, holeCenter.y + holeHeight * 0.05f), holeWidth * (1f + fallT * 0.26f), holeHeight * (1f + fallT * 0.20f), new Color(0.02f, 0.02f, 0.02f, 0.24f * (1f - vanishT * 0.5f)));
        }
    }

    private void EnsureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cam = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
        }

        cam.orthographic = true;
        cam.transform.position = new Vector3(0f, 0f, -20f);
        cam.transform.rotation = Quaternion.identity;
        cam.orthographicSize = 6f;
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    private void EnsureTextures()
    {
        if (circleTexture == null)
        {
            circleTexture = BuildCircleTexture(128);
        }
    }

    private void EnsureAudioSource()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 0.95f;
        }
    }

    private void Play(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        EnsureAudioSource();
        sfxSource.PlayOneShot(clip);
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

    private void DrawCircle(Vector2 center, float radius, Color color)
    {
        if (radius <= 0.5f)
        {
            return;
        }

        DrawTintedTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), circleTexture, color);
    }

    private void DrawEllipse(Vector2 center, float width, float height, Color color)
    {
        if (width <= 0.5f || height <= 0.5f)
        {
            return;
        }

        DrawTintedTexture(new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height), circleTexture, color);
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

    private static Texture2D BuildCircleTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };

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
}
