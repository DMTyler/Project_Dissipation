using DGraphics.Dissipation.Inspector;
using UnityEditor;
using UnityEngine;

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