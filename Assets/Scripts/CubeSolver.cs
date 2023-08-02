using System;
using System.Collections.Generic;
using CubeUtils;
using JetBrains.Annotations;
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
            
            case State.MiddleEdges:
                return SolveMiddleEdges();
            
            case State.YellowCross:
                return SolveYellowCross();
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private List<Rotation> SolveWhiteCenter()
    {
        var rotations = new List<Rotation>();
        CubePart partToMove = GetWhiteCenterToSolve();
                
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
            partToMove = GetWhiteEdgeToSolve();

        (Vector3Int whiteDirection, Vector3Int otherDirection) = GetEdgeDirections(partToMove);

        if (whiteDirection.Equals(Vector3Int.down) || whiteDirection.Equals(Vector3Int.up))
        {
            // Find correct position in layer
            CubePart correctEdge = GetCorrectEdgePositionInSlice(partToMove);

            if (correctEdge.Equals(partToMove))
            {
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
        CubePart partToMove;
        
        if (_nextPart)
        {
            partToMove = _nextPart;
            _nextPart = null;
        }
        else
            partToMove = GetWhiteCornerToSolve();

        (Vector3Int whiteDirection, _, List<Vector3Int> sides) = GetCornerDirections(partToMove);

        // Find wanted corner position
        CubePart wantedCorner = GetCorrectCornerPositionInSlice(partToMove);

        Vector3Int otherSide;
        int upperDegrees;
        int sideDegrees;

        if (partToMove.GetPosition().y == -1)
        {
            // In bottom slice
            // In wrong position, find twisting side and twist up
            Vector3Int rotationSide = whiteDirection.Equals(Vector3Int.down)
                ? sides[0]
                : whiteDirection;
            
            sides.Remove(rotationSide);
            otherSide = sides[0];

            var rotationSlice = new CubeSlice(rotationSide);
            
            sideDegrees = rotationSlice.GetRotationDegrees(otherSide, Vector3Int.up);
            upperDegrees = new CubeSlice(Vector3Int.up).GetRotationDegrees(rotationSide, otherSide);
            
            rotations.Add(new Rotation(rotationSlice, sideDegrees, Vector3Int.back));
            rotations.Add(new Rotation(Direction.Y, 1, upperDegrees, Vector3Int.back));
            rotations.Add(new Rotation(rotationSlice, -sideDegrees, Vector3Int.back));
            return rotations;
        }
        
        var upSlice = new CubeSlice(Vector3Int.up);

        // In upper slice
        if (!partToMove.Equals(wantedCorner))
        {
            // rotate to wanted corner
            upperDegrees = upSlice.GetRotationDegrees(partToMove, wantedCorner);
            rotations.Add(new Rotation(upSlice, upperDegrees, Vector3Int.back));
            
            _nextPart = partToMove;
            return rotations;
        }

        CubeSlice sideSlice;

        if (whiteDirection.Equals(Vector3Int.up))
        {
            // Orient white to side
            (Vector3Int _, List<Vector3Int> _, List<Vector3Int> newSides) = GetCornerDirections(partToMove);
            
            sideSlice = new CubeSlice(newSides[0]);
            sideDegrees = sideSlice.GetRotationDegrees(newSides[1], Vector3Int.up);
            upperDegrees = upSlice.GetRotationDegrees(newSides[0], newSides[1]);
            
            rotations.Add(new Rotation(sideSlice, sideDegrees, Vector3Int.back));
            rotations.Add(new Rotation(upSlice, upperDegrees * 2, Vector3Int.back));
            rotations.Add(new Rotation(sideSlice, -sideDegrees, Vector3Int.back));
            rotations.Add(new Rotation(upSlice, -upperDegrees, Vector3Int.back));

            _nextPart = partToMove;
            return rotations;
        }
        
        // put corner in correct position
        (Vector3Int newWhiteDirection, List<Vector3Int> newOtherDirections, List<Vector3Int> _) = GetCornerDirections(partToMove);
        
        newOtherDirections.RemoveAll(dir => dir.Equals(Vector3Int.up));
        otherSide = newOtherDirections[0];
        sideSlice = new CubeSlice(newWhiteDirection);

        sideDegrees = sideSlice.GetRotationDegrees(otherSide, Vector3Int.up);
        upperDegrees = upSlice.GetRotationDegrees(newWhiteDirection, otherSide);
        
        rotations.Add(new Rotation(sideSlice, sideDegrees, Vector3Int.back));
        rotations.Add(new Rotation(upSlice, upperDegrees, Vector3Int.back));
        rotations.Add(new Rotation(sideSlice, -sideDegrees, Vector3Int.back));

        return rotations;
    }

    private List<Rotation> SolveMiddleEdges()
    {
        var rotations = new List<Rotation>();
        CubePart partToMove;
        
        if (_nextPart)
        {
            partToMove = _nextPart;
            _nextPart = null;
        }
        else
            partToMove = GetMiddleEdgeToSolve();

        Dictionary<Vector3Int, Color> colorDict = partToMove.GetSideColors();
        var upSlice = new CubeSlice(Vector3Int.up);
        int upperDegrees;
        
        if (partToMove.GetPosition().y == 0)
        {
            // Part in middle, rotate to top
            var sides = new List<Vector3Int>(colorDict.Keys);

            var rotationSlice = new CubeSlice(sides[0]);
            
            int sideDegrees = rotationSlice.GetRotationDegrees(sides[1], Vector3Int.up);
            upperDegrees = upSlice.GetRotationDegrees(sides[0], sides[1]);
            
            rotations.Add(new Rotation(rotationSlice, sideDegrees, Vector3Int.back));
            rotations.Add(new Rotation(upSlice, upperDegrees, Vector3Int.back));
            rotations.Add(new Rotation(rotationSlice, -sideDegrees, Vector3Int.back));
            rotations.Add(new Rotation(upSlice, -upperDegrees, Vector3Int.back));

            return rotations;
        }
        
        // Find wanted part and up color direction
        Color upColor = colorDict[Vector3Int.up];
        var directions = new List<Vector3Int>(colorDict.Keys);
        directions.Remove(Vector3Int.up);

        Vector3Int facingSideDirection = directions[0];
        Color facingSideColor = colorDict[facingSideDirection];
        Vector3Int upColorDirection = default;
        CubePart wantedPart = default;

        foreach (Vector3Int side in _sides)
        {
            Color sideColor = new CubeSlice(side).GetCenter().GetSideColors()[side];
            if (sideColor == upColor)
                upColorDirection = side;

            if (sideColor == facingSideColor)
            {
                foreach (CubePart edge in new CubeSlice(side).GetEdges())
                {
                    if (edge.GetPosition().y == 1)
                        wantedPart = edge;
                }
            }
        }

        if (wantedPart == default)
            throw new SystemException();

        if (!partToMove.Equals(wantedPart))
        {
            // Not in correct position, orient in top
            upperDegrees = upSlice.GetRotationDegrees(partToMove, wantedPart);
            rotations.Add(new Rotation(upSlice, upperDegrees, Vector3Int.back));

            _nextPart = partToMove;
            return rotations;
        }

        Vector3Int invertedUpColorDirection = -upColorDirection;
        var facingSideSlice = new CubeSlice(facingSideDirection);
        var upColorSideSlice = new CubeSlice(upColorDirection);

        upperDegrees = upSlice.GetRotationDegrees(facingSideDirection, invertedUpColorDirection);
        int upColorSideDegrees = upColorSideSlice.GetRotationDegrees(facingSideDirection, Vector3Int.up);
        int facingSideDegrees = facingSideSlice.GetRotationDegrees(Vector3Int.up, invertedUpColorDirection);
        
        // perform algorithm
        rotations.Add(new Rotation(upSlice, upperDegrees, Vector3Int.back));
        rotations.Add(new Rotation(upColorSideSlice, upColorSideDegrees, Vector3Int.back));
        rotations.Add(new Rotation(upSlice, -upperDegrees, Vector3Int.back));
        rotations.Add(new Rotation(upColorSideSlice, -upColorSideDegrees, Vector3Int.back));
        
        rotations.Add(new Rotation(upSlice, -upperDegrees, Vector3Int.back));
        rotations.Add(new Rotation(facingSideSlice, facingSideDegrees, Vector3Int.back));
        rotations.Add(new Rotation(upSlice, upperDegrees, Vector3Int.back));
        rotations.Add(new Rotation(facingSideSlice, -facingSideDegrees, Vector3Int.back));

        return rotations;
    }

    private List<Rotation> SolveYellowCross()
    {
        var rotations = new List<Rotation>();

        (Vector3Int correctFacingSide, bool angled) = GetYellowCrossDirection();

        if (angled)
        {
            // FUR U`R`F`
            rotations.Add(new Rotation(Direction.Z, -1, -90, correctFacingSide));
            rotations.Add(new Rotation(Direction.Y, 1, 90, correctFacingSide));
            rotations.Add(new Rotation(Direction.X, 1, 90, correctFacingSide));
            
            rotations.Add(new Rotation(Direction.Y, 1, -90, correctFacingSide));
            rotations.Add(new Rotation(Direction.X, 1, -90, correctFacingSide));
            rotations.Add(new Rotation(Direction.Z, -1, 90, correctFacingSide));
        }
        else
        {
            // FRU R`U`F`
            rotations.Add(new Rotation(Direction.Z, -1, -90, correctFacingSide));
            rotations.Add(new Rotation(Direction.X, 1, 90, correctFacingSide));
            rotations.Add(new Rotation(Direction.Y, 1, 90, correctFacingSide));

            rotations.Add(new Rotation(Direction.X, 1, -90, correctFacingSide));
            rotations.Add(new Rotation(Direction.Y, 1, -90, correctFacingSide));
            rotations.Add(new Rotation(Direction.Z, -1, 90, correctFacingSide));
        }

        return rotations;
    }

    public State CheckState()
    {
        switch (CurrentState)
        {
            case State.WhiteCenter:
                Color color = new CubeSlice(Vector3Int.down).GetCenter().GetSideColors()[Vector3Int.down];
                // Bottom center must be white
                if (color == Color.White)
                {
                    CurrentState = State.WhiteCross;
                    return CheckState();
                }

                return CurrentState;

            case State.WhiteCross:
                foreach (CubePart part in new CubeSlice(Vector3Int.down).GetEdges())
                {
                    if (!IsWhiteEdgeCorrect(part))
                        return CurrentState;
                }

                CurrentState = State.WhiteCorners;
                return CheckState();

            case State.WhiteCorners:
                foreach (CubePart part in new CubeSlice(Vector3Int.down).GetCorners())
                {
                    if (!IsWhiteCornerCorrect(part))
                        return CurrentState;
                }

                CurrentState = State.MiddleEdges;
                return CheckState();

            case State.MiddleEdges:
                foreach (CubePart part in new CubeSlice(Direction.Y, 0).GetEdges())
                {
                    if (!IsMiddleEdgeCorrect(part))
                        return CurrentState;
                }

                CurrentState = State.YellowCross;
                return CheckState();
            
            case State.YellowCross:
                foreach (CubePart edge in new CubeSlice(Vector3Int.up).GetEdges())
                {
                    if (edge.GetSideColors()[Vector3Int.up] != Color.Yellow)
                        return CurrentState;
                }

                CurrentState = State.YellowCorners;
                return CheckState();
            
            case State.YellowCorners:
                return CurrentState;
            
            case State.Solved:
                return CurrentState;
            
            default:
                throw new SystemException();
        }
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

        return edge.Equals(GetCorrectEdgePositionInSlice(edge));
    }

    private static bool IsWhiteCornerCorrect(CubePart corner)
    {
        if (corner.GetPosition().y != -1)
            return false;

        if (corner.GetSideColors()[Vector3Int.down] != Color.White)
            return false;

        return corner.Equals(GetCorrectCornerPositionInSlice(corner));
    }

    private static bool IsMiddleEdgeCorrect(CubePart edge)
    {
        if (edge.GetPosition().y != 0)
            return false;

        foreach (KeyValuePair<Vector3Int,Color> pair in edge.GetSideColors())
        {
            Color centerColor = new CubeSlice(pair.Key).GetCenter().GetSideColors()[pair.Key];
            if (centerColor != pair.Value)
                return false;
        }

        return true;
    }

    private static CubePart GetWhiteCenterToSolve()
    {
        foreach (CubePart part in CubeBuilder.Parts)
        {
            if (part.IsCenter() && part.GetSideColors().ContainsValue(Color.White))
                return part;
        }

        throw new SystemException();
    }

    private static CubePart GetWhiteEdgeToSolve()
    {
        var candidates = new List<CubePart>();
        
        foreach (CubePart part in CubeBuilder.Parts)
        {
            // Add white edges that are not correct
            if (part.IsEdge()
                && part.GetSideColors().ContainsValue(Color.White)
                && !IsWhiteEdgeCorrect(part))
            {
                candidates.Add(part);
            }
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

    private static CubePart GetWhiteCornerToSolve()
    {
        var candidates = new List<CubePart>();

        foreach (CubePart part in CubeBuilder.Parts)
        {
            if (part.IsCorner()
                && part.GetSideColors().ContainsValue(Color.White)
                && !IsWhiteCornerCorrect(part))
            {
                candidates.Add(part);
            }
        }

        candidates.Sort((a, b) =>
        {
            // Order deterministically: top to bottom, white not up, x and z
        
            Vector3Int aPos = a.GetPosition();
            Vector3Int bPos = b.GetPosition();

            int heightDiff = bPos.y - aPos.y;
            if (heightDiff != 0)
                return heightDiff;

            if (aPos.y == 1 && bPos.y == 1)
            {
                int aWhiteTop = a.GetSideColors()[Vector3Int.up] == Color.White ? 0 : 1;
                int bWhiteTop = b.GetSideColors()[Vector3Int.up] == Color.White ? 0 : 1;
                int topDiff = bWhiteTop - aWhiteTop;
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

    private static CubePart GetMiddleEdgeToSolve()
    {
        var candidates = new List<CubePart>();

        foreach (CubePart part in CubeBuilder.Parts)
        {
            Dictionary<Vector3Int, Color> colorDict = part.GetSideColors();
            if (part.IsEdge() 
                && !colorDict.ContainsValue(Color.White)
                && !colorDict.ContainsValue(Color.Yellow) 
                && !IsMiddleEdgeCorrect(part))
            {
                candidates.Add(part);
            }
        }

        candidates.Sort((a, b) =>
        {
            // Order deterministically: top to bottom, x and z
        
            Vector3Int aPos = a.GetPosition();
            Vector3Int bPos = b.GetPosition();

            int heightDiff = bPos.y - aPos.y;
            if (heightDiff != 0)
                return heightDiff;

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

    private (Vector3Int, bool) GetYellowCrossDirection()
    {
        // check amount of yellow edges
        var noYellowEdges = true;
        foreach (CubePart edge in new CubeSlice(Vector3Int.up).GetEdges())
        {
            if (edge.GetSideColors()[Vector3Int.up] == Color.Yellow)
                noYellowEdges = false;
        }
        
        Vector3Int correctFacingSide;
        bool angled;

        if (noYellowEdges)
        {
            correctFacingSide = Vector3Int.back;
            angled = true;
        }
        else
        {
            // Two yellow edges exist
            // Find correct facing side
            correctFacingSide = default;
            angled = false;
            var upSlice = new CubeSlice(Vector3Int.up);

            foreach (Vector3Int facingSide in _sides)
            {
                _root.LookAt(Utils.GetLookDirection(facingSide));

                var left = false;
                var up = false;
                var right = false;

                foreach (CubePart edge in upSlice.GetEdges())
                {
                    Vector3Int edgePos = edge.GetPosition();
                    bool yellowUp = edge.GetSideColors()[Vector3Int.up] == Color.Yellow;
                    if (!yellowUp)
                        continue;

                    // left
                    if (edgePos.x == -1)
                        left = true;
                    // right
                    else if (edgePos.x == 1)
                        right = true;
                    // up
                    else if (edgePos.z == 1)
                        up = true;
                }

                if (!left)
                    continue;

                if (up || right)
                {
                    correctFacingSide = facingSide;
                    if (up)
                        angled = true;

                    break;
                }
            }
            
            _root.LookAt(Vector3.forward);

            if (correctFacingSide == default)
                throw new SystemException();
        }

        return (correctFacingSide, angled);
    }
    
    // private (Vector3Int, Vector3Int) GetYellowCornersDirection()
    // {
    //     
    // }
    
    [CanBeNull]
    public CubePart GetPartToSolve()
    {
        return CurrentState switch
        {
            State.WhiteCenter => GetWhiteCenterToSolve(),
            State.WhiteCross => _nextPart ? _nextPart : GetWhiteEdgeToSolve(),
            State.WhiteCorners => _nextPart ? _nextPart : GetWhiteCornerToSolve(),
            State.MiddleEdges => _nextPart ? _nextPart : GetMiddleEdgeToSolve(),
            _ => null
        };
    }
    
    public Vector3Int? GetSolveFacingDirection()
    {
        return CurrentState switch
        {
            State.YellowCross => GetYellowCrossDirection().Item1,
            // State.YellowCorners => GetYellowCornersDirection().Item1,
            _ => null
        };
    }
}