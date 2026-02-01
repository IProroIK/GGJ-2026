using System;
using System.Collections.Generic;
using DG.Tweening;
using Mask;
using Objectives;
using Settings;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

public class ColorByMask : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private SkinnedMeshRenderer _renderer;
    [SerializeField] private int _materialIndex = 0;

    [Header("Shader (Replace1)")]
    [SerializeField] private bool _enableReplace = true;
    [SerializeField] private bool _enableReplaceSlot = true;
    [SerializeField] private Color _fromColor = Color.red;
    [SerializeField] private Color _defaultToColor = Color.red;
    [Range(0f, 1f)] [SerializeField] private float _defaultRange = 0.1f;

    [Header("Tween")]
    [SerializeField] private float _tweenDuration = 0.25f;
    [SerializeField] private Ease _ease = Ease.OutQuad;

    [Header("Rules")]
    [SerializeField] private MaskColorRule[] _rules;

    private MaskManager _maskManager;
    private Material _material;

    private readonly Dictionary<Enums.MaskType, MaskColorRule> _map = new();

    private Tween _colorTween;
    private Tween _rangeTween;
    private LevelManager _levelManager;

    // shader ids
    private static readonly int EnableReplaceId  = Shader.PropertyToID("_EnableReplace");
    private static readonly int EnableReplace1Id = Shader.PropertyToID("_EnableReplace1");
    private static readonly int ColorFrom1Id     = Shader.PropertyToID("_ColorFrom1");
    private static readonly int ColorTo1Id       = Shader.PropertyToID("_ColorTo1");
    private static readonly int Range1Id         = Shader.PropertyToID("_Range1");

    [Inject]
    private void Construct(MaskManager maskManager, LevelManager levelManager)
    {
        _levelManager = levelManager;
        _maskManager = maskManager;
    }

    private void Awake()
    {
        BuildCache();

        if (_renderer != null)
            _material = _renderer.materials[_materialIndex];

        ApplyImmediate(_defaultToColor, _defaultRange);
    }

    private void OnEnable()
    {
        _levelManager.LevelChanged += OnRestart;
        _levelManager.LevelRestarted += OnRestart;
        _maskManager.OnMaskEquip += OnMaskEquip;
        _maskManager.OnMaskUnequip += OnMaskUnequip;
    }
    
    private void OnDisable()
    {
        _levelManager.LevelChanged -= OnRestart;
        _levelManager.LevelRestarted -= OnRestart;
        _maskManager.OnMaskEquip -= OnMaskEquip;
        _maskManager.OnMaskUnequip -= OnMaskUnequip;

        KillTweens();
    }
    
    private void OnRestart()
    {
        OnMaskUnequip(Enums.MaskType.None);
    }

    [Button]
    private void BuildCache()
    {
        _map.Clear();
        if (_rules == null) return;

        for (int i = 0; i < _rules.Length; i++)
            _map[_rules[i].maskType] = _rules[i];
    }

    private void OnMaskEquip(Enums.MaskType maskType)
    {
        if (!_map.TryGetValue(maskType, out var rule))
            return;

        TweenTo(rule.colorTo, rule.range);
    }

    private void OnMaskUnequip(Enums.MaskType maskType)
    {
        if (!_map.ContainsKey(maskType))
            return;

        TweenTo(_defaultToColor, _defaultRange);
    }

    private void TweenTo(Color toColor, float toRange)
    {
        if (_material == null)
            return;

        KillTweens();

        Color startColor = _material.GetColor(ColorTo1Id);
        float startRange = _material.GetFloat(Range1Id);

        _colorTween = DOTween.To(
            () => startColor,
            c =>
            {
                startColor = c;
                Apply(c, startRange);
            },
            toColor,
            _tweenDuration
        ).SetEase(_ease);

        _rangeTween = DOTween.To(
            () => startRange,
            r =>
            {
                startRange = r;
                Apply(startColor, r);
            },
            Mathf.Clamp01(toRange),
            _tweenDuration
        ).SetEase(_ease);
    }

    private void ApplyImmediate(Color toColor, float range)
    {
        if (_material == null) return;
        Apply(toColor, range);
    }

    private void Apply(Color toColor, float range)
    {
        _material.SetFloat(EnableReplaceId, _enableReplace ? 1f : 0f);
        _material.SetFloat(EnableReplace1Id, _enableReplaceSlot ? 1f : 0f);
        _material.SetColor(ColorFrom1Id, _fromColor);
        _material.SetColor(ColorTo1Id, toColor);
        _material.SetFloat(Range1Id, Mathf.Clamp01(range));
    }

    private void KillTweens()
    {
        _colorTween?.Kill();
        _rangeTween?.Kill();
        _colorTween = null;
        _rangeTween = null;
    }
}
