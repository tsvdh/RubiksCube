using System;
using System.Collections.Generic;
using System.Linq;
using CubeUtils;
using JetBrains.Annotations;
using UnityEngine;
using Color = CubeUtils.Color;
using Debug = System.Diagnostics.Debug;

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
    [CanBeNull] private CubePart _nextPart;

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
        switch (CurrentState)
        {
            case State.WhiteCenter:
                return SolveWhiteCenter();
            
            case State.WhiteCross:
                return SolveWhiteCross();

            case State.WhiteCorners:
                return SolveWhiteCorners();
            
            default:
                return new List<Rotation>();
        }
    }

    private List<Rotation> SolveWhiteCenter()
    {
        var rotations = new List<Rotation>();
        CubePart partToMove = default;
        
        // Find white center
        foreach (CubePart part in CubeBuilder.Parts)
        {
            if (part.IsCenter() && part.GetSideColors().ContainsValue(Color.White))
            {
                partToMove = part;
                break;
            }
        }

        if (partToMove == default)
            throw new SystemException();
                
        Vector3Int direction = new List<Vector3Int>(partToMove.GetSideColors().Keys)[0];

        if (direction.Equals(Vector3Int.down))
            return rotations;

        if (direction.Equals(Vector3Int.up))
            rotations.Add(new Rotation(Direction.X, 0, 180, Vector3Int.back));
        else
            rotations.Add(new Rotation(Direction.X, 0, -90, direction));

        return rotations;
    }

    private List<Rotation> SolveWhiteCross()
    {
        var rotations = new List<Rotation>();
        CubePart partToMove;
        
        if (_nextPart)
        {
            partToMove = _nextPart;
            _nextPart = null;
        }
        else
        {
            partToMove = GetWhiteEdgeToSolve();
        }
        
        (Vector3Int whiteDirection, Vector3Int otherDirection) = GetEdgeDirections(partToMove);

        if (whiteDirection.Equals(Vector3Int.down) || whiteDirection.Equals(Vector3Int.up))
        {
            // Find correct position in layer
            CubePart correctEdge = GetCorrectEdgePositionInSlice(partToMove);

            if (correctEdge.GetPosition().Equals(partToMove.GetPosition()))
            {
                // In correct position
                if (whiteDirection.Equals(Vector3Int.down))
                {
                    // Done
                    return rotations;
                }
                    
                // White pointing up, rotate down
                rotations.Add(new Rotation(Direction.Z, -1, 180, otherDirection));
                return rotations;
            }
            
            // Not in correct position
            if (whiteDirection.Equals(Vector3Int.down))
            {
                // Edge in wrong slice, rotate up
                rotations.Add(new Rotation(Direction.Z, -1, 180, otherDirection));
                return rotations;
            }

            var upSlice = new CubeSlice(Vector3Int.up);
            int upperDegrees = upSlice.GetRotationDegrees(partToMove, correctEdge);
            
            rotations.Add(new Rotation(upSlice, upperDegrees, Vector3Int.back));
            _nextPart = partToMove;
            
            return rotations;
        }
        
        if (partToMove.GetPosition().y == -1)
        {
            // Lower layer but not facing down, so just put in top layer
            rotations.Add(new Rotation(Direction.Z, -1, 180, whiteDirection));
            return rotations;
        }
        
        if (partToMove.GetPosition().y == 1)
        {
            // Upper layer not facing up, so orient to face up
            rotations.Add(new Rotation(Direction.Z, -1, -90, whiteDirection));
            rotations.Add(new Rotation(Direction.X, 1, 90, whiteDirection));
            rotations.Add(new Rotation(Direction.Y, 1, -90, whiteDirection));
            rotations.Add(new Rotation(Direction.X, 1, -90, whiteDirection));
            rotations.Add(new Rotation(Direction.Z, -1, 90, whiteDirection));
            return rotations;
        }

        // In middle layer, put in top layer with white up
        var sideSlice = new CubeSlice(otherDirection);
        int degrees = sideSlice.GetRotationDegrees(whiteDirection, Vector3Int.up);

        rotations.Add(new Rotation(sideSlice, degrees, Vector3Int.back));
        rotations.Add(new Rotation(Direction.Y, 1, 90, Vector3Int.back));
        rotations.Add(new Rotation(sideSlice, -degrees, Vector3Int.back));
        return rotations;
    }

    private List<Rotation> SolveWhiteCorners()
    {
        var rotations = new List<Rotation>();
        CubePart partToMove = default;

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
            CubePart wantedCorner = GetCorrectCornerPositionInSlice(part);

            Vector3Int otherSide;
            int upperDegrees;

            Vector3Int partPos = part.GetPosition();
            Vector3Int wantedCornerPos = wantedCorner.GetPosition();
            if (partPos.y == -1)
            {
                // In bottom slice
                if (partPos.Equals(wantedCornerPos) && colorDict[Vector3Int.down] == Color.White)
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
            int upperOrientationDegrees = upSlice.GetRotationDegrees(part, wantedCorner);
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
            sideSlice = new CubeSlice(newWhiteDirection);

            rotationSideDegrees = sideSlice.GetRotationDegrees(otherSide, Vector3Int.up);
            upperDegrees = upSlice.GetRotationDegrees(newWhiteDirection, otherSide);
            
            rotations.Add(new Rotation(sideSlice, rotationSideDegrees, Vector3Int.back));
            rotations.Add(new Rotation(upSlice, upperDegrees, Vector3Int.back));
            rotations.Add(new Rotation(sideSlice, -rotationSideDegrees, Vector3Int.back));
            
            // Move view back
            _root.RotateAround(Vector3.zero, Vector3.up, -upperOrientationDegrees);
        }

        throw new SystemException();
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
                    Vector3Int correctPos = GetCorrectCornerPositionInSlice(part).GetPosition();
                    
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
    
    private static Vector3Int InvertVector3Int(Vector3Int inVec)
    {
        return new Vector3Int
        {
            x = inVec.x *= -1,
            y = inVec.y *= -1,
            z = inVec.z *= -1
        };
    }

    private static (Vector3Int, Vector3Int) GetEdgeDirections(CubePart part)
    {
        if (!part.IsEdge())
            throw new SystemException();
        
        Dictionary<Vector3Int, Color> colorDict = part.GetSideColors();
        if (!colorDict.ContainsValue(Color.White))
            throw new SystemException();

        Vector3Int whiteDirection = default;
        Vector3Int otherDirection = default;

        foreach (KeyValuePair<Vector3Int, Color> pair in colorDict)
        {
            if (pair.Value == Color.White)
                whiteDirection = pair.Key;
            else
                otherDirection = pair.Key;
        }

        if (whiteDirection == default || otherDirection == default)
            throw new SystemException();

        return (whiteDirection, otherDirection);
    }

    private static (Vector3Int, List<Vector3Int>, List<Vector3Int>) GetCornerDirections(CubePart part)
    {
        if (!part.IsCorner())
            throw new SystemException();
        
        Dictionary<Vector3Int, Color> colorDict = part.GetSideColors();
        if (!colorDict.ContainsValue(Color.White))
            throw new SystemException();

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

    private static CubePart GetCorrectEdgePositionInSlice(CubePart part)
    {
        CubePart wantedEdge = default;

        (_, Vector3Int otherDirection) = GetEdgeDirections(part);
        Dictionary<Vector3Int, Color> colorDict = part.GetSideColors();
        
        CubeSlice rotationSlice = part.GetPosition().y == 1
            ? new CubeSlice(Vector3Int.up)
            : new CubeSlice(Vector3Int.down);
        
        foreach (CubePart edge in rotationSlice.GetEdges())
        {
            List<CubeSlice> slices = edge.GetSlices();
            // Remove rotation and middle slices
            slices.RemoveAll(slice => slice.Direction == Direction.Y);
            slices.RemoveAll(slice => slice.Position == 0);

            if (slices.Count > 1)
                throw new SystemException();

            Color centerColor = slices[0].GetCenter().GetSideColors()[slices[0].GetDirection()];

            if (centerColor == colorDict[otherDirection])
            {
                wantedEdge = edge;
                break;
            }
        }

        if (wantedEdge == default)
            throw new SystemException();

        return wantedEdge;
    }

    private static CubePart GetCorrectCornerPositionInSlice(CubePart part)
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

    private static bool IsWhiteEdgeCorrect(CubePart edge)
    {
        if (edge.GetPosition().y != -1)
            return false;

        if (edge.GetSideColors()[Vector3Int.down] != Color.White)
            return false;

        return !edge.Equals(GetCorrectEdgePositionInSlice(edge));
    }

    private CubePart GetWhiteEdgeToSolve()
    {
        var candidates = new List<CubePart>();
        
        foreach (CubePart part in CubeBuilder.Parts)
        {
            // Add white edges that are not correct
            if (part.IsEdge() && part.GetSideColors().ContainsValue(Color.White) 
                              && !IsWhiteEdgeCorrect(part))
                candidates.Add(part);
        }
                    
        candidates.Sort((a, b) =>
        {
            // Order deterministically: top to bottom, white up, x and z
        
            Vector3Int aPos = a.GetPosition();
            Vector3Int bPos = b.GetPosition();

            int heightDiff = bPos.y - aPos.y;
            if (heightDiff != 0)
                return heightDiff;

            if (aPos.y == 1 && bPos.y == 1)
            {
                int aWhiteTop = a.GetSideColors()[Vector3Int.up] == Color.White ? 0 : 1;
                int bWhiteTop = b.GetSideColors()[Vector3Int.up] == Color.White ? 0 : 1;
                int topDiff = aWhiteTop - bWhiteTop;
                if (topDiff != 0)
                    return topDiff;
            }

            int xDiff = aPos.x - bPos.x;
            if (xDiff != 0)
                return xDiff;

            int zDiff = aPos.z - bPos.z;
            if (zDiff != 0)
                return zDiff;

            throw new SystemException();
        });

        return candidates[0];
    }
}