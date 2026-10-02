// Advanced Dissolve <https://u3d.as/16cX>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;


namespace AmazingAssets.AdvancedDissolve.Editor
{
    public class ShaderGenerator : UnityEditor.Editor
    {
        static public class Enum
        {
            public enum ConversionState { Failed, Success, Skip };
            public enum RenderPipeline { Unknown, BuiltIn, Universal, HighDefinition }
            public enum ShaderPass { Unknown, Pass, UniversalForward, ShadowCaster, META, Meta, ScenePickingPass, SceneSelectionPass, DepthOnly, DepthForwardOnly, DepthNormals, DepthNormalsOnly, GBuffer, MotionVectors, Forward, ForwardOnly, FullScreenDebug, IndirectDXR, VisibilityDXR, ForwardDXR, GBufferDXR, PathTracingDXR, RayTracingPrepass, TransparentDepthPrepass, Universal2D, BuiltInForward, BuiltInForwardAdd, BuiltInDeferred }
        }

        static string thisAssetPath;


        [MenuItem("Assets/Amazing Assets/Advanced Dissolve/Generate Shader", false, 4201)]
        static public void Menu()
        {
            if (Selection.objects == null || Selection.objects.Length == 0)
                return;


            SortedDictionary<string, int> statisticsExtenstions = new SortedDictionary<string, int>();
            List<string> statisticsSuccess = new List<string>();
            List<string> statisticsSkipped = new List<string>();
            List<string> statisticsFailure = new List<string>();

            foreach (var item in Selection.objects)
            {
                string assetPath = AssetDatabase.GetAssetPath(item);
                string extension = Path.GetExtension(assetPath).ToLowerInvariant();
                if (statisticsExtenstions.ContainsKey(extension) == false) statisticsExtenstions.Add(extension, 0);
                statisticsExtenstions[extension] += 1;


                switch (Generate(item))
                {
                    case Enum.ConversionState.Failed: statisticsFailure.Add(assetPath); break;
                    case Enum.ConversionState.Success: statisticsSuccess.Add(assetPath); break;
                    case Enum.ConversionState.Skip: statisticsSkipped.Add(assetPath); break;

                    default:
                        break;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

                        
            #region Statistics      
            StackTraceLogType saveLog = Application.GetStackTraceLogType(LogType.Log);
            StackTraceLogType saveWarning = Application.GetStackTraceLogType(LogType.Warning);
            StackTraceLogType saveError = Application.GetStackTraceLogType(LogType.Error);
            
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);


            if (Selection.objects.Length > 1)
            {
                string str = ($"Files checked: {statisticsExtenstions.Sum(c => c.Value)}");
                foreach (var item in statisticsExtenstions)
                {
                    str += ($"\n{item.Key}: {item.Value}");
                }
                Debug.Log(str);
            }


            if (statisticsSuccess.Count > 0)
            {
                string str = $"Files updated: {statisticsSuccess.Count}";
                foreach (var group in statisticsSuccess.GroupBy(c => Path.GetExtension(c)).Select(c => c.ToList()).ToList())
                {
                    string currentStr = $"\n\n{Path.GetExtension(group[0])}: {group.Count}";
                    foreach (var item in group)
                    {
                        currentStr += $"\n{item}";
                    }
                    str += currentStr;
                }
                Debug.Log(str);
            }

            if (statisticsSkipped.Count > 0)
            {
                string str = $"Files skipped: {statisticsSkipped.Count}";
                foreach (var group in statisticsSkipped.GroupBy(c => Path.GetExtension(c)).Select(c => c.ToList()).ToList())
                {
                    string currentStr = $"\n\n{Path.GetExtension(group[0])}: {group.Count}";
                    foreach (var item in group)
                    {
                        currentStr += $"\n{item}";
                    }
                    str += currentStr;
                }
                Debug.LogWarning(str);
            }

            if (statisticsFailure.Count > 0)
            {
                string str = $"Files failed: {statisticsFailure.Count}";
                foreach (var group in statisticsFailure.GroupBy(c => Path.GetExtension(c)).Select(c => c.ToList()).ToList())
                {
                    string currentStr = $"\n\n{Path.GetExtension(group[0])}: {group.Count}";
                    foreach (var item in group)
                    {
                        currentStr += $"\n{item}";
                    }
                    str += currentStr;
                }
                Debug.LogError(str);
            }


            Application.SetStackTraceLogType(LogType.Log, saveLog);
            Application.SetStackTraceLogType(LogType.Warning, saveWarning);
            Application.SetStackTraceLogType(LogType.Error, saveError);
            #endregion
        }

        [MenuItem("Assets/Amazing Assets/Advanced Dissolve/Generate Shader", true, 4201)]
        static public bool Validate_Menu()
        {
            if (Selection.objects == null || Selection.objects.Length == 0)
                return false;

            foreach (var item in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(item);
                if (string.IsNullOrEmpty(path) == false && Path.GetExtension(path).ToLowerInvariant() == ".shadergraph")
                    return true;
            }

            return false;
        }

        static bool IsAssetReady(UnityEngine.Object obj)
        {
            if (obj == null)
                return false;


            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                return false;

            if (Path.GetExtension(path).ToLowerInvariant() != ".shadergraph")
                return false;

            Shader shader = (Shader)AssetDatabase.LoadAssetAtPath(path, typeof(Shader));
            if (shader == null)
                return false;

            return true;
        }


        static Enum.ConversionState Generate(UnityEngine.Object sourceShaderAsset)
        {
            if (IsAssetReady(sourceShaderAsset) == false)
                return Enum.ConversionState.Skip;


            string sourceShaderGraphAssetPath = AssetDatabase.GetAssetPath(sourceShaderAsset);
            string destinationShaderAssetPath = Path.Combine(Path.GetDirectoryName(sourceShaderGraphAssetPath), Path.GetFileNameWithoutExtension(sourceShaderGraphAssetPath) + ".shader");


            string hlslCode = GetShaderGraphHLSLCode(sourceShaderGraphAssetPath);
            if (hlslCode.Contains("AdvancedDissolveShaderGraphFunction") == false)
                return Enum.ConversionState.Failed;

            //Write hlsl into a file
            CreateShaderAssetFile(destinationShaderAssetPath, new List<string> { hlslCode });

            List<string> newShaderFile = File.ReadAllLines(destinationShaderAssetPath).ToList();

            //Add copyright banner
            newShaderFile.Insert(0, string.Empty);
            newShaderFile.Insert(0, "// Copyright (c) Amazing Assets <https://amazingassets.world>");
            newShaderFile.Insert(0, "// Advanced Dissolve <https://u3d.as/16cX>");


            //1) Change shader Name
            //2) Add properties
            //3) Add shader code
            //4) Add custom editor            
            //5) Save



            //1
            if (ChangeShaderName(sourceShaderGraphAssetPath, newShaderFile) == false)
                return Enum.ConversionState.Failed;

            //2
            if (AddProperties(newShaderFile, AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(sourceShaderAsset))) == false)
                return Enum.ConversionState.Failed;

            //3
            if (AddShaderCode(newShaderFile) == false)
                return Enum.ConversionState.Failed;

            //4
            AddCustomEditor(newShaderFile);

            //5
            CreateShaderAssetFile(destinationShaderAssetPath, newShaderFile);


            return Enum.ConversionState.Success;
        }


