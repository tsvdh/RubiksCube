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

    public CubeSlice(Vector3Int direction)
    {
        var total = 0;
        total += Math.Abs(direction.x);
        total += Math.Abs(direction.y);
        total += Math.Abs(direction.z);

        if (total != 1)
            throw new SystemException();
        
        if (direction.x != 0)
        {
            Direction = Direction.X;
            Position = direction.x;
        }
        else if (direction.y != 0)
        {
            Direction = Direction.Y;
            Position = direction.y;
        }
        else
        {
            Direction = Direction.Z;
            Position = direction.z;
        }
    }

    private List<CubePart> GetParts()
    {
        var parts = new List<CubePart>();

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
                parts.Add(part);
        }

        return parts;
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

        foreach (CubePart part in GetParts())
        {
            part.transform.RotateAround(Vector3.zero, rotationAxis, degrees);
        }
    }

    public CubePart GetCenter()
    {
        foreach (CubePart part in GetParts())
        {
            if (part.IsCenter())
                return part;
        }

        throw new SystemException();
    }

    public List<CubePart> GetEdges()
    {
        var edges = new List<CubePart>();

        foreach (CubePart part in GetParts())
        {
            if (part.IsEdge())
                edges.Add(part);
        }

        return edges;
    }
    
    public List<CubePart> GetCorners()
    {
        var edges = new List<CubePart>();

        foreach (CubePart part in GetParts())
        {
            if (part.IsCorner())
                edges.Add(part);
        }

        return edges;
    }

    public Vector3Int GetDirection()
    {
        Vector3Int direction = Direction switch
        {
            Direction.X => Vector3Int.right,
            Direction.Y => Vector3Int.up,
            Direction.Z => Vector3Int.forward,
            _ => throw new ArgumentOutOfRangeException()
        };

        direction.x *= Position;
        direction.y *= Position;
        direction.z *= Position;

        return direction;
    }

    public int GetRotationDegrees(CubePart from, CubePart to)
    {
        Vector3Int direction = Direction switch
        {
            Direction.X => Vector3Int.right,
            Direction.Y => Vector3Int.up,
            Direction.Z => Vector3Int.forward,
            _ => throw new ArgumentOutOfRangeException()
        };

        for (var i = 0; i < 4; i++)
        {
            int degrees = i * 90;

            from.transform.RotateAround(Vector3.zero, direction, degrees);

            bool positionEquals = from.GetPosition().Equals(to.GetPosition());

            from.transform.RotateAround(Vector3.zero, direction, -degrees);

            if (positionEquals)
            {
                // Don't rotate 270 but instead -90
                return degrees <= 180 ? degrees : -90;
            }
        }

        throw new SystemException();
    }

    public int GetRotationDegrees(Vector3Int dirFrom, Vector3Int dirTo)
    {
        CubePart a = default;
        CubePart b = default;

        foreach (CubePart edge in GetEdges())
        {
            Vector3Int direction = edge.GetPosition() - GetCenter().GetPosition();
            if (direction.Equals(dirFrom))
                a = edge;
            if (direction.Equals(dirTo))
                b = edge;
        }

        if (a == default || b == default)
            throw new SystemException();

        return GetRotationDegrees(a, b);
    }

    public List<CubePart> GetOverlappingParts(List<CubePart> otherParts)
    {
        List<CubePart> overlappingParts = GetParts();
        
        foreach (CubePart part in GetParts())
        {
            if (otherParts.Contains(part))
                overlappingParts.Add(part);
        }

        return overlappingParts;
    }
}