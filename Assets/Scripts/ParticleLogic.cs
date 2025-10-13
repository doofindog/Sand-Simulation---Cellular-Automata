using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public struct WorkData
{
    public int PositionX;
    public int PositionY;
    public ParticleType Type;
}

public class ParticleLogic : MonoBehaviour
{
    [SerializeField] private ParticleType _selectedType = ParticleType.Sand;
    private KeyCode[] inputKeys;
    private WorldManager _worldManager;
    private WorldChunk[] m_chunks;
    private List<WorkData> m_currentWork;
    private List<WorkData> m_nextWork;
    private Camera m_camera;
    private float m_timer;
    private float m_timerMax = 0.01f;

    [Header("=== Debugging ===")] 
    [SerializeField] private int m_chunkIndex;

    public void Init(WorldManager worldManager)
    {
        m_currentWork = new List<WorkData>();
        m_nextWork = new List<WorkData>();
        
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
        
        Vector2 mouseWorldPosition = m_camera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int pixelPos = GetWorldPos(mouseWorldPosition);
        WorldChunk debugChunk = _worldManager.GetChunkFromParticlePosition(pixelPos.x, pixelPos.y);
        if (debugChunk != null)
        {
            m_chunkIndex = debugChunk.ChunkId;
        }
        
        

        if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(1))
        {
            HandleOnMouseDown(Input.GetKey(KeyCode.LeftControl));
        }

        m_timer += Time.deltaTime;
        if (m_timer < m_timerMax)
            return;

        // TODO : Remove Comment. Commenting just to Test right now
        
        m_timer = 0;
        for (int i = 0; i < m_chunks.Length; i++)
        {
            WorldChunk chunk = m_chunks[i];
            if (chunk == null || !chunk.chunkActive)
                continue;
        
            var particles = chunk.GetParticles();
        
            for (int j = 0; j < particles.Length; j++)
            {
                var particle = particles[j];
                if (particle.type == ParticleType.Air)
                {
                    continue;
                }
        
                UpdateParticle(particle);
            }

            chunk.chunkActive = false;
        }
        
