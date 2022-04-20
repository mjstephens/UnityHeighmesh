using UnityEngine;

namespace Stephens.Heightmesh
{
    public class DataConfigWave : ScriptableObject
    {
        [Header("Wave Settings")]
        [SerializeField] [Range(0, 1)] internal float Opacity;
        [SerializeField] internal float Amplitude;
        [SerializeField] internal float Wavelength;
        [SerializeField] internal float Direction;
        [SerializeField] internal float Speed;
        [SerializeField] internal bool WorldAnchored;
    }
}