using System.Collections.Generic;
using UnityEngine;

public partial class PacJamDemoController
{
    private void TickModeTimer(float dt)
    {
        if (stagePhase != StagePhase.PelletRun || enemyPauseTimer > 0f)
        {
            return;
        }

        if (frightenedTimer > 0f)
        {
            frightenedTimer -= dt;
            if (frightenedTimer <= 0f)
            {
                frightenedTimer = 0f;
                ReleaseEatenGhostsAfterFrightened();
            }

            return;
        }

        modeTimer += dt;
        if (modeTimer >= ModeDurations[Mathf.Min(modePhase, ModeDurations.Length - 1)])
        {
            modeTimer = 0f;
            if (modePhase < ModeDurations.Length - 1) modePhase++;
        }
    }

    private void TickStagePhase(float dt)
    {
        if (stagePhase != StagePhase.GhostHuntIntro)
        {
            return;
        }

        ghostHuntIntroTimer -= dt;
        if (ghostHuntIntroTimer > 0f)
        {
            return;
        }

        ghostHuntIntroTimer = 0f;
        BeginGhostHuntStage();
    }

    private void TryStartGhostHuntTransition()
    {
        if (stagePhase != StagePhase.PelletRun || pellets.Count > 0)
        {
            return;
        }

        BeginGhostHuntTransition();
    }

    private void BeginGhostHuntTransition()
    {
        stagePhase = StagePhase.GhostHuntIntro;
        ghostHuntIntroTimer = GhostHuntIntroDuration;
        frightenedTimer = 0f;
        enemyPauseTimer = 0f;
        modeTimer = 0f;
        chaseLoopUnlocked = false;
        queuedGhostHuntBgm = ghostHuntBgm != null;
        ghostHuntBgmDelayTimer = PlayCue(ghostHuntIntroSfx);

        player.Cell = player.Spawn;
        player.Root.position = CellToWorld(player.Spawn, player.Root.position.z);
        player.Dir = Vector2Int.left;
        player.WantDir = Vector2Int.left;
        player.LastDir = Vector2Int.left;
        UpdateIndicator(player);

        for (int i = 0; i < ghosts.Count; i++)
        {
            Ghost g = ghosts[i];
            g.Captured = false;
            g.Eaten = false;
            g.Cell = g.Spawn;
            g.Root.gameObject.SetActive(true);
            g.Root.position = CellToWorld(g.Spawn, g.Root.position.z);
            g.Dir = Vector2Int.zero;
            g.WantDir = Vector2Int.zero;
            PaintGhost(g, GetGhostState(g));
            UpdateIndicator(g);
        }
    }

    private void BeginGhostHuntStage()
    {
        stagePhase = StagePhase.GhostHunt;

        for (int i = 0; i < ghosts.Count; i++)
        {
            Ghost g = ghosts[i];
            if (g.Captured) continue;

            g.Dir = ChooseGhostDir(g, GetGhostState(g));
            if (g.Dir == Vector2Int.zero)
            {
                g.Dir = FirstWalkableDir(g.Cell);
            }

            PaintGhost(g, GetGhostState(g));
            UpdateIndicator(g);
        }
    }

    private bool AreAllGhostsCaptured()
    {
        for (int i = 0; i < ghosts.Count; i++)
        {
            if (!ghosts[i].Captured) return false;
        }

        return ghosts.Count > 0;
    }
    private void BeginMariodemoTransition()
    {
        if (mariodemoTransitionActive)
        {
            return;
        }

        mariodemoTransitionActive = true;
        mariodemoTransitionTimer = MariodemoTeleportDuration;
        SetPlayerVisible(true);

        for (int i = 0; i < ghosts.Count; i++)
        {
            ghosts[i].Root.gameObject.SetActive(false);
        }
    }

