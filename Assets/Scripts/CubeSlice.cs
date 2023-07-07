using System;
using System.Collections.Generic;
using CubeUtils;
using UnityEngine;

public class CubeSlice
{
    public readonly Direction Direction;
    public readonly int Position;

    public CubeSlice(Direction direction, int position)
    {
        Direction = direction;
        Position = position;
    }

    public void Rotate(int degrees)
    {
        Vector3 rotationAxis = Direction switch
        {
            Direction.X => Vector3.right,
            Direction.Y => Vector3.up,
            Direction.Z => Vector3.forward,
            _ => throw new ArgumentOutOfRangeException()
        };

        foreach (CubePart part in CubeBuilder.Parts)
        {
            Vector3Int partPos = part.GetPosition();
            int directionPos = Direction switch
            {
                Direction.X => partPos.x,
                Direction.Y => partPos.y,
                Direction.Z => partPos.z,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (directionPos == Position)
                part.transform.RotateAround(Vector3.zero, rotationAxis, degrees);
        }
    }

    // public void Rotate(CubePart from, CubePart to) 
    // {
    //     
    // }

    public static List<CubeSlice> GetSlices(CubePart part)
    {
        Vector3Int partPos = part.GetPosition();
        return new List<CubeSlice>
        {
            new(Direction.X, partPos.x),
            new(Direction.Y, partPos.y),
            new(Direction.Z, partPos.z)
        };
    }
}