// Advanced Dissolve <https://u3d.as/16cX>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using UnityEngine;
using UnityEditor;


namespace AmazingAssets.AdvancedDissolve.Editor
{
    class AdvancedDissolveColorRGBDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, string label, UnityEditor.MaterialEditor editor)
        {            
            Color color = prop.colorValue;

            EditorGUI.BeginChangeCheck();
            color = EditorGUI.ColorField(position, new GUIContent(label), color, true, false, false);
            if (EditorGUI.EndChangeCheck())
            {
                prop.colorValue = color;
            }
        }
    }
}
