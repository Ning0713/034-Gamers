#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

internal static class PacJamPlayModeStartScene
{
    private const string StartMenuScenePath = "Assets/Scenes/PacJam/PacJamStartMenu.unity";

    [InitializeOnLoadMethod]
    private static void ConfigurePlayModeStartScene()
    {
        SceneAsset startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartMenuScenePath);
        if (startScene == null)
        {
            return;
        }

        if (EditorSceneManager.playModeStartScene != startScene)
        {
            EditorSceneManager.playModeStartScene = startScene;
        }
    }
}
#endif
