using UnityEngine;

namespace Stephens.Buoyancy
{
    public interface IBuoyant
    {
        #region PROPERTIES

        Vector3 Position { get; }

        #endregion PROPERTIES


        #region METHODS

        /// <summary>
        /// Tells the surface object where it's position on top of the world-anchored waves would be 
        /// </summary>
        void ReceiveSurfacePosition(Vector3 position, Vector2 offset);

        #endregion METHODS
    }
}