    private void TickMariodemoTransition(float dt)
    {
        mariodemoTransitionTimer -= dt;
        bool visible = Mathf.FloorToInt(Mathf.Max(mariodemoTransitionTimer, 0f) * 10f) % 2 == 0;
        SetPlayerVisible(visible);

        if (mariodemoTransitionTimer > 0f)
        {
            return;
        }

        mariodemoTransitionActive = false;
        SetPlayerVisible(true);
        MariodemoProgressState.BeginFromPacJam(lives);
        LoadMariodemoCutscene();
    }

    private void CaptureGhostInGhostHunt(Ghost g)
    {
        g.Captured = true;
        g.Eaten = false;
        g.Dir = Vector2Int.zero;
        g.WantDir = Vector2Int.zero;
        score += 200;
        Play(ghostEatenSfx);
        SpawnFx(ghostEatFxPrefab, g.Root.position);
        g.Root.gameObject.SetActive(false);
    }

    private void ReleaseEatenGhostsAfterFrightened()
    {
        for (int i = 0; i < ghosts.Count; i++)
        {
            Ghost g = ghosts[i];
            if (g.Captured || !g.Eaten) continue;

            g.Eaten = false;
            g.Cell = g.Spawn;
            g.Root.position = CellToWorld(g.Spawn, g.Root.position.z);
            g.Dir = ChooseGhostDir(g, GetGhostState(g));
            if (g.Dir == Vector2Int.zero)
            {
                for (int d = 0; d < Dirs.Length; d++)
                {
                    if (CanMove(g.Cell, Dirs[d]))
                    {
                        g.Dir = Dirs[d];
                        break;
                    }
                }
            }
        }
    }

    private void TickPlayer(float dt)
    {
        if (stagePhase == StagePhase.GhostHuntIntro)
        {
            SnapToCell(player);
            UpdateIndicator(player);
            return;
        }

        Vector2Int inputDir = ReadInputDir();
        if (inputDir != Vector2Int.zero)
        {
            player.WantDir = inputDir;

            if (player.Dir != Vector2Int.zero && inputDir == -player.Dir)
            {
                player.Dir = inputDir;
            }
        }

        TryApplyPlayerPreTurn();
        AdvanceActor(player, PlayerSpeed, dt, OnPlayerAtCellCenter);
        EatPellet(player.Cell);
        UpdateIndicator(player);
    }

    private void TickGhosts(float dt)
    {
        for (int i = 0; i < ghosts.Count; i++)
        {
            Ghost g = ghosts[i];
            if (g.Captured)
            {
                if (g.Root.gameObject.activeSelf)
                {
                    g.Root.gameObject.SetActive(false);
                }

                continue;
            }

            GhostState state = GetGhostState(g);

            if (state == GhostState.Guarded)
            {
                g.Cell = g.Spawn;
                g.Root.position = CellToWorld(g.Spawn, g.Root.position.z);
                g.Dir = Vector2Int.zero;
                PaintGhost(g, state);
                UpdateIndicator(g);
                continue;
            }

            if (enemyPauseTimer > 0f)
            {
                PaintGhost(g, state);
                UpdateIndicator(g);
                continue;
            }

            if (state == GhostState.Eaten && frightenedTimer <= 0f)
            {
                g.Eaten = false;
                state = GetGhostState(g);
            }

            if (state == GhostState.Eaten)
            {
                g.Cell = g.Spawn;
                g.Root.position = CellToWorld(g.Spawn, g.Root.position.z);
                g.Dir = Vector2Int.zero;
                PaintGhost(g, state);
                UpdateIndicator(g);
                continue;
            }

            if (g.Dir == Vector2Int.zero || (AtCenter(g) && !CanMove(g.Cell, g.Dir)))
            {
                Vector2Int bootstrap = ChooseGhostDir(g, state);
                if (bootstrap == Vector2Int.zero) bootstrap = FirstWalkableDir(g.Cell);
                g.Dir = bootstrap;
            }
            else if (stagePhase == StagePhase.GhostHunt && AtCenter(g) && IsGhostMoveBlockedByOtherGhost(g, g.Cell + g.Dir))
            {
                Vector2Int alternate = ChooseGhostDir(g, state);
                if (alternate != Vector2Int.zero && !IsGhostMoveBlockedByOtherGhost(g, g.Cell + alternate))
                {
                    g.Dir = alternate;
                }
                else
                {
                    g.Dir = Vector2Int.zero;
                }
            }

            AdvanceActor(g, GhostSpeed(state), dt, OnGhostAtCellCenter);
            if (g.Dir == Vector2Int.zero)
            {
                Vector2Int fallback = ChooseGhostDir(g, state);
                if (fallback == Vector2Int.zero) fallback = FirstWalkableDir(g.Cell);
                if (fallback != Vector2Int.zero) g.Dir = fallback;
            }

            PaintGhost(g, GetGhostState(g));
            UpdateIndicator(g);
        }
    }

