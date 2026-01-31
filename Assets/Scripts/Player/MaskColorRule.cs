using System;
using Settings;
using UnityEngine;

[Serializable]
public struct MaskColorRule
{
    public Enums.MaskType maskType;
    public Color colorTo;
    [Range(0f, 1f)] public float range;
}