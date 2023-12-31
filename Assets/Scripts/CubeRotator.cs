using System;
using System.Collections.Generic;
using CubeUtils;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class CubeRotator : MonoBehaviour
{
    public enum SolvingMode
    {
        Manual,
        Automatic,
        Instant
    }

    public enum Phase
    {
        Scrambling,
        Solving
    }

    public int scrambleStep;
    public bool instantScramble;
    public int solveStep;
    public SolvingMode solvingMode;
    public CubeSolver.State desiredState;
    public GameObject arrowPrefab;
    
    private int _frames;
    private Phase _phase;
    private List<Rotation> _rotations;
    private bool _highlighting;
    private GameObject _arrow;

    private CubeSolver _solver;

    // Start is called before the first frame update
    public void Start()
    {
        _phase = Phase.Scrambling;
        _rotations = new List<Rotation>();
        _solver = new CubeSolver();
        _arrow = Instantiate(arrowPrefab);
        _arrow.SetActive(false);
        _arrow.transform.localScale = new Vector3(.15f, .15f, .15f);

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
                    transform.LookAt(Utils.GetLookDirection(rotation.FacingDirection));
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
            _phase = Phase.Solving;
            
            _solver.CheckState();
            
            bool shouldRotate = Input.GetKeyDown(KeyCode.P) 
                                || (solvingMode == SolvingMode.Automatic && _solver.CurrentState <= desiredState);
            if (shouldRotate)
            {
                List<Rotation> rotations = _solver.SolveStep();
                _rotations.AddRange(rotations);
            }
            
            CubePart partToSolve = _solver.GetPartToSolve();
            if (partToSolve)
            {
                if (Input.GetKeyDown(KeyCode.O))
                    partToSolve.SetHighlight(true);
                else if (Input.GetKeyUp(KeyCode.O))
                    partToSolve.SetHighlight(false);
            }

            Vector3Int? facingDirection = _solver.GetSolveFacingDirection();
            if (facingDirection.HasValue)
            {
                if (Input.GetKeyDown(KeyCode.O))
                {
                    _arrow.SetActive(true);
                    _arrow.transform.position = facingDirection.Value * 5;
                    _arrow.transform.LookAt(Vector3.zero);
                }
                else if (Input.GetKeyUp(KeyCode.O))
                    _arrow.SetActive(false);
            }
        }
        
        if (_rotations.Count == 0) 
            return;
        
        Rotation curRotation = _rotations[0];
        
        transform.LookAt(Utils.GetLookDirection(curRotation.FacingDirection));

        int degreesPerStep = _phase == Phase.Scrambling ? scrambleStep : solveStep;
        
        // 50 steps per second
        float ratioOfStep = Time.deltaTime * 50;
        
        float step = curRotation.Degrees > 0 
                       ? Math.Min(curRotation.Degrees, degreesPerStep * ratioOfStep) 
                       : Math.Max(curRotation.Degrees, -degreesPerStep * ratioOfStep);
        
        curRotation.Slice.Rotate(step);
        curRotation.Degrees -= step;
        
        transform.LookAt(Vector3.forward);

        if (curRotation.Degrees == 0)
            _rotations.RemoveAt(0);
        else
            _rotations[0] = curRotation;
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
                _rotations.Add(new Rotation(randomDirection, randomPosition, randomDegrees, Vector3Int.back));
        }
    }
}
