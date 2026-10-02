// Advanced Dissolve <https://u3d.as/16cX>
// Copyright (c) Amazing Assets <https://amazingassets.world>

using UnityEditor;
using UnityEngine;
using UnityEditor.Rendering.Universal;


namespace AmazingAssets.AdvancedDissolve.Editor.ShaderGraph
{
    // Used for ShaderGraph Unlit shaders
    public class ExtendedShaderGraphUnlitGUI : ExtendedBaseShaderGUI
    {
        MaterialProperty[] properties;

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            // save off the list of all properties for shadergraph
            this.properties = properties;

            base.FindProperties(properties);
        }

        public static void UpdateMaterial(Material material, ExtendedShaderUtils.MaterialUpdateType updateType)
        {
            bool automaticRenderQueue = GetAutomaticQueueControlSetting(material);
            ExtendedBaseShaderGUI.UpdateMaterialSurfaceOptions(material, automaticRenderQueue);
			ExtendedBaseShaderGUI.UpdateMotionVectorKeywordsAndPass(material);
        }

        public override void ValidateMaterial(Material material)
        {
            UpdateMaterial(material, ExtendedShaderUtils.MaterialUpdateType.ModifiedMaterial);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            DrawShaderGraphProperties(material, properties);
        }

        public override void DrawAdvancedOptions(Material material)
        {
            // Always show the queue control field.  Only show the render queue field if queue control is set to user override
            DoPopup(Styles.queueControl, queueControlProp, Styles.queueControlNames);
            if (material.HasProperty(Property.QueueControl) && material.GetFloat(Property.QueueControl) == (float)QueueControl.UserOverride)
                materialEditor.RenderQueueField();
            base.DrawAdvancedOptions(material);
            materialEditor.DoubleSidedGIField();
        }
    }
}