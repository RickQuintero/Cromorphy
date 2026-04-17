using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(EdgeCollider2D))]
[RequireComponent(typeof(WaterTriggerHandler))]
public class InteractableWater : MonoBehaviour
{
    [Header("Mesh Generation")]
    [SerializeField] private int NumOfVertices = 70;
    public float Width = 10f;
    public float Height = 4f;
    public Material WaterMaterial;
    private const int _NUM_OF_Y_VERTICES = 2;

    [Header("Gizmo Color")]
    public Color GizmoColor = Color.white;

    private Mesh _mesh;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private Vector3[] _vertices;
    private int[] _topVericesIndex;

    private EdgeCollider2D _edgeCollider;

    private void Reset()
    {
        _edgeCollider = GetComponent<EdgeCollider2D>();
        _edgeCollider.isTrigger = true;  
    }
    public void ResetEdgeCollider()
    {
        _edgeCollider = GetComponent<EdgeCollider2D>();
        Vector2[] newPoints = new Vector2[2];
        Vector2 firstPoint = new Vector2(_vertices[_topVericesIndex[0]].x, _vertices[_topVericesIndex[0]].y);
        Vector2 SecondPoint = new Vector2(_vertices[_topVericesIndex[NumOfVertices - 1]].x, _vertices[_topVericesIndex[NumOfVertices - 1]].y);
        newPoints[0] = firstPoint;
        newPoints[1] = SecondPoint;
        _edgeCollider.offset = Vector2.zero;
        _edgeCollider.points = newPoints;
    }
    private void Start()
    {
        GenerateMesh();
    }

    public void GenerateMesh()
    {
        _mesh = new Mesh();
        //add vertices
        _vertices = new Vector3[NumOfVertices * _NUM_OF_Y_VERTICES];
        _topVericesIndex = new int[NumOfVertices];
        for (int y = 0; y < _NUM_OF_Y_VERTICES; y++)
        {
            for (int x = 0; x < NumOfVertices; x++)
            {
                 // Initialize vertex positions
                 float xPos = (x / (float)(NumOfVertices - 1)) * Width - (Width / 2f);
                 float yPos = (y / (float)(_NUM_OF_Y_VERTICES - 1)) * Height - (Height / 2f);
                 _vertices[y * NumOfVertices + x] = new Vector3(xPos, yPos, 0f);
                 if (y == _NUM_OF_Y_VERTICES - 1)
                 {
                     _topVericesIndex[x] = y * NumOfVertices + x;
                 }
            }
        }
        //add triangles
        int[] triangles = new int[(NumOfVertices - 1) * (_NUM_OF_Y_VERTICES - 1) * 6];
        int triIndex = 0;
        for (int y = 0; y < _NUM_OF_Y_VERTICES - 1; y++)
        {
            for (int x = 0; x < NumOfVertices - 1; x++)
            {
                int bottomLeft = y * NumOfVertices + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + NumOfVertices;
                int topRight = topLeft + 1;
                // first triangle: bottom-left half of quad
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = topRight;
                // second triangle: top-right half of quad
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topRight;
                triangles[triIndex++] = bottomRight;

            }
        }
        // UVs
        Vector2[] uvs = new Vector2[_vertices.Length];
        for (int i = 0; i < _vertices.Length; i++)
        {
            uvs[i] = new Vector2((_vertices[i].x + Width / 2) / Width, (_vertices[i].y + Height / 2) / Height);
        }
        
        if (_meshFilter == null) 
        {
            _meshFilter = GetComponent<MeshFilter>();
        }
        if (_meshRenderer == null) 
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }
        _meshRenderer.material = WaterMaterial;
        _mesh.vertices = _vertices;
        _mesh.triangles = triangles;
        _mesh.uv = uvs;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        _meshFilter.mesh = _mesh;
    }
}

