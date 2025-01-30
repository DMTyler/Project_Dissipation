using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DGraphics.Dissipation.Inspector
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class ButtonEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            var targetType = target.GetType();
            var methods = targetType
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttributes(typeof(ButtonAttribute), true).Length > 0)
                .ToArray();

            foreach (var method in methods)
            {
                var attribute = (ButtonAttribute)method.GetCustomAttributes(typeof(ButtonAttribute), true)[0];
                var buttonLabel = string.IsNullOrEmpty(attribute.Label) ? method.Name : attribute.Label;
                
                if (GUILayout.Button(buttonLabel))
                {
                    method.Invoke(target, null);
                }
            }
        }
    }

}