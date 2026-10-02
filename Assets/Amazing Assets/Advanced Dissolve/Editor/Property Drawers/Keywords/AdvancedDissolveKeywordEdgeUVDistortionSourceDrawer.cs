// Advanced Dissolve <https://u3d.as/16cX>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
namespace AmazingAssets.AdvancedDissolve.Editor
{
    class AdvancedDissolveKeywordEdgeUVDistortionSourceDrawer : AdvancedDissolveKeywordsDrawer
	{
        public override void EnumToKeywords(out string[] labels, out string[] keywords)
        {
			labels = System.Enum.GetNames(typeof(AdvancedDissolve.AdvancedDissolveKeywords.EdgeUVDistortionSource));
			keywords = new string[labels.Length];

			for (int i = 0; i < labels.Length; i++)
			{
				labels[i] = EnumStringToUnityStyle(labels[i]) + (i == 0 ? string.Empty : " (RG)");
				keywords[i] = AdvancedDissolve.AdvancedDissolveKeywords.ToString((AdvancedDissolve.AdvancedDissolveKeywords.EdgeUVDistortionSource)i);
			}
		}
    }
}
