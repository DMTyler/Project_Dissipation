using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DGraphics.Dissipation
{
   /// <summary>
   /// Divide mesh into triangles, calculate the Middle points of triangles and write them into UV7
   /// </summary>
   public class MeshDecomposer : MonoBehaviour
   {
      #region Params
      [SerializeField] private bool _divided = false;
      private ComputeShader _decomposeCS;
      #endregion
      
      #region Button Functions
      public void Calculate()
      {
         if (_decomposeCS == null)
         {
            if (ComputeShaderManager.Instance == null)
            {
               Debug.LogError("Compute Shader Manager not found for Mesh Decomposer");
               return;
            }

            _decomposeCS = ComputeShaderManager.Instance.MeshDecomposer;

            if (_decomposeCS == null)
            {
               Debug.LogError("Mesh Decompooser not found. " +
                              "Are you forget to add it to the Compute Shader Manager?");
               return;
            }
         }

         var meshes = new List<Mesh>();
         if (!_divided)
         {
           
            var meshFilters = GetComponentsInChildren<MeshFilter>();
            var skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var meshFilter in meshFilters)
            {
#if UNITY_EDITOR
               Undo.RecordObject(meshFilter, $"Divide Mesh for {meshFilter.name}");
#endif
               var mesh = meshFilter.sharedMesh = Divide(meshFilter.sharedMesh);
               meshes.Add(mesh);
            }
            
            foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            {
#if UNITY_EDITOR
               Undo.RecordObject(skinnedMeshRenderer, $"Divide Mesh for {skinnedMeshRenderer.name}");
#endif
               var mesh = skinnedMeshRenderer.sharedMesh = DivideSkin(skinnedMeshRenderer.sharedMesh);
               meshes.Add(mesh);
            }
         }
         
         _divided = true;
         meshes.ForEach(Middle);
      }

      /// <summary>
      /// Divide mesh into independent triangles
      /// </summary>
      public Mesh Divide(Mesh mesh)
      {
         if (mesh == null)
         {
            Debug.LogError("Mesh not found");
            return null;
         }
         
         var triangles = mesh.triangles;
         var vertices = mesh.vertices;
         var uvs = mesh.uv.Length > 0 ? mesh.uv : new Vector2[vertices.Length];
         var normals = mesh.normals.Length > 0 ? mesh.normals : new Vector3[vertices.Length];

         var triangleCount = mesh.triangles.Length / 3;
         var boneWeights = new GPUBoneWeight[vertices.Length];
         for (int i = 0; i < vertices.Length; i++)
         {
            boneWeights[i] = new GPUBoneWeight();
         }

         if (_decomposeCS == null)
         {
            Debug.LogError("MiddlePointCalculator compute shader not found");
            return null;
         }

         var cmd = CommandBufferPool.Get("DivideTriangles");
         var kernel = _decomposeCS.FindKernel("CSDivideTriangles");

         #region ----------------Set Inputs---------------------

         var verticesBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
         verticesBuffer.SetData(vertices);

         var trianglesBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
         trianglesBuffer.SetData(triangles);

         var uvsBuffer = new ComputeBuffer(uvs.Length, sizeof(float) * 2);
         uvsBuffer.SetData(uvs);

         var normalBuffer = new ComputeBuffer(normals.Length, sizeof(float) * 3);
         normalBuffer.SetData(normals);
         
         cmd.SetComputeIntParam(_decomposeCS, ShaderId.TriangleCount, triangleCount);

         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.Vertices, verticesBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.Triangles, trianglesBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.UVs, uvsBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.Normals, normalBuffer);

         #endregion -----------------------------------------------

         #region ----------------Set Outputs---------------------

         var newVerticesBuffer = new ComputeBuffer(triangles.Length, sizeof(float) * 3);
         newVerticesBuffer.SetData(new Vector3[triangles.Length]);

         var newTrianglesBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
         newTrianglesBuffer.SetData(new int[triangles.Length]);

         var newUVsBuffer = new ComputeBuffer(triangles.Length, sizeof(float) * 2);
         newUVsBuffer.SetData(new Vector2[triangles.Length]);

         var newNormalsBuffer = new ComputeBuffer(triangles.Length, sizeof(float) * 3);
         newNormalsBuffer.SetData(new Vector3[triangles.Length]);

         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.DivideResult.Vertices, newVerticesBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.DivideResult.Triangles, newTrianglesBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.DivideResult.UVs, newUVsBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.DivideResult.Normals, newNormalsBuffer);

         #endregion ----------------------------------------------

         var numThreads = new NumThreads(_decomposeCS, kernel);

         cmd.DispatchCompute(_decomposeCS, kernel, Mathf.CeilToInt(triangleCount / (float)numThreads.x), 1, 1);
         var fence = cmd.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.ComputeProcessing);
         
         Graphics.ExecuteCommandBuffer(cmd);
         Graphics.WaitOnAsyncGraphicsFence(fence);
         
         CommandBufferPool.Release(cmd);

         var newTriangles = new int[triangles.Length];
         var newUVs = new Vector2[triangles.Length];
         var newVertices = new Vector3[triangles.Length];
         var newNormals = new Vector3[triangles.Length];

         #region -------------Get Outputs-----------------

         newTrianglesBuffer.GetData(newTriangles);
         newUVsBuffer.GetData(newUVs);
         newVerticesBuffer.GetData(newVertices);
         newNormalsBuffer.GetData(newNormals);

         var newMesh = new Mesh
         {
            vertices = newVertices,
            triangles = newTriangles,
            uv = newUVs,
            normals = newNormals,
         };

         newMesh.RecalculateBounds();
         newMesh.name = mesh.name + "_Decomposed";
         
