using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Stephens.Heightmesh
{
    /// <summary>
    /// Defines a mesh whose vertices can adapt to match a grid of heights
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class Heightmesh : MonoBehaviour
    {
        #region VARIABLES

        [Header("Data")]
        [SerializeField] private DataConfigHeightmesh _configData;
        [SerializeField] private DataConfigRipple _dataConfigRipples;

        [Header("Ripple Waves")]
        [SerializeField] private Transform[] _waveSources;
        
        internal DataConfigHeightmesh DataConfig => _configData;
        internal Mesh Mesh { get; private set; }

        private Solver _solver;
        private DataHeightmesh _data;
        private Vector3[] _originalVertices;
        private MeshFilter _meshFilter;
        private static float _time;
        private readonly List<DataHeightwaveRipple> _dataRipples = new List<DataHeightwaveRipple>();

        #endregion VARIABLES
        
        
        #region INITIALIZATION

        private void Awake()
        {
	        Mesh = CreateMesh();
	        if (_solver == null)
	        {
		        _solver = ResolveSolver();
	        }
        }
        
        private Solver ResolveSolver()
        {
	        switch (_configData.Mode)
	        {
		        case HeightmeshUpdateMode.CPU_Naive: 
			        return new Solver_CPUNaive();
			        break;
		        case HeightmeshUpdateMode.CPU_Job:
		        case HeightmeshUpdateMode.CPU_JobBurst:
		        case HeightmeshUpdateMode.CPU_JobBurstThreaded:
			        return new Solver_CPUJob();
			        break;
		        case HeightmeshUpdateMode.GPU_Compute:

			        break;
	        }
	        
	        // Fallback
	        return new Solver_CPUNaive();
        }

        private void OnEnable()
        {
	        SystemHeightmesh.Instance.RegisterHeightmesh(this);
        }

        private void OnDisable()
        {
	        SystemHeightmesh.Instance.UnregisterHeightmesh(this);
        }

        #endregion INITIALIZATION

        
        #region SOLVE

        internal void Solve(List<IHeightmeshInput> inputs, List<DataConfigHeightmeshInput> configs, float time)
        {
	        _solver?.Solve(this, _originalVertices, inputs, configs, time);
        }

        #endregion SOLVE


        #region WAVE SOURCES

        private IEnumerator CR_RippleGen()
        {
	        while (true)
	        {
		        foreach (Transform source in _waveSources)
		        {
			        _dataRipples.Add(new DataHeightwaveRipple()
			        {
				        Position = GetXZPosition(transform.InverseTransformPoint(source.position)),
				        Speed = _dataConfigRipples.Speed,
				        Strength = _dataConfigRipples.Strength,
				        Distance = _dataConfigRipples.Distance,
				        LifetimeRemaining = _dataConfigRipples.Lifetime
			        });
		        }
		        
		        yield return new WaitForSeconds(_configData.UpdateRateRipples);
	        }
        }

        void UpdateWaveSourcePositions(float delta)
        {
	        for (int i = 0; i < _dataRipples.Count; i++)
	        {
		        // Weaken ripple
		        float newLife = _dataRipples[i].LifetimeRemaining - delta;
		        if (newLife <= 0)
		        {
			        _dataRipples.RemoveAt(i);
		        }
		        else
		        {
			        _dataRipples[i] = new DataHeightwaveRipple()
			        {
				        Position = _dataRipples[i].Position,
				        Strength = _dataRipples[i].Strength,
				        Distance = _dataRipples[i].Distance,
				        Speed = _dataRipples[i].Speed,
				        LifetimeRemaining = newLife
			        };
		        }
	        }
        }

        private Vector2 GetXZPosition(Vector3 source)
        {
	        return new Vector2(source.x, source.z);
        }

        #endregion WAVE SOURCES

        
        #region MESH

        private Mesh CreateMesh()
        {
	        Mesh = new Mesh
	        {
		        // Use 32 bit index buffer to allow water grids larger than ~250x250
		        indexFormat = IndexFormat.UInt32
	        };

	        // Create initial grid of vertex positions
	        _originalVertices = new Vector3[_configData.SurfaceWidthPoints * _configData.SurfaceLengthPoints];
	        
	        int index = 0;
	        for (int i = 0; i < _configData.SurfaceWidthPoints; i++)
	        {
		        for (int j = 0; j < _configData.SurfaceLengthPoints; j++)
		        {
			        float x = MapValue(
				        i, 
				        0.0f, 
				        _configData.SurfaceWidthPoints - 1, 
				        -_configData.SurfaceActualWidth / 2.0f,
				        _configData.SurfaceActualWidth / 2.0f);
			        float z = MapValue(
				        j, 
				        0.0f, 
				        _configData.SurfaceLengthPoints - 1, 
				        -_configData.SurfaceActualLength / 2.0f, 
				        _configData.SurfaceActualLength / 2.0f);

			        Vector3 pos = new Vector3(x, 0f, z);
			        _originalVertices[index++] = pos;
		        }
	        }

	        // Create an index buffer for the grid
	        int[] indices = new int[(_configData.SurfaceWidthPoints - 1) * (_configData.SurfaceLengthPoints - 1) * 6];
	        index = 0;
	        for (int i = 0; i < _configData.SurfaceWidthPoints - 1; i++)
	        {
		        for (int j = 0; j < _configData.SurfaceLengthPoints - 1; j++)
		        {
			        int baseIndex = i * _configData.SurfaceLengthPoints + j;
			        indices[index++] = baseIndex;
			        indices[index++] = baseIndex + 1;
			        indices[index++] = baseIndex + _configData.SurfaceLengthPoints + 1;
			        indices[index++] = baseIndex;
			        indices[index++] = baseIndex + _configData.SurfaceLengthPoints + 1;
			        indices[index++] = baseIndex + _configData.SurfaceLengthPoints;
		        }
	        }

	        Mesh.SetVertices(_originalVertices);
	        Mesh.triangles = indices;
	        Mesh.uv = GenerateUVs(_configData);
	        Mesh.RecalculateNormals();
	        GetComponent<MeshFilter>().mesh = Mesh;

	        return Mesh;
        }

        private Vector2[] GenerateUVs(DataConfigHeightmesh data)
        {
            Vector2[] uvs = new Vector2[(data.SurfaceWidthPoints * data.SurfaceLengthPoints)];
        
            //always set one uv over n tiles than flip the uv and set it again
            for (int x = 0; x <= data.SurfaceWidthPoints; x++)
            {
                for (int z = 0; z <= data.SurfaceLengthPoints; z++)
                {
                    Vector2 vec = new Vector2((x / data.UVScale) % 2, (z / data.UVScale) % 2);
                    int index = Index(x, z);
                    if (index < uvs.Length)
                    {
	                    uvs[index] = new Vector2(vec.x <= 1 ? vec.x : 2 - vec.x, vec.y <= 1 ? vec.y : 2 - vec.y);
                    }
                }
            }
        
            return uvs;
        }
        
        private int Index(int x, int z)
        {
            return x * (_configData.SurfaceWidthPoints) + z;
        }
        
        internal static float MapValue(float refValue, float refMin, float refMax, float targetMin, float targetMax)
        {
	        return targetMin + (refValue - refMin) * (targetMax - targetMin) / (refMax - refMin);
        }

        #endregion MESH
    }
}