        static string GetCustomEditorName(string defaultCustomEditor)
        {
            switch (GetProjectRenderPipeline())
            {
                case Enum.RenderPipeline.BuiltIn:
                    {
                        if (defaultCustomEditor.Contains("UnityEditor.Rendering.BuiltIn.ShaderGraph."))
                            return defaultCustomEditor.Replace("UnityEditor.Rendering.BuiltIn.ShaderGraph.", "AmazingAssets.AdvancedDissolve.Editor.ShaderGraph.");
                    }
                    break;

                case Enum.RenderPipeline.Universal:
                    {
                        if (defaultCustomEditor.Contains("ShaderGraphUnlitGUI"))
                            return "CustomEditorForRenderPipeline \"AmazingAssets.AdvancedDissolve.Editor.ShaderGraph.ShaderGraphUnlitGUI\" \"UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset\"";
                        else if (defaultCustomEditor.Contains("ShaderGraphLitGUI"))
                            return "CustomEditorForRenderPipeline \"AmazingAssets.AdvancedDissolve.Editor.ShaderGraph.ShaderGraphLitGUI\" \"UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset\"";
                    }
                    break;

                case Enum.RenderPipeline.HighDefinition:
                    {
                        if (defaultCustomEditor.Contains("Rendering.HighDefinition.HDUnlitGUI"))
                            return defaultCustomEditor.Replace("Rendering.HighDefinition.HDUnlitGUI", "AmazingAssets.AdvancedDissolve.Editor.ShaderGraph.ShaderGraphGUI");

                        if (defaultCustomEditor.Contains("Rendering.HighDefinition.DecalShaderGraphGUI"))
                            return defaultCustomEditor.Replace("Rendering.HighDefinition.DecalShaderGraphGUI", "AmazingAssets.AdvancedDissolve.Editor.ShaderGraph.ShaderGraphGUI");

                        if (defaultCustomEditor.Contains("Rendering.HighDefinition.LightingShaderGraphGUI"))
                            return defaultCustomEditor.Replace("Rendering.HighDefinition.LightingShaderGraphGUI", "AmazingAssets.AdvancedDissolve.Editor.ShaderGraph.ShaderGraphGUI");

                        if (defaultCustomEditor.Contains("Rendering.HighDefinition.LitShaderGraphGUI"))
                            return defaultCustomEditor.Replace("Rendering.HighDefinition.LitShaderGraphGUI", "AmazingAssets.AdvancedDissolve.Editor.ShaderGraph.ShaderGraphGUI");
                    }
                    break;

                default: break;
                
            }


            Debug.LogError("Undefined custom material editor.\n");

            return string.Empty;

        }


