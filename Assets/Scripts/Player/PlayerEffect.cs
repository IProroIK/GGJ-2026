using System;
using System.Collections.Generic;
using Mask;
using Objectives;
using Settings;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

public class PlayerEffect : MonoBehaviour
{
    [Header("Rules")] [SerializeField] private List<TrailEffectPlayer> _effects = new();

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
        _maskManager.OnMaskEquip += OnMaskEquip;
        _maskManager.OnMaskUnequip += OnMaskUnequip;
    }

    private void OnDisable()
    {
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

[Serializable]
public class TrailEffectPlayer
{
    [Header("Triggers")]
    [SerializeField] private Enums.MaskType[] _maskTypes = { Enums.MaskType.Intelligence };

    [Header("Events")]
    [SerializeField] private UnityEvent OnEquip;
    [SerializeField] private UnityEvent OnUnequip;

    [Header("Options")]
    [SerializeField] private bool _forceOffOnLevelRestart = true;
    
    [NonSerialized] private HashSet<Enums.MaskType> _maskSet;

    public void BuildCache()
    {
        _maskSet = new HashSet<Enums.MaskType>();
        
        if (_maskTypes == null) 
            return;

        foreach (var t in _maskTypes)
            _maskSet.Add(t);
    }

    public void TryEquip(Enums.MaskType maskType)
    {
        if (!IsAllowed(maskType)) return;
        OnEquip?.Invoke();
    }

    public void TryUnequip(Enums.MaskType maskType)
    {
        if (!IsAllowed(maskType)) return;
        OnUnequip?.Invoke();
    }

    public void ForceOff()
    {
        if (!_forceOffOnLevelRestart) return;
        OnUnequip?.Invoke();
    }

    private bool IsAllowed(Enums.MaskType maskType)
    {
        if (_maskSet == null) BuildCache();
        return _maskSet != null && _maskSet.Contains(maskType);
    }
}
