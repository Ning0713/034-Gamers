using UnityEngine;

public static class ForestdemoProgressState
{
    public const string CutsceneSceneName = "ForestdemoTransition";
    public const string StageSceneName = "ForestdemoDemo";
    public const string EndingSceneName = "ForestdemoEnding";

    public static int Lives { get; private set; } = MariodemoProgressState.InitialLives;

    public static void BeginFromMariodemo(int lives)
    {
        Lives = Mathf.Clamp(lives, 0, MariodemoProgressState.InitialLives);
        if (Lives <= 0)
        {
            Lives = MariodemoProgressState.InitialLives;
        }
    }

    public static void SetLives(int lives)
    {
        Lives = Mathf.Clamp(lives, 0, MariodemoProgressState.InitialLives);
    }

    public static void ResetProgress()
    {
        Lives = MariodemoProgressState.InitialLives;
    }
}
