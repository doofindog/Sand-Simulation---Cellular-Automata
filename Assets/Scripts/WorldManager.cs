using System.Collections;
using UnityEngine;
using UnityEngine.Profiling;


/// <summary>
/// Manager class used to generate worlds
/// </summary>
public class WorldManager : MonoBehaviour
{
    public static WorldManager instance;
    
    [Tooltip("Pixel size of a particle")]
    public int pixelPerUnit;
    [Tooltip("Size of the World. should be divisible by pixel per unit")]
    public Vector2Int worldSize;
    [Tooltip("A small section of the  world. should be divisible by pixel per unit")]
    public Vector2Int chunkSize;
    
    
    [SerializeField] private WorldChunk[] m_chunks;
    private int m_chunkWidth;
    private int m_chunkHeight;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        StartCoroutine(GenerateEmptyWorld());
    }

    private IEnumerator GenerateEmptyWorld()
    {
        if (Camera.main == null)
        {
            Debug.LogError("No Main Camera Found, Please set main Camera");
            yield break;
        }

        m_chunkWidth = worldSize.x / chunkSize.x;
        m_chunkHeight = worldSize.y / chunkSize.y;
        int totalChunks = m_chunkWidth * m_chunkHeight;
        
        m_chunks = new WorldChunk[totalChunks];

        GameObject world = new GameObject("World");
        
        Vector3 startPosition = Vector3.zero;
        
        for (int y = 0; y < m_chunkHeight; y++)
        {
            for (int x = 0; x < m_chunkWidth; x++)
            {
                int index = x + y * m_chunkWidth;
                
                GameObject chunkObj = new GameObject($"WorldChunk({x},{y})")
                {
                    transform =
                    {
                        position = startPosition 
                    }
                };
                
                Texture2D worldTexture = new Texture2D(chunkSize.x,chunkSize.y, TextureFormat.RGBA32, false, true)
                {
                    filterMode = FilterMode.Point,
                };
                
                SpriteRenderer spriteRenderer = chunkObj.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = Sprite.Create(
                    worldTexture,
                    new Rect(0, 0, chunkSize.x, chunkSize.y),
                    Vector2.one * 0.5f,
                    pixelPerUnit);
                
                WorldChunk worldChunk = chunkObj.AddComponent<WorldChunk>();
                
                int positionOffsetX = (chunkSize.x  / pixelPerUnit) * x;
                int positionOffsetY = (chunkSize.y  / pixelPerUnit) * y;
                chunkObj.transform.position += new Vector3(positionOffsetX, positionOffsetY);
                worldChunk.Init(index, new Vector2Int(x, y), chunkSize);
                worldChunk.transform.SetParent(world.transform);
                m_chunks[index] = worldChunk;
            }
        }
        yield return null;
        

        ParticleLogic logic = world.AddComponent<ParticleLogic>();
        logic.Init(this);
        
        Camera mainCamera = Camera.main;
        int size = worldSize.y / chunkSize.x;
        mainCamera.orthographicSize = size;
        mainCamera.transform.position = new Vector3(size - 1, size - 1, -10); 
        
    }

    public WorldChunk GetChunk(int x, int y)
    {
        if((x >= 0 && x < m_chunkWidth) && (y >= 0 && y < m_chunkHeight))
        {
            int index = x + y * m_chunkWidth;
            return m_chunks[index];
        }

        return null;
    }

    public WorldChunk GetChunk(int index)
    {
        if (index < 0 || index >= m_chunks.Length)
            return null;

        return m_chunks[index];
    }

    public WorldChunk[] GetAllChunks()
    {
        return m_chunks;
    }

    public WorldChunk GetChunkFromParticlePosition(int x, int y)
    {
        int chunkPositionX = x / chunkSize.x;
        int chunkPositionY = y / chunkSize.y;

        return GetChunk(chunkPositionX, chunkPositionY);
    }

    public Particle GetParticle(int x, int y)
    {
        WorldChunk chunk = GetChunkFromParticlePosition(x, y);
        
        int pixelPositionX = x % chunkSize.x;
        int pixelPositionY = y % chunkSize.y;
        int index = pixelPositionX + pixelPositionY * chunkSize.x;
        
        return chunk.GetParticleAtIndex(index);
    }

    public bool ContainsParticle(int x, int y)
    {
        WorldChunk chunk = GetChunkFromParticlePosition(x, y);
        
        int pixelPositionX = x % chunkSize.x;
        int pixelPositionY = y % chunkSize.y;
        
        int index = pixelPositionX + pixelPositionY * chunkSize.x;
            
        return chunk.ContainsParticle(index);
    }
}
