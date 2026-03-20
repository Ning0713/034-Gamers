#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

internal readonly struct AudioStatusSlot
{
    public AudioStatusSlot(string propertyName, string label)
    {
        PropertyName = propertyName;
        Label = label;
    }

    public string PropertyName { get; }
    public string Label { get; }
}

internal static class RuntimeAudioStatusInspector
{
    private static readonly Color AssignedColor = new Color(0.22f, 0.56f, 0.28f, 0.18f);
    private static readonly Color MissingColor = new Color(0.72f, 0.24f, 0.20f, 0.16f);

    public static void Draw(SerializedObject serializedObject, string title, AudioStatusSlot[] slots)
    {
        int assignedCount = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            SerializedProperty property = serializedObject.FindProperty(slots[i].PropertyName);
            if (property != null && property.objectReferenceValue != null)
            {
                assignedCount++;
            }
        }

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("只有放在 Assets 目录并已被 Unity 导入的音频，才会显示在这些槽里。项目根目录的 资源 文件夹不会直接出现在这里。", MessageType.Info);
        EditorGUILayout.HelpBox($"当前已绑定 {assignedCount}/{slots.Length} 个音效槽。", assignedCount == slots.Length ? MessageType.Info : MessageType.Warning);

        for (int i = 0; i < slots.Length; i++)
        {
            SerializedProperty property = serializedObject.FindProperty(slots[i].PropertyName);
            Object clip = property != null ? property.objectReferenceValue : null;
            Color oldColor = GUI.color;
            GUI.color = clip != null ? AssignedColor : MissingColor;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUI.color = oldColor;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(clip != null ? "已绑定" : "缺失", GUILayout.Width(42f));
                    EditorGUILayout.LabelField(slots[i].Label, GUILayout.Width(150f));
                    EditorGUILayout.LabelField(clip != null ? clip.name : "未绑定", clip != null ? EditorStyles.label : EditorStyles.boldLabel);
                    using (new EditorGUI.DisabledScope(clip == null))
                    {
                        if (GUILayout.Button("Ping", GUILayout.Width(44f)) && clip != null)
                        {
                            EditorGUIUtility.PingObject(clip);
                        }
                    }
                }
            }
            GUI.color = oldColor;
        }
    }
}

[CustomEditor(typeof(PacJamDemoController))]
internal sealed class PacJamDemoControllerEditor : Editor
{
    private static readonly AudioStatusSlot[] Slots =
    {
        new AudioStatusSlot("pelletSfx", "普通豆音效"),
        new AudioStatusSlot("powerPelletSfx", "大力丸音效"),
        new AudioStatusSlot("ghostEatenSfx", "吃鬼音效"),
        new AudioStatusSlot("playerHitSfx", "玩家死亡音效"),
        new AudioStatusSlot("entranceSfx", "入场音效"),
        new AudioStatusSlot("ghostHuntIntroSfx", "第二小关获胜音效"),
        new AudioStatusSlot("ghostHuntBgm", "第二小关 BGM"),
        new AudioStatusSlot("frightenedLoopSfx", "敌人逃跑循环音效"),
        new AudioStatusSlot("chaseLoopSfx", "追击敌人循环音效"),
        new AudioStatusSlot("moveLoopSfx", "移动循环音效"),
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script");
        serializedObject.ApplyModifiedProperties();
        serializedObject.UpdateIfRequiredOrScript();
        RuntimeAudioStatusInspector.Draw(serializedObject, "音频绑定状态", Slots);
    }
}

[CustomEditor(typeof(MariodemoController))]
internal sealed class MariodemoControllerEditor : Editor
{
    private static readonly AudioStatusSlot[] Slots =
    {
        new AudioStatusSlot("stageBgm", "关卡 BGM"),
        new AudioStatusSlot("jumpSfx", "跳跃音效"),
        new AudioStatusSlot("deathSfx", "死亡音效"),
        new AudioStatusSlot("stompSfx", "踩敌人音效"),
        new AudioStatusSlot("blockHitSfx", "顶砖音效"),
        new AudioStatusSlot("questionBlockSfx", "问号砖触发音效"),
        new AudioStatusSlot("coinPopupSfx", "金币弹出音效"),
        new AudioStatusSlot("bulletFireSfx", "管道发射音效"),
        new AudioStatusSlot("stageClearSfx", "过关音效"),
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script");
        serializedObject.ApplyModifiedProperties();
        serializedObject.UpdateIfRequiredOrScript();
        RuntimeAudioStatusInspector.Draw(serializedObject, "音频绑定状态", Slots);
    }
}

[CustomEditor(typeof(MariodemoTransitionCutsceneController))]
internal sealed class MariodemoTransitionCutsceneControllerEditor : Editor
{
    private static readonly AudioStatusSlot[] Slots =
    {
        new AudioStatusSlot("cutsceneStartSfx", "过场开始音效"),
        new AudioStatusSlot("cutsceneRollLoopSfx", "滚动循环音效"),
        new AudioStatusSlot("cutsceneBgm", "过场 BGM"),
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script");
        serializedObject.ApplyModifiedProperties();
        serializedObject.UpdateIfRequiredOrScript();
        RuntimeAudioStatusInspector.Draw(serializedObject, "音频绑定状态", Slots);
    }
}
#endif

