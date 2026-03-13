using System;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PacJamDemoController
{
    private static bool IsRestartPressed()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.rKey.wasPressedThisFrame;
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
}
