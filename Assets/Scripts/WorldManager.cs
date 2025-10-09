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
    
    
    private WorldChunk[] m_chunks;

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

        int chunkWidth = worldSize.x / chunkSize.x;
        int chunkHeight = worldSize.y / chunkSize.y;
        int totalChunks = chunkWidth + chunkHeight;

        Debug.Log($"{chunkWidth}, {chunkHeight}");
        
        m_chunks = new WorldChunk[totalChunks];

        GameObject world = new GameObject("World");

        float chunkSprintSize = chunkSize.x / pixelPerUnit; 
        Vector3 startPosition = new Vector3()
        {
           x = Camera.main.transform.position.x - (chunkWidth * 0.5f) - (chunkSprintSize * 0.5f),
           y = Camera.main.transform.position.y - (chunkHeight * 0.5f) - (chunkSprintSize) 
        };
        
        
        for (int y = 0; y < chunkHeight; y++)
        {
            for (int x = 0; x < chunkWidth; x++)
            {
                int index = x + y;
                
                GameObject worldObj = new GameObject($"WorldChunk({x},{y})")
                {
                    transform =
                    {
                        position = startPosition 
                    }
                };
                
                Texture2D worldTexture = new Texture2D(chunkSize.x,chunkSize.y)
                {
                    filterMode = FilterMode.Point
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
        if((x >= 0 && x < m_chunks.GetLength(0)) && (y >= 0 && y < m_chunks.GetLength(1)))
        {
            return m_chunks[x, y];
        }

        return null;
    }

    public WorldChunk[,] GetAllChunks()
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
        WorldChunk chunk = WorldManager.instance.GetChunkFromParticlePosition(x, y);
        
        int pixelPositionX = x % WorldManager.instance.chunkSize.x;
        int pixelPositionY = y % WorldManager.instance.chunkSize.y;
        return chunk.GetParticleAtIndex(pixelPositionX, pixelPositionY);
    }

    public bool ContainsParticle(int x, int y)
    {
        WorldChunk chunk = WorldManager.instance.GetChunkFromParticlePosition(x, y);
        
        int pixelPositionX = x % WorldManager.instance.chunkSize.x;
        int pixelPositionY = y % WorldManager.instance.chunkSize.y;
            
        return chunk.ContainsParticle(pixelPositionX, pixelPositionY);
    }
}
