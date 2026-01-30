using System;
using Mask;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game Config")]
public class GameConfig : ScriptableObject
{
    [field: SerializeField] public GamePlayVariablesEditor GamePlay { get; private set; } = new GamePlayVariablesEditor();
}



[Serializable]
public class GamePlayVariablesEditor
{
    [field: SerializeField, FoldoutGroup("Delays")] public float ChangeStateToTopGame { get; private set; } = 1f;
    [field: SerializeField, FoldoutGroup("Config")] public MasksData MaskData { get; private set; }
}
