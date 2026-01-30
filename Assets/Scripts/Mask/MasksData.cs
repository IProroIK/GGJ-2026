using System;
using System.Collections.Generic;
using Settings;
using UnityEngine;

namespace Mask
{
    [CreateAssetMenu(fileName = "MaskData", menuName = "Scriptables/MaskData")]
    public class MasksData : ScriptableObject
    {
        public List<MaskModel> Masks;
    }

    [Serializable]
    public class MaskModel
    {
        public Enums.MaskType MaskType;
        public Sprite MaskSprite;
        public string MaskName;
        public string MaskDescription;
    }
    
}