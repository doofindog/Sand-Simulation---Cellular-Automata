using UnityEngine;
using UnityEngine.Serialization;


[CreateAssetMenu(fileName = "particle", menuName = "World Generation/Create Particle")]
[System.Serializable]
public class ParticleData : ScriptableObject
{
    [Tooltip("Type of Particle")]
    public ParticleType particleType;
    public Color colour;
    
    [Header("Movement")]
    [FormerlySerializedAs("moveChecks")] [SerializeField] public ParticleMovement[] movements;

    [Header("Passthrough")]
    public float resistance;
}


// === C# defaults the underlying type to int — that’s 4 bytes per value. ==
// === Changing it to byte reduces the memory footprint to 1 byte per value. ==
// === Will be helpful if we have millions of particles. ==
public enum ParticleType : byte
{
    Air = 0,
    Water = 1,
    Sand = 2,
    Wood = 3,
    Gas = 4
}

