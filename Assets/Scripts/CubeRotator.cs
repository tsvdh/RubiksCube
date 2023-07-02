using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class CubeRotator : MonoBehaviour
{
    private enum Dimension
    {
        X, Y, Z
    }
    
    private struct Rotation
    {
        public Rotation(Dimension direction, int position, int degrees)
        {
            Direction = direction;
            Position = position;
            Degrees = degrees;
        }
        
        public Dimension Direction;
        public int Position;
        public int Degrees;
    }

    public int degreesPerStep;

    private int _frames = 0;
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

        Vector3 rotationAxis = curRotation.Direction switch
        {
            Dimension.X => Vector3.right,
            Dimension.Y => Vector3.up,
            Dimension.Z => Vector3.back,
            _ => throw new ArgumentOutOfRangeException()
        };

        int step = curRotation.Degrees > 0
            ? Math.Min(curRotation.Degrees, degreesPerStep)
            : Math.Max(curRotation.Degrees, -degreesPerStep);

        foreach (Transform child in transform)
        {
            Vector3 childPos = child.position;
            float dimensionPos = curRotation.Direction switch
            {
                Dimension.X => childPos.x,
                Dimension.Y => childPos.y,
                Dimension.Z => childPos.z,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (Mathf.RoundToInt(dimensionPos) == curRotation.Position)
            {
                child.RotateAround(Vector3.zero, rotationAxis, step);
            }
        }

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
            Dimension randomDimension = Random.Range(0, 3) switch
            {
                0 => Dimension.X,
                1 => Dimension.Y,
                2 => Dimension.Z,
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
            
            _rotations.Add(new Rotation(randomDimension, randomPosition, randomDegrees));
        }
    }
}
