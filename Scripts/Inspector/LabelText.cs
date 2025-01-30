using UnityEditor;
using UnityEngine;

namespace DGraphics.Dissipation.Inspector
{
    public class LabelTextAttribute : PropertyAttribute
    {
        public string Label { get; private set; }

        public LabelTextAttribute(string label)
        {
            Label = label;
        }
    }
    
    [CustomPropertyDrawer(typeof(LabelTextAttribute))]
    public class LabelTextDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var labelTextAttribute = attribute as LabelTextAttribute;
            if (labelTextAttribute == null) return;
            var newLabel = new GUIContent(labelTextAttribute.Label);
            EditorGUI.PropertyField(position, property, newLabel, true);
        }
    }
}

