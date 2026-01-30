using System;
using System.Collections.Generic;
using DG.Tweening;
using Mask;
using Settings;
using UnityEngine;
using Zenject;

public class WorldVisibilityController : MonoBehaviour
{
    [SerializeField] private List<Collider> _colliders;
    [SerializeField] private List<Material> _maskMaterials;
    private MaskManager _maskManager;
    [SerializeField] private Enums.MaskType _maskType =  Enums.MaskType.Intelligence;
    [SerializeField] private float _dissolveDuration = 0.5f;
    private Tweener _dissolveTween;
    private const string DissolveProp = "_Dissolve";
    private float _currentDissolve = 1f;
    
    [Inject]
    private void Construct(MaskManager maskManager)
    {
        _maskManager =  maskManager;
    }

    private void Awake()
    {
        DisableObjects();
    }

    private void OnEnable()
    {
        _maskManager.OnMaskEquip += OnMaskEquip;
        _maskManager.OnMaskUnequip += OnMaskUnequip;
    }
    
    private void OnDisable()
    {
        _maskManager.OnMaskEquip -= OnMaskEquip;
        _maskManager.OnMaskUnequip -= OnMaskUnequip;
        _dissolveTween?.Kill();
    }
    
    private void OnMaskEquip(Enums.MaskType maskType)
    {
        if (_maskType == maskType)
        {
            EnableObjects();
        }
    }
    
    private void OnMaskUnequip(Enums.MaskType maskType)
    {
        if (_maskType == maskType)
        {
            DisableObjects();
        }
    }

    private void EnableObjects()
    {
        foreach (var col in _colliders)
            if (col) col.gameObject.SetActive(true);

        TweenDissolve(to: 0f, ease: Ease.OutBack, onComplete: null);
    }

    private void DisableObjects()
    {
        foreach (var col in _colliders)
            if (col) col.gameObject.SetActive(false);
        
        TweenDissolve(to: 1f, ease: Ease.OutBack, onComplete: null);
    }

    private void TweenDissolve(float to, Ease ease, TweenCallback onComplete)
    {
        _dissolveTween?.Kill();
        _dissolveTween = null;

        float from = _currentDissolve;
        
        from = ReadCurrentDissolveOrDefault(from);
        _currentDissolve = from;

        _dissolveTween = DOTween.To(
                () => _currentDissolve,
                x =>
                {
                    _currentDissolve = x;
                    ApplyDissolve(_currentDissolve);
                },
                to,
                _dissolveDuration
            )
            .SetEase(ease)
            .SetUpdate(false)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .OnComplete(onComplete);
    }

    private void ApplyDissolve(float v)
    {
        foreach (var m in _maskMaterials)
        {
            if (!m) continue;
            if (m.HasProperty(DissolveProp))
                m.SetFloat(DissolveProp, v);
        }
    }

    private float ReadCurrentDissolveOrDefault(float fallback)
    {
        foreach (var m in _maskMaterials)
        {
            if (!m) continue;
            if (m.HasProperty(DissolveProp))
                return m.GetFloat(DissolveProp);
        }

        return fallback;
    }
}

