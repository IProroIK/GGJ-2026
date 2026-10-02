// Advanced Dissolve <https://u3d.as/16cX>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using UnityEngine;


namespace AmazingAssets.AdvancedDissolve
{
    [ExecuteAlways]
    public class AdvancedDissolvePropertiesController : AdvancedDissolveController
    {
        public enum UpdateMode { OnAwake, OnFixedUpdate, EveryFrame, Manual }


        public UpdateMode updateMode = UpdateMode.EveryFrame;

        public AdvancedDissolveProperties.Cutout.Standard cutoutStandard = new AdvancedDissolveProperties.Cutout.Standard();
        public AdvancedDissolveProperties.Cutout.Geometric cutoutGeometric = new AdvancedDissolveProperties.Cutout.Geometric();

        public AdvancedDissolveProperties.Edge.Base edgeBase = new AdvancedDissolveProperties.Edge.Base();
        public AdvancedDissolveProperties.Edge.AdditionalColor edgeAdditionalColor = new AdvancedDissolveProperties.Edge.AdditionalColor();
        public AdvancedDissolveProperties.Edge.UVDistortion edgeUVDistortion = new AdvancedDissolveProperties.Edge.UVDistortion();
        public AdvancedDissolveProperties.Edge.GlobalIllumination edgeGlobalIllumination = new AdvancedDissolveProperties.Edge.GlobalIllumination();



        protected override void Awake()
        {
            base.Awake();
        
            if (updateMode == UpdateMode.OnAwake)
                UpdateShaderData();
        }
        protected override void Update()
        {
            base.Update();

            if (updateMode == UpdateMode.EveryFrame || (updateMode == UpdateMode.OnFixedUpdate && Application.isEditor))
                UpdateShaderData();            
        }
        void FixedUpdate()
        {
            if (updateMode == UpdateMode.OnFixedUpdate)
                UpdateShaderData();
        }


        [ContextMenu("Force Update Properties Controller")]
        public override void ForceUpdateShaderData()
        {
            UpdateShaderData();
        }

        void UpdateShaderData()
        {
            if (materials == null)
                return;


            if (globalControlID == AdvancedDissolveKeywords.GlobalControlID.None)
            {
                cutoutStandard.UpdateLocal(materials);
                edgeBase.UpdateLocal(materials);
                edgeAdditionalColor.UpdateLocal(materials);
                edgeUVDistortion.UpdateLocal(materials);
                edgeGlobalIllumination.UpdateLocal(materials);
            }
            else
            {
                cutoutStandard.UpdateGlobal(globalControlID);
                edgeBase.UpdateGlobal(globalControlID);
                edgeAdditionalColor.UpdateGlobal(globalControlID);
                edgeUVDistortion.UpdateGlobal(globalControlID);
                edgeGlobalIllumination.UpdateGlobal(globalControlID);
            }
        }

        [ContextMenu("Reset Properties Controller")]
        override public void ResetShaderData()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Reset Properties Controller");
#endif
            cutoutStandard = new AdvancedDissolveProperties.Cutout.Standard();
            edgeBase = new AdvancedDissolveProperties.Edge.Base();
            edgeAdditionalColor = new AdvancedDissolveProperties.Edge.AdditionalColor();
            edgeUVDistortion = new AdvancedDissolveProperties.Edge.UVDistortion();
            edgeGlobalIllumination = new AdvancedDissolveProperties.Edge.GlobalIllumination();

            UpdateShaderData();
        }
    }
}