        foreach (WorldChunk chunk in m_chunks)
        {
            if (chunk == null || !chunk.isActiveNextFrame)
                continue;
            
            chunk.UpdateTexture();

            chunk.chunkActive = chunk.isActiveNextFrame;
            chunk.isActiveNextFrame = false;
        }
    }

    private int m_id = 0;
    private void HandleOnMouseDown(bool doubleSize)
    {
        Vector2 mouseWorldPosition = m_camera.ScreenToWorldPoint(Input.mousePosition);
        if (!doubleSize)
        {
            Vector2Int pixelPos = GetWorldPos(mouseWorldPosition);
            if (CheckPositionBounds(pixelPos.x, pixelPos.y))
            {
                AddParticle(pixelPos, ParticleType.Sand, m_id);
                m_id++;
            }
        }
        else
        {
            int xSize = 4;
            int ySize = 4;
            for (int x = -xSize / 2; x <= xSize; x++)
            {
                for (int y = -ySize / 2; y <= ySize; y++)
                {
                    Vector2Int pixelPos = GetWorldPos(mouseWorldPosition) + new Vector2Int(x, y);
                    if (CheckPositionBounds(pixelPos.x, pixelPos.y))
                    {
                        AddParticle(pixelPos, ParticleType.Sand);
                    }
                }
            }
        }
    }

    private void AddParticle(Vector2Int worldPosition, ParticleType type, int id = 0)
    {
        WorldChunk chunk = _worldManager.GetChunkFromParticlePosition(worldPosition.x, worldPosition.y);
        int pixelPositionX = worldPosition.x % _worldManager.chunkSize.x;
        int pixelPositionY = worldPosition.y % _worldManager.chunkSize.y;

        chunk.AddParticle(_selectedType, new Vector2Int(pixelPositionX, pixelPositionY), id);

        chunk.chunkActive = true;
    }

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

    private void UpdateParticle(Particle particle)
    {
        MoveParticle(particle);
    }

    private void MoveParticle(Particle particle)
    {
        
        ParticleType particleType = particle.type;
        ParticleMovement[] movements = GetParticleMovements(particleType);
        if (movements == null || movements.Length == 0)
        {
            return;
        }
        
        bool stopCheck = false;
        foreach (ParticleMovement movement in movements)
        {
            switch (movement.moveDir)
            {
                case ParticleMovement.MoveDirection.Down:
                {
                    stopCheck = TryMove(particle, new Vector2Int(0, -1), movement.distance);
                    break;
                }
                case ParticleMovement.MoveDirection.DownLeft:
                {
                    stopCheck = TryMove(particle, new Vector2Int(-1, -1), movement.distance);
                    break;
                }
                case ParticleMovement.MoveDirection.DownRight:
                {
                    stopCheck = TryMove(particle, new Vector2Int(1, -1), movement.distance);
                    break;
                }
                case ParticleMovement.MoveDirection.RandomDownDiagonal:
                {
                    int horizontalDirection = (Random.value < 0.5f) ? -1 : 1;
                    stopCheck = TryMove(particle, new Vector2Int(horizontalDirection, -1), movement.distance);
                    break;
                }
                case ParticleMovement.MoveDirection.RandomHorizontal:
                {
                    int horizontalDirection = (Random.value < 0.5f) ? -1 : 1;
                    stopCheck = TryMove(particle, new Vector2Int(horizontalDirection, 0), movement.distance);
                    break;
                }
                case ParticleMovement.MoveDirection.RandomUpDiagonal:
                {
                    int horizontalDirection = (Random.value < 0.5f) ? -1 : 1;
                    stopCheck = TryMove(particle, new Vector2Int(horizontalDirection, 1), movement.distance);
                    break;
                }
                case ParticleMovement.MoveDirection.Left:
                {
                    stopCheck = TryMove(particle, new Vector2Int(-1, 0), movement.distance);
                    break;
                }
                case ParticleMovement.MoveDirection.Right:
                {
                    stopCheck = TryMove(particle, new Vector2Int(1, 0), movement.distance);
                    break;
                }
                case ParticleMovement.MoveDirection.Up:
                {
                    stopCheck = TryMove(particle, new Vector2Int(0, 1), movement.distance);
                    break;
                }
            }

            if (stopCheck)
            {
                WorldChunk chunk = _worldManager.GetChunk(particle.chunkId);
                chunk.isActiveNextFrame = true;
                
                int x = particle.localPositionX;
                int y = particle.localPositionY;
                
                // If particle is on the world edge (any side)
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
                
                break;
            }
        }
    }

    private bool TryMove(Particle particle, Vector2Int dir, int distance)
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
            if (canMove)
            {
                particle = MoveParticleInDirection(particle, dir);
                particleMoved = true;
            }
            else
            {
                break;
            }
        }

        if (particleMoved)
        {


            SetUpdated(particle, 1);
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

        if (!CheckPositionBounds(neighbourPos.x, neighbourPos.y))
            return false;
        
        Particle neighbourParticle = _worldManager.GetParticle(neighbourPos.x, neighbourPos.y);

        return neighbourParticle.type != particle.type;
    }

    private Particle MoveParticleInDirection(Particle particle, Vector2Int dir)
    {
        int x = particle.positionX;
        int y = particle.positionY;
        Vector2Int neighbourPos = new Vector2Int(x, y) + dir;
        
        WorldChunk currentChunk = _worldManager.GetChunk(particle.chunkId);
        WorldChunk neighbourChunk = _worldManager.GetChunkFromParticlePosition(neighbourPos.x, neighbourPos.y);
        
        Particle neighbourParticle = _worldManager.GetParticle(neighbourPos.x, neighbourPos.y);
        
        neighbourChunk.AddParticle(particle.type, neighbourParticle.Index, particle.id);
        currentChunk.AddParticle(neighbourParticle.type, particle.Index, neighbourParticle.id);

        
        currentChunk.isActiveNextFrame = true;
        if (neighbourChunk != currentChunk)
        {
            neighbourChunk.isActiveNextFrame = true;
        }
        
        return neighbourParticle;
    }

    #endregion

    #region Resistance Check

    private bool ContainsParticle(int x, int y)
    {
        try
        {
            return _worldManager.ContainsParticle(x, y);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }

        return false;
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
}