using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ForestdemoTransitionCutsceneController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip vortexRiseSfx;
    [SerializeField] private AudioClip vortexLoopSfx;
    [SerializeField] private AudioClip vortexFallSfx;
    [SerializeField] private AudioClip transitionBgm;

    private const float FallbackDeltaTime = 1f / 120f;
    private const float CutsceneDuration = 4.9f;

    private Texture2D circleTexture;
    private AudioSource sfxSource;
    private AudioSource loopSource;
    private AudioSource bgmSource;
    private float cutsceneTime;

    private void Awake()
    {
        Time.timeScale = 1f;
        Application.targetFrameRate = 120;
        Application.runInBackground = true;
        EnsureCamera();
        EnsureTextures();
        EnsureAudioSources();
        Play(vortexRiseSfx);
    }

    private void Update()
    {
        EnsureCamera();
        cutsceneTime += GetSafeUnscaledDeltaTime();
        SyncLoopSource(loopSource, vortexLoopSfx);
        SyncLoopSource(bgmSource, transitionBgm);

        if (cutsceneTime >= CutsceneDuration * 0.62f && vortexFallSfx != null && !sfxSource.isPlaying)
        {
            Play(vortexFallSfx);
        }

        if (cutsceneTime >= CutsceneDuration)
        {
            StopAudio();
            SceneManager.LoadScene(ForestdemoProgressState.StageSceneName);
        }
    }

    private void OnGUI()
    {
        EnsureTextures();

        Color background = new Color(0.05f, 0.07f, 0.06f, 1f);
        DrawFilledRect(new Rect(0f, 0f, Screen.width, Screen.height), background);

        Rect mistRect = new Rect(0f, Screen.height * 0.08f, Screen.width, Screen.height * 0.84f);
        DrawTintedTexture(mistRect, circleTexture, new Color(0.22f, 0.38f, 0.26f, 0.10f));

        Vector2 holeCenter = new Vector2(Screen.width * 0.52f, Screen.height * 0.80f);
        float holeWidth = Mathf.Clamp(Screen.width * 0.18f, 140f, 220f);
        float holeHeight = Mathf.Clamp(Screen.height * 0.08f, 44f, 72f);
        DrawEllipse(holeCenter, holeWidth * 1.22f, holeHeight * 1.45f, new Color(0.46f, 0.90f, 0.52f, 0.10f));
        DrawEllipse(holeCenter, holeWidth * 1.02f, holeHeight * 1.22f, new Color(0.72f, 0.98f, 0.74f, 0.16f));
        DrawEllipse(holeCenter, holeWidth * 0.84f, holeHeight, new Color(0.10f, 0.22f, 0.10f, 0.92f));
        DrawEllipse(holeCenter + new Vector2(0f, -holeHeight * 0.06f), holeWidth * 0.55f, holeHeight * 0.54f, new Color(0.01f, 0.03f, 0.01f, 0.98f));

        float progress = Mathf.Clamp01(cutsceneTime / (CutsceneDuration - 0.18f));
        float eased = 1f - Mathf.Pow(1f - progress, 2.5f);
        float orbitTurns = Mathf.Lerp(0f, 5.8f, eased);
        float angle = orbitTurns * Mathf.PI * 2f;
        float radiusX = Mathf.Lerp(Screen.width * 0.30f, holeWidth * 0.16f, eased);
        float radiusY = Mathf.Lerp(Screen.height * 0.22f, holeHeight * 0.10f, eased);
        float drop = Mathf.Lerp(Screen.height * 0.18f, holeCenter.y - holeHeight * 0.18f, eased);
        Vector2 ballCenter = new Vector2(
            holeCenter.x + Mathf.Cos(angle) * radiusX,
            Mathf.Lerp(Screen.height * 0.25f, drop, eased) + Mathf.Sin(angle * 1.35f) * radiusY);
        float ballRadius = Mathf.Lerp(Screen.height * 0.050f, Screen.height * 0.015f, Mathf.SmoothStep(0f, 1f, eased));

        for (int i = 0; i < 11; i++)
        {
            float trailT = Mathf.Clamp01(progress - i * 0.036f);
            if (trailT <= 0f)
            {
                continue;
            }

            float trailEased = 1f - Mathf.Pow(1f - trailT, 2.5f);
            float trailAngle = Mathf.Lerp(0f, 5.8f, trailEased) * Mathf.PI * 2f;
            float trailRadiusX = Mathf.Lerp(Screen.width * 0.30f, holeWidth * 0.16f, trailEased);
            float trailRadiusY = Mathf.Lerp(Screen.height * 0.22f, holeHeight * 0.10f, trailEased);
            float trailDrop = Mathf.Lerp(Screen.height * 0.18f, holeCenter.y - holeHeight * 0.18f, trailEased);
            Vector2 trailCenter = new Vector2(
                holeCenter.x + Mathf.Cos(trailAngle) * trailRadiusX,
                Mathf.Lerp(Screen.height * 0.25f, trailDrop, trailEased) + Mathf.Sin(trailAngle * 1.35f) * trailRadiusY);
            float trailRadius = Mathf.Lerp(ballRadius * 0.75f, ballRadius * 0.22f, i / 10f);
            float alpha = Mathf.Lerp(0.22f, 0.03f, i / 10f);
            DrawCircle(trailCenter, trailRadius, new Color(0.98f, 0.88f, 0.18f, alpha));
        }

        DrawCircle(new Vector2(ballCenter.x + ballRadius * 0.18f, holeCenter.y + holeHeight * 0.55f), ballRadius * 1.04f, new Color(0f, 0f, 0f, 0.16f));
        DrawCircle(ballCenter, ballRadius, new Color(0.98f, 0.88f, 0.18f, 1f));
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

        if (loopSource == null)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(loopSource, true, 0.62f);
        }

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(bgmSource, true, 0.54f);
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
        StopLoopSource(loopSource);
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