    private void SimulateStep(float dt)
    {
        if (respawnTimer > 0f)
        {
            respawnTimer -= dt;
            if (respawnTimer < 0f) respawnTimer = 0f;
        }

        if (enemyPauseTimer > 0f)
        {
            enemyPauseTimer -= dt;
            if (enemyPauseTimer < 0f) enemyPauseTimer = 0f;
        }

        TickModeTimer(dt);
        TickStagePhase(dt);
        TickPlayer(dt);
        TryStartGhostHuntTransition();
        TickGhosts(dt);
        CheckCollisions();
    }

    private void CheckCollisions()
    {
        if (respawnTimer > 0f || stagePhase == StagePhase.GhostHuntIntro) return;

        float hitSqr = CollisionDistance * CollisionDistance;
        for (int i = 0; i < ghosts.Count; i++)
        {
            Ghost g = ghosts[i];
            if (g.Captured || !g.Root.gameObject.activeSelf) continue;
            if ((g.Root.position - player.Root.position).sqrMagnitude > hitSqr) continue;

            if (stagePhase == StagePhase.GhostHunt)
            {
                CaptureGhostInGhostHunt(g);
                if (AreAllGhostsCaptured())
                {
                    BeginMariodemoTransition();
                    return;
                }

                continue;
            }

            GhostState s = GetGhostState(g);
            if (s == GhostState.Frightened)
            {
                g.Eaten = true;
                g.Cell = g.Spawn;
                g.Root.position = CellToWorld(g.Spawn, g.Root.position.z);
                g.Dir = Vector2Int.zero;
                score += 200;
                Play(ghostEatenSfx);
                SpawnFx(ghostEatFxPrefab, g.Root.position);
                PaintGhost(g, GetGhostState(g));
                UpdateIndicator(g);
            }
            else if (s != GhostState.Eaten && s != GhostState.Captured)
            {
                lives--;
                Play(playerHitSfx);
                SpawnFx(playerHitFxPrefab, player.Root.position);
                if (lives <= 0) { gameOver = true; return; }

                frightenedTimer = 0f;
                respawnTimer = RespawnPauseDuration;
                ResetActorsToSpawn();
                return;
            }
        }
    }

    private void ResetActorsToSpawn()
    {
        player.Cell = player.Spawn;
        player.Root.position = CellToWorld(player.Spawn, player.Root.position.z);
        player.Dir = Vector2Int.left;
        player.WantDir = Vector2Int.left;
        chaseLoopUnlocked = false;
        enemyPauseTimer = EnemyPauseDuration;
        UpdateIndicator(player);

        for (int i = 0; i < ghosts.Count; i++)
        {
            Ghost g = ghosts[i];
            g.Cell = g.Spawn;
            g.Root.position = CellToWorld(g.Spawn, g.Root.position.z);
            g.Dir = g.Spawn.x < width / 2 ? Vector2Int.left : Vector2Int.right;
            g.Eaten = false;
            g.Captured = false;
            g.Root.gameObject.SetActive(true);
            PaintGhost(g, GetGhostState(g));
            UpdateIndicator(g);
        }
    }