        static bool ChangeShaderName(string sourceShaderAssetPath, List<string> newShaderFile)
        {
            //Get source shader name
            string originalName = Path.GetFileNameWithoutExtension(sourceShaderAssetPath);


            //Shader "name"         <-- find this line and set new shader name
            //{
            //      Properties
            //      {
            //  ...
            //  ...
            //  ...
            //      } 


            for (int i = 0; i < newShaderFile.Count; i++)
            {
                if (newShaderFile[i].Contains("Shader \""))
                {
                    newShaderFile[i] = "Shader \"" + "Amazing Assets/Advanced Dissolve/Shader Graph/" + originalName + "\"";

                    return true;
                }
            }

            return false;
        }

        static bool AddProperties(List<string> newShaderFile, GUID shaderGraphGUID)
        {
            //Properties
            //      {         <-- find this line ID and add Dissolve properties below it
            //  ...
            //  ...
            //  ...
            //      }        
            //SubShader
            //{



            int propertiesLineID = -1;

            for (int i = 0; i < newShaderFile.Count; i++)
            {
                if (newShaderFile[i].Trim() == "Properties")
                {
                    propertiesLineID = i + 1;

                    break;
                }
            }

            if (propertiesLineID == -1)
                return false;


            string mateiralPropertiesFilePath = Path.Combine(GetThisAssetProjectPath(), "Editor", "Shader Generator", "AdvancedDissolveProperties.txt");

            List<string> propertyFile = File.ReadAllLines(mateiralPropertiesFilePath).ToList();
            propertyFile.Add(System.Environment.NewLine);

            for (int i = 0; i < propertyFile.Count; i++)
            {
                if(propertyFile[i].Contains("_AdvancedDissolveShaderGraph"))
                {
                    propertyFile[i] = propertyFile[i].Replace("\"\"", $"\"{shaderGraphGUID}\"");
                    break;
                }
            }

            newShaderFile.InsertRange(propertiesLineID + 1, propertyFile);

            return true;
        }

        static void AddCustomEditor(List<string> newShaderFile)
        {
            //Find defult "CustomEditor" and replace it
            //Loop of 10 iterations is enough to find "CustomEditor"

            int customEditroLineID = -1;

            for (int i = newShaderFile.Count - 1; i >= newShaderFile.Count - 10; i -= 1)
            {
                if (newShaderFile[i].Contains("CustomEditorForRenderPipeline"))
                {
                    customEditroLineID = i;
                    break;
                }
            }

            if (customEditroLineID != -1)
            {
                string newCustomEditor = GetCustomEditorName(newShaderFile[customEditroLineID]);

                //Comment old editor
                newShaderFile[customEditroLineID] = "//" + newShaderFile[customEditroLineID];

                //Add new editor
                newShaderFile.Insert(customEditroLineID + 1, newCustomEditor);
            }


            //No "CustomEditor" detected
            else
            {
                //Find the end of a shader file
                for (int i = newShaderFile.Count - 1; i >= newShaderFile.Count - 10; i -= 1)
                {
                    if (newShaderFile[i].Trim() == "}")
                    {
                        newShaderFile.Insert(i, string.Format("    CustomEditor \"AmazingAssets.AdvancedDissolve.Editor.ShaderGraph.{0}\"", GetCustomEditorName(string.Empty)));
                        break;
                    }
                }
            }
        }

