using UnityEngine;

namespace Stephens.Heightmesh
{
    [CreateAssetMenu(
        fileName = "DataConfig_WaveGerstner_",
        menuName = "Heightmesh/Gerstner Wave")]
    public class DataConfigWaveGerstner : ScriptableObject
    {
        [SerializeField] internal float Amplitude;
        [SerializeField] internal float Direction;
        [SerializeField] internal float Speed;
        [SerializeField] internal float Wavelength;
        [SerializeField] internal Vector3 Origin;
        [SerializeField] internal bool OmniDirectional;
        [SerializeField] internal bool WorldAnchored;
    }
}