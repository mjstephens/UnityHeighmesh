using System.Collections.Generic;
using UnityEngine;

namespace Stephens.Heightmesh
{
    /// <summary>
    /// Base class for defining an input that can affect a heightmesh
    /// </summary>
    internal abstract class HeightmeshInput<TDataConfig, TData> : IHeightmeshInput
        where TDataConfig : DataConfigHeightmeshInput
        where TData : struct
    {
        #region VARIABLES

        internal TData Data => _data;
        
        protected TData _data;
        protected readonly TDataConfig _config;

        #endregion VARIABLES
        
        
        #region CONSTRUCTOR

        internal HeightmeshInput(TDataConfig config)
        {
            _config = config;
        }

        #endregion CONSTRUCTOR


        #region TICK


        void IHeightmeshInput.Tick(float delta)
        {
            // Update the data for this entity
            UpdateData(delta);
        }
        
        /// <summary>
        /// Updates the active input data from the data config
        /// </summary>
        /// <returns></returns>
        protected abstract void UpdateData(float delta);

        #endregion TICK


        #region UTILITY

        protected float GetClampedOffset(float delta, float speed, float previousOffset)
        {
            float offset = previousOffset + (delta * speed);
            return offset;
        }
        
        protected Vector2 GetClampedOffset(float delta, Vector2 speed, Vector2 previousOffset)
        {
            Vector2 offset = previousOffset + (delta * speed);

            return offset;
        }

        #endregion UTILITY
    }
}