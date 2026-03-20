using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public partial class PacJamDemoController
{
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

    private void ReturnToTitleMenu()
    {
        SceneManager.LoadScene(StartMenuSceneName);
    }

    private void LoadMariodemoCutscene()
    {
        SceneManager.LoadScene(MariodemoProgressState.CutsceneSceneName);
    }

    private void SetPlayerVisible(bool visible)
    {
        if (player == null) return;
        if (player.Body != null) player.Body.enabled = visible;
        if (player.IndicatorRenderer != null) player.IndicatorRenderer.enabled = visible;
    }

    private void SnapToCell(Actor a) { a.Root.position = CellToWorld(a.Cell, a.Root.position.z); }
    private bool CanMove(Vector2Int from, Vector2Int dir) { return dir != Vector2Int.zero && IsWalkable(from + dir); }
    private bool IsWalkable(Vector2Int c) { return c.x >= 0 && c.x < width && c.y >= 0 && c.y < height && map[c.x, c.y] != '#'; }
    private static Vector3 GridDirToWorld(Vector2Int d) { return new Vector3(d.x, -d.y, 0f); }
    private Vector3 CellToWorld(Vector2Int c, float z) { return new Vector3(c.x * CellSize, -c.y * CellSize, z); }
    private static Color Faint(Color c) { return Color.Lerp(c, Color.white, 0.65f); }
    private void Play(AudioClip clip)
    {
        if (clip == null) return;
        EnsureAudioSources();
        if (sfxSource != null) sfxSource.PlayOneShot(clip);
    }

    private void EnsureAudioSources()
    {
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(sfxSource, false, 0.95f);
        }

        if (stateLoopSource == null)
        {
            stateLoopSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(stateLoopSource, true, 0.70f);
        }

        if (moveLoopSource == null)
        {
            moveLoopSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(moveLoopSource, true, 0.60f);
        }

        if (cueSource == null)
        {
            cueSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(cueSource, false, 0.90f);
        }

        if (ghostHuntBgmSource == null)
        {
            ghostHuntBgmSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(ghostHuntBgmSource, true, 0.48f);
        }
    }

    private static void ConfigureAudioSource(AudioSource source, bool loop, float volume)
    {
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = volume;
    }

    private void UpdateLoopingAudio()
    {
        if (!runtimeInitialized || player == null || showQuitConfirm || gameOver || victory || respawnTimer > 0f)
        {
            StopManagedAudioLoops();
            return;
        }

        EnsureAudioSources();
        TickQueuedAudio();
        SyncLoopSource(stateLoopSource, DesiredStateLoopClip());
        SyncLoopSource(moveLoopSource, DesiredMoveLoopClip());
        SyncLoopSource(ghostHuntBgmSource, DesiredGhostHuntBgmClip());
    }

    private AudioClip DesiredStateLoopClip()
    {
        if (stagePhase != StagePhase.PelletRun)
        {
            return null;
        }

        if (frightenedTimer > 0f)
        {
            for (int i = 0; i < ghosts.Count; i++)
            {
                if (!ghosts[i].Captured && !ghosts[i].Eaten) return frightenedLoopSfx;
            }

            return null;
        }

        return chaseLoopUnlocked && modePhase % 2 == 1 ? chaseLoopSfx : null;
    }

    private AudioClip DesiredMoveLoopClip()
    {
        if (stagePhase == StagePhase.GhostHuntIntro)
        {
            return null;
        }

        return player.Dir != Vector2Int.zero ? moveLoopSfx : null;
    }

    private AudioClip DesiredGhostHuntBgmClip()
    {
        if (queuedGhostHuntBgm || stagePhase == StagePhase.PelletRun || ghostHuntBgm == null)
        {
            return null;
        }

        return stagePhase == StagePhase.GhostHuntIntro || stagePhase == StagePhase.GhostHunt ? ghostHuntBgm : null;
    }

    private void TickQueuedAudio()
    {
        if (!queuedGhostHuntBgm)
        {
            return;
        }

        ghostHuntBgmDelayTimer -= Time.unscaledDeltaTime;
        if (ghostHuntBgmDelayTimer > 0f)
        {
            return;
        }

        ghostHuntBgmDelayTimer = 0f;
        queuedGhostHuntBgm = false;
    }

    private float PlayCue(AudioClip clip)
    {
        EnsureAudioSources();
        if (cueSource == null)
        {
            return 0f;
        }

        cueSource.Stop();
        cueSource.clip = clip;
        if (clip == null)
        {
            return 0f;
        }

        cueSource.Play();
        return clip.length;
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

    private void StopManagedAudioLoops()
    {
        StopLoopSource(ghostHuntBgmSource);
        StopLoopSource(stateLoopSource);
        StopLoopSource(moveLoopSource);
        StopLoopSource(cueSource);
        queuedGhostHuntBgm = false;
        ghostHuntBgmDelayTimer = 0f;
    }

    private static void StopLoopSource(AudioSource source)
    {
        if (source == null) return;
        if (source.isPlaying) source.Stop();
        source.clip = null;
    }

    private static void SpawnFx(ParticleSystem prefab, Vector3 pos)
    {
        if (prefab == null) return;
        ParticleSystem fx = Instantiate(prefab, pos, Quaternion.identity);
        Destroy(fx.gameObject, 1.5f);
    }
}

