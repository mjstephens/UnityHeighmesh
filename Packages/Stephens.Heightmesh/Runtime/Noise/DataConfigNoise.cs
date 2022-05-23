using UnityEngine;

namespace Stephens.Heightmesh
{
    [CreateAssetMenu(
        fileName = "DataConfig_Noise_",
        menuName = "Heightmesh/Noise")]
    public class DataConfigNoise : DataConfigHeightmeshInput
    {
        [SerializeField] public NoiseType Type;
        [SerializeField] public float Strength;
        [SerializeField] public float Spread;
        [SerializeField] public Vector2 Speed;
    }
}