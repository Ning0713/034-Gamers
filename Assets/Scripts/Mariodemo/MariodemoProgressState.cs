using UnityEngine;

public static class MariodemoProgressState
{
    public const int InitialLives = 3;
    public const string CutsceneSceneName = "MariodemoCutscene";
    public const string StageSceneName = "MariodemoDemo";

    public static int Lives { get; private set; } = InitialLives;
    public static bool HasSecretDiamond { get; private set; }
    public static bool StartSecretWallOpened { get; private set; }

    public static void BeginFromPacJam(int lives)
    {
        Lives = Mathf.Clamp(lives, 0, InitialLives);
        if (Lives <= 0)
        {
            Lives = InitialLives;
        }

        HasSecretDiamond = false;
        StartSecretWallOpened = false;
    }

    public static void SetLives(int lives)
    {
        Lives = Mathf.Clamp(lives, 0, InitialLives);
    }

    public static void CollectSecretDiamond()
    {
        HasSecretDiamond = true;
    }

    public static void SetStartSecretWallOpened(bool opened)
    {
        StartSecretWallOpened = opened;
    }

    public static void ResetProgress()
    {
        Lives = InitialLives;
        HasSecretDiamond = false;
        StartSecretWallOpened = false;
    }
}