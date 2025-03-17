using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour
{
    [Header("Size & Offset")]
    [SerializeField] private int sizeX = 10;
    [SerializeField] private int sizeY = 10;

    [SerializeField] private float offsetX;
    [SerializeField] private float offsetZ;

    [Header("Noise")]
    [SerializeField] private float noiseScale = 5f;
    [SerializeField] private float heightMultiplier = 7f;

    [SerializeField] private int octavesCount = 1;
    [SerializeField] private float lacunarity = 2f;
    [SerializeField] private float persistance = 0.5f;

    [SerializeField] private Material mat;
    [Header("Textures")]
    [SerializeField] private float textureScale = 1.0f;
    [SerializeField] private float blendFactorMultiplier = 5.0f;
    [SerializeField] private List<Layer> terrainLayers = new List<Layer>();
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Mesh mesh;




    private bool inConstantGenerationCoroutine;
    private bool constantGenration;
    private bool fly;

    private void Start()
    {
        CreateMesh();
        GenerateNewTerrain();
        constantGenration = false;
        inConstantGenerationCoroutine = false;
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.T))
        {
            GenerateNewTerrain();
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            constantGenration = !constantGenration;
            
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            fly = !fly;
        }

        if (fly)
        {
            offsetX += 10 * Time.deltaTime;
        }

        if (constantGenration && !inConstantGenerationCoroutine)
        {
            StartCoroutine(ConstantGeneration());
        }
    }

    private IEnumerator ConstantGeneration()
    {
        inConstantGenerationCoroutine = true;
        GenerateNewTerrain();
        yield return new WaitForSeconds(0.01f);
        inConstantGenerationCoroutine = false;
    }

    public void GenerateNewTerrain()
    {
        mesh.Clear();
        GenerateVerticies();
        GenerateTriangles();
        mesh.RecalculateNormals();
        SetMaterialData();
        UpdateCollision();
        print("Generated");

    }

    private void CreateMesh()
    {
        if(GetComponent<MeshFilter>() == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        if(GetComponent<MeshRenderer>() == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        if(GetComponent<MeshCollider>() == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        mesh = new Mesh();
        meshFilter.mesh = mesh;
    }

    private void SetMaterialData()
    {
        float minTerrainHeight = mesh.bounds.min.y + transform.position.y;
        float maxTerrainHeight = mesh.bounds.max.y + transform.position.y;

        mat.SetFloat("_MinTerrainHeight", minTerrainHeight);
        mat.SetFloat("_MaxTerrainHeight", maxTerrainHeight);
        mat.SetFloat("_textureScale", textureScale);
        mat.SetFloat("_blendFactorMultiplier", blendFactorMultiplier);

        int layersCount = terrainLayers.Count;
        mat.SetInt("_NumTextures", layersCount);
        float[] heights = new float[layersCount];

        int index = 0;
        foreach(Layer l in terrainLayers)
        {
            heights[index] = l.startHeight;
            index++;
        }
        mat.SetFloatArray("_TerrainHeights", heights);
        System.Array.Sort(heights);
        mat.SetFloatArray("_TerrainHeights", heights);

        Texture2DArray textures = new Texture2DArray(512, 512, layersCount, TextureFormat.RGBA32, true);
        textures.anisoLevel = 9;
        textures.filterMode = FilterMode.Bilinear;
        textures.wrapMode = TextureWrapMode.Repeat;

        for (int i = 0; i < layersCount; i++)
        {
            Graphics.CopyTexture(terrainLayers[i].texture, 0, 0, textures, i, 0);
        }

        textures.Apply();
        mat.SetTexture("_terrainTextures", textures);

        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshRenderer.material = mat;
    }

    private void UpdateCollision()
    {
        MeshCollider meshColl = gameObject.GetComponent<MeshCollider>();
        meshColl.sharedMesh = mesh;
    }

    private void GenerateTriangles()
    {
        int[] triangles = new int[sizeX * sizeY * 6];

        int vertex = 0;
        int triangleIndex = 0;

        for (int z = 0; z < sizeY; z++)
        {
            for (int x = 0; x < sizeX; x++)
            {
                triangles[triangleIndex + 0] = vertex + 0;
                triangles[triangleIndex + 1] = vertex + sizeX + 1;
                triangles[triangleIndex + 2] = vertex + 1;

                triangles[triangleIndex + 3] = vertex + 1;
                triangles[triangleIndex + 4] = vertex + sizeX + 1;
                triangles[triangleIndex + 5] = vertex + sizeX + 2;

                vertex++;
                triangleIndex += 6;
            }
            vertex++;
        }

        mesh.triangles = triangles;
    }

    private void GenerateVerticies()
    {
        Vector3[] verticies = new Vector3[(sizeX + 1) * (sizeY + 1)];

        int i = 0;
        for (int z = 0; z <= sizeX; z++)
        {
            for (int x = 0; x <= sizeY; x++)
            {
                verticies[i] = new Vector3(x, ReturnYPerlinNoise(x, z), z);
                i++;
            }
        }

        mesh.vertices = verticies;
    }

    private float ReturnYPerlinNoise(int x, int z)
    {
        float noiseScale2 = noiseScale / 100;
        float perlinNoise = 0;

        for(int o = 0; o < octavesCount; o++)
        {
            float frequency = Mathf.Pow(lacunarity, o);
            float amplitude = Mathf.Pow(persistance, o);

            perlinNoise += Mathf.PerlinNoise((x + offsetX) * noiseScale2 * frequency, (z + offsetZ) * noiseScale2 * frequency) * amplitude;
        }
        perlinNoise *= heightMultiplier;

        return perlinNoise;
    }
}

[System.Serializable]
public class Layer
{
    public Texture2D texture;
    [Range(0, 1)] public float startHeight;
}


[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TerrainGenerator terrainGenerator = (TerrainGenerator)target;

        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();

        if (EditorGUI.EndChangeCheck())
        {
            terrainGenerator.GenerateNewTerrain();
        }

        if (GUILayout.Button("Generate"))
        {
            terrainGenerator.GenerateNewTerrain();
        }
    }
}
