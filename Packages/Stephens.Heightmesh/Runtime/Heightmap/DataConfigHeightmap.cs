using System;
using Unity.Collections;
using UnityEngine;

namespace Stephens.Heightmesh
{
    [CreateAssetMenu(
        fileName = "DataConfig_Heightmap_",
        menuName = "Heightmesh/Heightmap")]
    public class DataConfigHeightmap : DataConfigHeightmeshInput
    {
        [SerializeField] internal Texture2D Source;
        [SerializeField] internal HeightmapResolveMode ResolveMode;
        [SerializeField] internal bool Invert;
        [SerializeField] internal Vector2 Speed;
        [SerializeField] internal Vector2 Offset;
        [SerializeField] internal float Strength = 1;
        [SerializeField] [Range(0, 1)] internal float Opacity = 1;

        // Runtime
        internal NativeArray<float> Pixels { get; set; }
    }
}