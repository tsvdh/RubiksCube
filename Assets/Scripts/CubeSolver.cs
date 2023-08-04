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
    private static Transform _root;
    private static List<Vector3Int> _sides;
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
    
    // --- START Routing Functions ---
    
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
            State.YellowCorners => GetYellowCornersDirection(),
            State.TopCorners => GetTopCornersDirection(),
            _ => null
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
            
            case State.YellowCorners:
                return SolveYellowCorners();
            
            case State.TopCorners:
                return SolveTopCorners();
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    // --- END Routing Functions ---
    
    // --- START Solving ---

    private List<Rotation> SolveWhiteCenter()
    {
        var rotations = new List<Rotation>();
        CubePart partToMove = GetWhiteCenterToSolve();
        if (!partToMove)
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
            if (!partToMove)
                throw new SystemException();
        }

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
        {
            partToMove = GetWhiteCornerToSolve();
            if (!partToMove)
                throw new SystemException();
        }

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
        {
            partToMove = GetMiddleEdgeToSolve();
            if (!partToMove)
                throw new SystemException();
        }

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

            CurrentState = State.WhiteCorners;

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

    private List<Rotation> SolveYellowCorners()
    {
        var rotations = new List<Rotation>();

        Vector3Int correctFacingSide = GetYellowCornersDirection();
        
        // RUR` U RU2R`
        
        rotations.Add(new Rotation(Direction.X, 1, 90, correctFacingSide));
        rotations.Add(new Rotation(Direction.Y, 1, 90, correctFacingSide));
        rotations.Add(new Rotation(Direction.X, 1, -90, correctFacingSide));
        
        rotations.Add(new Rotation(Direction.Y, 1, 90, correctFacingSide));
        
        rotations.Add(new Rotation(Direction.X, 1, 90, correctFacingSide));
        rotations.Add(new Rotation(Direction.Y, 1, 180, correctFacingSide));
        rotations.Add(new Rotation(Direction.X, 1, -90, correctFacingSide));
        
        return rotations;
    }

    private List<Rotation> SolveTopCorners()
    {
        var rotations = new List<Rotation>();

        var upSlice = new CubeSlice(Vector3Int.up);
        int? degreesForCorrectUp = null;

        // Try every up slice rotation
        foreach (Vector3Int sideDirection in _sides)
        {
            int degrees = upSlice.GetRotationDegrees(Vector3Int.back, sideDirection);
            
            upSlice.Rotate(degrees);

            var correctCorners = 0;
            foreach (CubePart corner in upSlice.GetCorners())
            {
                if (corner.Equals(GetCorrectCornerPositionInSlice(corner)))
                    correctCorners++;
            }
            
            upSlice.Rotate(-degrees);
            
            if (correctCorners == 2 || correctCorners == 4)
            {
                // 2 correct if corners not solved, 4 if corners fully solved
                // both cannot happen in same state
                if (!degreesForCorrectUp.HasValue || Math.Abs(degrees) < Math.Abs(degreesForCorrectUp.Value))
                    degreesForCorrectUp = degrees;
            }
        }

        if (!degreesForCorrectUp.HasValue)
            throw new SystemException();

        if (degreesForCorrectUp.Value != 0)
        {
            rotations.Add(new Rotation(Direction.Y, 1, degreesForCorrectUp.Value, Vector3Int.back));
            return rotations;
        }

        Vector3Int correctFacingSide = GetTopCornersDirection().GetValueOrDefault();

        if (correctFacingSide == default)
            throw new SystemException();
        
        // R` F R` B2 R F` R` B2 R2
        rotations.Add(new Rotation(Direction.X, 1, -90, correctFacingSide));
        rotations.Add(new Rotation(Direction.Z, -1, -90, correctFacingSide));
        rotations.Add(new Rotation(Direction.X, 1, -90, correctFacingSide));
        rotations.Add(new Rotation(Direction.Z, 1, 180, correctFacingSide));
        rotations.Add(new Rotation(Direction.X, 1, 90, correctFacingSide));
        rotations.Add(new Rotation(Direction.Z, -1, 90, correctFacingSide));
        rotations.Add(new Rotation(Direction.X, 1, -90, correctFacingSide));
        rotations.Add(new Rotation(Direction.Z, 1, 180, correctFacingSide));
        rotations.Add(new Rotation(Direction.X, 1, 180, correctFacingSide));
        
        return rotations;
    }
    
    // --- END Solving ---
    
    // --- START State Check ---

    public State CheckState()
    {
        var upSlice = new CubeSlice(Vector3Int.up);
        
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
                foreach (CubePart edge in upSlice.GetEdges())
                {
                    if (edge.GetSideColors()[Vector3Int.up] != Color.Yellow)
                        return CurrentState;
                }

                CurrentState = State.YellowCorners;
                return CheckState();
            
            case State.YellowCorners:
                foreach (CubePart corner in upSlice.GetCorners())
                {
                    if (corner.GetSideColors()[Vector3Int.up] != Color.Yellow)
                        return CurrentState;
                }
                
                CurrentState = State.TopCorners;
                return CheckState();
            
            case State.TopCorners:
                foreach (CubePart corner in upSlice.GetCorners())
                {
                    if (!corner.Equals(GetCorrectCornerPositionInSlice(corner)))
                        return CurrentState;
                }

                CurrentState = State.TopEdges;
                return CheckState();

            case State.TopEdges:
                foreach (CubePart edge in upSlice.GetEdges())
                {
                    if (!edge.Equals(GetCorrectEdgePositionInSlice(edge)))
                        return CurrentState;
                }

                CurrentState = State.Solved;
                return CurrentState;
            
            case State.Solved:
                return CurrentState;
            
            default:
                throw new SystemException();
        }
    }
    
    // --- END State Check ---
    
    // --- START Utilities ---

    private static (Vector3Int, Vector3Int) GetEdgeDirections(CubePart part)
    {
        if (!part.IsEdge())
            throw new SystemException();
        
        Dictionary<Vector3Int, Color> colorDict = part.GetSideColors();
        if (!colorDict.ContainsValue(Color.White) && !colorDict.ContainsValue(Color.Yellow))
            throw new SystemException();

        Vector3Int significantDirection = default;
        Vector3Int otherDirection = default;

        foreach ((Vector3Int dir, Color color) in colorDict)
        {
            if (color == Color.White || color == Color.Yellow)
                significantDirection = dir;
            else
                otherDirection = dir;
        }

        if (significantDirection == default || otherDirection == default)
            throw new SystemException();

        return (significantDirection, otherDirection);
    }

    private static (Vector3Int, List<Vector3Int>, List<Vector3Int>) GetCornerDirections(CubePart part)
    {
        if (!part.IsCorner())
            throw new SystemException();
        
        Dictionary<Vector3Int, Color> colorDict = part.GetSideColors();

        Vector3Int significantDirection = default;
        var otherDirections = new List<Vector3Int>();

        foreach ((Vector3Int dir, Color color) in colorDict)
        {
            if (color == Color.White || color == Color.Yellow)
                significantDirection = dir;
            else
                otherDirections.Add(dir);
        }

        if (significantDirection == default || otherDirections.Count < 2)
            throw new SystemException();
        
        var sides = new List<Vector3Int>(colorDict.Keys);
        sides.RemoveAll(dir => dir.Equals(Vector3Int.up) || dir.Equals(Vector3Int.down));

        return (significantDirection, otherDirections, sides);
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

        foreach ((Vector3Int dir, Color color) in edge.GetSideColors())
        {
            Color centerColor = new CubeSlice(dir).GetCenter().GetSideColors()[dir];
            if (centerColor != color)
                return false;
        }

        return true;
    }
    
    // --- END Utilities ---
    
    // --- START Indicators ---

    [CanBeNull]
    private static CubePart GetWhiteCenterToSolve()
    {
        foreach (CubePart part in CubeBuilder.Parts)
        {
            if (part.IsCenter() && part.GetSideColors().ContainsValue(Color.White))
                return part;
        }

        return null;
    }

    [CanBeNull]
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

        if (candidates.Count == 0)
            return null;
                    
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

    [CanBeNull]
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

        if (candidates.Count == 0)
            return null;
        
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

    [CanBeNull]
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

        if (candidates.Count == 0)
            return null;

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

    private static (Vector3Int, bool) GetYellowCrossDirection()
    {
        // check amount of yellow edges
        var upSlice = new CubeSlice(Vector3Int.up);
        var noYellowEdges = true;
        foreach (CubePart edge in upSlice.GetEdges())
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
    
    private static Vector3Int GetYellowCornersDirection()
    {
        // Check amount of yellow corners
        var upSlice = new CubeSlice(Vector3Int.up);
        var amountYellowCorners = 0;
        foreach (CubePart corner in upSlice.GetCorners())
        {
            if (corner.GetSideColors()[Vector3Int.up] == Color.Yellow)
                amountYellowCorners++;
        }

        Vector3Int correctSide = default;

        foreach (Vector3Int facingSide in _sides)
        {
            _root.LookAt(Utils.GetLookDirection(facingSide));

            CubePart bottomLeft = default;
            foreach (CubePart corner in upSlice.GetCorners())
            {
                Vector3Int cornerPos = corner.GetPosition();
                if (cornerPos.x == -1 && cornerPos.z == -1)
                    bottomLeft = corner;
            }

            if (bottomLeft == default)
                throw new SystemException();

            Vector3Int yellowDirection = default;
            foreach ((Vector3Int dir, Color color) in bottomLeft.GetSideColors())
            {
                if (color == Color.Yellow)
                    yellowDirection = dir;
            }

            if (yellowDirection == default)
                throw new SystemException();

            if (amountYellowCorners == 0 && yellowDirection.Equals(Vector3Int.left))
            {
                correctSide = facingSide;
                break;
            }

            if (amountYellowCorners == 1 && yellowDirection.Equals(Vector3Int.up))
            {
                correctSide = facingSide;
                break;
            }

            if (amountYellowCorners == 2 && yellowDirection.Equals(Vector3Int.back))
            {
                correctSide = facingSide;
            }
        }

        _root.LookAt(Vector3.forward);

        if (correctSide == default)
            throw new SystemException();

        return correctSide;
    }

    private static Vector3Int? GetTopCornersDirection()
    {
        Vector3Int correctSide = default;
        
        foreach (Vector3Int facingSide in _sides)
        {
            _root.LookAt(Utils.GetLookDirection(facingSide));

            CubePart topRight = default;
            CubePart topLeft = default;
            CubePart bottomLeft = default;

            foreach (CubePart corner in new CubeSlice(Vector3Int.up).GetCorners())
            {
                Vector3Int cornerPos = corner.GetPosition();

                if (cornerPos.x == 1 && cornerPos.z == 1)
                    topRight = corner;

                if (cornerPos.x == -1 && cornerPos.z == 1)
                    topLeft = corner;

                if (cornerPos.x == -1 && cornerPos.z == -1)
                    bottomLeft = corner;
            }

            if (topRight == default || topLeft == default || bottomLeft == default)
                throw new SystemException();
            
            if (!topRight.Equals(GetCorrectCornerPositionInSlice(topRight)))
                continue;

            if (topLeft.Equals(GetCorrectCornerPositionInSlice(topLeft))
                || bottomLeft.Equals(GetCorrectCornerPositionInSlice(bottomLeft)))
            {
                correctSide = facingSide;
                break;
            }
        }
        
        _root.LookAt(Vector3.forward);

        if (correctSide == default)
            return null;

        return correctSide;
    }
    
    // --- END Indicators ---
}