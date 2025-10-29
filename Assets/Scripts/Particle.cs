using System;
using UnityEngine;

[System.Serializable]
public struct Particle
{
    public int chunkId;
    public int index;
    
    public int positionX;
    public int positionY;
    
    public int localPositionX;
    public int localPositionY;
    
    public ParticleType type;
    public ParticleFlags flags;
    
    public Color color;
    
    public Vector2Int Position => new Vector2Int(positionX, positionY);
    public ParticleType ParticleType => type;
    public int Index => index;

    public Particle Default()
    {
        return new Particle()
        {
            chunkId = chunkId,
            index = index,
            positionX = positionX,
            positionY = positionY,
            localPositionX = localPositionX,
            localPositionY = localPositionY,
            type = type,
        };
    }

    [Flags]
    public enum ParticleFlags
    {
        None = 0,
        Update = 1 << 0
    }
    
    public enum MovementType
    {
        Replace,
        Swap,
    }
}