    private GhostState GetGhostState(Ghost g)
    {
        if (g.Captured) return GhostState.Captured;
        if (stagePhase == StagePhase.GhostHuntIntro) return GhostState.Guarded;
        if (stagePhase == StagePhase.GhostHunt) return GhostState.Prey;
        if (g.Eaten) return GhostState.Eaten;
        if (frightenedTimer > 0f) return GhostState.Frightened;
        return modePhase % 2 == 0 ? GhostState.Scatter : GhostState.Chase;
    }

    private float GhostSpeed(GhostState s)
    {
        if (s == GhostState.Scatter) return GhostScatterSpeed;
        if (s == GhostState.Chase) return GhostChaseSpeed;
        if (s == GhostState.Frightened) return GhostFrightenedSpeed;
        if (s == GhostState.Eaten) return GhostEatenSpeed;
        if (s == GhostState.Prey) return GhostPreySpeed;
        return 0f;
    }

    private Vector2Int ChooseGhostDir(Ghost g, GhostState state)
    {
        if (state == GhostState.Guarded || state == GhostState.Captured)
        {
            return Vector2Int.zero;
        }

        List<Vector2Int> options = new List<Vector2Int>(4);
        for (int i = 0; i < Dirs.Length; i++) if (CanMove(g.Cell, Dirs[i])) options.Add(Dirs[i]);
        if (options.Count == 0) return Vector2Int.zero;

        Vector2Int rev = -g.Dir;
        if (g.Dir != Vector2Int.zero && options.Count > 1) options.Remove(rev);
        if (options.Count == 0) options.Add(rev);
        if (stagePhase == StagePhase.GhostHunt)
        {
            List<Vector2Int> filteredOptions = new List<Vector2Int>(options.Count);
            for (int i = 0; i < options.Count; i++)
            {
                Vector2Int candidate = options[i];
                if (!IsGhostMoveBlockedByOtherGhost(g, g.Cell + candidate))
                {
                    filteredOptions.Add(candidate);
                }
            }

            if (filteredOptions.Count > 0)
            {
                options = filteredOptions;
            }
        }

        if (state == GhostState.Frightened || state == GhostState.Prey)
        {
            Vector2Int awayFrom = player.Cell;
            Vector2Int bestAway = options[0];
            float farthestDist = float.MinValue;
            for (int i = 0; i < Dirs.Length; i++)
            {
                Vector2Int d = Dirs[i];
                if (!options.Contains(d)) continue;
                float dist = ((Vector2)(g.Cell + d - awayFrom)).sqrMagnitude;
                if (dist > farthestDist)
                {
                    farthestDist = dist;
                    bestAway = d;
                }
            }

            return bestAway;
        }

        Vector2Int target = GhostTarget(g, state);
        Vector2Int best = options[0];
        float bestDist = float.MaxValue;
        for (int i = 0; i < Dirs.Length; i++)
        {
            Vector2Int d = Dirs[i];
            if (!options.Contains(d)) continue;
            float dist = ((Vector2)(g.Cell + d - target)).sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = d;
            }
        }

