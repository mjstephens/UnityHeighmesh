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
    public class Heightmesh : MonoBehaviour, ISolverResultsReceivable
    {
        #region VARIABLES

        [Header("Data")]
        [SerializeField] private DataConfigHeightmesh _configData;
        [SerializeField] private DataConfigRipple _dataConfigRipples;

        [Header("Ripple Waves")]
        [SerializeField] private Transform[] _waveSources;

        internal DataConfigHeightmesh DataConfig => _configData;
        internal Mesh Mesh { get; private set; }
        internal Vector3[] OriginalVertices => _originalVertices;

        //private Solver _solver;
        private DataHeightmesh _data;
        private Vector3[] _originalVertices;
        private static float _time;
        private readonly List<DataHeightwaveRipple> _dataRipples = new List<DataHeightwaveRipple>();

        #endregion VARIABLES
        
        
        #region INITIALIZATION

        private void Awake()
        {
	        Mesh = new Mesh
	        {
		        // Use 32 bit index buffer to allow water grids larger than ~250x250
		        indexFormat = IndexFormat.UInt32
	        };
	        
	        GetComponent<MeshFilter>().mesh = Mesh;
	        CreateMesh();
	        _configData.OnValidated = OnConfigValidated;
	        // if (_solver == null)
	        // {
	        //  _solver = ResolveSolver();
	        // }
        }
        
        // private Solver ResolveSolver()
        // {
	       //  switch (_configData.Mode)
	       //  {
		      //   case SimulationSolverMode.CPU_Naive: 
			     //    return new Solver_CPUNaive();
			     //    break;
		      //   case SimulationSolverMode.CPU_Job:
		      //   case SimulationSolverMode.CPU_JobBurst:
		      //   case SimulationSolverMode.CPU_JobBurstThreaded:
			     //    return new Solver_CPUJob();
			     //    break;
		      //   case SimulationSolverMode.GPU_Compute:
        //
			     //    break;
	       //  }
	       //  
	       //  // Fallback
	       //  return new Solver_CPUNaive();
        // }

        private void OnEnable()
        {
	        SystemHeightmesh.Instance.RegisterHeightmesh(this);
        }

        private void OnDisable()
        {
	        SystemHeightmesh.Instance.UnregisterHeightmesh(this);
        }

        private void OnConfigValidated()
        {
	        if (Application.isPlaying)
	        {
		        CreateMesh();
	        }
        }

        #endregion INITIALIZATION

        
        #region SOLVE

	    void ISolverResultsReceivable.DoReceiveSolverResults(List<Vector3> positions, List<Vector2> offsets)
        {
	        Mesh.SetVertices(positions);
	        Mesh.RecalculateNormals();
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
	        Mesh.Clear();

	        // Create initial grid of vertex positions
	        float sizeWidth = _configData.Size;
	        float sizeLength = _configData.Size;
	        int surfaceWidthPoints = Mathf.Clamp
		        (Mathf.FloorToInt(_configData.Size * _configData.Resolution), 1, Mathf.FloorToInt(_configData.Size));
	        int surfaceLengthPoints = surfaceWidthPoints;
	        
	        _originalVertices = new Vector3[surfaceWidthPoints * surfaceLengthPoints];
	        
	        int index = 0;
	        for (int i = 0; i < surfaceWidthPoints; i++)
	        {
		        for (int j = 0; j < surfaceLengthPoints; j++)
		        {
			        float x = MapValue(
				        i, 
				        0.0f, 
				        surfaceWidthPoints - 1, 
				        -sizeWidth / 2.0f,
				        sizeWidth / 2.0f);
			        float z = MapValue(
				        j, 
				        0.0f, 
				        surfaceLengthPoints - 1, 
				        -sizeLength / 2.0f, 
				        sizeLength / 2.0f);

			        Vector3 pos = new Vector3(x, 0f, z);
			        _originalVertices[index++] = pos;
		        }
	        }

	        // Create an index buffer for the grid
	        int[] indices = new int[(surfaceWidthPoints - 1) * (surfaceLengthPoints - 1) * 6];
	        index = 0;
	        for (int i = 0; i < surfaceWidthPoints - 1; i++)
	        {
		        for (int j = 0; j < surfaceLengthPoints - 1; j++)
		        {
			        int baseIndex = i * surfaceLengthPoints + j;
			        indices[index++] = baseIndex;
			        indices[index++] = baseIndex + 1;
			        indices[index++] = baseIndex + surfaceLengthPoints + 1;
			        indices[index++] = baseIndex;
			        indices[index++] = baseIndex + surfaceLengthPoints + 1;
			        indices[index++] = baseIndex + surfaceLengthPoints;
		        }
	        }

	        Mesh.SetVertices(_originalVertices);
	        Mesh.triangles = indices;
	        Mesh.uv = GenerateUVs(surfaceWidthPoints, surfaceWidthPoints, surfaceLengthPoints);
	        Mesh.RecalculateNormals();

	        return Mesh;
        }

        private Vector2[] GenerateUVs(int UVScale, int surfaceWidthPoints, int surfaceLengthPoints)
        {
            Vector2[] uvs = new Vector2[(surfaceWidthPoints * surfaceLengthPoints)];
        
            //always set one uv over n tiles than flip the uv and set it again
            for (int x = 0; x <= surfaceWidthPoints; x++)
            {
                for (int z = 0; z <= surfaceLengthPoints; z++)
                {
                    Vector2 vec = new Vector2((x / UVScale) % 2, (z / UVScale) % 2);
                    int index = Index(x, z, surfaceWidthPoints);
                    if (index < uvs.Length)
                    {
	                    uvs[index] = new Vector2(vec.x <= 1 ? vec.x : 2 - vec.x, vec.y <= 1 ? vec.y : 2 - vec.y);
                    }
                }
            }
        
            return uvs;
        }
        
        private int Index(int x, int z, int surfaceWidthPoints)
        {
            return x * surfaceWidthPoints + z;
        }
        
        internal static float MapValue(float refValue, float refMin, float refMax, float targetMin, float targetMax)
        {
	        return targetMin + (refValue - refMin) * (targetMax - targetMin) / (refMax - refMin);
        }

        #endregion MESH
    }
}
