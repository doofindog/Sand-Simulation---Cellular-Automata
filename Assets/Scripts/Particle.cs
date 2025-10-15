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
    public byte updated;
    public Color32 colour;
    
    public Vector2Int Position => new Vector2Int(positionX, positionY);
    public ParticleType ParticleType => type;
    public bool Updated => updated == 1;
    public int Index => index;
}
