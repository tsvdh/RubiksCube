using System;
using System.Collections.Generic;
using CubeUtils;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class CubeRotator : MonoBehaviour
{
    public int degreesPerStep;

    private int _frames;
    private List<Rotation> _rotations;

    // Start is called before the first frame update
    public void Start()
    {
        _rotations = new List<Rotation>();
        
        Scramble();
    }

    // FixedUpdate is called once per logic frame
    public void FixedUpdate()
    {
        if (_frames++ < 10)
            return;
        
        if (_rotations.Count == 0)
            return;

        Rotation curRotation = _rotations[0];

        int step = curRotation.Degrees > 0
            ? Math.Min(curRotation.Degrees, degreesPerStep)
            : Math.Max(curRotation.Degrees, -degreesPerStep);

        curRotation.Slice.Rotate(step);
        curRotation.Degrees -= step;

        if (curRotation.Degrees == 0)
            _rotations.RemoveAt(0);
        else
            _rotations[0] = curRotation;
    }

    public void Scramble()
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
            
            _rotations.Add(new Rotation(randomDirection, randomPosition, randomDegrees));
        }
    }
}
