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
                            // White center in position
                            CurrentState = State.WhiteCross;
                            return SolveStep();
                        }

                        // Rotate to down center
                        int degrees = _sides.Contains(direction) ? -90 : -180;
                        
                        rotations.Add(new Rotation(Direction.X, 0, degrees, direction));

                        return rotations;
                    }
                }
                throw new SystemException();
            
            case State.WhiteCross:
                var numEdgesGood = 0;

                // Order top to bottom
                CubeBuilder.Parts.Sort((a, b) => b.GetPosition().y.CompareTo(a.GetPosition().y));
                
                foreach (CubePart part in CubeBuilder.Parts)
                {
                    Dictionary<Vector3Int, Color> colorDict = part.GetSideColors();

                    if (part.IsEdge() && colorDict.ContainsValue(Color.White))
                    {
                        // Found white edge
                        Vector3Int whiteDirection = default;
                        Vector3Int otherDirection = default;
                        
                        foreach (KeyValuePair<Vector3Int,Color> pair in colorDict)
                        {
                            if (pair.Value == Color.White)
                                whiteDirection = pair.Key;
                            else
                                otherDirection = pair.Key;
                        }

                        if (whiteDirection.Equals(Vector3Int.down) || whiteDirection.Equals(Vector3Int.up))
                        {
                            int edgesGoodBefore = numEdgesGood;
                            
                            // Find corresponding center piece
                            foreach (CubeSlice slice in CubeSlice.GetSlices(part))
                            {
                                if (otherDirection.Equals(slice.GetDirection()))
                                {
                                    Color centerColor = slice.GetCenter().GetSideColors()[otherDirection];
                                    
                                    // Compare edge's other color with center color
                                    if (centerColor == colorDict[otherDirection])
                                    {
                                        // Edge in good slice, orient if needed
                                        if (whiteDirection.Equals(Vector3Int.up))
                                        {
                                            rotations.Add(new Rotation(Direction.Z, -1, 180, otherDirection));
                                            return rotations;
                                        }
                                        
                                        numEdgesGood++;
                                        break;
                                    }

                                    // Edge in wrong slice, rotate up 
                                    if (whiteDirection.Equals(Vector3Int.down))
                                    {
                                        rotations.Add(new Rotation(Direction.Z, -1, 180, otherDirection));
                                        return rotations;
                                    }

                                    // or twist and orient
                                    foreach (Vector3Int side in _sides)
                                    {
                                        var otherSlice = new CubeSlice(side);
                                        CubePart otherCenter = otherSlice.GetCenter();

                                        if (otherCenter.GetSideColors()[side] == colorDict[otherDirection])
                                        {
                                            // Found matching side
                                            var upSlice = new CubeSlice(Direction.Y, 1);

                                            int degrees = upSlice.GetRotationDegrees(otherDirection, side);

                                            rotations.Add(new Rotation(upSlice, degrees, side));
                                            rotations.Add(new Rotation(otherSlice, 180, Vector3Int.back));
                                            return rotations;
                                        }
                                    }

                                    throw new SystemException();
                                }
                            }
                            
                            if (edgesGoodBefore == numEdgesGood)
                                throw new SystemException();
                        }
                        else if (part.GetPosition().y == -1)
                        {
                            // Lower layer but not facing down, so just put in top layer
                            rotations.Add(new Rotation(Direction.Z, -1, 180, whiteDirection));
                            return rotations;
                        }
                        else if (part.GetPosition().y == 1)
                        {
                            // Upper layer not facing up, so orient to face up
                            rotations.Add(new Rotation(Direction.Z, -1, -90, whiteDirection));
                            rotations.Add(new Rotation(Direction.X, 1, 90, whiteDirection));
                            rotations.Add(new Rotation(Direction.Y, 1, -90, whiteDirection));
                            rotations.Add(new Rotation(Direction.X, 1, -90, whiteDirection));
                            rotations.Add(new Rotation(Direction.Z, -1, 90, whiteDirection));
                            return rotations;
                        }
                        else
                        {
                            // In middle layer, put in top layer with white up
                            var sideSlice = new CubeSlice(otherDirection);
                            int degrees = sideSlice.GetRotationDegrees(whiteDirection, Vector3Int.up);

                            rotations.Add(new Rotation(sideSlice, degrees, Vector3Int.back));
                            rotations.Add(new Rotation(Direction.Y, 1, 90, Vector3Int.back));
                            rotations.Add(new Rotation(sideSlice, -degrees, Vector3Int.back));
                            return rotations;
                        }
                    }
                }

                if (numEdgesGood == 4)
                {
                    CurrentState = State.WhiteCorners;
                    return SolveStep();
                }

                throw new SystemException();
            default:
                return new List<Rotation>();
        }
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