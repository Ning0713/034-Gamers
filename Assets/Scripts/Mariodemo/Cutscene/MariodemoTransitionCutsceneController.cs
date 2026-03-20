using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MariodemoTransitionCutsceneController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip cutsceneStartSfx;
    [SerializeField] private AudioClip cutsceneRollLoopSfx;
    [SerializeField] private AudioClip cutsceneBgm;

    private const float FallbackDeltaTime = 1f / 120f;
    private const float CutsceneDuration = 4.8f;

    private Texture2D circleTexture;
    private AudioSource sfxSource;
    private AudioSource rollLoopSource;
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
        Play(cutsceneStartSfx);
    }

    private void Update()
    {
        EnsureCamera();
        cutsceneTime += GetSafeUnscaledDeltaTime();
        SyncLoopSource(rollLoopSource, cutsceneRollLoopSfx);
        SyncLoopSource(bgmSource, cutsceneBgm);

        if (cutsceneTime >= CutsceneDuration)
        {
            StopAudio();
            SceneManager.LoadScene(MariodemoProgressState.StageSceneName);
        }
    }

    private void OnGUI()
    {
        EnsureTextures();
        DrawFilledRect(new Rect(0f, 0f, Screen.width, Screen.height), Color.black);

        float roadHeight = Screen.height * 0.18f;
        float roadY = Screen.height * 0.68f;
        Rect roadRect = new Rect(0f, roadY, Screen.width, roadHeight);
        DrawFilledRect(roadRect, new Color(0.43f, 0.43f, 0.45f, 1f));
        DrawFilledRect(new Rect(0f, roadY + roadHeight * 0.08f, Screen.width, roadHeight * 0.10f), new Color(0.36f, 0.36f, 0.38f, 1f));
        DrawFilledRect(new Rect(0f, roadY + roadHeight * 0.78f, Screen.width, roadHeight * 0.10f), new Color(0.36f, 0.36f, 0.38f, 1f));

        float dashWidth = Screen.width * 0.08f;
        float dashHeight = roadHeight * 0.06f;
        float dashGap = dashWidth * 0.55f;
        float dashOffset = Mathf.Repeat(cutsceneTime * Screen.width * 0.22f, dashWidth + dashGap);
        for (float x = -dashWidth; x < Screen.width + dashWidth; x += dashWidth + dashGap)
        {
            DrawFilledRect(new Rect(x - dashOffset, roadY + roadHeight * 0.49f, dashWidth, dashHeight), new Color(0.78f, 0.78f, 0.80f, 0.7f));
        }

        float progress = Mathf.Clamp01(cutsceneTime / (CutsceneDuration - 0.45f));
        float ballX = Mathf.Lerp(Screen.width * 0.10f, Screen.width * 0.88f, progress);
        float bob = Mathf.Sin(cutsceneTime * 7.5f) * Screen.height * 0.008f;
        float radius = Mathf.Clamp(Screen.height * 0.050f, 26f, 42f);
        Vector2 ballCenter = new Vector2(ballX, roadY + roadHeight * 0.35f - bob);

        DrawCircle(new Vector2(ballCenter.x + radius * 0.18f, roadY + roadHeight * 0.62f), radius * 0.92f, new Color(0f, 0f, 0f, 0.20f));
        DrawCircle(ballCenter, radius, new Color(0.98f, 0.88f, 0.18f, 1f));
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

        if (rollLoopSource == null)
        {
            rollLoopSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(rollLoopSource, true, 0.70f);
        }

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(bgmSource, true, 0.65f);
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
        if (clip == null) return;
        EnsureAudioSources();
        sfxSource.PlayOneShot(clip);
    }

    private static void SyncLoopSource(AudioSource source, AudioClip clip)
    {
        if (source == null) return;

        if (clip == null)
        {
            if (source.isPlaying) source.Stop();
            source.clip = null;
            return;
        }

        if (source.clip != clip)
        {
            source.Stop();
            source.clip = clip;
        }

        if (!source.isPlaying) source.Play();
    }

    private void StopAudio()
    {
        StopLoopSource(sfxSource);
        StopLoopSource(rollLoopSource);
        StopLoopSource(bgmSource);
    }

    private static void StopLoopSource(AudioSource source)
    {
        if (source == null) return;
        if (source.isPlaying) source.Stop();
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
        if (radius <= 0.5f) return;
        DrawTintedTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), circleTexture, color);
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
