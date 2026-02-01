using System.Collections.Generic;
using Mask;
using Objectives;
using Settings;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

public class PlayerEffect : MonoBehaviour
{
    [Header("Rules")] [SerializeField, TableList] private List<TrailEffectPlayer> _effects = new();

    private LevelManager _levelManager;
    private MaskManager _maskManager;

    [Inject]
    private void Construct(LevelManager levelManager, MaskManager maskManager)
    {
        _levelManager = levelManager;
        _maskManager = maskManager;
    }

    private void Awake()
    {
        foreach (var e in _effects)
            e?.BuildCache();
    }

    private void OnEnable()
    {
        _levelManager.LevelRestarted += OnLevelRestarted;
        _levelManager.LevelChanged += OnLevelRestarted;
        _maskManager.OnMaskEquip += OnMaskEquip;
        _maskManager.OnMaskUnequip += OnMaskUnequip;
    }

    private void OnDisable()
    {
        _levelManager.LevelChanged -= OnLevelRestarted;
        _levelManager.LevelRestarted -= OnLevelRestarted;
        _maskManager.OnMaskEquip -= OnMaskEquip;
        _maskManager.OnMaskUnequip -= OnMaskUnequip;
    }

    private void OnLevelRestarted()
    {
        foreach (var e in _effects)
            e?.ForceOff();
    }

    private void OnMaskEquip(Enums.MaskType maskType)
    {
        foreach (var e in _effects)
            e?.TryEquip(maskType);
    }

    private void OnMaskUnequip(Enums.MaskType maskType)
    {
        foreach (var e in _effects)
            e?.TryUnequip(maskType);
    }
}