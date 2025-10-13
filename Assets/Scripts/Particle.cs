using System;
using UnityEngine;

[System.Serializable]
public class Particle
{
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

    public Particle DefaultParticle()
    {
        return new Particle()
        {
            positionX = positionX,
            positionY = positionY,
            
            localPositionX = localPositionX,
            localPositionY = localPositionY,
            
            type = ParticleType.Air,
            updated = 0,
            colour = new Color32(255,255,255,255)
        };
    }

    // public void Init(Vector2Int position,ParticleType type = ParticleType.Air)
    // {
    //     positionX = position.x;
    //     positionY = position.y;
    //     
    //     this.type = type;
    // }
    //
    // public void AddParticle(ParticleType type)
    // {
    //     this.type = type;
    // }
    //
    // public void RemoveParticleData()
    // {
    //     type = ParticleType.Air;
    // }
    //
    // public ParticleMovement[] GetMovements()
    // {
    //     return GetParticleData().movements;
    // }
    //
    //
    // public ParticleType GetParticleType()
    // {
    //     return type;
    // }
    //
    // public ParticleData GetParticleData()
    // {
    //     return ParticleManager.GetParticleData(type);
    // }
    //
    // public bool HasUpdated()
    // {
    //     return updated;
    // }
    //
    // public void SetUpdated(bool value)
    // {
    //     updated = value;
    // }
}