#if UNITY_EDITOR
         
         var folderPath = "Assets/DGraphics/Meshes/auto-gen";
         var fileName = mesh.name + "_Decomposed.asset";
         var filePath = Path.Combine(folderPath, fileName);
         if (!Directory.Exists(folderPath))
         {
            Directory.CreateDirectory(folderPath);
         }
         
         if (File.Exists(filePath))
         {
            AssetDatabase.DeleteAsset(filePath);
            AssetDatabase.Refresh();
         }
         
         AssetDatabase.CreateAsset(newMesh, filePath);
         AssetDatabase.SaveAssets();
         AssetDatabase.Refresh();
#endif
         
         #endregion

         #region ---------------Dispose-------------------

         newVerticesBuffer.Dispose();
         newTrianglesBuffer.Dispose();
         newUVsBuffer.Dispose();
         newNormalsBuffer.Dispose();

         verticesBuffer.Dispose();
         trianglesBuffer.Dispose();
         uvsBuffer.Dispose();
         normalBuffer.Dispose();

         #endregion-----------------------------

         return newMesh;
      }

      /// <summary>
      /// Divide skinned mesh into independent triangles
      /// </summary>
      public Mesh DivideSkin(Mesh mesh)
      {
         if (mesh == null)
         {
            Debug.LogError("Mesh not found");
            return null;
         }
         
         var bindposes = mesh.bindposes;
         var triangles = mesh.triangles;
         var vertices = mesh.vertices;
         var uvs = mesh.uv.Length > 0 ? mesh.uv : new Vector2[vertices.Length];
         var normals = mesh.normals.Length > 0 ? mesh.normals : new Vector3[vertices.Length];
         
         var boneWeights = ConvertBoneWeightsToGPU(mesh.boneWeights);
         if (boneWeights.Length == 0)
         {
            boneWeights = new GPUBoneWeight[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
               boneWeights[i] = new GPUBoneWeight();
            }
         }
         
         var triangleCount = mesh.triangles.Length / 3;

         if (_decomposeCS == null)
         {
            Debug.LogError("MiddlePointCalculator compute shader not found");
            return null;
         }

         var cmd = CommandBufferPool.Get("DivideSkinnedTriangles");
         var kernel = _decomposeCS.FindKernel("CSDivideSkinnedTriangles");

         #region ----------------Set Inputs---------------------

         var verticesBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
         verticesBuffer.SetData(vertices);

         var trianglesBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
         trianglesBuffer.SetData(triangles);

         var uvsBuffer = new ComputeBuffer(uvs.Length, sizeof(float) * 2);
         uvsBuffer.SetData(uvs);

         var normalBuffer = new ComputeBuffer(normals.Length, sizeof(float) * 3);
         normalBuffer.SetData(normals);
         
         var boneWeightBuffer = new ComputeBuffer(boneWeights.Length, Marshal.SizeOf(typeof(GPUBoneWeight)));
         boneWeightBuffer.SetData(boneWeights);

         cmd.SetComputeIntParam(_decomposeCS, ShaderId.TriangleCount, triangleCount);

         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.Vertices, verticesBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.Triangles, trianglesBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.UVs, uvsBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.Normals, normalBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.BoneWeights, boneWeightBuffer);

         #endregion -----------------------------------------------

         #region ----------------Set Outputs---------------------

         var newVerticesBuffer = new ComputeBuffer(triangles.Length, sizeof(float) * 3);
         newVerticesBuffer.SetData(new Vector3[triangles.Length]);

         var newTrianglesBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
         newTrianglesBuffer.SetData(new int[triangles.Length]);

         var newUVsBuffer = new ComputeBuffer(triangles.Length, sizeof(float) * 2);
         newUVsBuffer.SetData(new Vector2[triangles.Length]);

         var newNormalsBuffer = new ComputeBuffer(triangles.Length, sizeof(float) * 3);
         newNormalsBuffer.SetData(new Vector3[triangles.Length]);
         
         var newBoneWeightBuffer = new ComputeBuffer(triangles.Length, Marshal.SizeOf(typeof(GPUBoneWeight)));
         newBoneWeightBuffer.SetData(new GPUBoneWeight[triangles.Length]);

         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.DivideResult.Vertices, newVerticesBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.DivideResult.Triangles, newTrianglesBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.DivideResult.UVs, newUVsBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.DivideResult.Normals, newNormalsBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.DivideResult.BoneWeights, newBoneWeightBuffer);

         #endregion ----------------------------------------------

         var numThreads = new NumThreads(_decomposeCS, kernel);

         cmd.DispatchCompute(_decomposeCS, kernel, Mathf.CeilToInt(triangleCount / (float)numThreads.x), 1, 1);
         var fence = cmd.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.ComputeProcessing);
         
         Graphics.ExecuteCommandBuffer(cmd);
         Graphics.WaitOnAsyncGraphicsFence(fence);
         
         CommandBufferPool.Release(cmd);

         var newTriangles = new int[triangles.Length];
         var newUVs = new Vector2[triangles.Length];
         var newVertices = new Vector3[triangles.Length];
         var newNormals = new Vector3[triangles.Length];
         var newBoneWeights = new GPUBoneWeight[triangles.Length];
         
         #region -------------Get Outputs-----------------

         newTrianglesBuffer.GetData(newTriangles);
         newUVsBuffer.GetData(newUVs);
         newVerticesBuffer.GetData(newVertices);
         newNormalsBuffer.GetData(newNormals);
         newBoneWeightBuffer.GetData(newBoneWeights);

         var newMesh = new Mesh
         {
            vertices = newVertices,
            triangles = newTriangles,
            uv = newUVs,
            normals = newNormals,
            boneWeights = ConvertBoneWeightsToCPU(newBoneWeights),
            bindposes = bindposes,
         };

         newMesh.RecalculateBounds();
         newMesh.name = mesh.name + "_Decomposed";
         
