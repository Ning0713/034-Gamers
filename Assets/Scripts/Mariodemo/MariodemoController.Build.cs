using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class MariodemoController
{
    private void BuildStageRuntime()
    {
        ResetRuntimeObjects();
        SetupCamera();
        EnsureAudioSources();

        gameOver = false;
        stageClear = false;
        stageClearTimer = 0f;
        stageClearEntryTimer = 0f;
        stageClearCuePlayed = false;
        stageClearCueDelay = 0f;
        showQuitConfirm = false;
        respawnPending = false;
        respawnTimer = 0f;
        desiredHorizontal = 0f;
        playerPoweredUp = false;
        nextBreakClusterId = 1;
        goalFlagPole = null;
        goalDoorEntryTarget = new Vector2(GoalColumn + 0.50f, FloorTop + 1.04f);
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        jumpReleaseRequested = false;
        invulnerabilityTimer = RespawnInvulnerability;
        headHitCooldownTimer = 0f;

        stageRoot = new GameObject("MariodemoRoot").transform;
        stageRoot.SetParent(transform, false);
        environmentRoot = new GameObject("Environment").transform;
        environmentRoot.SetParent(stageRoot, false);
        actorRoot = new GameObject("Actors").transform;
        actorRoot.SetParent(stageRoot, false);
        hazardRoot = new GameObject("Hazards").transform;
        hazardRoot.SetParent(stageRoot, false);

        BuildWorld();
        CreatePlayer();
        MariodemoProgressState.SetLives(lives);
        UpdateLoopingAudio();
        UpdatePlayerVisual();
    }

    private void ResetRuntimeObjects()
    {
        StopLoopSource(bgmSource);

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        blocks.Clear();
        blockLookup.Clear();
        enemies.Clear();
        bullets.Clear();
        pipeSpawners.Clear();
        fallingBrickTraps.Clear();
        spikes.Clear();
        runtimeCoins.Clear();
        runtimeMushrooms.Clear();
        fallingBrokenBlocks.Clear();
        materialCache.Clear();
        playerRoot = null;
        playerRenderer = null;
        playerBody = null;
        playerCollider = null;
        startSecretWall = null;
        hiddenSecretDiamond = null;
    }

    private void BuildWorld()
    {
        BuildStartLane();
        BuildBrickIntro();
        BuildFirstPipeTrap();
        BuildUpperTrapRun();
        BuildBulletPipeRun();
        BuildDoubleGapTrap();
        BuildGoalLane();
    }

    private void BuildStartLane()
    {
        AddGroundRange(0, 12, (int)MapPartId.StartLane);
        for (int x = -4; x <= -1; x++)
        {
            AddGroundTile(x, 1, (int)MapPartId.StartLane);
            AddGroundTile(x, 2, (int)MapPartId.StartLane);
        }

        AddStartSecretWall();
        if (!MariodemoProgressState.HasSecretDiamond)
        {
            SpawnSecretDiamond(new Vector2(-2.50f, FloorTop + 1.70f));
        }

        AddEnemy(new Vector2(8.8f, FloorTop + EnemyRadius + 0.03f), -1, (int)MapPartId.StartLane);
    }
    private void BuildBrickIntro()
    {
        AddGroundRange(13, 22, (int)MapPartId.BrickIntro);
        AddBrickTile(15, 5, (int)MapPartId.BrickIntro);
        AddQuestionTile(16, 5, (int)MapPartId.BrickIntro, false);
        AddBrickTile(17, 5, (int)MapPartId.BrickIntro);
        AddQuestionTile(18, 5, (int)MapPartId.BrickIntro, true);
        AddBrickTile(19, 5, (int)MapPartId.BrickIntro);
        AddQuestionTile(18, 8, (int)MapPartId.BrickIntro, false);
        AddSpikeCluster(21.2f, 2, (int)MapPartId.BrickIntro);
    }

    private void BuildFirstPipeTrap()
    {
        AddGroundRange(23, 31, (int)MapPartId.FirstPipeTrap);
        AddSpikeCluster(24.0f, 2, (int)MapPartId.FirstPipeTrap);
        AddPipe(26, 2, 2, (int)MapPartId.FirstPipeTrap);
        AddPipeSpawner(GetPipeBulletOrigin(26, 2, 2), Vector2.up, 2.15f, 0.70f, (int)MapPartId.FirstPipeTrap);
        AddEnemy(new Vector2(30.2f, FloorTop + EnemyRadius + 0.03f), -1, (int)MapPartId.FirstPipeTrap);
    }

    private void BuildUpperTrapRun()
    {
        AddGroundRange(32, 34, (int)MapPartId.UpperTrapRun);
        AddGroundRange(38, 40, (int)MapPartId.UpperTrapRun);
        AddBrickTile(33, 4, (int)MapPartId.UpperTrapRun, true);
        AddBrickTile(34, 4, (int)MapPartId.UpperTrapRun, true);
        AddBrickTile(35, 4, (int)MapPartId.UpperTrapRun, true);
        AddBrickTile(36, 4, (int)MapPartId.UpperTrapRun, true);
        AddBrickTile(37, 4, (int)MapPartId.UpperTrapRun, true);
        AddEnemy(new Vector2(39.0f, FloorTop + EnemyRadius + 0.03f), -1, (int)MapPartId.UpperTrapRun);
    }

    private void BuildBulletPipeRun()
    {
        AddGroundRange(41, 54, (int)MapPartId.BulletPipeRun);
        AddPipe(44, 2, 2, (int)MapPartId.BulletPipeRun);
        AddPipeSpawner(GetPipeBulletOrigin(44, 2, 2), Vector2.up, 1.80f, 0.30f, (int)MapPartId.BulletPipeRun);
        AddSpikeCluster(47.0f, 2, (int)MapPartId.BulletPipeRun);
        AddPipe(50, 2, 3, (int)MapPartId.BulletPipeRun);
        AddPipeSpawner(GetPipeBulletOrigin(50, 2, 3), Vector2.up, 1.45f, 0.85f, (int)MapPartId.BulletPipeRun);
        AddEnemy(new Vector2(53.5f, FloorTop + EnemyRadius + 0.03f), -1, (int)MapPartId.BulletPipeRun);
    }

    private void BuildDoubleGapTrap()
    {
        AddGroundRange(55, 57, (int)MapPartId.DoubleGapTrap);
        AddGroundRange(60, 62, (int)MapPartId.DoubleGapTrap);
        AddGroundRange(65, 67, (int)MapPartId.DoubleGapTrap);
        AddBrickTile(55, 4, (int)MapPartId.DoubleGapTrap);
        AddBrickTile(56, 4, (int)MapPartId.DoubleGapTrap);
        AddFallingBrickTrapTile(57, 8, (int)MapPartId.DoubleGapTrap);
        AddFallingBrickTrapTile(58, 8, (int)MapPartId.DoubleGapTrap);
        AddFallingBrickTrapTile(59, 8, (int)MapPartId.DoubleGapTrap);
        AddBrickTile(60, 8, (int)MapPartId.DoubleGapTrap);
        AddBrickTile(61, 8, (int)MapPartId.DoubleGapTrap);
        AddBrickTile(62, 8, (int)MapPartId.DoubleGapTrap);
        AddPipeSpawner(new Vector2(64.0f, CameraY + CameraSize + 0.55f), Vector2.down, 1.55f, 0.25f, (int)MapPartId.DoubleGapTrap, 3.5f);
    }

    private void BuildGoalLane()
    {
        AddGroundRange(68, 72, (int)MapPartId.GoalLane);
        AddGroundRange(77, 85, (int)MapPartId.GoalLane);

        AddBrickTile(70, 7, (int)MapPartId.GoalLane);
        AddQuestionTile(71, 7, (int)MapPartId.GoalLane, false, true);
        AddBrickTile(72, 7, (int)MapPartId.GoalLane);

        AddGroundTile(72, 2, (int)MapPartId.GoalLane);
        AddGroundTile(72, 3, (int)MapPartId.GoalLane);

        int breakClusterId = AllocateBreakClusterId();
        for (int x = 73; x <= 76; x++)
        {
            for (int y = 0; y <= 5; y++)
            {
                AddBreakableGroundTile(x, y, (int)MapPartId.GoalLane, breakClusterId);
            }
        }

        AddGroundTile(77, 2, (int)MapPartId.GoalLane);
        AddGroundTile(77, 3, (int)MapPartId.GoalLane);
        AddGroundTile(77, 4, (int)MapPartId.GoalLane);

        for (int y = 7; y <= 11; y++)
        {
            AddGroundTile(75, y, (int)MapPartId.GoalLane);
        }

        AddEnemy(new Vector2(73.55f, 6f + EnemyRadius + 0.03f), 1, (int)MapPartId.GoalLane);
        AddFlagPole(FlagPoleX, (int)MapPartId.GoalLane);
        AddGoal(GoalColumn, (int)MapPartId.GoalLane);
    }
    private void AddGroundRange(int startX, int endX, int partId)
    {
        for (int x = startX; x <= endX; x++)
        {
            AddGroundTile(x, 0, partId);
            AddGroundTile(x, 1, partId);
        }
    }

    private BlockData AddGroundTile(int x, int y, int partId, bool breaksUnderHeavy = false, int breakClusterId = 0)
    {
        Vector2Int cell = new Vector2Int(x, y);
        GameObject root = CreateTileRoot("Ground_" + x + "_" + y, cell, environmentRoot);
        MeshRenderer main = CreateShape("Base", GetRectMesh(), new Vector3(0.98f, 0.98f, 1f), GroundColor, root.transform, 0f).GetComponent<MeshRenderer>();
        CreateShape("LineH", GetRectMesh(), new Vector3(0.84f, 0.08f, 1f), GroundDetailColor, root.transform, -0.01f).transform.localPosition = new Vector3(0f, 0.16f, -0.01f);
        CreateShape("LineV", GetRectMesh(), new Vector3(0.08f, 0.84f, 1f), GroundDetailColor, root.transform, -0.01f).transform.localPosition = new Vector3(0.18f, -0.04f, -0.01f);
        CreateShape("LineD", GetRectMesh(), new Vector3(0.42f, 0.06f, 1f), GroundDetailColor, root.transform, -0.01f).transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.96f, 0.96f);
        collider.sharedMaterial = GetFrictionlessMaterial();
        BlockData block = new BlockData
        {
            Cell = cell,
            Type = TileType.Ground,
            PartId = partId,
            Root = root,
            MainRenderer = main,
            Renderers = root.GetComponentsInChildren<MeshRenderer>(true),
            Collider = collider,
            BreaksUnderHeavy = breaksUnderHeavy,
            BreakClusterId = breakClusterId,
        };
        blocks[cell] = block;
        blockLookup[collider] = block;
        return block;
    }

    private void AddBreakableGroundTile(int x, int y, int partId, int breakClusterId)
    {
        AddGroundTile(x, y, partId, true, breakClusterId);
    }

    private int AllocateBreakClusterId()
    {
        return nextBreakClusterId++;
    }

    private BlockData AddBrickTile(int x, int y, int partId, bool hiddenUntilHit = false)
    {
        Vector2Int cell = new Vector2Int(x, y);
        GameObject root = CreateTileRoot("Brick_" + x + "_" + y, cell, environmentRoot);
        MeshRenderer main = CreateShape("Base", GetRectMesh(), new Vector3(0.94f, 0.94f, 1f), BrickColor, root.transform, 0f).GetComponent<MeshRenderer>();
        CreateShape("Band", GetRectMesh(), new Vector3(0.88f, 0.10f, 1f), BrickDetailColor, root.transform, -0.01f).transform.localPosition = new Vector3(0f, 0.12f, -0.01f);
        CreateShape("Band2", GetRectMesh(), new Vector3(0.88f, 0.10f, 1f), BrickDetailColor, root.transform, -0.01f).transform.localPosition = new Vector3(0f, -0.18f, -0.01f);
        CreateShape("JointL", GetRectMesh(), new Vector3(0.08f, 0.40f, 1f), BrickDetailColor, root.transform, -0.01f).transform.localPosition = new Vector3(-0.20f, -0.02f, -0.01f);
        CreateShape("JointR", GetRectMesh(), new Vector3(0.08f, 0.40f, 1f), BrickDetailColor, root.transform, -0.01f).transform.localPosition = new Vector3(0.20f, 0.18f, -0.01f);
        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.94f, 0.94f);
        collider.sharedMaterial = GetFrictionlessMaterial();
        BlockData block = new BlockData
        {
            Cell = cell,
            Type = TileType.Brick,
            PartId = partId,
            Root = root,
            MainRenderer = main,
            Renderers = root.GetComponentsInChildren<MeshRenderer>(true),
            Collider = collider,
            HiddenUntilHit = hiddenUntilHit,
        };
        SetBlockVisualsVisible(block, !hiddenUntilHit);
        blocks[cell] = block;
        blockLookup[collider] = block;
        return block;
    }

    private void AddFallingBrickTrapTile(int x, int y, int partId)
    {
        BlockData block = AddBrickTile(x, y, partId);
        Vector3 position = block.Root != null ? block.Root.transform.position : CellToWorld(new Vector2Int(x, y), 0f);
        fallingBrickTraps.Add(new FallingBrickTrapData
        {
            Block = block,
            Position = new Vector2(position.x, position.y),
            VerticalVelocity = 0f,
            Armed = true,
            Falling = false,
        });
    }

    private void AddQuestionTile(int x, int y, int partId, bool spawnsEnemy, bool spawnsMushroom = false)
    {
        Vector2Int cell = new Vector2Int(x, y);
        GameObject root = CreateTileRoot("Question_" + x + "_" + y, cell, environmentRoot);
        MeshRenderer main = CreateShape("Base", GetRectMesh(), new Vector3(0.94f, 0.94f, 1f), QuestionColor, root.transform, 0f).GetComponent<MeshRenderer>();
        CreateShape("Center", GetRectMesh(), new Vector3(0.18f, 0.18f, 1f), QuestionDetailColor, root.transform, -0.01f).transform.localPosition = new Vector3(0f, 0.08f, -0.01f);
        CreateShape("Dot", GetRectMesh(), new Vector3(0.10f, 0.10f, 1f), QuestionDetailColor, root.transform, -0.01f).transform.localPosition = new Vector3(0f, -0.20f, -0.01f);
        CreateShape("CornerTL", GetRectMesh(), new Vector3(0.08f, 0.08f, 1f), QuestionDetailColor, root.transform, -0.01f).transform.localPosition = new Vector3(-0.30f, 0.30f, -0.01f);
        CreateShape("CornerTR", GetRectMesh(), new Vector3(0.08f, 0.08f, 1f), QuestionDetailColor, root.transform, -0.01f).transform.localPosition = new Vector3(0.30f, 0.30f, -0.01f);
        CreateShape("CornerBL", GetRectMesh(), new Vector3(0.08f, 0.08f, 1f), QuestionDetailColor, root.transform, -0.01f).transform.localPosition = new Vector3(-0.30f, -0.30f, -0.01f);
        CreateShape("CornerBR", GetRectMesh(), new Vector3(0.08f, 0.08f, 1f), QuestionDetailColor, root.transform, -0.01f).transform.localPosition = new Vector3(0.30f, -0.30f, -0.01f);
        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.94f, 0.94f);
        collider.sharedMaterial = GetFrictionlessMaterial();
        BlockData block = new BlockData { Cell = cell, Type = TileType.Question, PartId = partId, Root = root, MainRenderer = main, Renderers = root.GetComponentsInChildren<MeshRenderer>(true), Collider = collider, SpawnsEnemy = spawnsEnemy, SpawnsMushroom = spawnsMushroom };
        blocks[cell] = block;
        blockLookup[collider] = block;
    }

    private void AddStartSecretWall()
    {
        GameObject root = new GameObject("StartSecretWall");
        root.transform.SetParent(hazardRoot, false);
        root.transform.position = new Vector3(-0.06f, FloorTop + 2.25f, -0.14f);
        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.28f, 8.70f);
        collider.sharedMaterial = GetFrictionlessMaterial();

        bool opened = MariodemoProgressState.StartSecretWallOpened;
        collider.enabled = !opened;
        startSecretWall = new SecretWallData
        {
            Root = root.transform,
            Collider = collider,
            Trigger = new Rect(-0.30f, FloorTop - 0.08f, 0.50f, 1.55f),
            HitCount = 0,
            ComboTimer = 0f,
            ContactLatch = false,
            Opened = opened,
        };
    }
    private void SetBlockVisualsVisible(BlockData block, bool visible)
    {
        if (block == null || block.Renderers == null)
        {
            return;
        }

        for (int i = 0; i < block.Renderers.Length; i++)
        {
            MeshRenderer renderer = block.Renderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = visible;
        }
    }

    private void AddPipe(int startX, int width, int height, int partId)
    {
        int topY = 1 + height;
        int endX = startX + width - 1;
        for (int x = startX; x <= endX; x++)
        {
            for (int y = 2; y <= topY; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                GameObject root = CreateTileRoot("Pipe_" + x + "_" + y, cell, environmentRoot);
                bool top = y == topY;
                float bodyHeight = top ? 0.70f : 1.02f;
                float bodyCenterY = top ? -0.12f : 0f;

                MeshRenderer main = CreateShape("Body", GetRectMesh(), new Vector3(1.00f, bodyHeight, 1f), PipeColor, root.transform, 0f).GetComponent<MeshRenderer>();
                main.transform.localPosition = new Vector3(0f, bodyCenterY, 0f);

                if (top)
                {
                    CreateShape("Cap", GetRectMesh(), new Vector3(1.18f, 0.28f, 1f), PipeColor, root.transform, -0.01f).transform.localPosition = new Vector3(0f, 0.30f, -0.01f);
                }

                BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(0.96f, 0.96f);
                collider.sharedMaterial = GetFrictionlessMaterial();
                BlockData block = new BlockData { Cell = cell, Type = TileType.Pipe, PartId = partId, Root = root, MainRenderer = main, Collider = collider };
                blocks[cell] = block;
                blockLookup[collider] = block;
            }
        }
    }

    private void AddSpikeCluster(float startX, int count, int partId)
    {
        GameObject root = new GameObject("SpikeCluster_" + partId + "_" + spikes.Count);
        root.transform.SetParent(hazardRoot, false);
        for (int i = 0; i < count; i++)
        {
            float x = startX + i * 0.55f;
            GameObject tri = CreateShape("Spike_" + i, GetTriMesh(), new Vector3(0.48f, 0.72f, 1f), SpikeColor, root.transform, 0f);
            tri.transform.position = new Vector3(x, FloorTop + 0.30f, -0.2f);
        }
        spikes.Add(new SpikeData
        {
            Root = root,
            PartId = partId,
            Collider = null,
            Bounds = Rect.zero,
        });
    }

    private void AddEnemy(Vector2 position, int direction, int partId, bool usesPatrolRange = true, bool lockPatrolOnWideGround = false, bool useGravity = false, float verticalVelocity = 0f)
    {
        GameObject root = new GameObject("Enemy_" + enemies.Count);
        root.transform.SetParent(actorRoot, false);
        root.transform.position = new Vector3(position.x, position.y, -0.20f);
        MeshRenderer outline = CreateShape("Outline", GetCircleMesh(), new Vector3(0.76f, 0.76f, 1f), EnemyOutlineColor, root.transform, 0f).GetComponent<MeshRenderer>();
        MeshRenderer fill = CreateShape("Fill", GetCircleMesh(), new Vector3(0.62f, 0.62f, 1f), EnemyFillColor, root.transform, -0.01f).GetComponent<MeshRenderer>();
        ResolveEnemyPatrolRange(position, out float patrolMinX, out float patrolMaxX);
        enemies.Add(new EnemyData
        {
            Root = root.transform,
            OutlineRenderer = outline,
            FillRenderer = fill,
            Position = position,
            Direction = direction == 0 ? -1 : direction,
            Alive = true,
            PartId = partId,
            PatrolMinX = patrolMinX,
            PatrolMaxX = patrolMaxX,
            UsesPatrolRange = usesPatrolRange,
            LockPatrolOnWideGround = lockPatrolOnWideGround,
            UseGravity = useGravity,
            VerticalVelocity = verticalVelocity,
        });
    }
    private void ResolveEnemyPatrolRange(Vector2 position, out float minX, out float maxX)
    {
        const float sampleStep = 0.25f;
        const int maxSamples = 96;
        minX = position.x;
        maxX = position.x;
        for (int i = 0; i < maxSamples; i++)
        {
            float candidateX = minX - sampleStep;
            if (!CanEnemyStandAt(candidateX, position.y)) break;
            minX = candidateX;
        }
        for (int i = 0; i < maxSamples; i++)
        {
            float candidateX = maxX + sampleStep;
            if (!CanEnemyStandAt(candidateX, position.y)) break;
            maxX = candidateX;
        }
        minX -= 0.04f;
        maxX += 0.04f;
    }
    private bool CanEnemyStandAt(float sampleX, float referenceY)
    {
        Vector2 probe = new Vector2(sampleX, referenceY);
        if (!TryGetSurfaceY(probe + Vector2.up * 0.90f, out float surfaceY)) return false;
        float centerY = surfaceY + EnemyRadius;
        if (Mathf.Abs(centerY - referenceY) > 0.80f) return false;
        return !IsSolidAtCircle(new Vector2(sampleX, centerY + 0.18f), 0.14f);
    }

    private Vector2 GetPipeBulletOrigin(int startX, int width, int height)
    {
        float mouthX = startX + width * 0.5f;
        float mouthY = height + 2.20f;
        return new Vector2(mouthX, mouthY);
    }

    private void AddPipeSpawner(Vector2 origin, Vector2 direction, float interval, float startOffset, int partId, float speedMultiplier = 1f)
    {
        Vector2 spawnDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
        pipeSpawners.Add(new PipeSpawnerData
        {
            Origin = origin,
            Direction = spawnDirection,
            Cooldown = interval,
            CooldownTimer = Mathf.Max(0f, startOffset),
            PartId = partId,
            Armed = true,
            SpeedMultiplier = Mathf.Max(0.1f, speedMultiplier),
        });
    }

    private PhysicsMaterial2D GetFrictionlessMaterial()
    {
        if (frictionlessMaterial == null)
        {
            frictionlessMaterial = new PhysicsMaterial2D("MariodemoNoFriction")
            {
                friction = 0f,
                bounciness = 0f,
            };
        }

        return frictionlessMaterial;
    }
    private void AddFlagPole(float x, int partId)
    {
        GameObject root = new GameObject("GoalFlagPole");
        root.transform.SetParent(environmentRoot, false);
        root.transform.position = new Vector3(x, FloorTop + 1.56f, -0.12f);
        CreateShape("Pole", GetRectMesh(), new Vector3(0.12f, 3.18f, 1f), FlagPoleColor, root.transform, 0f);
        CreateShape("Topper", GetCircleMesh(), new Vector3(0.20f, 0.20f, 1f), FlagPoleColor, root.transform, -0.01f).transform.localPosition = new Vector3(0f, 1.55f, -0.01f);
        MeshRenderer flagRenderer = CreateShape("Flag", GetTriMesh(), new Vector3(0.52f, 0.42f, 1f), FlagClothColor, root.transform, -0.01f).GetComponent<MeshRenderer>();
        flagRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        flagRenderer.transform.localPosition = new Vector3(0.26f, 1.08f, -0.01f);

        goalFlagPole = new FlagPoleData
        {
            Root = root.transform,
            FlagRenderer = flagRenderer,
            Trigger = new Rect(x - 0.28f, FloorTop, 0.56f, 3.36f),
            Activated = false,
        };
    }

    private void AddGoal(int x, int partId)
    {
        Vector2Int cell = new Vector2Int(x, 2);
        GameObject root = CreateTileRoot("Goal_" + x, cell, environmentRoot);
        root.transform.localPosition += new Vector3(0f, 0.72f, 0f);
        MeshRenderer main = CreateShape("Frame", GetRectMesh(), new Vector3(0.94f, 1.72f, 1f), GoalFrameColor, root.transform, 0f).GetComponent<MeshRenderer>();
        CreateShape("Fill", GetRectMesh(), new Vector3(0.70f, 1.42f, 1f), GoalFillColor, root.transform, -0.01f);
        goalDoorEntryTarget = new Vector2(x + 0.50f, FloorTop + 1.04f);
        blocks[cell] = new BlockData { Cell = cell, Type = TileType.Goal, PartId = partId, Root = root, MainRenderer = main, Collider = null };
    }

    private void CreatePlayer()
    {
        GameObject root = new GameObject("Player");
        root.transform.SetParent(actorRoot, false);
        root.transform.position = new Vector3(playerSpawn.x, playerSpawn.y, -0.24f);
        playerRenderer = CreateShape("Body", GetCircleMesh(), new Vector3(0.70f, 0.70f, 1f), PlayerColor, root.transform, 0f).GetComponent<MeshRenderer>();
        playerBody = root.AddComponent<Rigidbody2D>();
        playerBody.gravityScale = 3.9f;
        playerBody.freezeRotation = true;
        playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        playerCollider = root.AddComponent<CircleCollider2D>();
        playerCollider.radius = PlayerRadius;
        playerCollider.sharedMaterial = GetFrictionlessMaterial();
        playerRoot = root.transform;
        ApplyPlayerPowerState();
    }

    private void SpawnRuntimeCoin(Vector2 origin)
    {
        GameObject root = new GameObject("Coin_" + runtimeCoins.Count);
        root.transform.SetParent(actorRoot, false);
        root.transform.position = new Vector3(origin.x, origin.y, -0.18f);
        CreateShape("Body", GetCircleMesh(), new Vector3(CoinRadius * 2f, CoinRadius * 2f, 1f), CoinColor, root.transform, 0f);

        runtimeCoins.Add(new RuntimeCoinData
        {
            Root = root.transform,
            Origin = origin,
            Elapsed = 0f,
        });
    }

    private void SpawnRuntimeMushroom(Vector2 emergedPosition, int partId)
    {
        GameObject root = new GameObject("Mushroom_" + runtimeMushrooms.Count);
        root.transform.SetParent(actorRoot, false);
        root.transform.position = new Vector3(emergedPosition.x, emergedPosition.y - MushroomRiseHeight, -0.18f);
        CreateShape("Stem", GetRectMesh(), new Vector3(0.24f, 0.28f, 1f), MushroomStemColor, root.transform, -0.01f).transform.localPosition = new Vector3(0f, -0.14f, -0.01f);
        CreateShape("Cap", GetCircleMesh(), new Vector3(0.74f, 0.58f, 1f), MushroomCapColor, root.transform, 0f).transform.localPosition = new Vector3(0f, 0.12f, 0f);
        CreateShape("SpotL", GetCircleMesh(), new Vector3(0.18f, 0.18f, 1f), MushroomSpotColor, root.transform, -0.02f).transform.localPosition = new Vector3(-0.16f, 0.17f, -0.02f);
        CreateShape("SpotR", GetCircleMesh(), new Vector3(0.18f, 0.18f, 1f), MushroomSpotColor, root.transform, -0.02f).transform.localPosition = new Vector3(0.16f, 0.17f, -0.02f);
        CreateShape("SpotC", GetCircleMesh(), new Vector3(0.15f, 0.15f, 1f), MushroomSpotColor, root.transform, -0.02f).transform.localPosition = new Vector3(0f, 0.28f, -0.02f);

        runtimeMushrooms.Add(new RuntimeMushroomData
        {
            Root = root.transform,
            Position = new Vector2(emergedPosition.x, emergedPosition.y - MushroomRiseHeight),
            SpawnOrigin = emergedPosition,
            Emergence = 0f,
            VerticalVelocity = 0f,
            Direction = 1,
            Active = true,
            PartId = partId,
        });
    }

    private void SpawnSecretDiamond(Vector2 position)
    {
        Sprite diamondSprite = GetSecretDiamondSprite();
        if (diamondSprite == null)
        {
            return;
        }

        GameObject root = new GameObject("SecretDiamond");
        root.transform.SetParent(actorRoot, false);
        root.transform.position = new Vector3(position.x, position.y, -0.16f);
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = diamondSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 18;
        root.transform.localScale = Vector3.one * 0.92f;

        hiddenSecretDiamond = new SecretDiamondData
        {
            Root = root.transform,
            Position = position,
        };
    }
    private void StartBrokenBlockFall(BlockData block)
    {
        if (block == null || block.Root == null)
        {
            return;
        }

        Vector3 sourcePosition = block.Root.transform.position;
        Color shardColor = block.Type == TileType.Brick || block.Type == TileType.Question ? BrickColor : GroundColor;
        float z = sourcePosition.z;

        if (block.Collider != null)
        {
            block.Collider.enabled = false;
            blockLookup.Remove(block.Collider);
        }

        blocks.Remove(block.Cell);
        Destroy(block.Root.gameObject);

        Vector2[] offsets =
        {
            new Vector2(-0.18f, 0.18f),
            new Vector2(0.18f, 0.18f),
            new Vector2(-0.18f, -0.18f),
            new Vector2(0.18f, -0.18f),
        };
        Vector2[] velocities =
        {
            new Vector2(-1.55f, 4.15f),
            new Vector2(1.55f, 4.30f),
            new Vector2(-1.95f, 3.05f),
            new Vector2(1.95f, 3.20f),
        };
        float[] angularVelocities = { -240f, 240f, -330f, 330f };

        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject root = new GameObject("BrokenGroundPiece_" + block.Cell.x + "_" + block.Cell.y + "_" + i);
            root.transform.SetParent(hazardRoot, false);
            Vector3 worldPosition = new Vector3(sourcePosition.x + offsets[i].x, sourcePosition.y + offsets[i].y, z);
            root.transform.position = worldPosition;
            root.transform.rotation = Quaternion.Euler(0f, 0f, i * 9f);
            CreateShape("Body", GetRectMesh(), new Vector3(0.30f, 0.30f, 1f), shardColor, root.transform, 0f);

            fallingBrokenBlocks.Add(new FallingBrokenBlockData
            {
                Root = root.transform,
                Position = new Vector2(worldPosition.x, worldPosition.y),
                Velocity = velocities[i],
                AngularVelocity = angularVelocities[i],
            });
        }
    }
    private void SpawnBullet(Vector2 position, Vector2 direction, int partId, float speedMultiplier = 1f)
    {
        float actualSpeed = BulletSpeed * Mathf.Max(0.1f, speedMultiplier);
        Vector2 velocity = direction.sqrMagnitude > 0.0001f ? direction.normalized * actualSpeed : Vector2.up * actualSpeed;
        GameObject root = new GameObject("Bullet_" + bullets.Count);
        root.transform.SetParent(actorRoot, false);
        root.transform.position = new Vector3(position.x, position.y, -0.22f);
        root.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, velocity.normalized));
        MeshRenderer outline = CreateShape("Outline", GetTriMesh(), new Vector3(0.66f, 0.66f, 1f), EnemyOutlineColor, root.transform, 0f).GetComponent<MeshRenderer>();
        MeshRenderer fill = CreateShape("Fill", GetTriMesh(), new Vector3(0.50f, 0.50f, 1f), EnemyFillColor, root.transform, -0.01f).GetComponent<MeshRenderer>();

        bullets.Add(new BulletData
        {
            Root = root.transform,
            OutlineRenderer = outline,
            FillRenderer = fill,
            Position = position,
            Velocity = velocity,
            Active = true,
            PartId = partId,
        });
    }
}






