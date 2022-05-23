using System;
using UnityEngine;

namespace Stephens.Buoyancy
{
    public class ExampleFloatingObject : MonoBehaviour
    {
        #region VARIABLES

        [Header("References")]
        [SerializeField] private FloatPoint[] _floatPoints;
        [SerializeField] private Transform _model;
        
        
        public float AirDrag = 1;
        public float WaterDrag = 10;
        public bool AffectDirection = true;
        [SerializeField] private float _upForce = 0.9f;
        [SerializeField] private float _rotSmoothTime = 0.5f;
        

        private Vector3[] _floatPositions;
        private Rigidbody _rb;
        private Vector3 _smoothVectorRotation;

        #endregion VARIABLES


        #region INITIALIZATION

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _floatPositions = new Vector3[_floatPoints.Length];
        }

        #endregion INITIALIZATION


        #region TICK

        private void FixedUpdate()
        {
            // Get normal of surface
            for (int i = 0; i < _floatPositions.Length; i++)
            {
                _floatPositions[i] = _floatPoints[i].SentinelPos;
            }
            Vector3 normal = GetNormal(_floatPositions);

            float submergedLevel = 0;
            bool pointUnderWater = false;
            foreach (FloatPoint p in _floatPoints)
            {
                if (p.Submerged)
                {
                    submergedLevel += p.SubmergedDistance / _floatPoints.Length;
                    pointUnderWater = true;
                }
            }

            var gravity = Physics.gravity;
            _rb.drag = AirDrag;

            if (submergedLevel > 0)
            {
                _rb.drag = WaterDrag;
                gravity = AffectDirection ? normal * -Physics.gravity.y : -Physics.gravity;
                transform.Translate(Vector3.up * submergedLevel * _upForce);
                _rb.AddForce(gravity * Mathf.Clamp(Mathf.Abs(submergedLevel),0,1));
            }
            
            
            if (pointUnderWater)
            {
                //attach to water surface
                normal = Vector3.SmoothDamp(transform.up, normal, ref _smoothVectorRotation, _rotSmoothTime);
                _rb.rotation = Quaternion.FromToRotation(transform.up, normal) * _rb.rotation;
            }
        }

        #endregion TICK
        
        
        
        
        
        private static Vector3 GetNormal(Vector3[] points)
        {
            //https://www.ilikebigbits.com/2015_03_04_plane_from_points.html
            if (points.Length < 3)
                return Vector3.up;

            Vector3 center = GetCenter(points);

            float xx = 0f, xy = 0f, xz = 0f, yy = 0f, yz = 0f, zz = 0f;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 r = points[i] - center;
                xx += r.x * r.x;
                xy += r.x * r.y;
                xz += r.x * r.z;
                yy += r.y * r.y;
                yz += r.y * r.z;
                zz += r.z * r.z;
            }

            float det_x = yy * zz - yz * yz;
            float det_y = xx * zz - xz * xz;
            float det_z = xx * yy - xy * xy;

            if (det_x > det_y && det_x > det_z)
            {
                return new Vector3(det_x, xz * yz - xy * zz, xy * yz - xz * yy).normalized;
            }

            return det_y > det_z ? 
                new Vector3(xz * yz - xy * zz, det_y, xy * xz - yz * xx).normalized : 
                new Vector3(xy * yz - xz * yy, xy * xz - yz * xx, det_z).normalized;
        }

        private static Vector3 GetCenter(Vector3[] points)
        {
            Vector3 center = Vector3.zero;
            for (int i = 0; i < points.Length; i++)
            {
                center += points[i] / points.Length;
            }
            return center;
        }
    }
}