using UnityEngine;

public sealed partial class MariodemoController
{
    private void DrawRuntimeUi()
    {
        EnsureUiStyles();
        DrawLivesIndicator();

        if (!showQuitConfirm && !gameOver && !stageClear)
        {
            DrawQuitButton();
        }

        if (gameOver)
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
        int remainingLives = Mathf.Clamp(lives, 0, MariodemoProgressState.InitialLives);

        for (int i = 0; i < MariodemoProgressState.InitialLives; i++)
        {
            bool isActive = i < remainingLives;
            float x = margin + i * (diameter + gap);
            Rect rect = new Rect(x, margin, diameter, diameter);
            DrawTintedTexture(rect, uiCircleTexture, isActive ? LifeActiveColor : LifeLostColor);
        }

        if (MariodemoProgressState.HasSecretDiamond)
        {
            Texture2D diamondTexture = GetSecretDiamondTexture();
            if (diamondTexture != null)
            {
                float x = margin + MariodemoProgressState.InitialLives * (diameter + gap);
                Rect rect = new Rect(x, margin, diameter, diameter);
                GUI.DrawTexture(rect, diamondTexture, ScaleMode.ScaleToFit, true);
            }
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
        if (dialogTexture == null)
        {
            showQuitConfirm = false;
            return;
        }

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
            RestartStage();
            return;
        }

        if (restartTexture != null)
        {
            float scale = hovered ? 1.05f : 1f;
            Rect iconRect = CenterRect(buttonRect.center, buttonRect.width * scale, buttonRect.height * scale);
            GUI.DrawTexture(iconRect, restartTexture, ScaleMode.ScaleToFit, true);
        }
    }

    private Texture2D GetQuitDialogTexture()
    {
        if (!quitDialogTextureChecked)
        {
            quitDialogTextureChecked = true;
            loadedQuitDialogTexture = Resources.Load<Texture2D>(QuitDialogResourcePath);
        }

        return loadedQuitDialogTexture;
    }

    private Texture2D GetRestartButtonTexture()
    {
        if (!restartButtonTextureChecked)
        {
            restartButtonTextureChecked = true;
            loadedRestartButtonTexture = Resources.Load<Texture2D>(RestartButtonResourcePath);
        }

        return loadedRestartButtonTexture;
    }

    private Texture2D GetSecretDiamondTexture()
    {
        if (!secretDiamondTextureChecked)
        {
            secretDiamondTextureChecked = true;
            loadedSecretDiamondTexture = Resources.Load<Texture2D>(SecretDiamondResourcePath);
        }

        return loadedSecretDiamondTexture;
    }

    private Sprite GetSecretDiamondSprite()
    {
        if (loadedSecretDiamondSprite != null)
        {
            return loadedSecretDiamondSprite;
        }

        Texture2D diamondTexture = GetSecretDiamondTexture();
        if (diamondTexture == null)
        {
            return null;
        }

        float pixelsPerUnit = Mathf.Max(1f, Mathf.Max(diamondTexture.width, diamondTexture.height));
        loadedSecretDiamondSprite = Sprite.Create(
            diamondTexture,
            new Rect(0f, 0f, diamondTexture.width, diamondTexture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit);
        return loadedSecretDiamondSprite;
    }
    private void EnsureQuitButtonIcons()
    {
        if (quitStayIconTexture != null && quitExitIconTexture != null) return;
        quitStayIconTexture = BuildTriangleIconTexture(120, true);
        quitExitIconTexture = BuildSquareIconTexture(120);
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

    private void DrawPanel(Rect rect)
    {
        DrawPanelShadow(rect, 6f);
        DrawFilledRect(rect, new Color(0.98f, 0.97f, 0.95f, 0.98f));
    }

    private static void DrawPanelShadow(Rect rect, float offset)
    {
        DrawFilledRect(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), new Color(0f, 0f, 0f, 0.10f));
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
        return new Rect(rect.x + insetX, rect.y + insetY, Mathf.Max(0f, rect.width - insetX * 2f), Mathf.Max(0f, rect.height - insetY * 2f));
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

    private static bool CircleIntersectsRect(Vector2 center, float radius, Rect rect)
    {
        float closestX = Mathf.Clamp(center.x, rect.xMin, rect.xMax);
        float closestY = Mathf.Clamp(center.y, rect.yMin, rect.yMax);
        float dx = center.x - closestX;
        float dy = center.y - closestY;
        return dx * dx + dy * dy <= radius * radius;
    }
}

