using UnityEngine;

public class WorldChunk : MonoBehaviour
{
    //Private Variables
    private Vector2Int m_chunkPosition;
    private Color m_defaultColor;
    private Texture2D m_worldTexture;
    [SerializeField]private Particle[] m_particles;
    private Color[] m_chuckColour;         
    private WorldChunk[] m_neighbourChunks;
    private Vector2Int m_chunkSize;
    
    //Public Variables
    public Sprite sprite;
    public bool chunkActive;
    public bool isActiveNextStep;
    
    public void Init(Vector2Int chunkPosition,Vector2Int chunkSize)
    {
        m_chunkSize = chunkSize;
        
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        sprite = spriteRenderer.sprite;
        m_chunkPosition = chunkPosition;
        m_worldTexture = sprite.texture;
        Debug.Log($"Particle Size :{chunkSize.x} , {chunkSize.y}");
        m_particles = new Particle[chunkSize.x * chunkSize.y];

        Camera mainCamera = Camera.main;
        Bounds bounds = spriteRenderer.bounds;
        
        float screenAspect = (float)Screen.width / (float)Screen.height;
        float cameraHeight = mainCamera.orthographicSize * 2;
        Bounds cameraBounds =  new Bounds(
            mainCamera.transform.position,
            new Vector3(cameraHeight * screenAspect, cameraHeight, 0));
        
        for(int y = 0; y < chunkSize.y; y++)
        {
            int yIndex = y + m_chunkPosition.y * chunkSize.y;
            for (int x = 0; x < chunkSize.y; x++)
            {
                int xIndex = x + m_chunkPosition.x * chunkSize.x;
                int index = x + y * chunkSize.x;
                m_particles[index] = new Particle();
                m_particles[index].Init(new Vector2Int(xIndex, yIndex));
                DrawPixel(new Vector2Int(x,y), Color.white);
                
                float xRatio = x / (float)m_worldTexture.width;
                float yRatio = y / (float)m_worldTexture.height;

                Vector3 worldPos = new Vector3(bounds.min.x + (bounds.size.y * xRatio),bounds.min.y + (bounds.size.y * yRatio),0);
                if(worldPos.x < cameraBounds.min.x || worldPos.x > cameraBounds.max.x ||
                   worldPos.y < cameraBounds.min.y || worldPos.y > cameraBounds.max.y)
                {
                    m_particles[index].AddParticle(ParticleType.Wood);
                    DrawPixel(new Vector2Int(x, y), Color.red);
                }
            } 
        }

        m_chuckColour = new Color[m_particles.Length];
    }

    public Particle GetParticleAtIndex(int x, int y)
    {
        int index = x + y * m_chunkSize.x; 
        return m_particles[index];
    }
    
    public Particle GetParticleAtIndex(int index)
    {
        return m_particles[index];
    }

    public bool ContainsParticle(int x, int y)
    {
        Particle particle = GetParticleAtIndex(x, y);
        return particle != null && particle.GetParticleType() != ParticleType.Air;
    }
    
    public bool ContainsParticle(int index)
    {
        Particle particle = GetParticleAtIndex(index);
        return particle != null && particle.GetParticleType() != ParticleType.Air;
    }

    public Particle AddParticle(ParticleType type, Vector2Int particlePos)
    {
        if (ContainsParticle(particlePos.x, particlePos.y))
        {
            return null;
        }
     
        int index = particlePos.x + particlePos.y * m_chunkSize.x;
        m_particles[index].AddParticle(type);
        chunkActive = true;
        return m_particles[index];
    }

    public void DrawPixel(Vector2Int pixelPosition, Color color)
    {
        m_worldTexture.SetPixel(pixelPosition.x, pixelPosition.y, color);
        m_worldTexture.Apply();
    }

    public void UpdateTexture()
    {
        var worldManager = WorldManager.instance;
        int width = worldManager.chunkSize.x;
        int height = worldManager.chunkSize.y;
        
        for (int i = 0; i < m_particles.Length; i++)
        {
            Particle particle = m_particles[i];
            if (particle == null)
            {
                Debug.Log("Particle is null at : " + i);
                continue;
            }
            if (particle.GetParticleType() != ParticleType.Air)
            {
                m_chuckColour[i] = particle.GetParticleData().colour;
            }
            else
            {
                m_chuckColour[i]= Color.white;
            }

            if (particle.HasUpdated())
            {
                particle.SetUpdated(false);
            }
        }
        
        m_worldTexture.SetPixels(m_chuckColour);
        m_worldTexture.Apply();
    }

    public Particle[] GetParticles()
    {
        return m_particles;
    }
}
