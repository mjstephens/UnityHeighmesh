using Unity.Collections;
using UnityEngine;

namespace Stephens.Heightmesh
{
    internal struct DataHeightmap : IHeightmeshInputData
    {
        internal bool Invert;
        internal int Width;
        internal int Height;
        internal Vector2 Offset;
        internal float Opacity;
        internal float Strength;
        internal HeightmapResolveMode ResolveMode;
    }
}