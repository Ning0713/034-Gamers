using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class MariodemoController
{
    private void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            desiredHorizontal = 0f;
            jumpReleaseRequested = false;
            return;
        }

        bool left = keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed;
        bool right = keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed;
        desiredHorizontal = 0f;
        if (left && !right) desiredHorizontal = -1f;
        if (right && !left) desiredHorizontal = 1f;

        bool jumpPressed = keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame;
        bool jumpReleased = keyboard.upArrowKey.wasReleasedThisFrame || keyboard.wKey.wasReleasedThisFrame || keyboard.spaceKey.wasReleasedThisFrame;
        if (jumpPressed)
        {
            jumpBufferTimer = JumpBufferTime;
        }

        if (jumpReleased)
        {
            jumpReleaseRequested = true;
        }
    }

    private void TickTimers(float dt)
    {
        if (coyoteTimer > 0f) coyoteTimer -= dt;
        if (jumpBufferTimer > 0f) jumpBufferTimer -= dt;
        if (invulnerabilityTimer > 0f) invulnerabilityTimer -= dt;
        if (headHitCooldownTimer > 0f) headHitCooldownTimer -= dt;
    }

    private float GetPlayerRadius()
    {
        return PlayerRadius * (playerPoweredUp ? PlayerPoweredScale : 1f);
    }

    private float GetEnemyRadius(EnemyData enemy)
    {
        return EnemyRadius * (enemy != null && enemy.PoweredUp ? EnemyPoweredScale : 1f);
    }

    private void ApplyPlayerPowerState()
    {
        if (playerCollider != null)
        {
            playerCollider.radius = GetPlayerRadius();
        }

        UpdatePlayerVisual();
    }

    private void SetPlayerPoweredUp(bool powered)
    {
        if (playerPoweredUp == powered)
        {
            return;
        }

        float oldRadius = GetPlayerRadius();
        playerPoweredUp = powered;
        float newRadius = GetPlayerRadius();
        if (playerBody != null)
        {
            playerBody.position += Vector2.up * Mathf.Max(0f, newRadius - oldRadius);
        }

        ApplyPlayerPowerState();
    }

    private void SetEnemyPoweredUp(EnemyData enemy, bool powered)
    {
        if (enemy == null || enemy.PoweredUp == powered)
        {
            return;
        }

        float oldRadius = GetEnemyRadius(enemy);
        enemy.PoweredUp = powered;
        float newRadius = GetEnemyRadius(enemy);
        enemy.Position = new Vector2(enemy.Position.x, enemy.Position.y + Mathf.Max(0f, newRadius - oldRadius));
        UpdateEnemyPresentation(enemy);
        if (enemy.Root != null)
        {
            enemy.Root.position = new Vector3(enemy.Position.x, enemy.Position.y, enemy.Root.position.z);
        }
    }

    private void UpdateEnemyPresentation(EnemyData enemy)
    {
        if (enemy == null || enemy.OutlineRenderer == null || enemy.FillRenderer == null)
        {
            return;
        }

        float scale = enemy.PoweredUp ? EnemyPoweredScale : 1f;
        enemy.OutlineRenderer.transform.localScale = new Vector3(0.76f * scale, 0.76f * scale, 1f);
        enemy.FillRenderer.transform.localScale = new Vector3(0.62f * scale, 0.62f * scale, 1f);
    }

    private bool IsPlayerGrounded()
    {
        if (playerRoot == null) return false;
        float playerRadius = GetPlayerRadius();
        Vector2 feet = (Vector2)playerRoot.position + Vector2.down * (playerRadius + 0.09f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(feet, 0.12f);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit == playerCollider) continue;
            if (IsSolidCollider(hit)) return true;
        }

        return false;
    }

    private void TickPlayerHeadHit()
    {
        if (playerBody == null || playerBody.velocity.y <= 0.1f || headHitCooldownTimer > 0f)
        {
            return;
        }

        float playerRadius = GetPlayerRadius();
        Vector2 head = (Vector2)playerRoot.position + Vector2.up * (playerRadius + 0.16f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(head, 0.14f);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!blockLookup.TryGetValue(hits[i], out BlockData block)) continue;
            if (block.Type != TileType.Brick && block.Type != TileType.Question) continue;

            headHitCooldownTimer = HeadHitCooldown;
            if (block.HiddenUntilHit)
            {
                block.HiddenUntilHit = false;
                SetBlockVisualsVisible(block, true);
            }

            Play(blockHitSfx);
            if (block.Type == TileType.Question && !block.Used)
            {
                block.Used = true;
                if (block.MainRenderer != null)
                {
                    block.MainRenderer.sharedMaterial = GetMaterial(QuestionUsedColor);
                }

                if (block.SpawnsEnemy)
                {
                    Play(questionBlockSfx);
                    Vector2 spawn = new Vector2(block.Cell.x + 0.5f, block.Cell.y + 0.95f);
                    AddEnemy(spawn, 1, block.PartId, false, true, true, EnemyLaunchSpeed);
                }
                else if (block.SpawnsMushroom)
                {
                    Play(questionBlockSfx);
                    SpawnRuntimeMushroom(new Vector2(block.Cell.x + 0.5f, block.Cell.y + 1.18f), block.PartId);
                }
                else
                {
                    SpawnRuntimeCoin(new Vector2(block.Cell.x + 0.5f, block.Cell.y + 0.95f));
                    Play(coinPopupSfx);
                }
            }

            break;
        }
    }
    private void TickRuntimeCoins(float dt)
    {
        for (int i = runtimeCoins.Count - 1; i >= 0; i--)
        {
            RuntimeCoinData coin = runtimeCoins[i];
            if (coin.Root == null)
            {
                runtimeCoins.RemoveAt(i);
                continue;
            }

            coin.Elapsed += dt;
            float t = Mathf.Clamp01(coin.Elapsed / CoinRiseDuration);
            float rise = Mathf.Lerp(0f, CoinRiseHeight, 1f - (1f - t) * (1f - t));
            coin.Root.position = new Vector3(coin.Origin.x, coin.Origin.y + rise, coin.Root.position.z);

            if (coin.Elapsed < CoinRiseDuration)
            {
                continue;
            }

            Destroy(coin.Root.gameObject);
            runtimeCoins.RemoveAt(i);
        }
    }

    private void TickRuntimeMushrooms(float dt)
    {
        for (int i = runtimeMushrooms.Count - 1; i >= 0; i--)
        {
            RuntimeMushroomData mushroom = runtimeMushrooms[i];
            if (mushroom.Root == null || !mushroom.Active)
            {
                if (mushroom.Root != null)
                {
                    Destroy(mushroom.Root.gameObject);
                }

                runtimeMushrooms.RemoveAt(i);
                continue;
            }

            if (mushroom.Emergence < MushroomRiseDuration)
            {
                mushroom.Emergence += dt;
                float t = Mathf.Clamp01(mushroom.Emergence / MushroomRiseDuration);
                float rise = Mathf.Lerp(-MushroomRiseHeight, 0f, 1f - (1f - t) * (1f - t));
                mushroom.Position = new Vector2(mushroom.SpawnOrigin.x, mushroom.SpawnOrigin.y + rise);
                mushroom.VerticalVelocity = 0f;
            }
            else
            {
                if (mushroom.Direction == 0)
                {
                    mushroom.Direction = 1;
                }

                Vector2 position = mushroom.Position;
                Vector2 sideProbe = position + new Vector2(mushroom.Direction * (MushroomRadius + 0.14f), 0.08f);
                if (IsSolidAtCircle(sideProbe, 0.10f))
                {
                    mushroom.Direction *= -1;
                }

                position.x += mushroom.Direction * MushroomMoveSpeed * dt;
                mushroom.VerticalVelocity -= MushroomGravity * dt;
                position.y += mushroom.VerticalVelocity * dt;

                if (TryGetSurfaceY(position + Vector2.up * 0.85f, out float surfaceY))
                {
                    float groundedY = surfaceY + MushroomRadius;
                    if (position.y <= groundedY + 0.02f && mushroom.VerticalVelocity <= 0f)
                    {
                        position.y = groundedY;
                        mushroom.VerticalVelocity = 0f;
                    }
                }

                mushroom.Position = position;
            }

            mushroom.Root.position = new Vector3(mushroom.Position.x, mushroom.Position.y, mushroom.Root.position.z);
            if (mushroom.Position.y < -4f || mushroom.Position.x < -2f || mushroom.Position.x > LevelWidth + 2f)
            {
                Destroy(mushroom.Root.gameObject);
                runtimeMushrooms.RemoveAt(i);
            }
        }
    }
    private void CheckMushroomPickups()
    {
        if (runtimeMushrooms.Count == 0)
        {
            return;
        }

        Vector2 playerPosition = playerBody != null ? playerBody.position : Vector2.zero;
        float playerRadius = GetPlayerRadius();
        for (int i = runtimeMushrooms.Count - 1; i >= 0; i--)
        {
            RuntimeMushroomData mushroom = runtimeMushrooms[i];
            if (mushroom.Root == null)
            {
                runtimeMushrooms.RemoveAt(i);
                continue;
            }

            bool consumed = false;
            if (playerBody != null)
            {
                float playerCombined = playerRadius + MushroomRadius - 0.02f;
                if ((mushroom.Position - playerPosition).sqrMagnitude <= playerCombined * playerCombined)
                {
                    SetPlayerPoweredUp(true);
                    consumed = true;
                }
            }

            if (!consumed)
            {
                for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
                {
                    EnemyData enemy = enemies[enemyIndex];
                    if (!enemy.Alive)
                    {
                        continue;
                    }

                    float enemyCombined = GetEnemyRadius(enemy) + MushroomRadius - 0.02f;
                    if ((mushroom.Position - enemy.Position).sqrMagnitude > enemyCombined * enemyCombined)
                    {
                        continue;
                    }

                    SetEnemyPoweredUp(enemy, true);
                    consumed = true;
                    break;
                }
            }

            if (!consumed)
            {
                continue;
            }

            Play(mushroomPowerUpSfx != null ? mushroomPowerUpSfx : questionBlockSfx);
            Destroy(mushroom.Root.gameObject);
            runtimeMushrooms.RemoveAt(i);
        }
    }

    private void TickStartSecretWall(float dt)
    {
        if (startSecretWall == null || startSecretWall.Opened || playerBody == null)
        {
            return;
        }

        if (startSecretWall.ComboTimer > 0f)
        {
            startSecretWall.ComboTimer = Mathf.Max(0f, startSecretWall.ComboTimer - dt);
            if (startSecretWall.ComboTimer <= 0f)
            {
                startSecretWall.HitCount = 0;
            }
        }

        bool insideHitZone = CircleIntersectsRect(playerBody.position, GetPlayerRadius(), startSecretWall.Trigger);
        bool touchingWall = false;
        if (insideHitZone && playerCollider != null && startSecretWall.Collider != null)
        {
            ColliderDistance2D wallDistance = playerCollider.Distance(startSecretWall.Collider);
            touchingWall = wallDistance.isOverlapped || wallDistance.distance <= 0.03f;
        }

        bool pressingIntoSecretWall = touchingWall && desiredHorizontal < -0.20f;
        if (pressingIntoSecretWall)
        {
            if (!startSecretWall.ContactLatch)
            {
                startSecretWall.ContactLatch = true;
                startSecretWall.HitCount = startSecretWall.ComboTimer > 0f ? startSecretWall.HitCount + 1 : 1;
                startSecretWall.ComboTimer = SecretWallComboWindow;
                Play(blockHitSfx);
                playerBody.position += Vector2.right * 0.05f;
                playerBody.velocity = new Vector2(0f, playerBody.velocity.y);
                if (startSecretWall.HitCount >= 3)
                {
                    OpenStartSecretWall();
                }
            }

            return;
        }

        if (!touchingWall || desiredHorizontal >= -0.05f)
        {
            startSecretWall.ContactLatch = false;
        }
    }

    private void OpenStartSecretWall()
    {
        if (startSecretWall == null || startSecretWall.Opened)
        {
            return;
        }

        startSecretWall.Opened = true;
        startSecretWall.HitCount = 0;
        startSecretWall.ComboTimer = 0f;
        startSecretWall.ContactLatch = false;
        if (startSecretWall.Collider != null)
        {
            startSecretWall.Collider.enabled = false;
        }

        MariodemoProgressState.SetStartSecretWallOpened(true);
        Physics2D.SyncTransforms();
        Play(secretWallOpenSfx != null ? secretWallOpenSfx : (questionBlockSfx != null ? questionBlockSfx : blockHitSfx));
    }

    private void CheckSecretDiamondPickup()
    {
        if (hiddenSecretDiamond == null || hiddenSecretDiamond.Root == null || playerBody == null || MariodemoProgressState.HasSecretDiamond)
        {
            return;
        }

        float combined = GetPlayerRadius() + 0.46f;
        if ((hiddenSecretDiamond.Position - playerBody.position).sqrMagnitude > combined * combined)
        {
            return;
        }

        MariodemoProgressState.CollectSecretDiamond();
        Play(secretDiamondPickupSfx != null ? secretDiamondPickupSfx : coinPopupSfx);
        Destroy(hiddenSecretDiamond.Root.gameObject);
        hiddenSecretDiamond = null;
    }

    private void TickHeavyBreakableBlocks()
    {
        bool brokeAny = false;
        if (playerPoweredUp && playerBody != null)
        {
            brokeAny |= TryBreakHeavyBlocksAt(playerBody.position, GetPlayerRadius());
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyData enemy = enemies[i];
            if (enemy == null || !enemy.Alive || !enemy.PoweredUp)
            {
                continue;
            }

            brokeAny |= TryBreakHeavyBlocksAt(enemy.Position, GetEnemyRadius(enemy));
        }

        if (brokeAny)
        {
            Physics2D.SyncTransforms();
        }
    }

    private bool TryBreakHeavyBlocksAt(Vector2 center, float radius)
    {
        Vector2 feetCenter = center + Vector2.down * Mathf.Max(0.10f, radius - 0.05f);
        float probeRadius = Mathf.Max(0.20f, radius * 0.46f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(feetCenter, probeRadius);
        var blocksToBreak = new System.Collections.Generic.List<BlockData>();
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D collider = hits[i];
            if (collider == null || collider == playerCollider)
            {
                continue;
            }

            if (!blockLookup.TryGetValue(collider, out BlockData block) || !block.BreaksUnderHeavy)
            {
                continue;
            }

            if (blocksToBreak.Contains(block))
            {
                continue;
            }

            blocksToBreak.Add(block);
        }

        if (blocksToBreak.Count == 0)
        {
            return false;
        }

        Play(heavyBreakSfx != null ? heavyBreakSfx : blockHitSfx);
        for (int i = 0; i < blocksToBreak.Count; i++)
        {
            StartBrokenBlockFall(blocksToBreak[i]);
        }

        return true;
    }

    private void TickBrokenBlocks(float dt)
    {
        for (int i = fallingBrokenBlocks.Count - 1; i >= 0; i--)
        {
            FallingBrokenBlockData block = fallingBrokenBlocks[i];
            if (block.Root == null)
            {
                fallingBrokenBlocks.RemoveAt(i);
                continue;
            }

            block.Velocity.y -= BrokenBlockGravity * dt;
            block.Position += block.Velocity * dt;
            block.Root.position = new Vector3(block.Position.x, block.Position.y, block.Root.position.z);
            block.Root.Rotate(0f, 0f, block.AngularVelocity * dt, Space.Self);
            if (block.Position.y < -5f)
            {
                Destroy(block.Root.gameObject);
                fallingBrokenBlocks.RemoveAt(i);
            }
        }
    }
    private void CheckFlagPoleCollision()
    {
        if (goalFlagPole == null || goalFlagPole.Activated || playerBody == null)
        {
            return;
        }

        if (!CircleIntersectsRect(playerBody.position, GetPlayerRadius(), goalFlagPole.Trigger))
        {
            return;
        }

        goalFlagPole.Activated = true;
        PlayEvent(flagPoleSfx);
        if (goalFlagPole.FlagRenderer != null)
        {
            goalFlagPole.FlagRenderer.sharedMaterial = GetMaterial(FlagClothActiveColor);
            goalFlagPole.FlagRenderer.transform.localPosition = new Vector3(0.26f, 0.52f, -0.01f);
        }
    }

    private void TickStageClearSequence(float dt)
    {
        stageClearTimer = Mathf.Max(0f, stageClearTimer - dt);
        if (playerRoot == null)
        {
            return;
        }

        stageClearEntryTimer += dt;
        float t = Mathf.Clamp01(stageClearEntryTimer / GoalEntryDuration);
        float eased = 1f - (1f - t) * (1f - t);
        Vector3 target = new Vector3(goalDoorEntryTarget.x, goalDoorEntryTarget.y, stageClearStartPosition.z);
        Vector3 position = Vector3.Lerp(stageClearStartPosition, target, eased);
        playerRoot.position = position;
        if (playerBody != null)
        {
            playerBody.position = new Vector2(position.x, position.y);
        }

        if (!stageClearCuePlayed && stageClearEntryTimer >= stageClearCueDelay)
        {
            stageClearCuePlayed = true;
            PlayEvent(stageClearSfx);
        }
    }
    private void TickFallingBrickTraps(float dt)
    {
        if (playerBody == null || fallingBrickTraps.Count == 0)
        {
            return;
        }

        Vector2 playerPosition = playerBody.position;
        bool movedTrap = false;

        for (int i = fallingBrickTraps.Count - 1; i >= 0; i--)
        {
            FallingBrickTrapData trap = fallingBrickTraps[i];
            if (trap.Block == null || trap.Block.Root == null)
            {
                fallingBrickTraps.RemoveAt(i);
                continue;
            }

            if (trap.Armed && !trap.Falling && ShouldTriggerFallingBrickTrap(trap, playerPosition))
            {
                trap.Armed = false;
                trap.Falling = true;
                trap.VerticalVelocity = -1.2f;
                Play(fallingBrickDropSfx);
            }

            if (!trap.Falling)
            {
                continue;
            }

            trap.VerticalVelocity -= FallingBrickGravity * dt;
            Vector2 position = trap.Position;
            position.y += trap.VerticalVelocity * dt;
            TryLandFallingBrickTrap(trap, ref position);

            trap.Position = position;
            trap.Block.Root.transform.position = new Vector3(position.x, position.y, trap.Block.Root.transform.position.z);
            movedTrap = true;

            Rect trapRect = new Rect(position.x - FallingBrickHalfSize, position.y - FallingBrickHalfSize, FallingBrickHalfSize * 2f, FallingBrickHalfSize * 2f);
            if (CircleIntersectsRect(playerPosition, GetPlayerRadius(), trapRect))
            {
                Physics2D.SyncTransforms();
                KillPlayer();
                return;
            }

            if (position.y < -4f)
            {
                if (trap.Block.Collider != null)
                {
                    blockLookup.Remove(trap.Block.Collider);
                }

                blocks.Remove(trap.Block.Cell);
                Destroy(trap.Block.Root.gameObject);
                fallingBrickTraps.RemoveAt(i);
            }
        }

        if (movedTrap)
        {
            Physics2D.SyncTransforms();
        }
    }

    private bool ShouldTriggerFallingBrickTrap(FallingBrickTrapData trap, Vector2 playerPosition)
    {
        if (playerPosition.y >= trap.Position.y - 0.30f)
        {
            return false;
        }

        return Mathf.Abs(playerPosition.x - trap.Position.x) <= FallingBrickTriggerHalfWidth;
    }

    private bool TryLandFallingBrickTrap(FallingBrickTrapData trap, ref Vector2 position)
    {
        if (trap.VerticalVelocity > 0f || trap.Block == null)
        {
            return false;
        }

        if (!TryGetSurfaceYIgnoring(position + Vector2.up * 0.70f, trap.Block.Collider, out float surfaceY))
        {
            return false;
        }

        float landedY = surfaceY + FallingBrickHalfSize;
        if (position.y > landedY + 0.02f)
        {
            return false;
        }

        position.y = landedY;
        trap.Falling = false;
        trap.VerticalVelocity = 0f;
        return true;
    }

    private void TickEnemies(float dt)
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyData enemy = enemies[i];
            if (!enemy.Alive)
            {
                if (enemy.Root != null) Destroy(enemy.Root.gameObject);
                enemies.RemoveAt(i);
                continue;
            }

            Vector2 position = enemy.Position;
            TickEnemyVerticalMotion(enemy, ref position, dt);
            if (!enemy.Alive)
            {
                continue;
            }

            float step = enemy.Direction * EnemySpeed * dt;
            float nextX = position.x + step;
            bool outsidePatrol = enemy.UsesPatrolRange && (nextX < enemy.PatrolMinX || nextX > enemy.PatrolMaxX);
            bool blockedAhead = IsEnemyBlockedAhead(enemy, position, enemy.Direction);
            if (outsidePatrol || blockedAhead)
            {
                enemy.Direction *= -1;
                step = enemy.Direction * EnemySpeed * dt;
                nextX = position.x + step;
            }

            if (CanEnemyAdvance(enemy, position, nextX))
            {
                position.x = nextX;
            }

            enemy.Position = position;
            enemy.Root.position = new Vector3(position.x, position.y, enemy.Root.position.z);
        }
    }
    private bool IsEnemyBlockedAhead(EnemyData enemy, Vector2 position, int direction)
    {
        float radius = GetEnemyRadius(enemy);
        Vector2 probe = position + new Vector2(direction * (radius + 0.18f), Mathf.Max(0.12f, radius * 0.55f));
        return IsSolidAtCircle(probe, 0.14f);
    }
    private void TickEnemyVerticalMotion(EnemyData enemy, ref Vector2 position, float dt)
    {
        if (enemy.UseGravity)
        {
            enemy.VerticalVelocity -= EnemyGravity * dt;
            position.y += enemy.VerticalVelocity * dt;
            if (TryLandEnemy(enemy, ref position))
            {
                return;
            }

            if (position.y < -4f)
            {
                enemy.Alive = false;
            }
            return;
        }

        if (TryGetSurfaceY(position + Vector2.up * 1.0f, out float surfaceY))
        {
            position.y = surfaceY + GetEnemyRadius(enemy);
            TryLockEnemyToWideGroundPatrol(enemy, position);
            return;
        }

        enemy.UseGravity = true;
        enemy.VerticalVelocity = -EnemyFallSpeed;
        position.y += enemy.VerticalVelocity * dt;
        if (TryLandEnemy(enemy, ref position))
        {
            return;
        }

        if (position.y < -4f)
        {
            enemy.Alive = false;
        }
    }

    private bool TryLandEnemy(EnemyData enemy, ref Vector2 position)
    {
        if (enemy.VerticalVelocity > 0f)
        {
            return false;
        }

        if (!TryGetSurfaceY(position + Vector2.up * 1.0f, out float surfaceY))
        {
            return false;
        }

        float groundedY = surfaceY + GetEnemyRadius(enemy);
        if (position.y > groundedY + 0.02f)
        {
            return false;
        }

        position.y = groundedY;
        enemy.UseGravity = false;
        enemy.VerticalVelocity = 0f;
        TryLockEnemyToWideGroundPatrol(enemy, position);
        return true;
    }

    private void TryLockEnemyToWideGroundPatrol(EnemyData enemy, Vector2 position)
    {
        if (!enemy.LockPatrolOnWideGround || enemy.UsesPatrolRange)
        {
            return;
        }

        ResolveEnemyPatrolRange(position, out float patrolMinX, out float patrolMaxX);
        if (patrolMaxX - patrolMinX < WideGroundPatrolThreshold)
        {
            return;
        }

        enemy.PatrolMinX = patrolMinX;
        enemy.PatrolMaxX = patrolMaxX;
        enemy.UsesPatrolRange = true;
        enemy.LockPatrolOnWideGround = false;
    }

    private bool CanEnemyAdvance(EnemyData enemy, Vector2 position, float nextX)
    {
        if (enemy.UsesPatrolRange && (nextX < enemy.PatrolMinX || nextX > enemy.PatrolMaxX))
        {
            return false;
        }

        return !IsEnemyBlockedAhead(enemy, position, enemy.Direction);
    }

    private void TickPipeSpawners(float dt)
    {
        if (playerRoot == null)
        {
            return;
        }

        float playerX = playerRoot.position.x;
        float playerVelocityX = playerBody != null ? playerBody.velocity.x : desiredHorizontal * MoveSpeed;
        if (Mathf.Abs(playerVelocityX) < PipeTriggerMinApproachSpeed)
        {
            playerVelocityX = desiredHorizontal * MoveSpeed;
        }

        for (int i = 0; i < pipeSpawners.Count; i++)
        {
            PipeSpawnerData spawner = pipeSpawners[i];
            if (spawner.CooldownTimer > 0f)
            {
                spawner.CooldownTimer -= dt;
            }

            float offsetX = playerX - spawner.Origin.x;
            if (Mathf.Abs(offsetX) >= PipeTriggerRearmDistance)
            {
                spawner.Armed = true;
                continue;
            }

            if (!spawner.Armed || spawner.CooldownTimer > 0f)
            {
                continue;
            }

            if (!ShouldPipeSpawnerFire(offsetX, playerVelocityX))
            {
                continue;
            }

            spawner.CooldownTimer = spawner.Cooldown;
            spawner.Armed = false;
            SpawnBullet(spawner.Origin, spawner.Direction, spawner.PartId, spawner.SpeedMultiplier);
            Play(bulletFireSfx);
        }
    }

    private bool ShouldPipeSpawnerFire(float offsetX, float playerVelocityX)
    {
        if (Mathf.Abs(offsetX) > PipeTriggerApproachDistance)
        {
            return false;
        }

        if (Mathf.Abs(playerVelocityX) < PipeTriggerMinApproachSpeed)
        {
            return false;
        }

        if (playerVelocityX > 0f && offsetX > 0f)
        {
            return false;
        }

        if (playerVelocityX < 0f && offsetX < 0f)
        {
            return false;
        }

        float projectedOffsetX = offsetX + playerVelocityX * PipeTriggerProjectionLead;
        return Mathf.Abs(projectedOffsetX) <= 0.18f || Mathf.Sign(offsetX) != Mathf.Sign(projectedOffsetX);
    }

    private void TickBullets(float dt)
    {
        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            BulletData bullet = bullets[i];
            if (!bullet.Active)
            {
                if (bullet.Root != null) Destroy(bullet.Root.gameObject);
                bullets.RemoveAt(i);
                continue;
            }

            bullet.Position += bullet.Velocity * dt;
            if (bullet.Position.x < -2f || bullet.Position.x > LevelWidth + 2f || bullet.Position.y < -4f || bullet.Position.y > CameraY + CameraSize + 4f || IsSolidAtCircle(bullet.Position, BulletRadius))
            {
                bullet.Active = false;
                continue;
            }

            bullet.Root.position = new Vector3(bullet.Position.x, bullet.Position.y, bullet.Root.position.z);
        }
    }

    private void CheckEnemyPlayerCollisions()
    {
        if (playerBody == null || invulnerabilityTimer > 0f) return;

        Vector2 playerPos = playerBody.position;
        float playerRadius = GetPlayerRadius();
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyData enemy = enemies[i];
            if (!enemy.Alive) continue;

            float combined = playerRadius + GetEnemyRadius(enemy) - 0.04f;
            if ((enemy.Position - playerPos).sqrMagnitude > combined * combined) continue;

            if (playerBody.velocity.y < -1.2f && playerPos.y > enemy.Position.y + 0.16f)
            {
                enemy.Alive = false;
                Vector2 bounce = playerBody.velocity;
                bounce.y = JumpSpeed * 0.48f;
                playerBody.velocity = bounce;
                Play(stompSfx);
                continue;
            }

            KillPlayer();
            return;
        }
    }
    private void CheckBulletPlayerCollisions()
    {
        if (playerBody == null || invulnerabilityTimer > 0f) return;

        Vector2 playerPos = playerBody.position;
        float combined = GetPlayerRadius() + BulletRadius;
        for (int i = 0; i < bullets.Count; i++)
        {
            BulletData bullet = bullets[i];
            if (!bullet.Active) continue;
            if ((bullet.Position - playerPos).sqrMagnitude > combined * combined) continue;

            KillPlayer();
            return;
        }
    }
    private void CheckSpikePlayerCollisions()
    {
        // Grass is decorative in Mariodemo for now.
    }

    private void CheckGoalCollision()
    {
        if (playerBody == null || goalFlagPole == null || !goalFlagPole.Activated) return;

        Rect goalRect = new Rect(GoalColumn + 0.02f, FloorTop, 1.00f, 2.20f);
        if (CircleIntersectsRect(playerBody.position, GetPlayerRadius(), goalRect))
        {
            PlayEvent(goalEnterSfx);
            CompleteStage();
        }
    }
    private void CheckFallDeath()
    {
        if (playerBody != null && playerBody.position.y < -3.5f)
        {
            KillPlayer();
        }
    }

    private void KillPlayer()
    {
        if (gameOver || stageClear || respawnPending) return;

        lives = Mathf.Max(0, lives - 1);
        MariodemoProgressState.SetLives(lives);
        desiredHorizontal = 0f;
        StopLoopSource(sfxSource);
        StopLoopSource(bgmSource);
        if (playerBody != null)
        {
            playerBody.velocity = Vector2.zero;
            playerBody.simulated = false;
        }

        if (lives <= 0)
        {
            PlayEvent(gameOverSfx != null ? gameOverSfx : deathSfx);
            gameOver = true;
            return;
        }

        respawnPending = true;
        respawnTimer = Mathf.Max(0.15f, deathSfx != null ? deathSfx.length : 0.75f);
        PlayEvent(deathSfx);
    }

    private void CompleteStage()
    {
        if (stageClear) return;

        stageClear = true;
        stageClearEntryTimer = 0f;
        stageClearCuePlayed = false;
        stageClearCueDelay = Mathf.Max(GoalEntryDuration + VictoryCueGap, goalEnterSfx != null ? goalEnterSfx.length + VictoryCueGap : GoalEntryDuration + VictoryCueGap);
        stageClearTimer = Mathf.Max(StageClearDelay, stageClearCueDelay + (stageClearSfx != null ? stageClearSfx.length : 0f) + 0.10f);
        stageClearStartPosition = playerRoot != null ? playerRoot.position : Vector3.zero;
        StopLoopSource(bgmSource);
        desiredHorizontal = 0f;
        if (playerBody != null)
        {
            playerBody.velocity = Vector2.zero;
            playerBody.simulated = false;
        }
    }
    private void RestartStage()
    {
        lives = MariodemoProgressState.InitialLives;
        MariodemoProgressState.ResetProgress();
        BuildStageRuntime();
    }

    private void UpdateCameraFollow()
    {
        SetupCamera();
        if (mainCamera == null || playerRoot == null) return;

        float halfWidth = CameraSize * mainCamera.aspect;
        float hiddenStartMinX = halfWidth - HiddenStartLeftExtent;
        bool revealHiddenStart = MariodemoProgressState.StartSecretWallOpened && playerRoot.position.x < HiddenStartCameraTriggerX;
        float minX = revealHiddenStart ? hiddenStartMinX : halfWidth;
        float maxX = Mathf.Max(minX, LevelWidth - halfWidth);
        float followX = Mathf.Clamp(playerRoot.position.x, minX, maxX);
        mainCamera.transform.position = new Vector3(followX, CameraY, -20f);
    }

    private bool TryGetSurfaceY(Vector2 origin, out float y)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 12f);
        float best = float.NegativeInfinity;
        bool found = false;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D collider = hits[i].collider;
            if (collider == null || !IsSolidCollider(collider)) continue;
            if (!found || hits[i].point.y > best)
            {
                best = hits[i].point.y;
                found = true;
            }
        }

        y = best;
        return found;
    }

    private bool TryGetSurfaceYIgnoring(Vector2 origin, Collider2D ignoredCollider, out float y)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 12f);
        float best = float.NegativeInfinity;
        bool found = false;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D collider = hits[i].collider;
            if (collider == null || collider == ignoredCollider || !IsSolidCollider(collider)) continue;
            if (!found || hits[i].point.y > best)
            {
                best = hits[i].point.y;
                found = true;
            }
        }

        y = best;
        return found;
    }

    private bool IsSolidAtCircle(Vector2 center, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit == playerCollider) continue;
            if (IsSolidCollider(hit)) return true;
        }

        return false;
    }

    private bool IsSolidCollider(Collider2D collider)
    {
        return collider != null && blockLookup.TryGetValue(collider, out BlockData block) && block.Type != TileType.Goal;
    }

    private void UpdatePlayerVisual()
    {
        if (playerRenderer == null || playerRoot == null) return;

        bool visible = stageClear || invulnerabilityTimer <= 0f || Mathf.FloorToInt(invulnerabilityTimer * 12f) % 2 == 0;
        playerRenderer.enabled = visible;

        float scale = playerPoweredUp ? PlayerPoweredScale : 1f;
        if (stageClear)
        {
            float t = Mathf.Clamp01(stageClearEntryTimer / GoalEntryDuration);
            scale *= Mathf.Lerp(1f, 0.22f, t);
        }

        playerRenderer.transform.localScale = new Vector3(0.70f * scale, 0.70f * scale, 1f);
    }
    private void EnsureAudioSources()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(sfxSource, false, 0.95f);
        }

        if (eventSfxSource == null)
        {
            eventSfxSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(eventSfxSource, false, 1f);
        }

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(bgmSource, true, 0.52f);
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

    private void PlayEvent(AudioClip clip)
    {
        if (clip == null) return;
        EnsureAudioSources();
        if (eventSfxSource.isPlaying)
        {
            eventSfxSource.Stop();
        }
        eventSfxSource.PlayOneShot(clip);
    }

    private void UpdateLoopingAudio()
    {
        EnsureAudioSources();
        SyncLoopSource(bgmSource, !showQuitConfirm && !gameOver && !stageClear ? stageBgm : null);
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

    private void StopManagedAudio()
    {
        StopLoopSource(sfxSource);
        StopLoopSource(eventSfxSource);
        StopLoopSource(bgmSource);
    }

    private static void StopLoopSource(AudioSource source)
    {
        if (source == null) return;
        if (source.isPlaying) source.Stop();
        source.clip = null;
    }

    private void LoadForestdemoTransition()
    {
        ForestdemoProgressState.BeginFromMariodemo(lives);
        SceneManager.LoadScene(ForestdemoProgressState.CutsceneSceneName);
    }

    private void ReturnToTitleMenu()
    {
        SceneManager.LoadScene(StartMenuSceneName);
    }
}











