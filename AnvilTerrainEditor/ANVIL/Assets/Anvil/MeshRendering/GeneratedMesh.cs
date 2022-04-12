using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GeneratedMesh : MonoBehaviour
{
    // ---
    //  Prefab Objects
    // ---
    // [Header("Vert Visualization")]
    // [SerializeField] protected bool visualizeVerts = false;
    // [SerializeField] protected GameObject vert_vis;
    // protected List<GameObject> vert_list;

    // ---
    //  Generated Data Fields
    // ---
    [Header("Mesh Fields")]
    [SerializeField] protected Vector3[] vertices;
    // [SerializeField] protected Vector3[] normals;
    [SerializeField] protected int[] indices;

    // ---
    //  Unity Mesh Components
    // ---
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    Mesh mesh;

    public void SetMesh(Vector3[] v, int[] i)
    {
        vertices = v;
        indices = i;
    }

    private async void RenderMesh() {
        mesh.Clear();

        mesh.vertices = vertices;


        // if (normals.Length > 0)
        //     mesh.normals = normals;

        mesh.triangles = indices;
    }
    
    void Awake()
    {
        #if UNITY_EDITOR
        meshFilter = this.GetComponent<MeshFilter>();
        meshRenderer = this.GetComponent<MeshRenderer>();
        // vert_list = new List<GameObject>();

        mesh = new Mesh();
        meshFilter.sharedMesh = mesh;

        #endif
    }

    void Start()
    {
        #if UNITY_EDITOR
        SetMesh(vertices, indices);

        #endif

        #if !UNITY_EDITOR
        // if (visualizeVerts && vert_vis != null)
        // {
        //     // Delete all prefabs
        //     for (int i = vert_list.Count-1; i >= 0; --i )
        //     {
        //         DestroyImmediate(vert_list[i]);
        //     }

        //     // Create new prefabs
        //     vert_list.Clear();
        //     foreach (Vector3 vector in vertices)
        //     {
        //         GameObject newVert = Instantiate(vert_vis, transform.position+vector, Quaternion.identity);
        //         vert_list.Add(newVert);
        //     }
        // }
        #endif
    }
    
    void Update()
    {
        RenderMesh();
    }

    void OnDrawGizmos()
    {

        // Display the explosion radius when selected
        Gizmos.color = Color.green;

        foreach (Vector3 vector in vertices)
        {
            Gizmos.DrawWireSphere(transform.position+vector, 0.1f);
        }
        
    }

}
