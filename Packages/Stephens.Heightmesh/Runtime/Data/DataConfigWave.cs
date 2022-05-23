using UnityEngine;

namespace Stephens.Heightmesh
{
    public class DataConfigWave : DataConfigHeightmeshInput
    {
        [Header("Wave Settings")]
        [SerializeField] [Range(0, 1)] public float Opacity;
        [SerializeField] public float Amplitude;
        [SerializeField] public float Wavelength;
        [SerializeField] public float Direction;
        [SerializeField] public float Speed;
        [SerializeField] internal bool WorldAnchored;
    }
}