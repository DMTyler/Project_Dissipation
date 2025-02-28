using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DGraphics.Dissipation
{
    public class MeshDissipationOptions
    {
        [MenuItem("Tools/DGraphics/Setup Dissipation")]
        public static void SetupDissipation()
        {
            // Bind render feature to URP
            AddRenderFeatureToGraphicsSettings<DissipationRendererFeature>();
            
            // Add shader manager
            AddShaderManager();
        }
        
        private static void AddRenderFeatureToGraphicsSettings<T>() where T : ScriptableRendererFeature
        {
            var pipelineAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            if (pipelineAsset == null)
            {
                Debug.LogError("Current Graphics Settings is not using Universal Render Pipeline (URP). \n" +
                               $"{nameof(T)} is only available under URP.");
                return;
            }
            
            // Get the default renderer data
            var indexInfo = typeof(UniversalRenderPipelineAsset)
                .GetField("m_DefaultRendererIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            if (indexInfo == null)
            {
                Debug.LogError("Cannot Retrieve Default Renderer in UniversalRenderPipelineAsset.");
                return;
            }
            
            var rendererIndex = (int)indexInfo.GetValue(pipelineAsset);
            
            var rendererDataListInfo = typeof(UniversalRenderPipelineAsset)
                .GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rendererDataListInfo == null)
            {
                Debug.LogError("Cannot Retrieve Renderer Data List in UniversalRenderPipelineAsset.");
                return;
            }

            var rendererDataList = rendererDataListInfo.GetValue(pipelineAsset) as ScriptableRendererData[];
            if (rendererDataList == null)
            {
                Debug.LogError("Cannot Retrieve Renderer Data List in UniversalRenderPipelineAsset.");
                return;
            }
            
            var rendererData = rendererDataList[rendererIndex] as UniversalRendererData;
            if (rendererData == null)
            {
                Debug.LogError("Cannot Retrieve Default Renderer Data in UniversalRenderPipelineAsset.");
                return;
            }
            
            /*var rendererDataSo = new SerializedObject(rendererData);*/
            
            var rendererFeaturesInfo = typeof(UniversalRendererData)
                .GetField("m_RendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rendererFeaturesInfo == null)
            {
                Debug.LogError("Cannot Retrieve Renderer Features in UniversalRendererData.");
                return;
            }

            var rendererFeatures = rendererFeaturesInfo.GetValue(rendererData) as List<ScriptableRendererFeature>;
            if (rendererFeatures == null)
            {
                Debug.LogError("Cannot Retrieve Renderer Features in UniversalRendererData.");
                return;
            }
            
            var rendererFeatureMapInfo = typeof(UniversalRendererData)
                .GetField("m_RendererFeatureMap", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rendererFeatureMapInfo == null)
            {
                Debug.LogError("Cannot Retrieve Renderer Feature Map in UniversalRendererData.");
                return;
                
            }
            
            var rendererFeatureMap = rendererFeatureMapInfo.GetValue(rendererData) as List<long>;
            if (rendererFeatureMap == null)
            {
                Debug.LogError("Cannot Retrieve Renderer Feature Map in UniversalRendererData.");
                return;
            }
            
            foreach (var feature in rendererFeatures)
            {
                if (feature.GetType() == typeof(T))
                {
                    return;
                }
            }

            var scrpitableFeature = ScriptableObject.CreateInstance<T>();
            scrpitableFeature.name = "Outline PostProcessing Renderer Feature";
            Undo.RegisterCreatedObjectUndo(scrpitableFeature, "Add Outline PostProcessing Renderer Feature");
            if (EditorUtility.IsPersistent(rendererData))
            {
                AssetDatabase.AddObjectToAsset(scrpitableFeature, rendererData);
            }
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(rendererData, out var guid, out long localId);
            
            var editor = Editor.CreateEditor(rendererData);
            var rendererFeaturesInEditorInfo = typeof(ScriptableRendererDataEditor)
                .GetField("m_RendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rendererFeaturesInEditorInfo == null)
            {
                Debug.LogError("Cannot Retrieve Renderer Features in Editor in UniversalRendererData.");
                return;
            }
            
            var rendererFeaturesInEditor = editor.serializedObject.FindProperty("m_RendererFeatures");
            rendererFeaturesInEditorInfo.SetValue(editor, rendererFeaturesInEditor);
            
            var rendererFeatureMapInEditorInfo = typeof(ScriptableRendererDataEditor)
                .GetField("m_RendererFeaturesMap", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rendererFeatureMapInEditorInfo == null)
            {
                Debug.LogError("Cannot Retrieve Renderer Features Map in Editor in UniversalRendererData.");
                return;
            }
            
            var rendererFeatureMapInEditor = editor.serializedObject.FindProperty("m_RendererFeatureMap");
            rendererFeatureMapInEditorInfo.SetValue(editor, rendererFeatureMapInEditor);
            
            var addComponentMethodInfo = typeof(ScriptableRendererDataEditor)
                .GetMethod("AddComponent", BindingFlags.NonPublic | BindingFlags.Instance);
            if (addComponentMethodInfo == null)
            {
                Debug.LogError("Cannot Retrieve AddComponent method in ScriptableRendererData Editor.");
                return;
            }
            var typename = typeof(T).FullName;
            addComponentMethodInfo.Invoke(editor, new object[] {typename});
        }

        private static void AddShaderManager()
        {
            if (Object.FindObjectOfType<ComputeShaderManager>() != null) return;
            
            var manager = Object.Instantiate(new GameObject("ComputeShaderManager"));
            Undo.RegisterCreatedObjectUndo(manager, "Add ComputeShaderManager");
            var script = manager.AddComponent<ComputeShaderManager>();
            
            var transformer = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/DGraphics/Dissipation/Shaders/Compute/MeshTransformer.compute");
            if (transformer == null)
            {
                Debug.LogWarning("Cannot find DissipationTransformer.compute. Relative path may be incorrect." +
                                 "Please bind mesh transformer manually or check the path: Assets/DGraphics/Dissipation/Shaders/Compute/MeshTransformer.compute");
            }
            else
            {
                script.MeshTransformer = transformer;
            }
            
            var decomposer = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/DGraphics/Dissipation/Shaders/Compute/MeshDecomposer.compute");
            if (decomposer == null)
            {
                Debug.LogWarning("Cannot find DissipationDecomposer.compute. Relative path may be incorrect." +
                                 "Please bind mesh decomposer manually or check the path: Assets/DGraphics/Dissipation/Shaders/Compute/MeshDecomposer.compute");
            }
            else
            {
                script.MeshDecomposer = decomposer;
            }
        }
    }
}