using System;
using System.Collections.Generic;
using CubeUtils;
using UnityEngine;
using Color = CubeUtils.Color;

public class CubeSolver
{
    public enum State
    {
        WhiteCenter,
        WhiteCross,
        WhiteCorners,
        MiddleEdges,
        YellowCross,
        YellowCorners,
        TopCorners,
        TopEdges,
        Solved
    }

    public State CurrentState;
    private Transform _root;
    private List<Vector3Int> _sides;

    public CubeSolver()
    {
        CurrentState = State.WhiteCenter;
        _root = CubeBuilder.Root;
        _sides = new List<Vector3Int>
        {
            Vector3Int.forward,
            Vector3Int.back,
            Vector3Int.left,
            Vector3Int.right
        };
    }

    public List<Rotation> SolveStep()
    {
        var rotations = new List<Rotation>();
        
        switch (CurrentState)
        {
            case State.WhiteCenter:
                foreach (CubePart part in CubeBuilder.Parts)
                {
                    Dictionary<Vector3Int, Color> colorDict = part.GetSideColors();

                    if (part.IsCenter() && colorDict.ContainsValue(Color.White))
                    {
                        // Found white center
                        Vector3Int direction = new List<Vector3Int>(colorDict.Keys)[0];

                        if (direction.Equals(Vector3Int.down))
                        {
                            CurrentState = State.WhiteCross;
                            return SolveStep();
                        }

                        int degrees = _sides.Contains(direction) ? -90 : -180;
                        
                        rotations.Add(new Rotation(Direction.X, 0, degrees, InvertVector3Int(direction)));
                        
                        break;
                    }
                }
                break;
            default:
                return new List<Rotation>();
        }

        return rotations;
    }

    private static Vector3Int InvertVector3Int(Vector3Int inVec)
    {
        return new Vector3Int
        {
            x = inVec.x *= -1,
            y = inVec.y *= -1,
            z = inVec.z *= -1
        };
    }
}