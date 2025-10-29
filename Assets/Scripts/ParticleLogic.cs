using System;
using System.Runtime.CompilerServices;
using Azen.Logger;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class ParticleLogic : MonoBehaviour
{
    private static readonly Color NoneColor = new Color(0, 0, 0, 0);
    
    [FormerlySerializedAs("_selectedType")] [SerializeField] private ParticleType m_selectedType = ParticleType.Sand;
    private KeyCode[] inputKeys;
    private WorldManager m_worldManager;
    private WorldChunk[] m_chunks;
    private Camera m_camera;
    private float m_timer;
    [SerializeField] private float m_updateTime = 0.01f;
    private bool m_flip;
    private int m_nextParticleId;

    [Header("=== Debugging ===")] 
    [SerializeField] private int m_chunkIndex;
    [SerializeField] private bool m_debug_addParticleInCenter;

    public void Init(WorldManager worldManager)
    {
        inputKeys = new[]
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
            KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9
        };

        m_worldManager = worldManager;
        m_chunks = m_worldManager.GetAllChunks();
        m_camera = Camera.main;
    }

    public void Update()
    {
        if (inputKeys == null)
        {
            return;
        }
        
        SetSelectedType();
        ReadMouseInput();

        m_timer += Time.deltaTime;
        if (m_timer < m_updateTime)
            return;

        m_timer = 0;

        ProcessChunks();
        WriteChunk();
    }

    private void SetSelectedType()
    {
        for (int i = 0; i < inputKeys.Length; i++)
        {
            if (Input.GetKeyDown(inputKeys[i]))
            {
                ParticleData data = ParticleManager.GetParticleAtIndex(i);
                m_selectedType = data.particleType;
            }
        }
    }
    
    private void ReadMouseInput()
    {
        bool increaseSize = Input.GetKey(KeyCode.LeftShift);
        bool click = Input.GetMouseButton(0) || Input.GetMouseButtonDown(1);
        if (!click) return;

        Vector2 mouseWorldPosition = m_camera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int center = GetWorldPos(mouseWorldPosition);

        if (!CheckPositionBounds(center.x, center.y)) return;

        if (!increaseSize)
        {
            // Single pixel
            PlaceAtWorld(center.x, center.y, m_selectedType);
            return;
        }

        // Filled circle brush
        const int radius = 4;               // <- adjust if you want a bigger/smaller circle
        int r2 = radius * radius;

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy > r2) continue;

                int x = center.x + dx;
                int y = center.y + dy;

                if (!CheckPositionBounds(x, y)) continue;

                PlaceAtWorld(x, y, m_selectedType);
            }
        }
    }

