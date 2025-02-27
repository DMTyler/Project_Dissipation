using System;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DGraphics.Dissipation
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MeshDissipationSetter))]
    public class MeshDissipationSetterEditor : Editor
    {
        private int _selectedLanguage = 0;
        private bool _advancedFoldout;
        private readonly string[] _languageOptions = {"中文", "English"};

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var targetScript = (MeshDissipationSetter)target;
            
            EditorGUILayout.LabelField(_selectedLanguage == 0? "语言" : "Language", EditorStyles.boldLabel);
            _selectedLanguage = GUILayout.Toolbar(_selectedLanguage, _languageOptions);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField(_selectedLanguage == 0? "动画参数" : "Anim Parameters", EditorStyles.boldLabel);
            DrawAnimParams(serializedObject.FindProperty("AnimParams"), targetScript);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(_selectedLanguage == 0? "其他参数" : "Other Parameters", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(true); // Read only
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_meshTransformPair"), 
                _selectedLanguage == 0 ? new GUIContent("网格-变换对") : new GUIContent("Mesh-Transform Pair"), true);
            EditorGUI.EndDisabledGroup();
            
            // Vertical Buttons
            EditorGUILayout.Space(10);
            if (GUILayout.Button(_selectedLanguage == 0? "一键选择所有 Mesh" : "Select All Meshes"))
            {
                targetScript.SelectAllMeshes();
            }

            if (GUILayout.Button(_selectedLanguage == 0? "初始化" : "Initialize"))
            {
                targetScript.Setup();
            }

            if (GUILayout.Button(_selectedLanguage == 0? "重置" : "Reset"))
            {
                targetScript.Reset();
            }
            
            // Horizontal Buttons
            if (targetScript.Initialized)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();

                GUI.enabled = targetScript.Initialized && !targetScript.IsStarted;
                if (GUILayout.Button(_selectedLanguage == 0 ? "开始" : "Start"))
                {
                    targetScript.StartAnim();
                }

                GUI.enabled = targetScript.Initialized && targetScript.IsStarted && !targetScript.IsPaused;
                if (GUILayout.Button(_selectedLanguage == 0 ? "暂停" : "Pause"))
                {
                    targetScript.PauseAnim();
                }

                GUI.enabled = targetScript.Initialized && targetScript.IsStarted && targetScript.IsPaused;
                if (GUILayout.Button(_selectedLanguage == 0 ? "继续" : "Continue"))
                {
                    targetScript.ContinueAnim();
                }

                GUI.enabled = targetScript.Initialized && targetScript.IsStarted;
                if (GUILayout.Button(_selectedLanguage == 0 ? "停止" : "Stop"))
                {
                    targetScript.StopAnim();
                }

                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAnimParams(SerializedProperty animParamsProperty, MeshDissipationSetter targetScript)
    {
        EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("GlobalSimulationMode"), 
            _selectedLanguage == 0 ? new GUIContent("全局模拟模式") : new GUIContent("Global Simulation Mode"));

        EditorGUILayout.Space(8);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("BaseDirection"),
            _selectedLanguage == 0 ? new GUIContent("基础方向") : new GUIContent("Base Direction"));
        
        if (EditorGUI.EndChangeCheck())
        {
            targetScript.AnimParams.Init();
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("DirectionSimulationMode"),
            _selectedLanguage == 0 ? new GUIContent("方向模拟模式") : new GUIContent("Direction Simulation Mode"));
        if (EditorGUI.EndChangeCheck())
        {
            targetScript.AnimParams.Init();
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("EnableRandomDirection"),
            _selectedLanguage == 0 ? new GUIContent("随机方向") : new GUIContent("Random Direction"));
        if (EditorGUI.EndChangeCheck())
        {
            targetScript.AnimParams.Init();
        }

        if (animParamsProperty.FindPropertyRelative("EnableRandomDirection").boolValue)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(animParamsProperty.FindPropertyRelative("RandomAngleRange"), 0f, 180f, 
                _selectedLanguage == 0 ? "最大随机角度" : "Max Random Angle");
            if (EditorGUI.EndChangeCheck())
            {
                targetScript.AnimParams.Init();
            }
        }

        // 速度模式
        EditorGUILayout.Space(8);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("SpeedMode"),
            _selectedLanguage == 0 ? new GUIContent("速度模式") : new GUIContent("Speed Mode"));
        if (EditorGUI.EndChangeCheck())
        {
            targetScript.AnimParams.Init();
        }

        var speedMode = (MeshDissipationAnimParams.AnimSpeedMode)animParamsProperty.FindPropertyRelative("SpeedMode").enumValueIndex + 1;
        if (speedMode == MeshDissipationAnimParams.AnimSpeedMode.Constant)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("ConstantSpeed"),
                _selectedLanguage == 0 ? new GUIContent("常数速度") : new GUIContent("Constant Speed"));
            if (EditorGUI.EndChangeCheck())
            {
                targetScript.AnimParams.Init();
            }
        }
        else if (speedMode == MeshDissipationAnimParams.AnimSpeedMode.RandomBetweenTwoConstants)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("MinSpeed"),
                _selectedLanguage == 0 ? new GUIContent("最小速度") : new GUIContent("Min Speed"));
            EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("MaxSpeed"), 
                _selectedLanguage == 0 ? new GUIContent("最大速度") : new GUIContent("Max Speed"));
            if (EditorGUI.EndChangeCheck())
            {
                targetScript.AnimParams.Init();
            }
        }
        else if (speedMode == MeshDissipationAnimParams.AnimSpeedMode.Curve)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("SpeedCurve"), 
                _selectedLanguage == 0 ? new GUIContent("速度曲线") : new GUIContent("Speed Curve"));
            if (EditorGUI.EndChangeCheck())
            {
                targetScript.AnimParams.Init();
            }
        }

        // 生命周期
        EditorGUILayout.Space(8);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("EnableRandomLifeTime"),
            _selectedLanguage == 0 ? new GUIContent("随机生命周期") : new GUIContent("Random Life Time"));
        if (EditorGUI.EndChangeCheck())
        {
            targetScript.AnimParams.Init();
        }

        if (!animParamsProperty.FindPropertyRelative("EnableRandomLifeTime").boolValue)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("LifeTime"),
                _selectedLanguage == 0 ? new GUIContent("生命周期") : new GUIContent("Life Time"));
            if (EditorGUI.EndChangeCheck())
            {
                targetScript.AnimParams.Init();
            }
        }
        else
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("MinLifeTime"), 
                _selectedLanguage == 0 ? new GUIContent("最小生命周期") : new GUIContent("Min Lifetime"));
            EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("MaxLifeTime"), 
                _selectedLanguage == 0 ? new GUIContent("最大生命周期") : new GUIContent("Max Lifetime"));
            if (EditorGUI.EndChangeCheck())
            {
                targetScript.AnimParams.Init();
            }
        }

        // 起始时间逻辑
        EditorGUILayout.Space(8);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("StartTimeMode"),
            _selectedLanguage == 0 ? new GUIContent("起始时间模式") : new GUIContent("Start Time Mode"));
        if (EditorGUI.EndChangeCheck())
        {
            targetScript.AnimParams.Init();
        }

        var startTimeMode = (MeshDissipationAnimParams.AnimStartTimeMode)animParamsProperty.FindPropertyRelative("StartTimeMode").enumValueIndex + 1;
        if (startTimeMode == MeshDissipationAnimParams.AnimStartTimeMode.RandomUnderConstant)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("MaxStartTime"),
                _selectedLanguage == 0 ? new GUIContent("最大起始时间") : new GUIContent("Max Start Time"));
            if (EditorGUI.EndChangeCheck())
            {
                targetScript.AnimParams.Init();
            }
        }
        else if (startTimeMode == MeshDissipationAnimParams.AnimStartTimeMode.RandomBasedOnGreyMap)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("GreyMapTextures"),
                _selectedLanguage == 0 ? new GUIContent ("灰度图") : new GUIContent("Grey Map"),true);
            EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("BaseMaxStartTime"),
                _selectedLanguage == 0 ? new GUIContent("基础最大起始时间") : new GUIContent("Base Max Start Time"));
            EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("RandomStartTimeRange"),
                _selectedLanguage == 0 ? new GUIContent("随机起始时间范围") : new GUIContent("Random Start Time Range"));
            if (EditorGUI.EndChangeCheck())
            {
                targetScript.AnimParams.Init();
            }
        }

        // 过程扰动
        EditorGUILayout.Space(8);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("EnableProcessDisplacement"),
            _selectedLanguage == 0 ? new GUIContent("过程扰动") : new GUIContent("Process Displacement"));
        if (EditorGUI.EndChangeCheck())
        {
            targetScript.AnimParams.Init();
        }

        if (animParamsProperty.FindPropertyRelative("EnableProcessDisplacement").boolValue)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("MaxDisplacementAmplitude"),
                _selectedLanguage == 0 ? new GUIContent("最大扰动幅度") : new GUIContent("Max Displacement Amplitude"));
            if (EditorGUI.EndChangeCheck())
            {
                targetScript.AnimParams.Init();
            }
        }
        
        EditorGUILayout.Space(8);
        _advancedFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_advancedFoldout, _selectedLanguage == 0 ? "高级参数" : "Advanced Parameters");
        if (_advancedFoldout)
        {
            var meshNamesProp = animParamsProperty.FindPropertyRelative("MeshNames");
            meshNamesProp.isExpanded = EditorGUILayout.Foldout(meshNamesProp.isExpanded, _selectedLanguage == 0 ? "Mesh 名称" : "Mesh Names");
            if (meshNamesProp.isExpanded)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.indentLevel++;
                for (var i = 0; i < meshNamesProp.arraySize; i++)
                {
                    EditorGUILayout.PropertyField(meshNamesProp.GetArrayElementAtIndex(i), new GUIContent($"Element {i}"));
                }
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.IntSlider(animParamsProperty.FindPropertyRelative("CurveSampleCountPerSecond"), 1, 240, 
                _selectedLanguage == 0 ? "每秒曲线采样次数" : "Curve Sample Count / Sec");
            if (EditorGUI.EndChangeCheck())
            {
                targetScript.AnimParams.Init();
            }

            if (speedMode == MeshDissipationAnimParams.AnimSpeedMode.Curve)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("SpeedCurveSampleCount"),
                    _selectedLanguage == 0 ? new GUIContent("速度曲线采样次数") : new GUIContent("Speed Curve Sample Count"));
                EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("SpeedCurveSums"), 
                    _selectedLanguage == 0 ? new GUIContent("速度曲线累加值") : new GUIContent("Speed Curve Sums"),true);
                EditorGUI.EndDisabledGroup();
            }

            if (startTimeMode == MeshDissipationAnimParams.AnimStartTimeMode.RandomBasedOnGreyMap)
            {
                var greyMapResolutionProp = animParamsProperty.FindPropertyRelative("GreyMapResolution");
                int[] options = { 128, 256, 512, 1024 };
                var currentIndex = System.Array.IndexOf(options, greyMapResolutionProp.intValue);
                EditorGUI.BeginChangeCheck();
                currentIndex = EditorGUILayout.Popup(_selectedLanguage == 0 ? "灰度图大小" : "Grey Map Resolution", 
                    currentIndex, Array.ConvertAll(options, o => o.ToString()));
                
                if (EditorGUI.EndChangeCheck())
                {
                    greyMapResolutionProp.intValue = options[currentIndex];
                    targetScript.AnimParams.Init();
                }
            }

            if (animParamsProperty.FindPropertyRelative("EnableProcessDisplacement").boolValue)
            {
                EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("DisplacementWaveCount"),
                    _selectedLanguage == 0 ? new GUIContent("过程扰动波数") : new GUIContent("Displacement Wave Count"));
                EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("MinDisplacementFrequency"), 
                    _selectedLanguage == 0 ? new GUIContent("最小过程扰动频率") : new GUIContent("Min Displacement Frequency"));
                EditorGUILayout.PropertyField(animParamsProperty.FindPropertyRelative("MaxDisplacementFrequency"), 
                    _selectedLanguage == 0 ? new GUIContent("最大过程扰动频率") : new GUIContent("Max Displacement Frequency"));
            }

            DrawRandomSeedField(animParamsProperty.FindPropertyRelative("DirectionRandomSeed"), 
                _selectedLanguage == 0 ? "方向随机种子" : "Direction Random Seed",
                animParamsProperty.FindPropertyRelative("EnableRandomDirection").boolValue,
                () => targetScript.AnimParams.DirectionRandomSeed = Random.Range(0, 255));

            DrawRandomSeedField(animParamsProperty.FindPropertyRelative("SpeedRandomSeed"), 
                _selectedLanguage == 0 ? "速度随机种子" : "Speed Random Seed",
                speedMode == MeshDissipationAnimParams.AnimSpeedMode.RandomBetweenTwoConstants,
                () => targetScript.AnimParams.SpeedRandomSeed = Random.Range(0, 255));

            DrawRandomSeedField(animParamsProperty.FindPropertyRelative("LifeTimeRandomSeed"), 
                _selectedLanguage == 0 ? "生命周期随机种子" : "Life Time Random Seed",
                animParamsProperty.FindPropertyRelative("EnableRandomLifeTime").boolValue,
                () => targetScript.AnimParams.LifeTimeRandomSeed = Random.Range(0, 255));

            DrawRandomSeedField(animParamsProperty.FindPropertyRelative("StartTimeRandomSeed"), 
                _selectedLanguage == 0 ? "起始时间随机种子" : "Start Time Random Seed",
                true, () => targetScript.AnimParams.StartTimeRandomSeed = Random.Range(0, 255));

            DrawRandomSeedField(animParamsProperty.FindPropertyRelative("DisplacementRandomSeed"), 
                _selectedLanguage == 0 ? "过程扰动随机种子" : "Displacement Random Seed",
                animParamsProperty.FindPropertyRelative("EnableProcessDisplacement").boolValue,
                () => targetScript.AnimParams.DisplacementRandomSeed = Random.Range(0, 255));
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

        private void DrawRandomSeedField(SerializedProperty property, string label, bool show, Action onRandomButtonClicked)
        {
            if (!show) return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(property, new GUIContent(label));
            if (GUILayout.Button(_selectedLanguage == 0 ? "随机" : "Randomize", GUILayout.Width(60)))
            {
                onRandomButtonClicked();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}