using UnityEditor;
using UnityEngine;
using UnityEngine.XR.OpenXR;

namespace Valve.OpenXR.Utils.Editor
{
    [CustomEditor(typeof(ValveOpenXRSupportFeature))]
    internal class ValveXRSupportFeatureEditor : UnityEditor.Editor
    {
        private SerializedProperty optimizeBufferDiscardsProp;
        private SerializedProperty lateLatchingModeProp;
        private SerializedProperty lateLatchingDebugProp;

        private static GUIContent s_optimizeBufferDiscardsLabel = new GUIContent("Optimize Buffer Discards");
        private static GUIContent s_lateLatchingModeLabel = new GUIContent("Late-Latching Mode");
        private static GUIContent s_lateLatchingDebugLabel = new GUIContent("Late-Latching Debug");

        void OnEnable()
        {
            optimizeBufferDiscardsProp = serializedObject.FindProperty("optimizeBufferDiscards");
            lateLatchingDebugProp = serializedObject.FindProperty("lateLatchingDebug");
            lateLatchingModeProp = serializedObject.FindProperty("lateLatchingMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUIUtility.labelWidth = 275.0f;
            EditorGUILayout.PropertyField(optimizeBufferDiscardsProp, s_optimizeBufferDiscardsLabel);
            EditorGUILayout.PropertyField(lateLatchingModeProp, s_lateLatchingModeLabel);
            EditorGUILayout.PropertyField(lateLatchingDebugProp, s_lateLatchingDebugLabel);
            EditorGUIUtility.labelWidth = 0.0f;

            serializedObject.ApplyModifiedProperties();
            
            OpenXRSettings androidOpenXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            var serializedOpenXrSettings = new SerializedObject(androidOpenXRSettings);
            ((ValveOpenXRSupportFeature)target).ApplySettingsOverride(androidOpenXRSettings);
            serializedOpenXrSettings.ApplyModifiedProperties();
        }
    }
}