        static bool AddShaderCode(List<string> newShaderFile)
        {

            // SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)   <-- find this line ID and add Dissolve keywords above it
            //{
            //    SurfaceDescription surface = (SurfaceDescription)0;
            //  ...
            //  ...
            //  ... SG_AdvancedDissolve_XXXXXXXX({0}, {1}, {2});                            <-- find this line, extract {0} parameter and then disalbe this line
            //  ...
            //  ...
            //  return surface;                                                             <-- find this line ID and add Dissolve core methods above it
            //}    



            string pathToDefinesCGINC = Path.Combine(GetThisAssetProjectPath(), "Shaders", "cginc", "Defines.cginc");
            string pathToCoreCGINC = Path.Combine(GetThisAssetProjectPath(), "Shaders", "cginc", "Core.cginc");
            string pathToShaderKeywordsFile = Path.Combine(GetThisAssetProjectPath(), "Editor", "Shader Generator", "AdvancedDissolveKeywords.txt");

            pathToDefinesCGINC = "#include \"" + pathToDefinesCGINC.Replace(Path.DirectorySeparatorChar, '/') + "\"";
            pathToCoreCGINC = "#include \"" + pathToCoreCGINC.Replace(Path.DirectorySeparatorChar, '/') + "\"";

            List<string> keywordsFile = File.ReadAllLines(pathToShaderKeywordsFile).ToList();
            keywordsFile.RemoveAll(x => x.Contains("_AD_EDGE_UV_DISTORTION_SOURCE_CUSTOM_MAP"));

            List<string> defines = new List<string>();
            defines.Add(System.Environment.NewLine);
            defines.AddRange(keywordsFile);

            defines.Add(System.Environment.NewLine);
            defines.Add("#define ADVANCED_DISSOLVE_SHADER_GRAPH");

            switch (GetProjectRenderPipeline())
            {
                case Enum.RenderPipeline.BuiltIn:
                    defines.Add("#define ADVANCED_DISSOLVE_BUILTIN_RENDER_PIPELINE");
                    break;
                case Enum.RenderPipeline.Universal:
                defines.Add("#define ADVANCED_DISSOLVE_UNIVERSAL_RENDER_PIPELINE");
                    break;
                case Enum.RenderPipeline.HighDefinition:
                defines.Add("#define ADVANCED_DISSOLVE_HIGH_DEFINITION_RENDER_PIPELINE");
                    break;

                default:
                    return false;
            }

            defines.Add(pathToDefinesCGINC);
            defines.Add("/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////");
            defines.Add(System.Environment.NewLine);

            List<string> coreData = new List<string>();
            coreData.Add(System.Environment.NewLine);
            coreData.Add("//Advanced Dissolve");
            coreData.Add(pathToCoreCGINC);
            coreData.Add(System.Environment.NewLine);


            Enum.ShaderPass shaderPass = Enum.ShaderPass.Unknown;
            bool definesAdded = false;
            bool coreDataAdded = false;
            bool codeAdded = false;
            string customCutoutAlpha = "1";
            string customEdgeColor = "1";

            string surfaceAlbedo = string.Empty;
            bool useEmission = false;


            for (int i = 0; i < newShaderFile.Count; i++)
            {
                //Detect current shader pass
                GetShaderPassFromString(newShaderFile[i], ref shaderPass);


                //Add keywords
                if (newShaderFile[i].Contains("CBUFFER_START(UnityPerMaterial)"))
                {
                    newShaderFile.InsertRange(i, defines);
                    i += defines.Count;

                    //Define meta pass
                    if (shaderPass == Enum.ShaderPass.META || shaderPass == Enum.ShaderPass.Meta)
                    {
                        newShaderFile.Insert(i - 4, "#define ADVANCED_DISSOLVE_META_PASS");

                        //Reset
                        shaderPass = Enum.ShaderPass.Unknown;

                        i += 1;
                    }

                    definesAdded = true;
                }


                //Add core data
                if (newShaderFile[i].Contains("SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)"))
                {
                    GetSurfaceProperties(newShaderFile, i, out surfaceAlbedo, out useEmission);


                    newShaderFile.InsertRange(i, coreData);
                    i += coreData.Count;

                    coreDataAdded = true;
                }


                //Find Custom Cutout Alpha & Custom Edge Color
                if (newShaderFile[i].Contains("SG_AdvancedDissolve_") && newShaderFile[i].Contains("void ") == false)
                {
                    //Just make sure this line is what we are looking for
                    if (newShaderFile[i].Contains("(") && newShaderFile[i].Contains(")") && newShaderFile[i].Contains(",") && newShaderFile[i].Contains(";"))
                    {
                        //SG_AdvancedDissolve_40a0ec735cfda6043bd217c139a901ab(_9D6BCFCD_Out_2, _258F41d_Out_2, _AdvancedDissolve_21DC4B79, _AdvancedDissolve_21DC4B79_Out_3);
                        //                                                    ↑               ↑               ↑
                        //                                                    |               |               |
                        //                                                    |               |               |
                        //index1 - - - - - - - - - - - - - - - - - - - - - - -                |               |
                        //                                                                    |               |
                        //index2 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -                |
                        //                                                                                    |
                        //index3 - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 

                        int index1 = newShaderFile[i].IndexOf("(");
                        int index2 = newShaderFile[i].IndexOf(",");

                        customCutoutAlpha = newShaderFile[i].Substring(index1 + 1, index2 - index1 - 1).Trim();


                        if (newShaderFile[i].Contains("float4") == false) //User has provied Custom Edge Color
                        {
                            int index3 = newShaderFile[i].IndexOf(",", newShaderFile[i].IndexOf(",") + 1);
                            customEdgeColor = newShaderFile[i].Substring(index2 + 1, index3 - index2 - 1).Trim();
                        }
                    }
                }

                //Add core method
                if (newShaderFile[i].Contains("return surface;"))
                {
                    newShaderFile[i] = string.Empty;

                    List<string> code = new List<string>();
                    code.Add(System.Environment.NewLine);

                    code.Add("//" + shaderPass.ToString());

                    code.Add(GetShaderCode(customCutoutAlpha, customEdgeColor, surfaceAlbedo, useEmission));

                    code.Add(System.Environment.NewLine);
                    code.Add("return surface;");


                    newShaderFile.InsertRange(i, code);
                    i += code.Count;

                    codeAdded = true;
                }
            }


            //Make sure shader always has '_ALPHATEST_ON' keyword
            for (int i = 0; i < newShaderFile.Count; i++)
            {
                if (newShaderFile[i].Contains("#pragma") && newShaderFile[i].Contains("shader_feature"))
                {
                    if(newShaderFile[i].Contains("_BUILTIN_AlphaClip"))
                        newShaderFile[i] = "#define _BUILTIN_AlphaClip 1";

                    if (newShaderFile[i].Contains("_BUILTIN_ALPHATEST_ON"))
                        newShaderFile[i] = "#define _BUILTIN_ALPHATEST_ON 1";
                    
                    if(newShaderFile[i].Contains("_ALPHATEST_ON"))
                    newShaderFile[i] = "#define _ALPHATEST_ON 1";
                }
            }


            return definesAdded && coreDataAdded && codeAdded ? true : false;
        }

