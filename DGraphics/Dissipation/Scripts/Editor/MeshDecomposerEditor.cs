using UnityEditor;
using UnityEngine;

namespace DGraphics.Dissipation
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MeshDecomposer))]
    public class MeshDecomposerEditor : Editor
    {
        private int _selectedLanguage = 0;
        private readonly string[] _languageOptions = {"中文", "English"};
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var targetScript = (MeshDecomposer)target;
            
            EditorGUILayout.LabelField(_selectedLanguage == 0? "语言" : "Language", EditorStyles.boldLabel);
            _selectedLanguage = GUILayout.Toolbar(_selectedLanguage, _languageOptions);
            EditorGUILayout.Space(10);
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_meshFilters"), 
                _selectedLanguage == 0? new GUIContent("网格过滤器") : new GUIContent("Mesh Filter"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_divided"), 
                _selectedLanguage == 0? new GUIContent("已分割") : new GUIContent("Divided"));
            EditorGUI.EndDisabledGroup();
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}