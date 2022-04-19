using UnityEngine;

namespace Stephens.Heightmesh
{
    [CreateAssetMenu(
        fileName = "DataConfig_WaveGerstner_",
        menuName = "Heightmesh/Gerstner Wave")]
    public class DataConfigWaveGerstner : DataConfigWave
    {
        [Header("Gerstner Settings")]
        [SerializeField] internal Vector3 Origin;
        [SerializeField] internal bool OmniDirectional;
    }
}