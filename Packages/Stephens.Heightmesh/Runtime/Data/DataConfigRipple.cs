using UnityEngine;

namespace Stephens.Heightmesh
{
    [CreateAssetMenu(
        fileName = "DataConfig_Ripple_",
        menuName = "Heightmesh/Ripple")]
    public class DataConfigRipple : ScriptableObject
    {
        [SerializeField] internal float Strength;
        [SerializeField] internal float Speed;
        [SerializeField] internal float Distance;
        [SerializeField] internal float Lifetime;
    }
}