using System;
using System.Collections.Generic;
using Settings;
using UnityEngine;
using UnityEngine.Events;

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