[CustomEditor(typeof(InteractableWater))]
public class InteractableWaterEditor : Editor
{
    private InteractableWater _interactableWater;
    private void OnEnable()
    {
        _interactableWater = (InteractableWater)target;
    }
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = new VisualElement();
        InspectorElement.FillDefaultInspector(root, serializedObject, this);
        root.Add(new VisualElement { style = { height = 10 } }); // Add some spacing
        Button generateMeshButton = new Button(() => _interactableWater.GenerateMesh())
        {
            text = "Generate Mesh"
        };
        Button placeEdgeColliderButton = new Button(() => _interactableWater.ResetEdgeCollider())
        {
            text = "Place Edge Collider"
        };
        root.Add(generateMeshButton);
        root.Add(placeEdgeColliderButton);
        return root;
    }
    private void ChangeDimensions(ref float width, ref float height, float calculatedWidthMax, float calculatedHeightMax)
    {
        width = Mathf.Max(0.1f, calculatedWidthMax);
        height = Mathf.Max(0.1f, calculatedHeightMax);
    }
    private void OnSceneGUI()
    {
    // Draw the wireframe box
    Handles.color = _interactableWater.GizmoColor;
    Vector3 center = _interactableWater.transform.position;
    Vector3 size = new Vector3(_interactableWater.Width, _interactableWater.Height, 0.1f);
    Handles.DrawWireCube(center, size);

    // Handles for width and height
    float handleSize = HandleUtility.GetHandleSize(center) * 0.1f;
    Vector3 snap = Vector3.one * 0.1f;

    // Corner handles
    Vector3[] corners = new Vector3[4];
    corners[0] = center + new Vector3(-_interactableWater.Width / 2, -_interactableWater.Height / 2, 0); // Bottom-left
    corners[1] = center + new Vector3(_interactableWater.Width / 2, -_interactableWater.Height / 2, 0);  // Bottom-right
    corners[2] = center + new Vector3(-_interactableWater.Width / 2, _interactableWater.Height / 2, 0);  // Top-left
    corners[3] = center + new Vector3(_interactableWater.Width / 2, _interactableWater.Height / 2, 0);   // Top-right

    // Handle for each corner
    EditorGUI.BeginChangeCheck();
    Vector3 newBottomLeft = Handles.FreeMoveHandle(corners[0], handleSize, snap, Handles.CubeHandleCap);
    if (EditorGUI.EndChangeCheck())
    {
        ChangeDimensions(ref _interactableWater.Width, ref _interactableWater.Height,
            corners[1].x - newBottomLeft.x,
            corners[3].y - newBottomLeft.y);

        _interactableWater.transform.position += new Vector3(
            (newBottomLeft.x - corners[0].x) / 2,
            (newBottomLeft.y - corners[0].y) / 2,
            0);
    }

    EditorGUI.BeginChangeCheck();
    Vector3 newBottomRight = Handles.FreeMoveHandle(corners[1], handleSize, snap, Handles.CubeHandleCap);
    if (EditorGUI.EndChangeCheck())
    {
        ChangeDimensions(ref _interactableWater.Width, ref _interactableWater.Height,
            newBottomRight.x - corners[0].x,
            corners[3].y - newBottomRight.y);

        _interactableWater.transform.position += new Vector3(
            (newBottomRight.x - corners[1].x) / 2,
            (newBottomRight.y - corners[1].y) / 2,
            0);
    }

    EditorGUI.BeginChangeCheck();
    Vector3 newTopLeft = Handles.FreeMoveHandle(corners[2], handleSize, snap, Handles.CubeHandleCap);
    if (EditorGUI.EndChangeCheck())
    {
        ChangeDimensions(ref _interactableWater.Width, ref _interactableWater.Height,
            corners[3].x - newTopLeft.x,
            newTopLeft.y - corners[0].y);

        _interactableWater.transform.position += new Vector3(
            (newTopLeft.x - corners[2].x) / 2,
            (newTopLeft.y - corners[2].y) / 2,
            0);
    }

    EditorGUI.BeginChangeCheck();
    Vector3 newTopRight = Handles.FreeMoveHandle(corners[3], handleSize, snap, Handles.CubeHandleCap);
    if (EditorGUI.EndChangeCheck())
    {
        ChangeDimensions(ref _interactableWater.Width, ref _interactableWater.Height,
            newTopRight.x - corners[2].x,
            newTopRight.y - corners[1].y);

        _interactableWater.transform.position += new Vector3(
            (newTopRight.x - corners[3].x) / 2,
            (newTopRight.y - corners[3].y) / 2,
            0);
    }

    // Update the mesh if the handles are moved
    if (GUI.changed)
    {
        _interactableWater.GenerateMesh();
    }
    }
}
