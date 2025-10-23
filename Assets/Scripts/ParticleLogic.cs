using System;
using System.Runtime.CompilerServices;
using Azen.Logger;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class ParticleLogic : MonoBehaviour
{
    [FormerlySerializedAs("_selectedType")] [SerializeField] private ParticleType m_selectedType = ParticleType.Sand;
    private KeyCode[] inputKeys;
    private WorldManager _worldManager;
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

        _worldManager = worldManager;
        m_chunks = _worldManager.GetAllChunks();
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
        if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(1))
        {
            Vector2 mouseWorldPosition = m_camera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int pixelPos = GetWorldPos(mouseWorldPosition);
            
            TryAddParticleAtPosition(pixelPos);
        }  
    }
    
    private void TryAddParticleAtPosition(Vector2Int pixelPos)
    {
        if (CheckPositionBounds(pixelPos.x, pixelPos.y) ==  false) return;
        
        WorldChunk chunk = _worldManager.GetChunkFromParticlePosition(pixelPos.x, pixelPos.y);
        int pixelPositionX = pixelPos.x % _worldManager.chunkSize.x;
        int pixelPositionY = pixelPos.y % _worldManager.chunkSize.y;
        int index = pixelPositionX + pixelPositionY * _worldManager.chunkSize.x;
        
        // Claim Index Regardless
        chunk.ClaimParticle(index, m_selectedType);
        chunk.IsActiveNextFrame = true;
    }
    
    private void ProcessChunks()
    {
        // === Loop Through All Chunks ===
        for (int i = 0; i < m_chunks.Length; i++)
        {
            WorldChunk chunk = m_chunks[i];
            CustomLogger.Log("Chunk is Active -> " + m_chunks[i].chunkActive, CustomLogger.LogCategory.WorldChunk);
            if (chunk == null || !chunk.chunkActive) continue;

            //=== Update Each Particle ===
            // CustomLogger.Log("Processing Chunk: " + i, CustomLogger.LogCategory.ParticleLogic);
            var particles = chunk.GetParticles();
            for (int j = 0; j < particles.Length; j++)
            {
                //=== Move Particle | Wake Neighbour Chunks if Required | Write Particle To Buffer. ===
                UpdateParticle(particles[j]);
            }

            chunk.chunkActive = false;
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
            chunk.ClearClaims();

            chunk.chunkActive = chunk.isActiveNextFrame;
            chunk.IsActiveNextFrame = false;
        }
    }
    
    private void UpdateParticle(Particle particle)
    {
        ParticleType particleType = particle.type;
        ParticleMovement[] movements = GetParticleMovements(particleType);

        //TODO : Instead of Move Particle, Add A Particle variable called Processed Particle and Assign it to back then write it.
        Vector2Int dir = Vector2Int.zero;
        bool particleMoved = false;
        foreach (ParticleMovement movement in movements)
        {
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
            
            particleMoved = TryMoveParticleInDirection(particle, dir, movement.distance);
            if (particleMoved)
            {
                TryActivateNeighbourChunk(particle);
                break;
            }
        }

        // === If Particle has not moved, Let Current Particle Try Claim index ===
        if (!particleMoved)
        {
            CustomLogger.Log("Particle has not moved", CustomLogger.LogCategory.ParticleLogic);
            WorldChunk chunk = _worldManager.GetChunk(particle.chunkId);
            int chunkID = chunk.ChunkId;
            int particleIndex = particle.index;
            
            TryClaimParticle(chunkID, particleIndex, particle.type);
        }
    }
    
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
        WorldChunk chunk = _worldManager.GetChunk(particle.chunkId);
        chunk.IsActiveNextFrame = true;

        int x = particle.localPositionX;
        int y = particle.localPositionY;

        // === If particle has been updated in the border. Activate its neighbor chunk ===
        if (x == 0 || y == 0 || x == _worldManager.worldSize.x - 1 || y == _worldManager.worldSize.y - 1)
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
        CustomLogger.Log("Trying to move particle in direction: " + dir + " distance: " + distance, CustomLogger.LogCategory.ParticleLogic);
        
        bool movedAtLeastOneCell = false;
        
        int startX = particle.positionX;
        int startY = particle.positionY;
        
        for (int step = 1; step <= distance; step++)
        {
            //Neighbour Position;
            int nx = startX + dir.x * step;
            int ny = startY + dir.y * step;

            if (!CheckPositionBounds(nx, ny))
            {
                break;
            }
            
            Particle targetParticle = _worldManager.GetParticle(nx, ny);
            int targetChunkId = targetParticle.chunkId;
            int targetIndex   = targetParticle.index;

            if (!TryClaimParticle(targetChunkId, targetIndex, particle.type))
            {
                CustomLogger.Log("Cannot Claim Particle", CustomLogger.LogCategory.ParticleLogic);
                break;
            }

            movedAtLeastOneCell = true;
        }

        return movedAtLeastOneCell;
    }
    
    private bool TryClaimParticle(int chunkId, int index, ParticleType type)
    {
        ParticleType currentType = _worldManager.GetClaimedTypeByIndex(chunkId, index);
        
        if (!CheckResistanceInDirection(type, currentType))
            return false;
        
        WorldChunk chunk = _worldManager.GetChunk(chunkId);
        chunk.ClaimParticle(index, type);
        return true;
    }

    private void WakeUpChunkInDir(Vector2Int position, Vector2Int direction)
    {
        Vector2Int chunkPosition = position + direction;
        WorldChunk chunk = _worldManager.GetChunk(chunkPosition.x, chunkPosition.y);
        if (chunk != null)
        {
            chunk.IsActiveNextFrame = true;
            return;
        }
    }

    private Border GetBorder(Particle particle)
    {
        Border border = Border.None;

        int width = _worldManager.chunkSize.x;
        int height = _worldManager.chunkSize.y;
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
    
    #endregion

    #region Particle Checks
    
    private bool CheckResistanceInDirection(ParticleType currentType, ParticleType neighbourType)
    {
        ParticleData particleData = GetParticleData(currentType);
        ParticleData neighbourParticleData = GetParticleData(neighbourType);
        float currentResistance = particleData.resistance;
        float neighbourResistance = neighbourParticleData.resistance;
        
        return currentResistance > neighbourResistance;
    }

    private bool CheckPositionBounds(int x, int y)
    {
        if (x < 0 || x >= _worldManager.worldSize.x || y < 0 || y >= _worldManager.worldSize.y)
        {
            return false;
        }

        return true;
    }

    #endregion

    #region ParicleHelpers

    private ParticleMovement[] GetParticleMovements(ParticleType type)
    {
        ParticleData data = ParticleManager.GetParticleData(type);
        ParticleMovement[] movements = data.movements;
        return movements == null ? Array.Empty<ParticleMovement>() : movements;
    }

    private void SetUpdated(Particle particle, byte value)
    {
        //WorldChunk chunk = _worldManager.GetChunkFromParticlePosition(particle.positionX, particle.positionY);
        WorldChunk chunk = _worldManager.GetChunk(particle.chunkId);
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
            (_worldManager.worldSize.x / _worldManager.chunkSize.x) - 1; // minus 1 because the index starts as one
        int maxIndexY = (_worldManager.worldSize.y / _worldManager.chunkSize.y) - 1;
        WorldChunk minChunk = _worldManager.GetChunk(0, 0);
        WorldChunk maxChunk = _worldManager.GetChunk(maxIndexX, maxIndexY);

        Renderer minSprite = minChunk.Renderer; // to get the bounds in world space;
        Renderer maxSprite = maxChunk.Renderer; // to get the bounds in world space;

        float xMin = minSprite.bounds.min.x;
        float yMin = minSprite.bounds.min.y;
        float xMax = maxSprite.bounds.max.x;
        float yMax = maxSprite.bounds.max.y;

        float xOldRange = xMax - xMin;
        float yOldRange = yMax - yMin;
        float xNewRange = _worldManager.worldSize.x;
        float yNewRange = _worldManager.worldSize.y;

        int xPixelPos = (int)((pos.x - xMin) * xNewRange / xOldRange);
        int yPixelPos = (int)((pos.y - yMin) * yNewRange / yOldRange);

        return new Vector2Int(xPixelPos, yPixelPos);
    }

    #endregion
}