        static void CreateShaderAssetFile(string sourceShaderAssetPath, List<string> newShaderFile)
        {
            File.WriteAllLines(sourceShaderAssetPath, newShaderFile);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }


        static string GetShaderCode(string customCutoutAlpha, string customEdgeColor, string albedo, bool useEmission)
        {
            string code = "AdvancedDissolveShaderGraph(IN.uv0.xy, IN.ObjectSpacePosition, IN.WorldSpacePosition, IN.AbsoluteWorldSpacePosition, IN.ObjectSpaceNormal, IN.WorldSpaceNormal, " + customCutoutAlpha + ", " + customEdgeColor;

            code += string.Format(", {0}{1}surface.Alpha, surface.AlphaClipThreshold);",
                                                                                        string.IsNullOrEmpty(albedo) ? string.Empty : ("surface." + albedo + ", "),
                                                                                        useEmission ? ("surface.Emission, ") : string.Empty);


            return code;
        }

        static void GetSurfaceProperties(List<string> newShaderFile, int startIndex, out string albedo, out bool emission)
        {
            albedo = string.Empty;
            emission = false;


            for (int i = startIndex; i < newShaderFile.Count; i++)
            {
                if (newShaderFile[i].Contains("surface.Albedo = "))
                    albedo = "Albedo";

                if (newShaderFile[i].Contains("surface.BaseColor = "))
                    albedo = "BaseColor";

                if (newShaderFile[i].Contains("surface.Color = "))
                    albedo = "Color";

                if (newShaderFile[i].Contains("surface.Emission = "))
                    emission = true;

                if (newShaderFile[i].Contains("return surface;"))
                    return;
            }

            Debug.LogError("Uknown Surface Properties");
        }


