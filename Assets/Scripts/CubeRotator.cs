using System;
using System.Collections.Generic;
using CubeUtils;
using UnityEngine;
using Random = UnityEngine.Random;

public class CubeRotator : MonoBehaviour
{
    public enum SolvingMode
    {
        Manual,
        Automatic,
        Instant
    }
    
    public int degreesPerStep;
    public bool instantScramble;
    public SolvingMode solvingMode;
    public CubeSolver.State desiredState;

    private int _frames;
    private List<Rotation> _rotations;
    private bool _highlighting;

    private CubeSolver _solver;

    // Start is called before the first frame update
    public void Start()
    {
        _rotations = new List<Rotation>();
        _solver = new CubeSolver();
        
        Scramble(instantScramble);
        _solver.CheckState();

        if (solvingMode == SolvingMode.Instant)
        {
            for (var i = 0; i < 100; i++)
            {
                if (_solver.CheckState() > desiredState)
                    break;
                
                foreach (Rotation rotation in _solver.SolveStep())
                {
                    transform.LookAt(GetLookDirection(rotation));
                    rotation.Slice.Rotate(rotation.Degrees);
                    transform.LookAt(Vector3.forward);
                }
            }
        }
    }

    // Update is called once per frame
    public void Update()
    {
        if (_frames++ < 10)
            return;

        if (_rotations.Count == 0)
        {
            _solver.CheckState();
            
            bool shouldRotate = Input.GetKeyDown(KeyCode.P) 
                                || (solvingMode == SolvingMode.Automatic && _solver.CurrentState <= desiredState);
            if (shouldRotate)
            {
                Debug.Log($"Current State: {Enum.GetName(typeof(CubeSolver.State), _solver.CurrentState)}");

                List<Rotation> rotations = _solver.SolveStep();

                _rotations.AddRange(rotations);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.O))
            _solver.GetPartToSolve().SetHighlight(true);
        if (Input.GetKeyUp(KeyCode.O))
            _solver.GetPartToSolve().SetHighlight(false);
    }

    // FixedUpdate is called once per logic frame
    public void FixedUpdate()
    {
        if (_rotations.Count == 0)
                return;
        
        Rotation curRotation = _rotations[0];
        
        transform.LookAt(GetLookDirection(curRotation));

        int step = curRotation.Degrees > 0
            ? Math.Min(curRotation.Degrees, degreesPerStep)
            : Math.Max(curRotation.Degrees, -degreesPerStep);

        curRotation.Slice.Rotate(step);
        curRotation.Degrees -= step;
        
        transform.LookAt(Vector3.forward);

        if (curRotation.Degrees == 0)
            _rotations.RemoveAt(0);
        else
            _rotations[0] = curRotation;
    }

    private static Vector3Int GetLookDirection(Rotation rotation)
    {
        Vector3Int lookDirection = Vector3Int.RoundToInt(rotation.FacingDirection);
        lookDirection.z *= -1;
        return lookDirection;
    }

    private void Scramble(bool instant)
    {
        for (var i = 0; i < 25; i++)
        {
            Direction randomDirection = Random.Range(0, 3) switch
            {
                0 => Direction.X,
                1 => Direction.Y,
                2 => Direction.Z,
                _ => throw new ArgumentOutOfRangeException()
            };

            int randomPosition = Random.Range(-1, 2);

            int randomDegrees = Random.Range(0, 4) switch
            {
                0 => 90,
                1 => 180,
                2 => -90,
                3 => -180,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            if (instant)
                new CubeSlice(randomDirection, randomPosition).Rotate(randomDegrees);
            else
                _rotations.Add(new Rotation(randomDirection, randomPosition, randomDegrees, Vector3Int.forward));
        }
    }
}