        return best;
    }

    private Vector2Int GhostTarget(Ghost g, GhostState state)
    {
        if (state == GhostState.Eaten) return g.Spawn;
        if (state == GhostState.Scatter) return g.ScatterTarget;

        Vector2Int pDir = player.Dir == Vector2Int.zero ? player.LastDir : player.Dir;
        Vector2Int pCell = player.Cell;
        if (g.Type == GhostType.Red) return pCell;
        if (g.Type == GhostType.Pink) return pCell + pDir * 4;
        if (g.Type == GhostType.Blue)
        {
            Vector2Int redCell = ghosts[0].Cell;
            Vector2Int a = pCell + pDir * 2;
            return a + (a - redCell);
        }

        if (((Vector2)(pCell - g.Cell)).magnitude > 8f) return pCell;
        return g.ScatterTarget;
    }

    private void PaintGhost(Ghost g, GhostState s)
    {
        Color c = g.NormalColor;
        if (s == GhostState.Guarded) c = Color.Lerp(g.NormalColor, GhostGuardedColor, 0.35f);
        if (s == GhostState.Frightened || s == GhostState.Prey) c = GhostPreyColor;
        if (s == GhostState.Eaten || s == GhostState.Captured) c = GhostCapturedColor;
        g.Body.sharedMaterial = GetMaterial(c);
        if (g.IndicatorRenderer != null) g.IndicatorRenderer.sharedMaterial = GetMaterial(Faint(c));
    }

    private void OnPlayerAtCellCenter(Actor a)
    {
        SnapToCell(a);
        EatPellet(a.Cell);

        if (CanMove(a.Cell, a.WantDir))
        {
            a.Dir = a.WantDir;
        }
        else if (!CanMove(a.Cell, a.Dir))
        {
            a.Dir = Vector2Int.zero;
        }
    }

    private void TryApplyPlayerPreTurn()
    {
        if (player == null || player.WantDir == Vector2Int.zero) return;
        if (player.Dir == Vector2Int.zero)
        {
            if (CanMove(player.Cell, player.WantDir))
            {
                SnapToCell(player);
                player.Dir = player.WantDir;
            }

            return;
        }

        if (player.WantDir == player.Dir) return;
        if (player.WantDir == -player.Dir) return;

        Vector2Int turnCell = player.Cell + player.Dir;
        if (!IsWalkable(turnCell) || !CanMove(turnCell, player.WantDir)) return;

        Vector3 turnWorld = CellToWorld(turnCell, player.Root.position.z);
        if ((player.Root.position - turnWorld).sqrMagnitude > PlayerPreTurnWindow * PlayerPreTurnWindow) return;

        player.Cell = turnCell;
        player.Root.position = turnWorld;
        player.Dir = player.WantDir;
    }

    private void OnGhostAtCellCenter(Actor a)
    {
        Ghost g = (Ghost)a;
        SnapToCell(g);
        if (g.Eaten)
        {
            g.Dir = Vector2Int.zero;
            return;
        }

        bool canForward = CanMove(g.Cell, g.Dir);
        int exits = CountWalkable(g.Cell);
        bool mustChoose = exits >= 3 || !canForward;
        if (!mustChoose && canForward)
        {
            return;
        }

        Vector2Int next = ChooseGhostDir(g, GetGhostState(g));
        if (next == Vector2Int.zero) next = FirstWalkableDir(g.Cell);
        g.Dir = next;
    }

    private bool IsGhostMoveBlockedByOtherGhost(Ghost self, Vector2Int targetCell)
    {
        for (int i = 0; i < ghosts.Count; i++)
        {
            Ghost other = ghosts[i];
            if (other == self || other.Captured || !other.Root.gameObject.activeSelf)
            {
                continue;
            }

            if (other.Cell == targetCell)
            {
                return true;
            }

            if (AtCenter(other) && other.Dir != Vector2Int.zero && other.Cell + other.Dir == targetCell)
            {
                return true;
            }
        }

        return false;
    }

    private void EatPellet(Vector2Int c)
    {
        if (stagePhase != StagePhase.PelletRun) return;

        Pellet p;
        if (!pellets.TryGetValue(c, out p)) return;

        pellets.Remove(c);
        Destroy(p.Go);
        score += p.Power ? 50 : 10;
        Play(pelletSfx);
        SpawnFx(pelletFxPrefab, CellToWorld(c, -0.1f));

        if (p.Power)
        {
            frightenedTimer = FrightenedDuration;
            chaseLoopUnlocked = true;
            Play(powerPelletSfx);
        }
    }
}

