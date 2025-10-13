using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class WorldChunk : MonoBehaviour
{
    //Private Variables
    private int m_chunkId;
    private Vector2Int m_chunkPosition;
    private Color m_defaultColor;
    private Texture2D m_worldTexture;
    [SerializeField] private Particle[] m_particles;
    private Particle[] m_modifiedParticles;
    private Color[] m_chuckColour;         
    private WorldChunk[] m_neighbourChunks;
    private Vector2Int m_chunkSize;
    private Camera m_mainCamera;
    
    //Public Variables
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    public Sprite sprite;
    public bool chunkActive;
    public bool isActiveNextFrame;
    
    public int ChunkId => m_chunkId;
    public Vector2Int ChunkPosition => m_chunkPosition;
    
    public void Init(int chunkIndex, Vector2Int chunkPosition,Vector2Int chunkSize)
    {
        m_chunkId = chunkIndex;
        m_chunkSize = chunkSize;
        m_chunkPosition = chunkPosition;


        if (m_spriteRenderer == null)
        {
            if(!TryGetComponent(out m_spriteRenderer))
                return;
        }
        
        m_spriteRenderer = GetComponent<SpriteRenderer>();
        sprite = m_spriteRenderer.sprite;
        m_worldTexture = sprite.texture;
        
        int len = chunkSize.x * chunkSize.y;
        
        m_particles = new Particle[len];
        m_chuckColour = new Color[len];
        
        Array.Clear(m_particles, 0, len);

        m_mainCamera = Camera.main;
        if (m_mainCamera == null)
        {
            return;
        }
        
        Bounds bounds = m_spriteRenderer.bounds;
        Vector3 boundsMin = bounds.min;
        Vector3 boundsSize = bounds.size;
        
        float screenAspect = (float)Screen.width / Screen.height;
        float cameraHeight = m_mainCamera.orthographicSize * 2;
        Bounds cameraBounds =  new Bounds(
            m_mainCamera.transform.position,
            new Vector3(cameraHeight * screenAspect, cameraHeight, 0));
        
        float invTexW = 1f / m_worldTexture.width;
        float invTexH = 1f / m_worldTexture.height;

        for (int i = 0; i < m_particles.Length; i++)
        {
            m_particles[i] = new Particle();
        }
        
        for(int y = 0; y < chunkSize.y; y++)
        {
            int yIndex = y + m_chunkPosition.y * chunkSize.y;
            for (int x = 0; x < chunkSize.y; x++)
            {
                int xIndex = x + m_chunkPosition.x * chunkSize.x;
                int index = x + y * chunkSize.x;
                

                Particle particle = m_particles[index];
                particle.index = index;
                particle.chunkId = m_chunkId;
                particle.positionX = xIndex;
                particle.positionY = yIndex;
                particle.localPositionX = x;
                particle.localPositionY = y;
                particle.type = ParticleType.Air;
                particle.colour = new Color32(255, 255, 255, 255);
                
                m_chuckColour[index] = particle.colour;
                
                float xRatio = x * invTexW;
                float yRatio = y * invTexH;
                
                float wx = boundsMin.x + (boundsSize.x * xRatio);
                float wy = boundsMin.y + (boundsSize.y * yRatio);

                if (wx < cameraBounds.min.x || wx > cameraBounds.max.x ||
                    wy < cameraBounds.min.y || wy > cameraBounds.max.y)
                {
                    particle.type = ParticleType.Air;
                    particle.colour = new Color32(255, 0, 0, 255);
                    m_chuckColour[index] = particle.colour;
                }
                
                m_particles[index] = particle;
            } 
        }
        
        UpdateTexture();
    }

    private Particle GetParticleAtIndex(int x, int y)
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
        return particle.type != ParticleType.Air;
    }
    
    public bool ContainsParticle(int index)
    {
        Particle particle = GetParticleAtIndex(index);
        return particle.type != ParticleType.Air;
    }

    public void AddParticle(ParticleType type, Vector2Int particlePos, int id = 0)
    {
        int index = particlePos.x + particlePos.y * m_chunkSize.x;
        Particle particle = m_particles[index];
        particle.type = type;
        particle.updated = 1;
        particle.id = id;
        m_particles[index] = particle;
    }
    
    
    public void AddParticle(ParticleType type, int index, int id = 0)
    {
        Particle particle = m_particles[index];
        particle.type = type;
        particle.updated = 1;
        particle.id = id;
        m_particles[index] = particle;
    }

    public void DrawPixel(Color[] color)
    {
        m_worldTexture.SetPixels(color);
        m_worldTexture.Apply();
    }

    public void UpdateTexture()
    {
        for (int i = 0; i < m_particles.Length; i++)
        {

            ParticleData particleData = ParticleManager.GetParticleData(m_particles[i].type);
            if (m_particles[i].type != ParticleType.Air)
            {
                m_chuckColour[i] = particleData.colour;
            }
            else
            {
                m_chuckColour[i] = Color.white;
            }


            if (m_particles[i].updated == 1)
            {
                Particle particle = m_particles[i];
                particle.updated = 0;
                m_particles[i] = particle;
            }
        }
        
        m_worldTexture.SetPixels(m_chuckColour);
        if (m_worldTexture != null && m_worldTexture.isReadable)
            m_worldTexture.Apply(false);
    }

    public Particle[] GetParticles()
    {
        return m_particles;
    }

    public void SetParticleUpdated(int particlePositionX, int particlePositionY, byte value)
    {
        int index = particlePositionX + particlePositionY * m_chunkSize.x;
        Particle particle = m_particles[index];
        particle.updated = value;
        m_particles[index] = particle;
    }
}
