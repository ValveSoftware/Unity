using UnityEditor;
using UnityEngine;
using UnityEngine.XR.OpenXR;

namespace Valve.OpenXR.Utils.Editor
{
    [CustomEditor(typeof(ValveOpenXRRenderRegionsFeature))]
    internal class ValveXRRenderRegionsFeatureEditor : UnityEditor.Editor
    {
#if UNITY_6000_1_OR_NEWER
        private static GUIContent s_MultiviewRenderRegionsOptimizationsLabel = EditorGUIUtility.TrTextContent("Multiview Render Regions Optimizations (Vulkan)", "Activates Multiview Render Regions optimizations at application start. Requires usage of Unity 6.1 or later, Vulkan as the Graphics API, Render Mode set to Multi-view and Symmetric rendering enabled.");
#endif
        private SerializedProperty symmetricProjection;
        
#if UNITY_6000_1_OR_NEWER
        private SerializedProperty multiviewRenderRegionsOptimizationMode;
#endif
        
        
        void OnEnable()
        {
            symmetricProjection = serializedObject.FindProperty("symmetricProjection");

#if UNITY_6000_1_OR_NEWER
            multiviewRenderRegionsOptimizationMode =
                serializedObject.FindProperty("multiviewRenderRegionsOptimizationMode");
#endif
        }

        public override void OnInspectorGUI()
        {
            // Update anything from the serializable object
            EditorGUIUtility.labelWidth = 275.0f;

            serializedObject.Update();
            EditorGUILayout.LabelField("Rendering Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(symmetricProjection, new GUIContent("Symmetric Projection (Vulkan)"));
            // OptimizeMultiviewRenderRegions (aka MVPVV) only supported on Unity 6.1 onwards
#if UNITY_6000_1_OR_NEWER
            EditorGUILayout.PropertyField(multiviewRenderRegionsOptimizationMode, s_MultiviewRenderRegionsOptimizationsLabel);
#endif
            EditorGUIUtility.labelWidth = 0.0f;

            // update any serializable properties
            serializedObject.ApplyModifiedProperties();

            OpenXRSettings androidOpenXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            var serializedOpenXrSettings = new SerializedObject(androidOpenXRSettings);

            androidOpenXRSettings.symmetricProjection = symmetricProjection.boolValue;
#if UNITY_6000_1_OR_NEWER
            androidOpenXRSettings.multiviewRenderRegionsOptimizationMode = (OpenXRSettings.MultiviewRenderRegionsOptimizationMode)multiviewRenderRegionsOptimizationMode.intValue;
#endif
            serializedOpenXrSettings.ApplyModifiedProperties();

            EditorGUIUtility.labelWidth = 0.0f;
        }
    }
}
