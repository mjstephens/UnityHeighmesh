using System;
using UnityEngine;

namespace Stephens.Heightmesh
{
    [CreateAssetMenu(
        fileName = "DataConfig_Heightmesh_",
        menuName = "Heightmesh/Heightmesh")]
    public class DataConfigHeightmesh : ScriptableObject
    {
        #region DATA

        [Header("Quality")]
        [SerializeField] internal float UpdateRateRipples = 0.1f;

        [Header("Surface")]
        [SerializeField] internal float Size = 200;
        [SerializeField] [Range(0.01f, 1)] internal float Resolution = 0.5f;
        [SerializeField] internal float UVScale = 1f;
        
        #endregion DATA


        #region VALIDATION

        internal Action OnValidated;
        private void OnValidate()
        {
            Size = Mathf.Max(Size, 1);
            OnValidated?.Invoke();
        }

        #endregion VALIDATION
    }
}