#if UNITY_EDITOR
         
         var folderPath = "Assets/DGraphics/Meshes/auto-gen";
         var fileName = mesh.name + "_Decomposed.asset";
         var filePath = Path.Combine(folderPath, fileName);
         if (!Directory.Exists(folderPath))
         {
            Directory.CreateDirectory(folderPath);
         }
         
         if (File.Exists(filePath))
         {
            AssetDatabase.DeleteAsset(filePath);
            AssetDatabase.Refresh();
         }
         
         AssetDatabase.CreateAsset(newMesh, filePath);
         AssetDatabase.SaveAssets();
         AssetDatabase.Refresh();
          
#endif
         #endregion

         #region ---------------Dispose-------------------

         newVerticesBuffer.Dispose();
         newTrianglesBuffer.Dispose();
         newUVsBuffer.Dispose();
         newNormalsBuffer.Dispose();
         newBoneWeightBuffer.Dispose();

         verticesBuffer.Dispose();
         trianglesBuffer.Dispose();
         uvsBuffer.Dispose();
         normalBuffer.Dispose();
         boneWeightBuffer.Dispose();

         #endregion-----------------------------

         return newMesh;
      }

      /// <summary>
      /// Calculate the middle point of each triangle and write into UV7
      /// </summary>
      public void Middle(Mesh mesh)
      {
         if (mesh == null)
         {
            return;
         }

         var triangles = mesh.triangles;
         var vertices = mesh.vertices;
         var uvs = mesh.uv;

         var triangleCount = mesh.triangles.Length / 3;

         if (_decomposeCS == null)
         {
            Debug.LogError("MiddlePointCalculator compute shader not found");
            return;
         }

         #region -------------------- Set Inputs -----------------------

         var kernel = _decomposeCS.FindKernel("CSMiddlePoint");
         var cmd = CommandBufferPool.Get("MiddlePoint");

         var verticesBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
         verticesBuffer.SetData(vertices);

         var trianglesBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
         trianglesBuffer.SetData(triangles);
         
         var uvsBuffer = new ComputeBuffer(uvs.Length, sizeof(float) * 2);
         uvsBuffer.SetData(uvs);

         cmd.SetComputeIntParam(_decomposeCS, ShaderId.TriangleCount, triangleCount);

         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.Vertices, verticesBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.Triangles, trianglesBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.UVs, uvsBuffer);
         
         #endregion ----------------------------------------------------

         var resultBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 4);
         resultBuffer.SetData(new Vector4[vertices.Length]);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.MiddleResult, resultBuffer);
         
         var resultUVBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 2);
         resultUVBuffer.SetData(new Vector2[vertices.Length]);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.MiddleUVResult, resultUVBuffer);

         var numThreads = new NumThreads(_decomposeCS, kernel);

         cmd.DispatchCompute(_decomposeCS, kernel, Mathf.CeilToInt(triangleCount / (float)numThreads.x), 1, 1);
         var fence = cmd.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.ComputeProcessing);
         
         Graphics.ExecuteCommandBuffer(cmd);
         Graphics.WaitOnAsyncGraphicsFence(fence);
         
         CommandBufferPool.Release(cmd);
         
         var result = new Vector4[vertices.Length];
         resultBuffer.GetData(result);
         
         var resultUV = new Vector2[vertices.Length];
         resultUVBuffer.GetData(resultUV);
         
         mesh.SetUVs(7, result);
         mesh.SetUVs(6, resultUV);
         
