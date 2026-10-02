// Advanced Dissolve <https://u3d.as/16cX>
// Copyright (c) Amazing Assets <https://amazingassets.world>

using UnityEngine;
using UnityEditor;


namespace AmazingAssets.AdvancedDissolve.Editor.ShaderGraph
{
    class ShaderGraphLitGUI : ExtendedShaderGraphLitGUI
    {
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);

            //AmazingAssets
            AmazingAssets.AdvancedDissolve.Editor.AdvancedDissolveMaterialProperties.Init(properties);
        }

        public override void DrawAdvancedDissolveOptions(Material material)
        {
            AmazingAssets.AdvancedDissolve.Editor.AdvancedDissolveMaterialProperties.DrawDissolveOptions(false, materialEditor, true, true, false, true, true, true);
        }

        public override void ValidateMaterial(Material material)
        {
            base.ValidateMaterial(material);

            AmazingAssets.AdvancedDissolve.AdvancedDissolveKeywords.Reload(material);
        }
    }
}