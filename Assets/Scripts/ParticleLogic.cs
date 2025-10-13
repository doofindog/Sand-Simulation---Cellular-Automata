using System;
using System.Collections;
using DrawXXL;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class ParticleLogic : MonoBehaviour
{
    [SerializeField] private ParticleType _selectedType = ParticleType.Sand;
    private KeyCode[] inputKeys;
    private WorldManager _worldManager;
    private WorldChunk[] m_chunks;
    private Camera m_camera;

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
        //StartCoroutine(UpdateChunks());
    }

    private float m_timer;
    private float m_timerMax = 0.01f;

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
            if (chunk == null || chunk.chunkActive == false)
            {
                continue;
            }
        
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
        }



        foreach (WorldChunk chunk in m_chunks)
        {
            if (chunk == null || chunk.chunkActive == false)
                continue;
            
            chunk.UpdateTexture();
            chunk.chunkActive = chunk.isActiveNextStep;
            chunk.isActiveNextStep = false;
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
                AddParticle(pixelPos, ParticleType.Sand);
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

    private void AddParticle(Vector2Int worldPosition, ParticleType type)
    {
        WorldChunk chunk = _worldManager.GetChunkFromParticlePosition(worldPosition.x, worldPosition.y);
        int pixelPositionX = worldPosition.x % _worldManager.chunkSize.x;
        int pixelPositionY = worldPosition.y % _worldManager.chunkSize.y;

        chunk.AddParticle(_selectedType, new Vector2Int(pixelPositionX, pixelPositionY));
    }

    private Vector2Int GetWorldPos(Vector2 pos)
    {
        int maxIndexX =
            (_worldManager.worldSize.x / _worldManager.chunkSize.x) -
            1; // minus 1 because the index starts as one
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
        if (particle.type == ParticleType.Air || particle.Updated)
        {
            return;
        }


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

        bool continueCheck = false;
        foreach (ParticleMovement movement in movements)
        {
            switch (movement.moveDir)
            {
                case ParticleMovement.MoveDirection.Down:
                {
                    continueCheck = TryMove(particle, new Vector2Int(0, -1), movement.distance);
                    break;
                }
                case ParticleMovement.MoveDirection.DownLeft:
                {
                    continueCheck = TryMove(particle, new Vector2Int(-1, -1), movement.distance);
                    break;
                }
                case ParticleMovement.MoveDirection.DownRight:
                {
                    continueCheck = TryMove(particle, new Vector2Int(1, -1), movement.distance);
                    break;
                }
                case ParticleMovement.MoveDirection.RandomDownDiagonal:
                {
                    int horizontalDirection = Random.Range(-1, 2);
                    continueCheck = TryMove(particle, new Vector2Int(horizontalDirection, -1), movement.distance);
                    break;
                }
                case ParticleMovement.MoveDirection.RandomHorizontal:
                {
                    int horizontalDirection = Random.Range(-1, 2);
                    continueCheck = TryMove(particle, new Vector2Int(horizontalDirection, 0), movement.distance);
                    break;
                }
                case ParticleMovement.MoveDirection.RandomUpDiagonal:
                {
                    int horizontalDirection = Random.Range(-1, 2);
                    continueCheck = TryMove(particle, new Vector2Int(horizontalDirection, 1), movement.distance);
                    break;
                }
                case ParticleMovement.MoveDirection.Left:
                {
                    continueCheck = TryMove(particle, new Vector2Int(-1, 0), movement.distance);
                    break;
                }
                case ParticleMovement.MoveDirection.Right:
                {
                    continueCheck = TryMove(particle, new Vector2Int(1, 0), movement.distance);
                    break;
                }
                case ParticleMovement.MoveDirection.Up:
                {
                    continueCheck = TryMove(particle, new Vector2Int(0, 1), movement.distance);
                    break;
                }
            }

            if (continueCheck)
            {
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
            if (CheckMoveInDirection(particle, dir))
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

    #region Movement Logic

    private bool CheckMoveInDirection(Particle particle, Vector2Int dir)
    {
        int x = particle.positionX;
        int y = particle.positionY;
        Vector2Int neighbourPos = new Vector2Int(x, y) + (dir);
        if (!CheckPositionBounds(neighbourPos.x, neighbourPos.y))
        {
            return false;
        }

        if (ContainsParticle(neighbourPos.x, neighbourPos.y))
        {
            return false;
        }

        return true;
    }

    private Particle MoveParticleInDirection(Particle particle, Vector2Int dir)
    {
        int x = particle.positionX;
        int y = particle.positionY;
        Vector2Int neighbourPos = new Vector2Int(x, y) + dir;
        WorldChunk currentChunk = _worldManager.GetChunkFromParticlePosition(neighbourPos.x, neighbourPos.y);
        WorldChunk neighbourChunk = _worldManager.GetChunkFromParticlePosition(neighbourPos.x, neighbourPos.y);
        Particle neighbourParticle = _worldManager.GetParticle(neighbourPos.x, neighbourPos.y);
        
        Vector2Int chunkSize = _worldManager.chunkSize;
        int neighbourPositionX = neighbourParticle.positionX % chunkSize.x;
        int neighbourPositionY = neighbourParticle.positionY % chunkSize.y;
        
        int currentPositionX = particle.positionX % chunkSize.x;
        int currentPositionY = particle.positionY % chunkSize.y;
        
        Vector2Int neighbourPosition = new Vector2Int(neighbourPositionX, neighbourPositionY);
        Vector2Int currentPosition = new Vector2Int(currentPositionX, currentPositionY);
        neighbourChunk.AddParticle(particle.type, neighbourPosition);
        currentChunk.AddParticle(ParticleType.Air, currentPosition);

        neighbourChunk.chunkActive = true;
        neighbourChunk.isActiveNextStep = true;
        
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
        chunk.isActiveNextStep = true;
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

    public void AddParticle(Particle particle, ParticleType type)
    {
        particle.type = type;
    }

    public void ResetParticle(Particle particle)
    {
        AddParticle(particle, ParticleType.Air);
    }

    public ParticleMovement[] GetParticleMovements(ParticleType type)
    {
        ParticleData data = ParticleManager.GetParticleData(type);
        return data.movements;
    }

    // public void SetUpdated(Particle particle, bool value)
    // {
    //     particle.updated = value;
    // }
    
    public void SetUpdated(Particle particle, byte value)
    {
        WorldChunk chunk = _worldManager.GetChunkFromParticlePosition(particle.positionX, particle.positionY);
        chunk.SetParticleUpdated(particle.localPositionX, particle.localPositionY, value);
    }

    private ParticleData GetParticleData(ParticleType type)
    {
        return ParticleManager.GetParticleData(type);
    }
    
#endregion
}