using UnityEngine;

namespace Stephens.Heightmesh
{
    public struct DataNoise : IHeightmeshInputData
    {
        internal NoiseType Type;
        internal float Strength;
        internal float Spread;

        internal Vector2 Offset;
    }
}