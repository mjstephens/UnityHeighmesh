using UnityEngine;

namespace Stephens.Buoyancy
{
    public class FloatPoint : MonoBehaviour
    {
        #region VARIABLES

        [Header("References")]
        [SerializeField] private GameObject _sentinelPrefab;

        internal bool Submerged => _sentinel.Submerged;
        internal Vector3 SentinelPos => _sentinel.transform.position;
        internal float SubmergedDistance => _sentinel.transform.position.y - transform.position.y;
        
        private BuoyancySentinel _sentinel;

        #endregion VARIABLES


        #region INITIALIZATION

        private void Awake()
        {
            // Create sentinel to monitor buoyancy
            _sentinel = Instantiate(_sentinelPrefab, transform).GetComponent<BuoyancySentinel>();
            _sentinel.RegisterTarget(transform);
        }

        #endregion INITIALIZATION
    }
}