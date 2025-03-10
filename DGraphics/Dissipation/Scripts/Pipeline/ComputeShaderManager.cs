using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DGraphics.Dissipation
{ 
    public class ComputeShaderManager : MonoBehaviour
    {
        private static ComputeShaderManager _instance;
        public static ComputeShaderManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = CreateInstance();
                return _instance;
            }
        }

        public ComputeShader MeshDecomposer;
        public ComputeShader MeshTransformer;

        private static ComputeShaderManager CreateInstance()
        {
            if (_instance != null) return _instance;
            var findResult = FindObjectOfType<ComputeShaderManager>();
            if (findResult != null)
            {
                if (Application.isPlaying)
                    DontDestroyOnLoad(findResult.gameObject);
                return findResult;
            }
            var go = new GameObject("ComputeShaderManager");
            if (Application.isPlaying)
                DontDestroyOnLoad(go);
            var instance = go.AddComponent<ComputeShaderManager>();
            return instance;
        }
    }

}

