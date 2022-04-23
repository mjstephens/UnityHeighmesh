using UnityEngine;

namespace Stephens.Heightmesh
{
    [CreateAssetMenu(
        fileName = "DataConfig_WaveGerstner_",
        menuName = "Heightmesh/Gerstner Wave")]
    public class DataConfigWaveGerstner : DataConfigWave
    {
        [Header("Gerstner Settings")] 
        [SerializeField] [Range(0, 1)] internal float PeakSharpness = 0.8f;
        [SerializeField] internal Vector3 Origin;
        [SerializeField] internal bool OmniDirectional;
    }
}