using System;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Stephens.Heightmesh
{
    public class SystemHeightmeshSingle : SystemHeightmesh
    {
        #region VARIABLES

        private Heightmesh _heightmesh;

        #endregion VARIABLES
        
        
        #region INITIALIZATION

        protected override void SpawnHeightmeshObjects()
        {
            Heightmesh mesh = Instantiate(_prefabHeightmeshObject).GetComponent<Heightmesh>();
            _heightmesh = mesh;
            _heightmesh.Setup(_heightmeshData);
        }

        #endregion INITIALIZATION


        #region TICK

        protected override void Update()
        {
            base.Update();
            
            TickHeightmesh(new []{_heightmesh});
        }

        #endregion TICK
    }
}