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
        [SerializeField] private DataConfigRipple _dataConfigRipples;

        [Header("Neighbors")]
        [SerializeField] private Heightmesh _neighborTop;
        [SerializeField] private Heightmesh _neighborBottom;

        [Header("Ripple Waves")]
        [SerializeField] private Transform[] _waveSources;

        internal DataConfigHeightmesh DataConfig => _configData;
        internal Vector3[] OriginalVertices => _originalVertices;
        internal Mesh Mesh { get; private set; }

        private bool _debug;
        private DataConfigHeightmesh _configData;
        private DataHeightmesh _data;
        private Vector3[] _originalVertices;
        private int _surfaceWidthPoints;
        private int _surfaceLengthPoints;
        private List<int> _topEdgeVertices = new List<int>();
        private List<int> _bottomEdgeVertices  = new List<int>();
        private List<int> _rightEdgeVertices  = new List<int>();	
        private List<int> _leftEdgeVertices  = new List<int>();		// Bottom to top
        private readonly List<DataHeightwaveRipple> _dataRipples = new List<DataHeightwaveRipple>();

        #endregion VARIABLES
        
        
        #region INITIALIZATION

        internal void Setup(DataConfigHeightmesh configData)
        {
	        _configData = configData;
	        _configData.OnValidated += OnConfigValidated;

	        Mesh = new Mesh
	        {
		        // Use 32 bit index buffer to allow water grids larger than ~250x250
		        indexFormat = IndexFormat.UInt32
	        };
	        
	        GetComponent<MeshFilter>().mesh = Mesh;
	        CreateMesh();
        }
        
        private void OnDisable()
        {
	        _configData.OnValidated -= OnConfigValidated;
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
	        DoCalculateMeshNormals();
	        Mesh.RecalculateTangents();

	        // vertices = Mesh.vertices;
	        // normals = Mesh.normals;
        }

	    private void DoCalculateMeshNormals()
	    {
		    Mesh.RecalculateNormals();
		    // Vector3[] norms = Mesh.normals;
		    // int count = Mesh.vertexCount;
		    //
		    // // Outer vertices need normals adjusted to prevent neighboring heighmeshes from having inconsistent lighting
		    // for (int i = 0; i < count; i++)
		    // {
			   //  // Is this vertex on the edge?
			   //  if (i < _surfaceLengthPoints ||				// left side
			   //      i >= count - _surfaceLengthPoints ||	// right side
			   //      i % _surfaceWidthPoints == 0 ||			// bottom
			   //      (i + 1) % _surfaceWidthPoints == 0)		// top
			   //  {
				  //   norms[i] = Vector3.up;
			   //  }
		    // }
		    //
		    // Mesh.normals = norms;
	    }
	    
	    Vector3[] vertices, normals;
	    void OnDrawGizmos () 
	    {
		    if (Mesh == null || !_debug) {
			    return;
		    }

		    Transform t = transform;
		    Gizmos.color = Color.cyan;
		    for (int i = 0; i < Mesh.vertexCount; i++)
		    {
			    Vector3 position = t.TransformPoint(vertices[i]);
			    Gizmos.color = Color.cyan;
			    Gizmos.DrawSphere(position, 1f);
			    Gizmos.color = Color.green;
			    Gizmos.DrawRay(position, normals[i] * 5f);
		    }
		    
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
	        _surfaceWidthPoints = Mathf.Clamp
		        (Mathf.FloorToInt(_configData.Size * _configData.Resolution), 2, Mathf.FloorToInt(_configData.Size));
	        _surfaceWidthPoints = Mathf.Max(_surfaceWidthPoints, 2);
	        _surfaceLengthPoints = _surfaceWidthPoints;	// May allow different width/height point densities in future
	        
	        _originalVertices = new Vector3[_surfaceWidthPoints * _surfaceLengthPoints];
	        _topEdgeVertices.Clear();
	        _bottomEdgeVertices.Clear();
	        _rightEdgeVertices.Clear();
	        _leftEdgeVertices.Clear();
	        
	        int index = 0;
	        for (int i = 0; i < _surfaceWidthPoints; i++)
	        {
		        for (int j = 0; j < _surfaceLengthPoints; j++)
		        {
			        float x = MapValue(
				        i, 
				        0.0f, 
				        _surfaceWidthPoints - 1, 
				        -sizeWidth / 2.0f,
				        sizeWidth / 2.0f);
			        float z = MapValue(
				        j, 
				        0.0f, 
				        _surfaceLengthPoints - 1, 
				        -sizeLength / 2.0f, 
				        sizeLength / 2.0f);

			        Vector3 pos = new Vector3(x, 0f, z);
			        _originalVertices[index++] = pos;
		        }
	        }

	        // Create an index buffer for the grid
	        int[] indices = new int[(_surfaceWidthPoints - 1) * (_surfaceLengthPoints - 1) * 6];
	        index = 0;
	        for (int i = 0; i < _surfaceWidthPoints - 1; i++)
	        {
		        for (int j = 0; j < _surfaceLengthPoints - 1; j++)
		        {
			        int baseIndex = i * _surfaceLengthPoints + j;
			        indices[index++] = baseIndex;
			        indices[index++] = baseIndex + 1;
			        indices[index++] = baseIndex + _surfaceLengthPoints + 1;
			        indices[index++] = baseIndex;
			        indices[index++] = baseIndex + _surfaceLengthPoints + 1;
			        indices[index++] = baseIndex + _surfaceLengthPoints;
		        }
	        }

	        Mesh.SetVertices(_originalVertices);
	        Mesh.triangles = indices;
	        Mesh.uv = GenerateUVs(_surfaceWidthPoints, _surfaceWidthPoints, _surfaceLengthPoints);
	        
	        // Outer vertices need normals adjusted to prevent neighboring heighmeshes from having inconsistent lighting
	        for (int i = 0; i < _originalVertices.Length; i++)
	        {
				if (i < _surfaceLengthPoints)
				{
					_leftEdgeVertices.Add(i);
				}						
	            else if (i >= _originalVertices.Length - _surfaceLengthPoints)
				{
					_rightEdgeVertices.Add(i);
				}
				else if (i % _surfaceWidthPoints == 0)
				{
					_bottomEdgeVertices.Add(i);
				}
				else if ((i + 1) % _surfaceWidthPoints == 0)
		        {
					 _topEdgeVertices.Add(i);
		        }
	        }

	        return Mesh;
        }

        private Vector2[] GenerateUVs(int UVScale, int surfaceWidthPoints, int surfaceLengthPoints)
        {
            Vector2[] uvs = new Vector2[surfaceWidthPoints * surfaceLengthPoints];
        
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
