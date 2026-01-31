#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class ReplaceRenderersMaterialWindow : OdinEditorWindow
{
    [Title("Replace Material In Selection")]
    [InfoBox("Выдели объекты в сцене → нажми Apply. Скрипт найдёт все Renderer в детях и заменит ВСЕ слоты материалов на один указанный материал.")]
    
    [BoxGroup("Settings")]
    [Required]
    [AssetsOnly]
    [LabelText("Material (asset)")]
    public Material TargetMaterial;

    [BoxGroup("Settings")]
    [LabelText("Include Inactive")]
    public bool IncludeInactive = true;

    [BoxGroup("Settings")]
    [LabelText("Affect Prefab Instances")]
    [InfoBox("Если выключено — пропускаем объекты внутри prefab instance (сцена останется чище).", InfoMessageType.None)]
    public bool AffectPrefabInstances = true;

    [BoxGroup("Settings")]
    [LabelText("Also Affect ParticleSystemRenderer")]
    public bool IncludeParticleRenderers = true;

    [BoxGroup("Preview")]
    [ReadOnly]
    public int SelectedRoots;

    [BoxGroup("Preview")]
    [ReadOnly]
    public int FoundRenderers;

    [BoxGroup("Preview")]
    [ReadOnly]
    public int ChangedRenderers;

    private List<Renderer> _cached = new();

    [MenuItem("Tools/Odin/Replace Renderers Material")]
    private static void Open()
    {
        GetWindow<ReplaceRenderersMaterialWindow>("Replace Material").Show();
    }

    protected override void OnGUI()
    {
        GUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh Preview", GUILayout.Height(28)))
            {
                RefreshPreview();
            }

            GUI.enabled = TargetMaterial != null;
            if (GUILayout.Button("Apply", GUILayout.Height(28)))
            {
                Apply();
            }
            GUI.enabled = true;
        }
    }

    [Button(ButtonSizes.Large)]
    private void RefreshPreview()
    {
        _cached.Clear();
        ChangedRenderers = 0;

        var roots = Selection.gameObjects ?? new GameObject[0];
        SelectedRoots = roots.Length;

        if (SelectedRoots == 0)
        {
            FoundRenderers = 0;
            return;
        }

        foreach (var root in roots)
        {
            if (root == null) continue;
            CollectRenderers(root, _cached);
        }

        FoundRenderers = _cached.Count;
    }

    private void CollectRenderers(GameObject root, List<Renderer> list)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(IncludeInactive);
        foreach (var r in renderers)
        {
            if (r == null) continue;

            if (!AffectPrefabInstances)
            {
                // Если это часть prefab instance — пропустим
                if (PrefabUtility.IsPartOfPrefabInstance(r.gameObject))
                    continue;
            }

            if (!IncludeParticleRenderers && r is ParticleSystemRenderer)
                continue;

            list.Add(r);
        }
    }

    [Button(ButtonSizes.Large)]
    private void Apply()
    {
        if (TargetMaterial == null)
        {
            EditorUtility.DisplayDialog("Replace Material", "TargetMaterial не задан.", "OK");
            return;
        }

        RefreshPreview();

        if (FoundRenderers == 0)
        {
            EditorUtility.DisplayDialog("Replace Material", "Renderer'ы не найдены в выделении.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Replace Renderers Material");

        ChangedRenderers = 0;

        foreach (var r in _cached)
        {
            if (r == null) continue;

            // sharedMaterials, чтобы не плодить инстансы
            var mats = r.sharedMaterials;
            if (mats == null || mats.Length == 0) continue;

            bool alreadyAllTarget = true;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != TargetMaterial)
                {
                    alreadyAllTarget = false;
                    break;
                }
            }
            if (alreadyAllTarget) continue;

            Undo.RecordObject(r, "Replace Renderer Material");

            var newMats = Enumerable.Repeat(TargetMaterial, mats.Length).ToArray();
            r.sharedMaterials = newMats;

            EditorUtility.SetDirty(r);
            ChangedRenderers++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        // Пометить сцену dirty, чтобы изменения сохранились
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.IsValid())
            EditorSceneManager.MarkSceneDirty(activeScene);

        EditorUtility.DisplayDialog(
            "Replace Material",
            $"Selected roots: {SelectedRoots}\nFound renderers: {FoundRenderers}\nChanged renderers: {ChangedRenderers}",
            "OK"
        );
    }
}
#endif
