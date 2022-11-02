using System;
using System.Linq;
using UnityEngine;

namespace Stephens.Heightmesh
{
    /// <summary>
    /// 3x3 cluster of adjacent heightmesh objects
    /// </summary>
    public class SystemHeightmeshGrid : SystemHeightmesh
    {
        #region VARIABLES

        [Header("Options")]
        [SerializeField] private bool _efficientTick;
        
        private Transform _parent;
        private readonly Heightmesh[] _heightmeshes = new Heightmesh[9];
        private bool _flag;

        #endregion VARIABLES
        
        
        #region INITIALIZATION

        protected override void SpawnHeightmeshObjects()
        {
            _parent = new GameObject("HeightmeshGrid").transform;
            
            // Spawn grid
            for (int i = 0; i < _heightmeshes.Length; i++)
            {
                Heightmesh mesh = Instantiate(_prefabHeightmeshObject, _parent).GetComponent<Heightmesh>();
                _heightmeshes[i] = mesh;
                mesh.transform.name = "HeightMesh[" + i + "]";
                mesh.Setup(_heightmeshData);
                
                // 6 7 8
                // 3 4 5
                // 0 1 2
                
            }

            RepositionHeightmeshes();
        }

        private void RepositionHeightmeshes()
        {
            // Align positions of grids
            float size = _heightmeshData.Size;
            _heightmeshes[0].transform.localPosition = new Vector3(-size, 0, -size);
            _heightmeshes[1].transform.localPosition = new Vector3(0, 0, -size);
            _heightmeshes[2].transform.localPosition = new Vector3(size, 0, -size);
            _heightmeshes[3].transform.localPosition = new Vector3(-size, 0, 0);
            _heightmeshes[5].transform.localPosition = new Vector3(size, 0, 0);
            _heightmeshes[6].transform.localPosition = new Vector3(-size, 0, size);
            _heightmeshes[7].transform.localPosition = new Vector3(0, 0, size);
            _heightmeshes[8].transform.localPosition = new Vector3(size, 0, size);
        }

        private void OnEnable()
        {
            _heightmeshData.OnValidated += RepositionHeightmeshes;
        }

        private void OnDisable()
        {
            _heightmeshData.OnValidated -= RepositionHeightmeshes;
        }

        #endregion INITIALIZATION
        
        
        #region TICK

        protected override void Update()
        {
            base.Update();

            if (_efficientTick)
            {
                _flag = !_flag;
                if (_flag)
                {
                    TickHeightmesh(new []
                    {
                        _heightmeshes[0],
                        _heightmeshes[1],
                        _heightmeshes[2],                    
                        _heightmeshes[3],
                        _heightmeshes[4]
                    });
                }
                else
                {
                    TickHeightmesh(new []
                    {
                        _heightmeshes[4],
                        _heightmeshes[5],
                        _heightmeshes[6],                    
                        _heightmeshes[7],
                        _heightmeshes[8]
                    });
                }
            }
            else
            {
                TickHeightmesh(_heightmeshes.ToArray());
            }
        }

        #endregion TICK
    }
}