// Helper that finds the right chunk/index for a world-space pixel and adds the element there.
    private void PlaceAtWorld(int worldX, int worldY, ParticleType type)
    {
        WorldChunk chunk = m_worldManager.GetChunkFromParticlePosition(worldX, worldY);
        if (chunk == null) return;

        int localX = worldX % m_worldManager.chunkSize.x;
        int localY = worldY % m_worldManager.chunkSize.y;
        int index = localX + localY * m_worldManager.chunkSize.x;

        Particle particle = chunk.GetParticleAtIndex(index);
        AddElement(particle, type);
    }
    
    private bool flip = false;
    private void ProcessChunks()
    {
        // === Loop Through All Chunks ===
        for (int i = 0; i < m_chunks.Length; i++)
        {
            WorldChunk chunk = m_chunks[i];
            if (chunk == null || !chunk.chunkActive) continue;

            var particles = chunk.GetParticles();

            if (!flip)
            {
                // Normal left-to-right iteration
                for (int j = 1; j < particles.Length; j += 2)
                    UpdateParticle(particles[j]);

                for (int j = 0; j < particles.Length; j += 2)
                    UpdateParticle(particles[j]);
            }
            else
            {
                // Flipped right-to-left iteration
                for (int j = particles.Length - 2; j >= 0; j -= 2)
                    UpdateParticle(particles[j]);

                for (int j = particles.Length - 1; j >= 0; j -= 2)
                    UpdateParticle(particles[j]);
            }

            chunk.chunkActive = false;
        }

        // Flip direction next frame
        flip = !flip;
    }

    
    private void UpdateParticle(Particle particle)
    {
        ParticleType particleType = particle.type;
        ParticleMovement[] movements = GetParticleMovements(particleType);
        if (movements == null)
        {
            return;
        }

        //TODO : Instead of Move Particle, Add A Particle variable called Processed Particle and Assign it to back then write it.
        Vector2Int dir = Vector2Int.zero;
        foreach (ParticleMovement movement in movements)
        {
            //CustomLogger.Log($"({particle.localPositionX}, {particle.localPositionY}) Particle Movement: " + movement.moveDir, CustomLogger.LogCategory.ParticleLogic);
            
            switch (movement.moveDir)
            {
                case ParticleMovement.MoveDirection.Up:
                    dir = Vector2Int.up;
                    break;
                case ParticleMovement.MoveDirection.Down:
                    dir = Vector2Int.down;
                    break;
                case ParticleMovement.MoveDirection.Left:
                    dir = Vector2Int.left;
                    break;
                case ParticleMovement.MoveDirection.Right:
                    dir = Vector2Int.right;
                    break;
                case ParticleMovement.MoveDirection.DownLeft:
                    dir = new Vector2Int(-1, -1);
                    break;
                case ParticleMovement.MoveDirection.DownRight:
                    dir = new Vector2Int(1, -1);
                    break;
                case ParticleMovement.MoveDirection.RandomDownDiagonal:
                    dir = new Vector2Int(Random.value < 0.5f ? -1 : 1, -1);
                    break;
                case ParticleMovement.MoveDirection.RandomHorizontal:
                    dir = new Vector2Int((Random.value < 0.5f) ? -1 : 1, 0);
                    break;
                case ParticleMovement.MoveDirection.RandomUpDiagonal:
                    dir = new Vector2Int((Random.value < 0.5f) ? -1 : 1, 1);
                    break;
            }

            if (TryMoveParticleInDirection(particle, dir, movement.distance))
            {
                //TryActivateNeighbourChunk(particle);
                break;
            }
        }
    }

    private void WriteChunk()
    {
        foreach (WorldChunk chunk in m_chunks)
        {
            if (chunk == null || !chunk.isActiveNextFrame) continue;
            
            //Swap Read and Write Buffers and Update Texture.
            chunk.SwapReadWrite();
            chunk.UpdateTexture();

            //Clear Write Buffer.
            chunk.ClearParticles();

            chunk.chunkActive = chunk.isActiveNextFrame;
            chunk.IsActiveNextFrame = false;
        }
    }
    
    
    
    
    
    //---------------------------------------------------------------------------------//
    
    
    
    
    
    #region Movement Logic
    
    [System.Flags]
    private enum Border : byte 
    {
        None = 0,
        Top = 1 << 0,
        Bottom = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
        TopLeft = 1 << 4,
        TopRight = 1 << 5,
        BottomLeft = 1 << 6,
        BottomRight = 1 << 7,
    }
    
    private void TryActivateNeighbourChunk(Particle particle)
    {
        WorldChunk chunk = m_worldManager.GetChunk(particle.chunkId);
        chunk.IsActiveNextFrame = true;

        int x = particle.localPositionX;
        int y = particle.localPositionY;

        // === If particle has been updated in the border. Activate its neighbor chunk ===
        if (x == 0 || y == 0 || x == m_worldManager.worldSize.x - 1 || y == m_worldManager.worldSize.y - 1)
        {
            Border border = GetBorder(particle);

            // Wake up adjacent chunks depending on which borders the particle touched
            if ((border & Border.Top) == Border.Top) WakeUpChunkInDir(chunk.ChunkPosition, Vector2Int.up);

            if ((border & Border.Bottom) == Border.Bottom) WakeUpChunkInDir(chunk.ChunkPosition, Vector2Int.down);

            if ((border & Border.Left) == Border.Left) WakeUpChunkInDir(chunk.ChunkPosition, Vector2Int.left);

            if ((border & Border.Right) == Border.Right) WakeUpChunkInDir(chunk.ChunkPosition, Vector2Int.right);

            // Diagonals — combine both axis directions
            if ((border & Border.TopLeft) == Border.TopLeft)
                WakeUpChunkInDir(chunk.ChunkPosition, new Vector2Int(-1, 1));

            if ((border & Border.TopRight) == Border.TopRight)
                WakeUpChunkInDir(chunk.ChunkPosition, new Vector2Int(1, 1));

            if ((border & Border.BottomLeft) == Border.BottomLeft)
                WakeUpChunkInDir(chunk.ChunkPosition, new Vector2Int(-1, -1));

            if ((border & Border.BottomRight) == Border.BottomRight)
                WakeUpChunkInDir(chunk.ChunkPosition, new Vector2Int(1, -1));
        }
    }

    private bool TryMoveParticleInDirection(Particle particle, Vector2Int dir, int distance)
    {
        //CustomLogger.Log("Trying to move particle in direction: " + dir + " distance: " + distance, CustomLogger.LogCategory.ParticleLogic);
        int startX = particle.positionX;
        int startY = particle.positionY;
        
        int nx = startX + dir.x * distance;
        int ny = startY + dir.y * distance;
        

   
        if (CheckPositionBounds(nx, ny))
        {
            //CustomLogger.Log("Position Bounds Check Passed", CustomLogger.LogCategory.ParticleLogic);
            
            Profiler.BeginSample("Start Particle Movement Check");
            WorldChunk targetChunk = m_worldManager.GetChunkFromParticlePosition(nx, ny);
        
            int pixelPositionX = nx % WorldManager.ChunkSizeX;
            int pixelPositionY = ny % WorldManager.ChunkSizeY;;
            int index = pixelPositionX + pixelPositionY * WorldManager.ChunkSizeX;
            bool targetFlagged = targetChunk.GetFlag(index);
            
            Particle targetParticle = targetFlagged ? targetChunk.GetWriteParticleAtIndex(index) : targetChunk.GetParticleAtIndex(index);
            ParticleType targetType = targetParticle.type;
            Profiler.EndSample();
            if (CheckResistance(particle.type, targetType))
            {
                //CustomLogger.Log($"({particle.localPositionX}, {particle.localPositionY}) : Resistance Check Passed", CustomLogger.LogCategory.ParticleLogic);
                SwapParticle(particle, targetParticle);
                return true;
            }
            
            //CustomLogger.Log($"({particle.localPositionX}, {particle.localPositionY}) -> {dir} : Resistance Check Failed", CustomLogger.LogCategory.ParticleLogic);
        }

      
        return false;
    }

    private void WakeUpChunkInDir(Vector2Int position, Vector2Int direction)
    {
        Vector2Int chunkPosition = position + direction;
        WorldChunk chunk = m_worldManager.GetChunk(chunkPosition.x, chunkPosition.y);
        if (chunk != null)
        {
            chunk.IsActiveNextFrame = true;
            return;
        }
    }

    private Border GetBorder(Particle particle)
    {
        Border border = Border.None;

        int width = m_worldManager.chunkSize.x;
        int height = m_worldManager.chunkSize.y;
        int x = particle.localPositionX;
        int y = particle.localPositionY;

        // Check Top
        if (y == 0) border |= Border.Top;

        // Check Bottom
        if (y == height - 1) border |= Border.Bottom;

        // Check Left
        if (x == 0) border |= Border.Left;

        // Check Right
        if (x == width - 1) border |= Border.Right;

        if ((x == 0 || y == 0)) border |= Border.TopLeft;

        if ((x == width - 1 || y == 0)) border |= Border.TopRight;

        if ((x == 0 || y == height - 1)) border |= Border.BottomLeft;

        if ((x == width - 1 || y == height - 1)) border |= Border.BottomRight;

        return border;
    }

    private void SwapParticle(Particle particleA, Particle particleB)
    {
        Profiler.BeginSample("Swap Particles");
        AddElement(particleA, particleB.type, particleB.color);
        AddElement(particleB, particleA.type, particleA.color);
        Profiler.EndSample();
    }


    private void AddElement(Particle particle, ParticleType type, Color color = default)
    {
        int chunkID = particle.chunkId;
        WorldChunk chunk = m_worldManager.GetChunk(chunkID);

        if (color == NoneColor)
        {
            ParticleData data = ParticleManager.GetParticleData(type);
            color = data.GetColor();
        }
        
        chunk.SetParticle(particle.index, type, color);
        chunk.IsActiveNextFrame = true;
    }
    
    #endregion

    #region Particle Checks
    
    private bool CheckResistance(ParticleType currentType, ParticleType neighbourType)
    {
        Profiler.BeginSample("Check Resistance");
        
        ParticleData particleData = GetParticleData(currentType);
        ParticleData neighbourParticleData = GetParticleData(neighbourType);
        float currentResistance = particleData.moveResistance;
        float neighbourResistance = neighbourParticleData.moveResistance;
        
        Profiler.EndSample();
        
        return currentResistance > neighbourResistance;
    }

    private bool CheckConsume(ParticleType currentType, ParticleType neighbourType)
    {
        ParticleData particleData = GetParticleData(currentType);
        ParticleData neighbourParticleData = GetParticleData(neighbourType);
        
        // float currentResistance = particleData.consumeValue;
        // float neighbourResistance = neighbourParticleData.consumeValue;
        
        //return currentResistance > neighbourResistance;
        return false;
    }

    private bool CheckPositionBounds(int x, int y)
    {
        if (x < 0 || x >= m_worldManager.worldSize.x || y < 0 || y >= m_worldManager.worldSize.y)
        {
            //CustomLogger.Log($"({x}, {y}) Position Bounds Check Failed", CustomLogger.LogCategory.ParticleLogic);
            return false;
        }

        return true;
    }

    #endregion

    #region ParicleHelpers

    private ParticleMovement[] GetParticleMovements(ParticleType type)
    {
        ParticleData data = ParticleManager.GetParticleData(type);
        return data.movements;
    }

    private void SetUpdated(Particle particle, byte value)
    {
        //WorldChunk chunk = _worldManager.GetChunkFromParticlePosition(particle.positionX, particle.positionY);
        WorldChunk chunk = m_worldManager.GetChunk(particle.chunkId);
        if (chunk == null) return;

        chunk.SetParticleUpdated(particle.localPositionX, particle.localPositionY, value);
    }

    private ParticleData GetParticleData(ParticleType type)
    {
        return ParticleManager.GetParticleData(type);
    }
    
    private Vector2Int GetWorldPos(Vector2 pos)
    {
        int maxIndexX =
            (m_worldManager.worldSize.x / m_worldManager.chunkSize.x) - 1; // minus 1 because the index starts as one
        int maxIndexY = (m_worldManager.worldSize.y / m_worldManager.chunkSize.y) - 1;
        WorldChunk minChunk = m_worldManager.GetChunk(0, 0);
        WorldChunk maxChunk = m_worldManager.GetChunk(maxIndexX, maxIndexY);

        Renderer minSprite = minChunk.Renderer; // to get the bounds in world space;
        Renderer maxSprite = maxChunk.Renderer; // to get the bounds in world space;

        float xMin = minSprite.bounds.min.x;
        float yMin = minSprite.bounds.min.y;
        float xMax = maxSprite.bounds.max.x;
        float yMax = maxSprite.bounds.max.y;

        float xOldRange = xMax - xMin;
        float yOldRange = yMax - yMin;
        float xNewRange = m_worldManager.worldSize.x;
        float yNewRange = m_worldManager.worldSize.y;

        int xPixelPos = (int)((pos.x - xMin) * xNewRange / xOldRange);
        int yPixelPos = (int)((pos.y - yMin) * yNewRange / yOldRange);

        return new Vector2Int(xPixelPos, yPixelPos);
    }

    #endregion
    
    public bool IsParticleFlagged(int x, int y)
    {
        WorldChunk chunk = m_worldManager.GetChunkFromParticlePosition(x, y);
        Vector2Int chunkSize = m_worldManager.chunkSize;
        
        int pixelPositionX = x % chunkSize.x;
        int pixelPositionY = y % chunkSize.y;
        int index = pixelPositionX + pixelPositionY * chunkSize.x;
        
        return chunk.GetFlag(index);
    }
    
    
    
}