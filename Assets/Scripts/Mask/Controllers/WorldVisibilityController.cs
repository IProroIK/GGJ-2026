using System;
using System.Collections.Generic;
using Mask;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

public class WorldVisibilityController : MonoBehaviour
{
    [SerializeField, TableList(ShowIndexLabels = true)] private List<WorldVisibilityObject> _worldVisibilityObjects;
    private MaskManager _maskManager;

    [Inject]
    private void Construct(MaskManager maskManager)
    {
        _maskManager =  maskManager;
    }
}

[Serializable]
public struct WorldVisibilityObject
{
    public GameObject gameObject;
    public MeshRenderer meshRenderer;
}
