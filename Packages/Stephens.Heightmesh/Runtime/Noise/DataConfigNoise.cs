using UnityEngine;

namespace Stephens.Heightmesh
{
    [CreateAssetMenu(
        fileName = "DataConfig_Noise_",
        menuName = "Heightmesh/Noise")]
    public class DataConfigNoise : ScriptableObject
    {
        [SerializeField] internal NoiseType Type;
        [SerializeField] internal float Strength;
        [SerializeField] internal float Spread;
        [SerializeField] internal Vector2 Speed;
    }
}