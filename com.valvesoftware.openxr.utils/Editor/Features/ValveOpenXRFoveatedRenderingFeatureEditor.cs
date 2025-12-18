using UnityEditor;
using UnityEngine;

namespace Valve.OpenXR.Utils.Editor
{
    [CustomEditor(typeof(ValveOpenXRFoveatedRenderingFeature))]
    internal class ValveOpenXRFoveatedRenderingFeatureEditor : UnityEditor.Editor
    {
        private SerializedProperty applySettingsOnStartup;
        private SerializedProperty initialFoveationLevel;
        private SerializedProperty initialUseEyeTracking;
        
        void OnEnable()
        {
            applySettingsOnStartup = serializedObject.FindProperty("applySettingsOnStartup");
            initialFoveationLevel = serializedObject.FindProperty("initialFoveationLevel");
            initialUseEyeTracking = serializedObject.FindProperty("initialUseEyeTracking");
        }

        public override void OnInspectorGUI()
        {
            // Update anything from the serializable object
            serializedObject.Update();

            var prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 300;

            EditorGUILayout.PropertyField(applySettingsOnStartup, new GUIContent("Apply Settings On Startup"));
            
            if (applySettingsOnStartup.boolValue)
            {
                EditorGUILayout.PropertyField(initialFoveationLevel, new GUIContent("Foveation Level"));
                EditorGUILayout.PropertyField(initialUseEyeTracking, new GUIContent("Gaze enabled?"));
            }

            EditorGUIUtility.labelWidth = prevLabelWidth;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