        static void GetShaderPassFromString(string line, ref Enum.ShaderPass shaderPass)
        {
            //Name "Depth Only"              <------ example


            if (string.IsNullOrEmpty(line) || line.Contains("Name \"") == false)
                return;

            //Remove "Name", ", space
            line = line.Replace("Name", string.Empty).Replace("\"", string.Empty).Replace(" ", string.Empty);



            foreach (Enum.ShaderPass pass in System.Enum.GetValues(typeof(Enum.ShaderPass)))
            {
                if (line == pass.ToString())
                {
                    shaderPass = pass;

                    return;
                }
            }

            //skip reporting DebugDXR pass
            if (line != "DebugDXR")
            	Debug.LogError("Unknown Shader Pass: " + line);

            shaderPass = Enum.ShaderPass.Unknown;
        }

        //Copied from Utilities class
        static public Enum.RenderPipeline GetProjectRenderPipeline()
        {
            if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline == null && UnityEngine.QualitySettings.renderPipeline == null)
                return Enum.RenderPipeline.BuiltIn;
            else
            {
                string sType = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline == null ? UnityEngine.QualitySettings.renderPipeline.GetType().ToString() :
                                                                                                      UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.GetType().ToString();

                if (sType.Contains("UnityEngine.Rendering.Universal."))
                    return Enum.RenderPipeline.Universal;

                else if (sType.Contains("UnityEngine.Rendering.HighDefinition."))
                    return Enum.RenderPipeline.HighDefinition;


                return Enum.RenderPipeline.Unknown;
            }
        }

        //Copied from Utilities class
        static internal string GetThisAssetProjectPath()
        {
            if (string.IsNullOrEmpty(thisAssetPath))
            {
                string fileName = "AmazingAssets.AdvancedDissolve.Editor";

                string[] assets = AssetDatabase.FindAssets(fileName, null);
                if (assets != null && assets.Length > 0)
                {
                    string currentFilePath = AssetDatabase.GUIDToAssetPath(assets[0]);
                    thisAssetPath = Path.GetDirectoryName(Path.GetDirectoryName(currentFilePath));
                }
                else
                {
                    Debug.Log("Cannot detect 'This Asset' path.");
                }
            }
            return thisAssetPath;
        }


        static string GetShaderGraphHLSLCode(string shaderGraphAssetPath)
        {
            AssetImporter importer = AssetImporter.GetAtPath(shaderGraphAssetPath);
            string assetName = Path.GetFileNameWithoutExtension(importer.assetPath);

            var graphData = GetGraphData(importer);
            var generator = new Generator(graphData, null, GenerationMode.ForReals, assetName, null);

            return  generator.generatedShader;
        }

        static GraphData GetGraphData(AssetImporter importer)
        {
            var textGraph = File.ReadAllText(importer.assetPath, Encoding.UTF8);
            var graphObject = ScriptableObject.CreateInstance<GraphObject>();
            graphObject.hideFlags = HideFlags.HideAndDontSave;
            bool isSubGraph;
            var extension = Path.GetExtension(importer.assetPath).Replace(".", "");
            switch (extension)
            {
                case ShaderGraphImporter.Extension:
                    isSubGraph = false;
                    break;
                case ShaderGraphImporter.LegacyExtension:
                    isSubGraph = false;
                    break;
                case ShaderSubGraphImporter.Extension:
                    isSubGraph = true;
                    break;
                default:
                    throw new Exception($"Invalid file extension {extension}");
            }
            var assetGuid = AssetDatabase.AssetPathToGUID(importer.assetPath);
            graphObject.graph = new GraphData
            {
                assetGuid = assetGuid,
                isSubGraph = isSubGraph,
                messageManager = null
            };
            MultiJson.Deserialize(graphObject.graph, textGraph);
            graphObject.graph.OnEnable();
            graphObject.graph.ValidateGraph();
            return graphObject.graph;
        }
    }
}
