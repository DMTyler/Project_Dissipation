using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace DGraphics.Dissipation
{
   /// <summary>
   /// Divide mesh into triangles, calculate the Middle points of triangles and write them into UV7
   /// </summary>
   [RequireComponent(typeof(MeshFilter))]
   public class MeshDecomposer : MonoBehaviour
   {
      #region Params

      [SerializeField, LabelText("Mesh Filters")] private List<MeshFilter> _meshFilters;
      [SerializeField, LabelText("Divided or not"), ReadOnly] private bool _divided = false;
      private ComputeShader _decomposeCS;

      #endregion

      #region Button Functions
      [Button("Calculate")]
      public void Calculate()
      {
         if (_decomposeCS == null)
         {
            if (ComputeShaderManager.Instance.MeshDecomposer == null)
            {
               Debug.LogError("Set Up Not Complete in ComputeShaderManager: MeshDecomposer not found.");
               return;
            }
            
            _decomposeCS = ComputeShaderManager.Instance.MeshDecomposer;
         }
         
         if (_meshFilters.Count == 0)
            _meshFilters = GetComponentsInChildren<MeshFilter>().ToList();
         if (!_divided) _meshFilters.ForEach(Divide);
         _divided = true;
         _meshFilters.Select(filter => filter.sharedMesh).ToList().ForEach(Middle);
      }

      /// <summary>
      /// Divide mesh into independent triangles
      /// </summary>
      public void Divide(MeshFilter meshFilter)
      {
         var _mesh = meshFilter.sharedMesh;
         if (_mesh == null)
         {
            Debug.LogError("Mesh not found");
            return;
         }

         var triangles = _mesh.triangles;
         var vertices = _mesh.vertices;
         var uvs = _mesh.uv.Length > 0 ? _mesh.uv : new Vector2[vertices.Length];
         var normals = _mesh.normals.Length > 0 ? _mesh.normals : new Vector3[vertices.Length];

         var triangleCount = _mesh.triangles.Length / 3;

         if (_decomposeCS == null)
         {
            Debug.LogError("MiddlePointCalculator compute shader not found");
            return;
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
         
         var newTriangleIndexBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
         newTriangleIndexBuffer.SetData(new int[triangles.Length]);

         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.DivideResult.Vertices, newVerticesBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.DivideResult.Triangles, newTrianglesBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.DivideResult.UVs, newUVsBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.DivideResult.Normals, newNormalsBuffer);
         cmd.SetComputeBufferParam(_decomposeCS, kernel, ShaderId.DivideResult.TriangleIndex, newTriangleIndexBuffer);

         #endregion ----------------------------------------------

         var numThreads = new NumThreads(_decomposeCS, kernel);

         cmd.DispatchCompute(_decomposeCS, kernel, Mathf.CeilToInt(triangleCount / (float)numThreads.x), 1, 1);
         Graphics.ExecuteCommandBuffer(cmd);
         CommandBufferPool.Release(cmd);

         var newTriangles = new int[triangles.Length];
         var newUVs = new Vector2[triangles.Length];
         var newVertices = new Vector3[triangles.Length];
         var newNormals = new Vector3[triangles.Length];
         var newTriangleIndex = new int[triangles.Length];

         #region -------------Get Outputs-----------------

         newTrianglesBuffer.GetData(newTriangles);
         newUVsBuffer.GetData(newUVs);
         newVerticesBuffer.GetData(newVertices);
         newNormalsBuffer.GetData(newNormals);
         newTriangleIndexBuffer.GetData(newTriangleIndex);

         var newMesh = new Mesh
         {
            vertices = newVertices,
            triangles = newTriangles,
            uv = newUVs,
            normals = newNormals,
         };

         _mesh = newMesh;
         meshFilter.sharedMesh = _mesh;

         #endregion---------------------------------

         #region ---------------Dispose-------------------

         newVerticesBuffer.Dispose();
         newTrianglesBuffer.Dispose();
         newUVsBuffer.Dispose();
         newNormalsBuffer.Dispose();
         newTriangleIndexBuffer.Dispose();

         verticesBuffer.Dispose();
         trianglesBuffer.Dispose();
         uvsBuffer.Dispose();
         normalBuffer.Dispose();

         #endregion-----------------------------

      }

      /// <summary>
      /// Calculate the middle point of each triangle and write into UV7
      /// </summary>
      public void Middle(Mesh _mesh)
      {
         if (_mesh == null)
         {
            if (!TryGetComponent<MeshFilter>(out var meshFilter))
            {
               Debug.LogError("MeshFilter not found");
               return;
            }

            _mesh = meshFilter.sharedMesh;
            if (_mesh == null)
            {
               Debug.LogError("Mesh not found");
               return;
            }
         }

         var triangles = _mesh.triangles;
         var vertices = _mesh.vertices;
         var uvs = _mesh.uv;

         var triangleCount = _mesh.triangles.Length / 3;

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
         Graphics.ExecuteCommandBuffer(cmd);
         CommandBufferPool.Release(cmd);

         var result = new Vector4[vertices.Length];
         resultBuffer.GetData(result);
         
         var resultUV = new Vector2[vertices.Length];
         resultUVBuffer.GetData(resultUV);
         
         _mesh.SetUVs(7, result);
         _mesh.SetUVs(6, resultUV);

         #region -------------------- Dispose -----------------------

         verticesBuffer.Dispose();
         trianglesBuffer.Dispose();
         resultBuffer.Dispose();
         resultUVBuffer.Dispose();
         uvsBuffer.Dispose();

         #endregion ------------------------------------------------
      }
      
      #endregion
      
      private struct ShaderId
      {
         public static int TriangleCount = Shader.PropertyToID("TriangleCount");
         public static int Vertices = Shader.PropertyToID("Vertices");
         public static int Triangles = Shader.PropertyToID("Triangles");
         public static int Normals = Shader.PropertyToID("Normals");
         public static int UVs = Shader.PropertyToID("UVs");
         public static int MiddleResult = Shader.PropertyToID("CSMiddlePointResult");
         public static int MiddleUVResult = Shader.PropertyToID("CSMiddlePointResultUVs");

         public struct DivideResult
         {
            public static int Vertices = Shader.PropertyToID("CSDivideTrianglesResultNewVertices");
            public static int Triangles = Shader.PropertyToID("CSDivideTrianglesResultNewTriangles");
            public static int UVs = Shader.PropertyToID("CSDivideTrianglesResultNewUVs");
            public static int Normals = Shader.PropertyToID("CSDivideTrianglesResultNewNormals");
            public static int TriangleIndex = Shader.PropertyToID("CSDivideTrianglesResultTriangleIndex");
         }
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
