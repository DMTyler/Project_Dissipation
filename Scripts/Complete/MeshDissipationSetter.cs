using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;


namespace DGraphics.Dissipation
{
    #region Main Class
    
    /// <summary>
    /// Setup transformation of vertices
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshDecomposer))]
    public class MeshDissipationSetter : MonoBehaviour
    {
        #region Animation Params
        [UnityEngine.Header("Animation Params"), LabelText("Animation Params")]
        public MeshDissipationAnimParams AnimParams = new();
        #endregion

        #region Other Params
        
        [Space(10), Header("Other Params")]
        [SerializeField, LabelText("Mesh Filters")] private List<MeshFilter> _meshFilters = new();
        #endregion
        
        #region Fields

        private bool _initialized;
        private MeshDissipationInfo _info;

        #endregion
        
        #region Button Functions
        
        [Button("Select All Meshes")]
        private void SelectAllMeshes()
        {
            _meshFilters = GetComponentsInChildren<MeshFilter>().ToList();
        }

        [Button("Initialize")]
        public void Setup()
        {
            _isStarted = false;
            _isPaused = false;
            
            var meshes = _meshFilters.Select(x => x.sharedMesh).ToList();
            if (meshes.Count == 0)
            {
                Debug.LogError($"Null Mesh List for Script: {nameof(MeshDissipationSetter)}");
                return;
            }
            
            var buffers = new List<GraphicsBuffer>();
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
                mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                var vertexBuffer = mesh.GetVertexBuffer(0);
                var dataArray = new MeshDissipationController.VertexData[mesh.vertexCount];
                vertexBuffer.GetData(dataArray);
                var initialPos = new List<Vector3>();
                
                // Retrieve initial position data
                foreach (var data in dataArray)
                {
                    initialPos.Add(data.position);
                }

                var buffer = new ComputeBuffer(mesh.vertexCount, sizeof(float) * 3, ComputeBufferType.Default);
                buffer.SetData(initialPos);
                
                initialPositionBuffers.Add(buffer);
                buffers.Add(vertexBuffer);
                counts.Add(mesh.vertexCount);
            }

            AnimParams.Init();
            AnimParams.SetMeshNames(meshes.Select(x => x.name).ToList());
            
            var animParams = AnimParams;
            
            _info = new MeshDissipationInfo(
                meshes.Count, 
                initialPositionBuffers, 
                buffers, 
                counts, 
                _meshFilters.Select(m => m.transform).ToList(),
                animParams);
            
            if (!MeshDissipationController.Register(_info, out var error2))
            {
                Debug.LogError("Failed to register MeshDissipationController.\n" +
                               $"Error message: {error2}");
                return;
            };
            
            _initialized = true;
            
        }

        [Button("Reset")]
        public void Reset()
        {
            _isStarted = false;
            _isPaused = false;
            if (_info != null)
            {
                _info.Reset();
                _info.Stop();
                MeshDissipationController.Unregister(_info);
                _info.Dispose();
                _info = null;
            }
            _initialized = false;
        }
        # endregion

        #region Animation Functions
        
        private bool _isStarted;
        private bool Startable() => (_initialized && !_isStarted);
        [Button("Start"), Sirenix.OdinInspector.ShowIf("_initialized"), EnableIf(nameof(Startable)), ButtonGroup("Anim")]
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
        private bool Pauseable() => _initialized && _isStarted && !_isPaused;
        [Button("Pause"), EnableIf(nameof(Pauseable)), Sirenix.OdinInspector.ShowIf(nameof(Pauseable), false), ButtonGroup("Anim")]
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
        [Button("Continue"), EnableIf(nameof(Continuable)), Sirenix.OdinInspector.ShowIf(nameof(Continuable), false), ButtonGroup("Anim")]
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
        [Button("Stop"), EnableIf(nameof(Stoppable)), ShowIf("_initialized"), ButtonGroup("Anim")]
        private void StopAnim()
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
        #endregion
    }
    #endregion
    
    #region Parameter Class

    [Serializable]
    public class MeshDissipationAnimParams : IDisposable, IAnimParams
    {
        public enum SimulationMode
        {
            World = 1,
            Object = 2,
        }
        
        [Header("Basic Settings")]
        [LabelText("Global Simulation Mode")]
        public SimulationMode GlobalSimulationMode = SimulationMode.World;
        
        [Space(8)]
        [LabelText("Direction")]
        [OnValueChanged(nameof(Init))]
        public Vector3 BaseDirection = new Vector3(1, 0, 0);
        
        [LabelText("Direction Simulation Mode")]
        [OnValueChanged(nameof(Init))]
        public SimulationMode DirectionSimulationMode = SimulationMode.World;
        
        [LabelText("Enable Random Direction")]
        [OnValueChanged(nameof(Init))]
        public bool EnableRandomDirection;
        
        [Range(0, 180), LabelText("Max Random Angle"), ShowIf("EnableRandomDirection")] 
        [OnValueChanged(nameof(Init))]
        public float RandomAngleRange = 45f;
        
        public enum AnimSpeedMode
        {
            Constant = 1,
            RandomBetweenTwoConstants = 2,
            Curve = 3,
        }
        
        [Space(8)]
        [LabelText("Speed Mode")]
        [OnValueChanged(nameof(Init))]
        public AnimSpeedMode SpeedMode = AnimSpeedMode.Constant;

        [ShowIf("SpeedMode", AnimSpeedMode.Constant)]
        [Min(0), LabelText("Speed")]
        [OnValueChanged(nameof(Init))]
        public float ConstantSpeed = 1f;

        [ShowIf("SpeedMode", AnimSpeedMode.RandomBetweenTwoConstants)]
        [BoxGroup("Speed Range"), Min(0), LabelText("Min Velocity")]
        [OnValueChanged(nameof(Init))]
        public float MinSpeed = 1f;
        
        [ShowIf("SpeedMode", AnimSpeedMode.RandomBetweenTwoConstants)]
        [BoxGroup("Speed Range"), Min(0), LabelText("Max Velocity")]
        [OnValueChanged(nameof(Init))]
        public float MaxSpeed = 1f;

        [ShowIf("SpeedMode", AnimSpeedMode.Curve)]
        [OnValueChanged(nameof(Init))]
        [LabelText("Speed Curve")]
        public AnimationCurve SpeedCurve = AnimationCurve.Linear(0, 1, 1, 1);
        
        [Space(8)]
        [LabelText("Enable Random Lifetime")]
        [OnValueChanged(nameof(Init))]
        public bool EnableRandomLifeTime;
        
        private bool _enableConstantLifeTime => !EnableRandomLifeTime;
        [LabelText("Lifetime"), ShowIf("_enableConstantLifeTime"), Min(0.01f)]
        [OnValueChanged(nameof(Init))]
        public float LifeTime = 1f;
        
        [BoxGroup("Lifetime Range"), LabelText("Min Lifetime"), ShowIf("EnableRandomLifeTime")]
        [OnValueChanged(nameof(Init)), Min(0.01f)]
        public float MinLifeTime = 1f;
        
        [BoxGroup("lifetime Range"), LabelText("Max Lifetime"), ShowIf("EnableRandomLifeTime")]
        [OnValueChanged(nameof(Init)), Min(0.01f)]
        public float MaxLifeTime = 1f;

        public enum AnimStartTimeMode
        {
            RandomUnderConstant = 1,
            RandomBasedOnGreyMap = 2,
        }

        [Space(8)] [LabelText("Starting Time Mode")] 
        [OnValueChanged(nameof(Init))]
        public AnimStartTimeMode StartTimeMode = AnimStartTimeMode.RandomUnderConstant;
        
        [LabelText("Max Starting Time"), ShowIf("StartTimeMode", AnimStartTimeMode.RandomUnderConstant), Min(0)]
        [OnValueChanged(nameof(Init))]
        public float MaxStartTime = 1f;

        private bool MeshNameLengthValid() => MeshNames.Count != 0;
        private bool GreyMapLengthValid() => MeshNames.Count == GreyMapTextures.Count || MeshNames.Count == 0;
        [LabelText("Grey Map"), ShowIf("StartTimeMode", AnimStartTimeMode.RandomBasedOnGreyMap)]
        [ValidateInput(nameof(MeshNameLengthValid), "Mesh is null. Please initialize first.")]
        [ValidateInput(nameof(GreyMapLengthValid), "Grey map count does not match mesh count.", InfoMessageType.Warning)]
        [OnValueChanged(nameof(Init))]
        public List<Texture2D> GreyMapTextures = new();
        
        [LabelText("Base Max Starting Time"), ShowIf("StartTimeMode", AnimStartTimeMode.RandomBasedOnGreyMap), Min(0)]
        [OnValueChanged(nameof(Init))]
        public float BaseMaxStartTime = 1f;
        
        [LabelText("Random Start Time Range"), ShowIf("StartTimeMode", AnimStartTimeMode.RandomBasedOnGreyMap), Min(0)]
        [OnValueChanged(nameof(Init))]
        public float RandomStartTimeRange = 0.1f;
        
        [Space(8)]
        [LabelText("Enable Process Displacement")]
        [OnValueChanged(nameof(Init))]
        public bool EnableProcessDisplacement;
        
        [LabelText("Max Displacement Amplitude"), ShowIf("EnableProcessDisplacement"), Min(0.01f)]
        [OnValueChanged(nameof(Init))]
        public float MaxDisplacementAmplitude = 0.1f;
        
        [Space(8)]
        [FoldoutGroup("Advanced Settings", expanded: false)] 
        [LabelText("Mesh Names"), ReadOnly, Tooltip("Debug Only. If null or not the same as the context, initialization may fail, please reinitialize.")]
        public List<string> MeshNames = new();

        [Space(8)]
        [FoldoutGroup("Advanced Settings")] 
        [OnValueChanged(nameof(Init))]
        [LabelText("Curve Sample Count Per Sec"), Range(1, 240)] 
        [Tooltip("If the curve is relatively complex or if the curve parameters deviate significantly from the actual performance, try increasing this value.")]
        public int CurveSampleCountPerSecond = 30;

        [ShowIf("SpeedMode", AnimSpeedMode.Curve)]
        [FoldoutGroup("Advanced Settings"), ReadOnly, LabelText("Total Sample Count")]
        public int SpeedCurveSampleCount;
        
        [ShowIf("SpeedMode", AnimSpeedMode.Curve)]
        [FoldoutGroup("Advanced Settings"), ReadOnly, LabelText("Speed Curve Sums")]
        public List<float> SpeedCurveSums = new();

        private static int[] GreyMapResolutions = { 128, 256, 512, 1024 };
        [Space(8)] 
        [FoldoutGroup("Advanced Settings"), ShowIf("StartTimeMode", AnimStartTimeMode.RandomBasedOnGreyMap)] 
        [LabelText("Grey Map Resolution")]
        [ValueDropdown("GreyMapResolutions")]
        [OnValueChanged(nameof(Init))]
        public int GreyMapResolution = 128;
        
        [Space(8)]
        [FoldoutGroup("Advanced Settings"), ShowIf("EnableProcessDisplacement"), LabelText("Displacement Wave Count"), Min(1)]
        public int DisplacementWaveCount = 4;
        
        [BoxGroup("Advanced Settings/Displacement Wave Frequency")]
        [ShowIf("EnableProcessDisplacement"), LabelText("Min Displacement Frequency"), Min(0.01f)]
        public float MinDisplacementFrequency = 0.5f;
        
        [BoxGroup("Advanced Settings/Displacement Wave Frequency")]
        [ShowIf("EnableProcessDisplacement"), LabelText("Max Displacement Frequency"), Min(0.01f)]
        public float MaxDisplacementFrequency = 1f;
        
        [Space(8)]
        [HorizontalGroup("Advanced Settings/Direction Random Seed")]
        [LabelText("Direction Random Seed"), ShowIf("EnableRandomDirection"), Range(0, 255)]
        public int DirectionRandomSeed = 42;

        [HorizontalGroup("Advanced Settings/Direction Random Seed")]
        [Button("Randomize"), ShowIf("EnableRandomDirection")]
        private void GenerateDirRandomSeed()
        {
            DirectionRandomSeed = UnityEngine.Random.Range(0, 255);
        }
        
        [HorizontalGroup("Advanced Settings/Speed Random Seed")]
        [Range(0, 255)]
        [LabelText("Speed Random Seed"), ShowIf("SpeedMode", AnimSpeedMode.RandomBetweenTwoConstants)]
        public int SpeedRandomSeed = 37;

        [HorizontalGroup("Advanced Settings/Speed Random Seed")]
        [Button("Randomize"), ShowIf("SpeedMode", AnimSpeedMode.RandomBetweenTwoConstants)]
        private void GenerateSpdRandomSeed()
        {
            SpeedRandomSeed = UnityEngine.Random.Range(0, 255);
        }
        
        [HorizontalGroup("Advanced Settings/Lifetime Random Seed")]
        [LabelText("Lifetime Random Seed"), ShowIf("EnableRandomLifeTime"), Range(0, 255)]
        public int LifeTimeRandomSeed = 128;

        [HorizontalGroup("Advanced Settings/Lifetime Random Seed")]
        [Button("Randomize"), ShowIf("EnableRandomLifeTime")]
        private void GenerateLTimeRandomSeed()
        {
            LifeTimeRandomSeed = UnityEngine.Random.Range(0, 255); 
        }

        [Space(8)]
        [HorizontalGroup("Advanced Settings/Starting Time Random Seed")]
        [LabelText("Starting Time Random Seed"), Range(0, 255)]
        public int StartTimeRandomSeed = 1;

        [HorizontalGroup("Advanced Settings/Starting Time Random Seed")]
        [Button("Randomize")]
        private void GenerateStTimeRandomSeed()
        {
            StartTimeRandomSeed = UnityEngine.Random.Range(0, 255);
        }
        
        [HorizontalGroup("Advanced Settings/Displacement Random Seed")]
        [LabelText("Displacement Random Seed"), ShowIf("EnableProcessDisplacement"), Range(0, 255)]
        public int DisplacementRandomSeed = 123;

        [HorizontalGroup("Advanced Settings/Displacement Random Seed")]
        [Button("Randomize"), ShowIf("EnableProcessDisplacement")]
        private void GeneratePDisRandomSeed()
        {
            DisplacementRandomSeed = UnityEngine.Random.Range(0, 255);
        }

        #region IAnimParams Implementation
        int IAnimParams.GlobalSimulationMode  => (int)GlobalSimulationMode;
        Vector3 IAnimParams.BaseDirection => BaseDirection;
        int IAnimParams.DirectionSimulationMode => (int)DirectionSimulationMode;
        bool IAnimParams.EnableRandomDirection => EnableRandomDirection;
        float IAnimParams.RandomAngleRange => RandomAngleRange;
        int IAnimParams.SpeedMode => (int)SpeedMode;
        float IAnimParams.ConstantSpeed => ConstantSpeed;
        float IAnimParams.MinSpeed => MinSpeed;
        float IAnimParams.MaxSpeed => MaxSpeed;
        int IAnimParams.SpeedCurveSampleCount => SpeedCurveSampleCount;
        bool IAnimParams.EnableRandomLifeTime => EnableRandomLifeTime;
        float IAnimParams.LifeTime => LifeTime;
        float IAnimParams.MinLifeTime => MinLifeTime;
        float IAnimParams.MaxLifeTime => MaxLifeTime;
        int IAnimParams.StartTimeMode => (int)StartTimeMode;
        float IAnimParams.MaxStartTime => MaxStartTime;
        float IAnimParams.BaseMaxStartTime => BaseMaxStartTime;
        float IAnimParams.RandomStartTimeRange => RandomStartTimeRange;
        int IAnimParams.DirectionRandomSeed => DirectionRandomSeed;
        int IAnimParams.SpeedRandomSeed => SpeedRandomSeed;
        int IAnimParams.LifeTimeRandomSeed => LifeTimeRandomSeed;
        int IAnimParams.StartTimeRandomSeed => StartTimeRandomSeed;
        bool IAnimParams.EnableProcessDisplacement => EnableProcessDisplacement;
        float IAnimParams.MaxDisplacementAmplitude => MaxDisplacementAmplitude;
        float IAnimParams.MinDisplacementFrequency => MinDisplacementFrequency;
        float IAnimParams.MaxDisplacementFrequency => MaxDisplacementFrequency;
        int IAnimParams.DisplacementWaveCount => DisplacementWaveCount;
        int IAnimParams.DisplacementRandomSeed => DisplacementRandomSeed;
        #endregion
        public ComputeBuffer SpeedCurveBuffer { get; private set; }
        public IReadOnlyList<RenderTexture> GreyMapRTs { get; private set; }

        private void GenerateGreyMapTextureComputeBuffers()
        {
            if (GreyMapRTs != null && GreyMapRTs.Count > 0)
            {
                GreyMapRTs.ForEach(b => b.Release());
            }
            
            var bufferList = new List<RenderTexture>();
            
            for (var i = 0; i < MeshNames.Count; i++)
            {
                var greyMap = Texture2D.blackTexture;
                if (i >= GreyMapTextures.Count || GreyMapTextures[i] == null)
                {
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
            GreyMapRTs?.ForEach(b => b.Release());
        }
    }
    
    #endregion
}