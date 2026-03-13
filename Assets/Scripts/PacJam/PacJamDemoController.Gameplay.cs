using System.Collections.Generic;
using UnityEngine;

public partial class PacJamDemoController
{
    private void TickModeTimer(float dt)
    {
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

    private void ReleaseEatenGhostsAfterFrightened()
    {
        for (int i = 0; i < ghosts.Count; i++)
        {
            Ghost g = ghosts[i];
            if (!g.Eaten) continue;

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
            GhostState state = GetGhostState(g);

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

        TickModeTimer(dt);
        TickPlayer(dt);
        TickGhosts(dt);
        CheckCollisions();

        if (pellets.Count == 0) victory = true;
    }

    private void CheckCollisions()
    {
        if (respawnTimer > 0f) return;

        float hitSqr = CollisionDistance * CollisionDistance;
        for (int i = 0; i < ghosts.Count; i++)
        {
            Ghost g = ghosts[i];
            if ((g.Root.position - player.Root.position).sqrMagnitude > hitSqr) continue;

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
            else if (s != GhostState.Eaten)
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
        UpdateIndicator(player);

        for (int i = 0; i < ghosts.Count; i++)
        {
            Ghost g = ghosts[i];
            g.Cell = g.Spawn;
            g.Root.position = CellToWorld(g.Spawn, g.Root.position.z);
            g.Dir = g.Spawn.x < width / 2 ? Vector2Int.left : Vector2Int.right;
            g.Eaten = false;
            PaintGhost(g, GetGhostState(g));
            UpdateIndicator(g);
        }
    }

    private GhostState GetGhostState(Ghost g)
    {
        if (g.Eaten) return GhostState.Eaten;
        if (frightenedTimer > 0f) return GhostState.Frightened;
        return modePhase % 2 == 0 ? GhostState.Scatter : GhostState.Chase;
    }

    private float GhostSpeed(GhostState s)
    {
        if (s == GhostState.Scatter) return GhostScatterSpeed;
        if (s == GhostState.Chase) return GhostChaseSpeed;
        if (s == GhostState.Frightened) return GhostFrightenedSpeed;
        return GhostEatenSpeed;
    }

    private Vector2Int ChooseGhostDir(Ghost g, GhostState state)
    {
        List<Vector2Int> options = new List<Vector2Int>(4);
        for (int i = 0; i < Dirs.Length; i++) if (CanMove(g.Cell, Dirs[i])) options.Add(Dirs[i]);
        if (options.Count == 0) return Vector2Int.zero;

        Vector2Int rev = -g.Dir;
        if (g.Dir != Vector2Int.zero && options.Count > 1) options.Remove(rev);
        if (options.Count == 0) options.Add(rev);

        if (state == GhostState.Frightened)
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
        if (s == GhostState.Frightened) c = new Color(0.55f, 0.55f, 0.55f);
        if (s == GhostState.Eaten) c = new Color(0.88f, 0.88f, 0.88f);
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

    private void EatPellet(Vector2Int c)
    {
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
            Play(powerPelletSfx);
        }
    }
}
