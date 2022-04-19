using UnityEngine;

namespace Stephens.Heightmesh
{
    /// <summary>
    /// Defines the data for a heightmap, to be used with a height mesh
    /// </summary>
    internal struct DataHeightmap
    {
        internal Texture2D Source;
        internal float[] Pixels;
        internal int Width;
        internal int Height;
    }
}