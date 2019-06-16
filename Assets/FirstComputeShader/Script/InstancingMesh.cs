using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Rendering;
using System.Linq;
using System.Runtime.InteropServices;

public class InstancingMesh : MonoBehaviour
{
    #region Propaty
    [SerializeField]
    private int _numInstance = 100;

    public Mesh _mesh;

    public Material _material;

    [SerializeField]
    private int[] _shaderPasses = new int[] { 0 };

    [SerializeField]
    private MeshTopology _topology = MeshTopology.Triangles;

    [SerializeField]
    private CameraEvent _commandAt = CameraEvent.AfterGBuffer;

    [SerializeField]
    private float _initPosRange = 10f;

    public ComputeShader _updater;

    private ComputeBuffer _meshIndicesBuffer;
    private ComputeBuffer _meshVertexDataBuffer;
    private ComputeBuffer _instanceDataBuffer;

    private CommandBuffer _command;

    private List<Camera> _targetCamList = new List<Camera>();
    #endregion

    #region Struct
    struct VertexData
    {
        public Vector3 vert;
        public Vector3 normal;
        public Vector2 uv;
        public Vector4 tangent;
    }

    struct InstanceData
    {
        public Vector3 position;
        public Quaternion rotation;
        public float scale;
    }
    #endregion

    #region MonoBehaviour Functions
    // Start is called before the first frame update
    void Start()
    {
        SetData();
    }

    void OnDestroy()
    {
        ReleaseData();      
    }

    // Update is called once per frame
    void Update()
    {
        UpdateData();
    }

    void OnRenderObject()
    {
        if (_targetCamList.Contains(Camera.current)) return;

        Camera.current.AddCommandBuffer(_commandAt, _command);
        _targetCamList.Add(Camera.current);
    }
    #endregion

    #region Private Functions
    void SetData()
    {
        var indices = _mesh.GetIndices(0);

        var vertexDataArray = Enumerable.Range(0, _mesh.vertexCount).Select(b => {
            return new VertexData()
            {
                vert = _mesh.vertices[b],
                normal = _mesh.normals[b],
                uv = _mesh.uv[b],
                tangent = _mesh.tangents[b]
            };
        }).ToArray();

        var instanceDataArray = Enumerable.Range(0, _numInstance).Select(b => {
            return new InstanceData()
            {
                position = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * _initPosRange,
                rotation = Random.rotation,
                scale = Random.value
            };
        }).ToArray();

        _meshIndicesBuffer = new ComputeBuffer(indices.Length, Marshal.SizeOf(typeof(int)));
        _meshVertexDataBuffer = new ComputeBuffer(vertexDataArray.Length, Marshal.SizeOf(typeof(VertexData)));
        _instanceDataBuffer = new ComputeBuffer(instanceDataArray.Length, Marshal.SizeOf(typeof(InstanceData)));

        _meshIndicesBuffer.SetData(indices);
        _meshVertexDataBuffer.SetData(vertexDataArray);
        _instanceDataBuffer.SetData(instanceDataArray);

        _material.SetBuffer("_Indices", _meshIndicesBuffer);
        _material.SetBuffer("_vData", _meshVertexDataBuffer);
        _material.SetBuffer("_iData", _instanceDataBuffer);

        _command = new CommandBuffer();
        _command.name = name + ".instancingMesh";

        foreach (var shaderPass in _shaderPasses)
            _command.DrawProcedural(Matrix4x4.identity, _material, shaderPass, _topology, indices.Length, _numInstance);
    }

    void ReleaseData()
    {
        new[] { _meshIndicesBuffer, _meshVertexDataBuffer, _instanceDataBuffer }.Where(b => b != null).ToList().ForEach(b =>
        {
            b.Release();
            b = null;
        });
    }

    void UpdateData()
    {
        var kernel = _updater.FindKernel("CSMain");
        _updater.SetBuffer(kernel, "_iData", _instanceDataBuffer);
        _updater.Dispatch(kernel, _numInstance / 1024 + 1, 1, 1);
    }
    #endregion
}
