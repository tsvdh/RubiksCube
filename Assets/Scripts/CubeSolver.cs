using System;
using System.Collections.Generic;
using CubeUtils;
using UnityEngine;
using UnityEngine.UI;
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

                        // Rotate to down center
                        int degrees = _sides.Contains(direction) ? -90 : -180;
                        
                        rotations.Add(new Rotation(Direction.X, 0, degrees, direction));

                        return rotations;
                    }
                }
                throw new SystemException();
            
            case State.WhiteCross:
                // Order top to bottom
                CubeBuilder.Parts.Sort((a, b) => b.GetPosition().y.CompareTo(a.GetPosition().y));
                
                foreach (CubePart part in CubeBuilder.Parts)
                {
                    Dictionary<Vector3Int, Color> colorDict = part.GetSideColors();

                    if (!(part.IsEdge() && colorDict.ContainsValue(Color.White)))
                        continue;
                    
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

                    if (whiteDirection == default || otherDirection == default)
                        throw new SystemException();

                    if (whiteDirection.Equals(Vector3Int.down) || whiteDirection.Equals(Vector3Int.up))
                    {
                        // Find corresponding center piece
                        foreach (CubeSlice slice in part.GetSlices())
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

                throw new SystemException();
            
            case State.WhiteCorners:
                // Order top to bottom
                CubeBuilder.Parts.Sort((a, b) => b.GetPosition().y.CompareTo(a.GetPosition().y));

                foreach (CubePart part in CubeBuilder.Parts)
                {
                    Dictionary<Vector3Int, Color> colorDict = part.GetSideColors();

                    if (!(part.IsCorner() && colorDict.ContainsValue(Color.White)))
                        continue;

                    // Found white corner
                    (Vector3Int whiteDirection, _, List<Vector3Int> sides) = GetCornerDirections(part);

                    // Find wanted corner position
                    CubePart wantedUpCorner = GetCorrectCornerPosition(part);

                    Vector3Int otherSide;
                    int upperDegrees;

                    Vector3Int partPos = part.GetPosition();
                    Vector3Int wantedUpCornerPos = wantedUpCorner.GetPosition();
                    if (partPos.y == -1)
                    {
                        // In bottom slice
                        if (partPos.x == wantedUpCornerPos.x && partPos.z == wantedUpCornerPos.z
                            && partPos.y == -1)
                        {
                            // In right position
                            continue;
                        }
                        
                        // In wrong position, find twisting side and twist up
                        Vector3Int rotationSide = whiteDirection.Equals(Vector3Int.down)
                            ? sides[0]
                            : whiteDirection;
                        
                        sides.Remove(rotationSide);
                        otherSide = sides[0];

                        var rotationSlice = new CubeSlice(rotationSide);
                        
                        int sideDegrees = rotationSlice.GetRotationDegrees(otherSide, Vector3Int.up);
                        upperDegrees = new CubeSlice(Vector3Int.up).GetRotationDegrees(rotationSide, otherSide);
                        
                        rotations.Add(new Rotation(rotationSlice, sideDegrees, Vector3Int.back));
                        rotations.Add(new Rotation(Direction.Y, 1, upperDegrees, Vector3Int.back));
                        rotations.Add(new Rotation(rotationSlice, -sideDegrees, Vector3Int.back));
                        return rotations;
                    }

                    // In upper slice, rotate to wanted corner
                    var upSlice = new CubeSlice(Vector3Int.up);
                    int upperOrientationDegrees = upSlice.GetRotationDegrees(part, wantedUpCorner);
                    rotations.Add(new Rotation(upSlice, upperOrientationDegrees, Vector3Int.back));
                    
                    // Move view according to orientation
                    _root.RotateAround(Vector3.zero, Vector3.up, upperOrientationDegrees);

                    CubeSlice sideSlice;
                    int rotationSideDegrees;

                    if (whiteDirection.Equals(Vector3Int.up))
                    {
                        (Vector3Int _, List<Vector3Int> _, List<Vector3Int> newSides) = GetCornerDirections(part);
                        
                        // Orient white to side
                        sideSlice = new CubeSlice(newSides[0]);
                        rotationSideDegrees = sideSlice.GetRotationDegrees(newSides[1], Vector3Int.up);
                        upperDegrees = upSlice.GetRotationDegrees(newSides[0], newSides[1]);
                        
                        
                        rotations.Add(new Rotation(sideSlice, rotationSideDegrees, Vector3Int.back));
                        rotations.Add(new Rotation(upSlice, upperDegrees * 2, Vector3Int.back));
                        rotations.Add(new Rotation(sideSlice, -rotationSideDegrees, Vector3Int.back));
                        rotations.Add(new Rotation(upSlice, -upperDegrees, Vector3Int.back));
                    }
                    
                    // twist corner to correct position
                    (Vector3Int newWhiteDirection, List<Vector3Int> newOtherDirections, List<Vector3Int> _) = GetCornerDirections(part);
                    
                    newOtherDirections.RemoveAll(dir => dir.Equals(Vector3Int.up));
                    otherSide = newOtherDirections[0];
                    sideSlice = new CubeSlice(otherSide);

                    rotationSideDegrees = sideSlice.GetRotationDegrees(otherSide, Vector3Int.up);
                    upperDegrees = upSlice.GetRotationDegrees(newWhiteDirection, otherSide);
                    
                    rotations.Add(new Rotation(sideSlice, rotationSideDegrees, Vector3Int.back));
                    rotations.Add(new Rotation(upSlice, upperDegrees, Vector3Int.back));
                    rotations.Add(new Rotation(sideSlice, -rotationSideDegrees, Vector3Int.back));
                    
                    // Move view back
                    _root.RotateAround(Vector3.zero, Vector3.up, -upperOrientationDegrees);
                }

                throw new SystemException();
            default:
                return new List<Rotation>();
        }
    }

    public State CheckState()
    {
        switch (CurrentState)
        {
            case State.WhiteCenter:
                Color color = new CubeSlice(Vector3Int.down).GetCenter().GetSideColors()[Vector3Int.down];
                // Bottom center must be white
                if (color == Color.White)
                    CurrentState = State.WhiteCross;
                
                return CurrentState;
            
            case State.WhiteCross:
                foreach (Vector3Int side in _sides)
                {
                    var sideSlice = new CubeSlice(side);
                    Color centerColor = sideSlice.GetCenter().GetSideColors()[side];
                    foreach (CubePart edge in sideSlice.GetEdges())
                    {
                        if (edge.GetPosition().y != -1)
                            continue;
                        
                        // Examining lower edge
                        Dictionary<Vector3Int, Color> colorDict = edge.GetSideColors();

                        // Bottom edge must have white down and center color to side
                        if (colorDict[Vector3Int.down] != Color.White 
                            || colorDict[side] != centerColor)
                            return CurrentState;

                        break;
                    }
                }

                CurrentState = State.WhiteCorners;
                return CurrentState;
            
            case State.WhiteCorners:
                List<CubePart> corners = new CubeSlice(Vector3Int.down).GetCorners();
                foreach (CubePart part in corners)
                {
                    // Bottom corner must have a white part
                    if (!part.GetSideColors().ContainsValue(Color.White))
                        return CurrentState;
                    
                    Vector3Int partPos = part.GetPosition();
                    Vector3Int correctPos = GetCorrectCornerPosition(part).GetPosition();
                    
                    // Each bottom corner must have white down and have the right colors for the centers
                    if (part.GetSideColors()[Vector3Int.down] != Color.White
                        || !partPos.Equals(correctPos))
                        return CurrentState;
                }
                
                CurrentState = State.MiddleEdges;
                return CurrentState;
        }

        return State.Solved;
    }

    private static (Vector3Int, List<Vector3Int>, List<Vector3Int>) GetCornerDirections(CubePart part)
    {
        Dictionary<Vector3Int, Color> colorDict = part.GetSideColors();

        Vector3Int whiteDirection = default;
        var otherDirections = new List<Vector3Int>();

        foreach (KeyValuePair<Vector3Int, Color> pair in colorDict)
        {
            if (pair.Value == Color.White)
                whiteDirection = pair.Key;
            else
                otherDirections.Add(pair.Key);
        }

        if (whiteDirection == default || otherDirections.Count < 2)
            throw new SystemException();
        
        var sides = new List<Vector3Int>(colorDict.Keys);
        sides.RemoveAll(dir => dir.Equals(Vector3Int.up) || dir.Equals(Vector3Int.down));

        return (whiteDirection, otherDirections, sides);
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

    private static CubePart GetCorrectCornerPosition(CubePart part)
    {
        CubePart wantedUpCorner = default;

        (_, List<Vector3Int> otherDirections, _) = GetCornerDirections(part);
        Dictionary<Vector3Int, Color> colorDict = part.GetSideColors();

        CubeSlice rotationSlice = part.GetPosition().y == 1
            ? new CubeSlice(Vector3Int.up)
            : new CubeSlice(Vector3Int.down);
        
        foreach (CubePart corner in rotationSlice.GetCorners())
        {
            var centerColors = new List<Color>();

            List<CubeSlice> slices = corner.GetSlices();
            slices.Remove(rotationSlice);
            foreach (CubeSlice slice in slices)
                centerColors.Add(slice.GetCenter().GetSideColors()[slice.GetDirection()]);

            var correctCorner = true; 
            foreach (Vector3Int otherDirection in otherDirections)
            {
                if (!centerColors.Contains(colorDict[otherDirection]))
                {
                    correctCorner = false;
                    break;
                }
            }

            if (correctCorner)
            {
                wantedUpCorner = corner;
                break;
            }
        }

        if (wantedUpCorner == default)
            throw new SystemException();

        return wantedUpCorner;
    }
}