using System.Collections.Generic;
using System.IO;
using Unity.XR.CoreUtils.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;
using Object = UnityEngine.Object;

namespace Valve.OpenXR.Utils.Samples.RenderModels
{
    /// <summary>Project validation for the Render Model sample: glTFast shaders must be preloaded so builds do not strip them.</summary>
    [InitializeOnLoad]
    internal static class RenderModelSampleValidation
    {
        private const string Category = "Valve Render Model Sample";
        private const string GltfPackageName = "com.unity.cloud.gltfast";

        static RenderModelSampleValidation()
        {
            AddRules(BuildTargetGroup.Standalone);
            AddRules(BuildTargetGroup.Android);
        }

        private static void AddRules(BuildTargetGroup group)
        {
            FeatureCheck featureCheck = new FeatureCheck(group);
            List<BuildValidationRule> rules = new List<BuildValidationRule>();
#if !UNITY_GLTFAST
            rules.Add(new BuildValidationRule
            {
                Category = Category,
                Message = "The Render Model sample displays the models with the Unity glTFast package.",
                IsRuleEnabled = featureCheck.IsRenderModelsEnabled,
                CheckPredicate = IsGltfastInstalled,
                FixIt = InstallGltfast,
                FixItMessage = "Install the Unity glTFast package.",
                Error = false
            });
#else
            // https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.0/manual/ProjectSetup.html
            rules.Add(new BuildValidationRule
            {
                Category = Category,
                Message = "glTFast needs its shaders in the preloaded assets, or builds strip them and render models show Unity's pink error shader.",
                IsRuleEnabled = featureCheck.IsRenderModelsEnabled,
                CheckPredicate = AreGltfastShadersPreloaded,
                FixIt = PreloadGltfastShaders,
                FixItMessage = "Add the glTFast shaders to the preloaded assets.",
                Error = false
            });
#endif
            BuildValidator.AddRules(group, rules);
        }

        private sealed class FeatureCheck
        {
            private readonly BuildTargetGroup group;

            public FeatureCheck(BuildTargetGroup group)
            {
                this.group = group;
            }

            public bool IsRenderModelsEnabled()
            {
                OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
                if (settings == null)
                {
                    return false;
                }

                ValveOpenXRRenderModelFeature feature = settings.GetFeature<ValveOpenXRRenderModelFeature>();
                return feature != null && feature.enabled;
            }
        }

#if !UNITY_GLTFAST
        private static bool IsGltfastInstalled()
        {
            return false;
        }

        private static void InstallGltfast()
        {
            Client.Add(GltfPackageName);
        }
#else
        private const string GltfPackagePath = "Packages/" + GltfPackageName;

        // glTFast picks its shaders by render pipeline. Built-in uses the .shader files under Runtime/Shader/Built-In, URP and HDRP the shader graphs
        // in Runtime/Shader, unless GLTFAST_BUILTIN_SHADER_GRAPH makes built-in use the graphs too.
        private static readonly string[] BuiltInGltfAssetFilenames = { "glTFPbrMetallicRoughness.shader", "glTFPbrSpecularGlossiness.shader", "glTFUnlit.shader" };
        private static readonly string[] ShaderGraphGltfAssetFilenames = { "glTF-pbrMetallicRoughness.shadergraph", "glTF-pbrSpecularGlossiness.shadergraph", "glTF-unlit.shadergraph" };

        private static string[] GetRequiredGltfAssetFilenames()
        {
#if GLTFAST_BUILTIN_SHADER_GRAPH
            return ShaderGraphGltfAssetFilenames;
#else
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                return BuiltInGltfAssetFilenames;
            }

            return ShaderGraphGltfAssetFilenames;
#endif
        }

        private static bool AreGltfastShadersPreloaded()
        {
            List<string> preloadedGltfFiles = new List<string>();
            foreach (Object asset in GetPreloadedAssets())
            {
                string path = AssetDatabase.GetAssetPath(asset);
                if (!string.IsNullOrEmpty(path) && path.StartsWith(GltfPackagePath))
                {
                    preloadedGltfFiles.Add(Path.GetFileName(path));
                }
            }

            foreach (string filename in GetRequiredGltfAssetFilenames())
            {
                if (!preloadedGltfFiles.Contains(filename))
                {
                    return false;
                }
            }

            return true;
        }

        private static void PreloadGltfastShaders()
        {
            string[] searchFolders = { GltfPackagePath };
            List<Object> preloaded = new List<Object>(GetPreloadedAssets());
            foreach (string filename in GetRequiredGltfAssetFilenames())
            {
                string[] guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(filename), searchFolders);
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (Path.GetFileName(path) != filename)
                    {
                        continue;
                    }

                    Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                    if (asset && !preloaded.Contains(asset))
                    {
                        preloaded.Add(asset);
                    }
                }
            }

            PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
        }

        private static List<Object> GetPreloadedAssets()
        {
            List<Object> result = new List<Object>();
            Object[] preloaded = PlayerSettings.GetPreloadedAssets();
            if (preloaded == null)
            {
                return result;
            }

            foreach (Object asset in preloaded)
            {
                if (asset)
                {
                    result.Add(asset);
                }
            }

            return result;
        }
#endif
    }
}
