using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;


namespace DGraphics.Dissipation
{
    #region Main Class
    
    /// <summary>
    /// Setup transformation of vertices
    /// </summary>
    [ExecuteAlways]
    public class MeshDissipationSetter : MonoBehaviour
    {
        #region Animation Params
        public MeshDissipationAnimParams AnimParams = new();
        #endregion

        #region Other Params
        
        [Serializable]
        public struct MeshTransformPair
        {
            public Mesh Mesh;
            public Transform Transform;
        }
        
        [SerializeField] private List<MeshTransformPair> _meshTransformPair = new();
        #endregion
        
        #region Fields

        private bool _initialized;
        public bool Initialized => _initialized;
        private MeshDissipationInfo _info;

        #endregion
        
        #region Button Functions
        public void SelectAllMeshes()
        {
            _meshTransformPair.AddRange(GetComponentsInChildren<MeshFilter>().Select(f => new MeshTransformPair
            {
                Mesh = f.sharedMesh,
                Transform = f.transform
            }).Where(p => !_meshTransformPair.Select(x => x.Mesh).Contains(p.Mesh)));
            
            _meshTransformPair.AddRange(GetComponentsInChildren<SkinnedMeshRenderer>().Select(r => new MeshTransformPair
            {
                Mesh = r.sharedMesh,
                Transform = r.transform
            }).Where(p => !_meshTransformPair.Select(x => x.Mesh).Contains(p.Mesh)));
        }
        
        public void Setup()
        {
            _isStarted = false;
            _isPaused = false;

            var meshes = _meshTransformPair.Select(x => x.Mesh).ToList();
            if (meshes.Count == 0)
            {
                Debug.LogError($"Null Mesh List for Script: {nameof(MeshDissipationSetter)}");
                return;
            }
            
            var vertexBuffers = new List<GraphicsBuffer>();
            var vertexStrides = new List<int>();
            var vertexOffsets = new List<int>();
            
            var uv6Buffers = new List<GraphicsBuffer>();
            var uv6Strides = new List<int>();
            var uv6Offsets = new List<int>();
            
            var uv7Buffers = new List<GraphicsBuffer>();
            var uv7Strides = new List<int>();
            var uv7Offsets = new List<int>();
            
            var counts = new List<int>();
            var initialPositionBuffers = new List<ComputeBuffer>();

            foreach (var mesh in meshes)
            {
                if (!mesh.isReadable)
                {
                    Debug.LogError($"Mesh {mesh.name} is not readable. Please enable Read/Write in the import settings.");
                    continue;
                }
                
                // Check if vertices attributes satisfy the requirements
                if (!MeshDissipationController.SatisfyVertexAttributes(mesh, out var error))
                {
                    Debug.LogError($"Mesh Attributes Requirement Not Satisfied. " +
                                   $"Did you forget to decompose mesh with {nameof(MeshDecomposer)} first before setup? \n" +
                                   $"Error message: {error}");
                    return;
                }

                // Retrieve vertex data
                if (!MeshDissipationController.TryGetGraphicsBuffer(mesh, VertexAttribute.Position,
                        out var vertexBuffer, out var vertexStride, out var vertexOffset) ||
                    !MeshDissipationController.TryGetGraphicsBuffer(mesh, VertexAttribute.TexCoord6, 
                        out var uv6Buffer, out var uv6Stride, out var uv6Offset) ||
                    !MeshDissipationController.TryGetGraphicsBuffer(mesh, VertexAttribute.TexCoord7, 
                        out var uv7Buffer, out var uv7Stride, out var uv7Offset))
                {
                    Debug.LogError($"Mesh {mesh.name} does not have required attributes. ");
                    return;
                }
                
                // Retrieve initial position data
                var vertexCount = mesh.vertexCount;
                var bufferData = new byte[vertexCount * vertexStride];
                vertexBuffer.GetData(bufferData);
                
                var initialPos = new Vector3[vertexCount];
                for (var i = 0; i < vertexCount; i++)
                {
                    var byteIndex = i * vertexStride + vertexOffset;
                    var position = new Vector3(
                        BitConverter.ToSingle(bufferData, byteIndex),
                        BitConverter.ToSingle(bufferData, byteIndex + 4),
                        BitConverter.ToSingle(bufferData, byteIndex + 8));
                    initialPos[i] = position;
                }

                var initialPositionBuffer = new ComputeBuffer(mesh.vertexCount, sizeof(float) * 3, ComputeBufferType.Default);
                initialPositionBuffer.SetData(initialPos);
                
                initialPositionBuffers.Add(initialPositionBuffer);
                vertexBuffers.Add(vertexBuffer);
                
                // The same GraphicsBuffer can only be bound to the same ByteAddressBuffer.
                // Considering that UV6 and UV7 are read-only,
                // we can use copying instead.
                uv6Buffers.Add(uv6Buffer.Copy()); 
                uv7Buffers.Add(uv7Buffer.Copy());
                
                vertexStrides.Add(vertexStride);
                uv6Strides.Add(uv6Stride);
                uv7Strides.Add(uv7Stride);
                
                vertexOffsets.Add(vertexOffset);
                uv6Offsets.Add(uv6Offset);
                uv7Offsets.Add(uv7Offset);
                
                counts.Add(mesh.vertexCount);
                
                uv6Buffer.Dispose();
                uv7Buffer.Dispose();
            }

            AnimParams.SetMeshNames(meshes.Select(x => x.name).ToList());
            AnimParams.Init();
            
            var animParams = AnimParams;
            
            _info = new MeshDissipationInfo(
                meshes.Count, 
                initialPositionBuffers, 
                
                vertexBuffers, 
                vertexStrides, 
                vertexOffsets, 
                
                uv6Buffers, 
                uv6Strides, 
                uv6Offsets, 
                
                uv7Buffers, 
                uv7Strides, 
                uv7Offsets, 
                counts, 
                _meshTransformPair.Select(m => m.Transform).ToList(),
                animParams);
            
            if (!this.Register(_info, out var error2))
            {
                Debug.LogError("Failed to register MeshDissipationController.\n" +
                               $"Error message: {error2}");
                return;
            };
            
            _initialized = true;
            
        }
        
