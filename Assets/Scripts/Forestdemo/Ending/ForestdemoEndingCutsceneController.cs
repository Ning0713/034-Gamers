using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ForestdemoEndingCutsceneController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip portalHumLoopSfx;
    [SerializeField] private AudioClip portalEnterSfx;
    [SerializeField] private AudioClip endingBgm;

    private const float FallbackDeltaTime = 1f / 120f;
    private const float CutsceneDuration = 5.8f;
    private const float ButtonFadeDuration = 0.28f;
    private const string StartMenuSceneName = "PacJamStartMenu";

    private static readonly Color GreenColor = new Color(0.17f, 0.94f, 0.35f, 1f);
    private static readonly Color ButtonBorderColor = new Color(0.92f, 0.92f, 0.92f, 1f);
    private static readonly Color ButtonFillColor = new Color(0.04f, 0.04f, 0.04f, 1f);

    private Texture2D circleTexture;
    private AudioSource sfxSource;
    private AudioSource humSource;
    private AudioSource bgmSource;
    private float cutsceneTime;
    private float buttonRevealTime;
    private bool enterPlayed;
    private bool showReturnButton;

    private void Awake()
    {
        Time.timeScale = 1f;
        Application.targetFrameRate = 120;
        Application.runInBackground = true;
        EnsureCamera();
        EnsureTextures();
        EnsureAudioSources();
    }

    private void Update()
    {
        EnsureCamera();
        EnsureAudioSources();

        float dt = GetSafeUnscaledDeltaTime();
        SyncLoopSource(bgmSource, endingBgm);

        if (showReturnButton)
        {
            buttonRevealTime = Mathf.Min(ButtonFadeDuration, buttonRevealTime + dt);
            StopLoopSource(humSource);
            return;
        }

        cutsceneTime = Mathf.Min(CutsceneDuration, cutsceneTime + dt);
        SyncLoopSource(humSource, portalHumLoopSfx);

        if (!enterPlayed && cutsceneTime >= CutsceneDuration * 0.72f)
        {
            enterPlayed = true;
            Play(portalEnterSfx);
        }

        if (cutsceneTime >= CutsceneDuration)
        {
            cutsceneTime = CutsceneDuration;
            showReturnButton = true;
            buttonRevealTime = 0f;
            StopLoopSource(humSource);
        }
    }

    private void OnGUI()
    {
        EnsureTextures();

        DrawFilledRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.02f, 0.03f, 0.04f, 1f));
        DrawEllipse(new Vector2(Screen.width * 0.24f, Screen.height * 0.72f), Screen.width * 0.34f, Screen.height * 0.58f, new Color(0.16f, 0.24f, 0.20f, 0.12f));
        DrawEllipse(new Vector2(Screen.width * 0.70f, Screen.height * 0.30f), Screen.width * 0.42f, Screen.height * 0.48f, new Color(0.18f, 0.26f, 0.21f, 0.10f));

        float floorY = Screen.height * 0.70f;
        DrawFilledRect(new Rect(0f, floorY, Screen.width, Screen.height * 0.30f), new Color(0.03f, 0.05f, 0.03f, 1f));
        DrawFilledRect(new Rect(Screen.width * 0.10f, floorY + Screen.height * 0.05f, Screen.width * 0.68f, Screen.height * 0.010f), new Color(0.24f, 0.38f, 0.28f, 0.26f));

        float pulse = 0.5f + 0.5f * Mathf.Sin(cutsceneTime * 3.2f);
        Rect portalOuter = new Rect(Screen.width * 0.73f, Screen.height * 0.26f, Screen.width * 0.11f, Screen.height * 0.40f);
        Rect portalInner = new Rect(portalOuter.x + portalOuter.width * 0.18f, portalOuter.y + portalOuter.height * 0.08f, portalOuter.width * 0.64f, portalOuter.height * 0.84f);
        Vector2 portalCenter = portalOuter.center;

        DrawEllipse(portalCenter, portalOuter.width * 3.6f, portalOuter.height * 1.8f, new Color(0.98f, 0.92f, 0.30f, 0.08f + pulse * 0.06f));
        DrawEllipse(portalCenter, portalOuter.width * 2.8f, portalOuter.height * 1.5f, new Color(0.95f, 0.98f, 0.62f, 0.10f + pulse * 0.08f));
        DrawEllipse(portalCenter, portalOuter.width * 2.1f, portalOuter.height * 1.2f, new Color(0.72f, 1.00f, 0.84f, 0.10f + pulse * 0.08f));
        DrawFilledRect(portalOuter, new Color(1.00f, 0.96f, 0.56f, 0.70f + pulse * 0.12f));
        DrawFilledRect(portalInner, new Color(0.90f, 1.00f, 0.95f, 0.95f));
        DrawFilledRect(new Rect(portalInner.x + portalInner.width * 0.18f, portalInner.y + portalInner.height * 0.10f, portalInner.width * 0.64f, portalInner.height * 0.80f), new Color(1f, 1f, 1f, 0.68f + pulse * 0.12f));

        for (int i = 0; i < 10; i++)
        {
            float t = i / 9f;
            float markerX = Mathf.Lerp(Screen.width * 0.22f, portalCenter.x - portalOuter.width * 0.42f, t);
            float markerSize = Mathf.Lerp(Screen.height * 0.010f, Screen.height * 0.020f, 1f - t);
            float markerAlpha = Mathf.Lerp(0.08f, 0.24f, 1f - t);
            DrawCircle(new Vector2(markerX, floorY - Screen.height * 0.04f), markerSize, new Color(0.98f, 0.88f, 0.18f, markerAlpha));
        }

        float progress = Mathf.Clamp01(cutsceneTime / (CutsceneDuration - 0.24f));
        float eased = Mathf.SmoothStep(0f, 1f, progress);
        float ballRadius = Mathf.Clamp(Screen.height * 0.045f, 24f, 38f);
        float vanish = Mathf.Clamp01((progress - 0.72f) / 0.28f);
        float ballAlpha = 1f - vanish;
        float radiusScale = Mathf.Lerp(1f, 0.34f, vanish);
        Vector2 ballCenter = new Vector2(
            Mathf.Lerp(Screen.width * 0.14f, portalCenter.x, eased),
            Mathf.Lerp(floorY - Screen.height * 0.055f, portalCenter.y, eased));

        for (int i = 0; i < 8; i++)
        {
            float trailT = Mathf.Clamp01(progress - i * 0.05f);
            if (trailT <= 0f)
            {
                continue;
            }

            float trailEased = Mathf.SmoothStep(0f, 1f, trailT);
            Vector2 trailCenter = new Vector2(
                Mathf.Lerp(Screen.width * 0.14f, portalCenter.x, trailEased),
                Mathf.Lerp(floorY - Screen.height * 0.055f, portalCenter.y, trailEased));
            float trailScale = Mathf.Lerp(0.90f, 0.24f, i / 7f);
            float trailAlpha = Mathf.Lerp(0.18f, 0.03f, i / 7f) * (1f - vanish * 0.55f);
            DrawCircle(trailCenter, ballRadius * trailScale, new Color(0.98f, 0.88f, 0.18f, trailAlpha));
        }

        DrawCircle(new Vector2(ballCenter.x + ballRadius * 0.14f, ballCenter.y + ballRadius * 0.88f), ballRadius * 0.92f * radiusScale, new Color(0f, 0f, 0f, 0.14f * ballAlpha));
        DrawCircle(ballCenter, ballRadius * radiusScale * (1f + pulse * 0.04f), new Color(0.98f, 0.88f, 0.18f, ballAlpha));
        DrawCircle(ballCenter, ballRadius * radiusScale * 0.48f, new Color(1.00f, 0.96f, 0.62f, ballAlpha * 0.42f));

        if (vanish > 0f)
        {
            DrawEllipse(portalCenter, portalOuter.width * (1.2f + vanish * 1.1f), portalOuter.height * (0.90f + vanish * 0.40f), new Color(1.00f, 0.98f, 0.78f, vanish * 0.18f));
        }

        if (showReturnButton)
        {
            DrawReturnButton();
        }
    }

    private void DrawReturnButton()
    {
        float reveal = Mathf.Clamp01(buttonRevealTime / ButtonFadeDuration);
        if (reveal <= 0f)
        {
            return;
        }

        Color overlayColor = new Color(0f, 0f, 0f, 0.22f * reveal);
        DrawFilledRect(new Rect(0f, Screen.height * 0.68f, Screen.width, Screen.height * 0.32f), overlayColor);

        float buttonWidth = Mathf.Clamp(Screen.width * 0.22f, 180f, 260f);
        float buttonHeight = Mathf.Clamp(Screen.height * 0.10f, 62f, 84f);
        Rect buttonRect = new Rect((Screen.width - buttonWidth) * 0.5f, Screen.height * 0.77f, buttonWidth, buttonHeight);
        bool hovered = buttonRect.Contains(Event.current.mousePosition);

        DrawMenuButtonFrame(buttonRect, hovered, reveal);

        float pulse = 1f + Mathf.Sin((cutsceneTime + buttonRevealTime) * 4.8f) * 0.06f + (hovered ? 0.04f : 0f);
        float outerRadius = Mathf.Min(buttonRect.width, buttonRect.height) * 0.23f * pulse;
        Vector2 center = buttonRect.center;
        DrawCircle(center, outerRadius, WithAlpha(GreenColor, reveal));
        DrawCircle(center, outerRadius * 0.58f, WithAlpha(ButtonFillColor, reveal));

        if (GUI.Button(buttonRect, GUIContent.none, GUIStyle.none))
        {
            StopAudio();
            SceneManager.LoadScene(StartMenuSceneName);
        }
    }

    private static void DrawMenuButtonFrame(Rect rect, bool hovered, float alpha)
    {
        DrawFilledRect(new Rect(rect.x + 4f, rect.y + 4f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.30f * alpha));
        DrawFilledRect(rect, WithAlpha(ButtonBorderColor, alpha));
        Rect innerRect = ShrinkRect(rect, 2f, 2f);
        Color innerColor = hovered ? new Color(0.08f, 0.08f, 0.08f, alpha) : WithAlpha(ButtonFillColor, alpha);
        DrawFilledRect(innerRect, innerColor);
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

    private void EnsureAudioSources()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(sfxSource, false, 0.95f);
        }

        if (humSource == null)
        {
            humSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(humSource, true, 0.62f);
        }

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(bgmSource, true, 0.64f);
        }
    }

    private static void ConfigureAudioSource(AudioSource source, bool loop, float volume)
    {
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = volume;
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

    private void StopAudio()
    {
        StopLoopSource(sfxSource);
        StopLoopSource(humSource);
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

    private static Rect ShrinkRect(Rect rect, float x, float y)
    {
        return new Rect(rect.x + x, rect.y + y, rect.width - x * 2f, rect.height - y * 2f);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a *= alpha;
        return color;
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
