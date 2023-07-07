using System;
using UnityEngine;

namespace CubeUtils
{
    public enum Color
    {
        Black,
        Blue,
        Green,
        Orange,
        Red,
        White,
        Yellow
    }

    public enum Direction
    {
        X, Y, Z
    }
    
    public struct Rotation
    {

        public Rotation(Direction direction, int position, int degrees, Vector3 viewDirection)
        {
            Slice = new CubeSlice(direction, position);
            Degrees = degrees;
            ViewDirection = viewDirection;
        }

        public CubeSlice Slice;
        public int Degrees;
        public Vector3 ViewDirection;
    }
}
