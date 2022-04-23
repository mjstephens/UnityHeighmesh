using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace Stephens.Heightmesh
{
    internal class Heightmap : HeightmeshInput<DataConfigHeightmap, DataHeightmap>
    {
        #region VARIABLES

        internal DataHeightmapModification Modification;

        private readonly float[] _pixels;

        #endregion VARIABLES


        #region CONSTRUCTION

        internal Heightmap(DataConfigHeightmap config) : base(config)
        {
            config.Pixels = new NativeArray<float>(GetPixels(config.Source).ToArray(), Allocator.Persistent);
        }

        #endregion CONSTRUCTION


        #region DATA

        protected override void UpdateData(float delta)
        {
            _data = new DataHeightmap()
            {
                Invert = _config.Invert,
                Height = _config.Source.height,
                Width = _config.Source.width,
                Offset = _config.Offset,
                Opacity = _config.Opacity,
                Strength = _config.Strength,
                ResolveMode = _config.ResolveMode
            };
        }

        #endregion DATA


        #region SOLVE
        
        internal static float CalcForVertexCPU(
            Vector3 vertex,
            Vector3 vertexCoords,
            NativeArray<float> pixels,
            Vector3 meshPosition,
            float meshWidth,
            DataHeightmap data)
        {
            float y = 0;
            float multFactor = data.Width / meshWidth;
            
            // Clamp texture offset
            Vector2 offset = GetClampedOffset(data);
            
            int texX = (int)((multFactor * vertexCoords.x) - (offset.x * data.Width)) + (data.Width / 2);
            int texZ = (int)((multFactor * vertexCoords.z) - (offset.y * data.Height)) + (data.Height / 2);
                    
            int currentPixel = (texX + (data.Width * texZ));
            if (currentPixel > pixels.Length - 1)
            {
                currentPixel -= pixels.Length;
            }
            else if (currentPixel < 0)
            {
                currentPixel += pixels.Length; 
            }
                    
            int pixelAdjust = Mathf.Clamp(currentPixel, 0, pixels.Length - 1);
            y = data.Invert ? 1 - pixels[pixelAdjust] : pixels[pixelAdjust];
            y *= data.Opacity;

            switch (data.ResolveMode)
            {
                case HeightmapResolveMode.Absolute: 
                    y *= data.Strength; 
                    break;
                case HeightmapResolveMode.Additive: 
                    y = vertex.y + (y * data.Strength); 
                    break;
                case HeightmapResolveMode.Subtractive:
                    y = vertex.y - ((vertex.y * 2) * y);
                    break;
            }

            return y;
        }
        
        private static Vector2 GetClampedOffset(DataHeightmap data)
        {
            Vector2 offset = data.Offset;
            
            while (offset.x > data.Width)
            {
                offset = new Vector2(offset.x - data.Width, offset.y);
            }
            while (offset.x < 0)
            {
                offset = new Vector2(offset.x + data.Width, offset.y);
            }
            
            while (offset.y > data.Height)
            {
                offset = new Vector2(offset.x, offset.y - data.Height);
            }
            while (offset.y < 0)
            {
                offset = new Vector2(offset.x, offset.y + data.Height);
            }
            
            return offset;
        }
        
        private static float[] GetPixels(Texture2D source)
        {
            Color32[] colors = source.GetPixels32();
            float[] pixels = new float[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                pixels[i] = 1 - (colors[i].a / 255f);
            }

            return pixels;
        }

        private static int Index(int x, int z, int dimensions)
        {
            return x * (dimensions + 1) + z;
        }

        #endregion SOLVE


        #region CLEANUP

        internal void Cleanup()
        {
            _config.Pixels.Dispose();
        }

        #endregion CLEANUP
    }
}