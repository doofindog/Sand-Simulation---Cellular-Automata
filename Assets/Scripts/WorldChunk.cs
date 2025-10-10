using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Profiling;

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
    private Camera m_mainCamera;
    
    //Public Variables
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    public Sprite sprite;
    public bool chunkActive;
    public bool isActiveNextStep;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureCapacity<T>(ref T[] arr, int len)
    {
        if (arr == null || arr.Length != len) arr = new T[len];
    }
    
    public void Init(Vector2Int chunkPosition,Vector2Int chunkSize)
    {
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
            Debug.LogError("No Main Camera Found, Please set main Camera");
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
                particle.Init(new Vector2Int(xIndex, yIndex));
                m_chuckColour[index] = new Color32(255, 255, 255, 255);
                
                //DrawPixel(new Vector2Int(x,y), Color.white);
                
                float xRatio = x * invTexW;
                float yRatio = y * invTexH;
                
                float wx = boundsMin.x + (boundsSize.x * xRatio);
                float wy = boundsMin.y + (boundsSize.y * yRatio);

                if (wx < cameraBounds.min.x || wx > cameraBounds.max.x ||
                    wy < cameraBounds.min.y || wy > cameraBounds.max.y)
                {
                    particle.AddParticle(ParticleType.Wood);
                    m_chuckColour[index] = new Color32(255, 0, 0, 255);
                }
            } 
        }
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

    public void DrawPixel(Color[] color)
    {
        m_worldTexture.SetPixels(color);
        m_worldTexture.Apply();
    }

    public void UpdateTexture()
    {

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
        if (m_worldTexture != null && m_worldTexture.isReadable)
            m_worldTexture.Apply(false);
    }

    public Particle[] GetParticles()
    {
        return m_particles;
    }
}
