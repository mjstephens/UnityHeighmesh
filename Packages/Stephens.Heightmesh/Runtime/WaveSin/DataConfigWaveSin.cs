using UnityEngine;

namespace Stephens.Heightmesh
{
    [CreateAssetMenu(
        fileName = "DataConfig_WaveSin_",
        menuName = "Heightmesh/Sin Wave")]
    public class DataConfigWaveSin : ScriptableObject
    {
        [SerializeField] internal float Direction;
        [SerializeField] internal float Speed;
        [SerializeField] internal float Amplitude;
        [SerializeField] internal float Strength;
        [SerializeField] internal bool WorldAnchored;
    }
}