        public void Reset()
        {
            _isStarted = false;
            _isPaused = false;
            if (_info != null)
            {
                _info.Reset();
                _info.Stop();
                this.Unregister(_info);
                _info.Dispose();
                _info = null;
            }
            _initialized = false;
        }
        # endregion

        #region Animation Functions
        
        private bool _isStarted;
        public bool IsStarted => _isStarted;
        private bool Startable() => (_initialized && !_isStarted);
        public void StartAnim()
        {
            if (!_initialized)
            {
                Debug.LogError($"Please initialize {nameof(MeshDissipationSetter)} first. \n" +
                               $"GameObject: {gameObject.name}");
                return;
            }
            
            if (!Startable()) 
                return;
            
            if (_info == null)
            {
                Debug.LogError($"Please initialize {nameof(MeshDissipationSetter)} first. \n" +
                               $"GameObject: {gameObject.name}");
                return;
            }
            
            _info.Start();
            _isStarted = true;
        }
        
        private bool _isPaused;
        public bool IsPaused => _isPaused;
        private bool Pauseable() => _initialized && _isStarted && !_isPaused;
        public void PauseAnim()
        {
            if (!_initialized)
            {
                Debug.LogError($"Please initialize {nameof(MeshDissipationSetter)} first. \n" +
                               $"GameObject: {gameObject.name}");
                return;
            }
            
            if (!Pauseable()) 
                return;
            
            if (_info == null)
            {
                Debug.LogError($"Please initialize {nameof(MeshDissipationSetter)} first. \n" +
                               $"GameObject: {gameObject.name}");
                return;
            }
            
            _info.RequestStop();
            _isPaused = true;
        }
        
        private bool Continuable() => _initialized && _isStarted && _isPaused;
        public void ContinueAnim()
        {
            if (!_initialized)
            {
                Debug.LogError($"Please initialize {nameof(MeshDissipationSetter)} first. \n" +
                               $"GameObject: {gameObject.name}");
                return;
            }
            
            if (!Continuable()) 
                return;
            
            if (_info == null)
            {
                Debug.LogError($"Please initialize {nameof(MeshDissipationSetter)} first. \n" +
                               $"GameObject: {gameObject.name}");
                return;
            }
            
            _info.Start();
            _isPaused = false;
        }
        
        private bool Stoppable() => _initialized && _isStarted;
        public void StopAnim()
        {
            if (!_initialized)
            {
                Debug.LogError($"Please initialize {nameof(MeshDissipationSetter)} first. \n" +
                               $"GameObject: {gameObject.name}");
                return;
            }
            
            if (!Stoppable()) 
                return;
            
            if (_info == null)
            {
                Debug.LogError($"Please initialize {nameof(MeshDissipationSetter)} first. \n" +
                               $"GameObject: {gameObject.name}");
                return;
            }
            
            _info.Reset();
            _info.RequestStop();
            _isStarted = false;
            _isPaused = false;
        }