#if UNITY_EDITOR
         EditorUtility.SetDirty(mesh);
#endif

         #region -------------------- Dispose -----------------------

         verticesBuffer.Dispose();
         trianglesBuffer.Dispose();
         uvsBuffer.Dispose();
         
         resultBuffer.Dispose();
         resultUVBuffer.Dispose();
         #endregion ------------------------------------------------
      }
      
      #endregion
      
      [StructLayout(LayoutKind.Sequential)]
      private struct GPUBoneWeight
      {
         public int boneIndex0;
         public int boneIndex1;
         public int boneIndex2;
         public int boneIndex3;
         public float weight0;
         public float weight1;
         public float weight2;
         public float weight3;
      }
      
      private struct ShaderId
      {
         public static int TriangleCount = Shader.PropertyToID("TriangleCount");
         public static int Vertices = Shader.PropertyToID("Vertices");
         public static int Triangles = Shader.PropertyToID("Triangles");
         public static int Normals = Shader.PropertyToID("Normals");
         public static int UVs = Shader.PropertyToID("UVs");
         public static int BoneWeights = Shader.PropertyToID("BoneWeights");
         public static int MiddleResult = Shader.PropertyToID("CSMiddlePointResult");
         public static int MiddleUVResult = Shader.PropertyToID("CSMiddlePointResultUVs");

         public struct DivideResult
         {
            public static int Vertices = Shader.PropertyToID("CSDivideTrianglesResultNewVertices");
            public static int Triangles = Shader.PropertyToID("CSDivideTrianglesResultNewTriangles");
            public static int UVs = Shader.PropertyToID("CSDivideTrianglesResultNewUVs");
            public static int Normals = Shader.PropertyToID("CSDivideTrianglesResultNewNormals");
            public static int BoneWeights = Shader.PropertyToID("CSDivideTrianglesResultNewBoneWeights");
         }
      }

      private GPUBoneWeight[] ConvertBoneWeightsToGPU(BoneWeight[] boneWeights)
      {
         var result = new GPUBoneWeight[boneWeights.Length];
         for (int i = 0; i < boneWeights.Length; i++)
         {
            result[i].boneIndex0 = boneWeights[i].boneIndex0;
            result[i].boneIndex1 = boneWeights[i].boneIndex1;
            result[i].boneIndex2 = boneWeights[i].boneIndex2;
            result[i].boneIndex3 = boneWeights[i].boneIndex3;
            result[i].weight0 = boneWeights[i].weight0;
            result[i].weight1 = boneWeights[i].weight1;
            result[i].weight2 = boneWeights[i].weight2;
            result[i].weight3 = boneWeights[i].weight3;
         }
         return result;
      }

      private BoneWeight[] ConvertBoneWeightsToCPU(GPUBoneWeight[] boneWeights)
      {
         var result = new BoneWeight[boneWeights.Length];
         for (int i = 0; i < boneWeights.Length; i++)
         {
            result[i].boneIndex0 = boneWeights[i].boneIndex0;
            result[i].boneIndex1 = boneWeights[i].boneIndex1;
            result[i].boneIndex2 = boneWeights[i].boneIndex2;
            result[i].boneIndex3 = boneWeights[i].boneIndex3;
            result[i].weight0 = boneWeights[i].weight0;
            result[i].weight1 = boneWeights[i].weight1;
            result[i].weight2 = boneWeights[i].weight2;
            result[i].weight3 = boneWeights[i].weight3;
         }
         return result;
      }
   }

   public class NumThreads
   {
      public int x
      {
         get { return (int)_numThreads[0]; }
      }

      public int y
      {
         get { return (int)_numThreads[1]; }
      }

      public int z
      {
         get { return (int)_numThreads[2]; }
      }

      uint[] _numThreads = new uint[] { 1, 1, 1 };

      public NumThreads(ComputeShader compute, int kernelIndex)
      {
         compute.GetKernelThreadGroupSizes(kernelIndex, out _numThreads[0], out _numThreads[1], out _numThreads[2]);
      }

      public static implicit operator int[](NumThreads t)
      {
         return new int[] { t.x, t.y, t.z };
      }
   }
}
