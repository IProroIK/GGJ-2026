// Advanced Dissolve <https://u3d.as/16cX>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
namespace AmazingAssets.AdvancedDissolve.Editor
{
    class AdvancedDissolveKeywordCutoutGeometricCountDrawer : AdvancedDissolveKeywordsDrawer
	{
		public override void EnumToKeywords(out string[] labels, out string[] keywords)
		{
			labels = System.Enum.GetNames(typeof(AdvancedDissolve.AdvancedDissolveKeywords.CutoutGeometricCount));
			keywords = new string[labels.Length];

			for (int i = 0; i < labels.Length; i++)
			{
				keywords[i] = AdvancedDissolve.AdvancedDissolveKeywords.ToString((AdvancedDissolve.AdvancedDissolveKeywords.CutoutGeometricCount)i);
			}
		}
	}
}
