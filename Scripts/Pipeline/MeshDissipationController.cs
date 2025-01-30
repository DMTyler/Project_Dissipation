using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DGraphics.Dissipation
{ 
    public class MeshDissipationController 
    {
        private static List<(
            VertexAttribute attribute,
            VertexAttributeFormat format,
            int dimension,
            int stream
            )> _attributeSequence = new()
        {
            (VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            (VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 0),
            (VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 0),
            (VertexAttribute.TexCoord6, VertexAttributeFormat.Float32, 2, 0),
            (VertexAttribute.TexCoord7, VertexAttributeFormat.Float32, 4, 0)
        };

        private static List<MeshDissipationInfo> _meshDissipationInfos = new();
        private static int _kernelID = 0;
        
        public struct ShaderTags
        {
            # region Shader Property IDs
            public static int T = Shader.PropertyToID("T");
            public static int VertexBuffer = Shader.PropertyToID("VertexBuffer");
            public static int InitialPositions = Shader.PropertyToID("InitialPositions");
            public static int ObjectToWorldOnStart = Shader.PropertyToID("ObjectToWorldOnStart");
            public static int IsStarted = Shader.PropertyToID("IsStarted");
            
            public static int GlobalSimulationMode = Shader.PropertyToID("GlobalSimulationMode");
            public static int BaseDirection = Shader.PropertyToID("BaseDirection");
            public static int DirectionSimulationMode = Shader.PropertyToID("DirectionSimulationMode");
            public static int EnableRandomAngle = Shader.PropertyToID("EnableRandomAngle");
            public static int RandomAngleRange = Shader.PropertyToID("RandomAngleRange");
            public static int SpeedMode = Shader.PropertyToID("SpeedMode");
            public static int Speed = Shader.PropertyToID("Speed");
            public static int MinSpeed = Shader.PropertyToID("MinSpeed");
            public static int MaxSpeed = Shader.PropertyToID("MaxSpeed");
            public static int SpeedCurveSums = Shader.PropertyToID("SpeedCurveSums");
            public static int SpeedCurveSampleCount = Shader.PropertyToID("SpeedCurveSampleCount");
            public static int EnableRandomLifeTime = Shader.PropertyToID("EnableRandomLifeTime");
            public static int LifeTime = Shader.PropertyToID("LifeTime");
            public static int MinLifeTime = Shader.PropertyToID("MinLifeTime");
            public static int MaxLifeTime = Shader.PropertyToID("MaxLifeTime");
            public static int StartTimeMode = Shader.PropertyToID("StartTimeMode");
            public static int MaxStartTime = Shader.PropertyToID("MaxStartTime");
            public static int GreyMap = Shader.PropertyToID("GreyMap");
            public static int BaseMaxStartTime = Shader.PropertyToID("BaseMaxStartTime");
            public static int RandomStartTimeRange = Shader.PropertyToID("RandomStartTimeRange");
            public static int DirectionRandomSeed = Shader.PropertyToID("DirectionRandomSeed");
            public static int SpeeedRandomSeed = Shader.PropertyToID("SpeedRandomSeed");
            public static int LifeTimeRandomSeed = Shader.PropertyToID("LifeTimeRandomSeed");
            public static int StartTimeRandomSeed = Shader.PropertyToID("StartTimeRandomSeed");
            public static int EnableProcessDisplacement = Shader.PropertyToID("EnableProcessDisplacement");
            public static int MaxDisplacementAmplitude = Shader.PropertyToID("MaxDisplacementAmplitude");
            public static int MinDisplacementFrequency = Shader.PropertyToID("MinDisplacementFrequency");
            public static int MaxDisplacementFrequency = Shader.PropertyToID("MaxDisplacementFrequency");
            public static int DisplacementWaveCount = Shader.PropertyToID("DisplacementWaveCount");
            public static int DisplacementRandomSeed = Shader.PropertyToID("DisplacementRandomSeed");
            
            public static int ObjectToWorldMat = Shader.PropertyToID("ObjectToWorldMat");
            public static int WorldToObjectMat = Shader.PropertyToID("WorldToObjectMat");
            # endregion
            public static void SetAnimParams(IAnimParams animParams, CommandBuffer cmd, ComputeShader cs,
                int kernelID)
            {
                cmd.SetComputeIntParam(cs, GlobalSimulationMode, animParams.GlobalSimulationMode);
                cmd.SetComputeVectorParam(cs, BaseDirection, animParams.BaseDirection);
                cmd.SetComputeIntParam(cs, DirectionSimulationMode, animParams.DirectionSimulationMode);
                cmd.SetComputeIntParam(cs, EnableRandomAngle, animParams.EnableRandomDirection ? 1 : 0);
                cmd.SetComputeFloatParam(cs, RandomAngleRange, animParams.RandomAngleRange);
                cmd.SetComputeIntParam(cs, SpeedMode, animParams.SpeedMode);
                cmd.SetComputeFloatParam(cs, Speed, animParams.ConstantSpeed);
                cmd.SetComputeFloatParam(cs, MinSpeed, animParams.MinSpeed);
                cmd.SetComputeFloatParam(cs, MaxSpeed, animParams.MaxSpeed);
                cmd.SetComputeBufferParam(cs, kernelID, SpeedCurveSums, animParams.SpeedCurveBuffer);
                cmd.SetComputeIntParam(cs, SpeedCurveSampleCount, animParams.SpeedCurveSampleCount);
                cmd.SetComputeIntParam(cs, EnableRandomLifeTime, animParams.EnableRandomLifeTime ? 1 : 0);
                cmd.SetComputeFloatParam(cs, LifeTime, animParams.LifeTime);
                cmd.SetComputeFloatParam(cs, MinLifeTime, animParams.MinLifeTime);
                cmd.SetComputeFloatParam(cs, MaxLifeTime, animParams.MaxLifeTime);
                cmd.SetComputeIntParam(cs, StartTimeMode, animParams.StartTimeMode);
                cmd.SetComputeFloatParam(cs, MaxStartTime, animParams.MaxStartTime);
                cmd.SetComputeFloatParam(cs, BaseMaxStartTime, animParams.BaseMaxStartTime);
                cmd.SetComputeFloatParam(cs, RandomStartTimeRange, animParams.RandomStartTimeRange);
                cmd.SetComputeIntParam(cs, DirectionRandomSeed, animParams.DirectionRandomSeed);
                cmd.SetComputeIntParam(cs, SpeeedRandomSeed, animParams.SpeedRandomSeed);
                cmd.SetComputeIntParam(cs, LifeTimeRandomSeed, animParams.LifeTimeRandomSeed);
                cmd.SetComputeIntParam(cs, StartTimeRandomSeed, animParams.StartTimeRandomSeed);
                cmd.SetComputeIntParam(cs, EnableProcessDisplacement, animParams.EnableProcessDisplacement ? 1 : 0);
                cmd.SetComputeFloatParam(cs, MaxDisplacementAmplitude, animParams.MaxDisplacementAmplitude);
                cmd.SetComputeFloatParam(cs, MinDisplacementFrequency, animParams.MinDisplacementFrequency);
                cmd.SetComputeFloatParam(cs, MaxDisplacementFrequency, animParams.MaxDisplacementFrequency);
                cmd.SetComputeIntParam(cs, DisplacementWaveCount, animParams.DisplacementWaveCount);
                cmd.SetComputeIntParam(cs, DisplacementRandomSeed, animParams.DisplacementRandomSeed);
            }
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct VertexData
        {
            public Vector3 position;
            public Vector3 normal;
            public Vector2 uv;
            public Vector2 uv6;
            public Vector4 uv7;
        }

        public static void InjectCommand(CommandBuffer cmd, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_meshDissipationInfos.Count == 0)
            {
                return;
            }

            if (ComputeShaderManager.Instance.MeshTransformer == null)
            {
                Debug.LogWarning("Set Up Not Complete in ComputeShaderManager: MeshTransformer is null.");
                return;
            }

            var transformerCS = ComputeShaderManager.Instance.MeshTransformer;
            
            foreach (var info in _meshDissipationInfos)
            {
                if (info.IsActive == false) continue;
                
                var kernel = _kernelID;
                
                ShaderTags.SetAnimParams(info.AnimParams, cmd, transformerCS, kernel);
                
                cmd.SetComputeFloatParam(transformerCS, ShaderTags.T, info.T);
                
                for (var i = 0; i < info.MeshCount; i++)
                {
                    var positions = info.InitialPositionsBuffers[i];
                    var buffer = info.VertexBuffers[i];
                    var objectToWorldOnStartBuffer = info.ObjectToWorldOnStartBuffers[i];
                    var isStartedBuffer = info.IsStartedBuffers[i];
                    
                    cmd.SetComputeMatrixParam(transformerCS, ShaderTags.ObjectToWorldMat, info.Transforms[i].localToWorldMatrix);
                    cmd.SetComputeMatrixParam(transformerCS, ShaderTags.WorldToObjectMat, info.Transforms[i].worldToLocalMatrix);
                    
                    cmd.SetComputeBufferParam(transformerCS, kernel, ShaderTags.ObjectToWorldOnStart, objectToWorldOnStartBuffer);
                    cmd.SetComputeBufferParam(transformerCS, kernel, ShaderTags.IsStarted, isStartedBuffer);
                    cmd.SetComputeBufferParam(transformerCS, kernel, ShaderTags.InitialPositions, positions);
                    cmd.SetComputeBufferParam(transformerCS, kernel, ShaderTags.VertexBuffer , buffer);
                    
                    var greyMap = info.AnimParams.GreyMapRTs[i];
                    cmd.SetComputeTextureParam(transformerCS, kernel, ShaderTags.GreyMap, greyMap);
                    
                    var numThread = new NumThreads(transformerCS, kernel);
                    var vertexCount = info.VertexCounts[i];
                    cmd.DispatchCompute(transformerCS, kernel, Mathf.CeilToInt((float)vertexCount / numThread.x), 1, 1);
                }
                
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                context.Submit();
                
                if (info.IsRequestingStop) info.Stop(); // Stop animation at next frame.
            }
        }

        public static bool Register(MeshDissipationInfo info, out string error)
        {
            error = null;
            if (info == null)
            {
                error = "MeshDissipationInfo is null. ";
                return false;
            }

            if (info.VertexBuffers == null || info.VertexBuffers.Count == 0)
            {
                error = "Vertex buffer list is empty. ";
                return false;
            }
            _meshDissipationInfos.Add(info);
            return true;
        }

        public static void Unregister(MeshDissipationInfo info)
        {
            if (info == null) 
                return;
            _meshDissipationInfos.Remove(info);
        }

        public static bool SatisfyVertexAttributes(Mesh mesh, out string error)
        {
            var satisfied = true;
            error = null;
            var attributes = mesh.GetVertexAttributes();
            for (var i = 0; i < _attributeSequence.Count; i++)
            {
                if (attributes[i].attribute != _attributeSequence[i].attribute)
                {
                    satisfied = false;
                    error += $"Attribute {i}: {_attributeSequence[i].attribute} not found, ";
                }

                if (attributes[i].format != _attributeSequence[i].format)
                {
                    satisfied = false;
                    error +=
                        $"Attribute {i}: {_attributeSequence[i].attribute} not satisfying format: {_attributeSequence[i].format}, ";
                }

                if (attributes[i].dimension != _attributeSequence[i].dimension)
                {
                    satisfied = false;
                    error +=
                        $"Attribute {i}: {_attributeSequence[i].attribute} not satisfying dimension: {_attributeSequence[i].dimension}, ";
                }

                if (attributes[i].stream != _attributeSequence[i].stream)
                {
                    satisfied = false;
                    error +=
                        $"Attribute {i}: {_attributeSequence[i].attribute} not satisfying stream: {_attributeSequence[i].stream}, ";
                }
            }

            return satisfied;
        }
        
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            var instances = UnityEngine.Object.FindObjectsOfType<MeshDissipationSetter>();
            foreach (var instance in instances)
            {
                instance.Reset();
            }
        }
    }
    
    public class MeshDissipationInfo : IDisposable
    {
        public readonly int MeshCount;
        public readonly IReadOnlyList<ComputeBuffer> InitialPositionsBuffers;
        public readonly IReadOnlyList<GraphicsBuffer> VertexBuffers;
        public readonly IReadOnlyList<int> VertexCounts;
        public readonly IReadOnlyList<Transform> Transforms;
        
        public readonly IAnimParams AnimParams;
        
        public readonly List<ComputeBuffer> ObjectToWorldOnStartBuffers = new();
        public readonly List<ComputeBuffer> IsStartedBuffers = new();
        
        public float T { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsRequestingStop { get; private set; }

        /// <summary>
        /// Update T value to add a certain increment.
        /// </summary>
        /// <returns>True if T value reaches 1.0f, indicating animation loop is complete.</returns>
        public bool Update()
        {
            var lifeTime = AnimParams.EnableRandomLifeTime? AnimParams.MaxLifeTime : AnimParams.LifeTime;

            if (AnimParams.StartTimeMode == (int)MeshDissipationAnimParams.AnimStartTimeMode.RandomUnderConstant)
                lifeTime += AnimParams.MaxStartTime;
            else
                lifeTime += (AnimParams.BaseMaxStartTime + AnimParams.RandomStartTimeRange);
            
            var temp = T;
            T = (T + Time.deltaTime) % lifeTime;
            
            if (temp + Time.deltaTime >= lifeTime)
            {
                ReleaseSingleAnimLoopResources();
                GenerateSingleAnimLoopResources();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reset T value to 0.
        /// </summary>
        public void Reset()
        {
            T = 0.0f;
            
            ReleaseSingleAnimLoopResources();
            GenerateSingleAnimLoopResources();
        }

        public void RequestStop()
        {
            IsRequestingStop = true;
        }

        public void Stop()
        {
            IsActive = false;
            ReleaseSingleAnimLoopResources();
        }

        public void Start()
        {
            if (IsActive) return;
            IsActive = true;
            IsRequestingStop = false;

            ReleaseSingleAnimLoopResources();
            GenerateSingleAnimLoopResources();
        }
        
        public MeshDissipationInfo(
            int MeshCount, 
            IReadOnlyList<ComputeBuffer> initialPositionsBuffers, 
            IReadOnlyList<GraphicsBuffer> vertexBuffers, 
            IReadOnlyList<int> vertexCounts,
            IReadOnlyList<Transform> transforms,
            IAnimParams animParams
            )
        {
            this.MeshCount = MeshCount;
            if (vertexBuffers.Count != MeshCount || vertexCounts.Count != MeshCount || initialPositionsBuffers.Count != MeshCount)
                throw new System.ArgumentException("Vertex buffers, vertex counts and initial positions count should match mesh count in MeshDissipationInfo.");
            InitialPositionsBuffers = new List<ComputeBuffer>(initialPositionsBuffers);
            VertexBuffers = new List<GraphicsBuffer>(vertexBuffers);
            VertexCounts = new List<int>(vertexCounts);
            Transforms = new List<Transform>(transforms);
            AnimParams = animParams;
        }

        private void GenerateSingleAnimLoopResources()
        {
            foreach (var count in VertexCounts)
            {
                var objBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(Matrix4x4)));
                var objBufferData = new Matrix4x4[count];
                for (var i = 0; i < count; i++)
                {
                    objBufferData[i] = Matrix4x4.identity;
                }
                objBuffer.SetData(objBufferData);
                ObjectToWorldOnStartBuffers.Add(objBuffer);
                
                var isStartedBuffer = new ComputeBuffer(count, sizeof(int));
                isStartedBuffer.SetData(new int[count]);
                IsStartedBuffers.Add(isStartedBuffer);
            }
        }

        private void ReleaseSingleAnimLoopResources()
        {
            if (ObjectToWorldOnStartBuffers.Count > 0)
            {
                ObjectToWorldOnStartBuffers.ForEach(buffer => buffer.Dispose());
            }

            if (IsStartedBuffers.Count > 0)
            {
                IsStartedBuffers.ForEach(buffer => buffer.Dispose());
            }
            
            ObjectToWorldOnStartBuffers.Clear();
            IsStartedBuffers.Clear();
        }

        public void Dispose()
        {
            foreach (var buffer in VertexBuffers)
            {
                buffer.Release();
            }
            
            foreach (var buffer in InitialPositionsBuffers)
            {
                buffer.Release();
            }
            
            foreach (var buffer in ObjectToWorldOnStartBuffers)
            {
                buffer.Release();
            }

            foreach (var buffer in IsStartedBuffers)
            {
                buffer.Release();
            }
        }
    }
}