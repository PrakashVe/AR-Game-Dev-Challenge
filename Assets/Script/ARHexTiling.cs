using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARHexTiling : MonoBehaviour
{
    [SerializeField]
    private ARPlaneManager arPlaneManager;
    [SerializeField]
    private GameObject hexPrefab;  // Assign a prefab for your hexagon tile
    [SerializeField]
    private Material[] hexMaterials;  // Assign materials for different hexagon colors

    private Dictionary<string, List<GameObject>> hexTiles = new();
    private float hexRadius = 0.5f;  // Radius of the hexagon (adjust to match your prefab size)


    // Start is called before the first frame update
    void Start()
    {
        // Get the hexagon radius from the prefab
        hexRadius = GetHexRadius(hexPrefab);
    }


    void OnEnable()
    {
        arPlaneManager.planesChanged += PlanesChanged;
    }

    void OnDisable()
    {
        arPlaneManager.planesChanged -= PlanesChanged;
    }

    void PlanesChanged(ARPlanesChangedEventArgs args)
    {
        foreach (var addedPlane in args.added)
        {
            CreateHexagonalTiling(addedPlane);
        }

        foreach (var updatedPlane in args.updated)
        {
            CreateHexagonalTiling(updatedPlane);
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
    private float GetHexRadius(GameObject hexPrefab)
    {
        // Get the MeshRenderer from the prefab
        MeshRenderer meshRenderer = hexPrefab.GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            // Get the bounding box size
            Bounds bounds = meshRenderer.bounds;

            // Calculate the radius from the width of the bounding box
            // Width of hexagon = ?3 * radius, so radius = width / ?3
            float width = bounds.size.x;
            float radius = width / Mathf.Sqrt(3);

            return radius;
        }

        // Default radius if no MeshRenderer is found
        return 0;  // Adjust this default if needed
    }

    void CreateHexagonalTiling(ARPlane arPlane)
    {
        // If plane not in hextiles, add it
        if (!hexTiles.ContainsKey(arPlane.trackableId.ToString()))
        {
            hexTiles.Add(arPlane.trackableId.ToString(), new List<GameObject>());
        }

        // Clear old hex tiles
        foreach (GameObject hexTile in hexTiles[arPlane.trackableId.ToString()])
        {
            Destroy(hexTile);
        }
        hexTiles[arPlane.trackableId.ToString()].Clear();

        // Get mesh collider for the ARPlane
        MeshCollider planeMesh = arPlane.GetComponent<MeshCollider>();
        if (!planeMesh || !planeMesh.sharedMesh)
        {
            // No mesh collider found, cannot create hexagonal tiling
            return;
        }

        Vector3[] meshVertices = planeMesh.sharedMesh.vertices;
        Vector3 minBounds = meshVertices[0];
        Vector3 maxBounds = meshVertices[0];

        // Find the min and max bounds of the plane
        foreach (Vector3 vertex in meshVertices)
        {
            minBounds.x = Mathf.Min(minBounds.x, vertex.x);
            minBounds.z = Mathf.Min(minBounds.z, vertex.z);
            maxBounds.x = Mathf.Max(maxBounds.x, vertex.x);
            maxBounds.z = Mathf.Max(maxBounds.z, vertex.z);
        }

        // Loop through hexagon grid positions
        float hexWidth = Mathf.Sqrt(3) * hexRadius / 2;
        float hexHeight = 2 * hexRadius + hexWidth;
        Vector3 hexStart = new(minBounds.x, 0, minBounds.z);
        
        int i = 0;

        // Generate hexagon grid
        while (hexStart.x + i * hexWidth <= maxBounds.x)
        {
            for (float z = hexStart.z; z <= maxBounds.z; z += hexHeight)
            {
                float x = hexStart.x + i * hexWidth;
                Vector3 hexPosition = new(x, 0, z);//arPlane.transform.position;

                // Offset every other row
                if ((i % 2) == 1)
                {
                    hexPosition.z += hexHeight / 2;
                    
                }

                // Check if hexagon is inside the plane boundary
                if (IsHexagonInPolygon(hexPosition, planeMesh))
                {
                    // Instantiate the hex tile
                    GameObject hexTile = Instantiate(hexPrefab);
                    hexTile.transform.SetParent(arPlane.transform);
                    hexTile.transform.localPosition = hexPosition;
                    hexTile.transform.localRotation = Quaternion.identity;

                    // Set material based on row number
                    hexTile.GetComponent<MeshRenderer>().material = hexMaterials[i % 3];
                    // Add to list
                    hexTiles[arPlane.trackableId.ToString()].Add(hexTile);
                }
            }

            i++;
        }
    }


    private bool IsHexagonInPolygon(Vector3 hexCenter, MeshCollider planeMesh)
    {
        // Calculate the 6 vertices of the hexagon based on its center and radius
        Vector3[] hexVertices = new Vector3[6];
        Vector3 rotatedCentre = planeMesh.transform.position + planeMesh.transform.rotation * hexCenter;

        for (int i = 0; i < 6; i++)
        {
            // Calculate the angle for each vertex (60 degrees between vertices)
            float angleDeg = 60 * i;
            float angleRad = Mathf.Deg2Rad * angleDeg;

            // Compute each vertex position relative to the hex center
            hexVertices[i] = new Vector3(
                rotatedCentre.x + hexRadius * Mathf.Cos(angleRad),
                rotatedCentre.y,
                rotatedCentre.z + hexRadius * Mathf.Sin(angleRad)
            );
        }
        // Check if all vertices are inside the polygon
        foreach (Vector3 vertex in hexVertices)
        {
            Ray ray = new(vertex + Vector3.up * 5, Vector3.down);  // Cast ray downward from above

            // Perform a raycast to check if the vertex is inside the plane boundary
            if (!planeMesh.Raycast(ray, out _, 10))
            {
                return false;
            }
        }
        // If all vertices are inside, return true
        return true;
    }    
}
