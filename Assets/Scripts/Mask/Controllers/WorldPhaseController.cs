using System.Collections.Generic;
using DG.Tweening;
using Mask;
using Settings;
using UnityEngine;
using Zenject;

public class WorldPhaseController : MonoBehaviour
{
    [SerializeField] private List<Collider> _colliders;
    [SerializeField] private List<Material> _materials;

    [SerializeField] private Enums.MaskType _maskType = Enums.MaskType.Shadow;
    [SerializeField] private float _phaseDuration = 0.5f;

    private MaskManager _maskManager;

    private Tweener _phaseTween;
    private float _currentPhase;

    private const string PhaseProp = "_Phase";

    [Inject]
    private void Construct(MaskManager maskManager)
    {
        _maskManager = maskManager;
    }

    private void Awake()
    {
        SetCollidersEnabled(true);
        _currentPhase = ReadCurrentPhaseOrDefault(0f);
        ApplyPhase(_currentPhase);
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
        _phaseTween?.Kill();
    }

    private void OnDestroy()
    {
        _currentPhase = 0f;
        ApplyPhase(0f);
    }

    private void OnMaskEquip(Enums.MaskType maskType)
    {
        if (maskType != _maskType)
            return;

        SetCollidersEnabled(false);

        TweenPhase(to: 1f, ease: Ease.OutSine, onComplete: null);
    }

    private void OnMaskUnequip(Enums.MaskType maskType)
    {
        if (maskType != _maskType)
            return;
        
        TweenPhase(
            to: 0f,
            ease: Ease.OutSine,
            onComplete: () => SetCollidersEnabled(true)
        );
    }

    private void TweenPhase(float to, Ease ease, TweenCallback onComplete)
    {
        _phaseTween?.Kill();
        _phaseTween = null;

        float from = _currentPhase;
        from = ReadCurrentPhaseOrDefault(from);
        _currentPhase = from;

        _phaseTween = DOTween.To(
                () => _currentPhase,
                x =>
                {
                    _currentPhase = x;
                    ApplyPhase(_currentPhase);
                },
                to,
                _phaseDuration
            )
            .SetEase(ease)
            .SetUpdate(false)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .OnComplete(onComplete);
    }

    private void ApplyPhase(float v)
    {
        foreach (var m in _materials)
        {
            if (!m) continue;

            if (m.HasProperty(PhaseProp))
                m.SetFloat(PhaseProp, v);
        }
    }

    private float ReadCurrentPhaseOrDefault(float fallback)
    {
        foreach (var m in _materials)
        {
            if (!m) continue;

            if (m.HasProperty(PhaseProp))
                return m.GetFloat(PhaseProp);
        }

        return fallback;
    }

    private void SetCollidersEnabled(bool enabled)
    {
        foreach (var c in _colliders)
        {
            if (!c) continue;
            c.enabled = enabled;
        }
    }
}
