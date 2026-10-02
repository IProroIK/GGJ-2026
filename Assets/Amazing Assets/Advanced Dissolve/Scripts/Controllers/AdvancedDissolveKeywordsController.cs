// Advanced Dissolve <https://u3d.as/16cX>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using UnityEngine;


namespace AmazingAssets.AdvancedDissolve
{
    [ExecuteAlways]
    public class AdvancedDissolveKeywordsController : AdvancedDissolveController
    {
        public AdvancedDissolveKeywords.State state;
        int previousState = -1;

        public AdvancedDissolveKeywords.CutoutStandardSource cutoutStandardSource;
        int previousCutoutStandardSource = -1;

        public AdvancedDissolveKeywords.CutoutStandardSourceMapsMappingType cutoutStandardSourceMapsMappingType;
        int previousCutoutStandardSourceMapsMappingType = -1;

        public AdvancedDissolveKeywords.CutoutGeometricType cutoutGeometricType;
        int previousCutoutGeometricType = -1;

        public AdvancedDissolveKeywords.CutoutGeometricCount cutoutGeometricCount;
        int previousCutoutGeometricCount = -1;

        public AdvancedDissolveKeywords.EdgeBaseSource edgeBaseSource;
        int previousEdgeBaseSource = -1;

        public AdvancedDissolveKeywords.EdgeAdditionalColorSource edgeAdditionalColorSource;
        int previousEdgeAdditionalColorSource = -1;

        public AdvancedDissolveKeywords.EdgeUVDistortionSource edgeUVDistortionSource;
        int previousEdgeUVDistortionSource = -1;

        //public AdvancedDissolve.Keywords.GlobalControlID globalControlID;  // defined in base class
        int previousGlobalControlID = -1;



        protected override void Awake()
        {
            base.Awake();

            ForceUpdateShaderData();
        }
        protected override void Update()
        {
            base.Update();

            if (materials == null)
                return;


            if (previousState != (int)state)
            {
                previousState = (int)state;
                AdvancedDissolveKeywords.SetKeyword(materials, state, true);
            }

            if(previousCutoutStandardSource != (int)cutoutStandardSource)
            {
                previousCutoutStandardSource = (int)cutoutStandardSource;
                AdvancedDissolveKeywords.SetKeyword(materials, cutoutStandardSource, true);
            }

            if(previousCutoutStandardSourceMapsMappingType != (int)cutoutStandardSourceMapsMappingType)
            {
                previousCutoutStandardSourceMapsMappingType = (int)cutoutStandardSourceMapsMappingType;
                AdvancedDissolveKeywords.SetKeyword(materials, cutoutStandardSourceMapsMappingType, true);
            }

            if(previousCutoutGeometricType != (int)cutoutGeometricType)
            {
                previousCutoutGeometricType = (int)cutoutGeometricType;
                AdvancedDissolveKeywords.SetKeyword(materials, cutoutGeometricType, true);
            }

            if(previousCutoutGeometricCount != (int)cutoutGeometricCount)
            {
                previousCutoutGeometricCount = (int)cutoutGeometricCount;
                AdvancedDissolveKeywords.SetKeyword(materials, cutoutGeometricCount, true);
            }

            if(previousEdgeBaseSource != (int)edgeBaseSource)
            {
                previousEdgeBaseSource = (int)edgeBaseSource;
                AdvancedDissolveKeywords.SetKeyword(materials, edgeBaseSource, true);
            }

            if(previousEdgeAdditionalColorSource != (int)edgeAdditionalColorSource)
            {
                previousEdgeAdditionalColorSource = (int)edgeAdditionalColorSource;
                AdvancedDissolveKeywords.SetKeyword(materials, edgeAdditionalColorSource, true);
            }

            if(previousEdgeUVDistortionSource != (int)edgeUVDistortionSource)
            {
                previousEdgeUVDistortionSource = (int)edgeUVDistortionSource;
                AdvancedDissolveKeywords.SetKeyword(materials, edgeUVDistortionSource, true);
            }

            if(previousGlobalControlID != (int)globalControlID)
            {
                previousGlobalControlID = (int)globalControlID;
                AdvancedDissolveKeywords.SetKeyword(materials, globalControlID, true);
            }
        }


        [ContextMenu("Force Update Keywords Controller")]
        override public void ForceUpdateShaderData()
        {
            previousState = -1;
            previousCutoutStandardSource = -1;
            previousCutoutStandardSourceMapsMappingType = -1;
            previousCutoutGeometricType = -1;
            previousCutoutGeometricCount = -1;
            previousEdgeBaseSource = -1;
            previousEdgeAdditionalColorSource = -1;
            previousEdgeUVDistortionSource = -1;
            previousGlobalControlID = -1;
        }

        [ContextMenu("Reset Keywords Controller")]
        override public void ResetShaderData()
        {

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Reset Keywords Controller");
#endif
            cutoutStandardSource = (AdvancedDissolveKeywords.CutoutStandardSource)0;
            cutoutStandardSourceMapsMappingType = (AdvancedDissolveKeywords.CutoutStandardSourceMapsMappingType)0;
            cutoutGeometricType = (AdvancedDissolveKeywords.CutoutGeometricType)0;
            cutoutGeometricCount = (AdvancedDissolveKeywords.CutoutGeometricCount)0;
            edgeBaseSource = (AdvancedDissolveKeywords.EdgeBaseSource)0;
            edgeAdditionalColorSource = (AdvancedDissolveKeywords.EdgeAdditionalColorSource)0;
            edgeUVDistortionSource = (AdvancedDissolveKeywords.EdgeUVDistortionSource)0;
            globalControlID = (AdvancedDissolveKeywords.GlobalControlID)0;


            ForceUpdateShaderData();
        }
    }
}
