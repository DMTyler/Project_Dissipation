using System.Collections.Generic;
using UnityEngine;

namespace DGraphics.Dissipation
{
    public interface IAnimParams
    {
        int GlobalSimulationMode { get; }
        Vector3 BaseDirection { get; }
        int DirectionSimulationMode { get; }
        bool EnableRandomDirection { get; }
        float RandomAngleRange { get; }
        int SpeedMode { get; }
        float ConstantSpeed { get; }
        float MinSpeed { get; }
        float MaxSpeed { get; }
        ComputeBuffer SpeedCurveBuffer { get; }
        int SpeedCurveSampleCount { get; }
        bool EnableRandomLifeTime { get; }
        float LifeTime { get; }
        float MinLifeTime { get; }
        float MaxLifeTime { get; }
        int StartTimeMode { get; }
        float MaxStartTime { get; }
        float BaseMaxStartTime { get; }
        float RandomStartTimeRange { get; }
        int DirectionRandomSeed { get; }
        int SpeedRandomSeed { get; }
        int LifeTimeRandomSeed { get; }
        int StartTimeRandomSeed { get; }
        bool EnableProcessDisplacement { get; }
        float MaxDisplacementAmplitude { get; }
        float MinDisplacementFrequency { get; }
        float MaxDisplacementFrequency { get; }
        int DisplacementWaveCount { get; }
        int DisplacementRandomSeed { get; }
        IReadOnlyList<RenderTexture> GreyMapRTs { get; }
    }
}