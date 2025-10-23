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
    [SerializeField] private Dictionary<int, List<int>> m_claimIntents;
    private Color[] m_chuckColour;
    private Vector2Int m_chunkSize;
    private Camera m_mainCamera;
    
    //Public Variables
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    public Sprite sprite;
    public bool chunkActive;
    public bool isActiveNextFrame;

    public bool IsActiveNextFrame
    {
        get => isActiveNextFrame;
        set
        {
            CustomLogger.Log($"Setting IsActiveNextFrame to {value}", CustomLogger.LogCategory.WorldChunk);
            isActiveNextFrame = value;
        }
    }
    
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
        m_claimIntents = new Dictionary<int, List<int, int>>();
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
            } 
        }
        
        ClearClaims();
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

    public void ClearClaims()
    {
        m_claimIntents.Clear();
    }

    public void SwapReadWrite()
    {
        // // CustomLogger.Log("Swapping Read and Write", CustomLogger.LogCategory.WorldChunk);
        // for (int i = 0; i < m_readParticle.Length; i++)
        // {
        //     ref Particle particle = ref m_readParticle[i];
        //     particle.type = m_clamInfos[i];
        // }
    }

    public void ClaimParticle(int currentIndex, int targetIndex)
    {
        m_claimIntents[targetIndex] ??= new List<int>();
        m_claimIntents[targetIndex].Add(currentIndex);
    }

    public int[] GetClaimsAtIndex(int index)
    {
        return m_claimIntents[index].ToArray();
    }
}