        #endregion
        
        #region Unity Events

        private void Update()
        {
            if (_isStarted && !_isPaused && _initialized)
            {
                if (_info == null)
                {
                    Debug.LogError($"Please initialize {nameof(MeshDissipationSetter)} first. \n" +
                                   $"GameObject: {gameObject.name}");
                    return;
                }
                _info.Update();
            }
                
        }
        private void Start()
        {
            if (Application.isPlaying)
                Setup();
        }

        private void OnDisable()
        {
            Reset();
            AnimParams?.Dispose();
        }
        
        private void OnDestroy()
        {
            Reset();
            AnimParams?.Dispose();
        }
        #endregion
    }
    #endregion
    
    #region Parameter Class

    [Serializable]
    public class MeshDissipationAnimParams : IDisposable
    {
        public enum SimulationMode
        {
            World = 1,
            Object = 2,
        }

        public SimulationMode GlobalSimulationMode = SimulationMode.World;
        public Vector3 BaseDirection = new Vector3(1, 0, 0);
        public SimulationMode DirectionSimulationMode = SimulationMode.World;
        public bool EnableRandomDirection;
        public float RandomAngleRange = 45f;
        
        public enum AnimSpeedMode
        {
            Constant = 1,
            RandomBetweenTwoConstants = 2,
            Curve = 3,
        }
        
        public AnimSpeedMode SpeedMode = AnimSpeedMode.Constant;
        public float ConstantSpeed = 1f;
        public float MinSpeed = 1f;
        public float MaxSpeed = 1f;
        public AnimationCurve SpeedCurve = AnimationCurve.Linear(0, 1, 1, 1);
        
        public bool EnableRandomLifeTime;
        
        private bool _enableConstantLifeTime => !EnableRandomLifeTime;
        public float LifeTime = 1f;
        public float MinLifeTime = 1f;
        public float MaxLifeTime = 1f;

        public enum AnimStartTimeMode
        {
            RandomUnderConstant = 1,
            RandomBasedOnGreyMap = 2,
        }
        
        public AnimStartTimeMode StartTimeMode = AnimStartTimeMode.RandomUnderConstant;
        public float MaxStartTime = 1f;
        private bool MeshNameLengthValid() => MeshNames.Count != 0;
        private bool GreyMapLengthValid() => MeshNames.Count == GreyMapTextures.Count || MeshNames.Count == 0;
        public List<Texture2D> GreyMapTextures = new();
        public float BaseMaxStartTime = 1f;
        public float RandomStartTimeRange = 0.1f;
        
        public bool EnableProcessDisplacement;
        public float MaxDisplacementAmplitude = 0.1f;
        
        public List<string> MeshNames = new();
        public int CurveSampleCountPerSecond = 30;
        public int SpeedCurveSampleCount;
        public List<float> SpeedCurveSums = new();

        private static int[] GreyMapResolutions = { 128, 256, 512, 1024 };
        public int GreyMapResolution = 128;
        public int DisplacementWaveCount = 4;
        public float MinDisplacementFrequency = 0.5f;
        public float MaxDisplacementFrequency = 1f;
        public int DirectionRandomSeed = 42;
        private void GenerateDirRandomSeed()
        {
            DirectionRandomSeed = UnityEngine.Random.Range(0, 255);
        }
        public int SpeedRandomSeed = 37;
        private void GenerateSpdRandomSeed()
        {
            SpeedRandomSeed = UnityEngine.Random.Range(0, 255);
        }
        public int LifeTimeRandomSeed = 128;
        private void GenerateLTimeRandomSeed()
        {
            LifeTimeRandomSeed = UnityEngine.Random.Range(0, 255); 
        }
        public int StartTimeRandomSeed = 1;
        private void GenerateStTimeRandomSeed()
        {
            StartTimeRandomSeed = UnityEngine.Random.Range(0, 255);
        }
        public int DisplacementRandomSeed = 123;
        private void GeneratePDisRandomSeed()
        {
            DisplacementRandomSeed = UnityEngine.Random.Range(0, 255);
        }
        public ComputeBuffer SpeedCurveBuffer { get; private set; }
        public IReadOnlyList<RenderTexture> GreyMapRTs { get; private set; }

