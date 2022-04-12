using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenTerrain : MonoBehaviour
{
    protected static string TerrainTag = "GeneratedTerrain";
    [SerializeField] protected GameObject MeshPrefab;

    // Collection references
    protected Transform islandCollection;
    protected List<GeneratedMesh> islands = new List<GeneratedMesh>();
    protected Transform bridgeCollection;
    protected List<GeneratedMesh> bridges = new List<GeneratedMesh>();

    // Create a new Generated Mesh object, and assign the provided verts and indices.
    // Add the new object to it's collection, and organize in the heirarchy.
    public void AddIsland(Vector3 position, Vector3[] verts, int[] indices)
    {
        GeneratedMesh newIsland = Instantiate(MeshPrefab, islandCollection).GetComponent<GeneratedMesh>();
        newIsland.gameObject.name = "Island (" + (islands.Count+1) + ")";
        newIsland.transform.position = position;

        newIsland.SetMesh(verts, indices);

        islands.Add(newIsland);
    }

    public void AddBridge(Vector3 position, Vector3[] verts, int[] indices)
    {
        GeneratedMesh newBridge = Instantiate(MeshPrefab, bridgeCollection).GetComponent<GeneratedMesh>();
        newBridge.gameObject.name = "Bridge (" + (bridges.Count+1) + ")";
        newBridge.transform.position = position;

        newBridge.SetMesh(verts, indices);
        
        bridges.Add(newBridge);
    }

    public void InitTerrain()
    {
        // Find all objects with the "GeneratedTerrain" tag and disable them.
        GameObject[] taggedTerrains = GameObject.FindGameObjectsWithTag(TerrainTag);
        foreach (GameObject ob in taggedTerrains) {
            ob.SetActive(false);
        }

        this.tag = TerrainTag;

        islandCollection = new GameObject("Islands").transform;
        islandCollection.parent = this.transform;

        bridgeCollection = new GameObject("Bridges").transform;
        bridgeCollection.parent = this.transform;
    }

    public void SetColliders() {
        foreach (GeneratedMesh mesh in islands) {
            mesh.gameObject.AddComponent(typeof(MeshCollider));
        }

        foreach (GeneratedMesh mesh in bridges) {
            mesh.gameObject.AddComponent(typeof(MeshCollider));
        }
    }
}
