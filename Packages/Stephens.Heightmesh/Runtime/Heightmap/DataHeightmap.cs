using Unity.Collections;
using UnityEngine;

namespace Stephens.Heightmesh
{
    public struct DataHeightmap : IHeightmeshInputData
    {
        public bool Valid { get; set; }

        internal bool Invert;
        internal int Width;
        internal int Height;
        internal Vector2 Offset;
        internal float Opacity;
        internal float Strength;
        internal HeightmapResolveMode ResolveMode;
    }
}