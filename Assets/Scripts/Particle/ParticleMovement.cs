using System;
using UnityEngine;

[System.Serializable]
public class ParticleMovement
{
    public enum MoveDirection
    {
        None,
        Up,
        UpRight,
        Right,
        DownRight,
        Down,
        DownLeft,
        Left,
        UpLeft,
        RandomDownDiagonal,
        RandomUpDiagonal,
        RandomHorizontal,
    }

    public MoveDirection moveDir;
    public int distance;
    [Range(0,1)] public float chance;
}
