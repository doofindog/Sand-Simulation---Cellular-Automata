using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Azen.Logger;
using UnityEngine;

public class WorldChunk : MonoBehaviour
{
    //Private Variables
    private int m_chunkId;
    private Vector2Int m_chunkPosition;
    private Color m_defaultColor;
    private Texture2D m_worldTexture;
    [SerializeField] private Particle[] m_readParticle;
    [SerializeField] private Particle[] m_writeParticle;
    [SerializeField] private ParticleWriteInfo[] m_writeInfo;
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
    public SpriteRenderer Renderer => m_spriteRenderer;
    
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
        
        m_readParticle = new Particle[len];
        m_writeParticle = new Particle[len];
        m_writeInfo = new ParticleWriteInfo[len];
        m_chuckColour = new Color[len];
        
        Array.Clear(m_readParticle, 0, len);

        m_mainCamera = Camera.main;
        if (m_mainCamera == null)
        {
            return;
        }
        
        for(int y = 0; y < chunkSize.y; y++)
        {
            int yIndex = y + m_chunkPosition.y * chunkSize.y;
            for (int x = 0; x < chunkSize.y; x++)
            {
                int xIndex = x + m_chunkPosition.x * chunkSize.x;
                int index = x + y * chunkSize.x;
                

                Particle particle = new Particle
                {
                    index = index,
                    chunkId = m_chunkId,
                    positionX = xIndex,
                    positionY = yIndex,
                    localPositionX = x,
                    localPositionY = y,
                    type = ParticleType.Air
                };

                m_chuckColour[index] = particle.colour;
                m_readParticle[index] = particle;
                WriteToParticleIndex(index, particle);
            } 
        }
        
        UpdateTexture();
    }

    private Particle GetParticleAtIndex(int x, int y)
    {
        int index = x + y * m_chunkSize.x; 
        return m_readParticle[index];
    }
    
    public Particle GetParticleAtIndex(int index)
    {
        return m_readParticle[index];
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
    
    public void AddParticle(int index, ParticleType type, int id)
    {
        Particle particle = m_readParticle[index];
        particle.type = type;
        particle.id = id;
        
        WriteToParticleIndex(index, particle);
    }

    public void UpdateTexture()
    {
        for (int i = 0; i < m_readParticle.Length; i++)
        {
            ParticleData particleData = ParticleManager.GetParticleData(m_readParticle[i].type);
            m_chuckColour[i] = particleData.colour;
        }
        
        m_worldTexture.SetPixels(m_chuckColour);
        if (m_worldTexture != null && m_worldTexture.isReadable)
        {
            m_worldTexture.Apply(false);
        }
    }

    public Particle[] GetParticles()
    {
        return m_readParticle;
    }

    public void SetParticleUpdated(int particlePositionX, int particlePositionY, byte value)
    {
        int index = particlePositionX + particlePositionY * m_chunkSize.x;
        Particle particle = m_readParticle[index];
        particle.updated = value;
        m_readParticle[index] = particle;
    }

    public void ClearWrite()
    {
        for(int i = 0; i < m_writeParticle.Length; i++)
        {
            Particle particle = m_writeParticle[i];
            particle.type = ParticleType.Air;
            particle.id = 0;
            
            m_writeParticle[i] = particle;
        }
    }

    public void SwapReadWrite()
    {
        CustomLogger.Log("Swapping Read and Write", CustomLogger.LogCategory.WorldChunk);
        (m_readParticle, m_writeParticle) = (m_writeParticle, m_readParticle);

        for (int i = 0; i < m_readParticle.Length; i++)
        {
            int readIndex = m_readParticle[i].index;
            int writeIndex = m_writeParticle[i].index;
            
            int readX = m_readParticle[i].localPositionX;
            int readY = m_readParticle[i].localPositionY;
            
            int writeX = m_writeParticle[i].localPositionX;
            int writeY = m_writeParticle[i].localPositionY;
            
            ParticleType readType = m_readParticle[i].type;
            ParticleType writeType = m_writeParticle[i].type;
            
            int readId = m_readParticle[i].id;
            int writeId = m_writeParticle[i].id;
            
            CustomLogger.Log($"Swaping -> {readIndex} -> {writeIndex} | ({readX},{readY}) -> ({writeX},{writeY}) | {readType} -> {writeType} | {{readId}} -> {{writeId}}", CustomLogger.LogCategory.WorldChunk);
        }
    }

    public void WriteToParticleIndex(int index, Particle updatedParticle)
    {
        m_writeParticle[index] = updatedParticle;
        CustomLogger.Log($"Updating Particle {updatedParticle.index} | ({updatedParticle.localPositionX}, {updatedParticle.localPositionY}) | {updatedParticle.ParticleType}) ", CustomLogger.LogCategory.WorldChunk);
    }
}
