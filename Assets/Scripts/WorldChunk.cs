using System;
using System.Collections;
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
    [SerializeField] private Particle[] m_mainGrid;
    [SerializeField] private Particle[] m_processGrid;
    [SerializeField] private BitArray m_flags;
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
            //CustomLogger.Log($"Setting IsActiveNextFrame to {value}", CustomLogger.LogCategory.WorldChunk);
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
        
        m_mainGrid = new Particle[len];
        m_processGrid = new Particle[len];
        m_flags = new BitArray(len, false);
        m_chuckColour = new Color[len];
        
        Array.Clear(m_mainGrid, 0, len);

        m_mainCamera = Camera.main;
        if (m_mainCamera == null)
        {
            return;
        }

        m_defaultColor = Color.white;
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
                    type = ParticleType.Air,
                    color = m_defaultColor
                };

                m_chuckColour[index] = m_defaultColor; 
                m_mainGrid[index] = particle;
                m_processGrid[index] = particle;
            } 
        }
        
        ClearParticles();
        UpdateTexture();
    }

    private Particle GetParticleAtIndex(int x, int y)
    {
        int index = x + y * m_chunkSize.x; 
        return m_mainGrid[index];
    }
    
    public Particle GetParticleAtIndex(int index)
    {
        return m_mainGrid[index];
    }
    
    public Particle GetWriteParticleAtIndex(int index)
    {
        return m_processGrid[index];
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
        for (int i = 0; i < m_mainGrid.Length; i++)
        {
            if(!m_flags[i])
                continue;
            
            m_chuckColour[i] = m_mainGrid[i].color;
        }
        
        m_worldTexture.SetPixels(m_chuckColour);
        if (m_worldTexture != null && m_worldTexture.isReadable)
        {
            m_worldTexture.Apply(false);
        }
    }

    public Particle[] GetParticles()
    {
        return m_mainGrid;
    }

    public void SetParticleUpdated(int particlePositionX, int particlePositionY, byte value)
    {
        int index = particlePositionX + particlePositionY * m_chunkSize.x;
        Particle particle = m_mainGrid[index];
        m_mainGrid[index] = particle;
    }

    public void ClearParticles()
    {
        for (int i = 0; i < m_processGrid.Length; i++)
        {
            m_processGrid[i] = m_processGrid[i].Default();
        }
        
        m_flags.SetAll(false);
    }

    public void SwapReadWrite()
    {
        for (int i = 0; i < m_mainGrid.Length; i++)
        {
            if (m_flags[i] == false)
            {
                continue;
            }
            
            m_mainGrid[i] = m_processGrid[i];
        }
        
    }

    public void SetParticle(int index, ParticleType type, Color color)
    {
        m_processGrid[index].type = type;
        m_processGrid[index].color = color;
        m_flags[index] = true;
    }

    public bool GetFlag(int index)
    {
        return m_flags[index];
    }
}
