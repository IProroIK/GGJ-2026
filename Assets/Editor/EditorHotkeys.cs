using UnityEditor;
using UnityEngine;

public class EditorHotkeys
{
    // Ctrl + Shift + E (Windows) / Cmd + Shift + E (Mac)
    [MenuItem("Hotkeys/Toggle Active %#e")]
    private static void ToggleActive()
    {
        foreach (var obj in Selection.gameObjects)
        {
            Undo.RecordObject(obj, "Toggle Active");
            obj.SetActive(!obj.activeSelf);
            EditorUtility.SetDirty(obj);
        }
    }

    [MenuItem("Hotkeys/Toggle Active %#e", true)]
    private static bool ToggleActiveValidate()
    {
        return Selection.activeGameObject != null;
    }

    [MenuItem("SDK/Select GameConfig #%t", false, -2)]
    public static void SelectGameConfig()
    {
        Selection.activeObject = Resources.Load<GameConfig>("GameConfig");
    }
}