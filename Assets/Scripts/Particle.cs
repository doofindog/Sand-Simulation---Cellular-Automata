using UnityEngine;

[System.Serializable]
public class Particle
{
    [SerializeField] private Vector2Int m_position;
    [SerializeField] private ParticleType m_type;
    [SerializeField] private bool m_updated;
    
    public Vector2Int Position => m_position;

    public void Init(Vector2Int position,ParticleType type = ParticleType.Air)
    {
        m_position = position;
        m_type = type;
    }

    public void AddParticle(ParticleType type)
    {
        m_type = type;
    }

    public void RemoveParticleData()
    {
        m_type = ParticleType.Air;
    }

    public ParticleMovement[] GetMovements()
    {
        return GetParticleData().movements;
    }
    

    public ParticleType GetParticleType()
    {
        return m_type;
    }

    public ParticleData GetParticleData()
    {
        return ParticleManager.GetParticleData(m_type);
    }

    public bool HasUpdated()
    {
        return m_updated;
    }

    public void SetUpdated(bool value)
    {
        m_updated = value;
    }

    public WorldChunk GetChunk()
    {
        int chunkPositionX = m_position.x / WorldManager.instance.chunkSize.x;
        int chunkPositionY = m_position.y / WorldManager.instance.chunkSize.y;

        return WorldManager.instance.GetChunk(chunkPositionX, chunkPositionY);
    }
}
