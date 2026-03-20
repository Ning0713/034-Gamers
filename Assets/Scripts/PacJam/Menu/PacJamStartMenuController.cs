using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class PacJamStartMenuController : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "PacJamDemo";
    [SerializeField] private string introCutsceneSceneName = "PacJamIntroCutscene";

    [Header("Audio")]
    [SerializeField] private AudioClip titleBgm;

    private static readonly Color TitleColor = new Color(0.96f, 0.96f, 0.96f, 1f);
    private static readonly Color YellowColor = new Color(0.98f, 0.89f, 0.20f, 1f);
    private static readonly Color GreenColor = new Color(0.17f, 0.94f, 0.35f, 1f);
    private static readonly Color RedColor = new Color(0.96f, 0.24f, 0.27f, 1f);
    private static readonly Color OrangeColor = new Color(0.99f, 0.56f, 0.12f, 1f);
    private static readonly Color BlueColor = new Color(0.28f, 0.88f, 1f, 1f);
    private static readonly Color TimelineLineColor = new Color(0.74f, 0.74f, 0.78f, 0.36f);
    private static readonly Color TimelineNodeOuterColor = new Color(0.96f, 0.96f, 0.98f, 0.96f);
    private static readonly Color TimelineNodeInnerColor = new Color(0.02f, 0.02f, 0.03f, 1f);
    private static readonly Color TimelineArrowColor = new Color(0.98f, 0.98f, 1f, 0.72f);
    private static readonly Color BlockColor = new Color(0.78f, 0.78f, 0.82f, 1f);
    private static readonly Color DoorFrameColor = new Color(0.78f, 0.78f, 0.82f, 1f);
    private static readonly Color DoorFillColor = new Color(0.08f, 0.08f, 0.10f, 1f);
    private static readonly Color ButtonBorderColor = new Color(0.92f, 0.92f, 0.92f, 1f);
    private static readonly Color ButtonFillColor = new Color(0.04f, 0.04f, 0.04f, 1f);
    private static readonly Color MushroomCapColor = new Color(1f, 0.36f, 0.34f, 1f);
    private static readonly Color MushroomStemColor = new Color(0.95f, 0.94f, 0.88f, 1f);
    private static readonly Vector2[] PacDotPoints =
    {
        new Vector2(0.28f, 0.28f),
        new Vector2(0.50f, 0.28f),
        new Vector2(0.72f, 0.28f),
        new Vector2(0.72f, 0.50f),
        new Vector2(0.72f, 0.72f),
        new Vector2(0.50f, 0.72f),
        new Vector2(0.28f, 0.72f),
        new Vector2(0.28f, 0.50f),
    };
    private static readonly Dictionary<char, string[]> TitleGlyphs = new Dictionary<char, string[]>
    {
        { 'G', new[] { "01110", "10001", "10000", "10111", "10001", "10001", "01110" } },
        { 'A', new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" } },
        { 'M', new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" } },
        { 'E', new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" } },
        { 'R', new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" } },
        { 'S', new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" } },
    };

    private const float FallbackDeltaTime = 1f / 120f;
    private const string TitleBgmAssetPath = "Assets/Music/Menu/StartMenuTheme.mp3";

    private Texture2D circleTexture;
    private Texture2D upTriangleTexture;
    private float menuTime;
    private bool startRequested;
    private bool quitRequested;
    private AudioSource bgmSource;

    private void Awake()
    {
        Time.timeScale = 1f;
        Application.targetFrameRate = 120;
        Application.runInBackground = true;
        EnsureCamera();
        EnsureTextures();
        EnsureAudioSource();
        SyncLoopSource(bgmSource, titleBgm);
    }

    private void Update()
    {
        EnsureCamera();
        EnsureAudioSource();
        SyncLoopSource(bgmSource, titleBgm);
        menuTime += GetSafeUnscaledDeltaTime();

        if (startRequested || IsStartPressed())
        {
            startRequested = false;
            MariodemoProgressState.ResetProgress();
            ForestdemoProgressState.ResetProgress();
            SceneManager.LoadScene(string.IsNullOrWhiteSpace(introCutsceneSceneName) ? gameplaySceneName : introCutsceneSceneName);
            return;
        }

        if (quitRequested)
        {
            quitRequested = false;
            QuitApplication();
            return;
        }
    }

    private void OnGUI()
    {
        EnsureTextures();
        DrawFilledRect(new Rect(0f, 0f, Screen.width, Screen.height), Color.black);

        Rect titleRect = new Rect(0f, Screen.height * 0.08f, Screen.width, Screen.height * 0.18f);
        DrawPixelTitle("GAMERS", titleRect, TitleColor);

        float sectionGap = Mathf.Clamp(Screen.width * 0.045f, 20f, 72f);
        float sectionWidth = Mathf.Clamp((Screen.width - sectionGap * 2f - 80f) / 3f, 160f, 280f);
        float sectionHeight = Mathf.Clamp(Screen.height * 0.24f, 140f, 220f);
        float totalWidth = sectionWidth * 3f + sectionGap * 2f;
        float startX = (Screen.width - totalWidth) * 0.5f;
        float sectionY = Mathf.Clamp(Screen.height * 0.44f, 230f, Screen.height * 0.58f);

        Rect leftRect = new Rect(startX, sectionY, sectionWidth, sectionHeight);
        Rect midRect = new Rect(leftRect.xMax + sectionGap, sectionY, sectionWidth, sectionHeight);
        Rect rightRect = new Rect(midRect.xMax + sectionGap, sectionY, sectionWidth, sectionHeight);

        DrawTimeline(titleRect, leftRect, midRect, rightRect, menuTime);
        DrawPacSection(leftRect, menuTime);
        DrawJumpSection(midRect, menuTime);
        DrawDoorSection(rightRect, menuTime);

        float buttonWidth = Mathf.Clamp(Screen.width * 0.22f, 180f, 260f);
        float buttonHeight = Mathf.Clamp(Screen.height * 0.10f, 62f, 84f);
        float buttonGap = Mathf.Clamp(Screen.height * 0.020f, 12f, 18f);
        float buttonGroupHeight = buttonHeight * 2f + buttonGap;
        float buttonY = Mathf.Min(Screen.height - buttonGroupHeight - 26f, sectionY + sectionHeight + Screen.height * 0.09f);

        Rect buttonRect = new Rect(
            (Screen.width - buttonWidth) * 0.5f,
            buttonY,
            buttonWidth,
            buttonHeight);
        Rect quitRect = new Rect(buttonRect.x, buttonRect.yMax + buttonGap, buttonWidth, buttonHeight);

        DrawStartButton(buttonRect);
        DrawQuitButton(quitRect);
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

    private void EnsureAudioSource()
    {
        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }

            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f;
            bgmSource.volume = 0.58f;
        }
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

    private void EnsureTextures()
    {
        if (circleTexture == null)
        {
            circleTexture = BuildCircleTexture(128);
        }

        if (upTriangleTexture == null)
        {
            upTriangleTexture = BuildTriangleTexture(128);
        }
    }

    private void DrawStartButton(Rect rect)
    {
        bool hovered = rect.Contains(Event.current.mousePosition);
        DrawMenuButtonFrame(rect, hovered);

        float pulse = 1f + Mathf.Sin(menuTime * 4.8f) * 0.06f + (hovered ? 0.04f : 0f);
        float outerRadius = Mathf.Min(rect.width, rect.height) * 0.23f * pulse;
        Vector2 center = rect.center;
        DrawCircle(center, outerRadius, GreenColor);
        DrawCircle(center, outerRadius * 0.58f, ButtonFillColor);

        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
        {
            startRequested = true;
        }
    }

    private void DrawQuitButton(Rect rect)
    {
        bool hovered = rect.Contains(Event.current.mousePosition);
        DrawMenuButtonFrame(rect, hovered);

        float pulse = 1f + Mathf.Sin(menuTime * 4.2f + 0.4f) * 0.05f + (hovered ? 0.04f : 0f);
        float iconSize = Mathf.Min(rect.width, rect.height) * 0.34f * pulse;
        float thickness = Mathf.Max(4f, iconSize * 0.18f);
        DrawCross(rect.center, iconSize, thickness, RedColor);

        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
        {
            quitRequested = true;
        }
    }

    private static void DrawMenuButtonFrame(Rect rect, bool hovered)
    {
        DrawFilledRect(new Rect(rect.x + 4f, rect.y + 4f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.30f));
        DrawFilledRect(rect, ButtonBorderColor);
        Rect innerRect = ShrinkRect(rect, 2f, 2f);
        DrawFilledRect(innerRect, hovered ? new Color(0.08f, 0.08f, 0.08f, 1f) : ButtonFillColor);
    }

    private void DrawTimeline(Rect titleRect, Rect leftRect, Rect midRect, Rect rightRect, float time)
    {
        float timelineY = Mathf.Lerp(titleRect.yMax, leftRect.y, 0.43f);
        float lineThickness = Mathf.Clamp(Screen.height * 0.0060f, 3f, 5f);
        float nodeRadius = Mathf.Clamp(Screen.height * 0.0165f, 10f, 14f);
        float lineStartX = leftRect.xMin - leftRect.width * 0.12f;
        float lineEndX = rightRect.xMax + rightRect.width * 0.12f;

        DrawFilledRect(new Rect(lineStartX, timelineY - lineThickness * 0.5f, lineEndX - lineStartX, lineThickness), TimelineLineColor);

        Vector2 leftNode = new Vector2(leftRect.center.x, timelineY);
        Vector2 midNode = new Vector2(midRect.center.x, timelineY);
        Vector2 rightNode = new Vector2(rightRect.center.x, timelineY);

        DrawTimelineNode(leftNode, nodeRadius);
        DrawTimelineNode(midNode, nodeRadius);
        DrawTimelineNode(rightNode, nodeRadius);

        DrawTimelineFlow(leftNode.x, midNode.x, timelineY, time, 0f, nodeRadius);
        DrawTimelineFlow(midNode.x, rightNode.x, timelineY, time, 0.42f, nodeRadius);
    }

    private void DrawTimelineNode(Vector2 center, float radius)
    {
        DrawCircle(center, radius, TimelineNodeOuterColor);
        DrawCircle(center, radius * 0.62f, TimelineNodeInnerColor);
    }

    private void DrawTimelineFlow(float startX, float endX, float y, float time, float phase, float nodeRadius)
    {
        float padding = nodeRadius * 1.7f;
        float usableStart = startX + padding;
        float usableEnd = endX - padding;
        if (usableEnd <= usableStart)
        {
            return;
        }

        float arrowWidth = Mathf.Clamp((usableEnd - usableStart) * 0.14f, 18f, 30f);
        float arrowHeight = arrowWidth * 0.92f;

        for (int i = 0; i < 4; i++)
        {
            float t = Mathf.Repeat(time * 0.26f + phase + i * 0.22f, 1f);
            float x = Mathf.Lerp(usableStart, usableEnd, t);
            float alpha = Mathf.Lerp(0.28f, TimelineArrowColor.a, 1f - Mathf.Abs(t - 0.5f) * 1.2f);
            Color arrowColor = new Color(TimelineArrowColor.r, TimelineArrowColor.g, TimelineArrowColor.b, Mathf.Clamp01(alpha));
            DrawTriangle(new Vector2(x, y), arrowWidth, arrowHeight, -90f, arrowColor);
        }
    }

    private void DrawPacSection(Rect rect, float time)
    {
        float cycle = 5.2f;
        float loop = Mathf.Repeat(time, cycle) / cycle;
        float pathProgress = loop * PacDotPoints.Length;
        int segment = Mathf.FloorToInt(pathProgress) % PacDotPoints.Length;
        float segmentT = pathProgress - Mathf.Floor(pathProgress);
        int eatenCount = Mathf.Clamp(Mathf.FloorToInt(pathProgress + 0.05f), 0, PacDotPoints.Length);

        for (int i = eatenCount; i < PacDotPoints.Length; i++)
        {
            DrawCircle(PointFromNormalized(rect, PacDotPoints[i]), Mathf.Min(rect.width, rect.height) * 0.030f, TitleColor);
        }

        Vector2 from = PointFromNormalized(rect, PacDotPoints[segment]);
        Vector2 to = PointFromNormalized(rect, PacDotPoints[(segment + 1) % PacDotPoints.Length]);
        Vector2 center = Vector2.Lerp(from, to, segmentT);
        DrawPacman(center, Mathf.Min(rect.width, rect.height) * 0.13f);
    }

    private void DrawJumpSection(Rect rect, float time)
    {
        float cycle = 4.8f;
        float local = Mathf.Repeat(time, cycle);
        float groundY = 0.80f;
        float radius = Mathf.Min(rect.width, rect.height) * 0.085f;
        float xNorm = 0.16f;
        float yNorm = groundY;

        if (local < 1.55f)
        {
            float p = local / 1.55f;
            xNorm = Mathf.Lerp(0.16f, 0.48f, EaseInOutSine(p));
        }
        else if (local < 2.45f)
        {
            float p = (local - 1.55f) / 0.90f;
            xNorm = Mathf.Lerp(0.48f, 0.68f, p);
            yNorm = groundY - Mathf.Sin(p * Mathf.PI) * 0.28f;
        }
        else if (local < 3.30f)
        {
            xNorm = 0.68f;
        }
        else
        {
            float p = (local - 3.30f) / 1.50f;
            xNorm = Mathf.Lerp(0.68f, 0.78f, EaseInOutSine(p));
        }

        for (int i = 0; i < 4; i++)
        {
            float blockX = 0.10f + i * 0.12f;
            DrawFilledRect(RectFromNormalized(rect, blockX, 0.84f, 0.10f, 0.06f), new Color(0.24f, 0.24f, 0.28f, 1f));
        }

        float bump = Mathf.Max(0f, 1f - Mathf.Abs(local - 2.08f) / 0.16f);
        Rect blockRect = RectFromNormalized(rect, 0.68f, 0.28f - bump * 0.03f, 0.16f, 0.16f);
        DrawFilledRect(blockRect, BlockColor);
        DrawFilledRect(ShrinkRect(blockRect, blockRect.width * 0.12f, blockRect.height * 0.12f), new Color(0.38f, 0.38f, 0.42f, 1f));

        DrawCircle(PointFromNormalized(rect, xNorm, yNorm), radius, OrangeColor);

        float mushroomPop = Mathf.Clamp01((local - 2.08f) / 0.65f);
        if (mushroomPop > 0f)
        {
            float fade = 1f - Mathf.Clamp01((local - 4.15f) / 0.40f);
            Color stemColor = new Color(MushroomStemColor.r, MushroomStemColor.g, MushroomStemColor.b, fade);
            Color capColor = new Color(MushroomCapColor.r, MushroomCapColor.g, MushroomCapColor.b, fade);

            Vector2 stemCenter = PointFromNormalized(rect, 0.76f, Mathf.Lerp(0.40f, 0.23f, mushroomPop));
            Rect stemRect = CenterRect(new Vector2(stemCenter.x, stemCenter.y + rect.height * 0.040f), rect.width * 0.08f, rect.height * 0.10f);
            DrawFilledRect(stemRect, stemColor);
            DrawTriangle(new Vector2(stemCenter.x, stemCenter.y - rect.height * 0.012f), rect.width * 0.16f, rect.height * 0.13f, 180f, capColor);
        }
    }

    private void DrawDoorSection(Rect rect, float time)
    {
        float cycle = 4.2f;
        float local = Mathf.Repeat(time, cycle);
        float groundY = 0.80f;
        float radius = Mathf.Min(rect.width, rect.height) * 0.085f;
        bool visible = local < 3.35f;
        float xNorm = 0.16f;
        float alpha = 1f;
        float scale = 1f;

        if (local < 2.55f)
        {
            float p = local / 2.55f;
            xNorm = Mathf.Lerp(0.16f, 0.60f, EaseInOutSine(p));
        }
        else if (local < 3.35f)
        {
            float p = (local - 2.55f) / 0.80f;
            xNorm = Mathf.Lerp(0.60f, 0.72f, p);
            alpha = 1f - p;
            scale = 1f - p * 0.72f;
        }
        else
        {
            visible = false;
        }

        DrawFilledRect(RectFromNormalized(rect, 0.10f, 0.84f, 0.78f, 0.02f), new Color(0.24f, 0.24f, 0.28f, 1f));

        if (visible)
        {
            DrawCircle(PointFromNormalized(rect, xNorm, groundY), radius * scale, new Color(BlueColor.r, BlueColor.g, BlueColor.b, alpha));
        }

        Rect doorOuter = RectFromNormalized(rect, 0.66f, 0.26f, 0.18f, 0.46f);
        DrawFilledRect(doorOuter, DoorFrameColor);
        DrawFilledRect(ShrinkRect(doorOuter, doorOuter.width * 0.16f, doorOuter.height * 0.10f), DoorFillColor);
    }

    private void DrawPixelTitle(string word, Rect area, Color color)
    {
        int totalColumns = MeasureWordColumns(word);
        if (totalColumns <= 0) return;

        float cell = Mathf.Floor(Mathf.Min(area.width / totalColumns, area.height / 7f));
        if (cell < 2f) return;

        float blockSize = Mathf.Max(1f, Mathf.Floor(cell * 0.78f));
        float totalWidth = totalColumns * cell;
        float totalHeight = 7f * cell;
        float startX = area.x + (area.width - totalWidth) * 0.5f;
        float startY = area.y + (area.height - totalHeight) * 0.5f;
        float inset = (cell - blockSize) * 0.5f;
        float shadowOffset = Mathf.Max(1f, Mathf.Floor(cell * 0.12f));

        foreach (char rawChar in word)
        {
            string[] glyph;
            if (!TitleGlyphs.TryGetValue(char.ToUpperInvariant(rawChar), out glyph))
            {
                startX += cell;
                continue;
            }

            for (int row = 0; row < glyph.Length; row++)
            {
                string line = glyph[row];
                for (int col = 0; col < line.Length; col++)
                {
                    if (line[col] != '1') continue;

                    Rect blockRect = new Rect(startX + col * cell + inset, startY + row * cell + inset, blockSize, blockSize);
                    DrawFilledRect(new Rect(blockRect.x + shadowOffset, blockRect.y + shadowOffset, blockRect.width, blockRect.height), new Color(0.12f, 0.12f, 0.12f, 1f));
                    DrawFilledRect(blockRect, color);
                }
            }

            startX += (glyph[0].Length + 1) * cell;
        }
    }

    private void DrawPacman(Vector2 center, float radius)
    {
        DrawCircle(center, radius, YellowColor);
    }

    private void DrawCircle(Vector2 center, float radius, Color color)
    {
        if (radius <= 0.5f) return;
        DrawTintedTexture(CenterRect(center, radius * 2f, radius * 2f), circleTexture, color);
    }

    private void DrawTriangle(Vector2 center, float width, float height, float angle, Color color)
    {
        Matrix4x4 oldMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, center);
        DrawTintedTexture(CenterRect(center, width, height), upTriangleTexture, color);
        GUI.matrix = oldMatrix;
    }

    private static void DrawCross(Vector2 center, float size, float thickness, Color color)
    {
        DrawRotatedRect(center, size, thickness, 45f, color);
        DrawRotatedRect(center, size, thickness, -45f, color);
    }

    private static void DrawRotatedRect(Vector2 center, float width, float height, float angle, Color color)
    {
        Matrix4x4 oldMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, center);
        DrawFilledRect(CenterRect(center, width, height), color);
        GUI.matrix = oldMatrix;
    }

    private static int MeasureWordColumns(string word)
    {
        int total = 0;
        for (int i = 0; i < word.Length; i++)
        {
            string[] glyph;
            if (!TitleGlyphs.TryGetValue(char.ToUpperInvariant(word[i]), out glyph)) continue;
            total += glyph[0].Length;
            if (i < word.Length - 1) total += 1;
        }

        return total;
    }

    private static bool IsStartPressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;
        return keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame;
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

    private static float EaseInOutSine(float t)
    {
        t = Mathf.Clamp01(t);
        return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
    }

    private static Rect RectFromNormalized(Rect rect, float x, float y, float width, float height)
    {
        return new Rect(
            rect.x + rect.width * x,
            rect.y + rect.height * y,
            rect.width * width,
            rect.height * height);
    }

    private static Vector2 PointFromNormalized(Rect rect, float x, float y)
    {
        return new Vector2(rect.x + rect.width * x, rect.y + rect.height * y);
    }

    private static Vector2 PointFromNormalized(Rect rect, Vector2 normalized)
    {
        return PointFromNormalized(rect, normalized.x, normalized.y);
    }

    private static Rect CenterRect(Vector2 center, float width, float height)
    {
        return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
    }

    private static Rect ShrinkRect(Rect rect, float insetX, float insetY)
    {
        return new Rect(
            rect.x + insetX,
            rect.y + insetY,
            Mathf.Max(0f, rect.width - insetX * 2f),
            Mathf.Max(0f, rect.height - insetY * 2f));
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

    private static Texture2D BuildTriangleTexture(int size)
    {
        Texture2D texture = CreateUiTexture(size);
        Color[] pixels = new Color[size * size];
        Vector2 a = new Vector2(0.50f, 0.08f);
        Vector2 b = new Vector2(0.08f, 0.92f);
        Vector2 c = new Vector2(0.92f, 0.92f);

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

    private static Texture2D BuildCircleTexture(int size)
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignAudioClipIfMissing(ref titleBgm, TitleBgmAssetPath);
    }

    private static void AutoAssignAudioClipIfMissing(ref AudioClip clip, string assetPath)
    {
        if (clip != null)
        {
            return;
        }

        clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
    }
#endif

    private static void QuitApplication()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

