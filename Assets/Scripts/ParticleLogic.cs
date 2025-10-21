using System;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class ParticleLogic : MonoBehaviour
{
    [SerializeField] private ParticleType _selectedType = ParticleType.Sand;
    private KeyCode[] inputKeys;
    private WorldManager _worldManager;
    private WorldChunk[] m_chunks;
    private Camera m_camera;
    private float m_timer;
    [SerializeField] private float m_updateTime = 0.01f;
    [SerializeField] private int m_iterationFrame = 1;
    private bool m_flip;

    [Header("=== Debugging ===")] 
    [SerializeField] private int m_chunkIndex;

    public void Init(WorldManager worldManager)
    {
        inputKeys = new[]
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9
        };
        
        _worldManager = worldManager;
        m_chunks = _worldManager.GetAllChunks();
        m_camera = Camera.main;
    }
    
    public void Update()
    {
        for (int i = 0; i < inputKeys.Length; i++)
        {
            if (Input.GetKeyDown(inputKeys[i]))
            {
                ParticleData data = ParticleManager.GetParticleAtIndex(i);
                _selectedType = data.particleType;
            }
        }
        
        m_timer += Time.deltaTime;
        if (m_timer < m_updateTime)
            return;
        
        m_timer = 0;

        
        for (int i = 0; i < m_chunks.Length; i++)
        {
            WorldChunk chunk = m_chunks[i];
            if (chunk == null || !chunk.chunkActive)
                continue;
            
            var particles = chunk.GetParticles();
            for (int j = 0; j < particles.Length; j++)
            {
                UpdateParticle(particles[j]);
            }
        }
        
        if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(1))
        {
            HandleOnMouseDown(Input.GetKey(KeyCode.LeftControl));
        }

        foreach (WorldChunk chunk in m_chunks)
        {
            if (chunk == null || !chunk.isActiveNextFrame)
                continue;
            
            chunk.SwapReadWrite();
            chunk.UpdateTexture();
            
            chunk.ClearWrite();

            chunk.chunkActive = chunk.isActiveNextFrame;
            chunk.isActiveNextFrame = false;
        }
    }
    
    private void HandleOnMouseDown(bool doubleSize)
    {
        Vector2 mouseWorldPosition = m_camera.ScreenToWorldPoint(Input.mousePosition);
        if (!doubleSize)
        {
            Vector2Int pixelPos = GetWorldPos(mouseWorldPosition);
            if (CheckPositionBounds(pixelPos.x, pixelPos.y))
            {
                AddParticle(pixelPos, _selectedType);
            }
        }
        else
        {
            int radius = 4; // brush radius in pixels (change as you like)

            Vector2Int center = GetWorldPos(mouseWorldPosition);

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    // inside the circle if dx^2 + dy^2 <= r^2
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        Vector2Int p = new Vector2Int(center.x + dx, center.y + dy);
                        if (CheckPositionBounds(p.x, p.y))
                            AddParticle(p, _selectedType);
                    }
                }
            }
        }
    }

    private void AddParticle(Vector2Int worldPosition, ParticleType type)
    {
        WorldChunk chunk = _worldManager.GetChunkFromParticlePosition(worldPosition.x, worldPosition.y);
        int pixelPositionX = worldPosition.x % _worldManager.chunkSize.x;
        int pixelPositionY = worldPosition.y % _worldManager.chunkSize.y;
        int index = pixelPositionX + pixelPositionY * _worldManager.chunkSize.x;
        
        chunk.AddParticle(index, type);

        chunk.isActiveNextFrame = true;
    }

    private void UpdateParticle(Particle particle)
    {
        ParticleType particleType = particle.type;
        ParticleMovement[] movements = GetParticleMovements(particleType);
        if (movements == null || movements.Length == 0)
        {
            return;
        }

        Vector2Int dir = Vector2Int.zero;
        
        if (movements.Length == 0)
        {
            WorldChunk chunk = _worldManager.GetChunk(particle.chunkId);
            chunk.WriteToParticleIndex(particle.index, particle);
        }
        else
        {
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
            
                var particleMoved = TryMoveParticleInDirection(particle, dir, movement.distance);
                if (particleMoved)
                {
                    TryActivateNeighbourChunk(particle);
                    break;
                }
            }
        }
        

    }

    private void TryActivateNeighbourChunk(Particle particle)
    {
        WorldChunk chunk = _worldManager.GetChunk(particle.chunkId);
        chunk.isActiveNextFrame = true;
                
        int x = particle.localPositionX;
        int y = particle.localPositionY;
                
        // === If particle has been updated in the border. Activate its neighbor chunk ===
        if (x == 0 || y == 0 || x == _worldManager.worldSize.x - 1 || y == _worldManager.worldSize.y - 1)
        {
            Border border = GetBorder(particle);
                    
            // Wake up adjacent chunks depending on which borders the particle touched
            if ((border & Border.Top) == Border.Top)
                WakeUpChunkInDir(chunk.ChunkPosition,Vector2Int.up);
                
            if ((border & Border.Bottom) == Border.Bottom)
                WakeUpChunkInDir(chunk.ChunkPosition, Vector2Int.down);
                
            if ((border & Border.Left) == Border.Left)
                WakeUpChunkInDir(chunk.ChunkPosition, Vector2Int.left);
                
            if ((border & Border.Right) == Border.Right)
                WakeUpChunkInDir(chunk.ChunkPosition, Vector2Int.right);
                
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
        // if (CheckResistanceInDirection(particle, dir))
        // {
        //     SwapParticleInDirection(particle, dir);
        //     return true;
        // }

        bool particleMoved = false;
        for (int i = 1; i <= distance; i++)
        {
            bool canMove = CheckMoveInDirection(particle, dir);
            if (!canMove)
            {
                break;
            }
            
            MoveParticleInDirection(particle, dir);
            particleMoved = true;
        }
        
        return particleMoved;
    }
    

    private void WakeUpChunkInDir(Vector2Int position, Vector2Int direction)
    {
        Vector2Int chunkPosition = position + direction;
        WorldChunk chunk = _worldManager.GetChunk(chunkPosition.x, chunkPosition.y);
        if (chunk != null)
        {
            chunk.isActiveNextFrame = true;
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
        if (y == 0 )
            border |= Border.Top;

        // Check Bottom
        if (y == height - 1)
            border |= Border.Bottom;

        // Check Left
        if (x == 0)
            border |= Border.Left;

        // Check Right
        if (x == width - 1)
            border |= Border.Right;
        
        if ((x == 0 || y == 0))
            border |= Border.TopLeft;

        if ((x == width - 1 || y == 0))
            border |= Border.TopRight;

        if ((x == 0 || y == height - 1) )
            border |= Border.BottomLeft;

        if ((x == width - 1 || y == height - 1))
            border |= Border.BottomRight;

        return border;
    }
    
    [System.Flags]
    private enum Border
    {
        None = 0,
        Top = 1 << 0,
        Bottom = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
        TopLeft      = 1 << 4,
        TopRight     = 1 << 5,
        BottomLeft   = 1 << 6,
        BottomRight  = 1 << 7,
    }

    #region Movement Logic

    private bool CheckMoveInDirection(Particle particle, Vector2Int dir)
    {
        int x = particle.positionX;
        int y = particle.positionY;
        Vector2Int neighbourPos = new Vector2Int(x, y) + (dir);
        //if (!CheckPositionBounds(neighbourPos.x, neighbourPos.y) || ContainsParticle(neighbourPos.x, neighbourPos.y)) return false;

        if (!CheckPositionBounds(neighbourPos.x, neighbourPos.y) || ContainsParticle(neighbourPos.x, neighbourPos.y))
            return false;
        
        Particle neighbourParticle = _worldManager.GetParticle(neighbourPos.x, neighbourPos.y);

        return neighbourParticle.type != particle.type;
    }

    private void MoveParticleInDirection(Particle particle, Vector2Int dir)
    {
        int x = particle.positionX;
        int y = particle.positionY;
        Vector2Int neighbourPos = new Vector2Int(x, y) + dir;
        
        WorldChunk currentChunk = _worldManager.GetChunk(particle.chunkId);
        WorldChunk neighbourChunk = _worldManager.GetChunkFromParticlePosition(neighbourPos.x, neighbourPos.y);
        
        Particle neighbourParticle = _worldManager.GetParticle(neighbourPos.x, neighbourPos.y);
        
        neighbourChunk.AddParticle(neighbourParticle.index, particle.type);
        currentChunk.AddParticle(particle.index, neighbourParticle.type);
    }

    #endregion

    #region Resistance Check

    private bool ContainsParticle(int x, int y)
    {
        int sizeX = _worldManager.worldSize.x;
        int sizeY = _worldManager.worldSize.y;
        
        WorldChunk chunk = _worldManager.GetChunkFromParticlePosition(x, y);
        if(chunk == null)
            return false;
        
        int pixelPositionX = x % sizeX;
        int pixelPositionY = y % sizeY;
        
        int index = pixelPositionX + pixelPositionY * sizeX;
            
        return chunk.ContainsParticle(index);
    }

    private bool CheckResistanceInDirection(Particle particle, Vector2Int dir)
    {
        Vector2Int neighbourPos = particle.Position + dir;

        if (!CheckPositionBounds(neighbourPos.x, neighbourPos.y))
        {
            return false;
        }

        Particle neighbourParticle = _worldManager.GetParticle(neighbourPos.x, neighbourPos.y);
        
        if (particle.type == neighbourParticle.type)
        {
            return false;
        }

        ParticleData particleData = GetParticleData(particle.type);
        ParticleData neighbourParticleData = GetParticleData(neighbourParticle.type);
        if (particleData.resistance > neighbourParticleData.resistance)
        {
            float chance = Random.Range(0.0f, 1.0f);
            return chance > 0.3f;
        }



        return false;
    }

    private void SwapParticleInDirection(Particle particle, Vector2Int dir)
    {
        Vector2Int neighbourPos = particle.Position + dir;
        WorldChunk chunk = _worldManager.GetChunkFromParticlePosition(neighbourPos.x, neighbourPos.y);
        Particle neighbourParticle = _worldManager.GetParticle(neighbourPos.x, neighbourPos.y);

        ParticleType particleType = neighbourParticle.ParticleType;
        // neighbourParticle.AddParticle(particle.ParticleType);
        // particle.AddParticle(particleType);
        chunk.chunkActive = true;
        chunk.isActiveNextFrame = true;
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

    public ParticleMovement[] GetParticleMovements(ParticleType type)
    {
        ParticleData data = ParticleManager.GetParticleData(type);
        return data.movements;
    }
    
    private void SetUpdated(Particle particle, byte value)
    {
        
        //WorldChunk chunk = _worldManager.GetChunkFromParticlePosition(particle.positionX, particle.positionY);
        WorldChunk chunk = _worldManager.GetChunk(particle.chunkId);
        if(chunk == null)
            return;
        
        chunk.SetParticleUpdated(particle.localPositionX, particle.localPositionY, value);
    }

    private ParticleData GetParticleData(ParticleType type)
    {
        return ParticleManager.GetParticleData(type);
    }
    
    #endregion

    private Vector2Int GetWorldPos(Vector2 pos)
    {
        int maxIndexX = (_worldManager.worldSize.x / _worldManager.chunkSize.x) - 1; // minus 1 because the index starts as one
        int maxIndexY = (_worldManager.worldSize.y / _worldManager.chunkSize.y) - 1;
        WorldChunk minChunk = _worldManager.GetChunk(0, 0);
        WorldChunk maxChunk = _worldManager.GetChunk(maxIndexX, maxIndexY);

        Renderer minSprite = minChunk.GetComponent<Renderer>(); // to get the bounds in world space;
        Renderer maxSprite = maxChunk.GetComponent<Renderer>(); // to get the bounds in world space;

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
}