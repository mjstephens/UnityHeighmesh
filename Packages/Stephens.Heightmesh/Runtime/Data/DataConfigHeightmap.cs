using UnityEngine;

namespace Stephens.Heightmesh
{
    [CreateAssetMenu(
        fileName = "DataConfig_Heightmap_",
        menuName = "Heightmesh/Heightmap")]
    public class DataConfigHeightmap : ScriptableObject
    {
        [SerializeField] internal Texture2D Source;
        [SerializeField] internal Vector2 Offset;
        [SerializeField] internal float Strength;
    }
}