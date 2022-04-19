using UnityEngine;

namespace Stephens.Heightmesh
{
    /// <summary>
    /// 
    /// </summary>
    internal class Heightmap
    {
        #region VARIABLES

        private readonly DataHeightmap _data;
        private readonly DataConfigHeightmap _configData;
        internal DataHeightmapModification Modification;

        #endregion VARIABLES


        #region CONSTRUCTION

        internal Heightmap(DataConfigHeightmap map)
        {
            _configData = map;
            _data = new DataHeightmap()
            {
                Source = map.Source,
                Pixels = GetPixels(map.Source),
                Height = map.Source.height,
                Width = map.Source.width
            };
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

        #endregion CONSTRUCTION
        

        #region READ

        internal float[] ReadHeight(DataHeightmesh target)
        {
            float[] heightPoints = new float[target.VerticesCount];
            float multFactor = (float)_data.Width / (float)target.Dimensions;
            
            // Clamp texture offset
            Vector2 offset = GetClampedOffset();
            
            // Cycle through mesh vertices
            for (int x = 0; x <= target.Dimensions; x++)
            {
                for (int z = 0; z <= target.Dimensions; z++)
                {
                    int texX = (int)((multFactor * x) + offset.x);
                    int texZ = (int)((multFactor * z) + offset.y);
                    
                    int currentPixel = (texX + (_data.Width * texZ));
                    if (currentPixel > _data.Pixels.Length - 1)
                    {
                        currentPixel -= _data.Pixels.Length;
                    }
                    else if (currentPixel < 0)
                    {
                        currentPixel += _data.Pixels.Length;
                    }
                    
                    int pixelAdjust = Mathf.Clamp((int)currentPixel, 0, _data.Pixels.Length - 1);
                    float y = _data.Pixels[pixelAdjust];
                    y *= _configData.Strength;
                    heightPoints[Index(x, z, target.Dimensions)] = y;
                }
            }

            return heightPoints;
        }
        
        private Vector2 GetClampedOffset()
        {
            Vector2 offset = _configData.Offset;
            
            while (offset.x > _data.Width)
            {
                offset = new Vector2(offset.x - _data.Width, offset.y);
            }
            while (offset.x < 0)
            {
                offset = new Vector2(offset.x + _data.Width, offset.y);
            }
            
            while (offset.y > _data.Height)
            {
                offset = new Vector2(offset.x, offset.y - _data.Height);
            }
            while (offset.y < 0)
            {
                offset = new Vector2(offset.x, offset.y + _data.Height);
            }
            
            return offset;
        }

        #endregion READ


        #region UTILITY

        private int Index(int x, int z, int dimensions)
        {
            return x * (dimensions + 1) + z;
        }

        #endregion UTILITY
    }
}