        private void GenerateGreyMapTextureComputeBuffers()
        {
            if (GreyMapRTs != null && GreyMapRTs.Count > 0)
            {
                GreyMapRTs.ToList().ForEach(b => b.Release());
            }
            
            var bufferList = new List<RenderTexture>();
            
            for (var i = 0; i < MeshNames.Count; i++)
            {
                var greyMap = Texture2D.blackTexture;
                if (i >= GreyMapTextures.Count || GreyMapTextures[i] == null)
                {
                    if (StartTimeMode == AnimStartTimeMode.RandomBasedOnGreyMap)
                        Debug.LogWarning($"Mesh {MeshNames[i]} has no grey map texture. Black texture will be used.");
                }
                else
                {
                    greyMap = GreyMapTextures[i];
                }
                
                var rt = new RenderTexture(GreyMapResolution, GreyMapResolution, 0, RenderTextureFormat.RFloat);
                rt.filterMode = FilterMode.Bilinear;
                rt.wrapMode = TextureWrapMode.Clamp;
                rt.Create();
                
                var mat = new Material(Shader.Find("Hidden/DGraphics/RedChannelOnly"));
                Graphics.Blit(greyMap, rt, mat);
                bufferList.Add(rt);
            }
            
            GreyMapRTs = bufferList;
        }

        private void SampleSpeedCurve()
        {
            SpeedCurveSums.Clear();
            var curve = SpeedCurve;
            SpeedCurveSampleCount = EnableRandomLifeTime
                ? Mathf.CeilToInt(MaxLifeTime * CurveSampleCountPerSecond)
                : Mathf.CeilToInt(LifeTime * CurveSampleCountPerSecond);

            var deltaT = 1f / CurveSampleCountPerSecond;
            var sum = 0f;

            for (int i = 0; i < SpeedCurveSampleCount; i++)
            {
                var t = i / (float)(SpeedCurveSampleCount - 1); // 归一化时间 [0, 1]
                var currentValue = curve.Evaluate(t);

                if (i == 0 || i == SpeedCurveSampleCount - 1)
                {
                    sum += currentValue * deltaT * 0.5f;
                }
                else
                {
                    sum += currentValue * deltaT;
                }

                SpeedCurveSums.Add(sum);
            }
        }

        private void MinMaxTest()
        {
            if (MinLifeTime > MaxLifeTime)
                MinLifeTime = MaxLifeTime;
            
            if (MinSpeed > MaxSpeed)
                MinSpeed = MaxSpeed;
            
            if (LifeTime < 0) LifeTime = 0;
            if (MinLifeTime < 0.01f) MinLifeTime = 0.01f;
            if (MaxLifeTime < 0.01f) MaxLifeTime = 0.01f;
            if (MinSpeed < 0) MinSpeed = 0;
            if (MaxSpeed < 0) MaxSpeed = 0;
            if (MaxStartTime < 0) MaxStartTime = 0;
            if (BaseMaxStartTime < 0) BaseMaxStartTime = 0;
            if (RandomStartTimeRange < 0) RandomStartTimeRange = 0;
            if (RandomStartTimeRange > BaseMaxStartTime) 
                RandomStartTimeRange = BaseMaxStartTime;
            if (MaxDisplacementAmplitude < 0.01f) MaxDisplacementAmplitude = 0.01f;
            if (MinDisplacementFrequency < 0.01f) MinDisplacementFrequency = 0.01f;
            if (MaxDisplacementFrequency < 0.01f) MaxDisplacementFrequency = 0.01f;
            if (DisplacementWaveCount < 1) DisplacementWaveCount = 1;
            if (!GreyMapResolutions.Contains(GreyMapResolution)) GreyMapResolution = 128;
            
            RandomAngleRange = Mathf.Clamp(RandomAngleRange, 0, 180);
        }

        public void Init()
        {
            MinMaxTest();
            SampleSpeedCurve();
            SpeedCurveBuffer?.Dispose();
            SpeedCurveBuffer = new ComputeBuffer(SpeedCurveSampleCount, sizeof(float), ComputeBufferType.Default);
            SpeedCurveBuffer.SetData(SpeedCurveSums);
            if (BaseDirection.magnitude == 0) BaseDirection = Vector3.right;
            BaseDirection = BaseDirection.normalized;
            GenerateGreyMapTextureComputeBuffers();
        }

        public void SetMeshNames(List<string> meshNames)
        {
            MeshNames = new List<string>(meshNames);
        }

        public void Dispose()
        {
            SpeedCurveBuffer?.Dispose();
            GreyMapRTs?.ToList().ForEach(b => b.Release());
        }
    }
    
    #endregion
}