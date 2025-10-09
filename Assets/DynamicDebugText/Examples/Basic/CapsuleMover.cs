
#region Using Statements
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#endregion

//Simple script to move capsules back and forth
public class CapsuleMover : MonoBehaviour {
    
    public enum MoveType {
        BackAndForth,
        Circle
    }

    public MoveType MovementType;
    public float Distance = 10;
    public float Speed = 5;
    
    private Vector3 startingPosition;
    private Vector3 offset;
    private void Start() { startingPosition = transform.position; }

    private void Update() {
        switch (MovementType) {
            case MoveType.Circle:
                offset = new Vector3(Mathf.Sin(Time.time * Speed), Mathf.Cos(Time.time * Speed), 0)*Distance;
                break;
            case MoveType.BackAndForth:
                offset = new Vector3(0, 0, Mathf.PingPong(Time.time * Speed, Distance) - Distance/2);
                break;
        }
        transform.position = startingPosition + offset;
    }
}