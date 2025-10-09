using UnityEngine;


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
        GenerateEmptyWorld();
    }

    private void GenerateEmptyWorld()
    {
        if (Camera.main == null)
        {
            Debug.LogError("No Main Camera Found, Please set main Camera");
            return;
        }

        m_chunkWidth = worldSize.x / chunkSize.x;
        m_chunkHeight = worldSize.y / chunkSize.y;
        int totalChunks = m_chunkWidth * m_chunkHeight;

        Debug.Log($"Chunk Size : {m_chunkWidth}, {m_chunkHeight}");
        
        m_chunks = new WorldChunk[totalChunks];

        GameObject world = new GameObject("World");

        float chunkSprintSize = chunkSize.x / pixelPerUnit; 
        Vector3 startPosition = new Vector3()
        {
           x = Camera.main.transform.position.x - (m_chunkWidth * 0.5f) - (chunkSprintSize * 0.5f),
           y = Camera.main.transform.position.y - (m_chunkHeight * 0.5f) - (chunkSprintSize) 
        };
        
        ChunkShiftX = (int)Mathf.Log(m_chunkWidth, 2);
        ChunkShiftY = (int)Mathf.Log(m_chunkHeight, 2);
        ChunkMaskX  = m_chunkWidth - 1;
        ChunkMaskY  = m_chunkHeight - 1;
        
        for (int y = 0; y < m_chunkHeight; y++)
        {
            for (int x = 0; x < m_chunkWidth; x++)
            {
                int index = x + y * m_chunkWidth;
                
                GameObject worldObj = new GameObject($"WorldChunk({x},{y})")
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
                
                SpriteRenderer spriteRenderer = worldObj.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = Sprite.Create(
                    worldTexture,
                    new Rect(0, 0, chunkSize.x, chunkSize.y),
                    Vector2.one * 0.5f,
                    pixelPerUnit);
                
                WorldChunk worldChunk = worldObj.AddComponent<WorldChunk>();
                
                int positionOffsetX = (chunkSize.x  / pixelPerUnit) * x;
                int positionOffsetY = (chunkSize.y  / pixelPerUnit) * y;
                worldObj.transform.position += new Vector3(positionOffsetX, positionOffsetY);
                
                worldChunk.Init(new Vector2Int(x, y), chunkSize);
                worldChunk.transform.SetParent(world.transform);
                m_chunks[index] = worldChunk;
            }
        }

        ParticleLogic logic = world.AddComponent<ParticleLogic>();
        logic.Init(this);
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

    public WorldChunk[] GetAllChunks()
    {
        return m_chunks;
    }

    public WorldChunk GetChunkFromParticlePosition(int x, int y)
    {
        int chunkPositionX = x / WorldManager.instance.chunkSize.x;
        int chunkPositionY = y / WorldManager.instance.chunkSize.y;

        return GetChunk(chunkPositionX, chunkPositionY);
    }

    public Particle GetParticle(int x, int y)
    {
        WorldChunk chunk = GetChunkFromParticlePosition(x, y);
        
        int pixelPositionX = x % WorldManager.instance.chunkSize.x;
        int pixelPositionY = y % WorldManager.instance.chunkSize.y;
        int index = pixelPositionX + pixelPositionY * WorldManager.instance.chunkSize.x;
        
        return chunk.GetParticleAtIndex(index);
    }

    
    public static int ChunkShiftX, ChunkShiftY;
    public static int ChunkMaskX,  ChunkMaskY;
    public bool ContainsParticle(int x, int y)
    {
        WorldChunk chunk = WorldManager.instance.GetChunkFromParticlePosition(x, y);
        
        int pixelPositionX = x % WorldManager.instance.chunkSize.x;
        int pixelPositionY = y % WorldManager.instance.chunkSize.y;
        
        int index = pixelPositionX + pixelPositionY * WorldManager.instance.chunkSize.x;
            
        return chunk.ContainsParticle(index);
    }
}
